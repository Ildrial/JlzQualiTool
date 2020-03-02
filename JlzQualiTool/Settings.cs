using System;
using System.IO;

namespace JlzQualiTool
{
    internal static class Settings
    {
        internal static string JlzAppData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "JlzQualiTool");
        internal static string Log = Path.Combine(JlzAppData, "log");
        internal static string SavePath = Path.Combine(JlzAppData, "data");

        internal static TimeSpan StartTime = new TimeSpan(9, 0, 0);

        internal static TimeSpan MatchInterval = new TimeSpan(0, 25, 0);
        internal static int TotalTeams;
    }
}