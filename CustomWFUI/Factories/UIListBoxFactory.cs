using CustomWFUI.Controls;

namespace CustomWFUI.Factories
{
    internal static class UIListBoxFactory
    {
        public static ListBox CreateStandard(
            string displayTextMember = null,
            bool allowReorder = true,
            bool showEnumeration = false)
        {
            return new ListBox
            {
                DisplayTextMember = displayTextMember,
                AllowReorder = allowReorder,
                ShowEnumeration = showEnumeration
            };
        }
    }
}