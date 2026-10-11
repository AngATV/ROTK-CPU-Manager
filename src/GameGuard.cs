using System;
using System.IO;

namespace RotkCpu
{
    internal static class GameGuard
    {
        internal const string DefaultFolder = @"C:\Games\ROTK";

        internal static string Normalize(string folder)
        {
            if (String.IsNullOrWhiteSpace(folder))
                throw new ArgumentException(Locale.T("Choisissez le dossier ROTK."));
            string fullPath = Path.GetFullPath(folder);
            string root = Path.GetPathRoot(fullPath);
            return fullPath.Length == root.Length ? fullPath
                : fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        internal static string Validate(string folder)
        {
            folder = Normalize(folder);
            if (!File.Exists(Path.Combine(folder, "H1Z1.exe")) || !File.Exists(Path.Combine(folder, "rotkc.dll")))
                throw new IOException(Locale.T("Ce dossier doit contenir H1Z1.exe et rotkc.dll (client ROTK)."));
            return folder;
        }

        internal static bool Matches(string folder, string executablePath)
        {
            return String.Equals(Path.GetFullPath(executablePath), Path.Combine(Normalize(folder), "H1Z1.exe"), StringComparison.OrdinalIgnoreCase);
        }
    }
}
