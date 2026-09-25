using UnityEngine;

namespace Basket.Core
{
    // One AI player's local view of the match, derived from the shared MatchSnapshot.
    // "Opponent" is the opponent this player is focused on: the ball handler when the
    // other team has the ball, otherwise the nearest opponent (or self when there is none).
    public readonly struct AIPerception
    {
        public readonly Vector3 SelfPosition;
        public readonly Vector3 OpponentPosition;
        public readonly Vector3 BallPosition;
        public readonly bool OpponentHasBall;
        public readonly bool SelfHasBall;
        public readonly bool TeammateHasBall;
        public readonly Vector3 AttackHoop;
        public readonly Vector3 DefendHoop;
        public readonly float Time;
        public readonly Vector3 SelfVelocity;
        public readonly bool SelfGrounded;
        public readonly Vector3 OpponentVelocity;
        public readonly bool OpponentGrounded;
        public readonly Vector3 BallVelocity;
        public readonly BallState BallState;

        public AIPerception(Vector3 selfPosition, Vector3 opponentPosition, Vector3 ballPosition, bool opponentHasBall, bool selfHasBall,
            bool teammateHasBall = false, Vector3 attackHoop = default, Vector3 defendHoop = default, float time = 0f,
            Vector3 selfVelocity = default, bool selfGrounded = true, Vector3 opponentVelocity = default, bool opponentGrounded = true,
            Vector3 ballVelocity = default, BallState ballState = BallState.Free)
        {
            SelfPosition = selfPosition;
            OpponentPosition = opponentPosition;
            BallPosition = ballPosition;
            OpponentHasBall = opponentHasBall;
            SelfHasBall = selfHasBall;
            TeammateHasBall = teammateHasBall;
            AttackHoop = attackHoop;
            DefendHoop = defendHoop;
            Time = time;
            SelfVelocity = selfVelocity;
            SelfGrounded = selfGrounded;
            OpponentVelocity = opponentVelocity;
            OpponentGrounded = opponentGrounded;
            BallVelocity = ballVelocity;
            BallState = ballState;
        }

        public static AIPerception FromSnapshot(MatchSnapshot s, int selfIndex)
        {
            TeamId selfTeam = s.GetTeam(selfIndex);
            Vector3 selfPos = s.GetPosition(selfIndex);
            int holder = s.BallHolderIndex;

            bool selfHas = holder == selfIndex;
            bool opponentHas = holder >= 0 && s.GetTeam(holder) != selfTeam;
            bool teammateHas = holder >= 0 && !selfHas && !opponentHas;

            int focus = opponentHas ? holder : s.FindNearestOpponent(selfIndex);
            Vector3 focusPos = focus >= 0 ? s.GetPosition(focus) : selfPos;
            Vector3 focusVel = focus >= 0 ? s.GetVelocity(focus) : Vector3.zero;
            bool focusGrounded = focus < 0 || s.IsGrounded(focus);

            return new AIPerception(selfPos, focusPos, s.BallPosition, opponentHas, selfHas, teammateHas,
                s.GetAttackingHoop(selfTeam), s.GetDefendedHoop(selfTeam), s.Time,
                s.GetVelocity(selfIndex), s.IsGrounded(selfIndex), focusVel, focusGrounded, s.BallVelocity, s.BallState);
        }
    }
}
