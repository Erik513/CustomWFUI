using System.Drawing;
using System.Windows.Forms;
using CustomWFUI.Controls;
using CustomWFUI.Styles;
using CustomWFUI.Helpers;

namespace CustomWFUI.Forms
{
    public class StyledForm : Form
    {
        private readonly TitleBarControl _titleBar;
        private readonly Panel _contentPanel;
        private readonly BorderlessResizeHandler _resizeHandler;

        public Panel ContentPanel
        {
            get { return _contentPanel; }
        }

        public TitleBarControl TitleBar
        {
            get { return _titleBar; }
        }

        public string FormTitle
        {
            get { return _titleBar.Title; }
            set { _titleBar.Title = value ?? ""; }
        }

        public Image FormIcon
        {
            get { return _titleBar.IconImage; }
            set { _titleBar.IconImage = value; }
        }

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

    public enum StyledFormType
    {
        Standard,
        Dialog
    }
    public class StyledFormOptions
    {
        public StyledFormType Type { get; set; } = StyledFormType.Standard;

        public bool Borderless { get; set; } = true;
        public int BorderSize { get; set; } = 1;
        public bool Resizable { get; set; } = true;

        public string Title { get; set; } = "";
        public ContentAlignment TitleTextAlign { get; set; } =
            ContentAlignment.MiddleCenter;

        public Image Icon { get; set; } = null;

        public bool ShowMinimizeButton { get; set; } = true;
        public bool ShowMaximizeButton { get; set; } = true;
        public bool ShowCloseButton { get; set; } = true;

        public bool AllowWindowSnapAndMaximize { get; set; } = true;

        public Color? TitleBarBackColor { get; set; } = UIStyles.Colors.BackgroundBlack;

        public Icon WindowIcon { get; set; } = null;    

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