using System;
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

        public string AwayTeamName => GetTeamName(Away);
        public int Court => (Id - 1) % 2 + 1;

        [XmlAttribute]
        public string Home { get; set; } = "";

        public string HomeTeamName => GetTeamName(Home);

        [XmlAttribute]
        public int Id { get; set; }

        [XmlAttribute]
        public int Order { get; set; }

        public int Round => Id / 10;

        // TODO use start time and match interval from Configuration
        public TimeSpan Time => Settings.StartTime + (((Round - 1) * (Settings.TotalTeams / 4)) + ((Order - 1) / 2)) * Settings.MatchInterval;

        private static string GetTeamName(string name)
        {
            if (name.StartsWith("W"))
            {
                return string.Format(Resources.Winner, name.Substring(1));
            }
            else if (name.StartsWith("L"))
            {
                return string.Format(Resources.Loser, name.Substring(1));
            }
            else if (string.IsNullOrEmpty(name))
            {
                return "??";
            }
            else if (int.TryParse(name, out int rank))
            {
                return string.Format(Resources.Rank, rank);
            }
            else
            {
                throw new InvalidOperationException($"Invalid name format for matchup: {name}");
            }
        }
    }
}