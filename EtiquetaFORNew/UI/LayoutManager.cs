using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace EtiquetaFORNew.UI
{
    /// <summary>Composição visual reutilizável, sem dependência das regras do designer.</summary>
    public static class LayoutManager
    {
        public static Panel CreateHeader(string title)
        {
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 48,
                BackColor = ThemeManager.HeaderBackground,
                Padding = new Padding(18, 0, 12, 0)
            };
            header.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = title,
                Font = ThemeManager.HeaderFont,
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleLeft
            });
            header.Controls.Add(new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 3,
                BackColor = ThemeManager.SmartPrintOrange
            });
            return header;
        }

        public static void EnableCanvasShadow(Panel workspace, Control canvas)
        {
            workspace.BackColor = ThemeManager.CanvasBackground;
            canvas.BackColor = ThemeManager.PanelBackground;
            canvas.Margin = new Padding(12);
            workspace.Paint += (s, e) =>
            {
                var shadow = new Rectangle(canvas.Left + 7, canvas.Top + 7, canvas.Width, canvas.Height);
                using (var brush = new SolidBrush(ThemeManager.Shadow))
                    e.Graphics.FillRectangle(brush, shadow);
            };
            canvas.LocationChanged += (s, e) => workspace.Invalidate();
            canvas.SizeChanged += (s, e) => workspace.Invalidate();
        }

        public static void ConvertConfigurationToAccordion(Panel panel)
        {
            var controls = panel.Controls.Cast<Control>().ToList();
            var boundaries = controls.OfType<Label>().Where(IsAccordionHeading).OrderBy(c => c.Top).ToList();
            if (boundaries.Count == 0) return;
            var title = controls.OfType<Label>().FirstOrDefault(c => c.Text.Contains("CONFIGURAÇÕES"));
            foreach (Control control in controls) panel.Controls.Remove(control);
            var header = new Label
            {
                Dock = DockStyle.Top,
                Height = 44,
                Padding = new Padding(12, 0, 0, 0),
                Text = "PROPRIEDADES DA PÁGINA",
                Font = ThemeManager.SectionFont,
                ForeColor = ThemeManager.TextPrimary,
                BackColor = ThemeManager.PanelBackground,
                TextAlign = ContentAlignment.MiddleLeft
            };
            if (title != null) title.Dispose();
            var host = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = ThemeManager.PanelBackground,
                Padding = new Padding(8, 6, 8, 12)
            };
            var sections = new List<AccordionSection>();
            for (int index = 0; index < boundaries.Count; index++)
            {
                Label boundary = boundaries[index];
                int start = boundary.Top;
                int end = index + 1 < boundaries.Count ? boundaries[index + 1].Top : int.MaxValue;
                var group = controls
                    .Where(c => c != boundary && c.Top >= start && c.Top < end && !(c is Panel && c.Height <= 3))
                    .OrderBy(c => c.Top).ToList();
                string sectionTitle = CleanHeading(boundary.Text);
                int bodyHeight = CalculateSectionBodyHeight(sectionTitle, group);
                var section = new AccordionSection(sectionTitle, bodyHeight);
                ArrangeSectionControls(section.Body, sectionTitle, group);
                section.Width = Math.Max(280, panel.ClientSize.Width - 36);
                section.Margin = new Padding(0, 0, 0, 7);
                section.ExpandedChanged += (s, e) =>
                {
                    if (!section.Expanded) return;
                    foreach (var other in sections.Where(item => item != section)) other.Expanded = false;
                };
                sections.Add(section);
                host.Controls.Add(section);
                boundary.Dispose();
            }
            host.SizeChanged += (s, e) =>
            {
                int width = Math.Max(260, host.ClientSize.Width - host.Padding.Horizontal - 22);
                foreach (AccordionSection section in sections) section.Width = width;
            };
            panel.BackColor = ThemeManager.PanelBackground;
            panel.Padding = Padding.Empty;
            panel.Controls.Add(host);
            panel.Controls.Add(header);
            if (sections.Count > 0) sections[0].Expanded = true;
        }

        private static bool IsAccordionHeading(Label label)
        {
            string text = label.Text ?? string.Empty;
            return text.Contains("Dimensões") || text.Contains("Impressão")
                || text.Contains("Layout da Página") || text.Contains("Margens da Página")
                || text.Contains("Zoom");
        }

        private static string CleanHeading(string text)
        {
            if (text.Contains("Dimensões")) return "Dimensões da Etiqueta";
            if (text.Contains("Impressão")) return "Impressão";
            if (text.Contains("Layout")) return "Layout da Página";
            if (text.Contains("Margens")) return "Margens da Página";
            if (text.Contains("Zoom")) return "Zoom";
            return text;
        }

        private static int CalculateSectionBodyHeight(string title, IList<Control> controls)
        {
            if (title == "Zoom") return 60;
            int editorCount = controls.Count(IsFieldControl);
            int checkBoxCount = controls.OfType<CheckBox>().Count();
            bool hasSubheading = controls.OfType<Label>()
                .Any(label => label.Text.Contains("Espaçamentos"));
            return 14 + editorCount * 38 + checkBoxCount * 32 + (hasSubheading ? 34 : 0);
        }

        private static void ArrangeSectionControls(
            Panel body, string title, IList<Control> sourceControls)
        {
            if (title == "Zoom")
            {
                ArrangeZoomControls(body, sourceControls);
                return;
            }

            var positions = sourceControls.ToDictionary(control => control, control => control.Bounds);
            var labels = sourceControls.OfType<Label>().ToList();
            var usedLabels = new HashSet<Label>();
            Label spacingHeading = labels.FirstOrDefault(label => label.Text.Contains("Espaçamentos"));
            var fields = sourceControls.Where(IsFieldControl)
                .OrderBy(control => positions[control].Top)
                .ThenBy(control => positions[control].Left)
                .ToList();

            var table = CreateFieldsTable();
            bool spacingAdded = false;
            foreach (Control field in fields)
            {
                Rectangle fieldBounds = positions[field];
                if (!spacingAdded && spacingHeading != null && fieldBounds.Top > positions[spacingHeading].Top)
                {
                    AddSubheadingRow(table, spacingHeading);
                    usedLabels.Add(spacingHeading);
                    spacingAdded = true;
                }

                Label fieldLabel = labels
                    .Where(label => !usedLabels.Contains(label)
                        && label != spacingHeading
                        && Math.Abs(positions[label].Top - fieldBounds.Top) <= 5
                        && positions[label].Left < fieldBounds.Left)
                    .OrderByDescending(label => positions[label].Left)
                    .FirstOrDefault();
                Label unitLabel = labels
                    .Where(label => !usedLabels.Contains(label)
                        && label != spacingHeading
                        && string.Equals(label.Text.Trim(), "mm", StringComparison.OrdinalIgnoreCase)
                        && Math.Abs(positions[label].Top - fieldBounds.Top) <= 5
                        && positions[label].Left >= fieldBounds.Right)
                    .OrderBy(label => positions[label].Left)
                    .FirstOrDefault();

                if (fieldLabel != null) usedLabels.Add(fieldLabel);
                if (unitLabel != null) usedLabels.Add(unitLabel);
                AddFieldRow(table, fieldLabel, field, unitLabel);
            }

            if (!spacingAdded && spacingHeading != null)
                AddSubheadingRow(table, spacingHeading);

            foreach (CheckBox checkBox in sourceControls.OfType<CheckBox>())
                AddSpanningControlRow(table, checkBox, 32);
            foreach (GroupBox groupBox in sourceControls.OfType<GroupBox>())
                AddSpanningControlRow(table, groupBox, Math.Max(60, groupBox.Height));

            body.Controls.Add(table);
        }

        private static TableLayoutPanel CreateFieldsTable()
        {
            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 3,
                RowCount = 0,
                Margin = Padding.Empty,
                Padding = new Padding(4, 4, 4, 6),
                BackColor = ThemeManager.PanelBackground
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58F));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 34F));
            return table;
        }

        private static void AddFieldRow(
            TableLayoutPanel table, Label label, Control field, Label unit)
        {
            int row = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
            if (label == null) label = new Label { Text = string.Empty };
            PrepareFieldLabel(label);
            PrepareField(field);
            table.Controls.Add(label, 0, row);
            table.Controls.Add(field, 1, row);

            if (unit != null)
            {
                PrepareUnitLabel(unit);
                table.Controls.Add(unit, 2, row);
            }
            else
            {
                table.SetColumnSpan(field, 2);
            }
        }

        private static void AddSubheadingRow(TableLayoutPanel table, Label heading)
        {
            int row = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            heading.Dock = DockStyle.Fill;
            heading.AutoSize = false;
            heading.Margin = new Padding(4, 6, 4, 3);
            heading.Padding = Padding.Empty;
            heading.Font = ThemeManager.SectionFont;
            heading.ForeColor = ThemeManager.SmartPrintOrange;
            heading.TextAlign = ContentAlignment.MiddleLeft;
            table.Controls.Add(heading, 0, row);
            table.SetColumnSpan(heading, 3);
        }

        private static void AddSpanningControlRow(
            TableLayoutPanel table, Control control, int height)
        {
            int row = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
            control.Dock = DockStyle.Fill;
            control.Margin = new Padding(4, 4, 4, 6);
            table.Controls.Add(control, 0, row);
            table.SetColumnSpan(control, 3);
        }

        private static void PrepareFieldLabel(Label label)
        {
            label.Dock = DockStyle.Fill;
            label.AutoSize = false;
            label.Margin = new Padding(4, 3, 8, 5);
            label.Padding = Padding.Empty;
            label.Font = ThemeManager.ButtonFont;
            label.ForeColor = ThemeManager.TextPrimary;
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.AutoEllipsis = true;
        }

        private static void PrepareUnitLabel(Label label)
        {
            label.Dock = DockStyle.Fill;
            label.AutoSize = false;
            label.Margin = new Padding(6, 3, 0, 5);
            label.Padding = Padding.Empty;
            label.Font = ThemeManager.ButtonFont;
            label.ForeColor = ThemeManager.TextSecondary;
            label.TextAlign = ContentAlignment.MiddleLeft;
        }

        private static void PrepareField(Control field)
        {
            field.Dock = DockStyle.Fill;
            field.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            field.Margin = new Padding(4, 6, 4, 7);
            field.MinimumSize = new Size(0, 24);
            ThemeManager.StyleInput(field);
            field.Margin = new Padding(4, 6, 4, 7);
            if (field is NumericUpDown numeric)
                numeric.TextAlign = HorizontalAlignment.Right;
        }

        private static void ArrangeZoomControls(Panel body, IList<Control> controls)
        {
            var row = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 48,
                ColumnCount = 3,
                RowCount = 1,
                Padding = new Padding(48, 8, 48, 4),
                Margin = Padding.Empty,
                BackColor = ThemeManager.PanelBackground
            };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 42F));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 42F));
            var buttons = controls.OfType<Button>().OrderBy(button => button.Left).ToList();
            var label = controls.OfType<Label>().FirstOrDefault();
            if (buttons.Count > 0) PrepareZoomControl(buttons[0]);
            if (label != null) PrepareZoomControl(label);
            if (buttons.Count > 1) PrepareZoomControl(buttons[1]);
            if (buttons.Count > 0) row.Controls.Add(buttons[0], 0, 0);
            if (label != null) row.Controls.Add(label, 1, 0);
            if (buttons.Count > 1) row.Controls.Add(buttons[1], 2, 0);
            body.Controls.Add(row);
        }

        private static void PrepareZoomControl(Control control)
        {
            control.Dock = DockStyle.Fill;
            control.Margin = new Padding(3, 0, 3, 0);
            if (control is Label label)
                label.TextAlign = ContentAlignment.MiddleCenter;
        }

        private static bool IsFieldControl(Control control)
        {
            return control is ComboBox || control is NumericUpDown || control is TextBox;
        }
    }

    internal sealed class AccordionSection : UserControl
    {
        private readonly Button header;
        private readonly int expandedHeight;
        private bool expanded;
        public AccordionSection(string title, int bodyHeight)
        {
            expandedHeight = bodyHeight + 38;
            BackColor = ThemeManager.PanelBackground;
            BorderStyle = BorderStyle.FixedSingle;
            Height = 38;
            Body = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ThemeManager.PanelBackground,
                Padding = new Padding(6),
                Visible = false
            };
            header = new Button
            {
                Dock = DockStyle.Top,
                Height = 37,
                Text = "▶  " + title,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0),
                Font = ThemeManager.SectionFont,
                ForeColor = ThemeManager.TextPrimary,
                BackColor = ThemeManager.StatusBackground,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            header.FlatAppearance.BorderSize = 0;
            header.FlatAppearance.MouseOverBackColor = ThemeManager.HoverBackground;
            header.Click += (s, e) => Expanded = !Expanded;
            Controls.Add(Body);
            Controls.Add(header);
        }

        public Panel Body { get; }
        public event EventHandler ExpandedChanged;
        public bool Expanded
        {
            get { return expanded; }
            set
            {
                if (expanded == value) return;
                expanded = value;
                Body.Visible = value;
                Height = value ? expandedHeight : 38;
                header.Text = (value ? "▼  " : "▶  ") + header.Text.Substring(3);
                ExpandedChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
