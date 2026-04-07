using System;
using System.IO;

namespace SpeechMarkupEditor.Infrastructure.Data;

public static class AppDatabasePaths
{
    public static string GetDatabasePath()
    {
        var appDataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SpeechMarkupEditor");

        Directory.CreateDirectory(appDataDirectory);
        return Path.Combine(appDataDirectory, "speechmarkup.db");
    }
}
