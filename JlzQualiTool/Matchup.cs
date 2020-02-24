using log4net;
using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace JlzQualiTool
{
    public class Matchup : INotifyPropertyChanged
    {
        private static ILog Log = log4net.LogManager.GetLogger(typeof(Matchup));
        private Team? away;
        private Team? home;

        // FIXME why are Ids not serialized?
        [IgnoreDataMember]
        public Team? Away
        {
            get
            {
                return this.away;
            }
            set
            {
                AwayId = value == null ? -1 : value.Id;
                this.away = value;
            }
        }

        [DataMember(Order = 4)]
        public int AwayGoal { get; set; }

        [DataMember(Order = 3)]
        public int AwayId { get; set; }

        [IgnoreDataMember]
        public Team? Home
        {
            get
            {
                return this.home;
            }
            set
            {
                HomeId = value == null ? -1 : value.Id;
                this.home = value;
            }
        }

        [DataMember(Order = 2)]
        public int HomeGoal { get; set; }

        [DataMember(Order = 1)]
        public int HomeId { get; set; }

        [DataMember(Order = 0)]
        public int Id { get; set; }

        public bool IsPlayed { get; set; }

        [IgnoreDataMember]
        public Team? Loser => this.HomeGoal < this.AwayGoal ? this.Home : this.Away;

        [DataMember]
        public DateTime Time { get; set; }

        [IgnoreDataMember]
        public Team? Winner => this.HomeGoal >= this.AwayGoal ? this.Home : this.Away;

        public void Publish()
        {
            this.OnPropertyChanged("HomeGoal");
            this.OnPropertyChanged("AwayGoal");
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}