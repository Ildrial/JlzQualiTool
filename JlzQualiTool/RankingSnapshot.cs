using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace JlzQualiTool
{
    public class RankingSnapshot : ObservableCollection<RankingEntry>
    {
        public RankingSnapshot(IEnumerable<Matchup> matchups)
        {
            var teams = matchups.SelectMany(x => new List<Team> { x.Away, x.Home }).Distinct();
            foreach (var team in teams)
            {
                // TODO can you do this in a single sophisticated linq statement?
                //var test = matchups.Where(m => m.WithTeam(team)).GroupBy(x => team).Select(x => new RankingEntry(x.Key, x.Sum(y => y.Points(team)), x.Sum(y => y.GoalsScored(team)), x.Sum(y => y.GoalsReceived(team)))).Single();

                var gamesPlayed = matchups.Where(m => m.WithTeam(team)).Count();
                var points = matchups.Where(m => m.WithTeam(team)).Sum(x => x.Points(team));
                var goalsScored = matchups.Where(m => m.WithTeam(team)).Sum(x => x.GoalsScored(team));
                var goalsReceived = matchups.Where(m => m.WithTeam(team)).Sum(x => x.GoalsReceived(team));
                var rankingEntry = new RankingEntry(team, gamesPlayed, points, goalsScored, goalsReceived);
                this.Add(rankingEntry);
            }

            // TODO how to correctly do all in one?
            //var testAll = matchups.GroupBy(x => x.Home).Select(x => new RankingEntry(x.Key, x.Sum(y => y.Points(x.Key)), x.Sum(y => y.GoalsScored(x.Key)), x.Sum(y => y.GoalsReceived(x.Key))));
        }
    }
}