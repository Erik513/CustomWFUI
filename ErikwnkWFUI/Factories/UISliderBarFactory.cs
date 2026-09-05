using ErikwnkWFUI.Controls;

namespace ErikwnkWFUI.Factories
{
    internal static class UISliderBarFactory
    {
        public static SliderBar CreateStandard(double value = 0, double maximum = 1.0)
        {
            return new SliderBar
            {
                Maximum = maximum,
                Value = value
            };
        }
    }
}
