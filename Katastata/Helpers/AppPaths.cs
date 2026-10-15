using System;
using System.IO;

namespace Katastata.Helpers
{
    public static class AppPaths
    {
        public static string DataDir => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Katastata");

        public static string DbPath => Path.Combine(DataDir, "katastata.db");
        public static string LogPath => Path.Combine(DataDir, "error.log");
    }
}