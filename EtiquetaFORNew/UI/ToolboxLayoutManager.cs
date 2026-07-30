using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace EtiquetaFORNew.UI
{
    /// <summary>
    /// Reorganiza os controles já criados da toolbox sem alterar seus eventos ou dados.
    /// </summary>
    public static class ToolboxLayoutManager
    {
        public static void Apply(Panel toolbox, Panel propertiesPanel, bool distributorMode)
        {
            if (toolbox == null) return;

            var controls = toolbox.Controls.Cast<Control>()
                .Where(control => control != propertiesPanel)
                .ToList();
            var positions = controls.ToDictionary(control => control, control => control.Bounds);
            var title = controls.OfType<Label>()
                .FirstOrDefault(label => label.Text.IndexOf("ELEMENTOS", StringComparison.OrdinalIgnoreCase) >= 0);
            var buttons = controls.OfType<Button>()
                .OrderBy(button => positions[button].Top)
                .ThenBy(button => positions[button].Left)
                .ToList();
            var combos = controls.OfType<ComboBox>().ToList();
            var labels = controls.OfType<Label>().Where(label => label != title).ToList();
            var pairs = combos.Select(combo => new FieldPair
            {
                Combo = combo,
                Label = FindLabel(combo, labels, positions),
                OriginalLeft = positions[combo].Left,
                OriginalTop = positions[combo].Top
            }).OrderBy(pair => pair.OriginalTop).ThenBy(pair => pair.OriginalLeft).ToList();

            foreach (Control control in controls)
                toolbox.Controls.Remove(control);
            if (propertiesPanel != null)
                toolbox.Controls.Remove(propertiesPanel);

            toolbox.SuspendLayout();
            try
            {
                toolbox.AutoScroll = true;
                toolbox.AutoScrollMinSize = Size.Empty;
                toolbox.BackColor = ThemeManager.PanelBackground;

                var root = CreateRoot();
                if (title != null)
                {
                    PrepareTitle(title);
                    AddRow(root, title, 34);
                }

                var regularPairs = pairs.Where(pair => pair.OriginalLeft < 180).ToList();
                var distributorPairs = pairs.Where(pair => pair.OriginalLeft >= 180).ToList();
                var content = CreateColumns();
                content.Controls.Add(CreateFieldsStack(regularPairs, distributorMode), 0, 0);

                if (distributorMode && distributorPairs.Count > 0)
                    content.Controls.Add(CreateFieldsStack(distributorPairs, true), 1, 0);
                else
                    content.Controls.Add(CreateStandardButtons(buttons), 1, 0);

                AddRow(root, content, Math.Max(
                    CalculateFieldsHeight(regularPairs.Count, distributorMode),
                    distributorMode
                        ? CalculateFieldsHeight(distributorPairs.Count, true)
                        : CalculateStandardButtonsHeight(buttons.Count)));

                if (distributorMode && buttons.Count > 0)
                    AddRow(root, CreateDistributorButtons(buttons), CalculateDistributorButtonsHeight(buttons.Count));

                if (propertiesPanel != null)
                {
                    propertiesPanel.Dock = DockStyle.Top;
                    propertiesPanel.Margin = new Padding(0, 10, 0, 0);
                    root.RowCount++;
                    root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                    root.Controls.Add(propertiesPanel, 0, root.RowCount - 1);
                }

                toolbox.Controls.Add(root);
            }
            finally
            {
                toolbox.ResumeLayout(true);
            }
        }

        private static TableLayoutPanel CreateRoot()
        {
            var root = new TableLayoutPanel
            {
                Name = "toolboxResponsiveLayout",
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 0,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                BackColor = ThemeManager.PanelBackground
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            return root;
        }

        private static TableLayoutPanel CreateColumns()
        {
            var columns = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                BackColor = ThemeManager.PanelBackground
            };
            columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            return columns;
        }

        private static TableLayoutPanel CreateFieldsStack(IList<FieldPair> pairs, bool compact)
        {
            var stack = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 0,
                Margin = new Padding(0, 0, 5, 0),
                Padding = new Padding(2, 0, 2, 0),
                BackColor = ThemeManager.PanelBackground
            };
            stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            int labelHeight = compact ? 17 : 19;
            int comboHeight = compact ? 27 : 29;

            foreach (FieldPair pair in pairs)
            {
                if (pair.Label != null)
                {
                    PrepareFieldLabel(pair.Label, compact);
                    AddRow(stack, pair.Label, labelHeight);
                }
                PrepareCombo(pair.Combo, compact);
                AddRow(stack, pair.Combo, comboHeight);
            }
            return stack;
        }

        private static TableLayoutPanel CreateStandardButtons(IList<Button> buttons)
        {
            var stack = CreateButtonTable(1);
            foreach (Button button in buttons)
            {
                PrepareButton(button);
                AddRow(stack, button, 42);
            }
            return stack;
        }

        private static TableLayoutPanel CreateDistributorButtons(IList<Button> buttons)
        {
            var grid = CreateButtonTable(2);
            int rows = (int)Math.Ceiling(buttons.Count / 2D);
            grid.RowCount = rows;
            for (int row = 0; row < rows; row++)
                grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            for (int index = 0; index < buttons.Count; index++)
            {
                PrepareButton(buttons[index]);
                grid.Controls.Add(buttons[index], index % 2, index / 2);
            }
            return grid;
        }

        private static TableLayoutPanel CreateButtonTable(int columns)
        {
            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = columns,
                RowCount = 0,
                Margin = new Padding(4, 0, 0, 0),
                Padding = new Padding(2),
                BackColor = ThemeManager.PanelBackground
            };
            for (int index = 0; index < columns; index++)
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / columns));
            return table;
        }

        private static Label FindLabel(
            ComboBox combo, IEnumerable<Label> labels, IDictionary<Control, Rectangle> positions)
        {
            Rectangle comboBounds = positions[combo];
            return labels
                .Where(label =>
                {
                    Rectangle labelBounds = positions[label];
                    return Math.Abs(labelBounds.Left - comboBounds.Left) <= 20
                        && labelBounds.Bottom <= comboBounds.Top + 3;
                })
                .OrderBy(label => comboBounds.Top - positions[label].Bottom)
                .FirstOrDefault();
        }

        private static void PrepareTitle(Label title)
        {
            title.Dock = DockStyle.Fill;
            title.AutoSize = false;
            title.Margin = new Padding(2, 0, 2, 4);
            title.Padding = Padding.Empty;
            title.Font = ThemeManager.SectionFont;
            title.ForeColor = ThemeManager.TextPrimary;
            title.TextAlign = ContentAlignment.MiddleLeft;
        }

        private static void PrepareFieldLabel(Label label, bool compact)
        {
            label.Dock = DockStyle.Fill;
            label.AutoSize = false;
            label.Margin = new Padding(3, compact ? 1 : 2, 3, 0);
            label.Padding = Padding.Empty;
            label.Font = new Font("Segoe UI Semibold", 8F, FontStyle.Bold);
            label.ForeColor = ThemeManager.TextSecondary;
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.AutoEllipsis = false;
        }

        private static void PrepareCombo(ComboBox combo, bool compact)
        {
            combo.Dock = DockStyle.Fill;
            combo.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            combo.Margin = new Padding(3, 0, compact ? 6 : 4, compact ? 3 : 5);
            combo.Font = new Font("Segoe UI", compact ? 8F : 8.5F);
            combo.IntegralHeight = true;
        }

        private static void PrepareButton(Button button)
        {
            button.Dock = DockStyle.Fill;
            button.Margin = new Padding(3);
            button.Padding = new Padding(10, 0, 6, 0);
            button.TextAlign = ContentAlignment.MiddleLeft;
        }

        private static void AddRow(TableLayoutPanel table, Control control, int height)
        {
            int row = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
            table.Controls.Add(control, 0, row);
        }

        private static int CalculateFieldsHeight(int count, bool compact)
        {
            return count * (compact ? 44 : 48);
        }

        private static int CalculateStandardButtonsHeight(int count)
        {
            return count * 42 + 4;
        }

        private static int CalculateDistributorButtonsHeight(int count)
        {
            return (int)Math.Ceiling(count / 2D) * 40 + 4;
        }

        private sealed class FieldPair
        {
            public Label Label { get; set; }
            public ComboBox Combo { get; set; }
            public int OriginalLeft { get; set; }
            public int OriginalTop { get; set; }
        }
    }
}
