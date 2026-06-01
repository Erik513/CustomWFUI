using System.Drawing;
using CustomWFUI.Controls;
using CustomWFUI.Styles;

namespace CustomWFUI.Factories
{
    public static class UIToggleSwitchFactory
    {
        public static ToggleSwitch CreateStandard(
            bool checkedState = true,
            string tooltipChecked = null,
            string tooltipUnchecked = null)
        {
            return CreateToggleSwitch(
                checkedState,
                new Size(45, 25),
                tooltipChecked,
                tooltipUnchecked);
        }

        public static ToggleSwitch CreateSmall(
            bool checkedState = true,
            string tooltipChecked = null,
            string tooltipUnchecked = null)
        {
            return CreateToggleSwitch(
                checkedState,
                new Size(35, 20),
                tooltipChecked,
                tooltipUnchecked);
        }

        public static ToggleSwitch CreateLarge(
            bool checkedState = true,
            string tooltipChecked = null,
            string tooltipUnchecked = null)
        {
            return CreateToggleSwitch(
                checkedState,
                new Size(55, 30),
                tooltipChecked,
                tooltipUnchecked);
        }

        private static ToggleSwitch CreateToggleSwitch(
            bool checkedState,
            Size size,
            string tooltipChecked,
            string tooltipUnchecked)
        {
            return new ToggleSwitch
            {
                Checked = checkedState,
                Size = size,
                ToolTipTextChecked = tooltipChecked,
                ToolTipTextUnchecked = tooltipUnchecked,
                BackColor = UIColors.Transparent
            };
        }
    }
}