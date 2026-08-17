using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CustomWFUI.Styles;

namespace CustomWFUI.Forms
{
    public class ToastForm : Form
    {
        private const int ToastWidth = 350;
        private const int ToastHeight = 80;
        private const int CornerRadius = 12;
        private const int CloseDelay = 2500;

        private static ToastForm _currentToast;

        private Timer _closeTimer;
        private Label _messageLabel;
        private Panel _contentPanel;

        public ToastForm()
        {
            ConfigureForm();
            CreateControls();
            CreateTimer();

            SizeChanged += OnToastSizeChanged;
            Load += OnToastLoad;
        }

        public static void ShowToast(string message, Form owner)
        {
            if (owner == null || owner.IsDisposed)
                return;

            CloseCurrentToast();

            ToastForm toast = new ToastForm();
            _currentToast = toast;

            toast.SetMessage(message);
            toast.Location = GetToastLocation(owner, toast);

            toast._closeTimer.Start();
            toast.Show(owner);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                SizeChanged -= OnToastSizeChanged;
                Load -= OnToastLoad;

                if (_closeTimer != null)
                {
                    _closeTimer.Stop();
                    _closeTimer.Tick -= OnCloseTimerTick;
                    _closeTimer.Dispose();
                    _closeTimer = null;
                }

                if (_currentToast == this)
                    _currentToast = null;

                if (Region != null)
                {
                    Region.Dispose();
                    Region = null;
                }
            }

            base.Dispose(disposing);
        }

        private void ConfigureForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            Size = new Size(ToastWidth, ToastHeight);
            BackColor = UIColors.Primary;
            TopMost = true;
            ShowInTaskbar = false;
            Opacity = 0.90;
        }

        private void CreateControls()
        {
            _contentPanel = CreateContentPanel();

            Label successIcon = CreateSuccessIcon();
            _messageLabel = CreateMessageLabel();

            _contentPanel.Controls.Add(_messageLabel);
            _contentPanel.Controls.Add(successIcon);

            Controls.Add(_contentPanel);
        }

        private Panel CreateContentPanel()
        {
            return new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(15, 10, 15, 10),
                BackColor = Color.Transparent
            };
        }

        private Label CreateSuccessIcon()
        {
            return new Label
            {
                Text = "✓",
                ForeColor = UIColors.AccentForeColor,
                Font = UIFonts.Icon,
                Size = new Size(30, 30),
                Location = new Point(10, 25),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
        }

        private Label CreateMessageLabel()
        {
            return new Label
            {
                Text = "",
                ForeColor = UIColors.AccentForeColor,
                Font = UIFonts.Normal,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent,
                AutoSize = false,
                Dock = DockStyle.Fill,
                Padding = new Padding(40, 0, 10, 0),
                AutoEllipsis = true
            };
        }

        private void CreateTimer()
        {
            _closeTimer = new Timer();
            _closeTimer.Interval = CloseDelay;
            _closeTimer.Tick += OnCloseTimerTick;
        }

        private void SetMessage(string message)
        {
            _messageLabel.Text = string.IsNullOrWhiteSpace(message)
                ? ""
                : message;
        }

        private static void CloseCurrentToast()
        {
            if (_currentToast == null || _currentToast.IsDisposed)
                return;

            _currentToast.Close();
            _currentToast = null;
        }

        private static Point GetToastLocation(Form owner, ToastForm toast)
        {
            return owner.PointToScreen(new Point(
                (owner.ClientSize.Width - toast.Width) / 2,
                (int)(owner.ClientSize.Height * 0.75) - toast.Height / 2));
        }

        private void ApplyRoundedRegion()
        {
            if (ClientRectangle.Width <= 0 || ClientRectangle.Height <= 0)
                return;

            Region oldRegion = Region;
            GraphicsPath path = GetRoundedRectanglePath(ClientRectangle, CornerRadius);

            try
            {
                Region = new Region(path);
            }
            finally
            {
                path.Dispose();

                if (oldRegion != null)
                    oldRegion.Dispose();
            }
        }

        private static GraphicsPath GetRoundedRectanglePath(Rectangle rect, int radius)
        {
            int diameter = radius * 2;
            GraphicsPath path = new GraphicsPath();

            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);

            path.CloseFigure();

            return path;
        }

        private void OnToastLoad(object sender, EventArgs e)
        {
            ApplyRoundedRegion();
        }

        private void OnToastSizeChanged(object sender, EventArgs e)
        {
            ApplyRoundedRegion();
        }

        private void OnCloseTimerTick(object sender, EventArgs e)
        {
            _closeTimer.Stop();
            Close();
        }
    }
}