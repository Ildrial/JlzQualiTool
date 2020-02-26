using System;
using System.Collections.Generic;
using System.Text;

namespace JlzQualiTool
{
    public class RankingEntry
    {
        public RankingEntry(Team team, int gamesPlayed, int points, int goalsScored, int goalsReceived)
        {
            Team = team;
            GamesPlayed = gamesPlayed;
            Points = points;
            GoalsScored = goalsScored;
            GoalsReceived = goalsReceived;
        }

        public int Difference => GoalsScored - GoalsReceived;
        public int GamesPlayed { get; }
        public int GoalsReceived { get; }
        public int GoalsScored { get; }
        public int Points { get; }
        public Team Team { get; }
    }
}