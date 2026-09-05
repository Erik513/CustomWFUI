using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Helpers;

namespace ErikwnkWFUI.Forms
{
    /// <summary>
    /// A <see cref="Form"/> with ErikwnkWFUI's own custom-drawn title bar
    /// (<see cref="TitleBar"/>) instead of the native Windows one, and
    /// optional borderless resizing. This is the base class every ErikwnkWFUI
    /// window is built on - either use it directly (<c>new StyledForm(title)</c>
    /// for a quick window, or the <see cref="StyledFormOptions"/> constructor
    /// for full control), or derive from it the way
    /// <see cref="MessageBoxForm"/> and <see cref="UpdateAvailableForm"/>
    /// do. Add your own controls to <see cref="ContentPanel"/>, not directly
    /// to the form - the title bar already occupies the top of the form's
    /// own <see cref="Control.Controls"/> collection.
    /// </summary>
    public class StyledForm : Form
    {
        private readonly TitleBarControl _titleBar;
        private readonly Panel _contentPanel;
        private readonly FlowLayoutPanel _versionStrip;
        private readonly Label _versionLabel;
        private readonly BorderlessResizeHandler _resizeHandler;

        /// <summary>The panel below the title bar - add your own UI here.</summary>
        public Panel ContentPanel
        {
            get { return _contentPanel; }
        }

        /// <summary>
        /// The reserved bottom-right strip holding <see cref="VersionLabel"/>,
        /// or null when it wasn't reserved at all (see
        /// <see cref="StyledFormOptions.VersionText"/>). Flows right-to-left,
        /// so <c>VersionStrip.Controls.Add(someControl)</c> places it
        /// directly to the left of the version text - e.g. an
        /// <see cref="AppUpdater.CreateUpdateAvailableButton"/> button.
        /// </summary>
        public FlowLayoutPanel VersionStrip
        {
            get { return _versionStrip; }
        }

        /// <summary>
        /// The reserved bottom-right version strip, or null when
        /// <see cref="StyledFormOptions.VersionText"/> wasn't set - reach
        /// through here to restyle it (font, color) after construction.
        /// </summary>
        public Label VersionLabel
        {
            get { return _versionLabel; }
        }

        /// <summary>
        /// The title bar itself - reach through here for anything not
        /// already proxied by <see cref="FormTitle"/>/<see cref="FormIcon"/>,
        /// e.g. <c>TitleBar.TitleForeColor</c>.
        /// </summary>
        public TitleBarControl TitleBar
        {
            get { return _titleBar; }
        }

        /// <summary>Shorthand for <c>TitleBar.Title</c>.</summary>
        public string FormTitle
        {
            get { return _titleBar.Title; }
            set { _titleBar.Title = value ?? ""; }
        }

        /// <summary>Shorthand for <c>TitleBar.IconImage</c> - the small logo shown at the top-left of the title bar, not the OS-level icon (see <see cref="StyledFormOptions.WindowIcon"/> for that).</summary>
        public Image FormIcon
        {
            get { return _titleBar.IconImage; }
            set { _titleBar.IconImage = value; }
        }

        /// <summary>
        /// Builds the form from a full <see cref="StyledFormOptions"/> - use
        /// <see cref="StyledFormOptions.CreateStandard"/> or
        /// <see cref="StyledFormOptions.CreateDialog"/> as a starting point.
        /// Passing null is the same as <c>StyledFormOptions.CreateStandard()</c>.
        /// </summary>
        public StyledForm(StyledFormOptions options = null)
        {
            options = options ?? StyledFormOptions.CreateStandard();

            Text = options.Title;

            if (options.WindowIcon != null)
            {
                Icon = options.WindowIcon;
                ShowIcon = true;
            }
            else
            {
                try
                {
                    Icon extractedIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

                    if (extractedIcon != null)
                    {
                        Icon = extractedIcon;
                        ShowIcon = true;
                    }
                }
                catch
                {
                }
            }

            if (options.Borderless)
                FormBorderStyle = FormBorderStyle.None;

            DoubleBuffered = true;
            BackColor = UIColors.BackgroundBlack;
            
            int borderSize = 0;
            if (options.Borderless)
                borderSize = options.Resizable ? 2 : 1;
            Padding = new Padding(borderSize);

            _titleBar = new TitleBarControl(
                options.Icon,
                options.Title,
                options.TitleTextAlign,
                options.ShowMinimizeButton,
                options.ShowMaximizeButton,
                options.ShowCloseButton,
                options.AllowWindowSnapAndMaximize,
                options.TitleBarBackColor);

            _contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = UIColors.BackgroundMedium,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };

            string versionText = options.VersionText;
            if (string.IsNullOrEmpty(versionText) && options.Type == StyledFormType.Settings)
                versionText = GetEntryAssemblyVersionText();

            if (!string.IsNullOrEmpty(versionText))
            {
                _versionLabel = UILabelFactory.CreateMuted(versionText);
                _versionLabel.AutoSize = true;
                _versionLabel.TextAlign = ContentAlignment.MiddleRight;
                _versionLabel.Margin = new Padding(0, 4, 0, 0);

                _versionStrip = new FlowLayoutPanel
                {
                    Dock = DockStyle.Bottom,
                    Height = 24,
                    FlowDirection = FlowDirection.RightToLeft,
                    WrapContents = false,
                    AutoSize = false,
                    Margin = new Padding(0),
                    Padding = new Padding(0, 0, 10, 0),
                    // Explicit, not left to the transparent-background
                    // ancestor-walk every OwnerDrawLabel does - the strip's
                    // own edges (outside the label's now AutoSize width)
                    // would otherwise show through with the FlowLayoutPanel's
                    // default gray SystemColors.Control instead of matching
                    // the rest of the window.
                    BackColor = UIColors.BackgroundBlack
                };
                _versionStrip.Controls.Add(_versionLabel);
            }

            Controls.Add(_contentPanel);
            Controls.Add(_titleBar);

            if (_versionStrip != null)
                Controls.Add(_versionStrip);

            if (options.Borderless && options.Resizable)
                _resizeHandler = new BorderlessResizeHandler(this);
        }
        /// <summary>Quick-start constructor for the common case - just a title and whether it behaves like a dialog (no minimize/maximize, fixed size), a settings screen (same, plus an automatic version strip), or a standard resizable window.</summary>
        public StyledForm(
            string title,
            StyledFormType type = StyledFormType.Standard)
            : this(type == StyledFormType.Dialog
                ? StyledFormOptions.CreateDialog(title)
                : type == StyledFormType.Settings
                    ? StyledFormOptions.CreateSettings(title)
                    : StyledFormOptions.CreateStandard(title))
        {
        }


        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (_resizeHandler != null)
                _resizeHandler.TryHandleMessage(ref m);
        }

        // Entry assembly, not the executing one - the executing assembly
        // here would be ErikwnkWFUI.dll itself, never the consuming app.
        // Falls back to the executing assembly only for the unusual hosts
        // (e.g. some test runners) where GetEntryAssembly() returns null.
        private static string GetEntryAssemblyVersionText()
        {
            Assembly assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            Version version = assembly.GetName().Version;

            return version != null ? "v" + version.ToString(3) : null;
        }
    }

    /// <summary>Which <see cref="StyledFormOptions"/> preset a <see cref="StyledForm"/> should start from.</summary>
    public enum StyledFormType
    {
        Standard,
        Dialog,
        /// <summary>Like <see cref="Dialog"/>, but automatically shows the entry assembly's version in the reserved bottom-right strip - see <see cref="StyledFormOptions.CreateSettings"/>.</summary>
        Settings
    }

    /// <summary>
    /// Full set of knobs for a <see cref="StyledForm"/>. Build one with
    /// <see cref="CreateStandard"/> or <see cref="CreateDialog"/> and tweak
    /// individual properties from there, rather than constructing it with
    /// <c>new StyledFormOptions { ... }</c> directly - the two factory
    /// methods keep related properties in sync (e.g. a dialog has no
    /// maximize button AND can't be snapped/maximized by dragging).
    /// </summary>
    public class StyledFormOptions
    {
        public StyledFormType Type { get; set; } = StyledFormType.Standard;

        /// <summary>True (the default) removes the native Windows title bar/frame in favor of ErikwnkWFUI's own <see cref="TitleBarControl"/>.</summary>
        public bool Borderless { get; set; } = true;
        public int BorderSize { get; set; } = 1;
        /// <summary>Whether the window can be resized by dragging its edges - only meaningful when <see cref="Borderless"/> is true.</summary>
        public bool Resizable { get; set; } = true;

        public string Title { get; set; } = "";
        public ContentAlignment TitleTextAlign { get; set; } =
            ContentAlignment.MiddleCenter;

        /// <summary>The small logo shown at the top-left of the title bar. See <see cref="StyledForm.FormIcon"/> for the equivalent post-construction property.</summary>
        public Image Icon { get; set; } = null;

        public bool ShowMinimizeButton { get; set; } = true;
        public bool ShowMaximizeButton { get; set; } = true;
        public bool ShowCloseButton { get; set; } = true;

        /// <summary>Whether double-clicking the title bar or dragging it to the top of the screen maximizes the window - independent of <see cref="ShowMaximizeButton"/>, but the two built-in presets always keep them in sync.</summary>
        public bool AllowWindowSnapAndMaximize { get; set; } = true;

        public Color? TitleBarBackColor { get; set; } = UIStyles.Colors.BackgroundBlack;

        /// <summary>The OS-level icon (taskbar, Alt-Tab, system menu) - not the title bar logo, see <see cref="Icon"/> for that. Falls back to the running exe's own icon when left null.</summary>
        public Icon WindowIcon { get; set; } = null;

        /// <summary>
        /// When set, reserves a thin strip at the bottom of the window with
        /// this text right-aligned - typically the app version, e.g.
        /// "v1.0.0". Left null (the default), <see cref="Type"/> ==
        /// <see cref="StyledFormType.Settings"/> fills it in automatically
        /// from the entry assembly's own version; for any other
        /// <see cref="Type"/>, null just means no strip at all. Set this
        /// explicitly to override the automatic value (e.g. a custom
        /// format, or a version string from a different assembly).
        /// </summary>
        public string VersionText { get; set; } = null;

        /// <summary>A normal, resizable window with minimize/maximize/close.</summary>
        public static StyledFormOptions CreateStandard(string title = "", ContentAlignment titleTextAlign = ContentAlignment.MiddleCenter, Color? backColor = null, Image icon = null, Icon windowIcon = null)
        {
            return new StyledFormOptions
            {
                Type = StyledFormType.Standard,
                Borderless = true,
                Resizable = true,
                Title = title,
                TitleTextAlign = titleTextAlign,
                TitleBarBackColor = backColor,
                Icon = icon,
                WindowIcon = windowIcon,
                ShowMinimizeButton = true,
                ShowMaximizeButton = true,
                ShowCloseButton = true,
                AllowWindowSnapAndMaximize = true
            };
        }

        /// <summary>A fixed-size dialog: no minimize/maximize, can't be snapped or maximized by dragging.</summary>
        public static StyledFormOptions CreateDialog(string title = "", ContentAlignment titleTextAlign = ContentAlignment.MiddleCenter, Color? backColor = null, Image icon = null, Icon windowIcon = null)
        {
            return new StyledFormOptions
            {
                Type = StyledFormType.Dialog,
                Borderless = true,
                Resizable = false,
                Title = title,
                TitleTextAlign = titleTextAlign,
                TitleBarBackColor = backColor,
                Icon = icon,
                WindowIcon = windowIcon,
                ShowMinimizeButton = false,
                ShowMaximizeButton = false,
                ShowCloseButton = true,
                AllowWindowSnapAndMaximize = false
            };
        }

        /// <summary>
        /// A fixed-size dialog like <see cref="CreateDialog"/>, but marked
        /// as a settings screen: <see cref="StyledForm"/> automatically
        /// shows the entry assembly's version (e.g. "v1.0.0") in a reserved
        /// bottom-right strip, without the caller having to look up or pass
        /// a version string itself. Use this for every "Settings" window -
        /// that's what makes the version strip show up consistently across
        /// all of them instead of each app wiring it up separately.
        /// </summary>
        public static StyledFormOptions CreateSettings(string title = "", ContentAlignment titleTextAlign = ContentAlignment.MiddleLeft, Color? backColor = null, Image icon = null, Icon windowIcon = null)
        {
            StyledFormOptions options = CreateDialog(title, titleTextAlign, backColor, icon, windowIcon);
            options.Type = StyledFormType.Settings;
            return options;
        }
    }
}