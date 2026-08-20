using System;
using System.Drawing;
using System.Windows.Forms;
using CustomWFUI.Styles;

namespace CustomWFUI.Factories
{
    internal static class UIProgressBarFactory
    {
        public static ProgressBar CreateStandard()
        {
            ProgressBar progressBar = new BorderedProgressBar(drawBorder: true)
            {
                Minimum = 0,
                Style = ProgressBarStyle.Continuous,
                ForeColor = UIColors.Green,
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
                // border below covers the remaining edge case where a panel
                // is BackgroundMedium too.
                BackColor = UIColors.BackgroundMedium,
                Margin = new Padding(0)
            };

            SetEnabledStyle(progressBar);

            return progressBar;
        }

        // No border, and BackColor deliberately left as whatever the caller
        // sets - ProgressBar.BackColor throws on Color.Transparent (there is
        // no real transparency for this control), so the only way to get the
        // "blends into its container" look some callers actually want is to
        // match BackColor to the surrounding panel by hand, e.g.
        // progressBar.BackColor = UIStyles.Colors.BackgroundLight to match a
        // BackgroundLight panel. CreateStandard's opinionated, always-visible
        // track/border stays the default; this is the explicit opt-out.
        public static ProgressBar CreateTransparent()
        {
            ProgressBar progressBar = new BorderedProgressBar(drawBorder: false)
            {
                Minimum = 0,
                Style = ProgressBarStyle.Continuous,
                ForeColor = UIColors.Green,
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
        // of overriding OnPaint. drawBorder lets CreateTransparent skip this
        // entirely for callers who want the bar to blend into its container.
        private sealed class BorderedProgressBar : ProgressBar
        {
            private const int WM_PAINT = 0x000F;

            private readonly bool _drawBorder;

            [System.Runtime.InteropServices.DllImport("uxtheme.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
            private static extern int SetWindowTheme(
                System.IntPtr hWnd,
                string pszSubAppName,
                string pszSubIdList);

            [System.Runtime.InteropServices.DllImport("user32.dll")]
            private static extern System.IntPtr GetWindowDC(System.IntPtr hWnd);

            [System.Runtime.InteropServices.DllImport("user32.dll")]
            private static extern int ReleaseDC(System.IntPtr hWnd, System.IntPtr hDC);

            public BorderedProgressBar(bool drawBorder)
            {
                _drawBorder = drawBorder;

                // With visual styles enabled, the native ProgressBar ignores
                // ForeColor/BackColor entirely and always shows the system
                // green bar. SetWindowTheme("", "") opts just this control
                // out of visual styles so the custom colors actually take
                // effect.
                HandleCreated += (s, e) => SetWindowTheme(Handle, "", "");
            }

            protected override void WndProc(ref Message m)
            {
                base.WndProc(ref m);

                if (m.Msg != WM_PAINT)
                    return;

                // The classic (unthemed) native ProgressBar can show a 1px
                // 3D sunken-edge frame independent of SetWindowTheme above -
                // confirmed only after a real theme/accent-triggered
                // rebuild (constructed while its parent hierarchy was
                // already visible), never on a fresh construction. This
                // affects BOTH variants - CreateStandard just masked it
                // before, since our own BorderMedium border (drawn below)
                // sits close enough in color/position to blend with the
                // native edge instead of standing out as a separate line
                // the way it did against CreateTransparent's plain
                // BackColor fill. Painting over it via the CLIENT dc
                // (Graphics.FromHwnd/GetDC) had no effect at all, which
                // points at this edge being drawn into the window's
                // NON-CLIENT area specifically. GetWindowDC (unlike GetDC)
                // gives access to the full window surface, non-client area
                // included, so painting the outer ring there reaches pixels
                // the client dc structurally never could - done first and
                // unconditionally, so CreateStandard's own border (drawn
                // after, on the client dc) always ends up on a clean edge
                // instead of layered on top of a stray native one.
                System.IntPtr windowDc = GetWindowDC(Handle);

                if (windowDc != System.IntPtr.Zero)
                {
                    try
                    {
                        using (Graphics g = Graphics.FromHdc(windowDc))
                        using (Pen pen = new Pen(BackColor))
                            g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
                    }
                    finally
                    {
                        ReleaseDC(Handle, windowDc);
                    }
                }

                if (_drawBorder)
                {
                    using (Graphics g = Graphics.FromHwnd(Handle))
                    using (Pen pen = new Pen(UIColors.BorderMedium))
                        g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
                }
            }
        }
    }
}
