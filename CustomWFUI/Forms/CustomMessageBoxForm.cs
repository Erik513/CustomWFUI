using System;
using System.Drawing;
using System.Windows.Forms;
using CustomWFUI.Factories;
using CustomWFUI.Styles;

namespace CustomWFUI.Forms
{
    public enum CustomMessageBoxButtons
    {
        OK,
        OKCancel,
        YesNo,
        YesNoCancel
    }

    public enum CustomMessageBoxIcon
    {
        None,
        Info,
        Warning,
        Error,
        Question,
        Success
    }

    public static class CustomMessageBox
    {
        public static DialogResult Show(
            string message,
            string title = "Hinweis",
            CustomMessageBoxButtons buttons = CustomMessageBoxButtons.OK,
            CustomMessageBoxIcon icon = CustomMessageBoxIcon.Info,
            Form owner = null)
        {
            CustomMessageBoxForm form = new CustomMessageBoxForm(message, title, buttons, icon);

            try
            {
                if (owner != null)
                    return form.ShowDialog(owner);

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
        private const int FormWidth = 500;
        private const int FormHeight = 220;
        private const int ButtonPanelHeight = 70;
        private const int ButtonColumnWidth = 125;
        private const int DialogButtonWidth = 120;
        private const int DialogButtonHeight = 35;

        private readonly string _message;
        private readonly CustomMessageBoxButtons _buttons;

        public CustomMessageBoxForm(
            string message,
            string title,
            CustomMessageBoxButtons buttons,
            CustomMessageBoxIcon icon)
            : base(StyledFormOptions.CreateDialog(title, GetTitleBarIcon(icon)))
        {
            _message = message ?? "";
            _buttons = buttons;

            ConfigureForm();
            BuildLayout();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            EnsureDialogResult();

            base.OnFormClosing(e);
        }

        private void ConfigureForm()
        {
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;

            Size = new Size(FormWidth, FormHeight);
            MinimumSize = Size;
            MaximumSize = Size;
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
            TableLayoutPanel layout = UITableLayoutPanelFactory.CreateStandard(1, 2);

            layout.Dock = DockStyle.Fill;
            layout.BackColor = UIStyles.Colors.BackgroundMedium;
            layout.Padding = new Padding(0);
            layout.Margin = new Padding(0);

            layout.RowStyles.Clear();
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, ButtonPanelHeight));

            return layout;
        }

        private Control CreateContentPanel()
        {
            Panel panel = UIPanelFactory.CreateMedium();
            panel.Padding = new Padding(28, 20, 28, 10);

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
            TableLayoutPanel buttonPanel = UITableLayoutPanelFactory.CreateStandard(1, 1);

            buttonPanel.Dock = DockStyle.Fill;
            buttonPanel.BackColor = UIColors.BackgroundMedium;
            buttonPanel.Padding = new Padding(12, 12, 28, 18);
            buttonPanel.Margin = new Padding(0);

            AddButtonsToPanel(buttonPanel);

            return buttonPanel;
        }

        private void AddButtonsToPanel(TableLayoutPanel buttonPanel)
        {
            DialogButtonInfo[] buttonInfos = GetButtons();

            buttonPanel.ColumnCount = buttonInfos.Length + 1;
            buttonPanel.ColumnStyles.Clear();
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            buttonPanel.RowStyles.Clear();
            buttonPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            for (int i = 0; i < buttonInfos.Length; i++)
            {
                buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, ButtonColumnWidth));

                Button button = CreateDialogButton(buttonInfos[i].Text, buttonInfos[i].Result);
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
            Size size = new Size(DialogButtonWidth, DialogButtonHeight);

            if (result == DialogResult.OK || result == DialogResult.Yes)
                return UIButtonFactory.CreateGreen(text, "", size);

            if (result == DialogResult.No)
                return UIButtonFactory.CreateDanger(text, "", size);

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
                        new DialogButtonInfo("Abbrechen", DialogResult.Cancel)
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
                        new DialogButtonInfo("Abbrechen", DialogResult.Cancel)
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