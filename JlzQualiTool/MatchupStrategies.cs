using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace JlzQualiTool
{
    public interface IMatchupStrategy
    {
        // TODO put round into constructor?
        void CreateMatchups(Round round);
    }

    public class InitialOrderStrategy : MatchupStrategyBase
    {
        public InitialOrderStrategy(RoundInfo roundInfo, List<Team> teams) : base(roundInfo) => Teams = teams;

        private List<Team> Teams { get; }

        protected override void CreateMatchupsInternal(Round round)
        {
            foreach (var matchupInfo in RoundInfo.MatchupInfos)
            {
                // TODO better resolution of Teams and matchups
                var matchup = new Matchup(matchupInfo)
                {
                    Home = Teams[int.Parse(matchupInfo.Home) - 1],
                    Away = Teams[int.Parse(matchupInfo.Away) - 1]
                };
                round.Matchups.Add(matchup);
            }
        }
    }

    public class KoStrategy : MatchupStrategyBase
    {
        public KoStrategy(RoundInfo roundInfo) : base(roundInfo)
        {
        }

        protected override void CreateMatchupsInternal(Round round)
        {
            var previousMatchups = round.PreviousRound.Matchups;

            foreach (var matchupInfo in RoundInfo.MatchupInfos)
            {
                var matchup = new Matchup(matchupInfo);
                round.Matchups.Add(matchup);

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
                    matchup.Away = awayRefMatchup.GetWinnerOrLoser(matchupInfo.Home.First()) ?? matchup.Away;

                    Log.Debug($"Match {awayRefMatchup.Id} played. Updating Away team for game {matchup.Id} ({matchup.Away.Name}).");

                    matchup.Publish();
                };
            }
        }
    }

    public abstract class MatchupStrategyBase : IMatchupStrategy
    {
        protected static ILog Log = log4net.LogManager.GetLogger(typeof(MatchupStrategyBase));

        public MatchupStrategyBase(RoundInfo roundInfo)
        {
            this.RoundInfo = roundInfo;
        }

        protected RoundInfo RoundInfo { get; }

        public void CreateMatchups(Round round)
        {
            Log.Info($"Creating matchups for round '{round.Number}':");
            CreateMatchupsInternal(round);
        }

        protected abstract void CreateMatchupsInternal(Round round);
    }

    public class NoStrategy : MatchupStrategyBase
    {
        public static IMatchupStrategy Get = new NoStrategy(new RoundInfo());

        private NoStrategy(RoundInfo info) : base(info)
        {
        }

        protected override void CreateMatchupsInternal(Round round)
        {
            throw new System.NotImplementedException();
        }
    }

    public class RankingStrategy : MatchupStrategyBase
    {
        public RankingStrategy(RoundInfo roundInfo) : base(roundInfo)
        {
        }

        protected override void CreateMatchupsInternal(Round round)
        {
            foreach (var matchupInfo in RoundInfo.MatchupInfos)
            {
                var homeRank = string.IsNullOrEmpty(matchupInfo.Home) ? 0 : int.Parse(matchupInfo.Home);
                var awayRank = string.IsNullOrEmpty(matchupInfo.Away) ? 0 : int.Parse(matchupInfo.Away);

                var matchup = new Matchup(matchupInfo);
                round.Matchups.Add(matchup);

                // TODO put in methods and use sender and event args
                round.PreviousRound.OnRankingUpdatedEvent += (o, e) =>
                {
                    // TODO handle rematches correctly
                    if (o != null)
                    {
                        var round = (Round)o;
                        if (round.Number != 4)
                        {
                            // TODO parts of table must be fixed before... (idea: fixed flag on ranking entry?)
                            if (round.Matchups.All(m => m.IsPlayed))
                            {
                                matchup.Home = round.Ranking[homeRank - 1].Team;
                                matchup.Away = round.Ranking[awayRank - 1].Team;
                                // TODO adjust log text
                                Log.Debug($"Complete ranking of round {round.Number} updated. Updating game {matchup.Id}: {matchup.Home.Name} - {matchup.Away.Name}.");
                            }
                        }
                        else
                        {
                            // TODO implement round 5 logic without fixed places.
                        }

                        matchup.Publish();
                    }
                };
            }
        }
    }
}