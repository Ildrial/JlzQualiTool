using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace JlzQualiTool
{
    public class RankingEntry : INotifyPropertyChanged
    {
        private static Random Randomizer = new Random(DateTime.Now.Millisecond);

        public RankingEntry(Team team, int gamesPlayed, int points, int goalsScored, int goalsReceived)
        {
            Team = team;
            GamesPlayed = gamesPlayed;
            Points = points;
            GoalsScored = goalsScored;
            GoalsReceived = goalsReceived;
            Chance = Randomizer.Next(1000000);
        }

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
                // TODO must not be hard coded. make configurable
                var modifiedDifference = (Points == 4 || Points == 0)
                    ? 1000 + Difference
                    : (1000 - Difference);
                // TODO for 0 points correct? according to qualiturnier rotkreuz not! verify with DH
                var goalsScored = (Points == 4 || Points == 0)
                    ? GoalsScored
                    : (1000 - GoalsScored);

                //int.TryParse($"{Points}{modifiedDifference.ToString("0000")}{goalsScored.ToString("0000")}", out int result);
                //return result;
                return $"{Points.ToString("00")}.{modifiedDifference.ToString("0000")}.{goalsScored.ToString("0000")}.{Chance.ToString("000000")}";
            }
        }

        public Team Team { get; }
        private int Chance { get; }

        public void Publish()
        {
            this.OnPropertyChanged("Team");
            this.OnPropertyChanged("Points");
            this.OnPropertyChanged("GoalsScored");
            this.OnPropertyChanged("GoalsReceived");
            this.OnPropertyChanged("Difference");
        }

        // Create the OnPropertyChanged method to raise the event
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}