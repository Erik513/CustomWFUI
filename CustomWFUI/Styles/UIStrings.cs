using System.Collections.Generic;

namespace CustomWFUI.Styles
{
    public enum UILanguage
    {
        English,
        German
    }

    /// <summary>
    /// User-facing text for the handful of built-in dialogs/controls that ship
    /// their own copy (update prompt, title bar tooltips). Defaults to English so
    /// existing consumers see no change; set <see cref="Language"/> once at
    /// startup (e.g. via UIStyles.Language) to switch everything at once.
    /// </summary>
    internal static class UIStrings
    {
        public static UILanguage Language { get; set; } = UILanguage.English;

        private static readonly Dictionary<string, string> English = new Dictionary<string, string>
        {
            ["TitleBar.Minimize"] = "Minimize window",
            ["TitleBar.Maximize"] = "Maximize window",
            ["TitleBar.Close"] = "Close window",

            ["UpdateAvailable.Title"] = "Update available",
            ["UpdateAvailable.Message"] = "A new version is available.",
            ["UpdateAvailable.Later"] = "Later",
            ["UpdateAvailable.UpdateNow"] = "Update now",
            ["UpdateAvailable.Downloading"] = "Downloading update... {0}%",

            ["Update.Title"] = "Update",
            ["Update.DownloadFailedMessage"] = "The update download failed. Opening the release page instead.",
        };

        private static readonly Dictionary<string, string> German = new Dictionary<string, string>
        {
            ["TitleBar.Minimize"] = "Minimieren",
            ["TitleBar.Maximize"] = "Maximieren",
            ["TitleBar.Close"] = "Schließen",

            ["UpdateAvailable.Title"] = "Update verfügbar",
            ["UpdateAvailable.Message"] = "Eine neue Version ist verfügbar.",
            ["UpdateAvailable.Later"] = "Später",
            ["UpdateAvailable.UpdateNow"] = "Jetzt aktualisieren",
            ["UpdateAvailable.Downloading"] = "Update wird heruntergeladen... {0}%",

            ["Update.Title"] = "Update",
            ["Update.DownloadFailedMessage"] = "Der Update-Download ist fehlgeschlagen. Die Release-Seite wird stattdessen geöffnet.",
        };

        public static string Get(string key)
        {
            Dictionary<string, string> table = Language == UILanguage.German ? German : English;

            string value;
            if (table.TryGetValue(key, out value))
            {
                return value;
            }

            return key;
        }
    }
}
