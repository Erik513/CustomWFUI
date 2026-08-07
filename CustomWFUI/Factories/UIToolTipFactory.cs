using System;
using System.Windows.Forms;
using CustomWFUI.Styles;

namespace CustomWFUI.Factories
{
    [Obsolete("Use CustomWFUI.UIStyles.ToolTips instead.", false)]
    public static class UIToolTipFactory
    {
        public static ToolTip CreateToolTip(
            string text = "")
        {
            return new ToolTip
            {
                ToolTipTitle = text ?? "",
                BackColor = UIColors.BackgroundMedium,
                ForeColor = UIColors.TextPrimary,
                AutoPopDelay = 10000,
                InitialDelay = 1000,
                ReshowDelay = 300,
                UseAnimation = true,
                UseFading = true
            };
        }
    }
}