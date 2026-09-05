using System;
using System.Collections.Generic;

namespace ErikwnkWFUI.Styles
{
    public enum UILanguage
    {
        English,
        German
    }

    /// <summary>
    /// User-facing text for the handful of built-in dialogs/controls that ship
    /// their own copy (update prompt, title bar tooltips). Defaults to English so
    /// existing consumers see no change; set <see cref="Language"/> (e.g. via
    /// UIStyles.Language) to switch everything at once, at startup or live at
    /// runtime - LanguageChanged lets already-built controls (like the title
    /// bar's own tooltips) react immediately.
    /// </summary>
    internal static class UIStrings
    {
        private static UILanguage _language = UILanguage.English;

        public static UILanguage Language
        {
            get { return _language; }
            set
            {
                if (_language == value)
                    return;

                _language = value;
                LanguageChanged?.Invoke(null, EventArgs.Empty);
            }
        }

        public static event EventHandler LanguageChanged;

        private static readonly Dictionary<string, string> English = new Dictionary<string, string>
        {
            ["TitleBar.Minimize"] = "Minimize",
            ["TitleBar.Maximize"] = "Maximize",
            ["TitleBar.Close"] = "Close",

            ["UpdateAvailable.Title"] = "Update available",
            ["UpdateAvailable.Message"] = "A new version is available.",
            ["UpdateAvailable.Later"] = "Later",
            ["UpdateAvailable.UpdateNow"] = "Update now",
            ["UpdateAvailable.Downloading"] = "Downloading update... {0}%",

            ["Update.Title"] = "Update",
            ["Update.DownloadFailedMessage"] = "The update download failed. Opening the release page instead.",
            ["Update.AvailableTooltip"] = "A new version is available - click to update",

            ["MessageBox.Cancel"] = "Cancel",
            ["InfoPopup.None"] = "None",

            ["ListView.CopySelection"] = "Copy selection",
            ["ListView.CopyAll"] = "Copy all",
            ["ListView.AsTable"] = "As table",
            ["ListView.RowCopied"] = "Row copied",
            ["ListView.RowsCopied"] = "{0} rows copied",
            ["ListView.WithHeaderSuffix"] = " (with header)",
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
            ["Update.AvailableTooltip"] = "Eine neue Version ist verfügbar - zum Aktualisieren klicken",

            ["MessageBox.Cancel"] = "Abbrechen",
            ["InfoPopup.None"] = "Keine",

            ["ListView.CopySelection"] = "Auswahl kopieren",
            ["ListView.CopyAll"] = "Alles kopieren",
            ["ListView.AsTable"] = "Als Tabelle",
            ["ListView.RowCopied"] = "Zeile kopiert",
            ["ListView.RowsCopied"] = "{0} Zeilen kopiert",
            ["ListView.WithHeaderSuffix"] = " (mit Kopfzeile)",
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
