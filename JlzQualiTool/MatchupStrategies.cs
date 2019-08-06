using log4net;
using QualiTool;
using System.Collections.Generic;
using System.Linq;

namespace JlzQualiTool
{
    public interface IMatchupStrategy
    {
        void CreateMatchups(Round round);
    }

    public class KoStrategy : MatchupStrategyBase
    {
        protected override void CreateMatchupsInternal(Round round)
        {
            var frm = round.PreviousRound.Matchups.ToList().OrderBy(m => m.Id).ToList();

            if (frm.Count() % 2 == 0)
            {
                for (int i = 0; i < frm.Count(); i = i + 2)
                {
                    round.CreateAndAddMatchup(frm[i].Winner, frm[i + 1].Winner);
                    round.CreateAndAddMatchup(frm[i].Loser, frm[i + 1].Loser);
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

    public class SeededStrategy : MatchupStrategyBase
    {
        public SeededStrategy(List<Team> teams)
        {
            Teams = teams;
        }

        private List<Team> Teams { get; }

        protected override void CreateMatchupsInternal(Round round)
        {
            for (int i = 0; i < 4; i++)
            {
                var home = Teams.First(t => t.Seed == i + 1);
                var away = Teams.First(t => t.Seed == 0 && t.Matchups.Count == 0 && t != home);
                round.CreateAndAddMatchup(home, away);

                var home2 = Teams.First(t => t.Seed == 0 && t.Matchups.Count == 0);
                var away2 = Teams.First(t => t.Seed == 0 && t.Matchups.Count == 0 && t != home2);
                round.CreateAndAddMatchup(home2, away2);
            }
        }
    }
}