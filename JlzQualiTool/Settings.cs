using System;
using System.IO;

namespace JlzQualiTool
{
    internal static class Settings
    {
        internal static string JlzAppData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "JlzQualiTool");
        internal static string SavePath = Path.Combine(JlzAppData, "data");
    }
}