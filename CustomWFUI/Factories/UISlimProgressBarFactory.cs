using System.Drawing;
using CustomWFUI.Controls;
using CustomWFUI.Styles;

namespace CustomWFUI.Factories
{
    internal static class UISlimProgressBarFactory
    {
        public static SlimProgressBar CreateGreen()
        {
            return Create(UIColors.Green);
        }

        public static SlimProgressBar CreatePrimary()
        {
            return Create(UIColors.Primary);
        }

        private static SlimProgressBar Create(Color foreColor)
        {
            return new SlimProgressBar
            {
                Minimum = 0,
                Maximum = 100,
                ForeColor = foreColor,
                BackColor = UIColors.BackgroundMedium
            };
        }
    }
}
