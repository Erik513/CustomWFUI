using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CustomWFUI.Styles;

namespace CustomWFUI.Factories
{
    internal static class UICheckBoxFactory
    {
        private const int BoxSize = 16;
        private const int BoxLeftMargin = 2;
        private const int TextLeftPadding = 6;
        private const int GlyphColumnWidth = BoxLeftMargin + BoxSize + TextLeftPadding;

        public static CheckBox CreateStandard(
            string text = "",
            bool checkedState = true)
        {
            CheckBox checkBox = new CheckBox
            {
                Text = text ?? "",
                Checked = checkedState,
                ForeColor = UIColors.TextPrimary,
                BackColor = Color.Transparent,
                Font = UIFonts.Normal,
                FlatStyle = FlatStyle.Flat
            };

            // Fully owner-drawn (box, checkmark, and label) instead of
            // trying to recolor pieces of the native glyph - three earlier
            // attempts each fixed one problem and caused another:
            // ForeColor drives the native checkmark's color too, so tuning
            // it for label readability made the checkmark invisible against
            // its own white box (and vice versa); the native glyph doesn't
            // dim on Enabled at all; and erasing only a guessed-width text
            // region left a sliver of the native glyph/text peeking out at
            // the boundary (the reported "verbuggt" stray-character look).
            // Painting everything ourselves sidesteps needing to know any
            // of the native control's internal layout at all.
            checkBox.Paint += OnCheckBoxPaint;
            checkBox.EnabledChanged += delegate { checkBox.Invalidate(); };
            checkBox.CheckedChanged += delegate { checkBox.Invalidate(); };

            return checkBox;
        }

        private static void OnCheckBoxPaint(object sender, PaintEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Color parentBackColor = checkBox.Parent != null
                ? checkBox.Parent.BackColor
                : UIColors.BackgroundMedium;

            using (SolidBrush eraseBrush = new SolidBrush(parentBackColor))
                g.FillRectangle(eraseBrush, checkBox.ClientRectangle);

            bool enabled = checkBox.Enabled;
            bool isChecked = checkBox.Checked;
            bool hasText = !string.IsNullOrEmpty(checkBox.Text);

            int boxX = hasText
                ? BoxLeftMargin
                : (checkBox.Width - BoxSize) / 2;
            int boxY = (checkBox.Height - BoxSize) / 2;
            Rectangle boxRect = new Rectangle(boxX, boxY, BoxSize, BoxSize);

            // Filled with the accent when checked (readable regardless of
            // theme/accent - no more fighting over one color that has to
            // work as both a checkmark stroke and a label color), outline
            // only when unchecked. TextDisabled substitutes for both border
            // and fill when disabled - it's a mid-gray in both themes
            // (never collapses to near-white/near-black the way BackgroundDark
            // or BorderDark can), so a disabled checkbox reads as muted
            // against either theme's page background instead of disappearing
            // into it.
            Color borderColor = enabled ? UIColors.BorderMedium : UIColors.TextDisabled;
            Color fillColor = isChecked
                ? (enabled ? UIColors.Primary : UIColors.TextDisabled)
                : Color.Transparent;

            using (GraphicsPath boxPath = CreateRoundedRectanglePath(boxRect, 3))
            {
                if (isChecked)
                {
                    using (SolidBrush fillBrush = new SolidBrush(fillColor))
                        g.FillPath(fillBrush, boxPath);
                }

                using (Pen borderPen = new Pen(borderColor, 1.5f))
                    g.DrawPath(borderPen, boxPath);
            }

            if (isChecked)
            {
                Color checkColor = UIColors.GetContrastingForeColor(fillColor);
                DrawCheckmark(g, boxRect, checkColor);
            }

            if (hasText)
            {
                Rectangle textArea = new Rectangle(
                    GlyphColumnWidth, 0, checkBox.Width - GlyphColumnWidth, checkBox.Height);

                Color textColor = enabled ? UIColors.TextPrimary : UIColors.TextDisabled;

                TextRenderer.DrawText(
                    g,
                    checkBox.Text,
                    checkBox.Font,
                    textArea,
                    textColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            }
        }

        private static void DrawCheckmark(Graphics g, Rectangle box, Color color)
        {
            Point p1 = new Point(box.X + (int)(box.Width * 0.22), box.Y + (int)(box.Height * 0.52));
            Point p2 = new Point(box.X + (int)(box.Width * 0.42), box.Y + (int)(box.Height * 0.74));
            Point p3 = new Point(box.X + (int)(box.Width * 0.80), box.Y + (int)(box.Height * 0.26));

            using (Pen pen = new Pen(color, 2f))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;

                g.DrawLines(pen, new[] { p1, p2, p3 });
            }
        }

        private static GraphicsPath CreateRoundedRectanglePath(Rectangle rect, int radius)
        {
            int diameter = radius * 2;
            GraphicsPath path = new GraphicsPath();

            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();

            return path;
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
