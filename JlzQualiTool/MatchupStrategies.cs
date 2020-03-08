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
            Round.PreviousRound.OnRankingUpdatedEvent += (o, e) => MatchupCalculator.Run(Round);
        }

        internal class MatchupCalculator
        {
            private MatchupCalculator(Round round) : this(round, new Dictionary<int, int>())
            {
            }

            private MatchupCalculator(Round round, Dictionary<int, int> swaps)
            {
                // TODO instance must only know previous' round ranking and remaining matchups + fixed!
                this.Round = round;
                this.Swaps = new Dictionary<int, int>(swaps);
            }

            private Round Round { get; }

            private Dictionary<int, int> Swaps { get; } = new Dictionary<int, int>();

            internal static void Run(Round round)
            {
                if (!round.PreviousRound.Matchups.All(m => m.IsPlayed) || round.Number == 5)
                {
                    // TODO parts of table must be fixed before all played... (idea: fixed flag on ranking entry?)
                    // TODO also do calculations if not complete previous round is already played.
                    return;
                }

                // TODO adjust log text once not complete ranking is required
                Log.Info($"Complete ranking of round {round.PreviousRound.Number} updated. Updating matchups for round {round.Number}");

                if (!new MatchupCalculator(round).CalculateRemainingMatchups(round.Matchups.ToList()))
                {
                    Log.Fatal($"Update of matchups for round {round.Number} failed!");
                    throw new InvalidOperationException($"Update of matchups for round {round.Number} failed!");
                }
            }

            private bool CalculateRemainingMatchups(List<Matchup> matchups)
            {
                var setMatchups = matchups.Where(m => m.IsSet).ToList();

                var indent = new string(' ', setMatchups.Count() + 2);
                Log.Info($"{indent}> UpdateMatchups recursive (depth: {setMatchups.Count()})");

                //Contract.Assert(matchups.Count() + setMatchups.Count() == Round.Info.MatchupInfos.Count(), $"{matchups.Count()} + {fixedMatchups.Count()} != {Round.Info.MatchupInfos.Count()}");

                if (matchups.All(m => m.IsSet))
                {
                    // TODO is this clause necesary at all?
                    return true;
                }

                var matchup = matchups.First(m => !m.IsSet);
                //if (!matchups.Remove(matchup))
                //{
                //    throw new InvalidOperationException("Matchup must be removed, dammit!");
                //}

                // TODO check if necessary
                var failedOpponents = new List<Team>();

                Log.Info($"{indent} + Selecting for Id: {matchup.Id}...");
                while (this.ConfigureMatchup(matchup, setMatchups, failedOpponents, out int swapKey))
                {
                    Log.Info($"{indent}   > [{matchup.Id}] {matchup.Home} vs. {matchup.Away}");

                    //matchup.IsSet = true;
                    setMatchups = matchups.Where(m => m.IsSet).ToList();

                    var swapsForNextRound = new Dictionary<int, int>(Swaps);
                    if (swapKey > 0)
                    {
                        Swaps[swapKey] = int.Parse(matchup.Info.Away);
                    }
                    // TODO Next function instead of instantiation here?
                    if (new MatchupCalculator(Round, swapsForNextRound).CalculateRemainingMatchups(matchups))
                    {
                        Log.Info($"{indent}<:) Leaving recursion successfully (depth: {setMatchups.Count()})");
                        return true;
                    }

                    // Ordner of these three calls is significant!
                    failedOpponents.Add(matchup.Away);
                    matchup.Reset();
                    //ClearUnfixedSwaps();
                }

                Log.Info($"{indent}<! Leaving recursion unsuccessfully (depth: {setMatchups.Count()})");

                return false;
            }

            private void ClearUnfixedSwaps()
            {
                // TODO probably should not be that complicated...
                var unfixedIds = Round.Matchups.Where(m => !m.IsSet).Select(m => int.Parse(m.Info.Home))
                    .Concat(Round.Matchups.Where(m => !m.IsSet).Select(m => int.Parse(m.Info.Away)));

                foreach (var key in Swaps.Keys)
                {
                    var value = Swaps[key];
                    if (unfixedIds.Contains(value))
                    {
                        Swaps.Remove(key);
                        Log.Info($"Removing Swap for key {key} and value {Swaps[key]}.");
                    }
                }
            }

            private bool ConfigureMatchup(Matchup matchup, List<Matchup> fixedMatchups, List<Team> failedOpponents, out int swapKey)
            {
                swapKey = 0;
                var homeRank = GetRankByConfigString(matchup.Info.Home);
                var awayRank = GetRankByConfigString(matchup.Info.Away);
                if (homeRank == 0 || awayRank == 0)
                {
                    // TODO handle wild cards
                    return false;
                }

                var home = Round.PreviousRound.Ranking[homeRank - 1].Team;

                Team? away = null;
                var originalRank = int.Parse(matchup.Info.Away);
                for (int a = originalRank - 1; a < Round.PreviousRound.Ranking.Count(); a++)
                {
                    awayRank = GetRankByConfigString((a + 1).ToString());
                    var testTeam = Round.PreviousRound.Ranking[awayRank - 1].Team;

                    if (home.HasPlayed(testTeam))
                    {
                        Log.Info($"\t\t\t - Rematch detected: {home} - {testTeam}");
                    }
                    else if (HasPlayedThisRound(testTeam, fixedMatchups))
                    {
                        Log.Info($"\t\t\t - Potential opponent of {home} has already played: {testTeam}");
                        failedOpponents.Add(testTeam);
                    }
                    else if (failedOpponents.Contains(testTeam))
                    {
                        Log.Info($"\t\t\t - Potential opponent of {home} already rejected: {testTeam}");
                    }
                    else
                    {
                        away = testTeam;
                        if (originalRank != awayRank)
                        {
                            swapKey = awayRank;
                            // TODO scrutiny
                            SwapPositions(matchup, awayRank);
                        }
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
                // TODO might need to handle cascading resolution
                var positionAsInt = int.Parse(position);
                return string.IsNullOrEmpty(position)
                    ? 0
                    : Swaps.ContainsKey(positionAsInt)
                        ? Swaps[positionAsInt]
                        : positionAsInt;
            }

            private bool HasPlayedThisRound(Team team, List<Matchup> fixedMatchups)
            {
                return fixedMatchups.Any(m => m.WithTeam(team));
            }

            private void SwapPositions(Matchup matchup, int newAway)
            {
                Log.Info($"\t\t\t - Swapping {newAway} and {matchup.Info.Away}");
                Swaps.Add(newAway, int.Parse(matchup.Info.Away));
            }
        }
    }
}