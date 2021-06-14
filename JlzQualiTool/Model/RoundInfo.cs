using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace JlzQualiTool
{
    [Serializable]
    [System.ComponentModel.DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public class RoundInfo
    {
        // TODO consider serialization in specific matchup types
        [XmlArrayItem("MatchupInfo", IsNullable = false)]
        public List<MatchupInfo> MatchupInfos { get; set; } = new List<MatchupInfo>();

        [XmlAttribute]
        public int Number { get; set; }

        public int TotalMatches => MatchupInfos.Count;

        [XmlAttribute]
        public MatchupType Type { get; set; }
    }
}