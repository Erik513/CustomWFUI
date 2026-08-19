using System.Drawing;
using System.Windows.Forms;
using CustomWFUI.Styles;

namespace CustomWFUI.Factories
{
    internal static class UICheckBoxFactory
    {
        private const int GlyphColumnWidth = 22;

        public static CheckBox CreateStandard(
            string text = "",
            bool checkedState = true)
        {
            CheckBox checkBox = new CheckBox
            {
                Text = text ?? "",
                Checked = checkedState,
                // ForeColor drives the checkmark glyph's color too (confirmed
                // by testing - a light, label-friendly ForeColor rendered the
                // check nearly invisible against the glyph's own white box).
                // PrimaryDark is the one color that has to work against that
                // fixed white box regardless of theme, so the label text is
                // drawn separately (see OnCheckBoxPaint) rather than sharing
                // this property with the glyph.
                ForeColor = UIColors.PrimaryDark,
                BackColor = Color.Transparent,
                Font = UIFonts.Normal,
                FlatStyle = FlatStyle.Flat
            };

            SetEnabledStyle(checkBox);

            return checkBox;
        }

        // Restores whatever the label's own color should be (not the glyph's
        // ForeColor, which OnCheckBoxPaint below manages) on an Enabled
        // round-trip - same pattern as UIButtonFactory's SetEnabledStyle.
        private static void SetEnabledStyle(CheckBox checkBox)
        {
            checkBox.Paint += OnCheckBoxPaint;
        }

        // Two unrelated problems, one Paint handler:
        //
        // 1. The native checkmark glyph is drawn using ForeColor, which is
        // fixed to PrimaryDark above so it stays visible against the glyph's
        // own white box - but that leaves nothing controlling the LABEL's
        // color, so it's redrawn here instead: erase whatever the native
        // paint drew (fill with the parent's background - CheckBox itself is
        // BackColor=Transparent) and draw fresh text in TextPrimary/
        // TextDisabled. This is also how disabled-state text dimming works
        // now; the native FlatStyle CheckBox doesn't dim text on its own
        // (confirmed by comparing enabled/disabled renders pixel-for-pixel).
        //
        // 2. The glyph itself (box + checkmark) doesn't read Enabled at all
        // either - a disabled checked checkbox looked exactly as vivid as an
        // enabled one. A translucent scrim over the glyph's column (always
        // flush left under CheckAlign = MiddleLeft, regardless of DPI/font)
        // dims it without needing the glyph's exact native bounds.
        private static void OnCheckBoxPaint(object sender, PaintEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;

            if (!string.IsNullOrEmpty(checkBox.Text))
            {
                Rectangle textArea = new Rectangle(
                    GlyphColumnWidth, 0, checkBox.Width - GlyphColumnWidth, checkBox.Height);

                Color parentBackColor = checkBox.Parent != null
                    ? checkBox.Parent.BackColor
                    : UIColors.BackgroundMedium;

                using (SolidBrush eraseBrush = new SolidBrush(parentBackColor))
                    e.Graphics.FillRectangle(eraseBrush, textArea);

                Color textColor = checkBox.Enabled
                    ? UIColors.TextPrimary
                    : UIColors.TextDisabled;

                TextRenderer.DrawText(
                    e.Graphics,
                    checkBox.Text,
                    checkBox.Font,
                    textArea,
                    textColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            }

            if (!checkBox.Enabled)
            {
                Rectangle glyphArea = new Rectangle(0, 0, GlyphColumnWidth, checkBox.Height);

                using (SolidBrush scrim = new SolidBrush(Color.FromArgb(150, 0, 0, 0)))
                    e.Graphics.FillRectangle(scrim, glyphArea);
            }
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