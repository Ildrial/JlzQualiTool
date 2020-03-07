using System.Diagnostics.Contracts;

namespace JlzQualiTool
// TODO rename class
{
    using log4net;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;

    public class Team : INotifyPropertyChanged
    {
        private static ILog Log = log4net.LogManager.GetLogger(typeof(Team));

        public Team(string name, bool isPlaceHolder = true)
        {
            Name = name;
            IsPlaceHolder = isPlaceHolder;
        }

        public Team()
        {
        }

        public int Difference => this.GoalsScored - this.GoalsReceived;
        public int GoalsReceived { get; set; }
        public int GoalsScored { get; set; }
        public bool IsPlaceHolder { get; } = false;

        // TODO derive from Opponents!
        public int Matches { get; set; }

        public string Name { get; set; } = "";
        public int Points { get; set; }
        public int PreSeasonPonits { get; set; }
        public int Seed { get; set; }
        public int SelfAssessmentPoints { get; set; }
        public int TotalPoints => this.PreSeasonPonits + this.SelfAssessmentPoints;
        private List<Team> Opponents { get; } = new List<Team>(5);

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

        public void AddOpponent(Team team)
        {
            if (Opponents.Contains(team))
            {
                Log.Fatal($"!!! Duplicate Matchup recorded: {this} - {team} !!!");
                throw new InvalidOperationException($"{this} already played against {team}!");
            }
            else
            {
                Opponents.Add(team);
            }
        }

        public bool HasPlayed(Team team)
        {
            return Opponents.Contains(team);
        }

        public void Publish()
        {
            this.OnPropertyChanged("Points");
            this.OnPropertyChanged("Matches");
            this.OnPropertyChanged("GoalsScored");
            this.OnPropertyChanged("GoalsReceived");
            this.OnPropertyChanged("Difference");
        }

        public override string ToString()
        {
            return this.Name;
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