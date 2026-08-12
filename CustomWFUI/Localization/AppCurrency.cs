using System;
using System.Globalization;

namespace CustomWFUI
{
    public enum AppCurrency
    {
        Euro,
        Dollar
    }

    /// <summary>
    /// Formats amounts for a chosen currency - deliberately independent of
    /// AppLanguage/AppLocalization. Someone reading German text doesn't
    /// necessarily want US number formatting just because they picked
    /// Dollar, and vice versa, so this only controls the currency symbol
    /// and its own number format, not the rest of the UI.
    /// </summary>
    public static class AppCurrencyFormatter
    {
        // Matches AppLocalization's own default language (English).
        private static AppCurrency _currency = AppCurrency.Dollar;

        public static AppCurrency Currency
        {
            get { return _currency; }
            set
            {
                if (_currency == value)
                    return;

                _currency = value;
                CurrencyChanged?.Invoke(null, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Raised when Currency changes. Like AppLocalization.LanguageChanged,
        /// this only signals that already-displayed amounts should be
        /// re-formatted and re-applied - it doesn't do that automatically.
        /// </summary>
        public static event EventHandler CurrencyChanged;

        /// <summary>
        /// Formats an amount using the currently selected currency's own
        /// symbol, placement and number format (e.g. "1.234,50€" for Euro,
        /// "$1,234.50" for Dollar) - regardless of the app's UI language.
        /// </summary>
        public static string Format(decimal amount, string decimalFormat = "#,0.00")
        {
            switch (Currency)
            {
                case AppCurrency.Dollar:
                    return "$" + amount.ToString(decimalFormat, CultureInfo.GetCultureInfo("en-US"));

                case AppCurrency.Euro:
                default:
                    return amount.ToString(decimalFormat, CultureInfo.GetCultureInfo("de-DE")) + "€";
            }
        }

        /// <summary>
        /// Just the symbol for the currently selected currency, for cases
        /// that build their own formatted string (e.g. appending it to an
        /// already-formatted number).
        /// </summary>
        public static string Symbol
        {
            get { return Currency == AppCurrency.Dollar ? "$" : "€"; }
        }
    }
}
