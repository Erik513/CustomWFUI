using System;
using System.Collections.Generic;

namespace CustomWFUI
{
    public enum AppLanguage
    {
        English,
        German
    }

    /// <summary>
    /// General-purpose text localization for a consumer app's own strings -
    /// separate from CustomWFUI's internal UIStrings, which only covers the
    /// library's own built-in dialogs (title bar tooltips, update prompt).
    /// An app registers one dictionary per language once at startup, then
    /// looks strings up by key. Switching Language raises LanguageChanged so
    /// already-built UI can refresh itself.
    /// </summary>
    public static class AppLocalization
    {
        private static readonly Dictionary<AppLanguage, Dictionary<string, string>> Tables =
            new Dictionary<AppLanguage, Dictionary<string, string>>();

        private static AppLanguage _language = AppLanguage.English;

        public static AppLanguage Language
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

        /// <summary>
        /// Raised when Language changes. UI built before the change has to
        /// re-fetch its strings and re-apply them itself - this event only
        /// signals that it should, it doesn't do that automatically.
        /// </summary>
        public static event EventHandler LanguageChanged;

        /// <summary>
        /// Adds (or overwrites) the translations for one language. Call once
        /// per language at startup, before building any UI that uses Get.
        /// </summary>
        public static void Register(AppLanguage language, IDictionary<string, string> translations)
        {
            if (translations == null)
                return;

            Dictionary<string, string> table;
            if (!Tables.TryGetValue(language, out table))
            {
                table = new Dictionary<string, string>();
                Tables[language] = table;
            }

            foreach (KeyValuePair<string, string> pair in translations)
                table[pair.Key] = pair.Value;
        }

        /// <summary>
        /// Looks up a string in the current language. Falls back to
        /// English, then to the key itself, so a missing translation never
        /// crashes - it just shows the raw key, which is easy to notice.
        /// </summary>
        public static string Get(string key)
        {
            if (key == null)
                return "";

            string value;

            Dictionary<string, string> table;
            if (Tables.TryGetValue(_language, out table) && table.TryGetValue(key, out value))
                return value;

            if (_language != AppLanguage.English &&
                Tables.TryGetValue(AppLanguage.English, out table) &&
                table.TryGetValue(key, out value))
            {
                return value;
            }

            return key;
        }

        /// <summary>
        /// Same as Get, but runs the result through string.Format with the
        /// given arguments - for strings with placeholders like "{0}".
        /// </summary>
        public static string Get(string key, params object[] args)
        {
            return string.Format(Get(key), args);
        }
    }
}
