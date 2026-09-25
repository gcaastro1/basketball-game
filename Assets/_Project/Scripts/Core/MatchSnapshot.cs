using UnityEngine;

namespace Basket.Core
{
    // Read-only (for consumers) view of the match that every controller decides from.
    // Refreshed in place once per simulation tick by the owner, so it allocates nothing
    // per frame. Replaces the old 1v1-only AIPerception inputs: any number of players on
    // two teams.
    public sealed class MatchSnapshot
    {
        private readonly TeamId[] teams;
        private readonly Vector3[] positions;
        private readonly Vector3[] velocities;
        private readonly bool[] grounded;
        private readonly int[] matchups;
        private readonly Vector3[] attackingHoops = new Vector3[TeamIdExtensions.TeamCount];

        public MatchSnapshot(int playerCount)
        {
            teams = new TeamId[playerCount];
            positions = new Vector3[playerCount];
            velocities = new Vector3[playerCount];
            grounded = new bool[playerCount];
            matchups = new int[playerCount];
            for (int i = 0; i < playerCount; i++) matchups[i] = -1;
        }

        public int PlayerCount => teams.Length;
        public Vector3 BallPosition { get; private set; }
        public Vector3 BallVelocity { get; private set; }
        public BallState BallState { get; private set; }
        // -1 when nobody holds the ball.
        public int BallHolderIndex { get; private set; } = -1;
        public MatchPhase Phase { get; private set; }
        public float Time { get; private set; }
        // Negative when the rules have no shot clock.
        public float ShotClock { get; private set; } = -1f;
        // True while the team in possession still has to take the ball beyond the arc.
        public bool BallMustBeCleared { get; private set; }
        public float ThreePointRadius { get; private set; } = 6.75f;

        public TeamId GetTeam(int index) => teams[index];
        public Vector3 GetPosition(int index) => positions[index];
        // Includes vertical velocity while jumping.
        public Vector3 GetVelocity(int index) => velocities[index];
        public bool IsGrounded(int index) => grounded[index];
        // Opponent this player is paired with (man-to-man), or -1.
        public int GetMatchup(int index) => matchups[index];
        public void SetMatchup(int index, int opponentIndex) => matchups[index] = opponentIndex;

        // Half court: both teams attack the same hoop. Full court (5v5) sets them apart.
        public Vector3 GetAttackingHoop(TeamId team) => attackingHoops[(int)team];
        public Vector3 GetDefendedHoop(TeamId team) => attackingHoops[(int)team.Opponent()];

        public TeamId? TeamInPossession => BallHolderIndex >= 0 ? teams[BallHolderIndex] : (TeamId?)null;

        public void SetPlayer(int index, TeamId team, Vector3 position, Vector3 velocity, bool isGrounded = true)
        {
            teams[index] = team;
            positions[index] = position;
            velocities[index] = velocity;
            grounded[index] = isGrounded;
        }

        public void SetBall(Vector3 position, BallState state, int holderIndex, Vector3 velocity = default)
        {
            BallPosition = position;
            BallVelocity = velocity;
            BallState = state;
            BallHolderIndex = holderIndex;
        }

        public void SetAttackingHoop(TeamId team, Vector3 hoopPosition) => attackingHoops[(int)team] = hoopPosition;

        public void SetMatch(MatchPhase phase, float time, float shotClock = -1f, bool ballMustBeCleared = false)
        {
            Phase = phase;
            Time = time;
            ShotClock = shotClock;
            BallMustBeCleared = ballMustBeCleared;
        }

        public void SetThreePointRadius(float radius) => ThreePointRadius = radius;

        // True if no teammate of `index` is closer (on the floor plane) to the ball.
        public bool IsClosestOfTeamToBall(int index)
        {
            TeamId team = teams[index];
            float mine = FlatSqr(positions[index] - BallPosition);
            for (int i = 0; i < teams.Length; i++)
            {
                if (i == index || teams[i] != team) continue;
                if (FlatSqr(positions[i] - BallPosition) < mine) return false;
            }
            return true;
        }

        private static float FlatSqr(Vector3 v) => v.x * v.x + v.z * v.z;

        // Nearest player of the other team on the court plane; -1 if there is none.
        public int FindNearestOpponent(int selfIndex)
        {
            TeamId selfTeam = teams[selfIndex];
            Vector3 selfPos = positions[selfIndex];
            int best = -1;
            float bestSqr = float.MaxValue;
            for (int i = 0; i < teams.Length; i++)
            {
                if (teams[i] == selfTeam) continue;
                Vector3 d = positions[i] - selfPos;
                d.y = 0f;
                float sqr = d.sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = i;
                }
            }
            return best;
        }
    }
}
