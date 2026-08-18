using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using CustomWFUI.Styles;

namespace CustomWFUI.Factories
{
    internal static class UIButtonFactory
    {
        private static readonly Size DefaultButtonSize = new Size(30, 30);
        private static readonly Size DefaultIconButtonSize = new Size(32, 32);
        private const double DisabledColorFactor = 0.65;

        // Tracks the ToolTip component each button got from AddToolTip, so
        // UpdateTooltip can change its text later (e.g. on a language
        // switch) instead of only being able to set it once at creation.
        private static readonly ConditionalWeakTable<Button, ToolTip> _tooltips =
            new ConditionalWeakTable<Button, ToolTip>();

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
            SetPressedForeColor(button, foreColor);
            AddToolTip(button, tooltip);

            return button;
        }

        // Every button's mouse-down background (the *Light/*Lighter colors
        // passed as mouseDownBackColor in CreateStyledButton) is a
        // lighter/brighter shade than its idle one - so pressed text
        // always goes dark, unconditionally, regardless of hue.
        // FlatAppearance only lets a button swap its BACKGROUND per mouse
        // state, not its text/icon color, so this has to be done by hand.
        //
        // MouseLeave fires on every plain hover-then-move-away too, not
        // just after a press - so this must only touch ForeColor if a
        // press is actually in progress (isPressed), and must restore
        // whatever ForeColor was live right before the press (not the
        // color captured at construction time), since callers are free
        // to recolor a button after creation (e.g. DealOrNoDeal's price
        // buttons set ForeColor = Black on top of this factory's default).
        private static void SetPressedForeColor(Button button, Color idleForeColor)
        {
            Color pressedForeColor = UIColors.DarkForeColor;
            bool isPressed = false;
            Color restoreForeColor = idleForeColor;

            button.MouseDown += delegate
            {
                if (!button.Enabled)
                    return;

                restoreForeColor = button.ForeColor;
                isPressed = true;

                if (restoreForeColor != pressedForeColor)
                    button.ForeColor = pressedForeColor;
            };

            button.MouseUp += delegate
            {
                if (!isPressed)
                    return;

                isPressed = false;
                button.ForeColor = restoreForeColor;
            };

            button.MouseLeave += delegate
            {
                if (!isPressed)
                    return;

                isPressed = false;
                button.ForeColor = restoreForeColor;
            };
        }

        private static void AddToolTip(Button button, string tooltip)
        {
            if (button == null || string.IsNullOrWhiteSpace(tooltip))
                return;

            ToolTip toolTip = UIToolTipFactory.CreateToolTip();
            toolTip.SetToolTip(button, tooltip);
            _tooltips.Add(button, toolTip);

            button.Disposed += delegate
            {
                toolTip.Dispose();
            };
        }

        /// <summary>
        /// Changes an already-created button's tooltip text - e.g. to
        /// re-translate it after a language switch. No-op if the button was
        /// created without a tooltip in the first place.
        /// </summary>
        public static void UpdateTooltip(Button button, string tooltip)
        {
            if (button == null)
                return;

            ToolTip toolTip;
            if (_tooltips.TryGetValue(button, out toolTip))
                toolTip.SetToolTip(button, tooltip ?? "");
        }

        // Like SetPressedForeColor above: restores whatever BackColor/ForeColor
        // was live right before the button got disabled, not the color
        // captured here at construction time - so a caller's manual override
        // (button.BackColor = ... after creation) survives an Enabled
        // round-trip instead of silently snapping back to the factory
        // default the next time the button re-enables.
        private static void SetEnabledStyle(Button button, Color enabledBackColor, Color enabledForeColor)
        {
            Color disabledBackColor = Darken(enabledBackColor, DisabledColorFactor);
            Color disabledForeColor = UIColors.TextDisabled;

            Color restoreBackColor = enabledBackColor;
            Color restoreForeColor = enabledForeColor;

            button.EnabledChanged += delegate
            {
                if (button.Enabled)
                {
                    button.BackColor = restoreBackColor;
                    button.ForeColor = restoreForeColor;
                }
                else
                {
                    restoreBackColor = button.BackColor;
                    restoreForeColor = button.ForeColor;
                    button.BackColor = disabledBackColor;
                    button.ForeColor = disabledForeColor;
                }
            };
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