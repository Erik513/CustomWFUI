using System.Drawing;
using CustomWFUI.Controls;

namespace CustomWFUI.Factories
{
    internal static class UIListBoxControlFactory
    {
        public static ListBoxControl CreateStandard(
            string headerTitle = null,
            string displayTextMember = null,
            bool allowReorder = false,
            bool showEnumeration = false,
            ContentAlignment headerTextAlign = ContentAlignment.MiddleLeft)
        {
            return new ListBoxControl(
                displayTextMember,
                allowReorder,
                showEnumeration,
                headerTitle,
                headerTextAlign);
        }
    }
}