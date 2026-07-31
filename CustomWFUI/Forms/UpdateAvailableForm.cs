using System;
using System.Drawing;
using System.Windows.Forms;
using CustomWFUI.Factories;
using CustomWFUI.Styles;

namespace CustomWFUI.Forms
{
    public static class UpdatePrompt
    {
        /// <summary>
        /// Shows a standardized "a new version is available" dialog. Returns true when
        /// the user chose to update, false when they dismissed it for now.
        /// </summary>
        public static bool ShowUpdateAvailable(
            string currentVersion,
            string latestVersion,
            Form owner = null,
            string title = "Update available")
        {
            UpdateAvailableForm form = new UpdateAvailableForm(
                currentVersion,
                latestVersion,
                title);

            try
            {
                DialogResult result = owner != null
                    ? form.ShowDialog(owner)
                    : form.ShowDialog();

                return result == DialogResult.Yes;
            }
            finally
            {
                form.Dispose();
            }
        }
    }

    public class UpdateAvailableForm : StyledForm
    {
        private static readonly Size DialogSize = new Size(420, 190);

        private readonly string _currentVersion;
        private readonly string _latestVersion;

        public UpdateAvailableForm(
            string currentVersion,
            string latestVersion,
            string title = "Update available")
            : base(StyledFormOptions.CreateDialog(
                title: title,
                titleTextAlign: ContentAlignment.MiddleLeft,
                backColor: UIStyles.Colors.BackgroundBlack,
                icon: SystemIcons.Information.ToBitmap()))
        {
            _currentVersion = currentVersion ?? "";
            _latestVersion = latestVersion ?? "";

            ConfigureForm();
            BuildLayout();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (DialogResult == DialogResult.None)
                DialogResult = DialogResult.No;

            base.OnFormClosing(e);
        }

        private void ConfigureForm()
        {
            StartPosition = FormStartPosition.CenterScreen;
            ShowInTaskbar = false;

            Size = DialogSize;
            MinimumSize = DialogSize;
            MaximumSize = DialogSize;
        }

        private void BuildLayout()
        {
            Panel rootPanel = UIStyles.Panels.CreateMedium();

            TableLayoutPanel mainLayout = UITableLayoutPanelFactory.CreateStandard(1, 2);
            mainLayout.Dock = DockStyle.Fill;
            mainLayout.BackColor = UIColors.BackgroundMedium;
            mainLayout.Padding = new Padding(0);
            mainLayout.Margin = new Padding(0);
            mainLayout.RowStyles.Clear();
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));

            mainLayout.Controls.Add(CreateContentPanel(), 0, 0);
            mainLayout.Controls.Add(CreateButtonPanel(), 0, 1);

            rootPanel.Controls.Add(mainLayout);

            ContentPanel.Controls.Clear();
            ContentPanel.Controls.Add(rootPanel);
        }

        private Control CreateContentPanel()
        {
            Panel panel = UIPanelFactory.CreateMedium();
            panel.Padding = new Padding(24, 18, 24, 8);

            FlowLayoutPanel layout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Color.Transparent
            };

            Label headline = UILabelFactory.CreateNormal("A new version is available.");
            headline.AutoSize = true;
            headline.Font = UIFonts.Title;
            headline.ForeColor = UIColors.TextPrimary;
            headline.Margin = new Padding(0, 0, 0, 10);

            Label versionLine = UILabelFactory.CreateNormal(
                "v" + _currentVersion + "  →  v" + _latestVersion);
            versionLine.AutoSize = true;
            versionLine.Font = UIFonts.Normal;
            versionLine.ForeColor = UIColors.TextSecondary;

            layout.Controls.Add(headline);
            layout.Controls.Add(versionLine);
            panel.Controls.Add(layout);

            return panel;
        }

        private Control CreateButtonPanel()
        {
            TableLayoutPanel buttonPanel = UITableLayoutPanelFactory.CreateStandard(3, 1);
            buttonPanel.Dock = DockStyle.Fill;
            buttonPanel.BackColor = UIColors.BackgroundMedium;
            buttonPanel.Padding = new Padding(12, 12, 24, 18);
            buttonPanel.Margin = new Padding(0);

            buttonPanel.ColumnStyles.Clear();
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));

            buttonPanel.RowStyles.Clear();
            buttonPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            Button laterButton = UIButtonFactory.CreateStandard(
                "Later", "", new Size(80, 30));
            laterButton.Dock = DockStyle.Fill;
            laterButton.Margin = new Padding(0, 0, 6, 0);
            laterButton.DialogResult = DialogResult.No;
            laterButton.Click += OnButtonClick;
            CancelButton = laterButton;

            Button updateButton = UIButtonFactory.CreateGreen(
                "Update now", "", new Size(100, 30));
            updateButton.Dock = DockStyle.Fill;
            updateButton.Margin = new Padding(6, 0, 0, 0);
            updateButton.DialogResult = DialogResult.Yes;
            updateButton.Click += OnButtonClick;
            AcceptButton = updateButton;

            buttonPanel.Controls.Add(laterButton, 1, 0);
            buttonPanel.Controls.Add(updateButton, 2, 0);

            return buttonPanel;
        }

        private void OnButtonClick(object sender, EventArgs e)
        {
            Button button = sender as Button;

            if (button == null)
                return;

            DialogResult = button.DialogResult;
            Close();
        }
    }
}
