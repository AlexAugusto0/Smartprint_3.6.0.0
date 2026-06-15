using EtiquetaFORNew.Forms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace EtiquetaFORNew
{
    public partial class FormListaTemplates : Form
    {
        public string TemplateSelecionado { get; private set; }
        private ListBox lstTemplates;
        private CheckBox chkDefinirPadrao;
        private bool atualizandoCheckbox = false; // Flag para evitar recursão

        public FormListaTemplates()
        {
            InitializeComponent();
            CarregarLista();
        }

        private void InitializeComponent()
        {
            this.Text = "Carregar Template";
            this.Size = new Size(500, 450);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            Label lblInstrucao = new Label
            {
                Text = "Selecione um template para carregar:",
                Location = new Point(20, 10),
                Size = new Size(300, 20),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            Label lblInstrucao2 = new Label
            {
                Text = "*Dê dois cliques para carregar:",
                Location = new Point(25, 30),
                Size = new Size(200, 20),
                Font = new Font("Segoe UI", 7, FontStyle.Bold)
            };

            lstTemplates = new ListBox
            {
                Name = "lstTemplates",
                Location = new Point(30, 50),
                Size = new Size(440, 230),
                Font = new Font("Segoe UI", 10),
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 25
            };
            lstTemplates.DoubleClick += (s, e) => CarregarSelecionado();
            lstTemplates.DrawItem += LstTemplates_DrawItem;
            lstTemplates.SelectedIndexChanged += LstTemplates_SelectedIndexChanged;

            // Checkbox para definir como padrão
            chkDefinirPadrao = new CheckBox
            {
                Text = "⭐ Definir como template padrão",
                Location = new Point(20, 290),
                Size = new Size(250, 25),
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(230, 126, 34)
            };
            chkDefinirPadrao.CheckedChanged += ChkDefinirPadrao_CheckedChanged;

            Label lblInfo = new Label
            {
                Text = $"📁 Local: {TemplateManager.ObterPastaTemplates()}",
                Location = new Point(20, 325),
                Size = new Size(440, 20),
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.Gray
            };

            Button btnNovo = new Button
            {
                Text = "Nova Etiqueta",
                Location = new Point(20, 355),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(255, 143, 0),
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat
            };
            btnNovo.FlatAppearance.BorderSize = 0;
            btnNovo.Click += (s, e) => NovaEtiqueta();

            Button btnExcluir = new Button
            {
                Text = "Excluir",
                Location = new Point(130, 355),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(231, 76, 60),
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat
            };
            btnExcluir.FlatAppearance.BorderSize = 0;
            btnExcluir.Click += BtnExcluir_Click;

            Button btnCarregar = new Button
            {
                Text = "Carregar",
                //Location = new Point(260, 355),
                Location = new Point(370, 355),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(161, 179, 179),
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.OK
            };
            btnCarregar.FlatAppearance.BorderSize = 0;
            btnCarregar.Click += (s, e) => CarregarSelecionado();

            //Button btnCancelar = new Button
            //{
            //    Text = "Cancelar",
            //    Location = new Point(370, 355),
            //    Size = new Size(90, 30),
            //    BackColor = Color.FromArgb(149, 165, 166),
            //    ForeColor = Color.Black,
            //    FlatStyle = FlatStyle.Flat,
            //    DialogResult = DialogResult.Cancel
            //};
            //btnCancelar.FlatAppearance.BorderSize = 0;

            Button btnTemplateApi = new Button
            {
                Text = "Nuvem API",
                Location = new Point(370, 15),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(0, 162, 232),
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat                
            };
            btnTemplateApi.FlatAppearance.BorderSize = 0;
            btnTemplateApi.Click += (s, e) =>
            {
                using (var formNuvem = new FormTemplateApi())
                {
                    formNuvem.ShowDialog(this);
                }
            };

            this.Controls.AddRange(new Control[] {
                lblInstrucao, lblInstrucao2, lstTemplates, chkDefinirPadrao, lblInfo,
                btnNovo, btnExcluir, btnCarregar,// btnTemplateApi, // btnCancelar
            });
        }

        private void LstTemplates_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            e.DrawBackground();

            string item = lstTemplates.Items[e.Index].ToString();
            bool ehPadrao = TemplatePadraoManager.EhTemplatePadrao(item);

            // Cor de fundo
            Color corFundo = e.State.HasFlag(DrawItemState.Selected)
                ? Color.FromArgb(52, 152, 219)
                : e.BackColor;

            using (Brush brushFundo = new SolidBrush(corFundo))
            {
                e.Graphics.FillRectangle(brushFundo, e.Bounds);
            }

            // Ícone de estrela se for padrão
            string texto = item;
            if (ehPadrao)
            {
                texto = "⭐ " + item;
            }

            // Texto
            Color corTexto = e.State.HasFlag(DrawItemState.Selected)
                ? Color.White
                : (ehPadrao ? Color.FromArgb(230, 126, 34) : Color.Black);

            using (Brush brushTexto = new SolidBrush(corTexto))
            {
                e.Graphics.DrawString(
                    texto,
                    e.Font,
                    brushTexto,
                    e.Bounds.Left + 5,
                    e.Bounds.Top + 4
                );
            }

            e.DrawFocusRectangle();
        }

        private void LstTemplates_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstTemplates.SelectedItem == null || atualizandoCheckbox)
                return;

            // Atualiza checkbox baseado no template selecionado
            string templateSelecionado = lstTemplates.SelectedItem.ToString();

            // Usa flag para evitar recursão
            atualizandoCheckbox = true;
            chkDefinirPadrao.Checked = TemplatePadraoManager.EhTemplatePadrao(templateSelecionado);
            atualizandoCheckbox = false;
        }

        private void ChkDefinirPadrao_CheckedChanged(object sender, EventArgs e)
        {
            // Ignora mudanças enquanto está atualizando programaticamente
            if (atualizandoCheckbox)
                return;

            if (lstTemplates.SelectedItem == null ||
                lstTemplates.SelectedItem.ToString() == "(Nenhum template salvo)")
                return;

            string templateSelecionado = lstTemplates.SelectedItem.ToString();

            if (chkDefinirPadrao.Checked)
            {
                // Marca o template selecionado como padrão
                TemplatePadraoManager.DefinirTemplatePadrao(templateSelecionado);
            }
            else
            {
                // Remove a marcação de padrão (apenas se este template for o padrão atual)
                if (TemplatePadraoManager.EhTemplatePadrao(templateSelecionado))
                {
                    TemplatePadraoManager.RemoverTemplatePadrao();
                }
            }

            // Atualiza visual da lista
            lstTemplates.Invalidate();
        }

        private void CarregarLista()
        {
            lstTemplates.Items.Clear();
            var templates = TemplateManager.ListarTemplates();

            // Filtrar o template temporário
            templates = templates.Where(t => t != "_ultimo_template").ToList();

            if (templates.Count == 0)
            {
                lstTemplates.Items.Add("(Nenhum template salvo)");
                chkDefinirPadrao.Enabled = false;
                return;
            }

            chkDefinirPadrao.Enabled = true;

            // Adiciona templates, ordenando para colocar o padrão primeiro
            string templatePadrao = TemplatePadraoManager.ObterTemplatePadrao();

            var templatesOrdenados = templates.OrderBy(t =>
                t.Equals(templatePadrao, StringComparison.OrdinalIgnoreCase) ? 0 : 1
            ).ThenBy(t => t).ToList();

            foreach (var template in templatesOrdenados)
            {
                lstTemplates.Items.Add(template);
            }

            if (lstTemplates.Items.Count > 0)
            {
                lstTemplates.SelectedIndex = 0;
            }
        }

        private void CarregarSelecionado()
        {
            if (lstTemplates.SelectedItem == null ||
                lstTemplates.SelectedItem.ToString() == "(Nenhum template salvo)")
            {
                MessageBox.Show("Selecione um template!", "Atenção",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
                return;
            }

            TemplateSelecionado = lstTemplates.SelectedItem.ToString();
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void NovaEtiqueta()
        {
            try
            {
                FormPrincipal Entrada = new FormPrincipal();
                TemplateEtiqueta templateParaAbrir = null;
                string nomeTemplate = null;

                // Pergunta nome do novo template
                using (var formNome = new FormNomeTemplate())
                {
                    if (formNome.ShowDialog() == DialogResult.OK)
                    {
                        nomeTemplate = formNome.NomeTemplate;
                        templateParaAbrir = new TemplateEtiqueta
                        {
                            Largura = 100,
                            Altura = 30,
                            Elementos = new List<ElementoEtiqueta>()
                        };
                    }
                    else
                    {
                        return;
                    }
                }

                // Abre o Designer NOVO com template e nome
                if (templateParaAbrir != null && !string.IsNullOrEmpty(nomeTemplate))
                {
                    using (var formDesigner = new FormDesignNovo(templateParaAbrir, nomeTemplate))
                    {
                        if (formDesigner.ShowDialog() == DialogResult.OK)
                        {
                            MessageBox.Show(
                                $"Template '{nomeTemplate}' salvo com sucesso!",
                                "Sucesso",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);

                            // Atualiza lista de templates
                            CarregarLista();
                        }
                    }
                }
            }
            catch { }

            this.Close();
        }

        private void BtnExcluir_Click(object sender, EventArgs e)
        {
            if (lstTemplates.SelectedItem == null ||
                lstTemplates.SelectedItem.ToString() == "(Nenhum template salvo)")
            {
                MessageBox.Show("Selecione um template para excluir!", "Atenção",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string nomeTemplate = lstTemplates.SelectedItem.ToString();

            if (MessageBox.Show($"Deseja realmente excluir o template '{nomeTemplate}'?",
                "Confirmar Exclusão", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                // Se for o template padrão, remove a marcação
                if (TemplatePadraoManager.EhTemplatePadrao(nomeTemplate))
                {
                    TemplatePadraoManager.RemoverTemplatePadrao();
                }

                if (TemplateManager.ExcluirTemplate(nomeTemplate))
                {
                    MessageBox.Show("Template excluído com sucesso!", "Sucesso",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    CarregarLista();
                }
            }
        }
    }
}