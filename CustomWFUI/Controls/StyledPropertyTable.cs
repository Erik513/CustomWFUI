using System.Drawing;
using System.Windows.Forms;
using static CustomWFUI.UIStyles;

namespace CustomWFUI.Controls
{
    /// <summary>
    /// A two-column "label: editor" table, grouped into optional labeled
    /// sections - typical use is a settings/options panel. Build it up with
    /// <see cref="AddSection"/> and <see cref="AddRow(string,Control[])"/>;
    /// a row can hold more than one editor control side by side via the
    /// <see cref="UIColumn"/>-based overloads.
    /// </summary>
    public partial class StyledPropertyTable : UserControl
    {
        private const int DefaultLabelColumnWidth = 130;
        private const int DefaultRowHeight = 42;
        private const int DefaultSectionHeight = 32;
        private const int LabelLeftPadding = 10;

        private readonly TableLayoutPanel _layout;

        public StyledPropertyTable()
        {
            Dock = DockStyle.Top;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            BackColor = Colors.BackgroundMedium;
            Margin = new Padding(0);
            Padding = new Padding(0);

            _layout = CreateMainLayout();

            Controls.Add(_layout);
        }

        /// <summary>Removes every row and section, leaving an empty table.</summary>
        public void ClearRows()
        {
            _layout.Controls.Clear();
            _layout.RowStyles.Clear();
            _layout.RowCount = 0;
        }

        /// <summary>Adds a full-width section header row.</summary>
        public void AddSection(string title)
        {
            int row = AddRowStyle(DefaultSectionHeight);

            Label label = CreateSectionLabel(title);

            _layout.Controls.Add(label, 0, row);
            _layout.SetColumnSpan(label, 2);
        }

        /// <summary>
        /// Adds a row: a label on the left, one or more editor controls
        /// sharing the remaining width equally on the right (each gets
        /// <see cref="UIColumn.Auto"/> sizing). Use the
        /// <see cref="AddRow(string,UIColumn[])"/> overload instead if the
        /// controls need different widths.
        /// </summary>
        public void AddRow(
            string labelText,
            params Control[] controls)
        {
            UIColumn[] columns = CreateAutoColumns(controls);

            AddRow(
                labelText,
                DefaultRowHeight,
                columns);
        }

        /// <inheritdoc cref="AddRow(string,Control[])"/>
        public void AddRow(
            string labelText,
            int rowHeight,
            params Control[] controls)
        {
            UIColumn[] columns = CreateAutoColumns(controls);

            AddRow(
                labelText,
                rowHeight,
                columns);
        }

        /// <summary>Adds a row with explicit per-column sizing - see <see cref="UIColumn.Auto"/>/<see cref="UIColumn.Absolute"/>/<see cref="UIColumn.Percent"/>.</summary>
        public void AddRow(
            string labelText,
            params UIColumn[] columns)
        {
            AddRow(
                labelText,
                DefaultRowHeight,
                columns);
        }

        /// <inheritdoc cref="AddRow(string,UIColumn[])"/>
        public void AddRow(
            string labelText,
            int rowHeight,
            params UIColumn[] columns)
        {
            int row = AddRowStyle(rowHeight);

            Label label = CreateRowLabel(labelText);
            Panel editorArea = CreateEditorAreaPanel();
            TableLayoutPanel editorLayout = CreateEditorLayout(columns);

            editorArea.Controls.Add(editorLayout);

            _layout.Controls.Add(label, 0, row);
            _layout.Controls.Add(editorArea, 1, row);
        }

        private UIColumn[] CreateAutoColumns(Control[] controls)
        {
            if (controls == null || controls.Length == 0)
                return new[] { UIColumn.Auto(null) };

            UIColumn[] columns = new UIColumn[controls.Length];

            for (int i = 0; i < controls.Length; i++)
                columns[i] = UIColumn.Auto(controls[i]);

            return columns;
        }

        // Every panel/TableLayoutPanel in this file used to have
        // BackColor=Transparent, relying on WinForms' built-in "ask my
        // parent to paint what's behind me" fake-transparency support to
        // make them blend into their surroundings. With this many nested
        // transparent layers (main layout > editor area > editor layout >
        // wrapper, four levels deep for a single control), that mechanism
        // showed real, reproducible ghosting on live screen captures
        // (CopyFromScreen and PrintWindow both, so not a capture-tooling
        // artifact): a control's neighbor - another row's label, or another
        // control entirely - leaking into its painted area. Every layer now
        // gets the same opaque color it would have resolved to anyway, so
        // there is no "ask parent" indirection left for anything to get
        // wrong.
        private TableLayoutPanel CreateMainLayout()
        {
            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 2,
                RowCount = 0,
                BackColor = Colors.BackgroundMedium,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };

            layout.ColumnStyles.Add(
                new ColumnStyle(
                    SizeType.Absolute,
                    DefaultLabelColumnWidth));

            layout.ColumnStyles.Add(
                new ColumnStyle(
                    SizeType.Percent,
                    100));

            return layout;
        }

        private TableLayoutPanel CreateEditorLayout(UIColumn[] columns)
        {
            int columnCount = columns == null || columns.Length == 0
                ? 1
                : columns.Length;

            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = columnCount,
                RowCount = 1,
                BackColor = Colors.BackgroundMedium,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };

            layout.RowStyles.Add(
                new RowStyle(
                    SizeType.Percent,
                    100));

            for (int i = 0; i < columnCount; i++)
            {
                UIColumn column = columns != null && i < columns.Length
                    ? columns[i]
                    : UIColumn.Auto(null);

                layout.ColumnStyles.Add(
                    column.Style);

                Panel cellPanel = CreateEditorCellPanel();

                if (column.Control != null)
                {
                    ConfigureEditorControl(column.Control);
                    AddControlToCell(cellPanel, column.Control);
                }

                layout.Controls.Add(cellPanel, i, 0);
            }

            return layout;
        }

        private void AddControlToCell(
            Panel cellPanel,
            Control control)
        {
            if (cellPanel == null || control == null)
                return;

            TableLayoutPanel wrapper = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Colors.BackgroundLight,
                Padding = new Padding(6, 0, 8, 0),
                Margin = new Padding(0)
            };

            wrapper.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100));

            wrapper.RowStyles.Add(
                new RowStyle(SizeType.Percent, 50));

            wrapper.RowStyles.Add(
                new RowStyle(SizeType.AutoSize));

            wrapper.RowStyles.Add(
                new RowStyle(SizeType.Percent, 50));

            wrapper.Controls.Add(control, 0, 1);

            cellPanel.Controls.Add(wrapper);
        }

        private int AddRowStyle(int height)
        {
            int row = _layout.RowCount;

            _layout.RowCount++;

            _layout.RowStyles.Add(
                new RowStyle(
                    SizeType.Absolute,
                    height));

            return row;
        }

        private Label CreateRowLabel(string text)
        {
            Label label = Labels.CreateNormal(text ?? "");

            label.Dock = DockStyle.Fill;
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.Padding = new Padding(LabelLeftPadding, 0, 0, 0);
            label.Margin = new Padding(0);
            label.BackColor = Colors.BackgroundMedium;

            return label;
        }

        private Label CreateSectionLabel(string title)
        {
            Label label = Labels.CreateTitle(title ?? "");

            label.Dock = DockStyle.Fill;
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.Padding = new Padding(LabelLeftPadding, 0, 0, 0);
            label.Margin = new Padding(0);
            label.BackColor = Colors.BackgroundDark;

            return label;
        }

        private Panel CreateEditorAreaPanel()
        {
            return new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Colors.BackgroundMedium,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };
        }

        private Panel CreateEditorCellPanel()
        {
            return new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Colors.BackgroundLight,
                Padding = new Padding(0),
                // Right margin matches the existing top/bottom margin, so
                // the table's own background shows as a border on all three
                // sides - previously only 0 on the right, so each editor
                // cell ran flush to the table's right edge with no border
                // there.
                Margin = new Padding(0, 2, 2, 2)
            };
        }

        private void ConfigureEditorControl(Control control)
        {
            if (control == null)
                return;

            if (control is ToggleSwitch)
            {
                control.Dock = DockStyle.None;
                control.Anchor = AnchorStyles.Left;
                control.Margin = new Padding(0);
                return;
            }

            // A checkbox's box+label has a natural, fixed preferred size -
            // stretching it to Fill this row's whole (often much wider)
            // cell doesn't gain anything, so it gets the same left-anchored
            // treatment as ToggleSwitch/Button instead of the Fill every
            // other control type gets by default below.
            if (control is CheckBox)
            {
                control.Dock = DockStyle.None;
                control.Anchor = AnchorStyles.Left;
                control.Margin = new Padding(0);
                return;
            }

            if (control is TextBox)
            {
                TextBox textBox = (TextBox)control;

                textBox.Multiline = false;
                textBox.BorderStyle = BorderStyle.None;
                textBox.Dock = DockStyle.Fill;
                textBox.Margin = new Padding(0);

                return;
            }

            if (control is ComboBox)
            {
                control.Dock = DockStyle.Fill;
                control.Margin = new Padding(0);
                return;
            }

            if (control is Button)
            {
                control.Dock = DockStyle.None;
                control.Anchor = AnchorStyles.Left;
                control.Margin = new Padding(0);
                return;
            }
            if (control is NumericUpDown)
            {
                control.Dock = DockStyle.Left;
                control.Margin = new Padding(0);
                return;
            }

            control.Dock = DockStyle.Fill;
            control.Margin = new Padding(0);
        }
    }
}