using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace JlzQualiTool
{
    [Serializable]
    [System.ComponentModel.DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public class RoundInfo
    {
        [XmlAttribute]
        public int Mode { get; set; }

        [XmlAttribute]
        public int Number { get; set; }
    }
}