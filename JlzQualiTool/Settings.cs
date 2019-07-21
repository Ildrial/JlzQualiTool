using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace JlzQualiTool
{
    internal static class Settings
    {
        internal static string JlzAppData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "JlzQualiTool");
        internal static string SavePath = Path.Combine(JlzAppData, "data");
    }
}