using System;
using System.Drawing;
using System.Windows.Forms;
using CustomWFUI.Controls;

namespace CustomWFUI.Forms
{
    /// <summary>
    /// A small options/settings dialog built around a StyledPropertyTable:
    /// add rows to PropertyTable, then call FitToContent() so the window
    /// sizes itself to exactly fit whatever was added, instead of guessing
    /// a fixed size up front.
    /// </summary>
    public class StyledOptionsForm : StyledForm
    {
        private readonly StyledPropertyTable _propertyTable;
        private readonly Panel _buttonBar;
        private readonly Button _saveButton;
        private readonly Button _cancelButton;

        public StyledPropertyTable PropertyTable
        {
            get { return _propertyTable; }
        }

        /// <summary>
        /// "Save"/"Cancel" by default - this class has no notion of the
        /// consuming app's language, so callers that localize their own UI
        /// (e.g. via AppLocalization) need to set these explicitly, or the
        /// buttons stay stuck in English regardless of the app's language.
        /// </summary>
        public string SaveButtonText
        {
            get { return _saveButton.Text; }
            set { _saveButton.Text = value; }
        }

        public string CancelButtonText
        {
            get { return _cancelButton.Text; }
            set { _cancelButton.Text = value; }
        }

        /// <summary>
        /// Raised when the save button is clicked, before the dialog closes
        /// with DialogResult.OK - read whatever the option controls added
        /// to PropertyTable are currently set to here.
        /// </summary>
        public event EventHandler SaveClicked;

        public StyledOptionsForm(string title = "Options")
            : base(StyledFormOptions.CreateDialog(title))
        {
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(340, 160);

            // Free space between the form's edges and the property
            // table/button bar - previously they were docked flush against
            // ContentPanel, so the table's own (darker) background filled
            // the whole window edge-to-edge instead of reading as a card.
            ContentPanel.Padding = new Padding(20);

            _propertyTable = UIStyles.PropertyTables.Create();
            _propertyTable.Dock = DockStyle.Top;
            // The table's default background matches ContentPanel's
            // BackgroundMedium, so its (transparent) label column blended
            // straight into the form - darker tone makes it read as its
            // own card.
            _propertyTable.BackColor = UIStyles.Colors.BackgroundDark;

            // Same BackgroundMedium as ContentPanel - so the form reads as
            // one consistent color, with only the property table set apart.
            _buttonBar = UIStyles.Panels.CreateMedium();
            _buttonBar.Dock = DockStyle.Bottom;
            _buttonBar.Height = 56;

            _cancelButton = UIStyles.Buttons.CreateStandard("Cancel", size: new Size(100, 32));
            _cancelButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _cancelButton.Location = new Point(_buttonBar.Width - 220, 12);
            _cancelButton.DialogResult = DialogResult.Cancel;
            _cancelButton.Click += (s, e) => Close();

            _saveButton = UIStyles.Buttons.CreatePrimary("Save", size: new Size(100, 32));
            _saveButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _saveButton.Location = new Point(_buttonBar.Width - 112, 12);
            _saveButton.Click += (s, e) =>
            {
                SaveClicked?.Invoke(this, EventArgs.Empty);
                DialogResult = DialogResult.OK;
                Close();
            };

            _buttonBar.Controls.Add(_cancelButton);
            _buttonBar.Controls.Add(_saveButton);

            // Fill (property table) added before the bottom-docked bar, so
            // the bar reliably keeps its band - see BuildCard in
            // DealOrNoDeal.cs for the same Dock-order reasoning.
            ContentPanel.Controls.Add(_propertyTable);
            ContentPanel.Controls.Add(_buttonBar);

            Resize += (s, e) => RepositionButtons();
            RepositionButtons();
        }

        private void RepositionButtons()
        {
            _cancelButton.Location = new Point(_buttonBar.Width - 220, 12);
            _saveButton.Location = new Point(_buttonBar.Width - 112, 12);
        }

        /// <summary>
        /// Sizes the window to exactly fit the rows currently in
        /// PropertyTable plus the button bar - call once after populating
        /// PropertyTable, instead of guessing a fixed window size.
        /// </summary>
        public void FitToContent(int extraWidth = 16, int extraHeight = 16)
        {
            Size preferred = _propertyTable.PreferredSize;

            int width = Math.Max(MinimumSize.Width,
                preferred.Width + extraWidth + Padding.Horizontal + ContentPanel.Padding.Horizontal);
            int height = TitleBar.Height + preferred.Height + _buttonBar.Height + extraHeight
                + Padding.Vertical + ContentPanel.Padding.Vertical;

            ClientSize = new Size(width, Math.Max(MinimumSize.Height, height));
        }
    }
}
