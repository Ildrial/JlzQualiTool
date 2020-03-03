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

        [XmlArrayItem("RoundInfo", IsNullable = false)]
        public List<RoundInfo> RoundInfos { get; set; } = new List<RoundInfo>();

        // TODO define time span like P1Y2M3DT10H30M (https://www.w3.org/TR/xmlschema-2/#duration)
        //[XmlAttribute]
        //public TimeSpan StartTime { get; set; } = new TimeSpan(9, 0, 0);
        //[XmlAttribute]
        //public TimeSpan MatchInterval { get; set; } = new TimeSpan(0, 25, 0);

        [XmlAttribute]
        public byte TotalTeams { get; set; }
    }
}