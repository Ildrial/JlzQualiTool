using log4net;
using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace JlzQualiTool
{
    public class Matchup : INotifyPropertyChanged
    {
        private static ILog Log = log4net.LogManager.GetLogger(typeof(Matchup));
        private Team away = Team.Tbd;
        private Team home = Team.Tbd;

        [IgnoreDataMember]
        public Team Away
        {
            get
            {
                return this.away;
            }
            set
            {
                AwayId = value == null ? -1 : value.Id;
                this.away = value ?? Team.Tbd;
            }
        }

        [DataMember(Order = 4)]
        public int? AwayGoal { get; set; }

        [DataMember(Order = 3)]
        public int AwayId { get; set; }

        [IgnoreDataMember]
        public Team Home
        {
            get
            {
                return this.home;
            }
            set
            {
                HomeId = value == null ? -1 : value.Id;
                this.home = value ?? Team.Tbd;
            }
        }

        [DataMember(Order = 2)]
        public int? HomeGoal { get; set; }

        [DataMember(Order = 1)]
        public int HomeId { get; set; }

        [DataMember(Order = 0)]
        public int Id { get; set; }

        [DataMember(Order = 5)]
        public bool IsPlayed { get; set; }

        [IgnoreDataMember]
        public Team? Loser => !IsPlayed ? Team.Tbd : this.HomeGoal < this.AwayGoal ? this.Home : this.Away;

        [IgnoreDataMember]
        public DateTime Time { get; set; }

        [IgnoreDataMember]
        public Team? Winner => !IsPlayed ? Team.Tbd : this.HomeGoal >= this.AwayGoal ? this.Home : this.Away;

        public void Publish()
        {
            this.OnPropertyChanged("HomeGoal");
            this.OnPropertyChanged("AwayGoal");
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}