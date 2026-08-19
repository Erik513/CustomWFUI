using System;
using System.Drawing;
using System.Windows.Forms;
using CustomWFUI.Controls;
using CustomWFUI.Forms;
using CustomWFUI.Styles;

namespace CustomWFUI.Showcase
{
    // A single window that instantiates one of every CustomWFUI control so
    // its look (and, for the interactive ones, its behavior) can all be
    // checked in one place instead of hunting through consuming apps. Theme
    // and accent switching rebuild every control from scratch rather than
    // recoloring in place, because several factories only read
    // UIColors/accent at construction time (documented on SetAccent/ApplyTheme
    // themselves) - there's no supported way to retint an already-built
    // control short of recreating it.
    public class ShowcaseForm : StyledForm
    {
        private static readonly (string Name, Color Color)[] AccentPresets =
        {
            ("Blue", UIAccentColors.Blue),
            ("Red", UIAccentColors.Red),
            ("Orange", UIAccentColors.Orange),
            ("Amber", UIAccentColors.Amber),
            ("Yellow", UIAccentColors.Yellow),
            ("Green", UIAccentColors.Green),
            ("Teal", UIAccentColors.Teal),
            ("Cyan", UIAccentColors.Cyan),
            ("Indigo", UIAccentColors.Indigo),
            ("Purple", UIAccentColors.Purple),
            ("Magenta", UIAccentColors.Magenta),
            ("Pink", UIAccentColors.Pink),
            ("Brown", UIAccentColors.Brown),
            ("Gray", UIAccentColors.Gray),
            ("White", UIAccentColors.White)
        };

        private const int CardWidth = 1020;

        private Panel _toolbar;
        private Panel _scrollHost;
        private InfoPopupForm _infoPopup;
        private Timer _infoPopupHideTimer;
        private bool _isLightTheme;

        public ShowcaseForm()
            : base(StyledFormOptions.CreateStandard("CustomWFUI Showcase"))
        {
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(1180, 900);
            MinimumSize = new Size(700, 500);

            _infoPopup = new InfoPopupForm("Info");

            _infoPopupHideTimer = new Timer { Interval = 3000 };
            _infoPopupHideTimer.Tick += delegate
            {
                _infoPopupHideTimer.Stop();
                _infoPopup.Hide();
            };

            BuildUi();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_infoPopupHideTimer != null)
                {
                    _infoPopupHideTimer.Stop();
                    _infoPopupHideTimer.Dispose();
                    _infoPopupHideTimer = null;
                }

                if (_infoPopup != null)
                {
                    _infoPopup.Dispose();
                    _infoPopup = null;
                }
            }

            base.Dispose(disposing);
        }

        // Full teardown/rebuild - see the class comment for why this can't
        // just recolor the existing controls in place.
        private void BuildUi()
        {
            // AutoScrollPosition's getter returns the offset negated (a
            // WinForms quirk - the setter expects it positive), and the old
            // _scrollHost is about to be disposed, so this has to be read
            // before teardown and reapplied after rebuild - otherwise every
            // theme/accent switch jumped back to the top, losing whatever
            // section you were actually looking at.
            Point savedScroll = _scrollHost != null
                ? new Point(-_scrollHost.AutoScrollPosition.X, -_scrollHost.AutoScrollPosition.Y)
                : Point.Empty;

            ContentPanel.SuspendLayout();
            ContentPanel.Controls.Clear();

            if (_toolbar != null)
                _toolbar.Dispose();
            if (_scrollHost != null)
                _scrollHost.Dispose();

            _toolbar = BuildToolbar();
            _scrollHost = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = UIColors.BackgroundMedium
            };

            BuildContent(_scrollHost);

            ContentPanel.Controls.Add(_scrollHost);
            ContentPanel.Controls.Add(_toolbar);
            ContentPanel.ResumeLayout();

            _scrollHost.AutoScrollPosition = savedScroll;
        }

        private Panel BuildToolbar()
        {
            Panel toolbar = UIStyles.Panels.CreateElevated();
            toolbar.Dock = DockStyle.Top;
            toolbar.Height = 56;
            toolbar.Padding = new Padding(16, 0, 16, 0);

            FlowLayoutPanel flow = UIStyles.FlowLayoutPanels.CreateStandard();
            flow.Dock = DockStyle.Fill;
            flow.FlowDirection = FlowDirection.LeftToRight;
            flow.WrapContents = false;
            flow.Margin = new Padding(0);

            Label themeLabel = UIStyles.Labels.CreateNormal("Theme:");
            themeLabel.AutoSize = true;
            themeLabel.Margin = new Padding(0, 18, 8, 0);
            flow.Controls.Add(themeLabel);

            Button darkButton = !_isLightTheme
                ? UIStyles.Buttons.CreatePrimary("Dark")
                : UIStyles.Buttons.CreateStandard("Dark");
            darkButton.Margin = new Padding(0, 10, 6, 0);
            darkButton.Width = 70;
            darkButton.Click += delegate
            {
                _isLightTheme = false;
                UIStyles.Colors.ApplyTheme(UIThemes.Dark);
                BuildUi();
            };
            flow.Controls.Add(darkButton);

            Button lightButton = _isLightTheme
                ? UIStyles.Buttons.CreatePrimary("Light")
                : UIStyles.Buttons.CreateStandard("Light");
            lightButton.Margin = new Padding(0, 10, 24, 0);
            lightButton.Width = 70;
            lightButton.Click += delegate
            {
                _isLightTheme = true;
                UIStyles.Colors.ApplyTheme(UIThemes.Light);
                BuildUi();
            };
            flow.Controls.Add(lightButton);

            Label accentLabel = UIStyles.Labels.CreateNormal("Accent:");
            accentLabel.AutoSize = true;
            accentLabel.Margin = new Padding(0, 18, 8, 0);
            flow.Controls.Add(accentLabel);

            foreach ((string name, Color color) in AccentPresets)
            {
                Button swatch = new Button
                {
                    Size = new Size(28, 28),
                    Margin = new Padding(0, 9, 6, 0),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = color,
                    Cursor = Cursors.Hand,
                    Text = ""
                };
                swatch.FlatAppearance.BorderColor = UIColors.BorderLight;
                swatch.FlatAppearance.BorderSize = 1;

                ToolTip tip = new ToolTip();
                tip.SetToolTip(swatch, name);

                swatch.Click += delegate
                {
                    UIStyles.Colors.SetAccent(color);
                    BuildUi();
                };

                flow.Controls.Add(swatch);
            }

            toolbar.Controls.Add(flow);
            return toolbar;
        }

        private void BuildContent(Panel host)
        {
            int y = 16;

            y = AddButtonsCard(host, y);
            y = AddCheckBoxesAndTogglesCard(host, y);
            y = AddInputsCard(host, y);
            y = AddProgressBarsCard(host, y);
            y = AddLabelsCard(host, y);
            y = AddPanelsCard(host, y);
            y = AddListsCard(host, y);
            y = AddPopupsCard(host, y);
        }

        // --- Card helpers -----------------------------------------------

        private Panel StartCard(Panel host, int y, string title, int height)
        {
            Panel card = UIStyles.Panels.CreateElevated();
            card.Dock = DockStyle.None;
            card.Location = new Point(16, y);
            card.Size = new Size(CardWidth, height);

            Label header = UIStyles.Labels.CreateTitle(title);
            header.AutoSize = true;
            header.Location = new Point(16, 12);
            card.Controls.Add(header);

            host.Controls.Add(card);
            return card;
        }

        private void PlaceRow(Panel card, string label, int rowY, params Control[] controls)
        {
            Label rowLabel = UIStyles.Labels.CreateNormal(label);
            rowLabel.AutoSize = true;
            rowLabel.Location = new Point(16, rowY + 6);
            card.Controls.Add(rowLabel);

            int x = 200;
            foreach (Control control in controls)
            {
                control.Location = new Point(x, rowY);
                card.Controls.Add(control);
                x += control.Width + 12;
            }
        }

        // --- Sections ------------------------------------------------------

        private int AddButtonsCard(Panel host, int y)
        {
            const int height = 260;
            Panel card = StartCard(host, y, "Buttons", height);

            Size textButtonSize = new Size(110, 32);

            Button standard = UIStyles.Buttons.CreateStandard("Standard", size: textButtonSize);
            Button standardDisabled = UIStyles.Buttons.CreateStandard("Disabled", size: textButtonSize);
            standardDisabled.Enabled = false;
            PlaceRow(card, "CreateStandard", 60, standard, standardDisabled);

            Button primary = UIStyles.Buttons.CreatePrimary("Primary", size: textButtonSize);
            Button primaryDisabled = UIStyles.Buttons.CreatePrimary("Disabled", size: textButtonSize);
            primaryDisabled.Enabled = false;
            PlaceRow(card, "CreatePrimary", 100, primary, primaryDisabled);

            Button green = UIStyles.Buttons.CreateGreen("Confirm", size: textButtonSize);
            Button greenDisabled = UIStyles.Buttons.CreateGreen("Disabled", size: textButtonSize);
            greenDisabled.Enabled = false;
            PlaceRow(card, "CreateGreen", 140, green, greenDisabled);

            Button danger = UIStyles.Buttons.CreateDanger("Delete", size: textButtonSize);
            Button dangerDisabled = UIStyles.Buttons.CreateDanger("Disabled", size: textButtonSize);
            dangerDisabled.Enabled = false;
            PlaceRow(card, "CreateDanger", 180, danger, dangerDisabled);

            Button browse = UIStyles.Buttons.CreateBrowseInFolder("Browse", new Size(36, 30));
            Button browseDisabled = UIStyles.Buttons.CreateBrowseInFolder("Browse", new Size(36, 30));
            browseDisabled.Enabled = false;
            Button icon = UIStyles.Buttons.CreateIconButton("★", 36);
            PlaceRow(card, "Browse / Icon", 220, browse, browseDisabled, icon);

            return y + height + 16;
        }

        private int AddCheckBoxesAndTogglesCard(Panel host, int y)
        {
            const int height = 160;
            Panel card = StartCard(host, y, "CheckBoxes / ToggleSwitches", height);

            CheckBox checkedBox = UIStyles.CheckBoxes.CreateStandard("Checked", true);
            CheckBox uncheckedBox = UIStyles.CheckBoxes.CreateStandard("Unchecked", false);
            CheckBox disabledBox = UIStyles.CheckBoxes.CreateStandard("Disabled", true);
            disabledBox.Enabled = false;
            CheckBox compactBox = UIStyles.CheckBoxes.CreateCompact(true);
            PlaceRow(card, "CheckBoxes", 60, checkedBox, uncheckedBox, disabledBox, compactBox);

            ToggleSwitch small = UIStyles.ToggleSwitches.CreateSmall(true);
            ToggleSwitch standardToggle = UIStyles.ToggleSwitches.CreateStandard(true);
            ToggleSwitch large = UIStyles.ToggleSwitches.CreateLarge(false);
            ToggleSwitch disabledToggle = UIStyles.ToggleSwitches.CreateStandard(true);
            disabledToggle.Enabled = false;
            PlaceRow(card, "ToggleSwitches", 110, small, standardToggle, large, disabledToggle);

            return y + height + 16;
        }

        private int AddInputsCard(Panel host, int y)
        {
            const int height = 200;
            Panel card = StartCard(host, y, "TextBoxes / ComboBox / NumericUpDown", height);

            TextBox standardBox = UIStyles.TextBoxes.CreateStandard("", "Standard");
            standardBox.Width = 180;
            TextBox borderless = UIStyles.TextBoxes.CreateBorderstyleNone("Borderless text", "");
            borderless.Width = 180;
            PlaceRow(card, "TextBoxes", 60, standardBox, borderless);

            ComboBox combo = UIStyles.ComboBoxes.CreateStandard();
            combo.Width = 180;
            combo.Items.AddRange(new object[] { "Option A", "Option B", "Option C" });
            combo.SelectedIndex = 0;
            PlaceRow(card, "ComboBox", 110, combo);

            NumericUpDown numeric = UIStyles.NumericUpDowns.CreateStandard(0, 100, 1, 42);
            numeric.Width = 100;
            PlaceRow(card, "NumericUpDown", 160, numeric);

            return y + height + 16;
        }

        private int AddProgressBarsCard(Panel host, int y)
        {
            const int height = 160;
            Panel card = StartCard(host, y, "ProgressBars", height);

            ProgressBar standardBar = UIStyles.ProgressBars.CreateStandard();
            standardBar.Size = new Size(300, 24);
            standardBar.Value = 65;
            ProgressBar disabledBar = UIStyles.ProgressBars.CreateStandard();
            disabledBar.Size = new Size(300, 24);
            disabledBar.Value = 65;
            disabledBar.Enabled = false;
            PlaceRow(card, "CreateStandard", 60, standardBar, disabledBar);

            ProgressBar transparentBar = UIStyles.ProgressBars.CreateTransparent();
            transparentBar.Size = new Size(300, 24);
            transparentBar.Value = 40;
            transparentBar.BackColor = card.BackColor;
            PlaceRow(card, "CreateTransparent", 110, transparentBar);

            return y + height + 16;
        }

        private int AddLabelsCard(Panel host, int y)
        {
            const int height = 140;
            Panel card = StartCard(host, y, "Labels", height);

            Label title = UIStyles.Labels.CreateTitle("Title label");
            title.AutoSize = true;
            PlaceRow(card, "CreateTitle", 60, title);

            Label normal = UIStyles.Labels.CreateNormal("Normal body text");
            normal.AutoSize = true;
            PlaceRow(card, "CreateNormal", 90, normal);

            Label muted = UIStyles.Labels.CreateMuted("Muted / de-emphasized text");
            muted.AutoSize = true;
            PlaceRow(card, "CreateMuted", 120, muted);

            return y + height + 16;
        }

        private int AddPanelsCard(Panel host, int y)
        {
            const int height = 130;
            Panel card = StartCard(host, y, "Panels (background shades)", height);

            (string Name, Panel Panel)[] swatches =
            {
                ("Dark", UIStyles.Panels.CreateDark()),
                ("Medium", UIStyles.Panels.CreateMedium()),
                ("Elevated", UIStyles.Panels.CreateElevated()),
                ("Primary", UIStyles.Panels.CreatePrimary())
            };

            int x = 16;
            foreach ((string name, Panel panel) in swatches)
            {
                panel.Dock = DockStyle.None;
                panel.Size = new Size(150, 60);
                panel.Location = new Point(x, 60);

                Label caption = UIStyles.Labels.CreateNormal(name);
                caption.AutoSize = true;
                caption.Location = new Point(6, 6);
                caption.BackColor = Color.Transparent;
                panel.Controls.Add(caption);

                card.Controls.Add(panel);
                x += 166;
            }

            return y + height + 16;
        }

        private int AddListsCard(Panel host, int y)
        {
            const int height = 320;
            Panel card = StartCard(host, y, "StyledListBoxControl / StyledDataTable", height);

            StyledListBoxControl listBox = UIStyles.StyledListBoxControls.Create(
                "Sample list",
                allowReorder: true,
                showEnumeration: true);
            listBox.Location = new Point(16, 60);
            listBox.Size = new Size(300, 240);
            listBox.Items.Add("First item");
            listBox.Items.Add("Second item");
            listBox.Items.Add("Third item (drag to reorder)");
            card.Controls.Add(listBox);

            StyledDataTable dataTable = UIStyles.DataTables.Create();
            dataTable.Location = new Point(340, 60);
            dataTable.Size = new Size(300, 240);
            dataTable.SetColumns(new[] { "Name", "Value" }, new[] { 150, 130 });
            dataTable.SetRows(new[]
            {
                new[] { "Accent", "Blue" },
                new[] { "Theme", "Dark" },
                new[] { "Version", "1.0" }
            });
            card.Controls.Add(dataTable);

            StyledListView listView = new StyledListView
            {
                Location = new Point(660, 60),
                Size = new Size(300, 240),
                View = View.Details
            };
            listView.Columns.Add("Item", 180);
            listView.Columns.Add("Status", 100);
            listView.Items.Add(new ListViewItem(new[] { "Row A", "OK" }));
            listView.Items.Add(new ListViewItem(new[] { "Row B", "Pending" }));
            card.Controls.Add(listView);

            return y + height + 16;
        }

        private int AddPopupsCard(Panel host, int y)
        {
            const int height = 100;
            Panel card = StartCard(host, y, "Popups (CustomMessageBox / ToastForm / InfoPopupForm)", height);

            Button messageBoxButton = UIStyles.Buttons.CreateStandard("Show CustomMessageBox");
            messageBoxButton.Width = 200;
            messageBoxButton.Click += delegate
            {
                CustomMessageBox.Show(
                    "This is a sample CustomMessageBox.",
                    "Sample",
                    CustomMessageBoxButtons.YesNo,
                    CustomMessageBoxIcon.Question,
                    this);
            };

            Button toastButton = UIStyles.Buttons.CreateStandard("Show ToastForm");
            toastButton.Width = 160;
            toastButton.Click += delegate
            {
                ToastForm.ShowToast("Sample toast message", this);
            };

            Button infoPopupButton = UIStyles.Buttons.CreateStandard("Show InfoPopupForm");
            infoPopupButton.Width = 180;
            infoPopupButton.Click += delegate
            {
                _infoPopup.ShowInfo("This is a sample InfoPopupForm.", infoPopupButton);

                // InfoPopupForm is designed as a hover tooltip - real
                // consumers show it on MouseEnter and hide it on MouseLeave.
                // A click-to-preview button has no such pairing, so without
                // this it would just stay open forever; restart the same
                // timer on every click instead of leaking a new one each time.
                _infoPopupHideTimer.Stop();
                _infoPopupHideTimer.Start();
            };

            PlaceRow(card, "Trigger", 60, messageBoxButton, toastButton, infoPopupButton);

            return y + height + 16;
        }
    }
}
