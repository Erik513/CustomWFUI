using System;
using System.Drawing;
using System.Windows.Forms;
using CustomWFUI.Styles;

namespace CustomWFUI.Factories
{
    internal static class UIProgressBarFactory
    {
        // Named by color, matching UIButtonFactory's CreateStandard/
        // CreatePrimary/CreateGreen/CreateRed convention - "Transparent" is
        // a uniform suffix for the no-border/blend-in variant of each
        // color, rather than something only the green bar has. Previously
        // this was CreateStandard/CreateTransparent, which meant "Standard"
        // silently meant "green" here while meaning "neutral gray" for
        // buttons - the same word, two different colors depending on which
        // factory you were looking at.
        public static ProgressBar CreateGreen()
        {
            return Create(UIColors.Green, drawBorder: true);
        }

        // No border, and BackColor deliberately left as whatever the caller
        // sets - ProgressBar.BackColor throws on Color.Transparent (there is
        // no real transparency for this control), so the only way to get the
        // "blends into its container" look some callers actually want is to
        // match BackColor to the surrounding panel by hand, e.g.
        // progressBar.BackColor = UIStyles.Colors.BackgroundLight to match a
        // BackgroundLight panel. CreateGreen's opinionated, always-visible
        // track/border stays the default; this is the explicit opt-out.
        public static ProgressBar CreateGreenTransparent()
        {
            return Create(UIColors.Green, drawBorder: false);
        }

        // Same as CreateGreen, but the fill follows the app-wide accent
        // (UIColors.Primary, the same color CreatePrimary buttons use)
        // instead of the fixed green - for progress bars that should read
        // as "this app's own accent", not "success/in-progress" specifically.
        public static ProgressBar CreatePrimary()
        {
            return Create(UIColors.Primary, drawBorder: true);
        }

        // CreatePrimary's accent-following fill, with CreateGreenTransparent's
        // no-border/blend-in look.
        public static ProgressBar CreatePrimaryTransparent()
        {
            return Create(UIColors.Primary, drawBorder: false);
        }

        // Fill color follows Value instead of being fixed: red at Minimum,
        // yellow at the midpoint, green at Maximum, blending smoothly
        // between them rather than snapping - for a "status/health" bar
        // where the color itself communicates good/bad, not just the fill
        // amount. Doesn't take SetEnabledStyle's restore-color path since
        // the color is recomputed fresh on every paint anyway (from Value
        // when enabled, DisabledGray when not) - there's no fixed color to
        // save and restore.
        public static ProgressBar CreateStatus()
        {
            return CreateStatusBar(drawBorder: true);
        }

        // CreateStatus's Value-driven fill, with CreateGreenTransparent's
        // no-border/blend-in look.
        public static ProgressBar CreateStatusTransparent()
        {
            return CreateStatusBar(drawBorder: false);
        }

        private static ProgressBar CreateStatusBar(bool drawBorder)
        {
            return new BorderedProgressBar(drawBorder, useStatusGradient: true)
            {
                Minimum = 0,
                Maximum = 100,
                Style = ProgressBarStyle.Continuous,
                BackColor = UIColors.BackgroundMedium,
                Margin = new Padding(0)
            };
        }

        private static ProgressBar Create(Color foreColor, bool drawBorder)
        {
            ProgressBar progressBar = new BorderedProgressBar(drawBorder)
            {
                Minimum = 0,
                Style = ProgressBarStyle.Continuous,
                ForeColor = foreColor,
                // BackgroundMedium rather than BackgroundLight - a progress
                // bar's track needs to read as a recessed surface, not
                // whatever shade the caller's panel happens to use (several
                // consumers set their panel BackColor to BackgroundLight too,
                // which made the track invisible - the fill looked like it
                // was floating on transparent background). BackgroundDark
                // fixed that but read as near-black, harsher than the rest
                // of the library's dark grays; BackgroundMedium is still
                // reliably a shade below the common panel roles
                // (Light/Lighter/Elevated) without going that dark, and the
                // border (when drawBorder is true) covers the remaining
                // edge case where a panel is BackgroundMedium too. Callers
                // wanting the borderless/blend-in look reassign this
                // themselves - see CreateTransparent's own comment.
                BackColor = UIColors.BackgroundMedium,
                Margin = new Padding(0)
            };

            SetEnabledStyle(progressBar);

            return progressBar;
        }

        // Like UIButtonFactory/UICheckBoxFactory's SetEnabledStyle - restores
        // whatever ForeColor was live right before Enabled went false, so a
        // caller's manual fill-color override survives the round-trip.
        // Previously ProgressBar had no disabled-state handling at all
        // (confirmed identical pixels enabled vs. disabled via screenshot).
        //
        // Disabled fill uses UIColors.DisabledGray - a plain darkened
        // version of whatever the current fill color happened to be
        // (accent green, or a caller's own override) still read as "that
        // same color, just dimmer", not "disabled/inactive", inconsistent
        // with every other disabled control in the library (buttons,
        // CheckBox, ToggleSwitch, Labels all resolve to this same fixed
        // gray). Disabled progress bars are a rare case in practice, but
        // when one does show up it should read as gray like everything
        // else, not a dark shade of its own accent.
        private static void SetEnabledStyle(ProgressBar progressBar)
        {
            Color restoreForeColor = progressBar.ForeColor;

            progressBar.EnabledChanged += delegate
            {
                if (progressBar.Enabled)
                {
                    progressBar.ForeColor = restoreForeColor;
                }
                else
                {
                    restoreForeColor = progressBar.ForeColor;
                    progressBar.ForeColor = UIColors.DisabledGray;
                }
            };
        }

        // Draws a 1px border around the bar so its extent is still legible
        // even in the rare case a caller's panel happens to also be
        // BackgroundMedium - relying on BackColor contrast alone isn't
        // guaranteed the way it is for bordered buttons/textboxes.
        // ProgressBar is natively drawn (WM_PAINT bypasses .NET's owner-draw
        // pipeline, so Paint never fires for it - confirmed by testing),
        // hence painting straight onto the HWND after the base paint instead
        // of overriding OnPaint. drawBorder lets the *Transparent variants
        // skip this entirely for callers who want the bar to blend into
        // its container.
        private sealed class BorderedProgressBar : ProgressBar
        {
            private const int WM_PAINT = 0x000F;

            private readonly bool _drawBorder;
            private readonly bool _useStatusGradient;

            [System.Runtime.InteropServices.DllImport("uxtheme.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
            private static extern int SetWindowTheme(
                System.IntPtr hWnd,
                string pszSubAppName,
                string pszSubIdList);

            [System.Runtime.InteropServices.DllImport("user32.dll")]
            private static extern System.IntPtr GetWindowDC(System.IntPtr hWnd);

            [System.Runtime.InteropServices.DllImport("user32.dll")]
            private static extern int ReleaseDC(System.IntPtr hWnd, System.IntPtr hDC);

            public BorderedProgressBar(bool drawBorder, bool useStatusGradient = false)
            {
                _drawBorder = drawBorder;
                _useStatusGradient = useStatusGradient;

                // With visual styles enabled, the native ProgressBar ignores
                // ForeColor/BackColor entirely and always shows the system
                // green bar. SetWindowTheme("", "") opts just this control
                // out of visual styles so the custom colors actually take
                // effect.
                HandleCreated += (s, e) => SetWindowTheme(Handle, "", "");
            }

            protected override void WndProc(ref Message m)
            {
                // The native fill is drawn by base.WndProc's own WM_PAINT
                // handling, reading whatever ForeColor currently is -
                // there's no ValueChanged event (WinForms' ProgressBar
                // doesn't have one) and Value isn't virtual, so recomputing
                // the color here, right before the native paint actually
                // happens, is simpler than trying to intercept every place
                // Value could change instead.
                if (_useStatusGradient && m.Msg == WM_PAINT)
                {
                    Color statusColor = Enabled
                        ? ComputeStatusColor(Value, Minimum, Maximum)
                        : UIColors.DisabledGray;

                    if (ForeColor != statusColor)
                        ForeColor = statusColor;
                }

                base.WndProc(ref m);

                if (m.Msg != WM_PAINT)
                    return;

                // The classic (unthemed) native ProgressBar can show a 1px
                // 3D sunken-edge frame independent of SetWindowTheme above -
                // confirmed only after a real theme/accent-triggered
                // rebuild (constructed while its parent hierarchy was
                // already visible), never on a fresh construction. This
                // affects BOTH variants. Painting over it via the CLIENT dc
                // (Graphics.FromHwnd/GetDC) had no effect at all, which
                // points at this edge being drawn into the window's
                // NON-CLIENT area specifically - GetWindowDC (unlike GetDC)
                // gives access to the full window surface, non-client area
                // included, so painting there reaches pixels the client dc
                // structurally never could.
                //
                // Erasing on the window dc first and then drawing a bordered
                // variant's own border separately on the client dc (two
                // draws, two device contexts) left a second, visibly
                // different-colored ring of its own: the erase used
                // BackColor (BackgroundMedium, this control's own recessed
                // track shade), one pixel further out than the border drawn
                // after it, which doesn't match the row's actual
                // surrounding background - so instead of one clean border,
                // there were two concentric rings in different colors,
                // which is likely the "line" the user spotted. Drawing the
                // final, correct color (the border color for a bordered
                // variant, or just BackColor for a *Transparent one)
                // directly on the window dc in one single pass avoids that
                // entirely - there's no separate erase step to leave a
                // mismatched trace behind.
                Color edgeColor = _drawBorder ? UIColors.BorderMedium : BackColor;

                System.IntPtr windowDc = GetWindowDC(Handle);

                if (windowDc != System.IntPtr.Zero)
                {
                    try
                    {
                        using (Graphics g = Graphics.FromHdc(windowDc))
                        using (Pen pen = new Pen(edgeColor))
                            g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
                    }
                    finally
                    {
                        ReleaseDC(Handle, windowDc);
                    }
                }
            }

            // Two-segment blend - red to yellow, then yellow to green -
            // rather than a straight red-to-green blend, which would pass
            // through a muddy brown/olive around the midpoint instead of a
            // clear yellow "midway" cue. The breakpoint between the two
            // segments is NOT the midpoint (0.5): green reading as "done"
            // should only dominate near the top of the range, so the
            // red->yellow segment gets the larger share (0-65%) and only
            // the last third blends yellow->green - a flat 50/50 split
            // turned green too early ("es wird zu schnell grün").
            private const double YellowBreakpoint = 0.65;

            private static Color ComputeStatusColor(int value, int minimum, int maximum)
            {
                double range = maximum - minimum;
                double fraction = range > 0 ? (value - minimum) / range : 0;
                fraction = Math.Max(0, Math.Min(1, fraction));

                if (fraction <= YellowBreakpoint)
                    return Lerp(UIColors.Red, UIColors.Yellow, fraction / YellowBreakpoint);

                return Lerp(UIColors.Yellow, UIColors.Green, (fraction - YellowBreakpoint) / (1 - YellowBreakpoint));
            }

            private static Color Lerp(Color from, Color to, double t)
            {
                t = Math.Max(0, Math.Min(1, t));

                return Color.FromArgb(
                    (int)Math.Round(from.R + (to.R - from.R) * t),
                    (int)Math.Round(from.G + (to.G - from.G) * t),
                    (int)Math.Round(from.B + (to.B - from.B) * t));
            }
        }
    }
}
