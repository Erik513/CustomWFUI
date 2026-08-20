using System.Drawing;
using System.Windows.Forms;
using CustomWFUI.Controls;
using CustomWFUI.Styles;
using CustomWFUI.Helpers;

namespace CustomWFUI.Forms
{
    /// <summary>
    /// A <see cref="Form"/> with CustomWFUI's own custom-drawn title bar
    /// (<see cref="TitleBar"/>) instead of the native Windows one, and
    /// optional borderless resizing. This is the base class every CustomWFUI
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
        private readonly BorderlessResizeHandler _resizeHandler;

        /// <summary>The panel below the title bar - add your own UI here.</summary>
        public Panel ContentPanel
        {
            get { return _contentPanel; }
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

            Controls.Add(_contentPanel);
            Controls.Add(_titleBar);

            if (options.Borderless && options.Resizable)
                _resizeHandler = new BorderlessResizeHandler(this);
        }
        /// <summary>Quick-start constructor for the common case - just a title and whether it behaves like a dialog (no minimize/maximize, fixed size) or a standard resizable window.</summary>
        public StyledForm(
            string title,
            StyledFormType type = StyledFormType.Standard)
            : this(type == StyledFormType.Dialog
                ? StyledFormOptions.CreateDialog(title)
                : StyledFormOptions.CreateStandard(title))
        {
        }


        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (_resizeHandler != null)
                _resizeHandler.TryHandleMessage(ref m);
        }
    }

    /// <summary>Which <see cref="StyledFormOptions"/> preset a <see cref="StyledForm"/> should start from.</summary>
    public enum StyledFormType
    {
        Standard,
        Dialog
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

        /// <summary>True (the default) removes the native Windows title bar/frame in favor of CustomWFUI's own <see cref="TitleBarControl"/>.</summary>
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
    }
}