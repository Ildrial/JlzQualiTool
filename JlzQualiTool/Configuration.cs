using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace JlzQualiTool
{
    [Serializable]
    [System.ComponentModel.DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    [XmlRoot("Configuration", IsNullable = false)]
    public class Configuration
    {
        [XmlAttribute]
        public byte Courts { get; set; }

        [XmlArrayItem("MatchupInfo", IsNullable = false)]
        public List<MatchupInfo> MatchupInfos { get; set; } = new List<MatchupInfo>();

        [XmlArrayItem("RoundInfo", IsNullable = false)]
        public List<RoundInfo> RoundInfos { get; set; } = new List<RoundInfo>();

        [XmlAttribute]
        public byte TotalTeams { get; set; }
    }
}