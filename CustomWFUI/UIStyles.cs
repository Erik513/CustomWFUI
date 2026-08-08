using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using CustomWFUI.Controls;
using CustomWFUI.Factories;
using CustomWFUI.Styles;

namespace CustomWFUI
{
    public static class UIStyles
    {
        /// <summary>
        /// Language used by built-in dialogs/controls that ship their own text
        /// (update prompt, title bar tooltips). Set once at startup, before any
        /// CustomWFUI form is created, to switch away from the English default.
        /// </summary>
        public static UILanguage Language
        {
            get { return UIStrings.Language; }
            set { UIStrings.Language = value; }
        }

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

            public static Color HoverOverlay { get { return UIColors.HoverOverlay; } }
            public static Color ActiveOverlay { get { return UIColors.ActiveOverlay; } }
            public static Color Selection { get { return UIColors.Selection; } }

            public static Color Transparent { get { return UIColors.Transparent; } }
            public static Color OverlayDark { get { return UIColors.OverlayDark; } }
            public static Color OverlayMedium { get { return UIColors.OverlayMedium; } }
            public static Color OverlayLight { get { return UIColors.OverlayLight; } }
        }

        public static class Fonts
        {
            public static Font Title { get { return UIFonts.Title; } }
            public static Font Normal { get { return UIFonts.Normal; } }
            public static Font Small { get { return UIFonts.Small; } }
            public static Font Monospace { get { return UIFonts.Monospace; } }
            public static Font Icon { get { return UIFonts.Icon; } }
            public static Font Emoji { get { return UIFonts.Emoji; } }
        }

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

            public static Button CreateDanger(
                string text = "",
                string tooltip = "",
                Size? size = null,
                bool isIcon = false)
            {
                return UIButtonFactory.CreateDanger(
                    text,
                    tooltip,
                    size,
                    isIcon);
            }

            public static Button CreateBrowseInFolder(
                string tooltip = "",
                Size? size = null,
                bool isIcon = true)
            {
                return UIButtonFactory.CreateBrowseInFolder(
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
        }

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

        public static class ComboBoxes
        {
            public static ComboBox CreateStandard(
                ComboBoxStyle comboBoxStyle = ComboBoxStyle.DropDownList)
            {
                return UIComboBoxFactory.CreateStandard(
                    comboBoxStyle);
            }
        }

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

        public static class ToolTips
        {
            public static ToolTip CreateToolTip(
                string text = "")
            {
                return UIToolTipFactory.CreateToolTip(text);
            }
        }

        public static class StyledListBoxes
        {
            public static StyledListBox Create(
                string displayTextMember = null,
                bool allowReorder = false,
                bool showEnumeration = false)
            {
                return UIStyledListBoxFactory.Create(
                    displayTextMember,
                    allowReorder,
                    showEnumeration);
            }
        }

        public static class StyledListBoxControls
        {
            public static StyledListBoxControl Create(
                string headerTitle = null,
                string displayTextMember = null,
                bool allowReorder = false,
                bool showEnumeration = false,
                ContentAlignment headerTextAlign = ContentAlignment.MiddleLeft)
            {
                return UIStyledListBoxControlFactory.Create(
                    headerTitle,
                    displayTextMember,
                    allowReorder,
                    showEnumeration,
                    headerTextAlign);
            }
        }

        public static class PropertyTables
        {
            public static StyledPropertyTable Create()
            {
                return UIStyledPropertyTableFactory.Create();
            }
        }

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

        public static class FlowLayoutPanels
        {
            public static FlowLayoutPanel CreateStandard()
            {
                return UIFlowLayoutPanelFactory.CreateStandard();
            }
        }

        public static class ProgressBars
        {
            public static ProgressBar CreateStandard()
            {
                return UIProgressBarFactory.CreateStandard();
            }
        }

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
