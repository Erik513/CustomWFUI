using System.Windows.Forms;
using CustomWFUI.Styles;

namespace CustomWFUI.Factories
{
    public static class UITextBoxFactory
    {
        public static TextBox CreateStandard(
            string text = "",
            string placeholder = "")
        {
            TextBox textBox = new TextBox
            {
                Text = text ?? "",
                BackColor = UIColors.BackgroundMedium,
                ForeColor = UIColors.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = UIFonts.Normal
            };

            if (!string.IsNullOrWhiteSpace(placeholder))
                SetPlaceholder(textBox, placeholder);

            return textBox;
        }

        public static TextBox CreateBorderstyleNone(
            string text = "",
            string placeholder = "")
        {
            TextBox textBox = new TextBox
            {
                Text = text ?? "",
                BackColor = UIColors.BackgroundLight,
                ForeColor = UIColors.TextPrimary,
                BorderStyle = BorderStyle.None,
                Font = UIFonts.Normal
            };

            if (!string.IsNullOrWhiteSpace(placeholder))
                SetPlaceholder(textBox, placeholder);

            return textBox;
        }

        private static void SetPlaceholder(
            TextBox textBox,
            string placeholder)
        {
            if (textBox == null)
                return;

            string placeholderText = placeholder ?? "";

            textBox.Text = placeholderText;
            textBox.ForeColor = UIColors.TextMuted;

            textBox.GotFocus += delegate
            {
                if (textBox.Text != placeholderText)
                    return;

                textBox.Text = "";
                textBox.ForeColor = UIColors.TextPrimary;
            };

            textBox.LostFocus += delegate
            {
                if (!string.IsNullOrWhiteSpace(textBox.Text))
                    return;

                textBox.Text = placeholderText;
                textBox.ForeColor = UIColors.TextMuted;
            };
        }
    }
}