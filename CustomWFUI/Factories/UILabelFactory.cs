using System.Drawing;
using System.Windows.Forms;
using CustomWFUI.Styles;

namespace CustomWFUI.Factories
{
    internal static class UILabelFactory
    {
        public static Label CreateTitle(string text = "")
        {
            return new OwnerDrawLabel(UIColors.TextPrimary)
            {
                Text = text ?? "",
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
            return new OwnerDrawLabel(UIColors.TextSecondary)
            {
                Text = text ?? "",
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
            return new OwnerDrawLabel(UIColors.TextMuted)
            {
                Text = text ?? "",
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

        // Plain Label ignores an explicit ForeColor once Enabled=false -
        // confirmed directly (set ForeColor to pure red, disabled text still
        // rendered its own gray) - it substitutes a fixed system color the
        // same way ButtonBase/CheckBox's native disabled rendering did
        // before those were made owner-drawn. Rather than leave every
        // disabled label a different, uncontrolled shade, this always
        // resolves to the same UIColors.DisabledGray used everywhere else
        // (buttons/CheckBox/ToggleSwitch), so "disabled" reads identically
        // across every control type and both themes.
        private sealed class OwnerDrawLabel : Label
        {
            private readonly Color _enabledForeColor;

            public OwnerDrawLabel(Color enabledForeColor)
            {
                _enabledForeColor = enabledForeColor;

                SetStyle(
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.UserPaint,
                    true);
            }

            protected override void OnPaintBackground(PaintEventArgs pevent)
            {
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                Graphics g = e.Graphics;

                // BackColor.A == 0 (Color.Transparent) means "blend into
                // whatever's actually behind me" - tried first as "just
                // don't erase, let the parent's already-painted pixels show
                // through", which looked right for a static layout but left
                // real stale-pixel ghosting in a FlowLayoutPanel (the
                // toolbar's Theme:/Accent: labels showed leftover pixels
                // from a sibling Button that used to occupy that spot
                // before a re-layout moved things around - confirmed via a
                // live screenshot). Erasing with the nearest OPAQUE
                // ancestor's BackColor instead - same technique
                // UICheckBoxFactory uses, but walking up more than one level
                // since a direct Parent.BackColor can itself be
                // Color.Transparent (e.g. UIFlowLayoutPanelFactory's
                // FlowLayoutPanel, the toolbar's actual direct parent) -
                // checking only one level up silently fell back to the same
                // "erase with a transparent brush" no-op as before.
                using (SolidBrush eraseBrush = new SolidBrush(GetEffectiveBackColor()))
                    g.FillRectangle(eraseBrush, ClientRectangle);

                Color textColor = Enabled ? _enabledForeColor : UIColors.DisabledGray;

                TextFormatFlags flags = ToTextFormatFlags(TextAlign);

                if (AutoEllipsis)
                    flags |= TextFormatFlags.EndEllipsis;

                Rectangle textArea = new Rectangle(
                    Padding.Left,
                    Padding.Top,
                    Width - Padding.Horizontal,
                    Height - Padding.Vertical);

                TextRenderer.DrawText(g, Text, Font, textArea, textColor, flags);

                // Deliberately no base.OnPaint(e) call - see the class
                // comment above.
            }

            private Color GetEffectiveBackColor()
            {
                if (BackColor.A > 0)
                    return BackColor;

                for (Control ancestor = Parent; ancestor != null; ancestor = ancestor.Parent)
                {
                    if (ancestor.BackColor.A > 0)
                        return ancestor.BackColor;
                }

                return UIColors.BackgroundMedium;
            }

            private static TextFormatFlags ToTextFormatFlags(ContentAlignment alignment)
            {
                TextFormatFlags horizontal;
                TextFormatFlags vertical;

                switch (alignment)
                {
                    case ContentAlignment.TopLeft:
                    case ContentAlignment.MiddleLeft:
                    case ContentAlignment.BottomLeft:
                        horizontal = TextFormatFlags.Left;
                        break;
                    case ContentAlignment.TopCenter:
                    case ContentAlignment.MiddleCenter:
                    case ContentAlignment.BottomCenter:
                        horizontal = TextFormatFlags.HorizontalCenter;
                        break;
                    default:
                        horizontal = TextFormatFlags.Right;
                        break;
                }

                switch (alignment)
                {
                    case ContentAlignment.TopLeft:
                    case ContentAlignment.TopCenter:
                    case ContentAlignment.TopRight:
                        vertical = TextFormatFlags.Top;
                        break;
                    case ContentAlignment.BottomLeft:
                    case ContentAlignment.BottomCenter:
                    case ContentAlignment.BottomRight:
                        vertical = TextFormatFlags.Bottom;
                        break;
                    default:
                        vertical = TextFormatFlags.VerticalCenter;
                        break;
                }

                return horizontal | vertical;
            }
        }
    }
}
