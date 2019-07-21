using System;
using System.Runtime.Serialization;

namespace QualiTool
{
    public class Matchup
    {
        [DataMember]
        public Team? Away { get; set; }

        [DataMember]
        public int AwayGoal { get; set; }

        [DataMember]
        public Team? Home { get; set; }

        [DataMember]
        public int HomeGoal { get; set; }

        [DataMember]
        public int Id { get; set; }

        public Team? Loser => this.HomeGoal < this.AwayGoal ? this.Home : this.Away;

        [DataMember]
        public int Round { get; set; }

        //[DataMember]
        public DateTime Time { get; set; }

        public Team? Winner => this.HomeGoal >= this.AwayGoal ? this.Home : this.Away;
    }
}