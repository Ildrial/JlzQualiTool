using QualiTool;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace JlzQualiTool
{
    [DataContract]
    public class Round
    {
        // TODO consider to have Round inherit from ObservableCollection<Matchup> instead of wrapping it.
        public Round()
        {
            this.Matchups = new ObservableCollection<Matchup>();
        }

        [DataMember(Order = 1)]
        public ObservableCollection<Matchup> Matchups { get; }

        [DataMember(Order = 0)]
        public int Number { get; set; }

        public void CreateAndAddMatchup(Team? home, Team? away)
        {
            var numberOfMatches = this.Matchups.Count;

            var @base = Number * 100;
            var gameNo = @base + numberOfMatches + 1;

            var matchup = new Matchup()
            {
                Away = away,
                Home = home,
                Time = DateTime.Now,
                Id = gameNo
            };
            home?.Matchups.Add(matchup);
            away?.Matchups.Add(matchup);

            this.Matchups.Add(matchup);
        }
    }
}