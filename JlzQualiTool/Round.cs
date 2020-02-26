using log4net;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;

namespace JlzQualiTool
{
    [DataContract]
    public class Round
    {
        public static Round Zero = new Round();

        private static ILog Log = log4net.LogManager.GetLogger(typeof(Round));

        // TODO consider to have Round inherit from ObservableCollection<Matchup> instead of wrapping it.
        public Round(int number, IMatchupStrategy strategy, Round previousRound)
        {
            this.Number = number;
            this.Strategy = strategy;
            this.PreviousRound = previousRound;

            // TODO correct place to do that?
            if (strategy != NoStrategy.Get)
            {
                Strategy.CreateMatchups(this);
            }
        }

        private Round() : this(0, NoStrategy.Get, Zero)
        {
        }

        [DataMember(Order = 1)]
        public ObservableCollection<Matchup> Matchups { get; } = new ObservableCollection<Matchup>();

        [DataMember(Order = 0)]
        public int Number { get; private set; }

        internal Round PreviousRound { get; }
        private IMatchupStrategy Strategy { get; }

        public Matchup CreateAndAddMatchup(Team home, Team away)
        {
            var numberOfMatches = this.Matchups.Count;

            var @base = Number * 100;
            var gameNo = @base + numberOfMatches + 1;
            var time = DateTime.Now;

            Log.Info($" > {gameNo} @ {time.ToString("HH:mm")}: {home.Name} - {away.Name}");

            var matchup = new Matchup()
            {
                Away = away,
                Home = home,
                Time = time,
                Id = gameNo
            };

            this.Matchups.Add(matchup);

            return matchup;
        }
    }
}