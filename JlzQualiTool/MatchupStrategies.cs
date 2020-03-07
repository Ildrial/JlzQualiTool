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

            // TODO check if events need to be detached.
            Round.PreviousRound.OnRankingUpdatedEvent += (o, e) => new MatchupCalculator(Round).Run();
        }

        internal class MatchupCalculator
        {
            internal MatchupCalculator(Round round)
            {
                this.Round = round;
            }

            private Round Round { get; }

            private Dictionary<int, int> Swaps { get; } = new Dictionary<int, int>();

            internal void Run()
            {
                if (!Round.PreviousRound.Matchups.All(m => m.IsPlayed) || Round.Number == 5)
                {
                    // TODO parts of table must be fixed before all played... (idea: fixed flag on ranking entry?)
                    // TODO also do calculations if not complete previous round is already played.
                    return;
                }

                // TODO adjust log text once not complete ranking is required
                Log.Info($"Complete ranking of round {Round.PreviousRound.Number} updated. Updating matchups for round {Round.Number}");

                // TODO only pass matchups that are not fixed yet
                if (!CalculateRemainingMatchups(Round.Matchups.ToList(), new List<Matchup>()))
                {
                    Log.Fatal($"Update of matchups for round {Round.Number} failed!");
                    throw new InvalidOperationException($"Update of matchups for round {Round.Number} failed!");
                }
            }

            private bool CalculateRemainingMatchups(List<Matchup> remainingMatchups, List<Matchup> fixedMatchups)
            {
                var indent = new string(' ', fixedMatchups.Count() + 2);
                Log.Info($"{indent}> UpdateMatchups recursive (depth: {fixedMatchups.Count()})");

                Contract.Assert(remainingMatchups.Count() + fixedMatchups.Count() == Round.Info.MatchupInfos.Count(), $"{remainingMatchups.Count()} + {fixedMatchups.Count()} != {Round.Info.MatchupInfos.Count()}");

                if (remainingMatchups.Count() == 0)
                {
                    return true;
                }

                var matchup = remainingMatchups.First();
                if (!remainingMatchups.Remove(matchup))
                {
                    throw new InvalidOperationException("Matchup must be removed, dammit!");
                }

                // TODO check if necessary
                var failedOpponents = new List<Team>();

                do
                {
                    if (!this.ConfigureMatchup(matchup, fixedMatchups, failedOpponents))
                    {
                        break;
                    }

                    fixedMatchups.Add(matchup);

                    Log.Info($"{indent} + Selecting for Id: {matchup.Id}, {matchup.Home} vs. {matchup.Away}");

                    if (CalculateRemainingMatchups(remainingMatchups.ToList(), fixedMatchups))
                    {
                        Log.Info($"{indent}<:) Leaving recursion successfully (depth: {fixedMatchups.Count()})");
                        return true;
                    }

                    fixedMatchups.Remove(matchup);
                    failedOpponents.Add(matchup.Away);
                } while (HasMorePossibleMatchups());

                Log.Info($"{indent}<! Leaving recursion unsuccessfully (depth: {fixedMatchups.Count()})");

                Swaps.Clear();
                return false;
            }

            private bool ConfigureMatchup(Matchup matchup, List<Matchup> fixedMatchups, List<Team> failedOpponents)
            {
                var homeRank = GetRankByConfigString(matchup.Info.Home);
                var awayRank = GetRankByConfigString(matchup.Info.Away);
                if (homeRank == 0 || awayRank == 0)
                {
                    // TODO handle wild cards
                    return false;
                }

                var home = Round.PreviousRound.Ranking[homeRank - 1].Team;

                // TODO without dummy team.
                Team? away = null;
                for (int a = awayRank - 1; a < Round.PreviousRound.Ranking.Count(); a++)
                {
                    var testTeam = Round.PreviousRound.Ranking[a].Team;

                    if (home.HasPlayed(testTeam))
                    {
                        Log.Info($"\t\t\t - Rematch detected: {home} - {testTeam}");
                        // TODO choose next possible away opponent
                    }
                    else if (HasPlayedThisRound(testTeam, fixedMatchups))
                    {
                        Log.Info($"\t\t\t - Potential oponent of {home} has already played: {testTeam}");
                        failedOpponents.Add(testTeam);
                        // TODO choose next possible away opponent
                    }
                    else if (failedOpponents.Contains(testTeam))
                    {
                        Log.Info($"\t\t\t - Potential oponent of {home} already failed: {testTeam}");
                        // TODO choose next possible away opponent
                    }
                    else
                    {
                        away = testTeam;
                        SwapPositions(matchup, a + 1);
                        break;
                    }
                }

                if (away != null)
                {
                    matchup.Home = home;
                    matchup.Away = away;
                }

                // TODO time and court!
                matchup.Publish();

                return away != null;
            }

            private int GetRankByConfigString(string position)
            {
                var positionAsInt = int.Parse(position);
                return string.IsNullOrEmpty(position)
                    ? 0
                    : Swaps.ContainsKey(positionAsInt)
                        ? Swaps[positionAsInt]
                        : positionAsInt;
            }

            private bool HasMorePossibleMatchups()
            {
                // TODO add parameters and implement logic
                return true;
            }

            private bool HasPlayedThisRound(Team team, List<Matchup> fixedMatchups)
            {
                return fixedMatchups.Any(m => m.WithTeam(team));
            }

            private void SwapPositions(Matchup matchup, int newAway)
            {
                Swaps.Add(newAway, int.Parse(matchup.Info.Away));
            }
        }
    }
}