using log4net;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
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
            }
            Round.PreviousRound.OnRankingUpdatedEvent += (o, e) => UpdateMatchups();
        }

        private bool IsMatchupValid(Matchup matchup)
        {
            // Must not play agains each other twice
            return true;
        }

        private bool SelectNextMatchup(List<Matchup> remainingMatchups, out Matchup matchup)
        {
            matchup = remainingMatchups.First();
            // TODO select first available instead of first always (i.e. backtracking)

            return IsMatchupValid(matchup);
        }

        private void UpdateMatchup(Matchup matchup)
        {
            var homeRank = string.IsNullOrEmpty(matchup.Info.Home) ? 0 : int.Parse(matchup.Info.Home);
            var awayRank = string.IsNullOrEmpty(matchup.Info.Away) ? 0 : int.Parse(matchup.Info.Away);

            if (homeRank != 0 && awayRank != 0)
            {
                // TODO parts of table must be fixed before... (idea: fixed flag on ranking entry?)
                if (Round.PreviousRound.Matchups.All(m => m.IsPlayed))
                {
                    matchup.Home = Round.PreviousRound.Ranking[homeRank - 1].Team;
                    matchup.Away = Round.PreviousRound.Ranking[awayRank - 1].Team;
                }
            }
            else
            {
                // TODO implement round 5 logic without fixed places, i.e. rank = 0;
            }

            matchup.Publish();
        }

        private void UpdateMatchups()
        {
            // TODO adjust log text once not complete ranking is required
            Log.Info($"Complete ranking of round {Round.PreviousRound.Number} updated. Updating matchups for round {Round.Number}");

            if (!UpdateMatchups(Round.Matchups.ToList(), new List<Matchup>()))
            {
                Log.Fatal($"Update of matchups for round {Round.Number} failed!");
                throw new InvalidOperationException($"Update of matchups for round {Round.Number} failed!");
            }
        }

        private bool UpdateMatchups(List<Matchup> remainingMatchups, List<Matchup> fixedMatchups)
        {
            Contract.Assert(remainingMatchups.Count() + fixedMatchups.Count() == Round.Info.MatchupInfos.Count(), $"{remainingMatchups.Count()} + {fixedMatchups.Count()} != {Round.Info.MatchupInfos.Count()}");
            var indent = new string(' ', fixedMatchups.Count() + 2);
            Log.Info($"{indent}- UpdateMatchups recursive (depth: {fixedMatchups.Count()})");
            if (remainingMatchups.Count() == 0)
            {
                return true;
            }

            while (SelectNextMatchup(remainingMatchups, out Matchup matchup))
            {
                this.UpdateMatchup(matchup);
                Log.Info($"{indent} > Selecting for Id: {matchup.Id}, {matchup.Home} vs. {matchup.Away}");
                if (!remainingMatchups.Remove(matchup))
                {
                    throw new InvalidOperationException("Matchup must be removed, dammit!");
                }

                fixedMatchups.Add(matchup);

                if (UpdateMatchups(remainingMatchups, fixedMatchups))
                {
                    return true;
                }
            }

            return false;
        }
    }
}