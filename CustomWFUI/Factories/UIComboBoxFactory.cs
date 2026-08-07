using System.Windows.Forms;
using CustomWFUI.Styles;

namespace CustomWFUI.Factories
{
    public static class UIComboBoxFactory
    {
        public static ComboBox CreateStandard(
            ComboBoxStyle comboBoxStyle = ComboBoxStyle.DropDownList)
        {
            return new ComboBox
            {
                BackColor = UIColors.BackgroundMedium,
                ForeColor = UIColors.TextPrimary,
                FlatStyle = FlatStyle.Flat,
                Font = UIFonts.Normal,
                DropDownStyle = comboBoxStyle
            };
        }
    }
}