using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace QualiTool
{
    using System.ComponentModel;

    public class Team : INotifyPropertyChanged
    {
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

        private static int ParseInt(string number)
        {
            return string.IsNullOrEmpty(number) ? 0 : int.Parse(number);
        }

        public Team()
        {
        }

        public int Id { get; set; }
        public string? Name { get; set; }
        public int PreSeasonPonits { get; set; }
        public int SelfAssessmentPoints { get; set; }
        public int Points { get; set; }
        public int Matches { get; set; }
        public int GoalsScored { get; set; }
        public int GoalsReceived { get; set; }
        public int Difference => this.GoalsScored - this.GoalsReceived;
        public int Seed { get; set; }
        public IList<Matchup> Matchups { get; } = new List<Matchup>(5);
        public int TotalPoints => this.PreSeasonPonits + this.SelfAssessmentPoints;

        // Create the OnPropertyChanged method to raise the event
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public void Publish()
        {
            this.OnPropertyChanged("Points");
            this.OnPropertyChanged("Matches");
            this.OnPropertyChanged("GoalsScored");
            this.OnPropertyChanged("GoalsReceived");
            this.OnPropertyChanged("Difference");
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}