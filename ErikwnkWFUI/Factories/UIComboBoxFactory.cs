using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Factories
{
    internal static class UIComboBoxFactory
    {
        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string pszSubIdList);

        [DllImport("user32.dll")]
        private static extern bool GetComboBoxInfo(IntPtr hwndCombo, ref ComboBoxInfo pcbi);

        [StructLayout(LayoutKind.Sequential)]
        private struct Rect
        {
            public int Left, Top, Right, Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ComboBoxInfo
        {
            public int cbSize;
            public Rect rcItem;
            public Rect rcButton;
            public int buttonState;
            public IntPtr hwndCombo;
            public IntPtr hwndEdit;
            public IntPtr hwndList;
        }

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

            // The dropdown popup is a SEPARATE native window (its own HWND,
            // fetched via GetComboBoxInfo) from the combo box itself - it
            // still had its own themed border in the OS accent (blue) even
            // after the line above, since stripping the combo box's own
            // window theme doesn't touch it. Comctl32 creates this child
            // window up front (hidden until first drop-down), so it's
            // already available right after the combo box's own handle is.
            UnthemeDropDownList(comboBox);

            return comboBox;
        }

        // Preserves selection by POSITION (same index in the new list as
        // before), not by value/identity - the common case this is for is
        // re-localization, where the same N choices get rebuilt with new
        // display text in the same order, and "same index" is the only
        // notion of "same item" that still makes sense once the old and new
        // items aren't equal to each other. A caller whose items carry their
        // own stable identity (e.g. an enum wrapped in a display object, the
        // way DJ-mode/generation-mode combo boxes elsewhere in this codebase
        // already do) and needs identity-based matching across a genuine
        // reordering should still just save/restore SelectedIndex (or a
        // matching key) by hand instead. Doesn't suppress SelectedIndexChanged
        // during the rebuild - Items.Clear() and the SelectedIndex restore
        // below can each raise it once, since there's no supported way to
        // detach every external subscriber generically; a handler reacting
        // to the transient -1 in between is the one thing this can't avoid.
        public static void ReplaceItems(ComboBox comboBox, IEnumerable<object> items, bool keepSelection = true)
        {
            if (comboBox == null || items == null)
                return;

            int previousSelectedIndex = keepSelection ? comboBox.SelectedIndex : -1;

            comboBox.BeginUpdate();
            try
            {
                comboBox.Items.Clear();
                foreach (object item in items)
                    comboBox.Items.Add(item);

                if (previousSelectedIndex >= 0 && previousSelectedIndex < comboBox.Items.Count)
                    comboBox.SelectedIndex = previousSelectedIndex;
            }
            finally
            {
                comboBox.EndUpdate();
            }
        }

        private static void UnthemeDropDownList(ComboBox comboBox)
        {
            ComboBoxInfo info = new ComboBoxInfo { cbSize = Marshal.SizeOf(typeof(ComboBoxInfo)) };

            if (GetComboBoxInfo(comboBox.Handle, ref info) && info.hwndList != IntPtr.Zero)
                SetWindowTheme(info.hwndList, "", "");
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

            // comboBox.Enabled wasn't checked at all here before - the closed
            // box's own text always rendered as the full-brightness
            // TextPrimary, with zero visual change when the control was
            // disabled (confirmed by rendering both states and comparing
            // pixels - they were identical).
            Color textColor = !comboBox.Enabled
                ? UIColors.TextDisabled
                : isSelected
                    ? UIColors.GetContrastingForeColor(backColor)
                    : UIColors.TextPrimary;

            using (SolidBrush backBrush = new SolidBrush(backColor))
            {
                e.Graphics.FillRectangle(backBrush, e.Bounds);
            }

            // GetItemText (not a raw .ToString() on the item) is what
            // respects DisplayMember, matching how a native ComboBox
            // renders its items - without this, binding a list of objects
            // via DisplayMember rendered each row as the item's fully-
            // qualified type name instead of the intended display text.
            object item = e.Index >= 0 && e.Index < comboBox.Items.Count ? comboBox.Items[e.Index] : null;
            string text = comboBox.GetItemText(item);

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