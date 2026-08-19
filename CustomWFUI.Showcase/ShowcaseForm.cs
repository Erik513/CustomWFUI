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
    //
    // Laid out with a StyledPropertyTable instead of manually-positioned
    // cards - it's a real CustomWFUI control too (dogfooding it here rather
    // than a one-off layout scheme), it keeps every row's label/editor
    // evenly aligned automatically, and it's exactly the kind of "several
    // controls in a settings-panel-shaped list" layout the Showcase already
    // needed.
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

            // StyledForm/TitleBarControl only read theme colors once, at
            // construction - this window itself is never recreated (only its
            // content is, which is why the rest of BuildUi exists at all),
            // so without this the title bar stayed on whatever theme was
            // active when the app first launched, regardless of later
            // Dark/Light switches.
            BackColor = UIColors.BackgroundBlack;
            TitleBar.RefreshTheme();

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
            StyledPropertyTable table = UIStyles.PropertyTables.Create();
            table.Dock = DockStyle.Top;

            AddButtonsSection(table);
            AddCheckBoxesAndTogglesSection(table);
            AddInputsSection(table);
            AddProgressBarsSection(table);
            AddLabelsSection(table);
            AddPanelsSection(table);
            AddListsSection(table);
            AddPopupsSection(table);

            host.Controls.Add(table);
        }

        // Every "small control" row (everything but Lists, which keeps its
        // own wider/taller layout) goes through this one helper so all rows
        // share the same 3 equal-width editor columns - Variant 1 (standard
        // state) | Variant 2 (another state, or empty) | Disabled. Equal
        // Percent columns line up across rows because every row's editor
        // area is the same overall width, not because the widths are
        // absolute - see StyledPropertyTable.CreateEditorLayout.
        private const float UniformColumnPercent = 100f / 3f;

        private void AddUniformRow(
            StyledPropertyTable table,
            string labelText,
            Control variant1,
            Control variant2,
            Control disabled)
        {
            table.AddRow(
                labelText,
                UIColumn.Percent(variant1, UniformColumnPercent),
                UIColumn.Percent(variant2, UniformColumnPercent),
                UIColumn.Percent(disabled, UniformColumnPercent));
        }

        private void AddButtonsSection(StyledPropertyTable table)
        {
            table.AddSection("Buttons");

            Size textButtonSize = new Size(110, 32);

            Button standard = UIStyles.Buttons.CreateStandard("Standard", size: textButtonSize);
            Button standardDisabled = UIStyles.Buttons.CreateStandard("Disabled", size: textButtonSize);
            standardDisabled.Enabled = false;
            AddUniformRow(table, "CreateStandard", standard, null, standardDisabled);

            Button primary = UIStyles.Buttons.CreatePrimary("Primary", size: textButtonSize);
            Button primaryDisabled = UIStyles.Buttons.CreatePrimary("Disabled", size: textButtonSize);
            primaryDisabled.Enabled = false;
            AddUniformRow(table, "CreatePrimary", primary, null, primaryDisabled);

            Button green = UIStyles.Buttons.CreateGreen("Confirm", size: textButtonSize);
            Button greenDisabled = UIStyles.Buttons.CreateGreen("Disabled", size: textButtonSize);
            greenDisabled.Enabled = false;
            AddUniformRow(table, "CreateGreen", green, null, greenDisabled);

            Button danger = UIStyles.Buttons.CreateRed("Delete", size: textButtonSize);
            Button dangerDisabled = UIStyles.Buttons.CreateRed("Disabled", size: textButtonSize);
            dangerDisabled.Enabled = false;
            AddUniformRow(table, "CreateRed", danger, null, dangerDisabled);

            Button browse = UIStyles.Buttons.CreateBrowseInFolder("Browse", new Size(36, 30));
            Button icon = UIStyles.Buttons.CreateIconButton("★", 36);
            Button browseDisabled = UIStyles.Buttons.CreateBrowseInFolder("Browse", new Size(36, 30));
            browseDisabled.Enabled = false;
            AddUniformRow(table, "Browse / Icon", browse, icon, browseDisabled);
        }

        private void AddCheckBoxesAndTogglesSection(StyledPropertyTable table)
        {
            table.AddSection("CheckBoxes / ToggleSwitches");

            CheckBox checkedBox = UIStyles.CheckBoxes.CreateStandard("Checked", true);
            CheckBox uncheckedBox = UIStyles.CheckBoxes.CreateStandard("Unchecked", false);
            CheckBox disabledBox = UIStyles.CheckBoxes.CreateStandard("Disabled", true);
            disabledBox.Enabled = false;
            AddUniformRow(table, "CheckBoxes", checkedBox, uncheckedBox, disabledBox);

            CheckBox compactChecked = UIStyles.CheckBoxes.CreateCompact(true);
            CheckBox compactUnchecked = UIStyles.CheckBoxes.CreateCompact(false);
            CheckBox compactDisabled = UIStyles.CheckBoxes.CreateCompact(true);
            compactDisabled.Enabled = false;
            AddUniformRow(table, "CreateCompact", compactChecked, compactUnchecked, compactDisabled);

            ToggleSwitch standardOn = UIStyles.ToggleSwitches.CreateStandard(true);
            ToggleSwitch standardOff = UIStyles.ToggleSwitches.CreateStandard(false);
            ToggleSwitch disabledToggle = UIStyles.ToggleSwitches.CreateStandard(true);
            disabledToggle.Enabled = false;
            AddUniformRow(table, "ToggleSwitches", standardOn, standardOff, disabledToggle);

            ToggleSwitch small = UIStyles.ToggleSwitches.CreateSmall(true);
            ToggleSwitch large = UIStyles.ToggleSwitches.CreateLarge(true);
            AddUniformRow(table, "Sizes (Small / Large)", small, large, null);
        }

        private void AddInputsSection(StyledPropertyTable table)
        {
            table.AddSection("TextBoxes / ComboBox / NumericUpDown");

            TextBox standardBox = UIStyles.TextBoxes.CreateStandard("", "Standard");
            TextBox borderless = UIStyles.TextBoxes.CreateBorderstyleNone("Borderless text", "");
            TextBox textDisabled = UIStyles.TextBoxes.CreateStandard("Disabled", "");
            textDisabled.Enabled = false;
            AddUniformRow(table, "TextBoxes", standardBox, borderless, textDisabled);

            ComboBox combo = UIStyles.ComboBoxes.CreateStandard();
            combo.Items.AddRange(new object[] { "Option A", "Option B", "Option C" });
            combo.SelectedIndex = 0;
            ComboBox comboDisabled = UIStyles.ComboBoxes.CreateStandard();
            comboDisabled.Items.AddRange(new object[] { "Option A", "Option B", "Option C" });
            comboDisabled.SelectedIndex = 0;
            comboDisabled.Enabled = false;
            AddUniformRow(table, "ComboBox", combo, null, comboDisabled);

            NumericUpDown numeric = UIStyles.NumericUpDowns.CreateStandard(0, 100, 1, 42);
            NumericUpDown numericDisabled = UIStyles.NumericUpDowns.CreateStandard(0, 100, 1, 42);
            numericDisabled.Enabled = false;
            AddUniformRow(table, "NumericUpDown", numeric, null, numericDisabled);
        }

        private void AddProgressBarsSection(StyledPropertyTable table)
        {
            table.AddSection("ProgressBars");

            ProgressBar standardBar = UIStyles.ProgressBars.CreateStandard();
            standardBar.Value = 65;
            ProgressBar disabledBar = UIStyles.ProgressBars.CreateStandard();
            disabledBar.Value = 65;
            disabledBar.Enabled = false;
            AddUniformRow(table, "CreateStandard", standardBar, null, disabledBar);

            ProgressBar transparentBar = UIStyles.ProgressBars.CreateTransparent();
            transparentBar.Value = 40;
            transparentBar.BackColor = UIColors.BackgroundLight;
            ProgressBar transparentDisabled = UIStyles.ProgressBars.CreateTransparent();
            transparentDisabled.Value = 40;
            transparentDisabled.BackColor = UIColors.BackgroundLight;
            transparentDisabled.Enabled = false;
            AddUniformRow(table, "CreateTransparent", transparentBar, null, transparentDisabled);
        }

        private void AddLabelsSection(StyledPropertyTable table)
        {
            table.AddSection("Labels");

            Label title = UIStyles.Labels.CreateTitle("Title label");
            Label titleDisabled = UIStyles.Labels.CreateTitle("Title label");
            titleDisabled.Enabled = false;
            AddUniformRow(table, "CreateTitle", title, null, titleDisabled);

            Label normal = UIStyles.Labels.CreateNormal("Normal body text");
            Label normalDisabled = UIStyles.Labels.CreateNormal("Normal body text");
            normalDisabled.Enabled = false;
            AddUniformRow(table, "CreateNormal", normal, null, normalDisabled);

            Label muted = UIStyles.Labels.CreateMuted("Muted / de-emphasized text");
            Label mutedDisabled = UIStyles.Labels.CreateMuted("Muted / de-emphasized text");
            mutedDisabled.Enabled = false;
            AddUniformRow(table, "CreateMuted", muted, null, mutedDisabled);
        }

        private void AddPanelsSection(StyledPropertyTable table)
        {
            table.AddSection("Panels (background shades)");

            (string Name, Panel Panel)[] swatches =
            {
                ("Dark", UIStyles.Panels.CreateDark()),
                ("Medium", UIStyles.Panels.CreateMedium()),
                ("Elevated", UIStyles.Panels.CreateElevated()),
                ("Primary", UIStyles.Panels.CreatePrimary())
            };

            foreach ((string name, Panel panel) in swatches)
            {
                Label caption = UIStyles.Labels.CreateNormal(name);
                caption.AutoSize = true;
                caption.Location = new Point(6, 6);
                caption.BackColor = Color.Transparent;
                panel.Controls.Add(caption);
            }

            AddUniformRow(table, "Shades 1", swatches[0].Panel, swatches[1].Panel, swatches[2].Panel);
            AddUniformRow(table, "Shades 2", swatches[3].Panel, null, null);
        }

        private void AddListsSection(StyledPropertyTable table)
        {
            table.AddSection("StyledListBoxControl / StyledDataTable / StyledListView");

            StyledListBoxControl listBox = UIStyles.StyledListBoxControls.Create(
                "Sample list",
                allowReorder: true,
                showEnumeration: true);
            listBox.Items.Add("First item");
            listBox.Items.Add("Second item");
            listBox.Items.Add("Third item (drag to reorder)");

            StyledDataTable dataTable = UIStyles.DataTables.Create();
            dataTable.SetColumns(new[] { "Name", "Value" }, new[] { 150, 130 });
            dataTable.SetRows(new[]
            {
                new[] { "Accent", "Blue" },
                new[] { "Theme", "Dark" },
                new[] { "Version", "1.0" }
            });

            StyledListView listView = new StyledListView { View = View.Details };
            listView.Columns.Add("Item", 180);
            listView.Columns.Add("Status", 100);
            listView.Items.Add(new ListViewItem(new[] { "Row A", "OK" }));
            listView.Items.Add(new ListViewItem(new[] { "Row B", "Pending" }));

            table.AddRow(
                "Lists",
                260,
                UIColumn.Percent(listBox, 34),
                UIColumn.Percent(dataTable, 33),
                UIColumn.Percent(listView, 33));
        }

        private void AddPopupsSection(StyledPropertyTable table)
        {
            table.AddSection("Popups (CustomMessageBox / ToastForm / InfoPopupForm)");

            Button messageBoxButton = UIStyles.Buttons.CreateStandard("Show CustomMessageBox", size: new Size(200, 32));
            messageBoxButton.Click += delegate
            {
                CustomMessageBox.Show(
                    "This is a sample CustomMessageBox.",
                    "Sample",
                    CustomMessageBoxButtons.YesNo,
                    CustomMessageBoxIcon.Question,
                    this);
            };

            Button toastButton = UIStyles.Buttons.CreateStandard("Show ToastForm", size: new Size(160, 32));
            toastButton.Click += delegate
            {
                ToastForm.ShowToast("Sample toast message", this);
            };

            Button infoPopupButton = UIStyles.Buttons.CreateStandard("Show InfoPopupForm", size: new Size(180, 32));
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

            table.AddRow("Trigger", messageBoxButton, toastButton, infoPopupButton);
        }
    }
}
