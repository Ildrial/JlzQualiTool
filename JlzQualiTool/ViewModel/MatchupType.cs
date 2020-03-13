using System.Xml.Serialization;

namespace JlzQualiTool
{
    public enum MatchupType
    {
        [XmlEnum(Name = "Ordered")]
        Ordered,

        [XmlEnum(Name = "Ko")]
        Ko,

        [XmlEnum(Name = "RankingBased")]
        RankingBased
    }
}