using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace JlzQualiTool
{
    public class StrategyFactory
    {
        public static IMatchupStrategy GetStrategy(Round round, ViewModel viewModel)
        {
            return round.Info.Type switch
            {
                MatchupType.Ordered => new InitialOrderStrategy(round, viewModel.Teams.ToList()),
                MatchupType.Ko => new KoStrategy(round),
                MatchupType.RankingBased => new RankingStrategy(round),
                _ => throw new InvalidOperationException($"Unknown matchup type '{round.Info.Type}'."),
            };
        }
    }
}