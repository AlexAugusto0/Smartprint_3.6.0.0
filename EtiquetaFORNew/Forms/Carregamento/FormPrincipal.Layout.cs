using EtiquetaFORNew.UI;
using System.Drawing;
using System.Windows.Forms;

namespace EtiquetaFORNew
{
    public partial class FormPrincipal
    {
        private TableLayoutPanel responsiveRoot;
        private TableLayoutPanel searchFieldsLayout;

        private void InitializeResponsiveLayout()
        {
            SuspendLayout();
            try
            {
                ConfigureFormSurface();
                ConfigureHeader();
                ConfigureProductSearch();
                ConfigureListActions();
                ConfigureProductGrid();
                ConfigureFooter();
                ApplyVisualTheme();
            }
            finally
            {
                ResumeLayout(true);
            }
        }

        private void ConfigureFormSurface()
        {
            AutoScroll = false;
            MinimumSize = new Size(1024, 640);
            ClientSize = new Size(1180, 720);
            Padding = Padding.Empty;

            responsiveRoot = new TableLayoutPanel
            {
                Name = "responsiveRoot",
                Dock = DockStyle.Fill,
                BackColor = ThemeManager.WorkspaceBackground,
                Padding = new Padding(16),
                ColumnCount = 1,
                RowCount = 5,
                Margin = Padding.Empty
            };
            responsiveRoot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            responsiveRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 58F));
            responsiveRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 96F));
            responsiveRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
            responsiveRoot.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            responsiveRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            Controls.Add(responsiveRoot);
            responsiveRoot.BringToFront();
        }

        private void ConfigureHeader()
        {
            panelTop.Dock = DockStyle.Fill;
            panelTop.Margin = new Padding(0, 0, 0, 8);
            panelTop.Padding = Padding.Empty;
            panelTop.BorderStyle = BorderStyle.FixedSingle;
            responsiveRoot.Controls.Add(panelTop, 0, 0);

            var headerLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                BackColor = ThemeManager.ToolbarBackground,
                Padding = new Padding(10, 6, 8, 6),
                Margin = Padding.Empty
            };
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 58F));
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            pictureBox1.Dock = DockStyle.Fill;
            pictureBox1.Margin = new Padding(2, 0, 8, 0);
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            headerLayout.Controls.Add(pictureBox1, 0, 0);

            lblTitulo.AutoSize = false;
            lblTitulo.Dock = DockStyle.Fill;
            lblTitulo.Margin = Padding.Empty;
            lblTitulo.TextAlign = ContentAlignment.MiddleLeft;
            headerLayout.Controls.Add(lblTitulo, 1, 0);

            var headerActions = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Margin = Padding.Empty
            };
            ConfigureToolbarButton(btnConfig);
            ConfigureToolbarButton(btnSincronizar);
            ConfigureToolbarButton(btnCalibracao);
            btnDesigner.Width = 174;
            btnDesigner.Margin = new Padding(8, 2, 4, 2);
            ThemeManager.StyleActionButton(btnDesigner, true);
            btnDesigner.ForeColor = ThemeManager.TextPrimary;
            headerActions.Controls.Add(btnConfig);
            headerActions.Controls.Add(btnSincronizar);
            headerActions.Controls.Add(btnCalibracao);
            headerActions.Controls.Add(btnDesigner);
            headerLayout.Controls.Add(headerActions, 2, 0);
            panelTop.Controls.Add(headerLayout);
            headerLayout.BringToFront();
        }

        private void ConfigureToolbarButton(Button button)
        {
            button.Size = new Size(45, 34);
            button.Margin = new Padding(3, 2, 3, 2);
            button.Padding = Padding.Empty;
            button.FlatStyle = FlatStyle.Standard;
            button.BackColor = ThemeManager.ToolbarBackground;
            button.ForeColor = ThemeManager.TextPrimary;
            button.Cursor = Cursors.Hand;
        }

        private void ConfigureProductSearch()
        {
            panel2.Dock = DockStyle.Fill;
            panel2.Margin = new Padding(0, 0, 0, 8);
            panel2.Padding = Padding.Empty;
            panel2.BorderStyle = BorderStyle.FixedSingle;
            responsiveRoot.Controls.Add(panel2, 0, 1);

            groupProduto.Dock = DockStyle.Fill;
            groupProduto.Margin = Padding.Empty;
            groupProduto.Padding = new Padding(10, 6, 10, 10);
            panel2.Controls.Add(groupProduto);

            panel1.Dock = DockStyle.Fill;
            panel1.Margin = Padding.Empty;
            panel1.Padding = Padding.Empty;

            searchFieldsLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 7,
                RowCount = 2,
                Margin = Padding.Empty,
                Padding = new Padding(2, 2, 2, 0)
            };
            searchFieldsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90F));
            searchFieldsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 132F));
            searchFieldsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            searchFieldsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 0F));
            searchFieldsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 0F));
            searchFieldsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 64F));
            searchFieldsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 114F));
            searchFieldsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 23F));
            searchFieldsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            AddSearchField(label1, cmbBuscaCodigo, 0);
            AddSearchField(label3, cmbBuscaReferencia, 1);
            AddSearchField(label2, cmbBuscaNome, 2);
            AddSearchField(lblTamanho, cmbTamanho, 3);
            AddSearchField(lblCor, cmbCor, 4);
            AddSearchField(lblQtd, numQtd, 5);

            BtnAdicionar2.Dock = DockStyle.Fill;
            BtnAdicionar2.Margin = new Padding(6, 19, 0, 1);
            ThemeManager.StyleActionButton(BtnAdicionar2, true);
            BtnAdicionar2.ForeColor = ThemeManager.TextPrimary;
            searchFieldsLayout.Controls.Add(BtnAdicionar2, 6, 0);
            searchFieldsLayout.SetRowSpan(BtnAdicionar2, 2);
            panel1.Controls.Add(searchFieldsLayout);
            searchFieldsLayout.BringToFront();
        }

        private void AddSearchField(Label label, Control field, int column)
        {
            label.Dock = DockStyle.Fill;
            label.Margin = new Padding(3, 1, 3, 0);
            label.TextAlign = ContentAlignment.BottomLeft;
            field.Dock = DockStyle.Fill;
            field.Margin = new Padding(3, 1, 3, 2);
            searchFieldsLayout.Controls.Add(label, column, 0);
            searchFieldsLayout.Controls.Add(field, column, 1);
        }

        private void ConfigureListActions()
        {
            var actionLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = Padding.Empty,
                Padding = new Padding(2, 4, 0, 6)
            };
            actionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            actionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            chkSelecionarTodos.AutoSize = true;
            chkSelecionarTodos.Anchor = AnchorStyles.Left;
            chkSelecionarTodos.Margin = new Padding(4, 0, 0, 0);
            actionLayout.Controls.Add(chkSelecionarTodos, 0, 0);

            var listActions = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = Padding.Empty
            };
            btnCarregar.Size = new Size(122, 34);
            btnCarregar.Margin = new Padding(0, 0, 8, 0);
            ThemeManager.StyleActionButton(btnCarregar);
            btnLimparTodos.Size = new Size(134, 34);
            btnLimparTodos.Margin = Padding.Empty;
            ConfigureDangerButton(btnLimparTodos);
            listActions.Controls.Add(btnCarregar);
            listActions.Controls.Add(btnLimparTodos);
            actionLayout.Controls.Add(listActions, 1, 0);
            responsiveRoot.Controls.Add(actionLayout, 0, 2);
        }

        private void ConfigureProductGrid()
        {
            dgvProdutos.Dock = DockStyle.Fill;
            dgvProdutos.Margin = Padding.Empty;
            dgvProdutos.BorderStyle = BorderStyle.None;
            dgvProdutos.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            dgvProdutos.RowTemplate.Height = 28;
            dgvProdutos.ColumnHeadersHeight = 34;
            dgvProdutos.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            colNome.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            colNome.MinimumWidth = 180;
            responsiveRoot.Controls.Add(dgvProdutos, 0, 3);
        }

        private void ConfigureFooter()
        {
            var footerLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = Padding.Empty,
                Padding = new Padding(0, 10, 0, 0)
            };
            footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            BtnFaq.Size = new Size(36, 34);
            BtnFaq.Anchor = AnchorStyles.Left;
            BtnFaq.Margin = Padding.Empty;
            BtnFaq.FlatStyle = FlatStyle.Flat;
            BtnFaq.FlatAppearance.BorderSize = 1;
            BtnFaq.FlatAppearance.BorderColor = ThemeManager.Border;
            BtnFaq.FlatAppearance.MouseOverBackColor = ThemeManager.HoverBackground;
            footerLayout.Controls.Add(BtnFaq, 0, 0);

            btnImprimir.Size = new Size(168, 34);
            btnImprimir.Anchor = AnchorStyles.Right;
            btnImprimir.Margin = Padding.Empty;
            ThemeManager.StylePrimaryActionButton(btnImprimir);
            footerLayout.Controls.Add(btnImprimir, 1, 0);
            responsiveRoot.Controls.Add(footerLayout, 0, 4);
        }

        private void ConfigureDangerButton(Button button)
        {
            button.Height = 34;
            button.Padding = new Padding(8, 0, 8, 0);
            button.Font = ThemeManager.ButtonFont;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = ThemeManager.Danger;
            button.FlatAppearance.MouseOverBackColor = ThemeManager.HoverBackground;
            button.BackColor = ThemeManager.PanelBackground;
            button.ForeColor = ThemeManager.Danger;
            button.Cursor = Cursors.Hand;
            button.TextAlign = ContentAlignment.MiddleCenter;
        }

        private void ApplyVisualTheme()
        {
            BackColor = ThemeManager.WorkspaceBackground;
            panelTop.BackColor = ThemeManager.ToolbarBackground;
            panel2.BackColor = ThemeManager.PanelBackground;
            groupProduto.BackColor = ThemeManager.PanelBackground;
            groupProduto.ForeColor = ThemeManager.TextPrimary;
            groupProduto.Font = ThemeManager.SectionFont;
            panel1.BackColor = ThemeManager.PanelBackground;
            searchFieldsLayout.BackColor = ThemeManager.PanelBackground;

            lblTitulo.Font = ThemeManager.HeaderFont;
            lblTitulo.ForeColor = ThemeManager.TextPrimary;
            chkSelecionarTodos.Font = ThemeManager.ButtonFont;
            chkSelecionarTodos.ForeColor = ThemeManager.TextPrimary;
            BtnFaq.BackColor = ThemeManager.PanelBackground;
            BtnFaq.ForeColor = ThemeManager.TextPrimary;

            foreach (Control control in new Control[]
            {
                cmbBuscaCodigo, cmbBuscaReferencia, cmbBuscaNome,
                cmbTamanho, cmbCor, numQtd
            })
            {
                ThemeManager.StyleInput(control);
            }

            dgvProdutos.BackgroundColor = ThemeManager.PanelBackground;
            dgvProdutos.GridColor = ThemeManager.Border;
            dgvProdutos.EnableHeadersVisualStyles = false;
            dgvProdutos.ColumnHeadersDefaultCellStyle.BackColor = ThemeManager.HeaderBackground;
            dgvProdutos.ColumnHeadersDefaultCellStyle.ForeColor = ThemeManager.HeaderText;
            dgvProdutos.ColumnHeadersDefaultCellStyle.Font = ThemeManager.SectionFont;
            dgvProdutos.ColumnHeadersDefaultCellStyle.SelectionBackColor = ThemeManager.HeaderBackground;
            dgvProdutos.ColumnHeadersDefaultCellStyle.SelectionForeColor = ThemeManager.HeaderText;
            dgvProdutos.DefaultCellStyle.BackColor = ThemeManager.PanelBackground;
            dgvProdutos.DefaultCellStyle.ForeColor = ThemeManager.TextPrimary;
            dgvProdutos.DefaultCellStyle.SelectionBackColor = ThemeManager.SmartPrintOrange;
            dgvProdutos.DefaultCellStyle.SelectionForeColor = ThemeManager.HeaderText;
            dgvProdutos.AlternatingRowsDefaultCellStyle.BackColor = ThemeManager.StatusBackground;
        }

        private void UpdateResponsiveLayoutForModule()
        {
            if (searchFieldsLayout == null)
                return;

            searchFieldsLayout.SuspendLayout();
            try
            {
                searchFieldsLayout.ColumnStyles[3].Width = isConfeccao ? 92F : 0F;
                searchFieldsLayout.ColumnStyles[4].Width = isConfeccao ? 108F : 0F;
            }
            finally
            {
                searchFieldsLayout.ResumeLayout(true);
            }
        }
    }
}
