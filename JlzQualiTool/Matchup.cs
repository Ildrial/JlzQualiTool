using log4net;
using System;
using System.ComponentModel;

namespace JlzQualiTool
{
    public class Matchup : INotifyPropertyChanged
    {
        private static ILog Log = log4net.LogManager.GetLogger(typeof(Matchup));

        public Team Away { get; set; } = Team.Tbd;

        public int? AwayGoal { get; set; }

        public int AwayId { get; set; }

        public Team Home { get; set; } = Team.Tbd;

        public int? HomeGoal { get; set; }

        public int HomeId { get; set; }

        public int Id { get; set; }

        public bool IsFixed => Home != null && Away != null;

        // TODO derive from home/away goals
        public bool IsPlayed { get; private set; }

        public bool IsTie => IsPlayed && AwayGoal == HomeGoal;

        public Team? Loser => !this.IsPlayed ? null : this.HomeGoal < this.AwayGoal ? this.Home : this.Away;

        public int Round => Id / 100;

        public DateTime Time { get; set; }

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