using QualiTool;
using System;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;

namespace JlzQualiTool
{
    [DataContract]
    public class Round
    {
        public static Round Zero = new Round();

        // TODO consider to have Round inherit from ObservableCollection<Matchup> instead of wrapping it.
        public Round(IMatchupStrategy strategy, IRankingRules rules, Round previousRound)
        {
            this.Strategy = strategy;
            this.Rules = rules;
            this.PreviousRound = previousRound;

            // TODO correct place to do that?
            if (strategy != NoStrategy.Get)
            {
                Strategy.CreateMatchups(this);
            }
        }

        public Round() : this(NoStrategy.Get, NoRules.Get, Zero)
        {
            // TODO make private
        }

        [DataMember(Order = 1)]
        public ObservableCollection<Matchup> Matchups { get; } = new ObservableCollection<Matchup>();

        [DataMember(Order = 0)]
        public int Number { get; set; }

        internal Round PreviousRound { get; }
        private IRankingRules Rules { get; }
        private IMatchupStrategy Strategy { get; }

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