using log4net;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace JlzQualiTool
{
    public class RankingSnapshot : ObservableCollection<RankingEntry>
    {
        private static ILog Log = log4net.LogManager.GetLogger(typeof(RankingSnapshot));
        public static RankingSnapshot None = new RankingSnapshot();

        public RankingSnapshot(IEnumerable<RankingEntry> rankingEntries)
            : base(rankingEntries) { }

        public RankingSnapshot(IEnumerable<Matchup> matchups, List<int> pointsWithInversedOrder)
        {
            List<RankingEntry> rankingEntries = new List<RankingEntry>();

            var teams = matchups.SelectMany(x => new List<Team> { x.Away, x.Home }).Distinct();
            foreach (var team in teams)
            {
                // TODO can you do this in a single sophisticated linq statement?
                //var test = matchups.Where(m => m.WithTeam(team)).GroupBy(x => team).Select(x => new RankingEntry(x.Key, x.Sum(y => y.Points(team)), x.Sum(y => y.GoalsScored(team)), x.Sum(y => y.GoalsReceived(team)))).Single();

                var gamesPlayed = matchups.Where(m => m.WithTeam(team)).Count();
                var points = matchups.Where(m => m.WithTeam(team)).Sum(x => x.Points(team));
                var goalsScored = matchups.Where(m => m.WithTeam(team)).Sum(x => x.GoalsScored(team));
                var goalsReceived = matchups.Where(m => m.WithTeam(team)).Sum(x => x.GoalsReceived(team));
                var rankingEntry = new RankingEntry(team, gamesPlayed, points, goalsScored, goalsReceived, pointsWithInversedOrder.Contains(points));
                rankingEntries.Add(rankingEntry);
            }

            rankingEntries = rankingEntries.OrderByDescending(e => e.Position).ToList();

            for (int i = 0; i < rankingEntries.Count; i++)
            {
                RankingEntry entry = rankingEntries[i];
                Log.Info($" - {i + 1}. {entry.Team.Name}: {entry.GamesPlayed}, {entry.Points}, {entry.GoalsScored} : {entry.GoalsReceived} ({entry.Position}).");
                this.Add(entry);
            }

            // TODO how to correctly do all in one?
            //var testAll = matchups.GroupBy(x => x.Home).Select(x => new RankingEntry(x.Key, x.Sum(y => y.Points(x.Key)), x.Sum(y => y.GoalsScored(x.Key)), x.Sum(y => y.GoalsReceived(x.Key))));
        }

        private RankingSnapshot()
        {
        }
    }
}