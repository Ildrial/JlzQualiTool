using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace JlzQualiTool
{
    public class StrategyFactory
    {
        public static IMatchupStrategy GetStrategy(RoundInfo roundInfo, ViewModel viewModel)
        {
            return roundInfo.Type switch
            {
                MatchupType.Ordered => new InitialOrderStrategy(roundInfo, viewModel.Teams.ToList()),
                MatchupType.Ko => new KoStrategy(roundInfo),
                MatchupType.RankingBased => new RankingStrategy(roundInfo),
                _ => throw new InvalidOperationException($"Unknown matchup type '{roundInfo.Type}'."),
            };
        }
    }
}