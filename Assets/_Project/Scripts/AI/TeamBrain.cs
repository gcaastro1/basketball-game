using System.Collections.Generic;
using UnityEngine;
using Basket.Core;

namespace Basket.AI
{
    // One per team. Turns the current situation into an order per player (TeamOrder) a
    // few times per second; AIAgentControllers of that team execute them. Also keeps the
    // team's own man-to-man map so defensive switches do not need the simulation.
    public sealed class TeamBrain
    {
        private enum ScreenPhase { None, Setting, Set, Rolling }

        private readonly TeamId team;
        private readonly AIConfig config;
        private readonly ITeamStrategy strategy;
        private readonly System.Random rng;
        private readonly TeamOrder[] orders;
        private readonly int[] man;
        private readonly int[] snapshotMatchups;
        private readonly float[] cutUntil;
        private readonly float[] nextCutAllowed;
        private float nextPlanTime = float.NegativeInfinity;
        private float nextSwitchAllowed;

        private TeamId? possessionTeam;
        private int lastHolder = -1;
        private PlayType play;
        private int screener = -1;
        private int screenSide = 1;
        private ScreenPhase screenPhase;
        private float screenPhaseTime;

        public TeamBrain(TeamId team, int playerCount, AIConfig config = null, ITeamStrategy strategy = null, System.Random rng = null)
        {
            this.team = team;
            this.config = config != null ? config : ScriptableObject.CreateInstance<AIConfig>();
            this.rng = rng ?? new System.Random();
            this.strategy = strategy ?? new AutoStrategy(this.config, this.rng);
            orders = new TeamOrder[playerCount];
            man = new int[playerCount];
            snapshotMatchups = new int[playerCount];
            cutUntil = new float[playerCount];
            nextCutAllowed = new float[playerCount];
            for (int i = 0; i < playerCount; i++)
            {
                man[i] = -1;
                snapshotMatchups[i] = int.MinValue;
            }
        }

        public TeamId Team => team;
        public PlayType CurrentPlay => play;
        public TeamOrder GetOrder(int index) => orders[index];
        public int GetMan(int index) => man[index];

        public void Update(MatchSnapshot s)
        {
            if (s.Time < nextPlanTime) return;
            nextPlanTime = s.Time + config.teamReplanInterval;

            SyncMatchups(s);
            TrackPossession(s);

            // A pass in the air: keep running the current plan.
            if (s.BallState == BallState.Passing) return;

            for (int i = 0; i < orders.Length; i++) orders[i] = TeamOrder.None;
            if (s.BallState == BallState.Shooting)
            {
                PlanRebound(s);
                return;
            }
            int holder = s.BallHolderIndex;
            if (holder < 0) return; // loose ball: individual behavior (closest chases)
            if (s.GetTeam(holder) == team) PlanOffense(s, holder, s.Time);
            else PlanDefense(s, holder, s.Time);
        }

        // ---------- bookkeeping ----------

        private void SyncMatchups(MatchSnapshot s)
        {
            bool changed = false;
            for (int i = 0; i < man.Length; i++)
            {
                if (snapshotMatchups[i] != s.GetMatchup(i)) changed = true;
            }
            if (!changed) return;
            // New possession set-up by the rules: take the fresh assignments.
            for (int i = 0; i < man.Length; i++)
            {
                snapshotMatchups[i] = s.GetMatchup(i);
                man[i] = s.GetMatchup(i);
            }
        }

        private void TrackPossession(MatchSnapshot s)
        {
            int holder = s.BallHolderIndex;
            if (holder < 0) return;
            TeamId holderTeam = s.GetTeam(holder);
            bool newPossession = possessionTeam != holderTeam;
            possessionTeam = holderTeam;
            if (newPossession && holderTeam == team)
            {
                play = strategy.ChoosePlay(s, team, holder);
                screener = -1;
                screenPhase = ScreenPhase.None;
                screenSide = rng.NextDouble() < 0.5 ? 1 : -1;
            }
            lastHolder = holder;
        }

        private List<int> Teammates(MatchSnapshot s, int except)
        {
            var list = new List<int>();
            for (int i = 0; i < s.PlayerCount; i++)
            {
                if (i != except && s.GetTeam(i) == team) list.Add(i);
            }
            return list;
        }

        private Vector3 RimFloor(MatchSnapshot s, bool attacking)
        {
            Vector3 rim = attacking ? s.GetAttackingHoop(team) : s.GetDefendedHoop(team);
            return new Vector3(rim.x, 0f, rim.z);
        }

        // ---------- offense ----------

        private void PlanOffense(MatchSnapshot s, int holder, float now)
        {
            Vector3 rimFloor = RimFloor(s, attacking: true);
            Vector3 holderPos = s.GetPosition(holder);
            List<int> offBall = Teammates(s, holder);
            orders[holder] = new TeamOrder(TeamOrderKind.Handle, rimFloor);

            // Pick and roll: closest teammate screens the handler's defender.
            int handlerDefender = s.GetMatchup(holder);
            if (play == PlayType.PickAndRoll && handlerDefender >= 0 && offBall.Count > 0)
            {
                if (screener < 0 || !offBall.Contains(screener)) screener = Closest(s, offBall, holderPos);
                PlanScreen(s, holder, handlerDefender, rimFloor, now);
                if (screenPhase != ScreenPhase.None) offBall.Remove(screener);
            }

            // 4 out, 1 in: with four or more off the ball, the best post player seals inside.
            if (offBall.Count >= 4)
            {
                int post = offBall[0];
                foreach (int i in offBall)
                {
                    if (s.GetAttribute(i, AttributeId.PostScoring) > s.GetAttribute(post, AttributeId.PostScoring)) post = i;
                }
                if (now >= cutUntil[post])
                {
                    offBall.Remove(post);
                    orders[post] = new TeamOrder(TeamOrderKind.Space, OffensePlanner.PostSpot(rimFloor, s.CourtCenter - rimFloor, holderPos, config.postSpotDistance));
                }
            }

            List<Vector3> slots = OffensePlanner.SpacingSlots(rimFloor, s.CourtCenter - rimFloor, config.spacingRadius,
                Mathf.Max(offBall.Count, 1), clearOut: play == PlayType.Isolation);
            var positions = new List<Vector3>();
            foreach (int i in offBall) positions.Add(s.GetPosition(i));
            int[] assignment = OffensePlanner.AssignSlots(positions, slots);

            bool someoneCutting = false;
            for (int k = 0; k < offBall.Count; k++)
            {
                int i = offBall[k];
                if (now < cutUntil[i]) someoneCutting = true;
            }
            for (int k = 0; k < offBall.Count; k++)
            {
                int i = offBall[k];
                Vector3 me = s.GetPosition(i);
                if (now < cutUntil[i])
                {
                    orders[i] = new TeamOrder(TeamOrderKind.Cut, OffensePlanner.CutTarget(me, rimFloor));
                    continue;
                }
                int defender = s.GetMatchup(i);
                bool defenderSagging = defender >= 0 && TeamMath.FlatDistance(me, s.GetPosition(defender)) >= config.cutTriggerDistance;
                if (!someoneCutting && play != PlayType.Isolation && defenderSagging && now >= nextCutAllowed[i])
                {
                    cutUntil[i] = now + config.cutDurationSeconds;
                    nextCutAllowed[i] = now + config.cutCooldownSeconds;
                    someoneCutting = true;
                    orders[i] = new TeamOrder(TeamOrderKind.Cut, OffensePlanner.CutTarget(me, rimFloor));
                    continue;
                }
                orders[i] = new TeamOrder(TeamOrderKind.Space, slots[assignment[k]]);
            }
        }

        private void PlanScreen(MatchSnapshot s, int holder, int handlerDefender, Vector3 rimFloor, float now)
        {
            Vector3 holderPos = s.GetPosition(holder);
            Vector3 screenerPos = s.GetPosition(screener);
            Vector3 spot = OffensePlanner.ScreenSpot(holderPos, s.GetPosition(handlerDefender), rimFloor, screenSide, config.screenOffset);

            if (screenPhase == ScreenPhase.None)
            {
                screenPhase = ScreenPhase.Setting;
                screenPhaseTime = now;
            }
            switch (screenPhase)
            {
                case ScreenPhase.Setting:
                    orders[screener] = new TeamOrder(TeamOrderKind.Screen, spot);
                    if (TeamMath.FlatDistance(screenerPos, spot) < 0.5f)
                    {
                        screenPhase = ScreenPhase.Set;
                        screenPhaseTime = now;
                    }
                    else if (now - screenPhaseTime > config.screenHoldMaxSeconds * 2f)
                    {
                        screenPhase = ScreenPhase.Rolling; // never got there: give up, roll
                        screenPhaseTime = now;
                    }
                    break;
                case ScreenPhase.Set:
                    // Stand still (a screen that moves is a foul); the handler attacks off it.
                    orders[screener] = new TeamOrder(TeamOrderKind.Screen, screenerPos);
                    orders[holder] = new TeamOrder(TeamOrderKind.Handle, OffensePlanner.DriveOffScreen(holderPos, screenerPos, rimFloor), screener);
                    bool handlerPast = TeamMath.FlatDistance(holderPos, rimFloor) < TeamMath.FlatDistance(screenerPos, rimFloor) - 0.3f;
                    if (handlerPast || now - screenPhaseTime > config.screenHoldMaxSeconds)
                    {
                        screenPhase = ScreenPhase.Rolling;
                        screenPhaseTime = now;
                    }
                    break;
                case ScreenPhase.Rolling:
                    if (now - screenPhaseTime < config.rollDurationSeconds)
                    {
                        orders[screener] = new TeamOrder(TeamOrderKind.Roll, OffensePlanner.CutTarget(screenerPos, rimFloor));
                    }
                    else
                    {
                        // Play is over: back to spacing (the screener joins the off-ball group).
                        screenPhase = ScreenPhase.None;
                        play = PlayType.Spacing;
                    }
                    break;
            }
        }

        // ---------- defense ----------

        private void PlanDefense(MatchSnapshot s, int holder, float now)
        {
            Vector3 rimFloor = RimFloor(s, attacking: false);
            List<int> defenders = Teammates(s, -1);

            foreach (int d in defenders)
            {
                if (man[d] < 0) man[d] = s.FindNearestOpponent(d);
            }
            int handlerDefender = -1;
            foreach (int d in defenders) if (man[d] == holder) handlerDefender = d;
            if (handlerDefender < 0 && defenders.Count > 0)
            {
                handlerDefender = Closest(s, defenders, s.GetPosition(holder));
                man[handlerDefender] = holder;
            }

            TrySwitch(s, holder, handlerDefender, defenders, now);

            foreach (int d in defenders)
            {
                int m = man[d];
                orders[d] = new TeamOrder(TeamOrderKind.Guard, m >= 0 ? s.GetPosition(m) : rimFloor, m);
            }

            // Help: the driver beat his man near the rim -> nearest other defender steps in.
            if (handlerDefender >= 0 && DefensePlanner.IsBeaten(s.GetPosition(holder), s.GetPosition(handlerDefender), rimFloor, config.helpRadius, config.beatenMargin))
            {
                Vector3 helpSpot = DefensePlanner.HelpSpot(s.GetPosition(holder), rimFloor, config.helpDepth);
                var others = new List<int>(defenders);
                others.Remove(handlerDefender);
                if (others.Count > 0)
                {
                    int helper = Closest(s, others, helpSpot);
                    orders[helper] = new TeamOrder(TeamOrderKind.Help, helpSpot, holder);
                }
            }
        }

        // An attacker standing on the handler's defender (a screen) -> swap assignments.
        private void TrySwitch(MatchSnapshot s, int holder, int handlerDefender, List<int> defenders, float now)
        {
            if (handlerDefender < 0 || now < nextSwitchAllowed) return;
            Vector3 defPos = s.GetPosition(handlerDefender);
            if (TeamMath.FlatDistance(defPos, s.GetPosition(holder)) < 1.8f) return; // still with his man

            for (int j = 0; j < s.PlayerCount; j++)
            {
                if (j == holder || s.GetTeam(j) == team) continue;
                if (TeamMath.FlatDistance(s.GetPosition(j), defPos) > config.screenSwitchDistance) continue;

                foreach (int d2 in defenders)
                {
                    if (man[d2] != j) continue;
                    man[d2] = holder;
                    man[handlerDefender] = j;
                    nextSwitchAllowed = now + config.switchCooldownSeconds;
                    return;
                }
            }
        }

        // ---------- rebound ----------

        private void PlanRebound(MatchSnapshot s)
        {
            bool weShot = possessionTeam == team;
            Vector3 rimFloor = RimFloor(s, attacking: weShot);
            List<int> mine = Teammates(s, -1);
            if (weShot)
            {
                // One crasher (nearest to the rim, not the shooter); the rest stay spaced.
                int crasher = -1;
                float best = float.MaxValue;
                foreach (int i in mine)
                {
                    if (i == lastHolder) continue;
                    float d = TeamMath.FlatDistance(s.GetPosition(i), rimFloor);
                    if (d < best)
                    {
                        best = d;
                        crasher = i;
                    }
                }
                if (crasher >= 0) orders[crasher] = new TeamOrder(TeamOrderKind.Crash, OffensePlanner.CutTarget(s.GetPosition(crasher), rimFloor));
                return;
            }
            foreach (int d in mine)
            {
                int m = man[d] >= 0 ? man[d] : s.FindNearestOpponent(d);
                if (m < 0) continue;
                orders[d] = new TeamOrder(TeamOrderKind.BoxOut, DefensePlanner.BoxOutSpot(s.GetPosition(m), rimFloor, config.boxOutDistance), m);
            }
        }

        private static int Closest(MatchSnapshot s, List<int> candidates, Vector3 point)
        {
            int best = -1;
            float bestDistance = float.MaxValue;
            foreach (int i in candidates)
            {
                float d = TeamMath.FlatDistance(s.GetPosition(i), point);
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = i;
                }
            }
            return best;
        }
    }
}
