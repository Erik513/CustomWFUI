using System;
using System.Drawing;
using System.Windows.Forms;
using CustomWFUI.Styles;

namespace CustomWFUI.Factories
{
    [Obsolete("Use CustomWFUI.UIStyles.Labels instead.", false)]
    public static class UILabelFactory
    {
        public static Label CreateTitle(string text = "")
        {
            return new Label
            {
                Text = text ?? "",
                ForeColor = UIColors.TextPrimary,
                Font = UIFonts.Title,
                BackColor = UIColors.BackgroundDark,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoEllipsis = true,
                AllowDrop = true,
                AutoSize = false,
                UseMnemonic = false
            };
        }

        public static Label CreateNormal(string text = "")
        {
            return new Label
            {
                Text = text ?? "",
                ForeColor = UIColors.TextSecondary,
                Font = UIFonts.Normal,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true,
                AllowDrop = true,
                AutoSize = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                UseMnemonic = false
            };
        }

        public static Label CreateMuted(string text = "")
        {
            return new Label
            {
                Text = text ?? "",
                ForeColor = UIColors.TextMuted,
                Font = UIFonts.Small,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true,
                AllowDrop = true,
                AutoSize = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                UseMnemonic = false
            };
        }
    }
}