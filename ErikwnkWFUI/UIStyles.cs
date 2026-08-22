using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI
{
    /// <summary>
    /// The single entry point for ErikwnkWFUI's control factories and styling
    /// - start here rather than the individual classes under
    /// <see cref="ErikwnkWFUI.Factories"/>/<see cref="ErikwnkWFUI.Styles"/>
    /// (most of which are internal). Each nested class below (<see
    /// cref="Buttons"/>, <see cref="Labels"/>, ...) mirrors one control type
    /// with a set of <c>Create*</c> factory methods that return a
    /// pre-themed, ready-to-add WinForms control. See <see cref="Colors"/>
    /// for how to customize the color scheme (accent color and/or full
    /// light/dark theme) and <see cref="Language"/> for switching the
    /// language of built-in dialog text.
    /// </summary>
    public static class UIStyles
    {
        /// <summary>
        /// Language used by built-in dialogs/controls that ship their own text
        /// (update prompt, title bar tooltips). Set once at startup, before any
        /// ErikwnkWFUI form is created, to switch away from the English default.
        /// </summary>
        public static UILanguage Language
        {
            get { return UIStrings.Language; }
            set { UIStrings.Language = value; }
        }

        /// <summary>
        /// Read-only access to every color used across ErikwnkWFUI's controls,
        /// plus <see cref="SetAccent"/>/<see cref="ApplyTheme"/> to customize
        /// them. Defaults to a dark gray theme with a blue accent; nothing
        /// here needs to be set unless you want to change that.
        /// </summary>
        public static class Colors
        {
            public static Color Black { get { return UIColors.Black; } }

            public static Color BackgroundBlack { get { return UIColors.BackgroundBlack; } }
            public static Color BackgroundDark { get { return UIColors.BackgroundDark; } }
            public static Color BackgroundDarkElevated { get { return UIColors.BackgroundDarkElevated; } }
            public static Color BackgroundMedium { get { return UIColors.BackgroundMedium; } }
            public static Color BackgroundMediumElevated { get { return UIColors.BackgroundMediumElevated; } }
            public static Color BackgroundLight { get { return UIColors.BackgroundLight; } }
            public static Color BackgroundLighter { get { return UIColors.BackgroundLighter; } }

            public static Color PrimaryDarkDark { get { return UIColors.PrimaryDarkDark; } }
            public static Color PrimaryDark { get { return UIColors.PrimaryDark; } }
            public static Color Primary { get { return UIColors.Primary; } }
            public static Color PrimaryLight { get { return UIColors.PrimaryLight; } }

            public static Color SecondaryDark { get { return UIColors.SecondaryDark; } }
            public static Color Secondary { get { return UIColors.Secondary; } }
            public static Color SecondaryLight { get { return UIColors.SecondaryLight; } }

            public static Color GreenDark { get { return UIColors.GreenDark; } }
            public static Color Green { get { return UIColors.Green; } }
            public static Color GreenLight { get { return UIColors.GreenLight; } }
            public static Color GreenLighter { get { return UIColors.GreenLighter; } }

            public static Color YellowDark { get { return UIColors.YellowDark; } }
            public static Color Yellow { get { return UIColors.Yellow; } }
            public static Color YellowLight { get { return UIColors.YellowLight; } }
            public static Color YellowLighter { get { return UIColors.YellowLighter; } }

            public static Color RedDark { get { return UIColors.RedDark; } }
            public static Color Red { get { return UIColors.Red; } }
            public static Color RedLight { get { return UIColors.RedLight; } }

            public static Color White { get { return UIColors.White; } }
            public static Color TextPrimary { get { return UIColors.TextPrimary; } }
            public static Color TextPrimaryDim { get { return UIColors.TextPrimaryDim; } }
            public static Color TextSecondary { get { return UIColors.TextSecondary; } }
            public static Color TextTertiary { get { return UIColors.TextTertiary; } }
            public static Color TextDisabled { get { return UIColors.TextDisabled; } }
            public static Color TextMuted { get { return UIColors.TextMuted; } }

            public static Color BorderDark { get { return UIColors.BorderDark; } }
            public static Color BorderMedium { get { return UIColors.BorderMedium; } }
            public static Color BorderLight { get { return UIColors.BorderLight; } }
            public static Color BorderPrimary { get { return UIColors.BorderPrimary; } }
            public static Color BorderRed { get { return UIColors.BorderRed; } }

            public static Color AccentForeColor { get { return UIColors.AccentForeColor; } }

            public static Color HoverOverlay { get { return UIColors.HoverOverlay; } }
            public static Color ActiveOverlay { get { return UIColors.ActiveOverlay; } }
            public static Color Selection { get { return UIColors.Selection; } }

            public static Color Transparent { get { return UIColors.Transparent; } }
            public static Color OverlayDark { get { return UIColors.OverlayDark; } }
            public static Color OverlayMedium { get { return UIColors.OverlayMedium; } }
            public static Color OverlayLight { get { return UIColors.OverlayLight; } }

            /// <summary>
            /// Replaces the blue accent used throughout every built-in
            /// control (buttons, toggle switches, selection highlights,
            /// ...) with shades computed from a single color. Set once, as
            /// early as possible - before building any UI - same as
            /// <see cref="Language"/>; a couple of controls only read their
            /// accent color once at construction time, so they won't
            /// retroactively pick up a change made after they're built.
            /// </summary>
            public static void SetAccent(Color accent)
            {
                UIColors.SetAccent(accent);
            }

            /// <summary>
            /// Switches the base theme (backgrounds/text/borders), e.g.
            /// UIThemes.Light to move off the dark default. Same "call once,
            /// before building any UI" caveat as <see cref="SetAccent"/>. Combine
            /// freely with SetAccent - the two are independent (e.g. a light
            /// theme with a purple accent).
            /// </summary>
            public static void ApplyTheme(UIColorTheme theme)
            {
                UIColors.ApplyTheme(theme);
            }
        }

        /// <summary>The fonts used across ErikwnkWFUI's controls.</summary>
        public static class Fonts
        {
            public static Font Title { get { return UIFonts.Title; } }
            public static Font Normal { get { return UIFonts.Normal; } }
            public static Font Small { get { return UIFonts.Small; } }
            public static Font Monospace { get { return UIFonts.Monospace; } }
            public static Font Icon { get { return UIFonts.Icon; } }
            public static Font Emoji { get { return UIFonts.Emoji; } }
        }

        /// <summary>Flat, themed <see cref="Button"/>s in a few preset colors (<see cref="CreateStandard"/>, <see cref="CreatePrimary"/>, <see cref="CreateGreen"/>, <see cref="CreateRed"/>) plus a couple of special-purpose ones.</summary>
        public static class Buttons
        {
            public static Button CreateStandard(
                string text = "",
                string tooltip = "",
                Size? size = null,
                bool isIcon = false)
            {
                return UIButtonFactory.CreateStandard(
                    text,
                    tooltip,
                    size,
                    isIcon);
            }

            public static Button CreatePrimary(
                string text = "",
                string tooltip = "",
                Size? size = null,
                bool isIcon = false)
            {
                return UIButtonFactory.CreatePrimary(
                    text,
                    tooltip,
                    size,
                    isIcon);
            }

            public static Button CreateGreen(
                string text = "",
                string tooltip = "",
                Size? size = null,
                bool isIcon = false)
            {
                return UIButtonFactory.CreateGreen(
                    text,
                    tooltip,
                    size,
                    isIcon);
            }

            public static Button CreateRed(
                string text = "",
                string tooltip = "",
                Size? size = null,
                bool isIcon = false)
            {
                return UIButtonFactory.CreateRed(
                    text,
                    tooltip,
                    size,
                    isIcon);
            }

            public static Button CreateBrowse(
                string tooltip = "",
                Size? size = null,
                bool isIcon = true)
            {
                return UIButtonFactory.CreateBrowse(
                    tooltip,
                    size,
                    isIcon);
            }

            public static Button CreateIconButton(
                string text,
                int size = 32)
            {
                return UIButtonFactory.CreateIconButton(
                    text,
                    size);
            }

            /// <summary>
            /// Changes an already-created button's tooltip text - e.g. to
            /// re-translate it after a language switch.
            /// </summary>
            public static void UpdateTooltip(Button button, string tooltip)
            {
                UIButtonFactory.UpdateTooltip(button, tooltip);
            }
        }

        /// <summary>A themed <see cref="System.Windows.Forms.DataGridView"/> for data bound via <see cref="System.Windows.Forms.DataGridView.DataSource"/> - the control to reach for when rows come from a bound source rather than being added by hand.</summary>
        public static class DataGridViews
        {
            public static System.Windows.Forms.DataGridView CreateStandard(object dataSource = null)
            {
                return UIDataGridViewFactory.CreateStandard(dataSource);
            }
        }

        /// <summary>Themed <see cref="Label"/>s: <see cref="CreateTitle"/> (bold/large), <see cref="CreateNormal"/> (body text), <see cref="CreateMuted"/> (de-emphasized).</summary>
        public static class Labels
        {
            public static Label CreateTitle(string text = "")
            {
                return UILabelFactory.CreateTitle(text);
            }

            public static Label CreateNormal(string text = "")
            {
                return UILabelFactory.CreateNormal(text);
            }

            public static Label CreateMuted(string text = "")
            {
                return UILabelFactory.CreateMuted(text);
            }
        }

        /// <summary>Themed <see cref="TextBox"/>es.</summary>
        public static class TextBoxes
        {
            public static TextBox CreateStandard(
                string text = "",
                string placeholder = "")
            {
                return UITextBoxFactory.CreateStandard(
                    text,
                    placeholder);
            }

            public static TextBox CreateBorderstyleNone(
                string text = "",
                string placeholder = "")
            {
                return UITextBoxFactory.CreateBorderstyleNone(
                    text,
                    placeholder);
            }
        }

        /// <summary>A themed <see cref="ComboBox"/>. Note: only the edit portion follows the theme - the native dropdown list itself still renders with system colors (see ErikwnkWFUI/README.md).</summary>
        public static class ComboBoxes
        {
            public static ComboBox CreateStandard(
                ComboBoxStyle comboBoxStyle = ComboBoxStyle.DropDownList)
            {
                return UIComboBoxFactory.CreateStandard(
                    comboBoxStyle);
            }
        }

        /// <summary>Plain <see cref="Panel"/>s pre-filled with one of the theme's background shades - handy as containers/cards.</summary>
        public static class Panels
        {
            public static Panel CreateDark()
            {
                return UIPanelFactory.CreateDark();
            }

            public static Panel CreateMedium()
            {
                return UIPanelFactory.CreateMedium();
            }

            public static Panel CreateElevated()
            {
                return UIPanelFactory.CreateElevated();
            }

            public static Panel CreateTransparent()
            {
                return UIPanelFactory.CreateTransparent();
            }
            public static Panel CreatePrimary()
            {
                return UIPanelFactory.CreatePrimary();
            }
        }

        /// <summary>Themed <see cref="CheckBox"/>es.</summary>
        public static class CheckBoxes
        {
            public static CheckBox CreateStandard(
                string text = "",
                bool checkedState = true)
            {
                return UICheckBoxFactory.CreateStandard(
                    text,
                    checkedState);
            }

            public static CheckBox CreateCompact(
                bool checkedState = true)
            {
                return UICheckBoxFactory.CreateCompact(
                    checkedState);
            }
        }


        /// <summary>iOS-style on/off <see cref="Controls.ToggleSwitch"/>es in three sizes. Colors can be overridden per-instance - see <see cref="Controls.ToggleSwitch.CheckedBackColor"/>.</summary>
        public static class ToggleSwitches
        {
            public static ToggleSwitch CreateStandard(
                bool checkedState = true,
                string tooltipChecked = null,
                string tooltipUnchecked = null)
            {
                return UIToggleSwitchFactory.CreateStandard(
                    checkedState,
                    tooltipChecked,
                    tooltipUnchecked);
            }

            public static ToggleSwitch CreateSmall(
                bool checkedState = true,
                string tooltipChecked = null,
                string tooltipUnchecked = null)
            {
                return UIToggleSwitchFactory.CreateSmall(
                    checkedState,
                    tooltipChecked,
                    tooltipUnchecked);
            }

            public static ToggleSwitch CreateLarge(
                bool checkedState = true,
                string tooltipChecked = null,
                string tooltipUnchecked = null)
            {
                return UIToggleSwitchFactory.CreateLarge(
                    checkedState,
                    tooltipChecked,
                    tooltipUnchecked);
            }
        }
        /// <summary>Themed <see cref="TableLayoutPanel"/>s, pre-sized to the given column/row count.</summary>
        public static class TableLayoutPanels
        {
            public static TableLayoutPanel CreateStandard(
                int columnCount,
                int rowCount)
            {
                return UITableLayoutPanelFactory.CreateStandard(
                    columnCount,
                    rowCount);
            }

            public static TableLayoutPanel CreateDark(
                int columnCount,
                int rowCount)
            {
                return UITableLayoutPanelFactory.CreateDark(
                    columnCount,
                    rowCount);
            }
        }

        /// <summary>Creates a plain <see cref="ToolTip"/> component - note it isn't owned by any control, so dispose it yourself if you're not letting it live for the app's whole lifetime.</summary>
        public static class ToolTips
        {
            public static ToolTip CreateToolTip(
                string text = "")
            {
                return UIToolTipFactory.CreateToolTip(text);
            }
        }

        /// <summary>A dark-themed, multi-column <see cref="Controls.ListView"/> (Details view) with spreadsheet-style cell-range selection, hand-rolled column reordering/resizing, and Ctrl+C/Ctrl+Shift+C copy - see the class itself for the full behavior.</summary>
        public static class ListViews
        {
            public static System.Windows.Forms.ListView CreateStandard()
            {
                return UIListViewFactory.CreateStandard();
            }
        }

        /// <summary>A themed, owner-drawn, optionally drag-reorderable <see cref="Controls.ListBox"/> - a heavier alternative to the native ListBox with icons, custom item colors, and enumeration support.</summary>
        public static class ListBoxes
        {
            public static Controls.ListBox CreateStandard(
                string displayTextMember = null,
                bool allowReorder = false,
                bool showEnumeration = false)
            {
                return UIListBoxFactory.CreateStandard(
                    displayTextMember,
                    allowReorder,
                    showEnumeration);
            }
        }

        /// <summary>A <see cref="Controls.ListBox"/> wrapped with an optional header bar - see <see cref="ListBoxes"/> for the list box on its own.</summary>
        public static class ListBoxControls
        {
            public static ListBoxControl CreateStandard(
                string headerTitle = null,
                string displayTextMember = null,
                bool allowReorder = false,
                bool showEnumeration = false,
                ContentAlignment headerTextAlign = ContentAlignment.MiddleLeft)
            {
                return UIListBoxControlFactory.CreateStandard(
                    headerTitle,
                    displayTextMember,
                    allowReorder,
                    showEnumeration,
                    headerTextAlign);
            }
        }

        /// <summary>A themed key/value <see cref="Controls.PropertyTable"/> (label + editor per row, grouped into sections) - typical use is a settings/options panel.</summary>
        public static class PropertyTables
        {
            public static PropertyTable CreateStandard()
            {
                return UIPropertyTableFactory.CreateStandard();
            }
        }

        /// <summary>A themed <see cref="NumericUpDown"/>. Note: the up/down spinner buttons themselves stay system-colored - only the field's own colors follow the theme.</summary>
        public static class NumericUpDowns
        {
            public static NumericUpDown CreateStandard(
                decimal minimum = 0,
                decimal maximum = 100,
                decimal increment = 1,
                decimal value = 0)
            {
                return UINumericUpDownFactory.CreateStandard(
                    minimum,
                    maximum,
                    increment,
                    value);
            }
        }

        /// <summary>A plain, transparent <see cref="FlowLayoutPanel"/> with no padding/margin - a layout helper, not itself themed.</summary>
        public static class FlowLayoutPanels
        {
            public static FlowLayoutPanel CreateStandard()
            {
                return UIFlowLayoutPanelFactory.CreateStandard();
            }
        }

        /// <summary>A themed <see cref="ProgressBar"/> (continuous style, not the native blocky one).</summary>
        public static class ProgressBars
        {
            public static ProgressBar CreateGreen()
            {
                return UIProgressBarFactory.CreateGreen();
            }

            /// <summary>Like <see cref="CreateGreen"/> but without the border, and BackColor left for you to match your own panel - the closest a ProgressBar can get to a transparent background.</summary>
            public static ProgressBar CreateGreenTransparent()
            {
                return UIProgressBarFactory.CreateGreenTransparent();
            }

            /// <summary>Like <see cref="CreateGreen"/> but the fill follows the app-wide accent instead of the fixed green.</summary>
            public static ProgressBar CreatePrimary()
            {
                return UIProgressBarFactory.CreatePrimary();
            }

            /// <summary>Like <see cref="CreatePrimary"/> but without the border, and BackColor left for you to match your own panel - the accent-following equivalent of <see cref="CreateGreenTransparent"/>.</summary>
            public static ProgressBar CreatePrimaryTransparent()
            {
                return UIProgressBarFactory.CreatePrimaryTransparent();
            }

            /// <summary>Fill color follows Value instead of being fixed - red at Minimum, yellow at the midpoint, green at Maximum, blending smoothly between them. For a "status/health" bar where color communicates good/bad.</summary>
            public static ProgressBar CreateStatus()
            {
                return UIProgressBarFactory.CreateStatus();
            }

            /// <summary>Like <see cref="CreateStatus"/> but without the border, and BackColor left for you to match your own panel - the status-gradient equivalent of <see cref="CreateGreenTransparent"/>.</summary>
            public static ProgressBar CreateStatusTransparent()
            {
                return UIProgressBarFactory.CreateStatusTransparent();
            }
        }

        /// <summary>
        /// A thin, fully custom-drawn <see cref="SlimProgressBar"/> - no
        /// native Win32 control underneath (unlike <see cref="ProgressBars"/>),
        /// so no border/theming quirks to work around. For slim status-strip-
        /// style progress indicators rather than a prominent, full-size bar.
        /// </summary>
        public static class SlimProgressBars
        {
            public static SlimProgressBar CreateGreen()
            {
                return UISlimProgressBarFactory.CreateGreen();
            }

            /// <summary>Like <see cref="CreateGreen"/> but the fill follows the app-wide accent instead of the fixed green.</summary>
            public static SlimProgressBar CreatePrimary()
            {
                return UISlimProgressBarFactory.CreatePrimary();
            }

            /// <summary>Fill color follows Value instead of being fixed - red at Minimum, yellow at the midpoint, green at Maximum, blending smoothly between them. For a "status/health" bar where color communicates good/bad.</summary>
            public static SlimProgressBar CreateStatus()
            {
                return UISlimProgressBarFactory.CreateStatus();
            }
        }

        /// <summary>
        /// ErikwnkWFUI's own bundled icons (Web/Folder/Document/Application),
        /// plus <see cref="LoadEmbedded"/> for loading your own app's icon
        /// from its embedded resources the same way - see
        /// ErikwnkWFUI/README.md for why that's preferable to a loose file.
        /// </summary>
        public static class Icons
        {
            public static Image Web { get { return UIIcons.Web; } }
            public static Image Folder { get { return UIIcons.Folder; } }
            public static Image Document { get { return UIIcons.Document; } }
            public static Image Application { get { return UIIcons.Application; } }

            public static Image LoadEmbedded(Assembly assembly, string resourceName)
            {
                return UIIcons.LoadEmbedded(assembly, resourceName);
            }
        }
    }

}
