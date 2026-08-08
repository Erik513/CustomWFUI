using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using CustomWFUI.Styles;

namespace CustomWFUI.Factories
{
    internal static class UIProgressBarFactory
    {
        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(
            IntPtr hWnd,
            string pszSubAppName,
            string pszSubIdList);

        public static ProgressBar CreateStandard()
        {
            ProgressBar progressBar = new ProgressBar
            {
                Minimum = 0,
                Style = ProgressBarStyle.Continuous,
                ForeColor = UIColors.Green,
                BackColor = UIColors.BackgroundLight,
                Margin = new Padding(0)
            };

            // With visual styles enabled, the native ProgressBar ignores
            // ForeColor/BackColor entirely and always shows the system green
            // bar. SetWindowTheme("", "") opts just this control out of visual
            // styles so the custom colors actually take effect.
            progressBar.HandleCreated += (s, e) =>
                SetWindowTheme(progressBar.Handle, "", "");

            return progressBar;
        }
    }
}
