using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using CustomWFUI.Styles;

namespace CustomWFUI.Factories
{
    public static class UIProgressBarFactory
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
                ForeColor = UIColors.Green,
                BackColor = UIColors.BackgroundLight,
                Margin = new Padding(0)
            };

            // Mit aktivierten Visual Styles ignoriert der native ProgressBar
            // ForeColor/BackColor komplett und zeigt immer den System-Grün-Balken.
            // SetWindowTheme("", "") schaltet die Visual Styles nur für dieses
            // Control ab, damit die eigenen Farben tatsächlich greifen.
            progressBar.HandleCreated += (s, e) =>
                SetWindowTheme(progressBar.Handle, "", "");

            return progressBar;
        }
    }
}
