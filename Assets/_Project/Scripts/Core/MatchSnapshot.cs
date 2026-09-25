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
        private readonly Vector3[] attackingHoops = new Vector3[TeamIdExtensions.TeamCount];

        public MatchSnapshot(int playerCount)
        {
            teams = new TeamId[playerCount];
            positions = new Vector3[playerCount];
            velocities = new Vector3[playerCount];
        }

        public int PlayerCount => teams.Length;
        public Vector3 BallPosition { get; private set; }
        public BallState BallState { get; private set; }
        // -1 when nobody holds the ball.
        public int BallHolderIndex { get; private set; } = -1;
        public MatchPhase Phase { get; private set; }
        public float Time { get; private set; }

        public TeamId GetTeam(int index) => teams[index];
        public Vector3 GetPosition(int index) => positions[index];
        public Vector3 GetVelocity(int index) => velocities[index];

        // Half court: both teams attack the same hoop. Full court (5v5) sets them apart.
        public Vector3 GetAttackingHoop(TeamId team) => attackingHoops[(int)team];
        public Vector3 GetDefendedHoop(TeamId team) => attackingHoops[(int)team.Opponent()];

        public TeamId? TeamInPossession => BallHolderIndex >= 0 ? teams[BallHolderIndex] : (TeamId?)null;

        public void SetPlayer(int index, TeamId team, Vector3 position, Vector3 velocity)
        {
            teams[index] = team;
            positions[index] = position;
            velocities[index] = velocity;
        }

        public void SetBall(Vector3 position, BallState state, int holderIndex)
        {
            BallPosition = position;
            BallState = state;
            BallHolderIndex = holderIndex;
        }

        public void SetAttackingHoop(TeamId team, Vector3 hoopPosition) => attackingHoops[(int)team] = hoopPosition;

        public void SetMatch(MatchPhase phase, float time)
        {
            Phase = phase;
            Time = time;
        }

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
