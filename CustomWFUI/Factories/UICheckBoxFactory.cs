using System;
using System.Drawing;
using System.Windows.Forms;
using CustomWFUI.Styles;

namespace CustomWFUI.Factories
{
    [Obsolete("Use CustomWFUI.UIStyles.CheckBoxes instead.", false)]
    public static class UICheckBoxFactory
    {
        public static CheckBox CreateStandard(
            string text = "",
            bool checkedState = true)
        {
            return new CheckBox
            {
                Text = text ?? "",
                Checked = checkedState,
                ForeColor = UIColors.TextSecondary,
                BackColor = Color.Transparent,
                Font = UIFonts.Normal,
                FlatStyle = FlatStyle.Flat
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