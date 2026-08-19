using System;
using System.Drawing;
using System.Windows.Forms;
using CustomWFUI.Factories;
using CustomWFUI.Styles;

namespace CustomWFUI.Forms
{
    /// <summary>Which buttons a <see cref="CustomMessageBox"/> shows.</summary>
    public enum CustomMessageBoxButtons
    {
        OK,
        OKCancel,
        YesNo,
        YesNoCancel
    }

    /// <summary>Which icon a <see cref="CustomMessageBox"/> shows in its title bar.</summary>
    public enum CustomMessageBoxIcon
    {
        None,
        Info,
        Warning,
        Error,
        Question,
        Success
    }

    /// <summary>Overall size preset for a <see cref="CustomMessageBox"/>.</summary>
    public enum CustomMessageBoxSize
    {
        Small,
        Medium,
        Large
    }

    /// <summary>CustomWFUI's themed replacement for <see cref="MessageBox"/> - call <see cref="Show"/>.</summary>
    public static class CustomMessageBox
    {
        /// <summary>Shows the dialog modally and returns which button was clicked (or the safe default - Cancel/No/OK - if the dialog is closed without clicking one).</summary>
        public static DialogResult Show(
            string message,
            string title = "Notice",
            CustomMessageBoxButtons buttons = CustomMessageBoxButtons.OK,
            CustomMessageBoxIcon icon = CustomMessageBoxIcon.Info,
            Form owner = null,
            CustomMessageBoxSize size = CustomMessageBoxSize.Medium)
        {
            CustomMessageBoxForm form = new CustomMessageBoxForm(
                message,
                title,
                buttons,
                icon,
                size);

            try
            {
                if (owner != null)
                    return form.ShowDialog(owner);

                // The constructor sets StartPosition = CenterParent, which
                // only actually centers when ShowDialog is given an owner -
                // called without one (e.g. a "this app is already running"
                // notice shown before any main form exists to own it), it
                // falls back to the top-left corner instead of the screen.
                form.StartPosition = FormStartPosition.CenterScreen;
                return form.ShowDialog();
            }
            finally
            {
                form.Dispose();
            }
        }
    }

    public class CustomMessageBoxForm : StyledForm
    {
        private readonly string _message;
        private readonly CustomMessageBoxButtons _buttons;
        private readonly CustomMessageBoxSize _size;

        private sealed class MessageBoxPreset
        {
            public Size FormSize { get; set; }
            public int ButtonPanelHeight { get; set; }
            public int ButtonColumnWidth { get; set; }
            public int ButtonWidth { get; set; }
            public int ButtonHeight { get; set; }
            public Padding ContentPadding { get; set; }
            public Padding ButtonPanelPadding { get; set; }
        }

        public CustomMessageBoxForm(
            string message,
            string title,
            CustomMessageBoxButtons buttons,
            CustomMessageBoxIcon icon,
            CustomMessageBoxSize size = CustomMessageBoxSize.Medium)
            : base(StyledFormOptions.CreateDialog(
                title: title,
                titleTextAlign: ContentAlignment.MiddleLeft,
                backColor: UIStyles.Colors.BackgroundBlack,
                icon: GetTitleBarIcon(icon)))
        {
            _message = message ?? "";
            _buttons = buttons;
            _size = size;

            ConfigureForm();
            BuildLayout();

            // Whichever button was added first (Yes, for YesNo) otherwise
            // silently ends up with the initial keyboard focus via normal
            // tab order - meaning a stray Space press confirms the
            // destructive option instead of doing nothing, since Space
            // activates whatever's focused (unlike Enter, which respects
            // AcceptButton). Defaulting focus to CancelButton (No/Cancel)
            // makes an accidental key press safe instead. ActiveControl,
            // not Control.Focus() - Focus() requires the top-level form to
            // already be the active window, which isn't guaranteed yet
            // this early; ActiveControl is the documented way to set a
            // form's initial focus before it's actually shown.
            if (CancelButton is Control cancelControl)
                ActiveControl = cancelControl;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            EnsureDialogResult();
            base.OnFormClosing(e);
        }

        private MessageBoxPreset GetPreset()
        {
            switch (_size)
            {
                case CustomMessageBoxSize.Small:
                    return new MessageBoxPreset
                    {
                        FormSize = new Size(380, 160),
                        ButtonPanelHeight = 52,
                        ButtonColumnWidth = 78,
                        ButtonWidth = 72,
                        ButtonHeight = 28,
                        ContentPadding = new Padding(18, 12, 18, 8),
                        ButtonPanelPadding = new Padding(8, 8, 18, 12)
                    };

                case CustomMessageBoxSize.Large:
                    return new MessageBoxPreset
                    {
                        FormSize = new Size(700, 320),
                        ButtonPanelHeight = 80,
                        ButtonColumnWidth = 140,
                        ButtonWidth = 130,
                        ButtonHeight = 40,
                        ContentPadding = new Padding(32, 26, 32, 14),
                        ButtonPanelPadding = new Padding(12, 16, 32, 22)
                    };

                default:
                    return new MessageBoxPreset
                    {
                        FormSize = new Size(500, 220),
                        ButtonPanelHeight = 70,
                        ButtonColumnWidth = 125,
                        ButtonWidth = 120,
                        ButtonHeight = 35,
                        ContentPadding = new Padding(28, 20, 28, 10),
                        ButtonPanelPadding = new Padding(12, 12, 28, 18)
                    };
            }
        }

        private void ConfigureForm()
        {
            MessageBoxPreset preset = GetPreset();

            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;

            Size = preset.FormSize;
            MinimumSize = preset.FormSize;
            MaximumSize = preset.FormSize;
        }

        private void BuildLayout()
        {
            Panel rootPanel = UIStyles.Panels.CreateMedium();
            TableLayoutPanel mainLayout = CreateMainLayout();

            mainLayout.Controls.Add(CreateContentPanel(), 0, 0);
            mainLayout.Controls.Add(CreateButtonPanel(), 0, 1);

            rootPanel.Controls.Add(mainLayout);

            ContentPanel.Controls.Clear();
            ContentPanel.Controls.Add(rootPanel);
        }

        private TableLayoutPanel CreateMainLayout()
        {
            MessageBoxPreset preset = GetPreset();

            TableLayoutPanel layout = UITableLayoutPanelFactory.CreateStandard(1, 2);

            layout.Dock = DockStyle.Fill;
            layout.BackColor = UIStyles.Colors.BackgroundMedium;
            layout.Padding = new Padding(0);
            layout.Margin = new Padding(0);

            layout.RowStyles.Clear();
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, preset.ButtonPanelHeight));

            return layout;
        }

        private Control CreateContentPanel()
        {
            MessageBoxPreset preset = GetPreset();

            Panel panel = UIPanelFactory.CreateMedium();
            panel.Padding = preset.ContentPadding;

            Label messageLabel = CreateMessageLabel();

            panel.Controls.Add(messageLabel);

            return panel;
        }

        private Label CreateMessageLabel()
        {
            Label label = UILabelFactory.CreateNormal(_message);

            label.Dock = DockStyle.Fill;
            label.TextAlign = ContentAlignment.MiddleCenter;
            label.ForeColor = UIStyles.Colors.TextPrimary;
            label.Font = UIStyles.Fonts.Normal;
            label.BackColor = Color.Transparent;
            label.AutoEllipsis = false;

            return label;
        }

        private Control CreateButtonPanel()
        {
            MessageBoxPreset preset = GetPreset();

            TableLayoutPanel buttonPanel = UITableLayoutPanelFactory.CreateStandard(1, 1);

            buttonPanel.Dock = DockStyle.Fill;
            buttonPanel.BackColor = UIColors.BackgroundMedium;
            buttonPanel.Padding = preset.ButtonPanelPadding;
            buttonPanel.Margin = new Padding(0);

            AddButtonsToPanel(buttonPanel);

            return buttonPanel;
        }

        private void AddButtonsToPanel(TableLayoutPanel buttonPanel)
        {
            MessageBoxPreset preset = GetPreset();
            DialogButtonInfo[] buttonInfos = GetButtons();

            buttonPanel.ColumnCount = buttonInfos.Length + 1;
            buttonPanel.ColumnStyles.Clear();
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            buttonPanel.RowStyles.Clear();
            buttonPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            for (int i = 0; i < buttonInfos.Length; i++)
            {
                buttonPanel.ColumnStyles.Add(
                    new ColumnStyle(
                        SizeType.Absolute,
                        preset.ButtonColumnWidth));

                Button button = CreateDialogButton(
                    buttonInfos[i].Text,
                    buttonInfos[i].Result);

                buttonPanel.Controls.Add(button, i + 1, 0);
            }
        }

        private Button CreateDialogButton(string text, DialogResult result)
        {
            Button button = CreateStyledButton(text, result);

            button.Dock = DockStyle.Fill;
            button.Margin = new Padding(6, 0, 0, 0);
            button.DialogResult = result;
            button.Click += OnDialogButtonClick;

            ConfigureAcceptCancelButton(button, result);

            return button;
        }

        private Button CreateStyledButton(string text, DialogResult result)
        {
            MessageBoxPreset preset = GetPreset();

            Size size = new Size(
                preset.ButtonWidth,
                preset.ButtonHeight);

            if (result == DialogResult.OK || result == DialogResult.Yes)
                return UIButtonFactory.CreateGreen(text, "", size);

            if (result == DialogResult.No)
                return UIButtonFactory.CreateRed(text, "", size);

            return UIButtonFactory.CreateStandard(text, "", size);
        }

        private void ConfigureAcceptCancelButton(Button button, DialogResult result)
        {
            if (result == DialogResult.OK || result == DialogResult.Yes)
                AcceptButton = button;

            if (result == DialogResult.Cancel || result == DialogResult.No)
                CancelButton = button;
        }

        private void OnDialogButtonClick(object sender, EventArgs e)
        {
            Button button = sender as Button;

            if (button == null)
                return;

            DialogResult = button.DialogResult;
            Close();
        }

        private DialogButtonInfo[] GetButtons()
        {
            switch (_buttons)
            {
                case CustomMessageBoxButtons.OK:
                    return new[]
                    {
                        new DialogButtonInfo("✓", DialogResult.OK)
                    };

                case CustomMessageBoxButtons.OKCancel:
                    return new[]
                    {
                        new DialogButtonInfo("✓", DialogResult.OK),
                        new DialogButtonInfo(UIStrings.Get("MessageBox.Cancel"), DialogResult.Cancel)
                    };

                case CustomMessageBoxButtons.YesNo:
                    return new[]
                    {
                        new DialogButtonInfo("✓", DialogResult.Yes),
                        new DialogButtonInfo("✖", DialogResult.No)
                    };

                case CustomMessageBoxButtons.YesNoCancel:
                    return new[]
                    {
                        new DialogButtonInfo("✓", DialogResult.Yes),
                        new DialogButtonInfo("✖", DialogResult.No),
                        new DialogButtonInfo(UIStrings.Get("MessageBox.Cancel"), DialogResult.Cancel)
                    };

                default:
                    return new[]
                    {
                        new DialogButtonInfo("✓", DialogResult.OK)
                    };
            }
        }

        private void EnsureDialogResult()
        {
            if (DialogResult != DialogResult.None)
                return;

            DialogResult = GetDefaultDialogResult();
        }

        private DialogResult GetDefaultDialogResult()
        {
            switch (_buttons)
            {
                case CustomMessageBoxButtons.OKCancel:
                case CustomMessageBoxButtons.YesNoCancel:
                    return DialogResult.Cancel;

                case CustomMessageBoxButtons.YesNo:
                    return DialogResult.No;

                default:
                    return DialogResult.OK;
            }
        }

        private static Image GetTitleBarIcon(CustomMessageBoxIcon icon)
        {
            switch (icon)
            {
                case CustomMessageBoxIcon.Info:
                    return SystemIcons.Information.ToBitmap();

                case CustomMessageBoxIcon.Warning:
                    return SystemIcons.Warning.ToBitmap();

                case CustomMessageBoxIcon.Error:
                    return SystemIcons.Error.ToBitmap();

                case CustomMessageBoxIcon.Question:
                    return SystemIcons.Question.ToBitmap();

                case CustomMessageBoxIcon.Success:
                    return SystemIcons.Shield.ToBitmap();

                default:
                    return null;
            }
        }

        private struct DialogButtonInfo
        {
            public readonly string Text;
            public readonly DialogResult Result;

            public DialogButtonInfo(string text, DialogResult result)
            {
                Text = text;
                Result = result;
            }
        }
    }
}