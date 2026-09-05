using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Styles;
using MessageBox = ErikwnkWFUI.Forms.MessageBox;
using MessageBoxButtons = ErikwnkWFUI.Forms.MessageBoxButtons;
using MessageBoxIcon = ErikwnkWFUI.Forms.MessageBoxIcon;

namespace ErikwnkWFUI.Showcase
{
    // A single window that instantiates one of every ErikwnkWFUI control so
    // its look (and, for the interactive ones, its behavior) can all be
    // checked in one place instead of hunting through consuming apps. Theme
    // and accent switching rebuild every control from scratch rather than
    // recoloring in place, because several factories only read
    // UIColors/accent at construction time (documented on SetAccent/ApplyTheme
    // themselves) - there's no supported way to retint an already-built
    // control short of recreating it.
    //
    // Laid out with a PropertyTable instead of manually-positioned
    // cards - it's a real ErikwnkWFUI control too (dogfooding it here rather
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
        private InfoPopupForm _compactInfoPopup;
        private Timer _compactInfoPopupHideTimer;
        private bool _isLightTheme;

        // Lets every ProgressBars-section row (the setters registered by
        // AddProgressBarsSection) actually show what a real, moving load
        // looks like instead of just sitting at one fixed demo percentage -
        // one shared value drives all of them in lockstep. A list of
        // setters rather than a list of controls because ProgressBar.Value
        // and SlimProgressBar.Value aren't behind a common interface;
        // storing "how to apply the current percentage to this specific
        // bar" sidesteps that without needing one. Rebuilt (cleared, then
        // repopulated) on every BuildUi() call alongside the controls
        // themselves, since the old bars get disposed each time a
        // theme/accent switch tears down and recreates the whole UI - an
        // un-cleared list would keep invoking setters that close over
        // disposed controls.
        private readonly List<Action<int>> _progressBarAnimationSetters = new List<Action<int>>();
        private Timer _progressBarAnimationTimer;
        private int _progressBarAnimationPercent;

        public ShowcaseForm()
            : base(StyledFormOptions.CreateStandard("ErikwnkWFUI Showcase"))
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

            // A separate instance from _infoPopup - Compact is meant to be
            // set once and left on for that instance's whole life (see its
            // own doc comment), so the plain-text demo above and this one
            // can't share a single popup.
            _compactInfoPopup = new InfoPopupForm { Compact = true, CompactSize = new Size(70, 32) };

            _compactInfoPopupHideTimer = new Timer { Interval = 3000 };
            _compactInfoPopupHideTimer.Tick += delegate
            {
                _compactInfoPopupHideTimer.Stop();
                _compactInfoPopup.Hide();
            };

            // 1% every 60ms - a full 0->100 sweep takes 6 seconds, slow
            // enough to actually watch rather than just flicker by.
            _progressBarAnimationTimer = new Timer { Interval = 60 };
            _progressBarAnimationTimer.Tick += delegate
            {
                _progressBarAnimationPercent = (_progressBarAnimationPercent + 1) % 101;

                foreach (Action<int> setter in _progressBarAnimationSetters)
                    setter(_progressBarAnimationPercent);
            };
            _progressBarAnimationTimer.Start();

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

                if (_progressBarAnimationTimer != null)
                {
                    _progressBarAnimationTimer.Stop();
                    _progressBarAnimationTimer.Dispose();
                    _progressBarAnimationTimer = null;
                }

                if (_infoPopup != null)
                {
                    _infoPopup.Dispose();
                    _infoPopup = null;
                }

                if (_compactInfoPopupHideTimer != null)
                {
                    _compactInfoPopupHideTimer.Stop();
                    _compactInfoPopupHideTimer.Dispose();
                    _compactInfoPopupHideTimer = null;
                }

                if (_compactInfoPopup != null)
                {
                    _compactInfoPopup.Dispose();
                    _compactInfoPopup = null;
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

            // The bars AddProgressBarsSection is about to (re)create are
            // brand new instances - drop the setters that closed over the
            // just-disposed previous ones before it repopulates this.
            _progressBarAnimationSetters.Clear();

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
            PropertyTable table = UIStyles.PropertyTables.CreateStandard();
            table.Dock = DockStyle.Top;

            // Alphabetical by section title, so the Showcase's own layout
            // doesn't silently depend on this list's order matching it.
            AddButtonsSection(table);
            AddCheckBoxesSection(table);
            AddComboBoxesSection(table);
            AddDataGridSection(table);
            AddLabelsSection(table);
            AddListBoxControlSection(table);
            AddListViewSection(table);
            AddNumericUpDownsSection(table);
            AddPanelsSection(table);
            AddPopupsSection(table);
            AddProgressBarsSection(table);
            AddSliderBarSection(table);
            AddSlimProgressBarsSection(table);
            AddTextBoxesSection(table);
            AddToggleSwitchesSection(table);
            AddVolumeSliderSection(table);

            host.Controls.Add(table);
        }

        // Every "small control" row (everything but Lists, which keeps its
        // own wider/taller layout) goes through this one helper so all rows
        // share the same 3 equal-width editor columns - Variant 1 (standard
        // state) | Variant 2 (another state, or empty) | Disabled. Equal
        // Percent columns line up across rows because every row's editor
        // area is the same overall width, not because the widths are
        // absolute - see PropertyTable.CreateEditorLayout.
        private const float UniformColumnPercent = 100f / 3f;

        private void AddUniformRow(
            PropertyTable table,
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

        // For sections where every row only ever has a Variant 1 and a
        // Disabled control - no natural "another state" to put in the
        // middle slot (Buttons, ProgressBars, Labels) - AddUniformRow's
        // always-null Variant 2 column just sat there empty in every single
        // row of those sections. Two 50/50 columns instead of three, at the
        // cost of no longer lining up column-for-column with sections that
        // DO use all three (CheckBoxes/ToggleSwitches, TextBoxes, Panels -
        // this doesn't attempt to keep cross-section alignment, only
        // requested for the sections that never used the middle slot).
        private const float TwoColumnPercent = 50f;

        private void AddTwoColumnRow(
            PropertyTable table,
            string labelText,
            Control primary,
            Control disabled)
        {
            table.AddRow(
                labelText,
                UIColumn.Percent(primary, TwoColumnPercent),
                UIColumn.Percent(disabled, TwoColumnPercent));
        }

        private void AddButtonsSection(PropertyTable table)
        {
            table.AddSection("Buttons");

            Size textButtonSize = new Size(110, 32);

            Button standard = UIStyles.Buttons.CreateStandard("Standard", size: textButtonSize);
            Button standardDisabled = UIStyles.Buttons.CreateStandard("Disabled", size: textButtonSize);
            standardDisabled.Enabled = false;
            AddTwoColumnRow(table, "CreateStandard", standard, standardDisabled);

            Button primary = UIStyles.Buttons.CreatePrimary("Primary", size: textButtonSize);
            Button primaryDisabled = UIStyles.Buttons.CreatePrimary("Disabled", size: textButtonSize);
            primaryDisabled.Enabled = false;
            AddTwoColumnRow(table, "CreatePrimary", primary, primaryDisabled);

            Button green = UIStyles.Buttons.CreateGreen("Confirm", size: textButtonSize);
            Button greenDisabled = UIStyles.Buttons.CreateGreen("Disabled", size: textButtonSize);
            greenDisabled.Enabled = false;
            AddTwoColumnRow(table, "CreateGreen", green, greenDisabled);

            Button danger = UIStyles.Buttons.CreateRed("Delete", size: textButtonSize);
            Button dangerDisabled = UIStyles.Buttons.CreateRed("Disabled", size: textButtonSize);
            dangerDisabled.Enabled = false;
            AddTwoColumnRow(table, "CreateRed", danger, dangerDisabled);

            Button browse = UIStyles.Buttons.CreateBrowse("Browse", new Size(36, 30));
            Button browseDisabled = UIStyles.Buttons.CreateBrowse("Browse", new Size(36, 30));
            browseDisabled.Enabled = false;
            AddTwoColumnRow(table, "CreateBrowse", browse, browseDisabled);
        }

        private void AddCheckBoxesSection(PropertyTable table)
        {
            table.AddSection("CheckBoxes");

            CheckBox checkedBox = UIStyles.CheckBoxes.CreateStandard("Checked", true);
            CheckBox uncheckedBox = UIStyles.CheckBoxes.CreateStandard("Unchecked", false);
            CheckBox disabledBox = UIStyles.CheckBoxes.CreateStandard("Disabled", true);
            disabledBox.Enabled = false;
            AddUniformRow(table, "CreateStandard", checkedBox, uncheckedBox, disabledBox);

            CheckBox compactChecked = UIStyles.CheckBoxes.CreateCompact(true);
            CheckBox compactUnchecked = UIStyles.CheckBoxes.CreateCompact(false);
            CheckBox compactDisabled = UIStyles.CheckBoxes.CreateCompact(true);
            compactDisabled.Enabled = false;
            AddUniformRow(table, "CreateCompact", compactChecked, compactUnchecked, compactDisabled);
        }

        private void AddToggleSwitchesSection(PropertyTable table)
        {
            table.AddSection("ToggleSwitches");

            ToggleSwitch standardOn = UIStyles.ToggleSwitches.CreateStandard(true, "On", "Off");
            ToggleSwitch standardOff = UIStyles.ToggleSwitches.CreateStandard(false, "On", "Off");
            ToggleSwitch disabledToggle = UIStyles.ToggleSwitches.CreateStandard(true, "On", "Off");
            disabledToggle.Enabled = false;
            AddUniformRow(table, "CreateStandard", standardOn, standardOff, disabledToggle);

            ToggleSwitch smallOn = UIStyles.ToggleSwitches.CreateSmall(true, "On", "Off");
            ToggleSwitch smallOff = UIStyles.ToggleSwitches.CreateSmall(false, "On", "Off");
            ToggleSwitch smallDisabled = UIStyles.ToggleSwitches.CreateSmall(true, "On", "Off");
            smallDisabled.Enabled = false;
            AddUniformRow(table, "CreateSmall", smallOn, smallOff, smallDisabled);

            ToggleSwitch largeOn = UIStyles.ToggleSwitches.CreateLarge(true, "On", "Off");
            ToggleSwitch largeOff = UIStyles.ToggleSwitches.CreateLarge(false, "On", "Off");
            ToggleSwitch largeDisabled = UIStyles.ToggleSwitches.CreateLarge(true, "On", "Off");
            largeDisabled.Enabled = false;
            AddUniformRow(table, "CreateLarge", largeOn, largeOff, largeDisabled);
        }

        private void AddTextBoxesSection(PropertyTable table)
        {
            table.AddSection("TextBoxes");

            TextBox standardBox = UIStyles.TextBoxes.CreateStandard("", "Standard");
            TextBox textDisabled = UIStyles.TextBoxes.CreateStandard("Disabled", "");
            textDisabled.Enabled = false;
            AddTwoColumnRow(table, "CreateStandard", standardBox, textDisabled);

            TextBox borderless = UIStyles.TextBoxes.CreateBorderstyleNone("Borderless text", "");
            TextBox borderlessDisabled = UIStyles.TextBoxes.CreateBorderstyleNone("Disabled", "");
            borderlessDisabled.Enabled = false;
            AddTwoColumnRow(table, "CreateBorderstyleNone", borderless, borderlessDisabled);
        }

        private void AddComboBoxesSection(PropertyTable table)
        {
            table.AddSection("ComboBoxes");

            ComboBox combo = UIStyles.ComboBoxes.CreateStandard();
            combo.Items.AddRange(new object[] { "Option A", "Option B", "Option C" });
            combo.SelectedIndex = 0;
            ComboBox comboDisabled = UIStyles.ComboBoxes.CreateStandard();
            comboDisabled.Items.AddRange(new object[] { "Option A", "Option B", "Option C" });
            comboDisabled.SelectedIndex = 0;
            comboDisabled.Enabled = false;
            AddTwoColumnRow(table, "CreateStandard", combo, comboDisabled);
        }

        private void AddNumericUpDownsSection(PropertyTable table)
        {
            table.AddSection("NumericUpDowns");

            NumericUpDown numeric = UIStyles.NumericUpDowns.CreateStandard(0, 100, 1, 42);
            NumericUpDown numericDisabled = UIStyles.NumericUpDowns.CreateStandard(0, 100, 1, 42);
            numericDisabled.Enabled = false;
            AddTwoColumnRow(table, "CreateStandard", numeric, numericDisabled);
        }

        private void AddProgressBarsSection(PropertyTable table)
        {
            table.AddSection("ProgressBars");

            // Every enabled bar below is driven by the shared animation
            // timer (see _progressBarAnimationSetters) instead of a fixed
            // demo value, so this section doubles as a preview of what an
            // actual, moving load looks like. Disabled bars are deliberately
            // left out of the animation and kept at one fixed value - the
            // point of that column is to show the disabled look clearly,
            // which a constantly-changing bar would undercut.
            ProgressBar standardBar = UIStyles.ProgressBars.CreateGreen();
            AnimateProgressBar(v => standardBar.Value = v);
            ProgressBar disabledBar = UIStyles.ProgressBars.CreateGreen();
            disabledBar.Value = 65;
            disabledBar.Enabled = false;
            AddTwoColumnRow(table, "CreateGreen", standardBar, disabledBar);

            ProgressBar transparentBar = UIStyles.ProgressBars.CreateGreenTransparent();
            transparentBar.BackColor = UIColors.BackgroundLight;
            AnimateProgressBar(v => transparentBar.Value = v);
            ProgressBar transparentDisabled = UIStyles.ProgressBars.CreateGreenTransparent();
            transparentDisabled.Value = 40;
            transparentDisabled.BackColor = UIColors.BackgroundLight;
            transparentDisabled.Enabled = false;
            AddTwoColumnRow(table, "CreateGreenTransparent", transparentBar, transparentDisabled);

            ProgressBar primaryBar = UIStyles.ProgressBars.CreatePrimary();
            AnimateProgressBar(v => primaryBar.Value = v);
            ProgressBar primaryDisabled = UIStyles.ProgressBars.CreatePrimary();
            primaryDisabled.Value = 65;
            primaryDisabled.Enabled = false;
            AddTwoColumnRow(table, "CreatePrimary", primaryBar, primaryDisabled);

            ProgressBar primaryTransparentBar = UIStyles.ProgressBars.CreatePrimaryTransparent();
            primaryTransparentBar.BackColor = UIColors.BackgroundLight;
            AnimateProgressBar(v => primaryTransparentBar.Value = v);
            ProgressBar primaryTransparentDisabled = UIStyles.ProgressBars.CreatePrimaryTransparent();
            primaryTransparentDisabled.Value = 40;
            primaryTransparentDisabled.BackColor = UIColors.BackgroundLight;
            primaryTransparentDisabled.Enabled = false;
            AddTwoColumnRow(table, "CreatePrimaryTransparent", primaryTransparentBar, primaryTransparentDisabled);

            ProgressBar statusBar = UIStyles.ProgressBars.CreateStatus();
            AnimateProgressBar(v => statusBar.Value = v);
            ProgressBar statusDisabled = UIStyles.ProgressBars.CreateStatus();
            statusDisabled.Value = 65;
            statusDisabled.Enabled = false;
            AddTwoColumnRow(table, "CreateStatus", statusBar, statusDisabled);

            ProgressBar statusTransparentBar = UIStyles.ProgressBars.CreateStatusTransparent();
            statusTransparentBar.BackColor = UIColors.BackgroundLight;
            AnimateProgressBar(v => statusTransparentBar.Value = v);
            ProgressBar statusTransparentDisabled = UIStyles.ProgressBars.CreateStatusTransparent();
            statusTransparentDisabled.Value = 40;
            statusTransparentDisabled.BackColor = UIColors.BackgroundLight;
            statusTransparentDisabled.Enabled = false;
            AddTwoColumnRow(table, "CreateStatusTransparent", statusTransparentBar, statusTransparentDisabled);
        }

        private void AddSlimProgressBarsSection(PropertyTable table)
        {
            table.AddSection("SlimProgressBars");

            SlimProgressBar slimGreenBar = UIStyles.SlimProgressBars.CreateGreen();
            AnimateProgressBar(v => slimGreenBar.Value = v);
            SlimProgressBar slimGreenDisabled = UIStyles.SlimProgressBars.CreateGreen();
            slimGreenDisabled.Value = 65;
            slimGreenDisabled.Enabled = false;
            AddTwoColumnRow(table, "CreateGreen", slimGreenBar, slimGreenDisabled);

            SlimProgressBar slimPrimaryBar = UIStyles.SlimProgressBars.CreatePrimary();
            AnimateProgressBar(v => slimPrimaryBar.Value = v);
            SlimProgressBar slimPrimaryDisabled = UIStyles.SlimProgressBars.CreatePrimary();
            slimPrimaryDisabled.Value = 40;
            slimPrimaryDisabled.Enabled = false;
            AddTwoColumnRow(table, "CreatePrimary", slimPrimaryBar, slimPrimaryDisabled);

            SlimProgressBar slimStatusBar = UIStyles.SlimProgressBars.CreateStatus();
            AnimateProgressBar(v => slimStatusBar.Value = v);
            SlimProgressBar slimStatusDisabled = UIStyles.SlimProgressBars.CreateStatus();
            slimStatusDisabled.Value = 40;
            slimStatusDisabled.Enabled = false;
            AddTwoColumnRow(table, "CreateStatus", slimStatusBar, slimStatusDisabled);
        }

        // Value/Enabled are set directly rather than via AnimateProgressBar -
        // a slider represents a user-set position, not a moving load, so a
        // fixed demo value (like the disabled ProgressBar rows use) is the
        // right comparison here, not motion.
        private void AddSliderBarSection(PropertyTable table)
        {
            table.AddSection("SliderBar");

            SliderBar slider = UIStyles.SliderBars.CreateStandard(0.4);
            SliderBar sliderDisabled = UIStyles.SliderBars.CreateStandard(0.65);
            sliderDisabled.Enabled = false;
            AddTwoColumnRow(table, "CreateStandard", slider, sliderDisabled);
        }

        // VolumeSlider adds its own drag-value popup on top of SliderBar -
        // drag the enabled one to see it. No separate "with popup" variant
        // to demo since that popup is the whole point of this control, not
        // an optional extra.
        private void AddVolumeSliderSection(PropertyTable table)
        {
            table.AddSection("VolumeSlider");

            VolumeSlider volume = UIStyles.VolumeSliders.CreateStandard(0.4);
            VolumeSlider volumeDisabled = UIStyles.VolumeSliders.CreateStandard(0.65);
            volumeDisabled.Enabled = false;
            AddTwoColumnRow(table, "CreateStandard", volume, volumeDisabled);
        }

        // Applies the current animation percentage immediately (so the bar
        // doesn't sit at its Value=0 default until the next timer tick)
        // and registers the setter so future ticks keep it moving.
        private void AnimateProgressBar(Action<int> setValue)
        {
            setValue(_progressBarAnimationPercent);
            _progressBarAnimationSetters.Add(setValue);
        }

        private void AddLabelsSection(PropertyTable table)
        {
            table.AddSection("Labels");

            Label title = UIStyles.Labels.CreateTitle("Title label");
            Label titleDisabled = UIStyles.Labels.CreateTitle("Title label");
            titleDisabled.Enabled = false;
            AddTwoColumnRow(table, "CreateTitle", title, titleDisabled);

            Label normal = UIStyles.Labels.CreateNormal("Normal body text");
            Label normalDisabled = UIStyles.Labels.CreateNormal("Normal body text");
            normalDisabled.Enabled = false;
            AddTwoColumnRow(table, "CreateNormal", normal, normalDisabled);

            Label muted = UIStyles.Labels.CreateMuted("Muted / de-emphasized text");
            Label mutedDisabled = UIStyles.Labels.CreateMuted("Muted / de-emphasized text");
            mutedDisabled.Enabled = false;
            AddTwoColumnRow(table, "CreateMuted", muted, mutedDisabled);
        }

        private void AddPanelsSection(PropertyTable table)
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

            table.AddRow("CreateDark", swatches[0].Panel);
            table.AddRow("CreateMedium", swatches[1].Panel);
            table.AddRow("CreateElevated", swatches[2].Panel);
            table.AddRow("CreatePrimary", swatches[3].Panel);
        }

        private void AddListBoxControlSection(PropertyTable table)
        {
            table.AddSection("ListBoxControl");

            ListBoxControl listBox = UIStyles.ListBoxControls.CreateStandard(
                "Sample list",
                allowReorder: true,
                showEnumeration: true);
            listBox.Items.Add("First item");
            listBox.Items.Add("Second item");
            listBox.Items.Add("Third item (drag to reorder)");

            table.AddRow("CreateStandard", 260, listBox);
        }

        private void AddListViewSection(PropertyTable table)
        {
            table.AddSection("ListView");

            const int demoWidth = 300;

            var standardListView = UIStyles.ListViews.CreateStandard();
            PopulateListViewDemo(standardListView);
            table.AddRow("CreateStandard", 260, UIColumn.Absolute(standardListView, demoWidth));
            PinToDemoWidth(standardListView, demoWidth);

            var primaryListView = UIStyles.ListViews.CreatePrimary();
            PopulateListViewDemo(primaryListView);
            table.AddRow("CreatePrimary", 260, UIColumn.Absolute(primaryListView, demoWidth));
            PinToDemoWidth(primaryListView, demoWidth);
        }

        // AddRow's UIColumn.Absolute only bounds the WRAPPER cell to
        // demoWidth - PropertyTable.ConfigureEditorControl (run as part of
        // AddRow, no special case for ListView) still sets Dock = Fill on
        // the control itself afterward, which stretches it right back out
        // to fill that cell/wrapper rather than actually sizing it to
        // demoWidth. Overriding Dock/Width here, after AddRow has already
        // run, is what actually keeps the control narrow.
        private static void PinToDemoWidth(System.Windows.Forms.Control control, int width)
        {
            control.Dock = DockStyle.None;
            control.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            control.Width = width;

            // Widening a column past this fixed width makes a horizontal
            // scrollbar appear, which shrinks ClientSize.Height - and
            // ListView's own whole-rows-only height snapping (SnapHeight-
            // ToWholeRows) then reacts to THAT by shrinking the control's
            // actual Height too, even though nothing about the demo's own
            // requested size changed. Locking both dimensions here, not
            // just Width, keeps the whole footprint constant regardless of
            // column widths.
            //
            // Deferred one more tick than the fix above needs strictly for
            // Width, so this runs AFTER ListView's own initial whole-rows
            // snap (itself deferred via BeginInvoke from OnHandleCreated)
            // has already settled - capturing Height any earlier would
            // lock in the raw, pre-snap value and immediately fight that
            // snap the moment it actually ran. BeginInvoke needs a real
            // handle to post to - this control may not have one yet at
            // this point in the Showcase's own construction, so that case
            // waits for HandleCreated first.
            void LockCurrentSize()
            {
                var lockedSize = control.Size;
                control.Resize += (sender, e) =>
                {
                    if (control.Size != lockedSize)
                    {
                        control.Size = lockedSize;
                    }
                };
            }

            if (control.IsHandleCreated)
            {
                control.BeginInvoke(new MethodInvoker(LockCurrentSize));
            }
            else
            {
                control.HandleCreated += (sender, e) => control.BeginInvoke(new MethodInvoker(LockCurrentSize));
            }
        }

        private static void PopulateListViewDemo(System.Windows.Forms.ListView listView)
        {
            listView.Columns.Add("Item", 180);
            listView.Columns.Add("Status", 100);

            // PropertyTable centers a row's editor control inside an
            // AutoSize middle row rather than stretching it (see
            // PropertyTable.AddControlToCell) - so the row height passed to
            // AddRow above only changes how much padding surrounds the
            // control, not the control's own size. The control's actual
            // rendered height comes from this Height instead.
            listView.Height = 220;

            // Enough rows to force a vertical scrollbar - only ~7 fit in
            // that height at once, so this doubles as a way to actually
            // exercise drag-select + scroll behavior here instead of only
            // in a consuming app.
            for (int i = 1; i <= 40; i++)
            {
                listView.Items.Add(new ListViewItem(new[] { "Row " + i, i % 2 == 0 ? "OK" : "Pending" }));
            }
        }

        private void AddDataGridSection(PropertyTable table)
        {
            table.AddSection("DataGridView");

            // Demonstrates actual DataSource binding - the one thing a
            // plain ListView (and anything built on it, like
            // ListView) simply cannot do at all.
            var source = new DataTable();
            source.Columns.Add("Track");
            source.Columns.Add("Artist");
            source.Columns.Add("Length");
            source.Rows.Add("Sample Song", "Sample Artist", "3:42");
            source.Rows.Add("Another Track", "Someone Else", "4:15");
            source.Rows.Add("Third One", "Someone Else", "2:58");

            var grid = UIStyles.DataGridViews.CreateStandard(source);
            grid.Dock = DockStyle.Fill;

            var disabledGrid = UIStyles.DataGridViews.CreateStandard(source.Copy());
            disabledGrid.Dock = DockStyle.Fill;
            disabledGrid.Enabled = false;

            table.AddRow(
                "CreateStandard",
                180,
                UIColumn.Percent(grid, 50),
                UIColumn.Percent(disabledGrid, 50));
        }

        private void AddPopupsSection(PropertyTable table)
        {
            table.AddSection("Popups (MessageBox / ToastForm / InfoPopupForm)");

            Button messageBoxButton = UIStyles.Buttons.CreateStandard("Show MessageBox", size: new Size(200, 32));
            messageBoxButton.Click += delegate
            {
                MessageBox.Show(
                    "This is a sample MessageBox.",
                    "Sample",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
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

            Button compactInfoPopupButton = UIStyles.Buttons.CreateStandard("Show InfoPopupForm (Compact)", size: new Size(220, 32));
            compactInfoPopupButton.Click += delegate
            {
                // Fixed-size mode meant for a short value like a slider's
                // percentage (see VolumeSlider) - not the free-form text the
                // plain demo above uses.
                _compactInfoPopup.ShowInfo("70%", compactInfoPopupButton);

                _compactInfoPopupHideTimer.Stop();
                _compactInfoPopupHideTimer.Start();
            };

            table.AddRow("MessageBox", messageBoxButton);
            table.AddRow("ToastForm", toastButton);
            table.AddRow("InfoPopupForm", infoPopupButton);
            table.AddRow("InfoPopupForm.Compact", compactInfoPopupButton);
        }
    }
}
