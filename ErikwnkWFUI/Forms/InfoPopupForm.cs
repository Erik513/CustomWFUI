using ErikwnkWFUI.Styles;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ErikwnkWFUI.Forms
{
    /// <summary>
    /// A small, borderless, rounded-corner tooltip-like popup for showing
    /// extra info next to a control or the mouse cursor - reuse a single
    /// instance across many show calls rather than creating a new one each
    /// time (it repositions/relabels itself instead of flashing closed and
    /// reopening). Two independent content modes: a single block of text
    /// (<see cref="ShowInfo"/>/<see cref="ShowInfoAtMouse"/>) or one or more
    /// labeled sections (<see cref="ShowSections"/> and friends).
    /// </summary>
    public class InfoPopupForm : Form
    {
        private const int MaxTextWidth = 260;
        private const int ScreenMargin = 10;
        private const int OwnerOffsetX = 8;
        private const int OwnerOffsetY = -10;

        private readonly Label _titleLabel;
        private readonly Label _textLabel;
        private readonly FlowLayoutPanel _layout;

        private Timer _showDelayTimer;
        private Control _pendingOwner;
        private Point _pendingMouseScreenPosition;
        private InfoPopupSection[] _pendingSections;
        private string _lastSectionContentKey;
        private bool _compact;
        private Size _compactSize = new Size(48, 24);

        /// <summary>
        /// Fixed-size mode for a single very short value (e.g. a percentage over
        /// a slider): the popup is exactly <see cref="CompactSize"/>, the text is
        /// centred, and it does not resize as the value's width changes
        /// ("9%" vs "100%"). Set this before the first Show call.
        /// </summary>
        public bool Compact
        {
            get { return _compact; }
            set
            {
                if (_compact == value)
                    return;
                _compact = value;

                if (value)
                {
                    _layout.Visible = false;
                    if (_layout.Controls.Contains(_textLabel))
                        _layout.Controls.Remove(_textLabel);

                    _textLabel.AutoSize = false;
                    _textLabel.Dock = DockStyle.Fill;
                    _textLabel.TextAlign = ContentAlignment.MiddleCenter;
                    _textLabel.MaximumSize = Size.Empty;
                    if (!Controls.Contains(_textLabel))
                        Controls.Add(_textLabel);
                    _textLabel.BringToFront();

                    AutoSize = false;
                    Padding = new Padding(0);
                    Opacity = 0.92;
                    ApplyCompactSize();
                }
            }
        }

        // A borderless top-level Form can't go below the OS minimum tracking
        // size (~136x39) unless MinimumSize is set explicitly - WinForms then
        // writes it into WM_GETMINMAXINFO. Lock min == max == the wanted size.
        private void ApplyCompactSize()
        {
            MinimumSize = Size.Empty;
            MaximumSize = Size.Empty;
            ClientSize = _compactSize;
            MinimumSize = Size;
            MaximumSize = Size;
            ApplyRoundedRegion();
        }

        /// <summary>The fixed client size used while <see cref="Compact"/> is true.</summary>
        public Size CompactSize
        {
            get { return _compactSize; }
            set
            {
                _compactSize = value;
                if (_compact)
                    ApplyCompactSize();
            }
        }

        // A value popup must never take activation away from the window whose
        // slider is being dragged.
        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }

        public InfoPopupForm(string title = "")
        {
            ConfigureForm();

            _layout = CreateLayoutPanel();
            _titleLabel = CreateTitleLabel(title);
            _textLabel = CreateTextLabel();

            if (!string.IsNullOrWhiteSpace(title))
                _layout.Controls.Add(_titleLabel);

            _layout.Controls.Add(_textLabel);
            Controls.Add(_layout);

            _showDelayTimer = new Timer();
            _showDelayTimer.Interval = 400;
            _showDelayTimer.Tick += OnShowDelayTimerTick;

            Load += OnFormLoad;
            SizeChanged += OnFormSizeChanged;
        }

        /// <summary>Shows (or repositions, if already visible) a single block of text, anchored to the bottom-right of <paramref name="owner"/>.</summary>
        public void ShowInfo(string text, Control owner)
        {
            if (owner == null || owner.IsDisposed)
                return;

            RefreshColors();

            _textLabel.Text = string.IsNullOrWhiteSpace(text)
                ? UIStrings.Get("InfoPopup.None")
                : text;

            PerformLayout();

            Location = GetPopupLocation(owner);

            if (!Visible)
                Show(owner.FindForm());

            BringToFront();
        }
        private Point GetPopupLocation(Control owner)
        {
            Point location = owner.PointToScreen(
                new Point(owner.Width + OwnerOffsetX, -Height + owner.Height + OwnerOffsetY));

            Rectangle screen = Screen.FromControl(owner).WorkingArea;

            if (location.Y < screen.Top + ScreenMargin)
                location.Y = screen.Top + ScreenMargin;

            if (location.X + Width > screen.Right)
                location.X = screen.Right - Width - ScreenMargin;

            if (location.X < screen.Left + ScreenMargin)
                location.X = screen.Left + ScreenMargin;

            if (location.Y + Height > screen.Bottom)
                location.Y = screen.Bottom - Height - ScreenMargin;

            return location;
        }

        /// <summary>
        /// Shows (or repositions) a single line of text centred horizontally on
        /// <paramref name="anchorLocalX"/> (a pixel x inside <paramref name="anchor"/>)
        /// and just above <paramref name="anchor"/> - flips below if there is no
        /// room. Typical use is a live value readout over a slider thumb.
        /// </summary>
        public void ShowCenteredAbove(string text, Control anchor, int anchorLocalX)
        {
            if (anchor == null || anchor.IsDisposed)
                return;

            RefreshColors();

            _textLabel.Text = string.IsNullOrWhiteSpace(text)
                ? UIStrings.Get("InfoPopup.None")
                : text;

            if (!_compact)
                PerformLayout();   // compact mode is a fixed size, don't resize

            Point anchorTop = anchor.PointToScreen(new Point(anchorLocalX, 0));
            int x = anchorTop.X - Width / 2;
            int y = anchorTop.Y - Height - 6;

            Rectangle screen = Screen.FromControl(anchor).WorkingArea;

            if (x < screen.Left + ScreenMargin)
                x = screen.Left + ScreenMargin;
            if (x + Width > screen.Right - ScreenMargin)
                x = screen.Right - ScreenMargin - Width;
            if (y < screen.Top + ScreenMargin)
                y = anchorTop.Y + anchor.Height + 6;

            Location = new Point(x, y);

            if (!Visible)
                Show(anchor.FindForm());

            BringToFront();
        }

        /// <summary>Same as <see cref="ShowInfo"/>, but anchored near <paramref name="mouseScreenPosition"/> instead of <paramref name="owner"/>'s bounds - typical use is showing this from a MouseMove handler.</summary>
        public void ShowInfoAtMouse(
            string text,
            Control owner,
            Point mouseScreenPosition)
        {
            if (owner == null || owner.IsDisposed)
                return;

            RefreshColors();

            _textLabel.Text =
                string.IsNullOrWhiteSpace(text)
                    ? UIStrings.Get("InfoPopup.None")
                    : text;

            PerformLayout();

            Location = GetPopupLocationNearMouse(
                owner,
                mouseScreenPosition);

            if (!Visible)
                Show(owner.FindForm());

            BringToFront();
        }
        private Point GetPopupLocationNearMouse(Control owner, Point mouseScreenPosition)
        {
            Point result = new Point(mouseScreenPosition.X + 12, mouseScreenPosition.Y + 12);

            Form ownerForm = owner.FindForm();

            Rectangle bounds = ownerForm != null
                ? ownerForm.Bounds
                : Screen.FromControl(owner).WorkingArea;

            if (result.X + Width > bounds.Right)
                result.X = mouseScreenPosition.X - Width - 12;

            if (result.Y + Height > bounds.Bottom)
                result.Y = mouseScreenPosition.Y - Height - 12;

            if (result.X < bounds.Left)
                result.X = bounds.Left + 10;

            if (result.Y < bounds.Top)
                result.Y = bounds.Top + 10;

            return result;
        }

        /// <summary>Shows one or more labeled <see cref="InfoPopupSection"/>s instead of plain text, anchored to <paramref name="owner"/>.</summary>
        public void ShowSections(Control owner, params InfoPopupSection[] sections)
        {
            if (owner == null || owner.IsDisposed)
                return;

            RefreshColors();

            _layout.Controls.Clear();

            foreach (InfoPopupSection section in sections)
            {
                if (!string.IsNullOrWhiteSpace(section.Header))
                {
                    Label headerLabel = CreateSectionHeaderLabel(section.Header);
                    _layout.Controls.Add(headerLabel);
                }

                if (!string.IsNullOrWhiteSpace(section.Text))
                {
                    Label textLabel = CreateSectionTextLabel(section.Text);
                    _layout.Controls.Add(textLabel);
                }
            }

            PerformLayout();
            Location = GetPopupLocation(owner);

            if (!Visible)
                Show(owner.FindForm());

            BringToFront();
        }
        /// <summary>
        /// Like <see cref="ShowSectionsAtMouse"/>, but waits 400ms before
        /// actually showing - meant for hover tooltips, so quickly passing
        /// the mouse over several items doesn't flash a popup for each one.
        /// Call <see cref="CancelPendingShow"/> on MouseLeave to cancel a
        /// still-pending show.
        /// </summary>
        public void ShowSectionsAtMouseDelayed(Control owner, Point mouseScreenPosition, params InfoPopupSection[] sections)
        {
            if (owner == null || owner.IsDisposed)
                return;

            _pendingOwner = owner;
            _pendingMouseScreenPosition = mouseScreenPosition;
            _pendingSections = sections;

            _showDelayTimer.Stop();
            _showDelayTimer.Start();
        }

        /// <summary>Cancels a show scheduled by <see cref="ShowSectionsAtMouseDelayed"/> that hasn't fired yet - does not hide the popup if it's already showing.</summary>
        public void CancelPendingShow()
        {
            if (_showDelayTimer != null)
                _showDelayTimer.Stop();

            _pendingOwner = null;
            _pendingSections = null;
        }

        private void OnShowDelayTimerTick(object sender, EventArgs e)
        {
            _showDelayTimer.Stop();

            if (_pendingOwner == null || _pendingOwner.IsDisposed || _pendingSections == null)
                return;

            ShowSectionsAtMouse(_pendingOwner, _pendingMouseScreenPosition, _pendingSections);
        }

        /// <summary>Immediate (non-delayed) version of <see cref="ShowSectionsAtMouseDelayed"/> - skips rebuilding the section labels if the content is identical to what's already showing.</summary>
        public void ShowSectionsAtMouse(Control owner, Point mouseScreenPosition, params InfoPopupSection[] sections)
        {
            if (owner == null || owner.IsDisposed)
                return;

            RefreshColors();

            string contentKey = BuildSectionContentKey(sections);

            if (contentKey != _lastSectionContentKey)
            {
                _lastSectionContentKey = contentKey;

                _layout.SuspendLayout();
                _layout.Controls.Clear();

                foreach (InfoPopupSection section in sections)
                {
                    if (!string.IsNullOrWhiteSpace(section.Header))
                        _layout.Controls.Add(CreateSectionHeaderLabel(section.Header));

                    if (!string.IsNullOrWhiteSpace(section.Text))
                        _layout.Controls.Add(CreateSectionTextLabel(section.Text));
                }

                _layout.ResumeLayout(true);
            }

            PerformLayout();
            Location = GetPopupLocationNearMouse(owner, mouseScreenPosition);

            if (!Visible)
                Show(owner.FindForm());

            BringToFront();
        }

        private string BuildSectionContentKey(InfoPopupSection[] sections)
        {
            if (sections == null || sections.Length == 0)
                return "";

            string key = "";

            foreach (InfoPopupSection section in sections)
            {
                if (section == null)
                    continue;

                key += section.Header + ":" + section.Text + "|";
            }

            return key;
        }

        private Label CreateSectionHeaderLabel(string text)
        {
            return new Label
            {
                AutoSize = true,
                Text = text,
                Font = UIFonts.Title,
                ForeColor = GetForeColor(),
                BackColor = Color.Transparent,
                Margin = new Padding(0, 6, 0, 3),
                MaximumSize = new Size(MaxTextWidth, 0)
            };
        }

        private Label CreateSectionTextLabel(string text)
        {
            return new Label
            {
                AutoSize = true,
                Text = text,
                Font = UIFonts.Normal,
                ForeColor = GetDimForeColor(),
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 4),
                MaximumSize = new Size(MaxTextWidth, 0)
            };
        }

        // TextPrimary/TextPrimaryDim are theme-dependent (near-white in Dark,
        // near-black in Light), but this popup's BackColor is PrimaryDark -
        // an accent shade that stays the same regardless of theme. Reading
        // TextPrimary directly meant Light theme rendered near-black text on
        // a still-dark accent background, unreadable. Same principle as
        // ToastForm/button press text: contrast has to be computed against
        // the actual background, not assumed from the theme.
        private static Color GetForeColor()
        {
            return UIColors.GetContrastingForeColor(UIColors.PrimaryDark);
        }

        // TextPrimaryDim's role (de-emphasized body text under a bold title)
        // doesn't have a fixed-background equivalent in UIColors, so this
        // blends the contrast-computed fore color partway toward the
        // background itself - keeps the same "quieter than the title" effect
        // regardless of which accent/theme combination is active.
        private static Color GetDimForeColor()
        {
            const double blendTowardBackground = 0.35;
            Color fore = GetForeColor();
            Color back = UIColors.PrimaryDark;

            return Color.FromArgb(
                fore.R + (int)((back.R - fore.R) * blendTowardBackground),
                fore.G + (int)((back.G - fore.G) * blendTowardBackground),
                fore.B + (int)((back.B - fore.B) * blendTowardBackground));
        }

        // Docs recommend reusing a single InfoPopupForm across many show
        // calls rather than creating a new one each time, which is exactly
        // why the color fix above wasn't enough on its own: BackColor/the
        // label colors were still only ever set once, at construction time -
        // so an app that switches accent/theme after building this popup
        // (or, in ErikwnkWFUI.Showcase's case, after every single accent
        // swatch click) kept showing whatever color was live when `new
        // InfoPopupForm(...)` first ran. Called at the top of every Show*
        // method so each call re-reads the current accent, the same way a
        // freshly-constructed ToastForm/MessageBox naturally would.
        private void RefreshColors()
        {
            BackColor = UIColors.PrimaryDark;

            Color foreColor = GetForeColor();
            Color dimForeColor = GetDimForeColor();

            if (_compact)
            {
                _textLabel.ForeColor = foreColor;
                return;
            }

            foreach (Control control in _layout.Controls)
            {
                Label label = control as Label;
                if (label == null)
                    continue;

                label.ForeColor = UIFonts.Title.Equals(label.Font) ? foreColor : dimForeColor;
            }
        }

        private void ConfigureForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;

            DoubleBuffered = true;
            BackColor = UIColors.PrimaryDark;
            Padding = new Padding(12);

            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
        }

        private FlowLayoutPanel CreateLayoutPanel()
        {
            return new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                BackColor = Color.Transparent,
                Margin = new Padding(0),
                Padding = new Padding(8)
            };
        }

        private Label CreateTitleLabel(string title)
        {
            return new Label
            {
                AutoSize = true,
                Text = title,
                Font = UIFonts.Title,
                ForeColor = GetForeColor(),
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 6)
            };
        }

        private Label CreateTextLabel()
        {
            return new Label
            {
                AutoSize = true,
                MaximumSize = new Size(MaxTextWidth, 0),
                Font = UIFonts.Normal,
                ForeColor = GetDimForeColor(),
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };
        }

        private void OnFormLoad(object sender, EventArgs e)
        {
            ApplyRoundedRegion();
        }

        private void OnFormSizeChanged(object sender, EventArgs e)
        {
            ApplyRoundedRegion();
        }

        /// <summary>
        /// Clips the window to a rounded rectangle. Always recomputed from the
        /// *current* <see cref="Control.ClientRectangle"/> - setting it once from
        /// a stale/pre-final size is what left one corner square before.
        /// </summary>
        // Win11 rounds window corners itself via DWM (anti-aliased, all four
        // identical) - just ask for the small radius. A window Region fights
        // this and leaves one corner square; on Win10 the call is a silent
        // no-op and the popup is simply a plain rectangle.
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWCP_ROUND = 2;
        private const int DWMWCP_ROUNDSMALL = 3;

        [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        private void ApplyRoundedRegion()
        {
            if (!IsHandleCreated)
                return;

            int pref = _compact ? DWMWCP_ROUNDSMALL : DWMWCP_ROUND;
            try
            {
                DwmSetWindowAttribute(Handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref pref, sizeof(int));
            }
            catch
            {
                // pre-Win11 - no rounded corners, plain rectangle
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_showDelayTimer != null)
                {
                    _showDelayTimer.Stop();
                    _showDelayTimer.Tick -= OnShowDelayTimerTick;
                    _showDelayTimer.Dispose();
                    _showDelayTimer = null;
                }

                if (base.Region != null)
                {
                    base.Region.Dispose();
                    base.Region = null;
                }
            }

            base.Dispose(disposing);
        }

    }
    /// <summary>One labeled block of text for <see cref="InfoPopupForm.ShowSections"/> and its overloads - a section with an empty/null Header or Text just omits that part.</summary>
    public class InfoPopupSection
    {
        public string Header { get; set; }
        public string Text { get; set; }

        public InfoPopupSection(string header, string text)
        {
            Header = header;
            Text = text;
        }
    }
}