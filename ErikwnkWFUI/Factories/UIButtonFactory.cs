using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Factories
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
                UIColors.PrimaryDark, UIColors.GetContrastingForeColor(UIColors.PrimaryDark), UIColors.BorderDark, 0,
                UIColors.Primary, UIColors.PrimaryLight);
        }

        public static Button CreateGreen(string text = "", string tooltip = "", Size? size = null, bool isIcon = false)
        {
            return CreateStyledButton(text, tooltip, size, isIcon,
                UIColors.GreenDark, UIColors.GetContrastingForeColor(UIColors.GreenDark), UIColors.BorderDark, 1,
                UIColors.Green, UIColors.GreenLight);
        }

        public static Button CreateRed(string text = "", string tooltip = "", Size? size = null, bool isIcon = false)
        {
            return CreateStyledButton(text, tooltip, size, isIcon,
                UIColors.RedDark, UIColors.GetContrastingForeColor(UIColors.RedDark), UIColors.BorderDark, 1,
                UIColors.Red, UIColors.RedLight);
        }

        public static Button CreateBrowse(string tooltip = "", Size? size = null, bool isIcon = true)
        {
            // Deliberately a font glyph instead of the OpenFolder PNG icon: a
            // 512x512 raster image downscaled to button size looks blurry and
            // clashes with this button's own yellow, unlike the flat-color
            // glyphs used by the other icon buttons.
            //
            // fixedForeColor: true - white regardless of computed contrast.
            // Yellow's background is itself a fixed, non-computed look (this
            // button doesn't follow the theme or accent), so the glyph color
            // should be equally fixed rather than flipping to black just
            // because Yellow happens to sit above the light/dark text
            // threshold - a deliberate, requested exception, not an
            // oversight the way the pre-fix "always black" button/disabled
            // text bugs earlier this session were.
            return CreateStyledButton("📁", tooltip, size, isIcon,
                UIColors.Yellow, UIColors.White, UIColors.BorderDark, 1,
                UIColors.YellowLight, UIColors.YellowLighter,
                fixedForeColor: true);
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
            Color mouseDownBackColor,
            bool fixedForeColor = false)
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

            SetEnabledStyle(button, backColor, foreColor, fixedForeColor);
            SetPressedForeColor(button, foreColor, mouseDownBackColor, fixedForeColor);
            AddToolTip(button, tooltip);

            button.Paint += OnButtonPaint;

            return button;
        }

        // WinForms' built-in disabled-Button rendering ignores ForeColor
        // entirely - confirmed by reading it back as exactly the light color
        // SetEnabledStyle assigns, while the actually-rendered pixels were a
        // barely-readable dark-on-dark blend (an embossed light/dark pair
        // the framework derives from BackColor, not ForeColor). The Paint
        // event fires after the button's own internal painting finishes, so
        // repainting the text here overrides it with the color we actually
        // computed. Only touches disabled buttons - enabled ones render
        // correctly on their own.
        private static void OnButtonPaint(object sender, PaintEventArgs e)
        {
            Button button = (Button)sender;

            if (button.Enabled || string.IsNullOrEmpty(button.Text))
                return;

            Rectangle textArea = Rectangle.Inflate(button.ClientRectangle, -2, -2);

            using (SolidBrush backBrush = new SolidBrush(button.BackColor))
                e.Graphics.FillRectangle(backBrush, textArea);

            TextRenderer.DrawText(
                e.Graphics,
                button.Text,
                button.Font,
                button.ClientRectangle,
                button.ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        // FlatAppearance only lets a button swap its BACKGROUND per mouse
        // state, not its text/icon color, so pressed text has to be
        // recolored by hand to stay readable against mouseDownBackColor -
        // that's NOT always a light shade of the idle color (e.g.
        // CreateStandard's press color is the app's accent, which is a dark
        // blue by default), so this computes the correct contrast for that
        // specific background rather than assuming dark text is always
        // right.
        //
        // MouseLeave fires on every plain hover-then-move-away too, not
        // just after a press - so this must only touch ForeColor if a
        // press is actually in progress (isPressed), and must restore
        // whatever ForeColor was live right before the press (not the
        // color captured at construction time), since callers are free
        // to recolor a button after creation (e.g. DealOrNoDeal's price
        // buttons set ForeColor = Black on top of this factory's default).
        private static void SetPressedForeColor(Button button, Color idleForeColor, Color pressedBackColor, bool fixedForeColor)
        {
            Color pressedForeColor = fixedForeColor
                ? idleForeColor
                : UIColors.GetContrastingForeColor(pressedBackColor);
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
        private static void SetEnabledStyle(Button button, Color enabledBackColor, Color enabledForeColor, bool fixedForeColor)
        {
            Color disabledBackColor = Darken(enabledBackColor, DisabledColorFactor);

            // Computed per button rather than a single fixed UIColors.TextDisabled
            // gray - that read fine against most variants' darkened background
            // by coincidence, but CreateBrowse's (Yellow darkened by
            // DisabledColorFactor is still a fairly bright olive) landed at a
            // contrast ratio of ~1.0 against it - i.e. functionally invisible.
            // Blended 35% toward the disabled background rather than used at
            // full brightness - GetContrastingForeColor alone picks the same
            // white/near-black constant an enabled button would also land on
            // (darkening the background rarely flips which side of the
            // threshold it's on), so disabled text read exactly as crisp/
            // bright as enabled text once OnButtonPaint fixed its visibility -
            // "looks the same as enabled". The blend keeps it clearly
            // readable (it's blending toward an already-darkened background,
            // not toward black) while actually looking muted.
            // fixedForeColor buttons (CreateBrowse) skip the contrast
            // computation - their idle fore color is a fixed part of their
            // look, not something GetContrastingForeColor should be picking
            // for them - but still get the same muting blend applied to it
            // for disabled. Every control's disabled state should read as
            // visibly grayed out regardless of what's otherwise fixed about
            // its look.
            Color disabledForeColor = fixedForeColor
                ? BlendTowardColor(enabledForeColor, disabledBackColor, 0.35)
                : BlendTowardColor(UIColors.GetContrastingForeColor(disabledBackColor), disabledBackColor, 0.35);

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

        private static Color BlendTowardColor(Color color, Color target, double amount)
        {
            amount = Math.Max(0, Math.Min(1, amount));

            return Color.FromArgb(
                color.A,
                color.R + (int)((target.R - color.R) * amount),
                color.G + (int)((target.G - color.G) * amount),
                color.B + (int)((target.B - color.B) * amount));
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