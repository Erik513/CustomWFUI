using ErikwnkWFUI.Controls;

namespace ErikwnkWFUI.Factories
{
    internal static class UIVolumeSliderFactory
    {
        // No maximum parameter, unlike UISliderBarFactory.CreateStandard -
        // VolumeSlider fixes Maximum to 1.0 itself (a volume is always 0..1).
        public static VolumeSlider CreateStandard(double value = 0)
        {
            return new VolumeSlider
            {
                Value = value
            };
        }
    }
}
