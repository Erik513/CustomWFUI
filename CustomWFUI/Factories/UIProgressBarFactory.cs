using System.Drawing;
using System.Windows.Forms;
using CustomWFUI.Styles;

namespace CustomWFUI.Factories
{
    internal static class UIProgressBarFactory
    {
        public static ProgressBar CreateStandard()
        {
            return new BorderedProgressBar
            {
                Minimum = 0,
                Style = ProgressBarStyle.Continuous,
                ForeColor = UIColors.Green,
                // BackgroundDark rather than BackgroundLight - a progress
                // bar's track needs to read as a recessed surface, not
                // whatever shade the caller's panel happens to use (several
                // consumers set their panel BackColor to BackgroundLight too,
                // which made the track invisible - the fill looked like it
                // was floating on transparent background). BackgroundDark is
                // the one shade lower than every panel role
                // (Medium/Light/Lighter/Elevated), so it stays visibly
                // recessed regardless of which of those the caller used.
                BackColor = UIColors.BackgroundDark,
                Margin = new Padding(0)
            };
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
