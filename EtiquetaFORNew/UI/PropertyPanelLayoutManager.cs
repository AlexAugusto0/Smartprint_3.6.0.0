using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace EtiquetaFORNew.UI
{
    /// <summary>
    /// Organiza visualmente o painel de propriedades sem reparentear controles
    /// nem interferir nos eventos e na atualização dos elementos.
    /// </summary>
    public static class PropertyPanelLayoutManager
    {
        private const int OuterPadding = 12;
        private const int LabelColumnWidth = 112;
        private const int RowHeight = 32;
        private const int RowGap = 4;

        private sealed class PropertyControls
        {
            public Panel Panel;
            public Label Title;
            public Label SelectedElement;
            public Label DimensionsLabel;
            public NumericUpDown Width;
            public NumericUpDown Height;
            public Label ContentLabel;
            public TextBox Content;
            public Label ExpressionLabel;
            public TextBox Expression;
            public Button InsertExpression;
            public Button ExpressionEditor;
            public Label BarcodeTypeLabel;
            public ComboBox BarcodeType;
            public Label BarcodeNumberVisibilityLabel;
            public CheckBox BarcodeNumberVisibility;
            public Label GroupedNumberLabel;
            public CheckBox GroupedNumber;
            public Label LinearRenderingLabel;
            public CheckBox LinearRendering;
            public Label AlignmentLabel;
            public List<Button> AlignmentButtons;
            public Label FontFamilyLabel;
            public ComboBox FontFamily;
            public Label FontSizeLabel;
            public NumericUpDown FontSize;
            public Label StyleLabel;
            public List<CheckBox> StyleChecks;
            public Label TextColorLabel;
            public Button TextColor;
            public Label TextShortcutsLabel;
            public List<Button> TextShortcuts;
            public Label BackgroundColorLabel;
            public Button BackgroundColor;
            public Label BackgroundShortcutsLabel;
            public List<Button> BackgroundShortcuts;
            public Label BorderLabel;
            public ComboBox Border;
            public Label BorderWidthLabel;
            public NumericUpDown BorderWidth;
            public List<Panel> Separators;
            public bool Applying;
        }

        private static readonly Dictionary<Panel, PropertyControls> ConfiguredPanels =
            new Dictionary<Panel, PropertyControls>();

        public static void Configure(Panel panel)
        {
            if (panel == null)
                return;

            PropertyControls controls;
            if (!ConfiguredPanels.TryGetValue(panel, out controls))
            {
                controls = DiscoverControls(panel);
                ConfiguredPanels.Add(panel, controls);
                ApplyTheme(controls);

                panel.SizeChanged += (sender, args) => Apply(panel);
                panel.VisibleChanged += (sender, args) => Apply(panel);
                panel.Layout += (sender, args) => Apply(panel);
                panel.Disposed += (sender, args) => ConfiguredPanels.Remove(panel);
            }

            Apply(panel);
        }

        public static void Apply(Panel panel)
        {
            PropertyControls controls;
            if (panel == null ||
                !ConfiguredPanels.TryGetValue(panel, out controls) ||
                controls.Applying ||
                panel.IsDisposed)
            {
                return;
            }

            controls.Applying = true;
            try
            {
                LayoutControls(controls);
            }
            finally
            {
                controls.Applying = false;
            }
        }

        private static PropertyControls DiscoverControls(Panel panel)
        {
            List<Label> labels = panel.Controls.OfType<Label>().ToList();
            List<Button> buttons = panel.Controls.OfType<Button>().ToList();
            List<NumericUpDown> numericControls = panel.Controls.OfType<NumericUpDown>()
                .OrderBy(control => control.Top)
                .ThenBy(control => control.Left)
                .ToList();
            List<ComboBox> comboBoxes = panel.Controls.OfType<ComboBox>()
                .OrderBy(control => control.Top)
                .ToList();
            List<TextBox> textBoxes = panel.Controls.OfType<TextBox>()
                .OrderBy(control => control.Top)
                .ToList();
            List<Label> shortcutLabels = labels
                .Where(label => string.Equals(label.Text, "Atalhos:", StringComparison.Ordinal))
                .OrderBy(label => label.Top)
                .ToList();

            return new PropertyControls
            {
                Panel = panel,
                Title = FindLabel(labels, "⚙ PROPRIEDADES"),
                SelectedElement = labels.FirstOrDefault(label => label.Name == "lblNomeElementoAtual"),
                DimensionsLabel = FindLabel(labels, "Dimensoes (mm):"),
                Width = numericControls.ElementAtOrDefault(0),
                Height = numericControls.ElementAtOrDefault(1),
                ContentLabel = labels.FirstOrDefault(label => label.Name == "lblConteudoTexto"),
                Content = textBoxes.FirstOrDefault(textBox => textBox.Name == "txtConteudoElemento"),
                ExpressionLabel = FindLabel(labels, "Expressão:"),
                Expression = textBoxes.FirstOrDefault(textBox => textBox.Name != "txtConteudoElemento"),
                InsertExpression = FindButton(buttons, "Inserir Campo"),
                ExpressionEditor = FindButton(buttons, "..."),
                BarcodeTypeLabel = labels.FirstOrDefault(label => label.Name == "lblSimbologiaCodigoBarras"),
                BarcodeType = comboBoxes.FirstOrDefault(combo => combo.Name == "cmbSimbologiaCodigoBarras"),
                BarcodeNumberVisibilityLabel = labels.FirstOrDefault(
                    label => label.Name == "lblExibirNumeracaoCodigoBarras"),
                BarcodeNumberVisibility = panel.Controls.OfType<CheckBox>()
                    .FirstOrDefault(check => check.Name == "chkExibirNumeracaoCodigoBarras"),
                GroupedNumberLabel = labels.FirstOrDefault(label => label.Name == "lblNumeracaoAgrupada"),
                GroupedNumber = panel.Controls.OfType<CheckBox>()
                    .FirstOrDefault(check => check.Name == "chkNumeracaoAgrupada"),
                LinearRenderingLabel = labels.FirstOrDefault(label => label.Name == "lblRenderizacaoLinear1D"),
                LinearRendering = panel.Controls.OfType<CheckBox>()
                    .FirstOrDefault(check => check.Name == "chkRenderizacaoLinear1D"),
                AlignmentLabel = FindLabel(labels, "Alinhamento:"),
                AlignmentButtons = buttons
                    .Where(button => button.Text == "←" || button.Text == "←→" || button.Text == "→")
                    .OrderBy(button => button.Left)
                    .ToList(),
                FontFamilyLabel = FindLabel(labels, "Família da Fonte:"),
                FontFamily = comboBoxes.FirstOrDefault(combo => combo.Name == "cmbFonte"),
                FontSizeLabel = FindLabel(labels, "Tamanho da Fonte:"),
                FontSize = numericControls.ElementAtOrDefault(2),
                StyleLabel = FindLabel(labels, "Estilo:"),
                StyleChecks = panel.Controls.OfType<CheckBox>()
                    .Where(check => check.Name != "chkNumeracaoAgrupada" &&
                                    check.Name != "chkRenderizacaoLinear1D" &&
                                    check.Name != "chkExibirNumeracaoCodigoBarras")
                    .OrderBy(check => check.Left)
                    .ToList(),
                TextColorLabel = FindLabel(labels, "Cor do Texto:"),
                TextColor = FindButton(buttons, "Escolher Cor"),
                TextShortcutsLabel = shortcutLabels.ElementAtOrDefault(0),
                TextShortcuts = buttons
                    .Where(button => button.Text == "T▓")
                    .OrderBy(button => button.Left)
                    .ToList(),
                BackgroundColorLabel = FindLabel(labels, "Cor de Fundo:"),
                BackgroundColor = FindButton(buttons, "Escolher Fundo"),
                BackgroundShortcutsLabel = shortcutLabels.ElementAtOrDefault(1),
                BackgroundShortcuts = buttons
                    .Where(button => string.IsNullOrEmpty(button.Text) || button.Text == "Ø")
                    .OrderBy(button => button.Left)
                    .ToList(),
                BorderLabel = FindLabel(labels, "Borda:"),
                Border = comboBoxes.FirstOrDefault(combo => combo.Name == "cmbBordaElemento"),
                BorderWidthLabel = FindLabel(labels, "Espessura da Borda:"),
                BorderWidth = numericControls.ElementAtOrDefault(3),
                Separators = panel.Controls.OfType<Panel>().ToList()
            };
        }

        private static void ApplyTheme(PropertyControls controls)
        {
            controls.Panel.BackColor = ThemeManager.PanelBackground;
            controls.Panel.Padding = Padding.Empty;
            controls.Panel.AutoScroll = true;

            foreach (Label label in controls.Panel.Controls.OfType<Label>())
            {
                label.Font = ThemeManager.SectionFont;
                label.ForeColor = ThemeManager.TextSecondary;
                label.TextAlign = ContentAlignment.MiddleLeft;
                label.AutoEllipsis = true;
            }

            if (controls.Title != null)
            {
                controls.Title.ForeColor = ThemeManager.TextPrimary;
                controls.Title.TextAlign = ContentAlignment.MiddleCenter;
            }

            if (controls.SelectedElement != null)
            {
                controls.SelectedElement.Font = ThemeManager.ButtonFont;
                controls.SelectedElement.ForeColor = ThemeManager.SmartPrintOrangeDark;
                controls.SelectedElement.BackColor = ThemeManager.HoverBackground;
            }

            foreach (TextBox textBox in controls.Panel.Controls.OfType<TextBox>())
                ThemeManager.StyleInput(textBox);

            foreach (ComboBox comboBox in controls.Panel.Controls.OfType<ComboBox>())
                ThemeManager.StyleInput(comboBox);

            foreach (NumericUpDown numeric in controls.Panel.Controls.OfType<NumericUpDown>())
            {
                ThemeManager.StyleInput(numeric);
                numeric.TextAlign = HorizontalAlignment.Right;
            }

            foreach (CheckBox checkBox in controls.Panel.Controls.OfType<CheckBox>())
                checkBox.ForeColor = ThemeManager.TextPrimary;

            IEnumerable<Button> layoutButtons = controls.AlignmentButtons
                .Concat(new[] { controls.InsertExpression, controls.ExpressionEditor })
                .Where(button => button != null);

            foreach (Button button in layoutButtons)
            {
                button.FlatStyle = FlatStyle.Flat;
                button.FlatAppearance.BorderSize = 1;
                button.FlatAppearance.BorderColor = ThemeManager.Border;
                button.BackColor = ThemeManager.StatusBackground;
                button.ForeColor = ThemeManager.TextPrimary;
                button.Cursor = Cursors.Hand;
            }

            foreach (Panel separator in controls.Separators)
                separator.BackColor = ThemeManager.Border;
        }

        private static void LayoutControls(PropertyControls controls)
        {
            Panel panel = controls.Panel;
            int availableRight = Math.Max(
                OuterPadding + LabelColumnWidth + 100,
                panel.ClientSize.Width - OuterPadding - SystemInformation.VerticalScrollBarWidth);
            int fieldLeft = OuterPadding + LabelColumnWidth;
            int fieldWidth = Math.Max(96, availableRight - fieldLeft);
            int y = OuterPadding;
            int separatorIndex = 0;

            panel.SuspendLayout();
            try
            {
                PlaceFullWidth(controls.Title, OuterPadding, y, availableRight - OuterPadding, 26);
                y += 32;

                if (IsVisible(controls.SelectedElement))
                {
                    PlaceFullWidth(
                        controls.SelectedElement,
                        OuterPadding,
                        y,
                        availableRight - OuterPadding,
                        34);
                    y += 40;
                }

                PlaceSeparator(controls, ref separatorIndex, y, availableRight);
                y += 9;

                y = PlaceDimensionsRow(
                    controls.DimensionsLabel,
                    controls.Width,
                    controls.Height,
                    y,
                    fieldLeft,
                    fieldWidth);
                y = PlaceStandardRow(
                    controls.ContentLabel,
                    controls.Content,
                    y,
                    fieldLeft,
                    fieldWidth,
                    true);
                y = PlaceStandardRow(
                    controls.ExpressionLabel,
                    controls.Expression,
                    y,
                    fieldLeft,
                    fieldWidth,
                    true);
                y = PlaceExpressionButtons(
                    controls.InsertExpression,
                    controls.ExpressionEditor,
                    y,
                    fieldLeft,
                    fieldWidth);
                y = PlaceStandardRow(
                    controls.BarcodeTypeLabel,
                    controls.BarcodeType,
                    y,
                    fieldLeft,
                    fieldWidth,
                    true);
                y = PlaceCheckBoxRow(
                    controls.BarcodeNumberVisibilityLabel,
                    new[] { controls.BarcodeNumberVisibility },
                    y,
                    fieldLeft,
                    fieldWidth);
                y = PlaceCheckBoxRow(
                    controls.GroupedNumberLabel,
                    new[] { controls.GroupedNumber },
                    y,
                    fieldLeft,
                    fieldWidth);
                y = PlaceCheckBoxRow(
                    controls.LinearRenderingLabel,
                    new[] { controls.LinearRendering },
                    y,
                    fieldLeft,
                    fieldWidth);

                PlaceSeparator(controls, ref separatorIndex, y, availableRight);
                y += 9;

                y = PlaceButtonRow(
                    controls.AlignmentLabel,
                    controls.AlignmentButtons,
                    y,
                    fieldLeft,
                    fieldWidth,
                    3);
                y = PlaceStandardRow(
                    controls.FontFamilyLabel,
                    controls.FontFamily,
                    y,
                    fieldLeft,
                    fieldWidth,
                    true);
                y = PlaceStandardRow(
                    controls.FontSizeLabel,
                    controls.FontSize,
                    y,
                    fieldLeft,
                    96,
                    false);
                y = PlaceCheckBoxRow(
                    controls.StyleLabel,
                    controls.StyleChecks,
                    y,
                    fieldLeft,
                    fieldWidth);

                PlaceSeparator(controls, ref separatorIndex, y, availableRight);
                y += 9;

                y = PlaceStandardRow(
                    controls.TextColorLabel,
                    controls.TextColor,
                    y,
                    fieldLeft,
                    fieldWidth,
                    true);
                y = PlaceButtonRow(
                    controls.TextShortcutsLabel,
                    controls.TextShortcuts,
                    y,
                    fieldLeft,
                    fieldWidth,
                    2);
                y = PlaceStandardRow(
                    controls.BackgroundColorLabel,
                    controls.BackgroundColor,
                    y,
                    fieldLeft,
                    fieldWidth,
                    true);
                y = PlaceButtonRow(
                    controls.BackgroundShortcutsLabel,
                    controls.BackgroundShortcuts,
                    y,
                    fieldLeft,
                    fieldWidth,
                    3);

                PlaceSeparator(controls, ref separatorIndex, y, availableRight);
                y += 9;

                y = PlaceStandardRow(
                    controls.BorderLabel,
                    controls.Border,
                    y,
                    fieldLeft,
                    fieldWidth,
                    true);
                y = PlaceStandardRow(
                    controls.BorderWidthLabel,
                    controls.BorderWidth,
                    y,
                    fieldLeft,
                    96,
                    false);

                for (int index = separatorIndex; index < controls.Separators.Count; index++)
                    controls.Separators[index].Visible = false;

                panel.AutoScrollMinSize = new Size(0, y + OuterPadding);
                panel.HorizontalScroll.Enabled = false;
                panel.HorizontalScroll.Visible = false;
            }
            finally
            {
                panel.ResumeLayout(false);
            }
        }

        private static int PlaceDimensionsRow(
            Label label,
            NumericUpDown width,
            NumericUpDown height,
            int y,
            int fieldLeft,
            int fieldWidth)
        {
            if (!AnyVisible(label, width, height))
                return y;

            PlaceLabel(label, y, fieldLeft);
            int gap = 8;
            int numericWidth = Math.Max(44, (fieldWidth - gap) / 2);
            PlaceEditor(width, fieldLeft, y + 3, numericWidth, false);
            PlaceEditor(height, fieldLeft + numericWidth + gap, y + 3, numericWidth, false);
            return y + RowHeight + RowGap;
        }

        private static int PlaceStandardRow(
            Label label,
            Control editor,
            int y,
            int fieldLeft,
            int width,
            bool expand)
        {
            if (!AnyVisible(label, editor))
                return y;

            PlaceLabel(label, y, fieldLeft);
            PlaceEditor(editor, fieldLeft, y + 3, width, expand);
            return y + RowHeight + RowGap;
        }

        private static int PlaceExpressionButtons(
            Button insertButton,
            Button editorButton,
            int y,
            int fieldLeft,
            int fieldWidth)
        {
            if (!AnyVisible(insertButton, editorButton))
                return y;

            int editorWidth = 40;
            int gap = 8;
            int insertWidth = Math.Max(60, fieldWidth - editorWidth - gap);
            PlaceButton(insertButton, fieldLeft, y, insertWidth, 28);
            PlaceButton(editorButton, fieldLeft + insertWidth + gap, y, editorWidth, 28);
            return y + 28 + RowGap;
        }

        private static int PlaceButtonRow(
            Label label,
            IList<Button> buttons,
            int y,
            int fieldLeft,
            int fieldWidth,
            int expectedColumns)
        {
            if (!IsVisible(label) && !buttons.Any(IsVisible))
                return y;

            PlaceLabel(label, y, fieldLeft);
            List<Button> visibleButtons = buttons.Where(IsVisible).ToList();
            int columns = Math.Max(1, Math.Min(expectedColumns, visibleButtons.Count));
            int gap = 6;
            int buttonWidth = Math.Max(30, (fieldWidth - gap * (columns - 1)) / columns);

            for (int index = 0; index < visibleButtons.Count; index++)
                PlaceButton(visibleButtons[index], fieldLeft + index * (buttonWidth + gap), y + 2, buttonWidth, 28);

            return y + RowHeight + RowGap;
        }

        private static int PlaceCheckBoxRow(
            Label label,
            IList<CheckBox> checkBoxes,
            int y,
            int fieldLeft,
            int fieldWidth)
        {
            if (!IsVisible(label) && !checkBoxes.Any(IsVisible))
                return y;

            PlaceLabel(label, y, fieldLeft);
            List<CheckBox> visibleChecks = checkBoxes.Where(IsVisible).ToList();
            int checkWidth = Math.Max(72, fieldWidth / Math.Max(1, visibleChecks.Count));

            for (int index = 0; index < visibleChecks.Count; index++)
            {
                CheckBox checkBox = visibleChecks[index];
                checkBox.Location = new Point(fieldLeft + index * checkWidth, y + 6);
                checkBox.Size = new Size(checkWidth, 22);
                checkBox.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            }

            return y + RowHeight + RowGap;
        }

        private static void PlaceLabel(Label label, int y, int fieldLeft)
        {
            if (label == null)
                return;

            label.Location = new Point(OuterPadding, y);
            label.Size = new Size(Math.Max(40, fieldLeft - OuterPadding - 8), RowHeight);
            label.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        }

        private static void PlaceEditor(
            Control control,
            int x,
            int y,
            int width,
            bool expand)
        {
            if (control == null)
                return;

            int height = control is Button ? 28 : 24;
            control.Location = new Point(x, y);
            control.Size = new Size(Math.Max(40, width), height);
            control.Anchor = expand
                ? AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                : AnchorStyles.Top | AnchorStyles.Left;
        }

        private static void PlaceButton(Button button, int x, int y, int width, int height)
        {
            if (button == null)
                return;

            button.Location = new Point(x, y);
            button.Size = new Size(width, height);
            button.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            button.TextAlign = ContentAlignment.MiddleCenter;
        }

        private static void PlaceFullWidth(Control control, int x, int y, int width, int height)
        {
            if (control == null)
                return;

            control.Location = new Point(x, y);
            control.Size = new Size(Math.Max(80, width), height);
            control.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        }

        private static void PlaceSeparator(
            PropertyControls controls,
            ref int separatorIndex,
            int y,
            int availableRight)
        {
            if (separatorIndex >= controls.Separators.Count)
                return;

            Panel separator = controls.Separators[separatorIndex++];
            separator.Visible = true;
            separator.Location = new Point(OuterPadding, y);
            separator.Size = new Size(Math.Max(80, availableRight - OuterPadding), 1);
            separator.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        }

        private static Label FindLabel(IEnumerable<Label> labels, string text)
        {
            return labels.FirstOrDefault(label => string.Equals(label.Text, text, StringComparison.Ordinal));
        }

        private static Button FindButton(IEnumerable<Button> buttons, string text)
        {
            return buttons.FirstOrDefault(button => string.Equals(button.Text, text, StringComparison.Ordinal));
        }

        private static bool AnyVisible(params Control[] controls)
        {
            return controls.Any(IsVisible);
        }

        private static bool IsVisible(Control control)
        {
            return control != null && control.Visible;
        }
    }
}
