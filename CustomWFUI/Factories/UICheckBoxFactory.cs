using System.Drawing;
using System.Windows.Forms;
using CustomWFUI.Styles;

namespace CustomWFUI.Factories
{
    internal static class UICheckBoxFactory
    {
        public static CheckBox CreateStandard(
            string text = "",
            bool checkedState = true)
        {
            CheckBox checkBox = new CheckBox
            {
                Text = text ?? "",
                Checked = checkedState,
                ForeColor = UIColors.TextSecondary,
                BackColor = Color.Transparent,
                Font = UIFonts.Normal,
                FlatStyle = FlatStyle.Flat
            };

            SetEnabledStyle(checkBox);

            return checkBox;
        }

        // Unlike Button/ComboBox, a FlatStyle.Flat CheckBox's native rendering
        // doesn't dim its text at all when disabled - confirmed by rendering
        // both states and comparing pixels, they were identical. Same
        // restore-last-live-value pattern as UIButtonFactory's
        // SetEnabledStyle, so a manual ForeColor override survives an
        // Enabled round-trip instead of resetting to the factory default.
        private static void SetEnabledStyle(CheckBox checkBox)
        {
            Color restoreForeColor = checkBox.ForeColor;

            checkBox.EnabledChanged += delegate
            {
                if (checkBox.Enabled)
                {
                    checkBox.ForeColor = restoreForeColor;
                }
                else
                {
                    restoreForeColor = checkBox.ForeColor;
                    checkBox.ForeColor = UIColors.TextDisabled;
                }
            };
        }

        public static CheckBox CreateCompact(
            bool checkedState = true)
        {
            CheckBox checkBox = CreateStandard(
                "",
                checkedState);

            checkBox.Size = new Size(25, 25);

            return checkBox;
        }
    }
}