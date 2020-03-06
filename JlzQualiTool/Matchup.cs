using log4net;
using System;
using System.ComponentModel;

namespace JlzQualiTool
{
    public class Matchup : INotifyPropertyChanged
    {
        private static ILog Log = log4net.LogManager.GetLogger(typeof(Matchup));

        public Matchup(MatchupInfo info)
        {
            this.Home = new Team(info.HomeTeamName);
            this.Away = new Team(info.AwayTeamName);
            this.Info = info;
            // TODO log creation

            OnMatchPlayedEvent += (o, e) => MatchPlayed();

            Log.Info($" > Created matchup with {Id} @ {Time.ToString(@"hh\:mm")}: {Home.Name} - {Away.Name} on court {Court}.");
        }

        public Team Away { get; set; }
        public int? AwayGoal { get; set; }
        public int AwayId { get; set; }
        public int Court => Info.Court;
        public string GameInfo => $"ID: {Id} \t {Time.ToString(@"hh\:mm")} \t {string.Format(Resources.Court, Court)}";
        public Team Home { get; set; }
        public int? HomeGoal { get; set; }
        public int HomeId { get; set; }
        public int Id => Info.Id;
        public MatchupInfo Info { get; } = new MatchupInfo();
        public bool IsFixed => !Home.IsPlaceHolder && !Away.IsPlaceHolder;

        // TODO derive from home/away goals
        public bool IsPlayed { get; private set; }

        public bool IsTie => IsPlayed && AwayGoal == HomeGoal;

        public Team? Loser => !this.IsPlayed ? null : this.HomeGoal < this.AwayGoal ? this.Home : this.Away;

        public int Round => Id / 100;
        public TimeSpan Time => Info.Time;

        public Team? Winner => !this.IsPlayed ? null : this.HomeGoal >= this.AwayGoal ? this.Home : this.Away;

        public Team GetWinnerOrLoser(char key)
        {
            switch (key)
            {
                case 'W':
                    return Winner ?? new Team($"{key}{Id}");

                case 'L':
                    return Loser ?? new Team($"{key}{Id}");

                default:
                    throw new InvalidOperationException("Must pass 'W' or 'L' as argument.");
            }
        }

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

        public override string ToString()
        {
            return $"{Id} ({Time}, {Court}: {Home} - {Away}\t {HomeGoal} : {AwayGoal}";
        }

        public bool WithTeam(Team team)
        {
            return Away == team || Home == team;
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void MatchPlayed()
        {
            // TODO maybe too late to set oponents after played? (a team may have two open matches)
            // Therefore, Set IsFixed flag explicitly and react on event?
            if (!IsPlayed)
            {
                this.SetOponents();
            }
            this.IsPlayed = true;
        }

        private void SetOponents()
        {
            this.Home.AddOpponent(this.Away);
            this.Away.AddOpponent(this.Home);
        }

        public event EventHandler OnMatchPlayedEvent;

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}