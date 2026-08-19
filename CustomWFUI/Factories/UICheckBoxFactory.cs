using System.Drawing;
using System.Drawing.Drawing2D;
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
            text = text ?? "";
            Font font = UIFonts.Normal;

            CheckBox checkBox = new OwnerDrawCheckBox
            {
                Text = text,
                Checked = checkedState,
                Font = font,
                FlatStyle = FlatStyle.Flat,
                AutoSize = false,
                Size = OwnerDrawCheckBox.ComputePreferredSize(text, font)
            };

            return checkBox;
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

        // Fully owner-drawn (box, checkmark, and label) instead of trying to
        // recolor pieces of the native glyph - four earlier attempts each
        // fixed one problem and caused another: ForeColor drives the native
        // checkmark's color too, so tuning it for label readability made the
        // checkmark invisible against its own white box (and vice versa);
        // the native glyph doesn't dim on Enabled at all; erasing only a
        // guessed-width text region left a sliver of the native glyph/text
        // peeking out at the boundary; and even a full-rectangle erase in a
        // plain CheckBox's Paint EVENT still let native ButtonBase text
        // render underneath/after it in specific layouts (confirmed via a
        // StyledPropertyTable row: a disabled or otherwise-repainted
        // checkbox showed its label doubled, faintly offset - ButtonBase's
        // own OnPaint drawing its default glyph/text using its own layout,
        // with our Paint-event handler's drawing on top not fully hiding it
        // depending on repaint timing). A real subclass that overrides
        // OnPaint and never calls base.OnPaint prevents ButtonBase's native
        // rendering from running at all, which is the only approach that
        // eliminated the doubling in every case tested.
        private sealed class OwnerDrawCheckBox : CheckBox
        {
            private const int BoxSize = 16;
            private const int BoxLeftMargin = 2;
            private const int TextLeftPadding = 6;
            private const int GlyphColumnWidth = BoxLeftMargin + BoxSize + TextLeftPadding;

            public OwnerDrawCheckBox()
            {
                // Deliberately NOT using ControlStyles.OptimizedDoubleBuffer/
                // DoubleBuffered=true - both route through WinForms'
                // process-wide BufferedGraphicsManager, which reuses ONE
                // shared backing bitmap across every double-buffered control
                // for efficiency. With several of these checkboxes (and
                // other double-buffered controls, e.g. ToggleSwitch) all
                // painting during the same layout/paint burst, that shared
                // buffer got reused before being fully overwritten, and a
                // neighboring control's leftover pixels (its text, or in one
                // observed case a ToggleSwitch's blue knob) showed up baked
                // into a checkbox's painted area on real screen captures
                // (CopyFromScreen and PrintWindow both showed it - not a
                // capture-tooling artifact). OnPaint below manages its own
                // private per-call Bitmap instead, so no buffer is ever
                // shared with any other control.
                SetStyle(
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.UserPaint,
                    true);
            }

            // Native CheckBox.AutoSize defaults to true and measures a
            // preferred size for the SYSTEM checkbox glyph, which knows
            // nothing about our custom 16px box/padding layout. Left
            // enabled, this caused the real (already laid-out, resized,
            // relaid-out) StyledPropertyTable instance to settle on
            // different bounds than what a single fresh construction+paint
            // produces - live screen captures (CopyFromScreen AND
            // PrintWindow, i.e. not a capture-tooling artifact) showed
            // leftover text fragments from neighboring rows/cells baked
            // into the checkbox's painted area, while a throwaway
            // freshly-constructed-then-immediately-DrawToBitmap probe never
            // reproduced it - consistent with a layout-history-dependent
            // bug, not a per-paint drawing bug. The factory now disables
            // AutoSize and assigns an explicitly computed Size instead, so
            // there is exactly one deterministic set of bounds regardless
            // of how many real layout passes the control goes through.
            internal static Size ComputePreferredSize(string text, Font font)
            {
                bool hasText = !string.IsNullOrEmpty(text);

                int width = hasText
                    ? GlyphColumnWidth + TextRenderer.MeasureText(text, font).Width + 4
                    : BoxSize + BoxLeftMargin * 2;

                int height = System.Math.Max(BoxSize + 8, font.Height + 8);

                return new Size(width, height);
            }

            // BackColor=Transparent (the previous approach) makes WinForms
            // route background painting through its "ask the parent to
            // render what's behind me" fake-transparency path - inside a
            // nested TableLayoutPanel (StyledPropertyTable's editor cells)
            // that path picked up stale/sibling content (a neighboring
            // checkbox's or the row label's text), showing through as
            // ghosted text on real screen paints. DrawToBitmap didn't
            // reproduce it (WM_PRINT paints each control directly, bypassing
            // that compositing path), which is why it looked fine there.
            // Overriding this as a no-op guarantees our own OnPaint (which
            // already does a full manual erase to the parent's BackColor)
            // is the only thing that ever paints this control's background.
            protected override void OnPaintBackground(PaintEventArgs pevent)
            {
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                if (Width <= 0 || Height <= 0)
                    return;

                using (Bitmap buffer = new Bitmap(Width, Height))
                {
                    using (Graphics g = Graphics.FromImage(buffer))
                        PaintTo(g);

                    e.Graphics.DrawImageUnscaled(buffer, 0, 0);
                }
            }

            private void PaintTo(Graphics g)
            {
                Color parentBackColor = Parent != null
                    ? Parent.BackColor
                    : UIColors.BackgroundMedium;

                using (SolidBrush eraseBrush = new SolidBrush(parentBackColor))
                    g.FillRectangle(eraseBrush, new Rectangle(0, 0, Width, Height));

                g.SmoothingMode = SmoothingMode.AntiAlias;

                bool hasText = !string.IsNullOrEmpty(Text);

                int boxX = hasText
                    ? BoxLeftMargin
                    : (Width - BoxSize) / 2;
                int boxY = (Height - BoxSize) / 2;
                Rectangle boxRect = new Rectangle(boxX, boxY, BoxSize, BoxSize);

                // Filled with the accent when checked (readable regardless of
                // theme/accent - no more fighting over one color that has to
                // work as both a checkmark stroke and a label color), outline
                // only when unchecked. DisabledGray substitutes for both
                // border and fill when disabled - a fixed mid-gray (unlike
                // TextDisabled, which is a theme role that differs between
                // Dark/Light), so a disabled checkbox looks identical in both
                // themes, the same way a disabled ToggleSwitch/button does.
                Color borderColor = Enabled ? UIColors.BorderMedium : UIColors.DisabledGray;
                Color fillColor = Checked
                    ? (Enabled ? UIColors.Primary : UIColors.DisabledGray)
                    : Color.Transparent;

                using (GraphicsPath boxPath = CreateRoundedRectanglePath(boxRect, 3))
                {
                    if (Checked)
                    {
                        using (SolidBrush fillBrush = new SolidBrush(fillColor))
                            g.FillPath(fillBrush, boxPath);
                    }

                    using (Pen borderPen = new Pen(borderColor, 1.5f))
                        g.DrawPath(borderPen, boxPath);
                }

                if (Checked)
                {
                    Color checkColor = UIColors.GetContrastingForeColor(fillColor);
                    DrawCheckmark(g, boxRect, checkColor);
                }

                if (hasText)
                {
                    Rectangle textArea = new Rectangle(
                        GlyphColumnWidth, 0, Width - GlyphColumnWidth, Height);

                    Color textColor = Enabled ? UIColors.TextPrimary : UIColors.DisabledGray;

                    TextRenderer.DrawText(
                        g,
                        Text,
                        Font,
                        textArea,
                        textColor,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                }

                // Deliberately no base.OnPaint(e) call - see the class
                // comment above.
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
        }
    }
}
