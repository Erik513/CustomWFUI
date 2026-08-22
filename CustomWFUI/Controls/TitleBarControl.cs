using System;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// A custom-drawn window title bar: logo, title text, minimize/maximize/
    /// close buttons, and window dragging (including drag-to-maximize and
    /// double-click-to-maximize) - all reimplemented in managed code since it
    /// replaces the native one. <see cref="ErikwnkWFUI.Forms.StyledForm"/> already creates and
    /// docks one of these for you; construct it directly only if you're
    /// building a custom top-level window from scratch instead of deriving
    /// from StyledForm.
    /// </summary>
    public class TitleBarControl : Panel
    {
        private const int TitleBarHeight = 30;
        private const int IconSize = TitleBarHeight;
        private const int IconLeftMargin = 0;
        private const int TitleHorizontalPadding = 10;

        private static readonly Size ButtonSize = new Size(30, 30);

        private readonly bool _allowWindowSnapAndMaximize;

        private Label _titleLabel;
        private PictureBox _iconPictureBox;
        private Button _minimizeButton;
        private Button _maximizeButton;
        private Button _closeButton;

        private FormDragHandle _titleBarDragHandle;
        private FormDragHandle _titleLabelDragHandle;
        private FormDragHandle _iconDragHandle;

        private Form _parentForm;

        /// <summary>The window title text.</summary>
        public string Title
        {
            get { return _titleLabel.Text; }
            set { _titleLabel.Text = value ?? ""; }
        }

        /// <summary>The small logo at the top-left, or null to hide it entirely (default). See ErikwnkWFUI/README.md for how to load this from your own embedded resources.</summary>
        public Image IconImage
        {
            get { return _iconPictureBox.Image; }
            set
            {
                _iconPictureBox.Image = value;
                _iconPictureBox.Visible = value != null;
                UpdateLayout();
            }
        }

        /// <summary>The title text's color.</summary>
        public Color TitleForeColor
        {
            get { return _titleLabel.ForeColor; }
            set { _titleLabel.ForeColor = value; }
        }

        public ContentAlignment TitleTextAlign
        {
            get { return _titleLabel.TextAlign; }
            set
            {
                _titleLabel.TextAlign = value;
                UpdateLayout();
            }
        }

        public bool ShowMinimizeButton
        {
            get { return _minimizeButton.Visible; }
            set
            {
                _minimizeButton.Visible = value;
                UpdateLayout();
            }
        }

        public bool ShowMaximizeButton
        {
            get { return _maximizeButton.Visible; }
            set
            {
                _maximizeButton.Visible = value;
                UpdateLayout();
            }
        }

        public bool ShowCloseButton
        {
            get { return _closeButton.Visible; }
            set
            {
                _closeButton.Visible = value;
                UpdateLayout();
            }
        }

        /// <summary>allowWindowSnapAndMaximize controls both double-click-to-maximize and drag-to-top-of-screen-to-maximize.</summary>
        public TitleBarControl(
            Image icon = null,
            string title = "",
            ContentAlignment titleTextAlign = ContentAlignment.MiddleCenter,
            bool showMinimizeButton = true,
            bool showMaximizeButton = true,
            bool showCloseButton = true,
            bool allowWindowSnapAndMaximize = true,
            Color? backColor = null)
        {
            Color resolvedBackColor = backColor ?? UIColors.BackgroundBlack;
            _allowWindowSnapAndMaximize = allowWindowSnapAndMaximize;

            ConfigureControl(resolvedBackColor);

            _titleLabel = CreateTitleLabel(
                title,
                titleTextAlign,
                resolvedBackColor);
            _iconPictureBox = CreateIconPictureBox(icon);
            _minimizeButton = CreateTitleBarButton("🗕", UIStrings.Get("TitleBar.Minimize"), showMinimizeButton);
            _maximizeButton = CreateTitleBarButton("🗖", UIStrings.Get("TitleBar.Maximize"), showMaximizeButton);
            _closeButton = CreateTitleBarButton("✕", UIStrings.Get("TitleBar.Close"), showCloseButton);

            AddControls();
            CreateDragHandles();
            WireControlEvents();

            UpdateLayout();
            RunWhenHandleReady(UpdateLayout);
        }

        public void UpdateMaximizeButton(bool isMaximized)
        {
            if (_maximizeButton == null)
                return;

            _maximizeButton.Text = isMaximized ? "❐" : "🗖";
        }

        /// <summary>
        /// Re-reads the current theme's colors and reapplies them to this
        /// title bar and its sub-controls (title label, minimize/maximize/
        /// close buttons) - only needed by an app that switches theme after
        /// the owning StyledForm was already built, since every color here
        /// is otherwise only ever read once, at construction. Overwrites a
        /// custom <c>StyledFormOptions.TitleBarBackColor</c> set at
        /// construction time with the current theme's default; only call
        /// this if that's the behavior you want.
        /// </summary>
        public void RefreshTheme()
        {
            BackColor = UIColors.BackgroundBlack;

            _titleLabel.BackColor = UIColors.BackgroundBlack;
            _titleLabel.ForeColor = UIColors.TextPrimary;

            // Matches every property CreateStandard itself sets (see
            // UIButtonFactory.CreateStandard) - BackColor/ForeColor alone
            // left FlatAppearance.BorderColor/MouseOverBackColor/
            // MouseDownBackColor stuck on the OLD theme's values, so a
            // button that used to be a barely-visible dark border on a dark
            // background became a dark border on a now-light background -
            // reported as "the minimize button's border looks thicker" and
            // "the buttons look totally different from MessageBox's".
            foreach (Button button in new[] { _minimizeButton, _maximizeButton, _closeButton })
            {
                button.BackColor = UIColors.BackgroundMedium;
                button.ForeColor = UIColors.TextPrimary;
                button.FlatAppearance.BorderColor = UIColors.BorderDark;
                button.FlatAppearance.MouseOverBackColor = UIColors.BackgroundLighter;
                button.FlatAppearance.MouseDownBackColor = UIColors.Primary;
            }
        }

        public void DisposeDragHandle()
        {
            if (_titleBarDragHandle != null)
            {
                _titleBarDragHandle.Dispose();
                _titleBarDragHandle = null;
            }

            if (_titleLabelDragHandle != null)
            {
                _titleLabelDragHandle.Dispose();
                _titleLabelDragHandle = null;
            }

            if (_iconDragHandle != null)
            {
                _iconDragHandle.Dispose();
                _iconDragHandle = null;
            }
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);

            if (!Visible)
                return;

            RunWhenHandleReady(UpdateLayout);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                UnwireControlEvents();
                UnwireParentFormEvents();
                DisposeDragHandle();
            }

            base.Dispose(disposing);
        }

        private void ConfigureControl(Color backColor)
        {
            Height = TitleBarHeight;
            Dock = DockStyle.Top;
            BackColor = backColor;
            Visible = true;
            Margin = new Padding(0);
            Padding = new Padding(0);

            SetStyle(
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.ResizeRedraw,
                true);

            UpdateStyles();
        }

        private Label CreateTitleLabel(
            string title,
            ContentAlignment textAlign,
            Color backColor)
        {
            return new Label
            {
                Text = title ?? "",
                AutoSize = false,
                BackColor = backColor,
                ForeColor = UIColors.TextPrimary,
                Font = UIFonts.Title,
                TextAlign = textAlign,
                Padding = new Padding(TitleHorizontalPadding, 0, TitleHorizontalPadding, 0),
                AutoEllipsis = true,
                UseMnemonic = false
            };
        }

        private PictureBox CreateIconPictureBox(Image icon)
        {
            return new PictureBox
            {
                Image = icon,
                Size = new Size(IconSize, IconSize),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent,
                Visible = icon != null
            };
        }

        private Button CreateTitleBarButton(string text, string tooltip, bool visible)
        {
            Button button = UIButtonFactory.CreateStandard(text, tooltip, ButtonSize);
            button.Visible = visible;

            return button;
        }

        private void AddControls()
        {
            Controls.Add(_titleLabel);
            Controls.Add(_iconPictureBox);
            Controls.Add(_minimizeButton);
            Controls.Add(_maximizeButton);
            Controls.Add(_closeButton);

            _titleLabel.SendToBack();
            _iconPictureBox.BringToFront();
            _minimizeButton.BringToFront();
            _maximizeButton.BringToFront();
            _closeButton.BringToFront();
        }

        private void WireControlEvents()
        {
            Resize += OnTitleBarResize;
            ParentChanged += OnTitleBarParentChanged;
            HandleCreated += OnTitleBarHandleCreated;

            _minimizeButton.Click += OnMinimizeButtonClick;
            _maximizeButton.Click += OnMaximizeButtonClick;
            _closeButton.Click += OnCloseButtonClick;

            UIStrings.LanguageChanged += OnUIStringsLanguageChanged;
        }

        private void UnwireControlEvents()
        {
            Resize -= OnTitleBarResize;
            ParentChanged -= OnTitleBarParentChanged;
            HandleCreated -= OnTitleBarHandleCreated;

            if (_minimizeButton != null)
                _minimizeButton.Click -= OnMinimizeButtonClick;

            if (_maximizeButton != null)
                _maximizeButton.Click -= OnMaximizeButtonClick;

            if (_closeButton != null)
                _closeButton.Click -= OnCloseButtonClick;

            UIStrings.LanguageChanged -= OnUIStringsLanguageChanged;
        }

        private void OnUIStringsLanguageChanged(object sender, EventArgs e)
        {
            UIButtonFactory.UpdateTooltip(_minimizeButton, UIStrings.Get("TitleBar.Minimize"));
            UIButtonFactory.UpdateTooltip(_maximizeButton, UIStrings.Get("TitleBar.Maximize"));
            UIButtonFactory.UpdateTooltip(_closeButton, UIStrings.Get("TitleBar.Close"));
        }

        private void CreateDragHandles()
        {
            DisposeDragHandle();

            _titleBarDragHandle = new FormDragHandle(this, _allowWindowSnapAndMaximize);
            _titleLabelDragHandle = new FormDragHandle(_titleLabel, _allowWindowSnapAndMaximize);

            if (_iconPictureBox != null)
                _iconDragHandle = new FormDragHandle(_iconPictureBox, _allowWindowSnapAndMaximize);
        }

        private void UpdateLayout()
        {
            if (_closeButton == null || _maximizeButton == null || _minimizeButton == null || _iconPictureBox == null || _titleLabel == null)
                return;

            int right = Width;

            right = PositionButtonFromRight(_closeButton, right);
            right = PositionButtonFromRight(_maximizeButton, right);
            right = PositionButtonFromRight(_minimizeButton, right);

            int left = 0;

            if (_iconPictureBox.Visible)
            {
                _iconPictureBox.Location = new Point(IconLeftMargin, 0);
                _iconPictureBox.BringToFront();

                left = IconLeftMargin + IconSize;
            }

            UpdateTitleLabelBounds(left, right);

            _titleLabel.SendToBack();
        }

        private void UpdateTitleLabelBounds(int leftLimit, int rightLimit)
        {
            int labelLeft;
            int labelWidth;

            if (IsTitleAlignedLeft(_titleLabel.TextAlign))
            {
                labelLeft = leftLimit;
                labelWidth = Math.Max(0, rightLimit - leftLimit);
            }
            else if (IsTitleAlignedRight(_titleLabel.TextAlign))
            {
                labelLeft = 0;
                labelWidth = Math.Max(0, rightLimit);
            }
            else
            {
                labelLeft = 0;
                labelWidth = Width;
            }

            _titleLabel.Bounds = new Rectangle(
                labelLeft,
                0,
                labelWidth,
                Height);
        }

        private bool IsTitleAlignedLeft(ContentAlignment alignment)
        {
            return alignment == ContentAlignment.TopLeft ||
                   alignment == ContentAlignment.MiddleLeft ||
                   alignment == ContentAlignment.BottomLeft;
        }

        private bool IsTitleAlignedRight(ContentAlignment alignment)
        {
            return alignment == ContentAlignment.TopRight ||
                   alignment == ContentAlignment.MiddleRight ||
                   alignment == ContentAlignment.BottomRight;
        }

        private int PositionButtonFromRight(Button button, int right)
        {
            if (!button.Visible)
                return right;

            button.Location = new Point(right - ButtonSize.Width, 0);
            button.BringToFront();

            return right - ButtonSize.Width;
        }

        private void AttachToParentFormWhenReady()
        {
            Form form = FindForm();

            if (form == null)
            {
                RunWhenHandleReady(AttachToParentFormWhenReady);
                return;
            }

            if (_parentForm == form)
            {
                UpdateMaximizeButton(form.WindowState == FormWindowState.Maximized);
                return;
            }

            UnwireParentFormEvents();

            _parentForm = form;
            _parentForm.Resize += OnParentFormResize;

            UpdateMaximizeButton(_parentForm.WindowState == FormWindowState.Maximized);
        }

        private void UnwireParentFormEvents()
        {
            if (_parentForm == null)
                return;

            _parentForm.Resize -= OnParentFormResize;
            _parentForm = null;
        }

        private void RunWhenHandleReady(Action action)
        {
            if (action == null)
                return;

            if (IsDisposed)
                return;

            if (IsHandleCreated)
            {
                BeginInvoke(action);
                return;
            }

            EventHandler handler = null;

            handler = delegate
            {
                HandleCreated -= handler;

                if (!IsDisposed && IsHandleCreated)
                    BeginInvoke(action);
            };

            HandleCreated += handler;
        }

        private void ToggleMaximize()
        {
            if (_parentForm == null || !_allowWindowSnapAndMaximize)
                return;

            _parentForm.WindowState = _parentForm.WindowState == FormWindowState.Maximized
                ? FormWindowState.Normal
                : FormWindowState.Maximized;
        }

        private void OnTitleBarResize(object sender, EventArgs e)
        {
            UpdateLayout();
        }

        private void OnTitleBarParentChanged(object sender, EventArgs e)
        {
            AttachToParentFormWhenReady();
        }

        private void OnTitleBarHandleCreated(object sender, EventArgs e)
        {
            AttachToParentFormWhenReady();
        }

        private void OnParentFormResize(object sender, EventArgs e)
        {
            Form form = _parentForm ?? FindForm();

            if (form == null)
                return;

            _parentForm = form;
            UpdateMaximizeButton(form.WindowState == FormWindowState.Maximized);
        }

        private void OnMinimizeButtonClick(object sender, EventArgs e)
        {
            if (_parentForm != null)
                _parentForm.WindowState = FormWindowState.Minimized;
        }

        private void OnMaximizeButtonClick(object sender, EventArgs e)
        {
            ToggleMaximize();
        }

        private void OnCloseButtonClick(object sender, EventArgs e)
        {
            if (_parentForm != null)
                _parentForm.Close();
        }
    }
}