using System;
using System.Drawing;
using System.Windows.Forms;

namespace CustomWFUI.Helpers
{
    public class BorderlessResizeHandler
    {
        private const int WmNcHitTest = 0x84;

        private const int HtLeft = 10;
        private const int HtRight = 11;
        private const int HtTop = 12;
        private const int HtTopLeft = 13;
        private const int HtTopRight = 14;
        private const int HtBottom = 15;
        private const int HtBottomLeft = 16;
        private const int HtBottomRight = 17;

        private const int ResizeBorder = 6;

        private readonly Form _form;

        public BorderlessResizeHandler(Form form)
        {
            _form = form;
        }

        public bool TryHandleMessage(ref Message message)
        {
            if (_form == null)
                return false;

            if (message.Msg != WmNcHitTest)
                return false;

            if (_form.WindowState == FormWindowState.Maximized)
                return false;

            IntPtr hitTestResult = GetResizeHitTestResult(message);

            if (hitTestResult == IntPtr.Zero)
                return false;

            message.Result = hitTestResult;
            return true;
        }

        private IntPtr GetResizeHitTestResult(Message message)
        {
            Point screenPoint = GetPointFromLParam(message.LParam);
            Point clientPoint = _form.PointToClient(screenPoint);

            bool left = clientPoint.X < ResizeBorder;
            bool right = clientPoint.X > _form.ClientSize.Width - ResizeBorder;
            bool top = clientPoint.Y < ResizeBorder;
            bool bottom = clientPoint.Y > _form.ClientSize.Height - ResizeBorder;

            if (left && top)
                return (IntPtr)HtTopLeft;

            if (right && top)
                return (IntPtr)HtTopRight;

            if (left && bottom)
                return (IntPtr)HtBottomLeft;

            if (right && bottom)
                return (IntPtr)HtBottomRight;

            if (left)
                return (IntPtr)HtLeft;

            if (right)
                return (IntPtr)HtRight;

            if (top)
                return (IntPtr)HtTop;

            if (bottom)
                return (IntPtr)HtBottom;

            return IntPtr.Zero;
        }

        private static Point GetPointFromLParam(IntPtr lParam)
        {
            int value = lParam.ToInt32();

            int x = value & 0xFFFF;
            int y = value >> 16;

            return new Point(x, y);
        }
    }
}