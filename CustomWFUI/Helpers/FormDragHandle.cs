using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CustomWFUI.Helpers
{
    internal class FormDragHandle : IDisposable
    {
        private const int WmNcLeftButtonDown = 0xA1;
        private const int HtCaption = 0x2;

        private const int DragThreshold = 4;
        private const int TimerDelay = 30;
        private const int SnapThreshold = 4;
        private const int OffscreenTolerance = 50;

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        private bool _mouseDown;
        private Point _mouseDownPosition;
        private bool _isRestoringFromMaximized;

        public Control DragHandle { get; private set; }

        public bool AllowWindowSnapAndMaximize { get; set; }

        public FormDragHandle(
            Control control,
            bool allowWindowSnapAndMaximize = true)
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));

            DragHandle = control;
            AllowWindowSnapAndMaximize = allowWindowSnapAndMaximize;

            WireEvents();
        }

        public void Dispose()
        {
            UnwireEvents();

            _mouseDown = false;
            _isRestoringFromMaximized = false;
            DragHandle = null;
        }

        private void WireEvents()
        {
            if (DragHandle == null)
                return;

            DragHandle.MouseDown += OnMouseDown;
            DragHandle.MouseMove += OnMouseMove;
            DragHandle.MouseUp += OnMouseUp;
        }

        private void UnwireEvents()
        {
            if (DragHandle == null)
                return;

            DragHandle.MouseDown -= OnMouseDown;
            DragHandle.MouseMove -= OnMouseMove;
            DragHandle.MouseUp -= OnMouseUp;
        }

        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;

            Form form = DragHandle.FindForm();

            if (form == null)
                return;

            if (e.Clicks == 2)
            {
                if (AllowWindowSnapAndMaximize)
                    ToggleMaximize(form);

                return;
            }

            _mouseDown = true;
            _mouseDownPosition = Cursor.Position;
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!_mouseDown || e.Button != MouseButtons.Left)
                return;

            Form form = DragHandle.FindForm();

            if (form == null)
                return;

            Point cursor = Cursor.Position;

            int dragDistance =
                Math.Abs(cursor.X - _mouseDownPosition.X) +
                Math.Abs(cursor.Y - _mouseDownPosition.Y);

            if (dragDistance < DragThreshold)
                return;

            _mouseDown = false;

            if (form.WindowState == FormWindowState.Maximized)
            {
                RestoreMaximizedFormAndStartDrag(form);
                return;
            }

            StartNativeDrag(form);
        }

        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            _mouseDown = false;
        }

        private void RestoreMaximizedFormAndStartDrag(Form form)
        {
            if (!AllowWindowSnapAndMaximize)
                return;

            if (_isRestoringFromMaximized)
                return;

            _isRestoringFromMaximized = true;

            form.WindowState = FormWindowState.Normal;

            RunDelayed(delegate
            {
                Point cursor = Cursor.Position;
                Screen screen = Screen.FromPoint(cursor);

                form.Location = new Point(
                    cursor.X - form.Width / 2,
                    Math.Max(screen.WorkingArea.Top, cursor.Y - 10));

                StartNativeDrag(form);

                _isRestoringFromMaximized = false;
            });
        }

        private void StartNativeDrag(Form form)
        {
            if (form == null)
                return;

            ReleaseCapture();
            SendMessage(form.Handle, WmNcLeftButtonDown, HtCaption, 0);

            SnapToTopIfNeeded(form);
            KeepFormOnScreen(form);
        }

        private void ToggleMaximize(Form form)
        {
            if (form == null)
                return;

            if (form.WindowState == FormWindowState.Maximized)
            {
                form.WindowState = FormWindowState.Normal;
                return;
            }

            RunDelayed(delegate
            {
                if (!form.IsDisposed)
                    form.WindowState = FormWindowState.Maximized;
            });
        }

        private void SnapToTopIfNeeded(Form form)
        {
            if (!AllowWindowSnapAndMaximize)
                return;

            if (form == null || form.WindowState == FormWindowState.Maximized)
                return;

            Point cursor = Cursor.Position;
            Screen screen = Screen.FromPoint(cursor);
            Rectangle area = screen.WorkingArea;

            if (cursor.Y > area.Top + SnapThreshold)
                return;

            RunDelayed(delegate
            {
                if (!form.IsDisposed)
                    form.WindowState = FormWindowState.Maximized;
            });
        }

        private void KeepFormOnScreen(Form form)
        {
            if (form == null || form.WindowState == FormWindowState.Maximized)
                return;

            Screen screen = Screen.FromControl(form);
            Rectangle area = screen.WorkingArea;

            bool outsideScreen =
                form.Right < area.Left + OffscreenTolerance ||
                form.Left > area.Right - OffscreenTolerance ||
                form.Bottom < area.Top + OffscreenTolerance ||
                form.Top > area.Bottom - OffscreenTolerance;

            if (!outsideScreen)
                return;

            int centerX = area.Left + (area.Width - form.Width) / 2;
            int centerY = area.Top + (area.Height - form.Height) / 2;

            form.Location = new Point(centerX, centerY);
        }

        private void RunDelayed(Action action)
        {
            if (action == null)
                return;

            Timer timer = new Timer();
            timer.Interval = TimerDelay;

            timer.Tick += delegate
            {
                timer.Stop();

                try
                {
                    action();
                }
                finally
                {
                    timer.Dispose();
                }
            };

            timer.Start();
        }
    }
}