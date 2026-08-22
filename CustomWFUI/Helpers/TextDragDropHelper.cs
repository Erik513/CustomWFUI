using System;
using System.Windows.Forms;

namespace ErikwnkWFUI.Helpers
{
    /// <summary>
    /// Helper for adding drag and drop support to controls.
    /// 
    /// TextDragDropHelper.EnableTextDragDrop(
    ///     txtPath,
    ///     text => txtPath.Text = text);
    /// </summary>
    public static class TextDragDropHelper
    {
        /// <summary>Wires up drag/drop of plain text onto <paramref name="control"/>, calling <paramref name="onTextDropped"/> with whatever text was dropped. Sets <see cref="Control.AllowDrop"/> and swaps the cursor to indicate whether the current drag target has droppable text.</summary>
        public static void EnableTextDragDrop(
            Control control,
            Action<string> onTextDropped)
        {
            if (control == null)
                return;

            control.AllowDrop = true;

            control.DragEnter += delegate (object sender, DragEventArgs e)
            {
                HandleDragEnter(control, e);
            };

            control.DragOver += delegate (object sender, DragEventArgs e)
            {
                HandleDragOver(e);
            };

            control.DragLeave += delegate (object sender, EventArgs e)
            {
                ResetCursor(control);
            };

            control.DragDrop += delegate (object sender, DragEventArgs e)
            {
                HandleDragDrop(control, e, onTextDropped);
            };
        }

        private static void HandleDragEnter(Control control, DragEventArgs e)
        {
            if (HasTextData(e))
            {
                e.Effect = DragDropEffects.Copy;
                control.Cursor = Cursors.Hand;
                return;
            }

            e.Effect = DragDropEffects.None;
            control.Cursor = Cursors.No;
        }

        private static void HandleDragOver(DragEventArgs e)
        {
            e.Effect = HasTextData(e)
                ? DragDropEffects.Copy
                : DragDropEffects.None;
        }

        private static void HandleDragDrop(
            Control control,
            DragEventArgs e,
            Action<string> onTextDropped)
        {
            try
            {
                string droppedText = GetDroppedText(e);

                if (!string.IsNullOrWhiteSpace(droppedText) && onTextDropped != null)
                    onTextDropped(droppedText);
            }
            finally
            {
                ResetCursor(control);
            }
        }

        private static bool HasTextData(DragEventArgs e)
        {
            return e != null &&
                   e.Data != null &&
                   e.Data.GetDataPresent(DataFormats.Text);
        }

        private static string GetDroppedText(DragEventArgs e)
        {
            if (!HasTextData(e))
                return null;

            return e.Data.GetData(DataFormats.Text) as string;
        }

        private static void ResetCursor(Control control)
        {
            if (control == null || control.IsDisposed)
                return;

            control.Cursor = Cursors.Default;
        }
    }
}