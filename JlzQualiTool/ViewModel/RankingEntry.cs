﻿using System;
using System.ComponentModel;

namespace JlzQualiTool
{
    public class RankingEntry : INotifyPropertyChanged
    {
        private static Random Randomizer = new Random(DateTime.Now.Millisecond);

        public int Difference => GoalsScored - GoalsReceived;
        public int GamesPlayed { get; }
        public string Goals => $"{this.GoalsScored} : {this.GoalsReceived}\t {this.Difference}";
        public int GoalsReceived { get; }
        public int GoalsScored { get; }
        public int Points { get; }

        public string Position
        {
            get
            {
                // TODO use GroupBy and Join for ordering instead.
                var modifiedDifference = IsInversed
                    ? (1000 - Difference)
                    : 1000 + Difference;
                var goalsScored = IsInversed
                    ? (1000 - GoalsScored)
                    : GoalsScored;

                return $"{Points.ToString("00")}.{modifiedDifference.ToString("0000")}.{goalsScored.ToString("0000")}.{Chance.ToString("000000")}";
            }
        }

        public Team Team { get; }

        private int Chance { get; }
        private bool IsInversed { get; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public RankingEntry(Team team, int gamesPlayed, int points, int goalsScored, int goalsReceived, bool isInversed)
        {
            Team = team;
            GamesPlayed = gamesPlayed;
            Points = points;
            GoalsScored = goalsScored;
            GoalsReceived = goalsReceived;
            IsInversed = isInversed;
            // TODO improve random number: currently it is changed every update and may change
            // predictions! not persistent
            Chance = Randomizer.Next(1000000);
        }

        public override string ToString()
        {
            return $"{Team} [{GamesPlayed}, {Points}, {GoalsScored}:{GoalsReceived},{Difference}]";
        }

        // Create the OnPropertyChanged method to raise the event
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}