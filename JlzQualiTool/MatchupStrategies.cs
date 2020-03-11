using log4net;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

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

            private RankingSnapshot PreviousRanking => Round.PreviousRound.Ranking;
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
                        Log.Info($"\t\t\t - Swapping {swapKey} ({Round.PreviousRound.Ranking[swapKey - 1]}) and {matchup.Info.Away} ({Round.PreviousRound.Ranking[int.Parse(matchup.Info.Away) - 1]}) ");
                        swapsForNextRound[swapKey] = int.Parse(matchup.Info.Away);
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
                var originalAwayRank = int.TryParse(matchup.Info.Away, out int a) ? a : 0;
                var originalHomeRank = int.TryParse(matchup.Info.Home, out int h) ? h : 0;
                var homeTeam = originalHomeRank > 0
                    ? this.GetTeamByPosition(originalHomeRank)
                    : this.GetFirstTeamWithoutMatch(fixedMatchups);

                List<int> checkOrder = CreateCheckOrder(originalAwayRank, originalHomeRank);

                Log.Info($"\t\t\t - Trying to match up {originalHomeRank} ({homeTeam}) with... [{string.Join(", ", checkOrder)}]");

                Team? awayTeam = null;

                for (int i = 0; i < checkOrder.Count(); i++)
                {
                    var awayRank = checkOrder[i];
                    var testTeam = GetTeamByPosition(awayRank);
                    Log.Info($"\t\t\t\t ... {awayRank} ({testTeam})");

                    if (IsMatchupValid(homeTeam, testTeam, fixedMatchups, failedOpponents))
                    {
                        awayTeam = testTeam;
                        if (originalAwayRank != awayRank)
                        {
                            // TODO improve handling to ensure no swapping back
                            if (!Swaps.ContainsKey(int.Parse(matchup.Info.Away)) || Swaps[int.Parse(matchup.Info.Away)] != awayRank)
                            {
                                swapKey = awayRank;
                            }
                            // TODO scrutiny
                            //SwapPositions(matchup, awayRank);
                        }
                        break;
                    }
                }

                if (awayTeam != null)
                {
                    matchup.Home = homeTeam;
                    matchup.Away = awayTeam;
                }

                // TODO time and court!
                matchup.Publish();

                return awayTeam != null;
            }

            private List<int> CreateCheckOrder(int originalAwayRank, int originalHomeRank)
            {
                if (originalAwayRank == 0)
                {
                    // Handling wild cards
                    // TODO optimize and start on home rank (requires additional parameter or different setting)
                    return Enumerable.Range(3, PreviousRanking.Count() - 2).ToList();
                }
                else
                {
                    Contract.Assert(originalHomeRank > 0, "Home team should have number greater than 0 assigned here.");
                    return Enumerable.Range(originalAwayRank, PreviousRanking.Count() - originalAwayRank + 1).Concat(Enumerable.Range(originalHomeRank + 1, originalAwayRank - originalHomeRank - 1).Reverse()).ToList();
                }
            }

            private Team GetFirstTeamWithoutMatch(List<Matchup> fixedMatchups)
            {
                foreach (var team in PreviousRanking.Select(r => r.Team))
                {
                    if (!fixedMatchups.Any(m => m.WithTeam(team)))
                    {
                        return team;
                    }
                }

                Contract.Assert(false, "At least one team without match must be found!");
                return new Team("I don't care, as I am actually dead code.");
            }

            /// <summary>
            /// Get team by position, considering already occurred swaps (which were done to avoid rematches).
            /// </summary>
            private Team GetTeamByPosition(int position)
            {
                Contract.Assert(position > 0, "Position must be greater than 0!");
                return Swaps.ContainsKey(position)

                        ? GetTeamByPosition(Swaps[position])
                        : PreviousRanking[position - 1].Team;
            }

            private bool HasPlayedThisRound(Team team, List<Matchup> fixedMatchups)
            {
                return fixedMatchups.Any(m => m.WithTeam(team));
            }

            private bool IsMatchupValid(Team homeTeam, Team testTeam, List<Matchup> fixedMatchups, List<Team> failedOpponents)
            {
                if (homeTeam.HasPlayed(testTeam))
                {
                    Log.Info($"\t\t\t - Rematch detected: {homeTeam} - {testTeam}");
                }
                else if (HasPlayedThisRound(testTeam, fixedMatchups))
                {
                    Log.Info($"\t\t\t - Potential opponent of {homeTeam} has already played: {testTeam}");
                    failedOpponents.Add(testTeam);
                }
                else if (failedOpponents.Contains(testTeam))
                {
                    Log.Info($"\t\t\t - Potential opponent of {homeTeam} already rejected: {testTeam}");
                }
                else
                {
                    return true;
                }

                return false;
            }

            private void SwapPositions(Matchup matchup, int newAway)
            {
                Log.Info($"\t\t\t - Swapping {newAway} and {matchup.Info.Away}");
                Swaps.Add(newAway, int.Parse(matchup.Info.Away));
            }
        }
    }
}