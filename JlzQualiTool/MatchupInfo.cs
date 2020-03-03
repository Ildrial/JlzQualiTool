using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace JlzQualiTool
{
    [Serializable]
    [System.ComponentModel.DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public class MatchupInfo
    {
        [XmlAttribute]
        public string Away { get; set; } = "";

        [XmlAttribute]
        public string Home { get; set; } = "";

        [XmlAttribute]
        public byte Id { get; set; }

        [XmlAttribute]
        public byte Order { get; set; }
    }
}