using System;
using System.Drawing;
using System.Windows.Forms;
using CustomWFUI.Styles;

namespace CustomWFUI.Factories
{
    internal static class UIProgressBarFactory
    {
        private const double DisabledColorFactor = 0.65;

        public static ProgressBar CreateStandard()
        {
            ProgressBar progressBar = new BorderedProgressBar
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

        // Like UIButtonFactory/UICheckBoxFactory's SetEnabledStyle - restores
        // whatever ForeColor was live right before Enabled went false, so a
        // caller's manual fill-color override survives the round-trip.
        // Previously ProgressBar had no disabled-state handling at all
        // (confirmed identical pixels enabled vs. disabled via screenshot).
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
                    progressBar.ForeColor = Darken(restoreForeColor, DisabledColorFactor);
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

        // Draws a 1px border around the bar so its extent is still legible
        // even in the rare case a caller's panel happens to also be
        // BackgroundDark - relying on BackColor contrast alone isn't
        // guaranteed the way it is for bordered buttons/textboxes.
        // ProgressBar is natively drawn (WM_PAINT bypasses .NET's owner-draw
        // pipeline, so Paint never fires for it - confirmed by testing),
        // hence painting straight onto the HWND after the base paint instead
        // of overriding OnPaint.
        private sealed class BorderedProgressBar : ProgressBar
        {
            private const int WM_PAINT = 0x000F;

            [System.Runtime.InteropServices.DllImport("uxtheme.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
            private static extern int SetWindowTheme(
                System.IntPtr hWnd,
                string pszSubAppName,
                string pszSubIdList);

            public BorderedProgressBar()
            {
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

                if (m.Msg == WM_PAINT)
                {
                    using (Graphics g = Graphics.FromHwnd(Handle))
                    using (Pen pen = new Pen(UIColors.BorderMedium))
                        g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
                }
            }
        }
    }
}
