using log4net;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JlzQualiTool
{
    public interface IMatchupStrategy
    {
        void CreateMatchups(Round round);
    }

    public class InitialOrderStrategy : MatchupStrategyBase
    {
        public InitialOrderStrategy(List<Team> teams)
        {
            Teams = teams;
        }

        private List<Team> Teams { get; }

        protected override void CreateMatchupsInternal(Round round)
        {
            for (int i = 0; i < Teams.Count; i += 2)
            {
                _ = round.CreateAndAddMatchup(this.Teams[i], this.Teams[i + 1]);
            }
        }
    }

    public class RankingStrategy : MatchupStrategyBase
    {
        private List<Tuple<int, int>> Pairings { get; }

        public RankingStrategy(List<Tuple<int, int>> pairings)
        {
            this.Pairings = pairings;
        }

        protected override void CreateMatchupsInternal(Round round)
        {
            if (round.PreviousRound.Matchups.Count() % 2 == 0)
            {
                for (int p = 0; p < Pairings.Count(); p++)
                {
                    var rank1 = Pairings[p].Item1;
                    var rank2 = Pairings[p].Item2;

                    var matchup = round.CreateAndAddMatchup(
                        new Team(string.Format(Resources.Rank, rank1)),
                        new Team(string.Format(Resources.Rank, rank2)));

                    // TODO put in methods and use sender and event args
                    round.PreviousRound.OnRankingUpdatedEvent += (o, e) =>
                    {
                        // TODO handle rematches correctly
                        if (o != null)
                        {
                            var round = (Round)o;
                            // TODO parts of table must be fixed before... (idea: fixed flag on ranking entry?)
                            if (round.Matchups.All(m => m.IsPlayed))
                            {
                                matchup.Home = round.Ranking[rank1 - 1].Team;
                                matchup.Away = round.Ranking[rank2 - 1].Team;
                                // TODO adjust log text
                                Log.Debug($"Complete ranking of round {round.Number} updated. Updating game {matchup.Id}: {matchup.Home.Name} - {matchup.Away.Name}.");
                            }

                            matchup.Publish();
                        }
                    };
                }
            }
            else
            {
                // TODO for 14 teams, respectively any number of even teams not dividable by 4
            }
        }
    }

    public class KoStrategy : MatchupStrategyBase
    {
        protected override void CreateMatchupsInternal(Round previousRound)
        {
            var frm = previousRound.PreviousRound.Matchups.ToList().OrderBy(m => m.Id).ToList();

            if (frm.Count() % 2 == 0)
            {
                for (int i = 0; i < frm.Count(); i += 2)
                {
                    var frm1 = frm[i];
                    var frm2 = frm[i + 1];

                    var matchup1 = previousRound.CreateAndAddMatchup(
                        new Team(string.Format(Resources.Winner, frm1.Id)),
                        new Team(string.Format(Resources.Winner, frm2.Id)));
                    var matchup2 = previousRound.CreateAndAddMatchup(
                        new Team(string.Format(Resources.Loser, frm1.Id)),
                        new Team(string.Format(Resources.Loser, frm2.Id)));

                    // TODO put in methods and use sender and event args
                    frm1.OnMatchPlayedEvent += (o, e) =>
                    {
                        matchup1.Home = frm1.Winner ?? matchup1.Home;
                        matchup2.Home = frm1.Loser ?? matchup2.Home;

                        Log.Debug($"Match {frm1.Id} played. Updating Home team for game {matchup1.Id} ({matchup1.Home.Name}) and {matchup2.Id} ({matchup2.Home.Name}).");

                        matchup1.Publish();
                        matchup2.Publish();
                    };
                    frm2.OnMatchPlayedEvent += (o, e) =>
                    {
                        matchup1.Away = frm2.Winner ?? matchup1.Away;
                        matchup2.Away = frm2.Loser ?? matchup2.Away;

                        Log.Debug($"Match {frm2.Id} played. Updating Away team for game {matchup1.Id} ({matchup1.Away.Name}) and {matchup2.Id} ({matchup2.Away.Name}).");

                        matchup1.Publish();
                        matchup2.Publish();
                    };
                }
            }
            else
            {
                // TODO for 14 teams, respectively any number of even teams not dividable by 4
            }
        }
    }

    public abstract class MatchupStrategyBase : IMatchupStrategy
    {
        protected static ILog Log = log4net.LogManager.GetLogger(typeof(MatchupStrategyBase));

        public void CreateMatchups(Round round)
        {
            Log.Info($"Creating matchups for round '{round.Number}':");
            CreateMatchupsInternal(round);
        }

        protected abstract void CreateMatchupsInternal(Round round);
    }

    public class NoStrategy : MatchupStrategyBase
    {
        public static IMatchupStrategy Get = new NoStrategy();

        protected override void CreateMatchupsInternal(Round round)
        {
            throw new System.NotImplementedException();
        }
    }
}