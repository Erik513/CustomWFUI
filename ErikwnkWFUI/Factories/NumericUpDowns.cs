using System;
using System.Windows.Forms;
using static ErikwnkWFUI.UIStyles;

namespace ErikwnkWFUI.Factories
{
    internal static class UINumericUpDownFactory
    {
        public static NumericUpDown CreateStandard(
            decimal minimum = 0,
            decimal maximum = 100,
            decimal increment = 1,
            decimal value = 0)
        {
            NumericUpDown numericUpDown = new NumericUpDown
            {
                Minimum = minimum,
                Maximum = maximum,
                Increment = increment,
                Value = Math.Min(maximum, Math.Max(minimum, value)),
                BackColor = Colors.BackgroundLight,
                ForeColor = Colors.TextPrimary,
                Font = Fonts.Normal,
                BorderStyle = BorderStyle.None,
                TextAlign = HorizontalAlignment.Center
            };

            return numericUpDown;
        }
    }
}