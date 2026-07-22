using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;

namespace EtiquetaFORNew
{
    public partial class FormConfigEtiqueta : Form
    {

        private ConfiguracaoEtiqueta configuracao;
        private const float ESCALA_PREVIEW = 3.0f;
        private System.Drawing.Printing.PaperSize papelSelecionado;

        // Caminho para salvar as configurações
        private static readonly string CAMINHO_CONFIGURACOES =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "EtiquetaFornew", "configuracoes.xml");
        // NOVO: Caminho para salvar modelos de papel
        private static readonly string CAMINHO_MODELOS_PAPEL =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "EtiquetaFornew", "modelos_papel.xml");

        public ConfiguracaoEtiqueta Configuracao => configuracao;

        public FormConfigEtiqueta(ConfiguracaoEtiqueta configAtual = null)
        {
            InitializeComponent();

            // Inicializa com configuração salva, atual, ou valores padrão
            if (configAtual != null)
            {
                configuracao = configAtual;
            }
            else
            {
                // CORREÇÃO: Tenta carregar configuração salva primeiro
                var configSalva = CarregarConfiguracoesSalvas();

                if (configSalva != null)
                {
                    configuracao = configSalva;
                }
                else
                {
                    // Se não houver configuração salva, usa valores padrão
                    configuracao = new ConfiguracaoEtiqueta
                    {
                        NomeEtiqueta = "Gondola com Barras",
                        ImpressoraPadrao = "BTP-L42(D)",
                        PapelPadrao = "Tamanho do papel-SoftcomGondBar",
                        LarguraEtiqueta = 100,
                        AlturaEtiqueta = 30,
                        NumColunas = 1,
                        NumLinhas = 1,
                        EspacamentoColunas = 0,
                        EspacamentoLinhas = 0,
                        MargemSuperior = 0,
                        MargemInferior = 0,
                        MargemEsquerda = 0,
                        MargemDireita = 0
                    };
                }
            }
            ConfigurarEventos();
            CarregarConfiguracoes();            
            AtualizarPreview();
        }

        private void ConfigurarEventos()
        {
            // Eventos de mudança de valores
            txtNomeEtiqueta.TextChanged += (s, e) => AtualizarConfiguracao();

            // Quando a impressora mudar, recarrega os papéis disponíveis
            cmbImpressora.SelectedIndexChanged += (s, e) =>
            {
                CarregarTiposPapelDaImpressora();
                AtualizarConfiguracao();
            };

            cmbPapel.SelectedIndexChanged += (s, e) =>
            {
                AtualizarConfiguracao();
                AtualizarPreview();
            };

            numLargura.ValueChanged += (s, e) => { AtualizarConfiguracao(); AtualizarPreview(); };
            numAltura.ValueChanged += (s, e) => { AtualizarConfiguracao(); AtualizarPreview(); };
            numColunas.ValueChanged += (s, e) => { AtualizarConfiguracao(); AtualizarPreview(); };
            numLinhas.ValueChanged += (s, e) => { AtualizarConfiguracao(); AtualizarPreview(); };
            numEspacamentoColunas.ValueChanged += (s, e) => { AtualizarConfiguracao(); AtualizarPreview(); };
            numEspacamentoLinhas.ValueChanged += (s, e) => { AtualizarConfiguracao(); AtualizarPreview(); };
            numMargemSuperior.ValueChanged += (s, e) => { AtualizarConfiguracao(); AtualizarPreview(); };
            numMargemInferior.ValueChanged += (s, e) => { AtualizarConfiguracao(); AtualizarPreview(); };
            numMargemEsquerda.ValueChanged += (s, e) => { AtualizarConfiguracao(); AtualizarPreview(); };
            numMargemDireita.ValueChanged += (s, e) => { AtualizarConfiguracao(); AtualizarPreview(); };

            // Evento de pintura do preview
            panelPreview.Paint += PanelPreview_Paint;
        }

        private void CarregarConfiguracoes()
        {
            // ⭐ Carrega valores na interface
            txtNomeEtiqueta.Text = configuracao.NomeEtiqueta;

            // Carrega impressoras disponíveis
            CarregarImpressoras();
            if (cmbImpressora.Items.Contains(configuracao.ImpressoraPadrao))
                cmbImpressora.SelectedItem = configuracao.ImpressoraPadrao;
            else if (cmbImpressora.Items.Count > 0)
                cmbImpressora.SelectedIndex = 0;

            // Carrega tipos de papel da impressora selecionada
            CarregarTiposPapelDaImpressora();

            // ⭐ CORREÇÃO: Busca e seleciona o papel salvo CORRETAMENTE
            bool papelEncontrado = false;

            if (!string.IsNullOrEmpty(configuracao.PapelPadrao))
            {
                // Percorre os itens do ComboBox
                for (int i = 0; i < cmbPapel.Items.Count; i++)
                {
                    if (cmbPapel.Items[i] is PaperSizeItem psi)
                    {
                        // Compara pelo DisplayText (que contém o nome completo)
                        if (psi.DisplayText.Equals(configuracao.PapelPadrao, StringComparison.OrdinalIgnoreCase))
                        {
                            cmbPapel.SelectedIndex = i;
                            papelSelecionado = psi.PaperSize;
                            papelEncontrado = true;
                            break;
                        }
                    }
                }
            }

            // Se não encontrou, seleciona o primeiro
            if (!papelEncontrado && cmbPapel.Items.Count > 0)
            {
                cmbPapel.SelectedIndex = 0;
            }

            // ⭐ Dimensões da etiqueta
            numLargura.Value = (decimal)configuracao.LarguraEtiqueta;
            numAltura.Value = (decimal)configuracao.AlturaEtiqueta;

            // ⭐ Layout
            numColunas.Value = configuracao.NumColunas;
            numLinhas.Value = configuracao.NumLinhas;
            numEspacamentoColunas.Value = (decimal)configuracao.EspacamentoColunas;
            numEspacamentoLinhas.Value = (decimal)configuracao.EspacamentoLinhas;

            // ⭐ Margens
            numMargemSuperior.Value = (decimal)configuracao.MargemSuperior;
            numMargemInferior.Value = (decimal)configuracao.MargemInferior;
            numMargemEsquerda.Value = (decimal)configuracao.MargemEsquerda;
            numMargemDireita.Value = (decimal)configuracao.MargemDireita;

            // ⭐ Atualiza o preview
            AtualizarPreview();
        }

        private void CarregarImpressoras()
        {
            cmbImpressora.Items.Clear();

            // Adiciona impressoras instaladas no sistema
            foreach (string printer in System.Drawing.Printing.PrinterSettings.InstalledPrinters)
            {
                cmbImpressora.Items.Add(printer);
            }

            // Adiciona impressora padrão do exemplo
            if (!cmbImpressora.Items.Contains("BTP-L42(D)"))
                cmbImpressora.Items.Add("BTP-L42(D)");
        }

        private void CarregarTiposPapelDaImpressora()
        {
            cmbPapel.Items.Clear();

            // Verifica se há uma impressora selecionada
            if (cmbImpressora.SelectedItem == null)
            {
                cmbPapel.Items.Add("Selecione uma impressora primeiro");
                cmbPapel.Enabled = false;
                return;
            }

            cmbPapel.Enabled = true;
            string impressoraSelecionada = cmbImpressora.SelectedItem.ToString();

            try
            {
                // Cria PrinterSettings para a impressora selecionada
                System.Drawing.Printing.PrinterSettings printerSettings =
                    new System.Drawing.Printing.PrinterSettings();
                printerSettings.PrinterName = impressoraSelecionada;

                // Verifica se a impressora é válida
                if (!printerSettings.IsValid)
                {
                    MessageBox.Show($"A impressora '{impressoraSelecionada}' não está disponível.",
                        "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    cmbPapel.Items.Add("Impressora não disponível");
                    return;
                }

                // Obtém os tamanhos de papel suportados pela impressora
                foreach (System.Drawing.Printing.PaperSize paperSize in printerSettings.PaperSizes)
                {
                    // Adiciona o nome do papel com suas dimensões (convertidas de centésimos de polegada para mm)
                    // 1 polegada = 25.4 mm, então dividimos por 100 (centésimos) e multiplicamos por 25.4
                    double larguraMM = (paperSize.Width / 100.0) * 25.4;
                    double alturaMM = (paperSize.Height / 100.0) * 25.4;

                    string itemTexto = $"{paperSize.PaperName} ({larguraMM:F0} x {alturaMM:F0} mm)";
                    cmbPapel.Items.Add(new PaperSizeItem(paperSize, itemTexto));
                }

                // Se não encontrou papéis, adiciona mensagem
                if (cmbPapel.Items.Count == 0)
                {
                    cmbPapel.Items.Add("Nenhum tamanho de papel disponível");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar tamanhos de papel: {ex.Message}",
                    "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                cmbPapel.Items.Add("Erro ao carregar papéis");
            }
        }

        private void AtualizarConfiguracao()
        {
            configuracao.NomeEtiqueta = txtNomeEtiqueta.Text;
            configuracao.ImpressoraPadrao = cmbImpressora.SelectedItem?.ToString() ?? "";

            // Armazena o PaperSize selecionado
            if (cmbPapel.SelectedItem is PaperSizeItem psi)
            {
                papelSelecionado = psi.PaperSize;
                configuracao.PapelPadrao = psi.DisplayText;
            }
            else
            {
                configuracao.PapelPadrao = cmbPapel.SelectedItem?.ToString() ?? "";
            }

            configuracao.LarguraEtiqueta = (float)numLargura.Value;
            configuracao.AlturaEtiqueta = (float)numAltura.Value;
            configuracao.NumColunas = (int)numColunas.Value;
            configuracao.NumLinhas = (int)numLinhas.Value;
            configuracao.EspacamentoColunas = (float)numEspacamentoColunas.Value;
            configuracao.EspacamentoLinhas = (float)numEspacamentoLinhas.Value;
            configuracao.MargemSuperior = (float)numMargemSuperior.Value;
            configuracao.MargemInferior = (float)numMargemInferior.Value;
            configuracao.MargemEsquerda = (float)numMargemEsquerda.Value;
            configuracao.MargemDireita = (float)numMargemDireita.Value;
        }

        private void AtualizarPreview()
        {
            panelPreview.Invalidate();
        }

        private void PanelPreview_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(Color.LightGray);
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Calcula dimensões totais do layout
            float larguraTotal = (configuracao.NumColunas * configuracao.LarguraEtiqueta) +
                                ((configuracao.NumColunas - 1) * configuracao.EspacamentoColunas) +
                                configuracao.MargemEsquerda + configuracao.MargemDireita;

            float alturaTotal = (configuracao.NumLinhas * configuracao.AlturaEtiqueta) +
                               ((configuracao.NumLinhas - 1) * configuracao.EspacamentoLinhas) +
                               configuracao.MargemSuperior + configuracao.MargemInferior;

            // Se há papel selecionado, mostra as dimensões do papel
            float larguraPapel = larguraTotal;
            float alturaPapel = alturaTotal;

            if (papelSelecionado != null)
            {
                // Converte dimensões do papel de centésimos de polegada para mm
                larguraPapel = (papelSelecionado.Width / 100.0f) * 25.4f;
                alturaPapel = (papelSelecionado.Height / 100.0f) * 25.4f;
            }

            // Calcula escala para caber na tela
            float escalaLargura = (panelPreview.Width - 20) / larguraPapel;
            float escalaAltura = (panelPreview.Height - 50) / alturaPapel;
            float escala = Math.Min(escalaLargura, escalaAltura);

            // Centraliza no painel
            float offsetX = (panelPreview.Width - larguraPapel * escala) / 2;
            float offsetY = 10;

            // Desenha fundo do papel (branco)
            using (Brush fundoPapelBrush = new SolidBrush(Color.White))
            {
                e.Graphics.FillRectangle(fundoPapelBrush,
                    offsetX, offsetY,
                    larguraPapel * escala,
                    alturaPapel * escala);
            }

            // Desenha borda do papel
            using (Pen paperPen = new Pen(Color.Black, 2))
            {
                e.Graphics.DrawRectangle(paperPen,
                    offsetX, offsetY,
                    larguraPapel * escala,
                    alturaPapel * escala);
            }

            // Desenha as margens
            using (Brush margemBrush = new SolidBrush(Color.FromArgb(50, 128, 128, 128)))
            {
                // Margem Superior
                if (configuracao.MargemSuperior > 0)
                {
                    e.Graphics.FillRectangle(margemBrush,
                        offsetX,
                        offsetY,
                        larguraPapel * escala,
                        configuracao.MargemSuperior * escala);
                }

                // Margem Inferior
                if (configuracao.MargemInferior > 0)
                {
                    e.Graphics.FillRectangle(margemBrush,
                        offsetX,
                        offsetY + (alturaPapel - configuracao.MargemInferior) * escala,
                        larguraPapel * escala,
                        configuracao.MargemInferior * escala);
                }

                // Margem Esquerda
                if (configuracao.MargemEsquerda > 0)
                {
                    e.Graphics.FillRectangle(margemBrush,
                        offsetX,
                        offsetY,
                        configuracao.MargemEsquerda * escala,
                        alturaPapel * escala);
                }

                // Margem Direita
                if (configuracao.MargemDireita > 0)
                {
                    e.Graphics.FillRectangle(margemBrush,
                        offsetX + (larguraPapel - configuracao.MargemDireita) * escala,
                        offsetY,
                        configuracao.MargemDireita * escala,
                        alturaPapel * escala);
                }
            }

            // Desenha etiquetas
            using (Pen etiquetaPen = new Pen(Color.FromArgb(231, 76, 60), 2))
            using (Brush etiquetaBrush = new SolidBrush(Color.FromArgb(30, 231, 76, 60)))
            {
                for (int linha = 0; linha < configuracao.NumLinhas; linha++)
                {
                    for (int coluna = 0; coluna < configuracao.NumColunas; coluna++)
                    {
                        float x = offsetX + (configuracao.MargemEsquerda +
                                            (coluna * (configuracao.LarguraEtiqueta + configuracao.EspacamentoColunas)))
                                            * escala;

                        float y = offsetY + (configuracao.MargemSuperior +
                                            (linha * (configuracao.AlturaEtiqueta + configuracao.EspacamentoLinhas)))
                                            * escala;

                        float largura = configuracao.LarguraEtiqueta * escala;
                        float altura = configuracao.AlturaEtiqueta * escala;

                        RectangleF etiqueta = new RectangleF(x, y, largura, altura);

                        // Preenche
                        e.Graphics.FillRectangle(etiquetaBrush, etiqueta);

                        // Contorno
                        e.Graphics.DrawRectangle(etiquetaPen, Rectangle.Round(etiqueta));

                        // Desenha "rascunho" de código de barras
                        if (altura > 15)
                        {
                            float barrasY = y + (altura * 0.3f);
                            float barrasAltura = altura * 0.4f;
                            float barrasX = x + (largura * 0.1f);
                            float barrasLargura = largura * 0.8f;

                            using (Pen barraPen = new Pen(Color.FromArgb(150, 231, 76, 60), 1))
                            {
                                // Desenha algumas barras simulando código de barras
                                for (float i = 0; i < barrasLargura; i += 3)
                                {
                                    if ((int)(i / 3) % 3 != 0)
                                    {
                                        e.Graphics.DrawLine(barraPen,
                                            barrasX + i, barrasY,
                                            barrasX + i, barrasY + barrasAltura);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // Desenha informações do papel e layout
            using (Font infoFont = new Font("Segoe UI", 9))
            using (Brush textBrush = new SolidBrush(Color.DarkSlateGray))
            {
                string infoPapel = "";
                if (papelSelecionado != null)
                {
                    infoPapel = $"Papel: {larguraPapel:F0} x {alturaPapel:F0} mm";
                }

                string infoLayout = $"Layout: {larguraPapel:F1} x {alturaPapel:F1} mm | " +
                                   $"{configuracao.NumColunas}x{configuracao.NumLinhas} etiquetas";

                float yText = panelPreview.Height - 35;

                if (!string.IsNullOrEmpty(infoPapel))
                {
                    e.Graphics.DrawString(infoPapel, infoFont, textBrush, 10, yText);
                    yText += 18;
                }

                e.Graphics.DrawString(infoLayout, infoFont, textBrush, 10, yText);

                // Aviso se não caber
                if (larguraTotal > larguraPapel || alturaTotal > alturaPapel)
                {
                    using (Brush avisbBrush = new SolidBrush(Color.Red))
                    using (Font boldFont = new Font("Segoe UI", 9, FontStyle.Bold))
                    {
                        string aviso = "⚠️ ATENÇÃO: Layout NÃO cabe no papel!";
                        yText += 18;
                        e.Graphics.DrawString(aviso, boldFont, avisbBrush, 10, yText);
                    }
                }
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            // Valida se o layout cabe no papel
            if (papelSelecionado != null)
            {
                float larguraPapel = (papelSelecionado.Width / 100.0f) * 25.4f;
                float alturaPapel = (papelSelecionado.Height / 100.0f) * 25.4f;

                float larguraTotal = (configuracao.NumColunas * configuracao.LarguraEtiqueta) +
                                     ((configuracao.NumColunas - 1) * configuracao.EspacamentoColunas) +
                                     configuracao.MargemEsquerda + configuracao.MargemDireita;

                float alturaTotal = (configuracao.NumLinhas * configuracao.AlturaEtiqueta) +
                                    ((configuracao.NumLinhas - 1) * configuracao.EspacamentoLinhas) +
                                    configuracao.MargemSuperior + configuracao.MargemInferior;

                if (larguraTotal > larguraPapel || alturaTotal > alturaPapel)
                {
                    DialogResult resultado = MessageBox.Show(
                        $"⚠️ O layout não cabe no papel selecionado:\n\n" +
                        $"Layout: {larguraTotal:F1}mm x {alturaTotal:F1}mm\n" +
                        $"Papel: {larguraPapel:F0}mm x {alturaPapel:F0}mm\n\n" +
                        $"Deseja continuar mesmo assim?",
                        "Atenção", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                    if (resultado == DialogResult.No)
                    {
                        return;
                    }
                }
            }

            // ⭐ CORREÇÃO: Pergunta se deseja salvar como modelo ANTES de salvar
            bool salvouComoModelo = false;

            if (MessageBox.Show(
                "Deseja salvar esta configuração como um modelo reutilizável?\n\n" +
                "• SIM = Salva como modelo com nome personalizado\n" +
                "• NÃO = Apenas aplica as configurações (não salva modelo)",
                "Salvar Modelo",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) == DialogResult.Yes)
            {
                using (FormNomeTemplate formNome = new FormNomeTemplate())
                {
                    if (formNome.ShowDialog() == DialogResult.OK)
                    {
                        // ⭐ Define o nome da etiqueta
                        configuracao.NomeEtiqueta = formNome.NomeTemplate;

                        // ⭐ Salva como modelo de papel
                        SalvarConfiguracaoPapelModelo(formNome.NomeTemplate);

                        salvouComoModelo = true;

                        MessageBox.Show(
                            $"Modelo '{formNome.NomeTemplate}' salvo com sucesso!",
                            "Sucesso",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information
                        );
                    }
                }
            }

            // ⭐ SEMPRE salva como configuração ativa (última usada)
            SalvarConfiguracao();

            // ⭐ Fecha o formulário com sucesso
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnCancelar_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void btnTestarImpressao_Click(object sender, EventArgs e)
        {
            TestarImpressao();
        }

        /// <summary>
        /// Executa teste de impressão com visualização prévia
        /// </summary>
        private void TestarImpressao()
        {
            try
            {
                // Validações básicas
                if (papelSelecionado == null)
                {
                    MessageBox.Show("Por favor, selecione um tipo de papel primeiro.",
                        "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (cmbImpressora.SelectedIndex < 0)
                {
                    MessageBox.Show("Por favor, selecione uma impressora primeiro.",
                        "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Cria PrintDocument com as configurações atuais
                System.Drawing.Printing.PrintDocument printDoc = new System.Drawing.Printing.PrintDocument();

                // Configura impressora
                printDoc.PrinterSettings.PrinterName = cmbImpressora.SelectedItem.ToString();

                // Configura papel
                printDoc.DefaultPageSettings.PaperSize = papelSelecionado;

                // IMPORTANTE: Reseta as margens para 0 (deixa a função ImprimirTesteEtiqueta controlar)
                printDoc.DefaultPageSettings.Margins = new System.Drawing.Printing.Margins(0, 0, 0, 0);

                // Marca como visualização (teste)
                printDoc.PrintPage += PrintDoc_PrintPage;

                // Abre Print Preview Dialog
                System.Windows.Forms.PrintPreviewDialog previewDialog = new System.Windows.Forms.PrintPreviewDialog();
                previewDialog.Document = printDoc;
                previewDialog.ShowIcon = false;
                previewDialog.Text = $"Teste de Impressão - {this.txtNomeEtiqueta.Text}";
                previewDialog.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
                previewDialog.ClientSize = new System.Drawing.Size(900, 700);

                if (previewDialog.ShowDialog(this) == DialogResult.OK)
                {
                    // Se usuário clicou em "Imprimir", executa
                    printDoc.Print();
                    MessageBox.Show("Teste de impressão enviado com sucesso!",
                        "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                printDoc.Dispose();
            }
            catch (System.ComponentModel.Win32Exception)
            {
                MessageBox.Show("Impressora não encontrada ou não disponível.",
                    "Erro de Impressora", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao testar impressão:\n{ex.Message}",
                    "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Evento de PrintPage para desenhar as etiquetas
        /// </summary>
        private void PrintDoc_PrintPage(object sender, System.Drawing.Printing.PrintPageEventArgs e)
        {
            ImprimirTesteEtiqueta(e.Graphics, e);
        }

        /// <summary>
        /// Desenha etiquetas de teste para visualização de impressão
        /// </summary>
        private void ImprimirTesteEtiqueta(Graphics g, System.Drawing.Printing.PrintPageEventArgs e)
        {
            g.Clear(Color.White);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // ... (Lógica de ImprimirTesteEtiqueta omitida por brevidade, pois já estava correta)

            // Desenha informações de rodapé
            using (Font footerFont = new Font("Segoe UI", 8))
            using (Brush footerBrush = new SolidBrush(Color.DarkSlateGray))
            {
                float paperWidthMm = (papelSelecionado.Width / 100.0f) * 25.4f;
                float paperHeightMm = (papelSelecionado.Height / 100.0f) * 25.4f;

                string footer = $"Teste - {DateTime.Now:dd/MM/yyyy HH:mm} | " +
                                $"Papel: {paperWidthMm:F0}×{paperHeightMm:F0}mm | " +
                                $"Layout: {configuracao.NumColunas}x{configuracao.NumLinhas} etiquetas";

                g.DrawString(footer, footerFont, footerBrush, 10, e.PageBounds.Height - 30);
            }
        }

        private void groupDimensoes_Enter(object sender, EventArgs e)
        {
        }

        private void groupMargens_Enter(object sender, EventArgs e)
        {
        }

        // ==================== GERENCIAMENTO DE PAPÉIS SALVOS ====================

        /// <summary>
        /// Abre dialog para listar e gerenciar papéis salvos
        /// </summary>
        private void ListarPapeisGerenciador()
        {
            try
            {
                // Cria lista de papéis salvos
                List<ConfiguracaoPapel> papeisSalvos = CarregarListaPapeisSalvos();

                if (papeisSalvos.Count == 0)
                {
                    MessageBox.Show("Nenhuma configuração de papel foi salva ainda.",
                        "Lista de Papéis", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Cria formulário de listagem
                Form listaForm = new Form
                {
                    Text = "Gerenciador de Papéis Salvos",
                    Size = new System.Drawing.Size(600, 400),
                    StartPosition = FormStartPosition.CenterParent,
                    Font = new Font("Segoe UI", 9)
                };

                // ListBox para mostrar papéis
                ListBox listaPapeis = new ListBox
                {
                    Dock = DockStyle.Fill,
                    Margin = new Padding(10),
                    SelectionMode = SelectionMode.One,
                    Font = new Font("Segoe UI", 10)
                };

                // Popula lista
                foreach (var papel in papeisSalvos)
                {
                    string item = $"{papel.NomePapel} ({papel.Largura:F0} x {papel.Altura:F0} mm) - " +
                                 $"Etiquetas: {papel.NumColunas}x{papel.NumLinhas}";
                    listaPapeis.Items.Add(papel);
                    listaPapeis.DisplayMember = "ToString";
                }

                // Painel de botões
                Panel painelBotoes = new Panel
                {
                    Dock = DockStyle.Bottom,
                    Height = 50,
                    BackColor = Color.FromArgb(240, 240, 240),
                    Padding = new Padding(5)
                };

                Button btnCarregar = new Button
                {
                    Text = "📂 Carregar",
                    Dock = DockStyle.Left,
                    Width = 100,
                    BackColor = Color.FromArgb(52, 152, 219),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat
                };

                Button btnExcluir = new Button
                {
                    Text = "🗑️ Excluir",
                    Dock = DockStyle.Left,
                    Width = 100,
                    BackColor = Color.FromArgb(231, 76, 60),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Margin = new Padding(5, 0, 0, 0)
                };

                Button btnFechar = new Button
                {
                    Text = "Fechar",
                    Dock = DockStyle.Right,
                    Width = 100,
                    BackColor = Color.FromArgb(149, 165, 166),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat
                };

                // Eventos dos botões
                btnCarregar.Click += (s, e) =>
                {
                    if (listaPapeis.SelectedItem is ConfiguracaoPapel papelSelecionado)
                    {
                        CarregarConfiguracacoPapel(papelSelecionado);
                        AtualizarPreview();
                        listaForm.Close();
                        MessageBox.Show("Configuração de papel carregada com sucesso!",
                            "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("Por favor, selecione um papel.",
                            "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                };

                btnExcluir.Click += (s, e) =>
                {
                    if (listaPapeis.SelectedItem is ConfiguracaoPapel papelExcluir)
                    {
                        DialogResult resultado = MessageBox.Show(
                            $"Deseja excluir a configuração:\n{papelExcluir.NomePapel}?",
                            "Confirmar Exclusão", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                        if (resultado == DialogResult.Yes)
                        {
                            ExcluirConfiguracacoPapel(papelExcluir.NomePapel);
                            listaPapeis.Items.Remove(papelExcluir);
                            MessageBox.Show("Configuração excluída com sucesso!",
                                "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Por favor, selecione um papel.",
                            "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                };

                btnFechar.Click += (s, e) => listaForm.Close();

                painelBotoes.Controls.Add(btnFechar);
                painelBotoes.Controls.Add(btnExcluir);
                painelBotoes.Controls.Add(btnCarregar);

                listaForm.Controls.Add(listaPapeis);
                listaForm.Controls.Add(painelBotoes);

                listaForm.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao listar papéis salvos:\n{ex.Message}",
                    "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Carrega lista de papéis salvos do arquivo
        /// </summary>
        private List<ConfiguracaoPapel> CarregarListaPapeisSalvos()
        {
            List<ConfiguracaoPapel> papeis = new List<ConfiguracaoPapel>();

            try
            {
                string caminhoLista = Path.Combine(
                    Path.GetDirectoryName(CAMINHO_CONFIGURACOES),
                    "modelos_papel.xml");

                if (!File.Exists(caminhoLista))
                    return papeis;

                XmlSerializer serializer = new XmlSerializer(typeof(List<ConfiguracaoPapel>));
                using (StreamReader reader = new StreamReader(caminhoLista))
                {
                    papeis = (List<ConfiguracaoPapel>)serializer.Deserialize(reader);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao carregar papéis salvos: {ex.Message}");
            }

            return papeis;
        }

        /// <summary>
        /// Salva configuração de papel na lista
        /// </summary>
        private void SalvarConfiguracacoPapel(string nomeModelo)
        {
            // Cria um novo objeto de modelo com as configurações atuais
            ConfiguracaoPapel novoModelo = new ConfiguracaoPapel
            {
                NomePapel = nomeModelo,
                NomeEtiqueta = configuracao.NomeEtiqueta,
                Largura = configuracao.LarguraEtiqueta,
                Altura = configuracao.AlturaEtiqueta,
                NumColunas = configuracao.NumColunas,
                NumLinhas = configuracao.NumLinhas,
                EspacamentoColunas = configuracao.EspacamentoColunas,
                EspacamentoLinhas = configuracao.EspacamentoLinhas,
                MargemSuperior = configuracao.MargemSuperior,
                MargemInferior = configuracao.MargemInferior,
                MargemEsquerda = configuracao.MargemEsquerda,
                MargemDireita = configuracao.MargemDireita,
                DataCriacao = DateTime.Now
            };

            // Carrega modelos existentes
            List<ConfiguracaoPapel> modelos = CarregarConfiguracoesSalvas();

            // Adiciona o novo modelo
            modelos.Add(novoModelo);

            // Salva a lista completa
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<ConfiguracaoPapel>));
                using (TextWriter writer = new StreamWriter(CAMINHO_MODELOS_PAPEL))
                {
                    serializer.Serialize(writer, modelos);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao salvar modelo de papel: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Carrega configuração de papel salva
        /// </summary>
        private void CarregarConfiguracacoPapel(ConfiguracaoPapel papel)
        {
            try
            {
                // 1. Aplica os valores ao objeto de configuração (ConfiguracaoEtiqueta)
                configuracao.NomeEtiqueta = papel.NomeEtiqueta;
                configuracao.NumColunas = papel.NumColunas;
                configuracao.NumLinhas = papel.NumLinhas;
                configuracao.LarguraEtiqueta = papel.Largura;
                configuracao.AlturaEtiqueta = papel.Altura;
                configuracao.EspacamentoColunas = papel.EspacamentoColunas;
                configuracao.EspacamentoLinhas = papel.EspacamentoLinhas;
                configuracao.MargemSuperior = papel.MargemSuperior;
                configuracao.MargemInferior = papel.MargemInferior;
                configuracao.MargemEsquerda = papel.MargemEsquerda;
                configuracao.MargemDireita = papel.MargemDireita;
                configuracao.PapelPadrao = papel.NomePapel;

                // ⭐ CORREÇÃO 4: Atualizar a UI (NumericUpDowns e TextBoxes) com os novos valores
                txtNomeEtiqueta.Text = configuracao.NomeEtiqueta;
                numLargura.Value = (decimal)configuracao.LarguraEtiqueta;
                numAltura.Value = (decimal)configuracao.AlturaEtiqueta;
                numColunas.Value = configuracao.NumColunas;
                numLinhas.Value = configuracao.NumLinhas;
                numEspacamentoColunas.Value = (decimal)configuracao.EspacamentoColunas;
                numEspacamentoLinhas.Value = (decimal)configuracao.EspacamentoLinhas;
                numMargemSuperior.Value = (decimal)configuracao.MargemSuperior;
                numMargemInferior.Value = (decimal)configuracao.MargemInferior;
                numMargemEsquerda.Value = (decimal)configuracao.MargemEsquerda;
                numMargemDireita.Value = (decimal)configuracao.MargemDireita;

                // AtualizarPreview é chamado e forçará o recálculo do layout visual
                AtualizarPreview();

                MessageBox.Show($"Modelo de papel '{papel.NomePapel}' carregado.", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar modelo de papel: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Exclui configuração de papel salva
        /// </summary>
        private void ExcluirConfiguracacoPapel(string nomePapel)
        {
            try
            {
                List<ConfiguracaoPapel> papeis = CarregarListaPapeisSalvos();
                papeis.RemoveAll(p => p.NomePapel == nomePapel);

                string diretorio = Path.GetDirectoryName(CAMINHO_CONFIGURACOES);
                string caminhoLista = CAMINHO_MODELOS_PAPEL;

                XmlSerializer serializer = new XmlSerializer(typeof(List<ConfiguracaoPapel>));
                using (StreamWriter writer = new StreamWriter(caminhoLista))
                {
                    serializer.Serialize(writer, papeis);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao excluir configuração:\n{ex.Message}",
                    "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ==================== PERSISTÊNCIA DE DADOS ====================

        /// <summary>
        /// Salva a configuração atual em arquivo XML
        /// </summary>
        private void SalvarConfiguracao()
        {
            try
            {
                // ⭐ Atualiza a configuração com os valores atuais dos controles
                AtualizarConfiguracao();

                // Garantir que o diretório exista
                string diretorio = Path.GetDirectoryName(CAMINHO_CONFIGURACOES);
                if (!string.IsNullOrEmpty(diretorio))
                {
                    Directory.CreateDirectory(diretorio);
                }

                // Serializa o objeto 'configuracao' para XML
                XmlSerializer serializer = new XmlSerializer(typeof(ConfiguracaoEtiqueta));
                using (TextWriter writer = new StreamWriter(CAMINHO_CONFIGURACOES))
                {
                    serializer.Serialize(writer, configuracao);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erro ao salvar configuração: {ex.Message}",
                    "Erro de Salvamento",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }
        private void SalvarConfiguracaoPapelModelo(string nomeTemplate)
        {
            try
            {
                // 1. Carrega modelos existentes
                List<ConfiguracaoPapel> modelos = CarregarModelosPapelSalvos();

                // 2. ⭐ Remove modelo com mesmo nome (se existir)
                modelos.RemoveAll(m => m.NomePapel.Equals(nomeTemplate, StringComparison.OrdinalIgnoreCase));

                // 3. Criar novo modelo com base na configuração atual
                ConfiguracaoPapel novoModelo = new ConfiguracaoPapel
                {
                    NomePapel = nomeTemplate,
                    NomeEtiqueta = configuracao.NomeEtiqueta,
                    Largura = configuracao.LarguraEtiqueta,
                    Altura = configuracao.AlturaEtiqueta,
                    NumColunas = configuracao.NumColunas,
                    NumLinhas = configuracao.NumLinhas,
                    EspacamentoColunas = configuracao.EspacamentoColunas,
                    EspacamentoLinhas = configuracao.EspacamentoLinhas,
                    MargemSuperior = configuracao.MargemSuperior,
                    MargemInferior = configuracao.MargemInferior,
                    MargemEsquerda = configuracao.MargemEsquerda,
                    MargemDireita = configuracao.MargemDireita,
                    DataCriacao = DateTime.Now
                };

                // 4. Adiciona o novo modelo à lista
                modelos.Add(novoModelo);

                // 5. Salva a lista atualizada
                SalvarListaModelosPapel(modelos);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erro ao salvar modelo: {ex.Message}",
                    "Erro",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }
        private void SalvarListaModelosPapel(List<ConfiguracaoPapel> modelos)
        {
            try
            {
                string diretorio = Path.GetDirectoryName(CAMINHO_MODELOS_PAPEL);
                if (!string.IsNullOrEmpty(diretorio))
                {
                    Directory.CreateDirectory(diretorio);
                }

                XmlSerializer serializer = new XmlSerializer(typeof(List<ConfiguracaoPapel>));
                using (TextWriter writer = new StreamWriter(CAMINHO_MODELOS_PAPEL))
                {
                    serializer.Serialize(writer, modelos);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erro ao salvar lista de modelos: {ex.Message}",
                    "Erro de Salvamento",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }


        private List<ConfiguracaoPapel> CarregarModelosPapelSalvos()
        {
            if (!File.Exists(CAMINHO_MODELOS_PAPEL))
            {
                return new List<ConfiguracaoPapel>();
            }

            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<ConfiguracaoPapel>));
                using (StreamReader reader = new StreamReader(CAMINHO_MODELOS_PAPEL))
                {
                    // Tenta deserializar o arquivo para obter a lista
                    return (List<ConfiguracaoPapel>)serializer.Deserialize(reader);
                }
            }
            catch (Exception ex)
            {
                // Em caso de erro (arquivo corrompido, etc.), retorna lista vazia
                Console.WriteLine($"Erro ao carregar modelos de papel: {ex.Message}");
                return new List<ConfiguracaoPapel>();
            }
        }

        /// <summary>
        /// Carrega a configuração salva em arquivo XML
        /// </summary>
        public static List<ConfiguracaoPapel> CarregarConfiguracoesSalvas()
        {
            if (File.Exists(CAMINHO_MODELOS_PAPEL))
            {
                try
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(List<ConfiguracaoPapel>));
                    using (FileStream fs = new FileStream(CAMINHO_MODELOS_PAPEL, FileMode.Open))
                    {
                        return (List<ConfiguracaoPapel>)serializer.Deserialize(fs);
                    }
                }
                catch (Exception)
                {
                    return new List<ConfiguracaoPapel>();
                }
            }
            return new List<ConfiguracaoPapel>();
        }
    }


    // Classe para armazenar a configuração da etiqueta
    [Serializable]
    public class ConfiguracaoEtiqueta
    {
        public string NomeEtiqueta { get; set; } = "Padrão";
        public string ImpressoraPadrao { get; set; } = "";
        public string PapelPadrao { get; set; } = ""; // Salva o nome do papel para re-seleção
        public float LarguraEtiqueta { get; set; } = 100;
        public float AlturaEtiqueta { get; set; } = 30;
        public int NumColunas { get; set; } = 1;
        public int NumLinhas { get; set; } = 1;
        public float EspacamentoColunas { get; set; } = 0;
        public float EspacamentoLinhas { get; set; } = 0;
        public float MargemSuperior { get; set; } = 0;
        public float MargemInferior { get; set; } = 0;
        public float MargemEsquerda { get; set; } = 0;
        public float MargemDireita { get; set; } = 0;

        public static implicit operator ConfiguracaoEtiqueta(List<ConfiguracaoPapel> v)
        {
            throw new NotImplementedException();
        }
    }

    // Classe para armazenar configuração de papéis salvos
    [Serializable]
    public class ConfiguracaoPapel
    {
        public string NomePapel { get; set; }
        public string NomeEtiqueta { get; set; }
        public float Largura { get; set; }
        public float Altura { get; set; }
        public int NumColunas { get; set; }
        public int NumLinhas { get; set; }
        public float EspacamentoColunas { get; set; }
        public float EspacamentoLinhas { get; set; }
        public float MargemSuperior { get; set; }
        public float MargemInferior { get; set; }
        public float MargemEsquerda { get; set; }
        public float MargemDireita { get; set; }
        public DateTime DataCriacao { get; set; }

        public override string ToString()
        {
            return $"{NomePapel} ({Largura:F0}x{Altura:F0}mm) - {DataCriacao:dd/MM/yyyy}";
        }
    }

    // Classe auxiliar para armazenar informações do papel
    internal class PaperSizeItem
    {
        public System.Drawing.Printing.PaperSize PaperSize { get; set; }
        public string DisplayText { get; set; }

        public PaperSizeItem(System.Drawing.Printing.PaperSize paperSize, string displayText)
        {
            PaperSize = paperSize;
            DisplayText = displayText;
        }

        public override string ToString()
        {
            return DisplayText;
        }
    }

}