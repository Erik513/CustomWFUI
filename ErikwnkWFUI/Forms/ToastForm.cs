using System;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Forms
{
    /// <summary>
    /// A small, rounded, self-dismissing confirmation toast (checkmark +
    /// message) - call the static <see cref="ShowToast"/> rather than
    /// constructing this directly. Only one toast shows at a time; showing
    /// a new one closes whatever's currently up.
    /// </summary>
    public class ToastForm : Form
    {
        private const int ToastWidth = 350;
        private const int ToastHeight = 80;
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

        /// <summary>Shows a toast near the bottom-center of <paramref name="owner"/>, auto-closing itself after ~2.5s.</summary>
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
            }

            base.Dispose(disposing);
        }

        private void ConfigureForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            Size = new Size(ToastWidth, ToastHeight);
            // A shade darker than the raw accent (PrimaryDark, the same
            // shade CreatePrimary buttons use as their idle background) -
            // the toast used to be the accent itself, unchanged.
            BackColor = UIColors.PrimaryDark;
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
                // AccentForeColor is computed against the raw accent
                // (Primary), not the darker PrimaryDark this form actually
                // uses as its background - contrast has to be computed
                // against the real background, same principle as button
                // text throughout this library.
                ForeColor = UIColors.GetContrastingForeColor(UIColors.PrimaryDark),
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
                ForeColor = UIColors.GetContrastingForeColor(UIColors.PrimaryDark),
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

        // Win11 rounds window corners itself via DWM (anti-aliased, all four
        // identical, and it's what draws the thin gray border InfoPopupForm
        // has - matched here rather than kept as a plain-rectangle GDI
        // Region, which never gets that border). On pre-Win11 the call is a
        // silent no-op and the toast is simply a plain rectangle - see
        // InfoPopupForm.ApplyRoundedRegion, which uses the same approach.
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWCP_ROUND = 2;

        [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        private void ApplyRoundedRegion()
        {
            if (!IsHandleCreated)
                return;

            int pref = DWMWCP_ROUND;
            try
            {
                DwmSetWindowAttribute(Handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref pref, sizeof(int));
            }
            catch
            {
                // pre-Win11 - no rounded corners, plain rectangle
            }
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