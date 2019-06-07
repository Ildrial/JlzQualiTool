using System;

namespace QualiTool
{
    public class Matchup
    {
        public DateTime Time { get; set; }

        public Team? Home { get; set; }

        public Team? Away { get; set; }
        public Team? Winner => this.HomeGoal >= this.AwayGoal ? this.Home : this.Away;
        public Team? Loser => this.HomeGoal < this.AwayGoal ? this.Home : this.Away;
        public int HomeGoal { get; set; }
        public int AwayGoal { get; set; }
        public int Round { get; set; }
        public int Id { get; set; }
    }
}
