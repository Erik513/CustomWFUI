using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using CustomWFUI.Styles;

namespace CustomWFUI.Factories
{
    internal static class UIComboBoxFactory
    {
        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string pszSubIdList);

        public static ComboBox CreateStandard(
            ComboBoxStyle comboBoxStyle = ComboBoxStyle.DropDownList)
        {
            ComboBox comboBox = new ComboBox
            {
                BackColor = UIColors.BackgroundMedium,
                ForeColor = UIColors.TextPrimary,
                FlatStyle = FlatStyle.Flat,
                Font = UIFonts.Normal,
                DropDownStyle = comboBoxStyle,
                // FlatStyle/BackColor/ForeColor only style the closed box -
                // the dropdown popup is a native Win32 list that always
                // highlights the hovered/selected row with the OS accent
                // (blue), no matter what's set above. Owner-drawing every
                // row is the only way to make that highlight follow the
                // app's own accent color instead.
                DrawMode = DrawMode.OwnerDrawFixed
            };

            comboBox.DrawItem += ComboBox_DrawItem;

            // Even with FlatStyle.Flat, Windows still paints a themed
            // focus/hover border around the box in the OS accent color
            // (blue) - that's drawn by the visual-styles engine, not by
            // any WinForms property, so it ignores BackColor/ForeColor
            // entirely. Detaching the control from its theme class is the
            // only way to stop it; the control then falls back to a
            // plain, non-accent-colored border. Forcing Handle here (rather
            // than waiting for the HandleCreated event) does this
            // synchronously before the caller adds items or parents the
            // control - deferring it left a window where an owner-drawn
            // item could get painted before the retheme took effect,
            // rendering blank.
            SetWindowTheme(comboBox.Handle, "", "");

            return comboBox;
        }

        private static void ComboBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || !(sender is ComboBox comboBox))
                return;

            // ComboBoxEdit marks the closed box's own display area (redrawn
            // whenever the control has focus) as opposed to a row inside
            // the open dropdown list - without excluding it, the closed
            // box would also pick up Selected and stay permanently
            // yellow-filled the whole time it has focus, not just while
            // hovering/selecting a row in the open popup.
            bool isDisplayArea = (e.State & DrawItemState.ComboBoxEdit) != 0;
            bool isSelected = !isDisplayArea && (e.State & DrawItemState.Selected) != 0;
            Color backColor = isSelected ? UIColors.Primary : UIColors.BackgroundMedium;
            Color textColor = isSelected ? UIColors.GetContrastingForeColor(backColor) : UIColors.TextPrimary;

            using (SolidBrush backBrush = new SolidBrush(backColor))
            {
                e.Graphics.FillRectangle(backBrush, e.Bounds);
            }

            string text = comboBox.Items[e.Index]?.ToString() ?? "";

            TextRenderer.DrawText(
                e.Graphics,
                text,
                comboBox.Font,
                e.Bounds,
                textColor,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPrefix);
        }
    }
}