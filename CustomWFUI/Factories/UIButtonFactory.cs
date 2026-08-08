using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CustomWFUI.Styles;

namespace CustomWFUI.Factories
{
    internal static class UIButtonFactory
    {
        private static readonly Size DefaultButtonSize = new Size(30, 30);
        private static readonly Size DefaultIconButtonSize = new Size(32, 32);
        private const double DisabledColorFactor = 0.65;

        public static Button CreateStandard(string text = "", string tooltip = "", Size? size = null, bool isIcon = false)
        {
            return CreateStyledButton(text, tooltip, size, isIcon,
                UIColors.BackgroundMedium, UIColors.TextPrimary, UIColors.BorderDark, 1,
                UIColors.BackgroundLighter, UIColors.Primary);
        }

        public static Button CreatePrimary(string text = "", string tooltip = "", Size? size = null, bool isIcon = false)
        {
            return CreateStyledButton(text, tooltip, size, isIcon,
                UIColors.PrimaryDark, UIColors.TextPrimary, UIColors.BorderDark, 0,
                UIColors.Primary, UIColors.PrimaryLight);
        }

        public static Button CreateGreen(string text = "", string tooltip = "", Size? size = null, bool isIcon = false)
        {
            return CreateStyledButton(text, tooltip, size, isIcon,
                UIColors.GreenDark, UIColors.TextPrimary, UIColors.BorderDark, 1,
                UIColors.Green, UIColors.GreenLight);
        }

        public static Button CreateDanger(string text = "", string tooltip = "", Size? size = null, bool isIcon = false)
        {
            return CreateStyledButton(text, tooltip, size, isIcon,
                UIColors.RedDark, UIColors.TextPrimary, UIColors.BorderDark, 1,
                UIColors.Red, UIColors.RedLight);
        }

        public static Button CreateBrowseInFolder(string tooltip = "", Size? size = null, bool isIcon = true)
        {
            // Deliberately a font glyph instead of the OpenFolder PNG icon: a
            // 512x512 raster image downscaled to button size looks blurry and
            // clashes with this button's own yellow, unlike the flat-color
            // glyphs used by the other icon buttons.
            return CreateStyledButton("📁", tooltip, size, isIcon,
                UIColors.Yellow, UIColors.TextPrimary, UIColors.BorderDark, 1,
                UIColors.YellowLight, UIColors.YellowLighter);
        }

        public static Button CreateIconButton(string text, int size = 32)
        {
            Button button = new Button
            {
                Text = text,
                Size = size > 0 ? new Size(size, size) : DefaultIconButtonSize,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(40, 255, 255, 255),
                ForeColor = UIColors.TextPrimary,
                Font = UIFonts.Emoji,
                Cursor = Cursors.Hand,
                Margin = new Padding(4),
                TextAlign = ContentAlignment.MiddleCenter
            };

            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(70, 255, 255, 255);
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(90, 255, 255, 255);

            button.Resize += OnRoundIconButtonResize;
            ApplyRoundRegion(button);

            return button;
        }

        private static Button CreateStyledButton(
            string text,
            string tooltip,
            Size? size,
            bool isIcon,
            Color backColor,
            Color foreColor,
            Color borderColor,
            int borderSize,
            Color mouseOverBackColor,
            Color mouseDownBackColor)
        {
            Button button = new Button
            {
                Text = text ?? "",
                Size = size ?? DefaultButtonSize,
                FlatStyle = FlatStyle.Flat,
                BackColor = backColor,
                ForeColor = foreColor,
                Font = isIcon ? UIFonts.Icon : UIFonts.Normal,
                TabStop = true,
                Cursor = Cursors.Hand,
                Margin = new Padding(0),
                Padding = isIcon ? new Padding(0) : new Padding(6, 0, 6, 0),
                TextAlign = ContentAlignment.MiddleCenter
            };

            button.FlatAppearance.BorderSize = borderSize;
            button.FlatAppearance.BorderColor = borderColor;
            button.FlatAppearance.MouseOverBackColor = mouseOverBackColor;
            button.FlatAppearance.MouseDownBackColor = mouseDownBackColor;

            SetEnabledStyle(button, backColor, foreColor);
            AddToolTip(button, tooltip);

            return button;
        }

        private static void AddToolTip(Button button, string tooltip)
        {
            if (button == null || string.IsNullOrWhiteSpace(tooltip))
                return;

            ToolTip toolTip = UIToolTipFactory.CreateToolTip();
            toolTip.SetToolTip(button, tooltip);

            button.Disposed += delegate
            {
                toolTip.Dispose();
            };
        }

        private static void SetEnabledStyle(Button button, Color enabledBackColor, Color enabledForeColor)
        {
            Color disabledBackColor = Darken(enabledBackColor, DisabledColorFactor);
            Color disabledForeColor = UIColors.TextDisabled;

            ApplyEnabledStyle(button, enabledBackColor, enabledForeColor, disabledBackColor, disabledForeColor);

            button.EnabledChanged += delegate
            {
                ApplyEnabledStyle(button, enabledBackColor, enabledForeColor, disabledBackColor, disabledForeColor);
            };
        }

        private static void ApplyEnabledStyle(Button button, Color enabledBackColor, Color enabledForeColor, Color disabledBackColor, Color disabledForeColor)
        {
            if (button == null)
                return;

            button.BackColor = button.Enabled ? enabledBackColor : disabledBackColor;
            button.ForeColor = button.Enabled ? enabledForeColor : disabledForeColor;
        }

        private static Color Darken(Color color, double factor)
        {
            factor = Math.Max(0, Math.Min(1, factor));

            return Color.FromArgb(
                color.A,
                Math.Max(0, Math.Min(255, (int)(color.R * factor))),
                Math.Max(0, Math.Min(255, (int)(color.G * factor))),
                Math.Max(0, Math.Min(255, (int)(color.B * factor))));
        }

        private static void OnRoundIconButtonResize(object sender, EventArgs e)
        {
            Button button = sender as Button;

            if (button == null)
                return;

            ApplyRoundRegion(button);
        }

        private static void ApplyRoundRegion(Button button)
        {
            if (button == null || button.Width <= 0 || button.Height <= 0)
                return;

            Region oldRegion = button.Region;
            GraphicsPath path = new GraphicsPath();

            try
            {
                path.AddEllipse(0, 0, button.Width, button.Height);
                button.Region = new Region(path);
            }
            finally
            {
                path.Dispose();

                if (oldRegion != null)
                    oldRegion.Dispose();
            }
        }
    }
}