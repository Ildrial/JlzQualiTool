using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace JlzQualiTool
{
    public interface IMatchupStrategy
    {
        void CreateMatchups();
    }

    public class InitialOrderStrategy : MatchupStrategyBase
    {
        public InitialOrderStrategy(Round round, List<Team> teams) : base(round) => Teams = teams;

        private List<Team> Teams { get; }

        protected override void CreateMatchupsInternal()
        {
            foreach (var matchupInfo in Round.Info.MatchupInfos)
            {
                // TODO better resolution of Teams and matchups
                var matchup = new Matchup(matchupInfo)
                {
                    Home = Teams[int.Parse(matchupInfo.Home) - 1],
                    Away = Teams[int.Parse(matchupInfo.Away) - 1]
                };
                Round.Matchups.Add(matchup);
            }
        }
    }

    public class KoStrategy : MatchupStrategyBase
    {
        public KoStrategy(Round round) : base(round)
        {
        }

        protected override void CreateMatchupsInternal()
        {
            var previousMatchups = Round.PreviousRound.Matchups;

            foreach (var matchupInfo in Round.Info.MatchupInfos)
            {
                var matchup = new Matchup(matchupInfo);
                Round.Matchups.Add(matchup);

                // TODO improve handling by maybe use specialized matchup info classes
                var homeGameIdRef = int.Parse(matchupInfo.Home.Substring(1));
                var awayGameIdRef = int.Parse(matchupInfo.Away.Substring(1));

                var homeRefMatchup = previousMatchups.Single(m => m.Id == homeGameIdRef);
                var awayRefMatchup = previousMatchups.Single(m => m.Id == awayGameIdRef);
                // TODO put in methods and use sender and event args
                homeRefMatchup.OnMatchPlayedEvent += (o, e) =>
                 {
                     matchup.Home = homeRefMatchup.GetWinnerOrLoser(matchupInfo.Home.First()) ?? matchup.Home;

                     Log.Debug($"Match {homeRefMatchup.Id} played. Updating Home team for game {matchup.Id} ({matchup.Home.Name}).");

                     matchup.Publish();
                 };
                awayRefMatchup.OnMatchPlayedEvent += (o, e) =>
                {
                    matchup.Away = awayRefMatchup.GetWinnerOrLoser(matchupInfo.Away.First()) ?? matchup.Away;

                    Log.Debug($"Match {awayRefMatchup.Id} played. Updating Away team for game {matchup.Id} ({matchup.Away.Name}).");

                    matchup.Publish();
                };
            }
        }
    }

    public abstract class MatchupStrategyBase : IMatchupStrategy
    {
        protected static ILog Log = log4net.LogManager.GetLogger(typeof(MatchupStrategyBase));

        public MatchupStrategyBase(Round round)
        {
            this.Round = round;
        }

        protected Round Round { get; }

        public void CreateMatchups()
        {
            Log.Info($"Creating matchups for round '{Round.Number}':");
            CreateMatchupsInternal();
        }

        protected abstract void CreateMatchupsInternal();
    }

    public class NoStrategy : MatchupStrategyBase
    {
        public static IMatchupStrategy Get = new NoStrategy(Round.Zero);

        private NoStrategy(Round round) : base(round)
        {
        }

        protected override void CreateMatchupsInternal()
        {
            throw new System.NotImplementedException();
        }
    }

    public class RankingStrategy : MatchupStrategyBase
    {
        public RankingStrategy(Round round) : base(round)
        {
        }

        protected override void CreateMatchupsInternal()
        {
            foreach (var matchupInfo in Round.Info.MatchupInfos)
            {
                var matchup = new Matchup(matchupInfo);
                Round.Matchups.Add(matchup);

                // TODO put in methods and use sender and event args
                Round.PreviousRound.OnRankingUpdatedEvent += (o, e) => UpdateMatchup(Round.PreviousRound, matchup);
            }
        }

        private void UpdateMatchup(Round previousRound, Matchup matchup)
        {
            var homeRank = string.IsNullOrEmpty(matchup.Info.Home) ? 0 : int.Parse(matchup.Info.Home);
            var awayRank = string.IsNullOrEmpty(matchup.Info.Away) ? 0 : int.Parse(matchup.Info.Away);

            if (homeRank != 0 && awayRank != 0)
            {
                // TODO parts of table must be fixed before... (idea: fixed flag on ranking entry?)
                if (previousRound.Matchups.All(m => m.IsPlayed))
                {
                    matchup.Home = previousRound.Ranking[homeRank - 1].Team;
                    matchup.Away = previousRound.Ranking[awayRank - 1].Team;
                    // TODO adjust log text
                    Log.Debug($"Complete ranking of round {previousRound.Number} updated. Updating game {matchup.Id}: {matchup.Home.Name} - {matchup.Away.Name}.");
                }
            }
            else
            {
                // TODO implement round 5 logic without fixed places, i.e. rank = 0;
            }

            matchup.Publish();
        }
    }
}