using System;
using System.Collections.Generic;
using System.IO;
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
        // TODO put ranking order into configuration
        public static Configuration Current { get; private set; } = new Configuration();

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

        public static void Load(int totalTeams)
        {
            var configFile = Path.Combine(Settings.ConfigLocation, $"config{totalTeams}.xml");

            if (!File.Exists(configFile))
            {
                throw new InvalidOperationException(string.Format(Resources.Exception_NoConfigFileFound, totalTeams));
            }

            XmlSerializer serializer = new XmlSerializer(typeof(Configuration));

            StreamReader reader = new StreamReader(configFile, Encoding.UTF8);
            var configuration = (Configuration)serializer.Deserialize(reader);
            reader.Close();

            Current = configuration;
        }
    }
}