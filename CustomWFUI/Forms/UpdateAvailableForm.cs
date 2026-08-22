using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Forms
{
    /// <summary>How an <see cref="UpdatePrompt.ShowUpdateAvailableAsync"/> dialog ended.</summary>
    public enum UpdateOutcome
    {
        /// <summary>Closed without clicking "Update now".</summary>
        Declined,
        /// <summary>applyUpdateAsync returned true.</summary>
        Applied,
        /// <summary>applyUpdateAsync returned false or threw.</summary>
        Failed
    }

    public static class UpdatePrompt
    {
        /// <summary>
        /// Shows a standardized "a new version is available" dialog. If the user
        /// clicks "Update now", the same window switches to a progress view and calls
        /// applyUpdateAsync, reporting its progress back into that view. Returns how
        /// the dialog ended.
        /// </summary>
        public static Task<UpdateOutcome> ShowUpdateAvailableAsync(
            string currentVersion,
            string latestVersion,
            Func<IProgress<int>, Task<bool>> applyUpdateAsync,
            Form owner = null,
            string title = null)
        {
            using (UpdateAvailableForm form = new UpdateAvailableForm(
                currentVersion,
                latestVersion,
                title ?? UIStrings.Get("UpdateAvailable.Title")))
            {
                UpdateOutcome outcome = UpdateOutcome.Declined;

                form.UpdateRequested += async (sender, e) =>
                {
                    form.ShowProgressState();
                    Progress<int> progress = new Progress<int>(form.SetProgress);

                    try
                    {
                        bool applied = await applyUpdateAsync(progress);
                        outcome = applied ? UpdateOutcome.Applied : UpdateOutcome.Failed;
                    }
                    catch
                    {
                        outcome = UpdateOutcome.Failed;
                    }

                    form.Close();
                };

                if (owner != null)
                {
                    form.ShowDialog(owner);
                }
                else
                {
                    form.ShowDialog();
                }

                return Task.FromResult(outcome);
            }
        }
    }

    /// <summary>The dialog behind <see cref="UpdatePrompt.ShowUpdateAvailableAsync"/> - prefer calling that over constructing this directly, it handles wiring <see cref="UpdateRequested"/> up to your download logic and reporting the outcome.</summary>
    public class UpdateAvailableForm : StyledForm
    {
        private static readonly Size DialogSize = new Size(420, 190);

        private readonly string _currentVersion;
        private readonly string _latestVersion;

        private Control _promptContent;
        private Control _buttonPanel;
        private Control _progressContent;
        private Label _progressStatusLabel;
        private ProgressBar _progressBar;
        private bool _updateRequested;

        /// <summary>Raised when "Update now" is clicked - call <see cref="ShowProgressState"/> and start downloading.</summary>
        public event EventHandler UpdateRequested;

        public UpdateAvailableForm(
            string currentVersion,
            string latestVersion,
            string title = null)
            : base(StyledFormOptions.CreateDialog(
                title: title ?? UIStrings.Get("UpdateAvailable.Title"),
                titleTextAlign: ContentAlignment.MiddleLeft,
                backColor: UIStyles.Colors.BackgroundBlack,
                icon: SystemIcons.Information.ToBitmap()))
        {
            _currentVersion = currentVersion ?? "";
            _latestVersion = latestVersion ?? "";

            ConfigureForm();
            BuildLayout();
        }

        /// <summary>
        /// Switches the window from the prompt (headline + buttons) to a progress
        /// view (status text + progress bar), without opening a second window.
        /// </summary>
        public void ShowProgressState()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(ShowProgressState));
                return;
            }

            ControlBox = false;
            _promptContent.Visible = false;
            _buttonPanel.Visible = false;
            _progressContent.Visible = true;
        }

        /// <summary>Updates the progress bar/status text - safe to call from a background download thread, marshals to the UI thread itself.</summary>
        public void SetProgress(int percent)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<int>(SetProgress), percent);
                return;
            }

            int clamped = Math.Max(0, Math.Min(100, percent));
            _progressBar.Value = clamped;
            _progressStatusLabel.Text = string.Format(UIStrings.Get("UpdateAvailable.Downloading"), clamped);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
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

            Panel contentHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            _promptContent = CreatePromptContent();
            _progressContent = CreateProgressContent();
            _progressContent.Visible = false;

            contentHost.Controls.Add(_progressContent);
            contentHost.Controls.Add(_promptContent);

            _buttonPanel = CreateButtonPanel();

            mainLayout.Controls.Add(contentHost, 0, 0);
            mainLayout.Controls.Add(_buttonPanel, 0, 1);

            rootPanel.Controls.Add(mainLayout);

            ContentPanel.Controls.Clear();
            ContentPanel.Controls.Add(rootPanel);
        }

        private Control CreatePromptContent()
        {
            Panel panel = UIPanelFactory.CreateMedium();
            panel.Dock = DockStyle.Fill;
            panel.Padding = new Padding(24, 18, 24, 8);

            FlowLayoutPanel layout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Color.Transparent
            };

            Label headline = UILabelFactory.CreateNormal(UIStrings.Get("UpdateAvailable.Message"));
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

        private Control CreateProgressContent()
        {
            Panel panel = UIPanelFactory.CreateMedium();
            panel.Dock = DockStyle.Fill;
            panel.Padding = new Padding(24, 18, 24, 8);

            FlowLayoutPanel layout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Color.Transparent
            };

            _progressStatusLabel = UILabelFactory.CreateNormal(string.Format(UIStrings.Get("UpdateAvailable.Downloading"), 0));
            _progressStatusLabel.AutoSize = true;
            _progressStatusLabel.Font = UIFonts.Normal;
            _progressStatusLabel.ForeColor = UIColors.TextPrimary;
            _progressStatusLabel.Margin = new Padding(0, 6, 0, 14);

            _progressBar = new ProgressBar
            {
                Width = DialogSize.Width - 24 - 24 - 24,
                Height = 18,
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                Style = ProgressBarStyle.Continuous
            };

            layout.Controls.Add(_progressStatusLabel);
            layout.Controls.Add(_progressBar);
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

            // Button widths grow to fit whatever UIStrings resolves to for the
            // active language (e.g. German "Jetzt aktualisieren" is wider than
            // English "Update now") - the 80/100 minimums keep English pixel-
            // identical to before.
            string laterText = UIStrings.Get("UpdateAvailable.Later");
            string updateText = UIStrings.Get("UpdateAvailable.UpdateNow");
            int laterButtonWidth = Math.Max(80, TextRenderer.MeasureText(laterText, UIFonts.Normal).Width + 50);
            int updateButtonWidth = Math.Max(100, TextRenderer.MeasureText(updateText, UIFonts.Normal).Width + 50);

            buttonPanel.ColumnStyles.Clear();
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, laterButtonWidth));
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, updateButtonWidth));

            buttonPanel.RowStyles.Clear();
            buttonPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            Button laterButton = UIButtonFactory.CreateStandard(
                laterText, "", new Size(laterButtonWidth, 30));
            laterButton.Dock = DockStyle.Fill;
            laterButton.Margin = new Padding(0, 0, 6, 0);
            laterButton.Click += (sender, e) => Close();
            CancelButton = laterButton;

            Button updateButton = UIButtonFactory.CreateGreen(
                updateText, "", new Size(updateButtonWidth, 30));
            updateButton.Dock = DockStyle.Fill;
            updateButton.Margin = new Padding(6, 0, 0, 0);
            updateButton.Click += OnUpdateButtonClick;
            AcceptButton = updateButton;

            buttonPanel.Controls.Add(laterButton, 1, 0);
            buttonPanel.Controls.Add(updateButton, 2, 0);

            return buttonPanel;
        }

        private void OnUpdateButtonClick(object sender, EventArgs e)
        {
            if (_updateRequested)
            {
                return;
            }

            _updateRequested = true;

            EventHandler handler = UpdateRequested;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }
    }
}
