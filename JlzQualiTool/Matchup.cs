using System;

namespace QualiTool
{
    public class Matchup
    {
        public Team? Away { get; set; }
        public int AwayGoal { get; set; }
        public Team? Home { get; set; }
        public int HomeGoal { get; set; }
        public int Id { get; set; }
        public Team? Loser => this.HomeGoal < this.AwayGoal ? this.Home : this.Away;
        public int Round { get; set; }
        public DateTime Time { get; set; }
        public Team? Winner => this.HomeGoal >= this.AwayGoal ? this.Home : this.Away;
    }
}