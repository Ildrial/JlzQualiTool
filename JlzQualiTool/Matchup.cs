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

        // TODO derive from home/away goals
        [DataMember(Order = 5)]
        public bool IsPlayed { get; set; }

        public bool IsTie => IsPlayed && AwayGoal == HomeGoal;

        [IgnoreDataMember]
        public Team? Loser => !this.IsPlayed ? null : this.HomeGoal < this.AwayGoal ? this.Home : this.Away;

        public int Round => Id / 100;

        [IgnoreDataMember]
        public DateTime Time { get; set; }

        [IgnoreDataMember]
        public Team? Winner => !this.IsPlayed ? null : this.HomeGoal >= this.AwayGoal ? this.Home : this.Away;

        public int GoalsReceived(Team team)
        {
            return !WithTeam(team)
                ? 0
                : Away == team
                    ? HomeGoal != null ? HomeGoal.Value : 0
                    : AwayGoal != null ? AwayGoal.Value : 0;
        }

        public int GoalsScored(Team team)
        {
            return !WithTeam(team)
                ? 0
                : Away == team
                    ? AwayGoal != null ? AwayGoal.Value : 0
                    : HomeGoal != null ? HomeGoal.Value : 0;
        }

        public int Points(Team team)
        {
            return !WithTeam(team)
                ? 0
                : IsTie
                    ? 1
                    : Winner == team ? 2 : 0;
        }

        public void Publish()
        {
            // TODO distinguish between publishing teams and goals
            this.OnPropertyChanged("Home");
            this.OnPropertyChanged("Away");
            this.OnPropertyChanged("HomeGoal");
            this.OnPropertyChanged("AwayGoal");
        }

        public void RaiseOnMatchPlayedEvent()
        {
            OnMatchPlayedEvent?.Invoke(this, EventArgs.Empty);
        }

        public bool WithTeam(Team team)
        {
            return Away == team || Home == team;
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public event EventHandler OnMatchPlayedEvent = new EventHandler((o, e) =>
        {
            if (o != null)
            {
                ((Matchup)o).IsPlayed = true;
            }
        });

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}