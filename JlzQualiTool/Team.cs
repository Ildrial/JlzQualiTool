﻿using System.Diagnostics.Contracts;

namespace JlzQualiTool
// TODO rename class
{
    using System.ComponentModel;

    public class Team : INotifyPropertyChanged
    {
        public static Team Tbd = new Team()
        {
            Name = "TBD"
        };

        public Team(string dummyName)
        {
            Name = dummyName;
        }

        public Team()
        {
        }

        public int Difference => this.GoalsScored - this.GoalsReceived;

        public int GoalsReceived { get; set; }

        public int GoalsScored { get; set; }

        public int Matches { get; set; }

        public string? Name { get; set; }

        public int Points { get; set; }

        public int PreSeasonPonits { get; set; }

        public int Seed { get; set; }

        public int SelfAssessmentPoints { get; set; }

        public int TotalPoints => this.PreSeasonPonits + this.SelfAssessmentPoints;

        public static Team FromLine(string line)
        {
            var cells = line.Split(',');
            Contract.Assert(cells.Length >= 3);
            return new Team
            {
                Name = cells[0].Trim(),
                PreSeasonPonits = ParseInt(cells[1]),
                SelfAssessmentPoints = ParseInt(cells[2])
            };
        }

        public void Publish()
        {
            this.OnPropertyChanged("Points");
            this.OnPropertyChanged("Matches");
            this.OnPropertyChanged("GoalsScored");
            this.OnPropertyChanged("GoalsReceived");
            this.OnPropertyChanged("Difference");
        }

        internal void ClearRankingInfo()
        {
            Matches = 0;
            Points = 0;
            GoalsScored = 0;
            GoalsReceived = 0;
        }

        // Create the OnPropertyChanged method to raise the event
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private static int ParseInt(string number)
        {
            return string.IsNullOrEmpty(number) ? 0 : int.Parse(number);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}