using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using EtiquetaFORNew.UI;

namespace EtiquetaFORNew.Forms
{
    /// <summary>
    /// FormDesign modernizado com configuração de página integrada e funcionalidade completa de elementos
    /// </summary>
    public partial class FormDesignNovo : Form, IMessageFilter
    {
        #region Campos Privados

        private TemplateEtiqueta template;
        private ConfiguracaoEtiqueta configuracao;
        private string nomeTemplateAtual;

        // Controles do canvas
        private Panel panelCanvas;
        private PictureBox pbCanvas;
        private Panel panelTop;
        private Panel panelBotoes;
        private Button btnZoomMenos;
        private Button btnZoomMais;
        private Label lblZoomPercentual;
        private ContextMenuStrip menuContextoCanvas;
        private DesignerToolbar toolbarInferior;
        private StatusBarManager statusBarManager;

        // Controles do painel de configuração
        private Panel panelConfiguracao;
        private Button btnToggleConfig;
        private NumericUpDown numLargura;
        private NumericUpDown numAltura;
        private ComboBox cmbImpressora;
        private ComboBox cmbPapel;
        private NumericUpDown numColunas;
        private NumericUpDown numLinhas;
        private NumericUpDown numEspacamentoColunas;
        private NumericUpDown numEspacamentoLinhas;
        private NumericUpDown numMargemSuperior;
        private NumericUpDown numMargemInferior;
        private NumericUpDown numMargemEsquerda;
        private NumericUpDown numMargemDireita;
        private CheckBox chkPadraoDesativar;
        private Panel panelPropriedades;
        private Button btnAlinharEsquerda;
        private Button btnAlinharCentro;
        private Button btnAlinharDireita;
        private NumericUpDown numElementoLargura;
        private NumericUpDown numElementoAltura;
        private NumericUpDown numTamanhoFonte;
        private CheckBox chkNegrito;
        private CheckBox chkItalico;
        private Button btnCor;
        private Button btnCorFundo;
        private Button btnFundoPreto;
        private Button btnFundoBranco;
        private Button btnFundoTransparente;
        private ComboBox cmbBordaElemento;
        private NumericUpDown numEspessuraBorda;
        private Label lblNumeracaoAgrupada;
        private CheckBox chkNumeracaoAgrupada;
        private Label lblSimbologiaCodigoBarras;
        private ComboBox cmbSimbologiaCodigoBarras;
        private Label lblExibirNumeracaoCodigoBarras;
        private CheckBox chkExibirNumeracaoCodigoBarras;
        private Label lblRenderizacaoLinear1D;
        private CheckBox chkRenderizacaoLinear1D;
        private Label lblPropriedadesElemento;
        private ComboBox cmbFonte;
        private Label lblCalculoPreco;
        private ComboBox cmbOperadorCalculoPreco;
        private NumericUpDown numValorCalculoPreco;
        private Label lblExpressaoFormula;
        private TextBox txtExpressaoFormula;
        private Button btnInserirCampoExpressao;
        private Button btnEditorExpressao;

        // Toolbox de elementos
        private Panel panelToolbox;
        private bool? toolboxDistribuidoraAtiva;
        private RectangleF boundsIniciaisEmMM;

        private bool rotacionando = false;
        private float anguloInicial = 0f;
        private PointF centroRotacao;

        // Controles de elementos e seleção
        private ElementoEtiqueta elementoSelecionado;
        private bool arrastando = false;
        private bool redimensionando = false;
        private Point pontoInicialMouse;
        private Rectangle boundsIniciais;
        private Point deltaArrasto;
        private int handleSelecionado = -1;
        private int handleSobMouse = -1;
        private ElementoEtiqueta elementoHoverRotacao;

        private List<ElementoEtiqueta> elementosSelecionados = new List<ElementoEtiqueta>();
        private bool selecionandoComRetangulo = false;
        private bool atualizandoPropriedades = false;
        private Point pontoInicialSelecao;
        private Rectangle retanguloSelecao;

        // =========================================================
        // RÉGUA DE ALINHAMENTO (Snap Lines)
        // =========================================================
        /// <summary>
        /// Linhas guia ativas durante o arrasto.
        /// bool = isVertical (true = linha vertical / alinhamento em X)
        /// float = posição em pixels no canvas
        /// </summary>
        private List<(bool isVertical, float posicaoPx)> linhasGuiaAtivas
            = new List<(bool, float)>();

        /// <summary>
        /// Tolerância em pixels para ativar o snap
        /// </summary>
        private const float SNAP_THRESHOLD_PX = 6f;

        /// <summary>
        /// Habilita/desabilita as linhas de alinhamento
        /// </summary>
        private bool snapAtivo = true;
        // =========================================================

        // Constantes
        private const float MM_PARA_PIXEL = 96f / 25.4f;
        private float zoom = 1.0f;
        private string elementosCopiados;
        private int sequenciaColagem;
        private bool filtroMouseAtivo;

        //Pilha que armazena os estados para desfazer do template
        private Stack<string> historicoUndo = new Stack<string>();

        //Limite para não consumir memória infinita
        private const int Max_Undo_Steps = 50;

        #endregion

        #region Construtor e Inicialização

        public FormDesignNovo(TemplateEtiqueta templateInicial, string nomeTemplate = null)
        {
            InitializeComponent();

            this.template = templateInicial ?? new TemplateEtiqueta();
            this.nomeTemplateAtual = nomeTemplate;

            if (!string.IsNullOrEmpty(nomeTemplate))
            {
                configuracao = ConfiguracaoManager.CarregarConfiguracao(nomeTemplate);
            }

            if (configuracao == null)
            {
                configuracao = new ConfiguracaoEtiqueta
                {
                    NomeEtiqueta = nomeTemplate ?? "Novo Template",
                    LarguraEtiqueta = template.Largura > 0 ? template.Largura : 100,
                    AlturaEtiqueta = template.Altura > 0 ? template.Altura : 30,
                    ImpressoraPadrao = "BTP-L42(D)",
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

            template.Largura = configuracao.LarguraEtiqueta;
            template.Altura = configuracao.AlturaEtiqueta;

            ConfigurarFormulario();
            CriarInterface();
        }

        private void FormDesignNovo_Load(object sender, EventArgs e)
        {
            VersaoHelper.DefinirTituloComVersao(this, "Designer de Etiquetas");
            CarregarDadosNaInterface();
            AjustarLayoutResponsivo();
            AtualizarStatusDocumento();
            AtualizarStatusBarSelecao();
        }

        private void ConfigurarFormulario()
        {
            SalvarEstadoHistorico();
            this.StartPosition = FormStartPosition.Manual;
            this.DoubleBuffered = true;
            this.BackColor = ThemeManager.WorkspaceBackground;
            this.KeyPreview = true;
            this.KeyDown += FormDesignNovo_KeyDown;
            this.Shown += FormDesignNovo_Shown;
            this.FormClosed += (s, e) => RemoverFiltroMouse();
            this.Activated += (s, e) => AplicarLayoutToolboxResponsivo();
            this.DpiChanged += (s, e) => AjustarLayoutResponsivo();
            
        }

        #endregion

        #region Criação de Interface

        private void CriarInterface()
        {
            // ==================== BARRA SUPERIOR ====================
            panelTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 48,
                BackColor = ThemeManager.HeaderBackground,
                BorderStyle = BorderStyle.None
            };
            this.Controls.Add(panelTop);

            Label lblEmoji = new Label
            {
                Text = "◆",
                Location = new Point(18, 9),
                Size = new Size(30, 30),
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = ThemeManager.SmartPrintOrange
            };
            panelTop.Controls.Add(lblEmoji);

            Label lblTitulo = new Label
            {
                Text = "SmartPrint Designer",
                Location = new Point(50, 9),
                Size = new Size(270, 30),
                Font = ThemeManager.HeaderFont,
                ForeColor = Color.White
            };
            panelTop.Controls.Add(lblTitulo);

            // ==================== BOTÕES DE AÇÃO ====================
            panelBotoes = new Panel
            {
                Dock = DockStyle.Right,
                Width = 560,
                Height = 60,
                BackColor = Color.Transparent
            };
            panelTop.Controls.Add(panelBotoes);

            Button btnFechar = new Button
            {
                Text = "✕ Fechar",
                Location = new Point(450, 15),
                Size = new Size(100, 30),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = Color.FromArgb(231, 76, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnFechar.FlatAppearance.BorderSize = 1;
            btnFechar.FlatAppearance.BorderColor = Color.Black;
            btnFechar.Click += BtnFechar_Click;
            panelBotoes.Controls.Add(btnFechar);

            Button btnPreview = new Button
            {
                Text = "👁 Preview",
                Location = new Point(340, 15),
                Size = new Size(100, 30),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = Color.FromArgb(155, 89, 182),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnPreview.FlatAppearance.BorderSize = 1;
            btnPreview.FlatAppearance.BorderColor = Color.Black;
            btnPreview.Click += BtnPreview_Click;
            panelBotoes.Controls.Add(btnPreview);

            Button btnNovo = new Button
            {
                Text = "📄 Novo",
                Location = new Point(230, 15),
                Size = new Size(100, 30),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnNovo.FlatAppearance.BorderSize = 1;
            btnNovo.FlatAppearance.BorderColor = Color.Black;
            btnNovo.Click += BtnNovo_Click;
            panelBotoes.Controls.Add(btnNovo);

            Button btnSalvar = new Button
            {
                Text = "💾 Salvar",
                Location = new Point(120, 15),
                Size = new Size(100, 30),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = Color.FromArgb(46, 204, 113),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnSalvar.FlatAppearance.BorderSize = 1;
            btnSalvar.FlatAppearance.BorderColor = Color.Black;
            btnSalvar.Click += BtnSalvar_Click;
            panelBotoes.Controls.Add(btnSalvar);

            // =========================================================
            // BOTÃO TOGGLE SNAP LINES
            // =========================================================
            Button btnToggleSnap = new Button
            {
                Text = "📐 Guias ON",
                Location = new Point(10, 15),
                Size = new Size(100, 30),
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                BackColor = Color.FromArgb(39, 174, 96),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
                
            };
            btnToggleSnap.FlatAppearance.BorderSize = 1;
            btnToggleSnap.FlatAppearance.BorderColor = Color.Black;
            btnToggleSnap.Click += (s, ev) =>            
            {
                snapAtivo = !snapAtivo;
                btnToggleSnap.Text = snapAtivo ? "📐 Guias ON" : "📐 Guias OFF";
                btnToggleSnap.BackColor = snapAtivo
                    ? Color.FromArgb(39, 174, 96)
                    : Color.FromArgb(149, 165, 166);
                if (!snapAtivo) linhasGuiaAtivas.Clear();
                pbCanvas?.Invalidate();
            };
            btnToggleSnap.Visible = false;
            panelBotoes.Controls.Add(btnToggleSnap);
            panelBotoes.Visible = false;

            // Barra de comandos inferior: os mesmos handlers existentes são reutilizados.
            toolbarInferior = ToolbarManager.Create(
                () => BtnNovo_Click(this, EventArgs.Empty),
                () => BtnSalvar_Click(this, EventArgs.Empty),
                () => BtnPreview_Click(this, EventArgs.Empty),
                Desfazer,
                () => AlterarZoom(-0.1f),
                () => AlterarZoom(0.1f),
                () => BtnFechar_Click(this, EventArgs.Empty));
            this.Controls.Add(toolbarInferior.Container);
            statusBarManager = new StatusBarManager(toolbarInferior.StatusStrip);
            toolbarInferior.UndoButton.Enabled = historicoUndo.Count > 1;
            // =========================================================

            // ==================== PAINEL LATERAL DIREITO - CONFIGURAÇÃO ====================
            panelConfiguracao = new Panel
            {
                Dock = DockStyle.Right,
                Width = 350,
                BackColor = ThemeManager.PanelBackground,
                Padding = new Padding(10),
                AutoScroll = true
            };
            this.Controls.Add(panelConfiguracao);

            CriarPainelConfiguracao();

            btnToggleConfig = new Button
            {
                Text = "▶",
                Size = new Size(30, 80),
                BackColor = Color.FromArgb(46, 204, 113),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnToggleConfig.FlatAppearance.BorderSize = 0;

            this.Controls.Add(btnToggleConfig);
            btnToggleConfig.BringToFront();

            this.Resize += (s, e) =>
            {
                AjustarLayoutResponsivo();
            };

            btnToggleConfig.Click += (s, e) =>
            {
                if (panelConfiguracao.Width > 0)
                {
                    panelConfiguracao.Width = 0;
                    btnToggleConfig.Text = "◀";
                    btnToggleConfig.BackColor = Color.FromArgb(52, 152, 219);
                    btnToggleConfig.Location = new Point(
                        this.ClientSize.Width - btnToggleConfig.Width - 5,
                        btnToggleConfig.Location.Y
                    );
                }
                else
                {
                    panelConfiguracao.Width = ObterLarguraPainelDireito();
                    btnToggleConfig.Text = "▶";
                    btnToggleConfig.BackColor = Color.FromArgb(46, 204, 113);
                    btnToggleConfig.Location = new Point(
                        this.ClientSize.Width - panelConfiguracao.Width - btnToggleConfig.Width,
                        btnToggleConfig.Location.Y
                    );
                }
            };

            // ==================== PAINEL LATERAL ESQUERDO - TOOLBOX ====================

            //if (ModuloAppHelper.EstaEmModuloDistribuidoraWeb())
            //{
                panelToolbox = new Panel
                {
                    Dock = DockStyle.Left,
                    Width = 390,
                    BackColor = ThemeManager.PanelBackground,
                    Padding = new Padding(10),
                    AutoScroll = true,
                    AutoScrollMinSize = new Size(0, 800)
                };
                this.Controls.Add(panelToolbox);
            //}
            //else { 
            //    panelToolbox = new Panel
            //    {
            //        Dock = DockStyle.Left,
            //        Width = 220,
            //        BackColor = Color.FromArgb(236, 240, 241),
            //        Padding = new Padding(10),
            //        AutoScroll = true,
            //        AutoScrollMinSize = new Size(0, 800)
            //    };
            //this.Controls.Add(panelToolbox);
            //}



            CriarToolbox();
            AplicarLayoutToolboxResponsivo();

            // ==================== CANVAS CENTRAL ====================
            panelCanvas = new DoubleBufferedPanel
            {
                Dock = DockStyle.Fill,
                BackColor = ThemeManager.CanvasBackground,
                AutoScroll = true
            };
            panelCanvas.Resize += (s, e) => AtualizarTamanhoCanvas();
            this.Controls.Add(panelCanvas);

            pbCanvas = new DoubleBufferedPictureBox
            {
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(50, 50),
                Size = new Size(400, 300)
            };
            pbCanvas.Paint += PbCanvas_Paint;
            pbCanvas.MouseDown += PbCanvas_MouseDown;
            pbCanvas.MouseMove += PbCanvas_MouseMove;
            pbCanvas.MouseUp += PbCanvas_MouseUp;
            pbCanvas.MouseWheel += PbCanvas_MouseWheel;
            pbCanvas.MouseEnter += (s, e) => pbCanvas.Focus();
            pbCanvas.MouseLeave += PbCanvas_MouseLeave;

            panelCanvas.Controls.Add(pbCanvas);
            LayoutManager.EnableCanvasShadow(panelCanvas, pbCanvas);

            CriarMenuContextoCanvas();

            // Cabeçalho e toolbar ocupam toda a largura; painéis laterais ficam entre ambos.
            panelTop.SendToBack();
            toolbarInferior.Container.SendToBack();
            AtualizarTamanhoCanvas();
        }

        #endregion

        #region Painel de Configuração

        private void CriarPainelConfiguracao()
        {
            int yPos = 10;

            Label lblTituloConfig = new Label
            {
                Text = "⚙ CONFIGURAÇÕES DA PÁGINA",
                Location = new Point(10, yPos),
                Size = new Size(320, 30),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 73, 94)
            };
            panelConfiguracao.Controls.Add(lblTituloConfig);
            yPos += 40;

            Panel linha1 = new Panel
            {
                Location = new Point(10, yPos),
                Size = new Size(320, 2),
                BackColor = Color.FromArgb(230, 126, 34)
            };
            panelConfiguracao.Controls.Add(linha1);
            yPos += 15;

            Label lblDimensoes = CriarLabelSecao("📐 Dimensões da Etiqueta", yPos);
            panelConfiguracao.Controls.Add(lblDimensoes);
            yPos += 25;

            yPos = CriarCampoNumerico("Largura (mm):", out numLargura, yPos, 1, 500, (decimal)configuracao.LarguraEtiqueta);
            numLargura.ValueChanged += (s, e) => AtualizarConfiguracao();

            yPos = CriarCampoNumerico("Altura (mm):", out numAltura, yPos, 1, 500, (decimal)configuracao.AlturaEtiqueta);
            numAltura.ValueChanged += (s, e) => AtualizarConfiguracao();

            yPos += 10;

            Panel linha2 = new Panel
            {
                Location = new Point(10, yPos),
                Size = new Size(320, 2),
                BackColor = Color.FromArgb(230, 126, 34)
            };
            panelConfiguracao.Controls.Add(linha2);
            yPos += 15;

            Label lblImpressao = CriarLabelSecao("🖨️ Impressão", yPos);
            panelConfiguracao.Controls.Add(lblImpressao);
            yPos += 25;

            Label lblImpressora = new Label
            {
                Text = "Impressora:",
                Location = new Point(15, yPos),
                Size = new Size(100, 20),
                Font = new Font("Segoe UI", 9)
            };
            panelConfiguracao.Controls.Add(lblImpressora);

            cmbImpressora = new ComboBox
            {
                Location = new Point(120, yPos - 2),
                Size = new Size(210, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbImpressora.SelectedIndexChanged += CmbImpressora_SelectedIndexChanged;
            panelConfiguracao.Controls.Add(cmbImpressora);
            yPos += 30;

            Label lblPapel = new Label
            {
                Text = "Papel:",
                Location = new Point(15, yPos),
                Size = new Size(100, 20),
                Font = new Font("Segoe UI", 9)
            };
            panelConfiguracao.Controls.Add(lblPapel);

            cmbPapel = new ComboBox
            {
                Location = new Point(120, yPos - 2),
                Size = new Size(210, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbPapel.SelectedIndexChanged += CmbPapel_SelectedIndexChanged;
            panelConfiguracao.Controls.Add(cmbPapel);
            yPos += 35;

            Panel linha3 = new Panel
            {
                Location = new Point(10, yPos),
                Size = new Size(320, 2),
                BackColor = Color.FromArgb(230, 126, 34)
            };
            panelConfiguracao.Controls.Add(linha3);
            yPos += 15;

            Label lblLayout = CriarLabelSecao("📊 Layout da Página", yPos);
            panelConfiguracao.Controls.Add(lblLayout);
            yPos += 25;

            Label lblColunas = new Label
            {
                Text = "Colunas:",
                Location = new Point(15, yPos),
                Size = new Size(60, 20)
            };
            panelConfiguracao.Controls.Add(lblColunas);

            numColunas = new NumericUpDown
            {
                Location = new Point(80, yPos - 2),
                Size = new Size(60, 23),
                Minimum = 1,
                Maximum = 10,
                Value = configuracao.NumColunas
            };
            numColunas.ValueChanged += (s, e) => AtualizarConfiguracao();
            panelConfiguracao.Controls.Add(numColunas);

            Label lblLinhas = new Label
            {
                Text = "Linhas:",
                Location = new Point(175, yPos),
                Size = new Size(50, 20)
            };
            panelConfiguracao.Controls.Add(lblLinhas);

            numLinhas = new NumericUpDown
            {
                Location = new Point(230, yPos - 2),
                Size = new Size(60, 23),
                Minimum = 1,
                Maximum = 20,
                Value = configuracao.NumLinhas
            };
            numLinhas.ValueChanged += (s, e) => AtualizarConfiguracao();
            panelConfiguracao.Controls.Add(numLinhas);
            yPos += 30;

            Panel linha4 = new Panel
            {
                Location = new Point(10, yPos),
                Size = new Size(320, 2),
                BackColor = Color.FromArgb(230, 126, 34)
            };
            panelConfiguracao.Controls.Add(linha4);
            yPos += 15;

            Label lblEspacamento = CriarLabelSecao("↔️ Espaçamentos", yPos);
            panelConfiguracao.Controls.Add(lblEspacamento);
            yPos += 25;

            yPos = CriarCampoNumerico("Entre Colunas (mm):", out numEspacamentoColunas, yPos, 0, 50,
                (decimal)configuracao.EspacamentoColunas, 0.1m);
            numEspacamentoColunas.ValueChanged += (s, e) => AtualizarConfiguracao();

            yPos = CriarCampoNumerico("Entre Linhas (mm):", out numEspacamentoLinhas, yPos, 0, 50,
                (decimal)configuracao.EspacamentoLinhas, 0.1m);
            numEspacamentoLinhas.ValueChanged += (s, e) => AtualizarConfiguracao();

            yPos += 10;

            Panel linha5 = new Panel
            {
                Location = new Point(10, yPos),
                Size = new Size(320, 2),
                BackColor = Color.FromArgb(230, 126, 34)
            };
            panelConfiguracao.Controls.Add(linha5);
            yPos += 15;

            Label lblMargens = CriarLabelSecao("📏 Margens da Página", yPos);
            panelConfiguracao.Controls.Add(lblMargens);
            yPos += 25;

            yPos = CriarCampoNumerico("Superior (mm):", out numMargemSuperior, yPos, 0, 50,
                (decimal)configuracao.MargemSuperior, 0.1m);
            numMargemSuperior.ValueChanged += (s, e) => AtualizarConfiguracao();

            yPos = CriarCampoNumerico("Inferior (mm):", out numMargemInferior, yPos, 0, 50,
                (decimal)configuracao.MargemInferior, 0.1m);
            numMargemInferior.ValueChanged += (s, e) => AtualizarConfiguracao();

            yPos = CriarCampoNumerico("Esquerda (mm):", out numMargemEsquerda, yPos, 0, 50,
                (decimal)configuracao.MargemEsquerda, 0.1m);
            numMargemEsquerda.ValueChanged += (s, e) => AtualizarConfiguracao();

            yPos = CriarCampoNumerico("Direita (mm):", out numMargemDireita, yPos, 0, 50,
                (decimal)configuracao.MargemDireita, 0.1m);
            numMargemDireita.ValueChanged += (s, e) => AtualizarConfiguracao();

            Panel linha6 = new Panel
            {
                Location = new Point(10, yPos),
                Size = new Size(320, 2),
                BackColor = Color.FromArgb(230, 126, 34)
            };
            panelConfiguracao.Controls.Add(linha6);
            yPos += 15;

            Label lblZoom = CriarLabelSecao("🔍 Zoom", yPos);
            panelConfiguracao.Controls.Add(lblZoom);
            yPos += 28;

            btnZoomMenos = CriarBotaoZoom("−", new Point(75, yPos));
            btnZoomMenos.Click += (s, e) => AlterarZoom(-0.1f);
            panelConfiguracao.Controls.Add(btnZoomMenos);

            lblZoomPercentual = new Label
            {
                Text = "100%",
                Location = new Point(125, yPos + 5),
                Size = new Size(70, 25),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            panelConfiguracao.Controls.Add(lblZoomPercentual);

            btnZoomMais = CriarBotaoZoom("+", new Point(205, yPos));
            btnZoomMais.Click += (s, e) => AlterarZoom(0.1f);
            panelConfiguracao.Controls.Add(btnZoomMais);

            panelConfiguracao.AutoScrollMinSize = new Size(0, yPos + 55);
            AtualizarControleZoom();
            LayoutManager.ConvertConfigurationToAccordion(panelConfiguracao);
        }

        private static Button CriarBotaoZoom(string texto, Point localizacao)
        {
            var botao = new Button
            {
                Text = texto,
                Location = localizacao,
                Size = new Size(42, 32),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            botao.FlatAppearance.BorderSize = 0;
            return botao;
        }

        private Label CriarLabelSecao(string texto, int yPos)
        {
            return new Label
            {
                Text = texto,
                Location = new Point(10, yPos),
                Size = new Size(320, 20),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 73, 94)
            };
        }

        private int CriarCampoNumerico(string label, out NumericUpDown control, int yPos,
            decimal min, decimal max, decimal valor, decimal increment = 1)
        {
            Label lbl = new Label
            {
                Text = label,
                Location = new Point(15, yPos),
                Size = new Size(140, 20),
                Font = new Font("Segoe UI", 9)
            };
            panelConfiguracao.Controls.Add(lbl);

            control = new NumericUpDown
            {
                Location = new Point(160, yPos - 2),
                Size = new Size(100, 23),
                Minimum = min,
                Maximum = max,
                Value = valor,
                DecimalPlaces = increment < 1 ? 2 : 0,
                Increment = increment
            };
            panelConfiguracao.Controls.Add(control);

            Label lblUnidade = new Label
            {
                Text = "mm",
                Location = new Point(270, yPos),
                Size = new Size(30, 20),
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.Gray
            };
            panelConfiguracao.Controls.Add(lblUnidade);

            return yPos + 30;
        }

        #endregion

        #region Toolbox

        private void CriarToolbox()
        {
            Label lblTitulo = new Label
            {
                Text = "🧰 ELEMENTOS",
                Location = new Point(10, 10),
                Size = new Size(180, 25),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 73, 94)
            };
            panelToolbox.Controls.Add(lblTitulo);

            int yPos = 45;

            Label lblCampos = new Label
            {
                Text = "Campos Dinâmicos:",
                Location = new Point(10, yPos),
                Size = new Size(180, 20),
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = Color.Gray
            };
            panelToolbox.Controls.Add(lblCampos);
            yPos += 25;

            ComboBox cmbCampos = new ComboBox
            {
                Location = new Point(10, yPos),
                Size = new Size(180, 23),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 8)
            };
            cmbCampos.Items.AddRange(new object[] {
                "Mercadoria", "Descricao", "CodigoMercadoria", "Codigo", "CodFabricante", "Referencia", "CodBarras",
                "Preco", "PrecoCusto", "PrecoVenda", "VendaA", "VendaB", "VendaC", "VendaD", "VendaE",
                "Quantidade", "Fornecedor", "Fabricante", "Grupo", "SubGrupo", "Marca", "Observacao", "Prateleira", "Garantia",
                "Tam", "Cores", "CodBarras_Grade", "PrecoOriginal", "PrecoPromocional"
            });
            cmbCampos.SelectedIndexChanged += (s, e) => {
                if (cmbCampos.SelectedItem != null)
                {
                    AdicionarCampo(cmbCampos.SelectedItem.ToString());
                    cmbCampos.SelectedIndex = -1;
                }
            };
            panelToolbox.Controls.Add(cmbCampos);
            yPos += 35;
            

            if (ModuloAppHelper.EstaEmModuloDistribuidoraWeb())
            {
                int yPos2 = 45;
                yPos2 = CriarComboCamposDistribuidora("Nota Fiscal:", EtiquetaDistribuidoraResolver.CamposNotaFiscal, yPos2);
                yPos2 = CriarComboCamposDistribuidora("Destinatario:", EtiquetaDistribuidoraResolver.CamposDestinatario, yPos2);
                yPos2 = CriarComboCamposDistribuidora("Endereco:", EtiquetaDistribuidoraResolver.CamposEndereco, yPos2);
                yPos2 = CriarComboCamposDistribuidora("Empresa:", EtiquetaDistribuidoraResolver.CamposEmpresa, yPos2);
                yPos2 = CriarComboCamposDistribuidora("Volumes:", EtiquetaDistribuidoraResolver.CamposVolumes, yPos2);
            }

            Label lblCodigoBarras = new Label
            {
                Text = "Códigos de Barras:",
                Location = new Point(10, yPos),
                Size = new Size(180, 20),
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = Color.Gray
            };
            panelToolbox.Controls.Add(lblCodigoBarras);
            yPos += 25;

            ComboBox cmbCodigoBarras = new ComboBox
            {
                Location = new Point(10, yPos),
                Size = new Size(180, 23),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 8)
            };
            cmbCodigoBarras.Items.AddRange(new object[] {
                "CodigoMercadoria", "CodFabricante", "CodBarras", "CodBarras_Grade"
            });
            if (ModuloAppHelper.EstaEmModuloDistribuidoraWeb())
            {
                foreach (string campoCodigo in EtiquetaDistribuidoraResolver.ObterCamposCodigoBarras())
                    cmbCodigoBarras.Items.Add(campoCodigo);
            }
            cmbCodigoBarras.SelectedIndexChanged += (s, e) => {
                if (cmbCodigoBarras.SelectedItem != null)
                {
                    AdicionarCodigoBarras(cmbCodigoBarras.SelectedItem.ToString());
                    cmbCodigoBarras.SelectedIndex = -1;
                }
            };
            panelToolbox.Controls.Add(cmbCodigoBarras);
            yPos += 35;
            if (ModuloAppHelper.EstaEmModuloDistribuidoraWeb())
            {
                Button btnTexto = CriarBotaoElemento("📝 Texto", yPos, () => AdicionarElemento(TipoElemento.Texto));
                yPos += 56;

                Button btnExpressao = CriarBotaoElemento("∑ Expressão", yPos, () => AdicionarElemento(TipoElemento.Expressao));
                yPos += 56;

                Button btnImagem = CriarBotaoElemento("🖼️ Imagem", yPos, () => AdicionarImagem());
                yPos += 56;

                Button btnRemover = CriarBotaoElemento("🗑️ Remover", yPos, () => RemoverElementoSelecionado());
                ThemeManager.StyleToolCard(btnRemover, true);
                CriarPainelPropriedades();
            }
            else
            {
                {
                    int yPos2 = 48;
                    Button btnTexto = CriarBotaoElemento("📝 Texto", yPos2, () => AdicionarElemento(TipoElemento.Texto));
                    yPos2 += 56;

                    Button btnExpressao = CriarBotaoElemento("∑ Expressão", yPos2, () => AdicionarElemento(TipoElemento.Expressao));
                    yPos2 += 56;

                    Button btnImagem = CriarBotaoElemento("🖼️ Imagem", yPos2, () => AdicionarImagem());
                    yPos2 += 56;

                    Button btnRemover = CriarBotaoElemento("🗑️ Remover", yPos2, () => RemoverElementoSelecionado());
                    ThemeManager.StyleToolCard(btnRemover, true);
                    CriarPainelPropriedades();
                }

            }
        }

        private void AplicarLayoutToolboxResponsivo()
        {
            if (panelToolbox == null) return;

            bool modoDistribuidora = ModuloAppHelper.EstaEmModuloDistribuidoraWeb();
            if (toolboxDistribuidoraAtiva.HasValue
                && toolboxDistribuidoraAtiva.Value == modoDistribuidora)
                return;

            panelToolbox.SuspendLayout();
            try
            {
                if (toolboxDistribuidoraAtiva.HasValue)
                {
                    Control[] controlesAnteriores = panelToolbox.Controls.Cast<Control>().ToArray();
                    panelToolbox.Controls.Clear();
                    foreach (Control controle in controlesAnteriores)
                        controle.Dispose();

                    panelPropriedades = null;
                    CriarToolbox();
                }

                ToolboxLayoutManager.Apply(panelToolbox, panelPropriedades, modoDistribuidora);
                toolboxDistribuidoraAtiva = modoDistribuidora;

                if (elementoSelecionado != null || elementosSelecionados.Count > 0)
                    AtualizarPainelPropriedades();
            }
            finally
            {
                panelToolbox.ResumeLayout(true);
            }
        }

        private int CriarComboCamposDistribuidora(string titulo, IEnumerable<string> campos, int yPos)
        {

            Label lblGrupo = new Label
            {
                Text = titulo,
                Location = new Point(200, yPos),
                Size = new Size(180, 20),
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 73, 94)
            };
            panelToolbox.Controls.Add(lblGrupo);
            yPos += 22;                ;

            ComboBox cmbGrupo = new ComboBox
            {
                Location = new Point(200, yPos),
                Size = new Size(180, 23),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 8)
            };

            foreach (string campo in campos)
                cmbGrupo.Items.Add(campo);

            cmbGrupo.SelectedIndexChanged += (s, e) =>
            {
                if (cmbGrupo.SelectedItem == null)
                    return;

                AdicionarCampo(cmbGrupo.SelectedItem.ToString());
                cmbGrupo.SelectedIndex = -1;
            };

            panelToolbox.Controls.Add(cmbGrupo);
            return yPos + 35;
        }

        private void CriarPainelPropriedades()
        {

            panelPropriedades = new Panel
            {
                Location = new Point(1, 400),
                Size = new Size(380, 550),
                AutoScroll = true,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false
            };
            panelToolbox.Controls.Add(panelPropriedades);

            int yPos = 10;

            lblPropriedadesElemento = new Label
            {
                Text = "⚙ PROPRIEDADES",
                Location = new Point(90, yPos),
                Size = new Size(160, 25),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 73, 94)
            };
            panelPropriedades.Controls.Add(lblPropriedadesElemento);
            yPos += 35;

            Label lblNomeElemento = new Label
            {
                Name = "lblNomeElementoAtual",
                Text = "",
                Location = new Point(10, yPos),
                Size = new Size(350, 34),
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(230, 126, 34),
                BackColor = Color.FromArgb(255, 243, 224),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(3, 2, 3, 2),
                AutoEllipsis = true,
                Visible = false
            };
            panelPropriedades.Controls.Add(lblNomeElemento);
            yPos += 42;

            {
                //Linhas Propriedades
                int yPos2 = 82;
                Panel linha1 = new Panel
                {
                    Location = new Point(180, yPos2),
                    Size = new Size(2, 452),
                    //BackColor = Color.FromArgb(230, 126, 34)
                    BackColor = Color.FromArgb(0, 0, 0)

                };
                panelPropriedades.Controls.Add(linha1);
                yPos2 += 15;

                {
                    int yPos3 = 82;
                    Panel linha2 = new Panel
                    {
                        Location = new Point(3, yPos3),
                        Size = new Size(359, 2),
                        //BackColor = Color.FromArgb(230, 126, 34)
                        BackColor = Color.FromArgb(0, 0, 0)
                    };
                    panelPropriedades.Controls.Add(linha2);
                    yPos3 += 35;
                }

                {
                    int yPos4 = 533;
                    Panel linha2 = new Panel
                    {
                        Location = new Point(3, yPos4),
                        Size = new Size(359, 2),
                        //BackColor = Color.FromArgb(230, 126, 34)
                        BackColor = Color.FromArgb(0, 0, 0)
                    };
                    panelPropriedades.Controls.Add(linha2);
                    yPos4 += 35;
                }

                //Linhas Propriedades Laterais
                {
                    int yPos5 = 82;
                    Panel linha5 = new Panel
                    {
                        Location = new Point(360, yPos5),
                        Size = new Size(2, 452),
                        //BackColor = Color.FromArgb(230, 126, 34)
                        BackColor = Color.FromArgb(0, 0, 0)

                    };
                    panelPropriedades.Controls.Add(linha5);
                    yPos5 += 15;
                    {
                        int yPos6 = 82;
                        Panel linha6 = new Panel
                        {
                            Location = new Point(3, yPos6),
                            Size = new Size(2, 452),
                            //BackColor = Color.FromArgb(230, 126, 34)
                            BackColor = Color.FromArgb(0, 0, 0)

                        };
                        panelPropriedades.Controls.Add(linha6);
                        yPos6 += 15;
                    }

                }
            }


            Label lblDimensoesElemento = new Label
            {
                Text = "Dimensoes (mm):",
                Location = new Point(10, yPos),
                Size = new Size(160, 20),
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = Color.Gray
            };
            panelPropriedades.Controls.Add(lblDimensoesElemento);
            yPos += 25;

            numElementoLargura = new NumericUpDown
            {
                Location = new Point(10, yPos),
                Size = new Size(75, 23),
                Minimum = 1,
                Maximum = 500,
                Value = 1
            };
            numElementoLargura.ValueChanged += (s, e) => AlterarLarguraElementosSelecionados();
            panelPropriedades.Controls.Add(numElementoLargura);

            numElementoAltura = new NumericUpDown
            {
                Location = new Point(95, yPos),
                Size = new Size(75, 23),
                Minimum = 1,
                Maximum = 500,
                Value = 1
            };
            numElementoAltura.ValueChanged += (s, e) => AlterarAlturaElementosSelecionados();
            panelPropriedades.Controls.Add(numElementoAltura);
            yPos += 35;

            Label lblConteudo = new Label
            {
                Name = "lblConteudoTexto",
                Text = "Conteúdo:",
                Location = new Point(10, yPos),
                Size = new Size(160, 20),
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = Color.Gray,
                Visible = false
            };
            panelPropriedades.Controls.Add(lblConteudo);
            yPos += 25;

            TextBox txtConteudo = new TextBox
            {
                Name = "txtConteudoElemento",
                Location = new Point(10, yPos),
                Size = new Size(160, 25),
                Font = new Font("Segoe UI", 9),
                Visible = false
            };
            txtConteudo.TextChanged += (s, e) =>
            {
                if (!atualizandoPropriedades && elementoSelecionado != null && elementoSelecionado.Tipo == TipoElemento.Texto)
                {
                    elementoSelecionado.Conteudo = txtConteudo.Text;
                    SalvarEstadoHistorico();
                    pbCanvas.Invalidate();
                }
            };
            panelPropriedades.Controls.Add(txtConteudo);
            yPos += 35;

            lblCalculoPreco = new Label
            {
                Text = "Calculo de Preco:",
                Location = new Point(10, yPos),
                Size = new Size(160, 20),
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = Color.Gray,
                Visible = false
            };
            //panelPropriedades.Controls.Add(lblCalculoPreco);
            //yPos += 25;

            cmbOperadorCalculoPreco = new ComboBox
            {
                Location = new Point(10, yPos),
                Size = new Size(70, 23),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9),
                Visible = false
            };
            cmbOperadorCalculoPreco.Items.AddRange(new object[] { "Nenhum", "+", "-", "*", "/" });
            cmbOperadorCalculoPreco.SelectedIndexChanged += (s, e) => AlterarCalculoPreco();
            //panelPropriedades.Controls.Add(cmbOperadorCalculoPreco);

            numValorCalculoPreco = new NumericUpDown
            {
                Location = new Point(90, yPos),
                Size = new Size(80, 23),
                Minimum = 0m,
                Maximum = 999999m,
                DecimalPlaces = 4,
                Increment = 0.01m,
                Value = 0m,
                Visible = false
            };
            numValorCalculoPreco.ValueChanged += (s, e) => AlterarCalculoPreco();
            //panelPropriedades.Controls.Add(numValorCalculoPreco);
            //yPos += 35;

            lblExpressaoFormula = new Label
            {
                Text = "Expressão:",
                Location = new Point(10, yPos),
                Size = new Size(160, 20),
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = Color.Gray,
                Visible = false
            };
            panelPropriedades.Controls.Add(lblExpressaoFormula);
            yPos += 25;

            txtExpressaoFormula = new TextBox
            {
                Location = new Point(10, yPos),
                Size = new Size(160, 25),
                Font = new Font("Segoe UI", 9),
                Visible = false,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.CustomSource
            };
            txtExpressaoFormula.AutoCompleteCustomSource.AddRange(CampoEtiquetaResolver.ObterCamposDisponiveis().ToArray());
            if (ModuloAppHelper.EstaEmModuloDistribuidoraWeb())
                txtExpressaoFormula.AutoCompleteCustomSource.AddRange(EtiquetaDistribuidoraResolver.ObterNovosCamposExpressao());
            txtExpressaoFormula.TextChanged += (s, e) => AlterarExpressao();
            txtExpressaoFormula.Leave += (s, e) => ValidarExpressaoSelecionada(true);
            panelPropriedades.Controls.Add(txtExpressaoFormula);
            yPos += 32;

            btnInserirCampoExpressao = new Button
            {
                Text = "Inserir Campo",
                Location = new Point(10, yPos),
                Size = new Size(110, 28),
                Font = new Font("Segoe UI", 8),
                BackColor = Color.FromArgb(236, 240, 241),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Visible = false
            };
            btnInserirCampoExpressao.Click += (s, e) => AbrirMenuCamposExpressao();
            panelPropriedades.Controls.Add(btnInserirCampoExpressao);

            btnEditorExpressao = new Button
            {
                Text = "...",
                Location = new Point(130, yPos),
                Size = new Size(40, 28),
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                BackColor = Color.FromArgb(236, 240, 241),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Visible = false
            };
            btnEditorExpressao.Click += (s, e) => AbrirEditorExpressao();
            panelPropriedades.Controls.Add(btnEditorExpressao);
            yPos += 38;


            //Reposicionamento
            {
                int yPos2 = 85;
                Label lblAlinhamento = new Label
                {

                    Text = "Alinhamento:",
                    Location = new Point(190, yPos2),
                    Size = new Size(160, 20),
                    Font = new Font("Segoe UI", 8, FontStyle.Bold),
                    ForeColor = Color.Gray
                };
                panelPropriedades.Controls.Add(lblAlinhamento);
                yPos2 += 25;

                btnAlinharEsquerda = new Button
                {
                    Text = "←",
                    Location = new Point(190, yPos2),
                    Size = new Size(50, 35),
                    Font = new Font("Segoe UI", 12),
                    BackColor = Color.FromArgb(236, 240, 241),
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand
                };
                btnAlinharEsquerda.FlatAppearance.BorderColor = Color.FromArgb(189, 195, 199);
                btnAlinharEsquerda.Click += (s, e) => AlterarAlinhamento(StringAlignment.Near);
                panelPropriedades.Controls.Add(btnAlinharEsquerda);

                btnAlinharCentro = new Button
                {
                    Text = "←→",
                    Location = new Point(245, yPos2),
                    Size = new Size(50, 35),
                    Font = new Font("Segoe UI", 12),
                    BackColor = Color.FromArgb(236, 240, 241),
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand
                };
                btnAlinharCentro.FlatAppearance.BorderColor = Color.FromArgb(189, 195, 199);
                btnAlinharCentro.Click += (s, e) => AlterarAlinhamento(StringAlignment.Center);
                panelPropriedades.Controls.Add(btnAlinharCentro);

                btnAlinharDireita = new Button
                {
                    Text = "→",
                    Location = new Point(300, yPos2),
                    Size = new Size(50, 35),
                    Font = new Font("Segoe UI", 12),
                    BackColor = Color.FromArgb(236, 240, 241),
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand
                };
                btnAlinharDireita.FlatAppearance.BorderColor = Color.FromArgb(189, 195, 199);
                btnAlinharDireita.Click += (s, e) => AlterarAlinhamento(StringAlignment.Far);
                panelPropriedades.Controls.Add(btnAlinharDireita);
                yPos2 += 45;


                Label lblFamilia = new Label
                {
                    Text = "Família da Fonte:",
                    Location = new Point(190, yPos2),
                    Size = new Size(160, 20),
                    Font = new Font("Segoe UI", 8, FontStyle.Bold),
                    ForeColor = Color.Gray
                };
                panelPropriedades.Controls.Add(lblFamilia);
                yPos2 += 25;


                cmbFonte = new ComboBox
                {
                    Name = "cmbFonte",
                    Location = new Point(190, yPos2),
                    Size = new Size(160, 23),
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Font = new Font("Segoe UI", 9)
                };
                foreach (var fontFamily in FontFamily.Families)
                {
                    cmbFonte.Items.Add(fontFamily.Name);
                }
                cmbFonte.SelectedIndexChanged += CmbFonte_SelectedIndexChanged;
                panelPropriedades.Controls.Add(cmbFonte);
                yPos2 += 35;

                Label lblFonte = new Label
                {
                    Text = "Tamanho da Fonte:",
                    Location = new Point(190, yPos2),
                    Size = new Size(160, 20),
                    Font = new Font("Segoe UI", 8, FontStyle.Bold),
                    ForeColor = Color.Gray
                };
                panelPropriedades.Controls.Add(lblFonte);
                yPos2 += 25;

                numTamanhoFonte = new NumericUpDown
                {
                    Location = new Point(190, yPos2),
                    Size = new Size(70, 23),
                    Minimum = 3,
                    Maximum = 72,
                    Value = 10
                };
                numTamanhoFonte.ValueChanged += (s, e) => AlterarTamanhoFonte();
                panelPropriedades.Controls.Add(numTamanhoFonte);
                yPos2 += 35;

                Label lblEstilo = new Label
                {
                    Text = "Estilo:",
                    Location = new Point(190, yPos2),
                    Size = new Size(160, 20),
                    Font = new Font("Segoe UI", 8, FontStyle.Bold),
                    ForeColor = Color.Gray
                };
                panelPropriedades.Controls.Add(lblEstilo);
                yPos2 += 25;

                chkNegrito = new CheckBox
                {
                    Text = "Negrito",
                    Location = new Point(190, yPos2),
                    Size = new Size(80, 20),
                    Font = new Font("Segoe UI", 8, FontStyle.Bold)
                };
                chkNegrito.CheckedChanged += (s, e) => AlterarEstiloFonte();
                panelPropriedades.Controls.Add(chkNegrito);

                chkItalico = new CheckBox
                {
                    Text = "Itálico",
                    Location = new Point(275, yPos2),
                    Size = new Size(75, 20),
                    Font = new Font("Segoe UI", 8, FontStyle.Italic)
                };
                chkItalico.CheckedChanged += (s, e) => AlterarEstiloFonte();
                panelPropriedades.Controls.Add(chkItalico);
                yPos2 += 35;
            }
            Label lblCor = new Label
            {
                Text = "Cor do Texto:",
                Location = new Point(10, yPos),
                Size = new Size(160, 20),
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = Color.Gray
            };
            panelPropriedades.Controls.Add(lblCor);
            yPos += 25;

            btnCor = new Button
            {
                Text = "Escolher Cor",
                Location = new Point(10, yPos),
                Size = new Size(160, 30),
                BackColor = Color.Black,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCor.Click += BtnCor_Click;
            panelPropriedades.Controls.Add(btnCor);
            yPos += 35;

            Label lblCoresRapidas = new Label
            {
                Text = "Atalhos:",
                Location = new Point(10, yPos),
                Size = new Size(70, 20),
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = Color.Gray
            };
            panelPropriedades.Controls.Add(lblCoresRapidas);

            Button btnTextoPreto = new Button
            {
                Text = "T▓",
                Location = new Point(85, yPos),
                Size = new Size(40, 25),
                BackColor = Color.White,
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            btnTextoPreto.Click += (s, e) => AplicarCorTexto(Color.Black);
            panelPropriedades.Controls.Add(btnTextoPreto);

            Button btnTextoBranco = new Button
            {
                Text = "T▓",
                Location = new Point(130, yPos),
                Size = new Size(40, 25),
                BackColor = Color.Black,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            btnTextoBranco.Click += (s, e) => AplicarCorTexto(Color.White);
            panelPropriedades.Controls.Add(btnTextoBranco);
            yPos += 35;

            Label lblCorFundo = new Label
            {
                Text = "Cor de Fundo:",
                Location = new Point(10, yPos),
                Size = new Size(160, 20),
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = Color.Gray
            };
            panelPropriedades.Controls.Add(lblCorFundo);
            yPos += 25;

            btnCorFundo = new Button
            {
                Text = "Escolher Fundo",
                Location = new Point(10, yPos),
                Size = new Size(160, 30),
                BackColor = Color.Transparent,
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCorFundo.Click += BtnCorFundo_Click;
            panelPropriedades.Controls.Add(btnCorFundo);
            yPos += 35;

            Label lblFundoRapido = new Label
            {
                Text = "Atalhos:",
                Location = new Point(10, yPos),
                Size = new Size(50, 20),
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = Color.Gray
            };
            panelPropriedades.Controls.Add(lblFundoRapido);

            btnFundoPreto = new Button
            {
                Location = new Point(65, yPos),
                Size = new Size(30, 25),
                BackColor = Color.Black,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            btnFundoPreto.Click += (s, e) => AplicarCorFundo(Color.Black);
            panelPropriedades.Controls.Add(btnFundoPreto);

            btnFundoBranco = new Button
            {
                Location = new Point(100, yPos),
                Size = new Size(30, 25),
                BackColor = Color.White,
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            btnFundoBranco.Click += (s, e) => AplicarCorFundo(Color.White);
            panelPropriedades.Controls.Add(btnFundoBranco);

            btnFundoTransparente = new Button
            {
                Text = "Ø",
                Location = new Point(135, yPos),
                Size = new Size(30, 25),
                BackColor = Color.LightGray,
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            btnFundoTransparente.Click += (s, e) => AplicarCorFundo(null);
            panelPropriedades.Controls.Add(btnFundoTransparente);
            yPos += 35;

            //Novo Posicionamento
            {
                int yPos2 = 325;
                Label lblBorda = new Label
                {
                    Text = "Borda:",
                    Location = new Point(190, yPos2),
                    Size = new Size(160, 20),
                    Font = new Font("Segoe UI", 8, FontStyle.Bold),
                    ForeColor = Color.Gray
                };
                panelPropriedades.Controls.Add(lblBorda);
                yPos2 += 25;

                cmbBordaElemento = new ComboBox
                {
                    Name = "cmbBordaElemento",
                    Location = new Point(190, yPos2),
                    Size = new Size(160, 23),
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Font = new Font("Segoe UI", 9)
                };
                cmbBordaElemento.Items.AddRange(new object[] { "Transparente", "Sólida Preta" });
                cmbBordaElemento.SelectedIndexChanged += (s, e) => AlterarBordaElementosSelecionados();
                panelPropriedades.Controls.Add(cmbBordaElemento);
                yPos2 += 35;

                Label lblEspessuraBorda = new Label
                {
                    Text = "Espessura da Borda:",
                    Location = new Point(190, yPos2),
                    Size = new Size(160, 20),
                    Font = new Font("Segoe UI", 8, FontStyle.Bold),
                    ForeColor = Color.Gray
                };
                panelPropriedades.Controls.Add(lblEspessuraBorda);
                yPos2 += 25;

                numEspessuraBorda = new NumericUpDown
                {
                    Location = new Point(190, yPos2),
                    Size = new Size(70, 23),
                    Minimum = 0.1m,
                    Maximum = 20,
                    DecimalPlaces = 1,
                    Increment = 0.1m,
                    Value = 1,
                    Enabled = false
                };
                numEspessuraBorda.ValueChanged += (s, e) => AlterarEspessuraBordaElementosSelecionados();
                panelPropriedades.Controls.Add(numEspessuraBorda);

            }

            lblSimbologiaCodigoBarras = new Label
            {
                Name = "lblSimbologiaCodigoBarras",
                Text = "Tipo do Código:",
                Location = new Point(10, 470),
                Size = new Size(160, 20),
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = Color.Gray,
                Visible = false
            };
            panelPropriedades.Controls.Add(lblSimbologiaCodigoBarras);

            cmbSimbologiaCodigoBarras = new ComboBox
            {
                Name = "cmbSimbologiaCodigoBarras",
                Location = new Point(190, 470),
                Size = new Size(120, 23),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 8),
                Visible = false
            };
            cmbSimbologiaCodigoBarras.Items.AddRange(new object[] { "Code128", "EAN-13" });
            cmbSimbologiaCodigoBarras.SelectedIndexChanged +=
                (s, e) => AlterarSimbologiaCodigoBarras();
            panelPropriedades.Controls.Add(cmbSimbologiaCodigoBarras);

            lblExibirNumeracaoCodigoBarras = new Label
            {
                Name = "lblExibirNumeracaoCodigoBarras",
                Text = "Exibir Numeração:",
                Location = new Point(10, 495),
                Size = new Size(160, 20),
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = Color.Gray,
                Visible = false
            };
            panelPropriedades.Controls.Add(lblExibirNumeracaoCodigoBarras);

            chkExibirNumeracaoCodigoBarras = new CheckBox
            {
                Name = "chkExibirNumeracaoCodigoBarras",
                Text = "Sim",
                Location = new Point(190, 495),
                Size = new Size(80, 22),
                Font = new Font("Segoe UI", 8),
                Checked = true,
                Visible = false
            };
            chkExibirNumeracaoCodigoBarras.CheckedChanged +=
                (s, e) => AlterarExibicaoNumeracaoCodigoBarras();
            panelPropriedades.Controls.Add(chkExibirNumeracaoCodigoBarras);

            lblNumeracaoAgrupada = new Label
            {
                Name = "lblNumeracaoAgrupada",
                Text = "Numeração Desagrupada:",
                Location = new Point(10, 500),
                Size = new Size(160, 20),
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = Color.Gray,
                Visible = false
            };
            panelPropriedades.Controls.Add(lblNumeracaoAgrupada);

            chkNumeracaoAgrupada = new CheckBox
            {
                Name = "chkNumeracaoAgrupada",
                Text = "Sim",
                Location = new Point(190, 500),
                Size = new Size(80, 22),
                Font = new Font("Segoe UI", 8),
                Visible = false
            };
            chkNumeracaoAgrupada.CheckedChanged += (s, e) => AlterarNumeracaoAgrupada();
            panelPropriedades.Controls.Add(chkNumeracaoAgrupada);

            lblRenderizacaoLinear1D = new Label
            {
                Name = "lblRenderizacaoLinear1D",
                Text = "Renderização:",
                Location = new Point(10, 530),
                Size = new Size(160, 20),
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = Color.Gray,
                Visible = false
            };
            panelPropriedades.Controls.Add(lblRenderizacaoLinear1D);

            chkRenderizacaoLinear1D = new CheckBox
            {
                Name = "chkRenderizacaoLinear1D",
                Text = "Linear (1D)",
                Location = new Point(190, 530),
                Size = new Size(100, 22),
                Font = new Font("Segoe UI", 8),
                Visible = false
            };
            chkRenderizacaoLinear1D.CheckedChanged += (s, e) => AlterarRenderizacaoLinear1D();
            panelPropriedades.Controls.Add(chkRenderizacaoLinear1D);

            PropertyPanelLayoutManager.Configure(panelPropriedades);
        }
        //    else 
        //    { 
        //        panelPropriedades = new Panel
        //        {
        //            Location = new Point(1, 400),
        //            Size = new Size(210, 600),
        //            AutoScroll = true,
        //            BackColor = Color.White,
        //            BorderStyle = BorderStyle.FixedSingle,
        //            Visible = false
        //        };
        //    panelToolbox.Controls.Add(panelPropriedades);

        //    int yPos = 10;

        //    lblPropriedadesElemento = new Label
        //    {
        //        Text = "⚙ PROPRIEDADES",
        //        Location = new Point(10, yPos),
        //        Size = new Size(160, 25),
        //        Font = new Font("Segoe UI", 10, FontStyle.Bold),
        //        ForeColor = Color.FromArgb(52, 73, 94)
        //    };
        //    panelPropriedades.Controls.Add(lblPropriedadesElemento);
        //    yPos += 35;

        //    Label lblNomeElemento = new Label
        //    {
        //        Name = "lblNomeElementoAtual",
        //        Text = "",
        //        Location = new Point(10, yPos),
        //        Size = new Size(160, 34),
        //        Font = new Font("Segoe UI", 8),
        //        ForeColor = Color.FromArgb(230, 126, 34),
        //        BackColor = Color.FromArgb(255, 243, 224),
        //        BorderStyle = BorderStyle.FixedSingle,
        //        Padding = new Padding(3, 2, 3, 2),
        //        AutoEllipsis = true,
        //        Visible = false
        //    };
        //    panelPropriedades.Controls.Add(lblNomeElemento);
        //    yPos += 42;

        //    Label lblDimensoesElemento = new Label
        //    {
        //        Text = "Dimensoes (mm):",
        //        Location = new Point(10, yPos),
        //        Size = new Size(160, 20),
        //        Font = new Font("Segoe UI", 8, FontStyle.Bold),
        //        ForeColor = Color.Gray
        //    };
        //    panelPropriedades.Controls.Add(lblDimensoesElemento);
        //    yPos += 25;

        //    numElementoLargura = new NumericUpDown
        //    {
        //        Location = new Point(10, yPos),
        //        Size = new Size(75, 23),
        //        Minimum = 1,
        //        Maximum = 500,
        //        Value = 1
        //    };
        //    numElementoLargura.ValueChanged += (s, e) => AlterarLarguraElementosSelecionados();
        //    panelPropriedades.Controls.Add(numElementoLargura);

        //    numElementoAltura = new NumericUpDown
        //    {
        //        Location = new Point(95, yPos),
        //        Size = new Size(75, 23),
        //        Minimum = 1,
        //        Maximum = 500,
        //        Value = 1
        //    };
        //    numElementoAltura.ValueChanged += (s, e) => AlterarAlturaElementosSelecionados();
        //    panelPropriedades.Controls.Add(numElementoAltura);
        //    yPos += 35;

        //    Label lblConteudo = new Label
        //    {
        //        Name = "lblConteudoTexto",
        //        Text = "Conteúdo:",
        //        Location = new Point(10, yPos),
        //        Size = new Size(160, 20),
        //        Font = new Font("Segoe UI", 8, FontStyle.Bold),
        //        ForeColor = Color.Gray,
        //        Visible = false
        //    };
        //    panelPropriedades.Controls.Add(lblConteudo);
        //    yPos += 25;

        //    TextBox txtConteudo = new TextBox
        //    {
        //        Name = "txtConteudoElemento",
        //        Location = new Point(10, yPos),
        //        Size = new Size(160, 25),
        //        Font = new Font("Segoe UI", 9),
        //        Visible = false
        //    };
        //    txtConteudo.TextChanged += (s, e) =>
        //    {
        //        if (!atualizandoPropriedades && elementoSelecionado != null && elementoSelecionado.Tipo == TipoElemento.Texto)
        //        {
        //            elementoSelecionado.Conteudo = txtConteudo.Text;
        //            SalvarEstadoHistorico();
        //            pbCanvas.Invalidate();
        //        }
        //    };
        //    panelPropriedades.Controls.Add(txtConteudo);
        //    yPos += 35;

        //    lblCalculoPreco = new Label
        //    {
        //        Text = "Calculo de Preco:",
        //        Location = new Point(10, yPos),
        //        Size = new Size(160, 20),
        //        Font = new Font("Segoe UI", 8, FontStyle.Bold),
        //        ForeColor = Color.Gray,
        //        Visible = false
        //    };
        //    //panelPropriedades.Controls.Add(lblCalculoPreco);
        //    //yPos += 25;

        //    cmbOperadorCalculoPreco = new ComboBox
        //    {
        //        Location = new Point(10, yPos),
        //        Size = new Size(70, 23),
        //        DropDownStyle = ComboBoxStyle.DropDownList,
        //        Font = new Font("Segoe UI", 9),
        //        Visible = false
        //    };
        //    cmbOperadorCalculoPreco.Items.AddRange(new object[] { "Nenhum", "+", "-", "*", "/" });
        //    cmbOperadorCalculoPreco.SelectedIndexChanged += (s, e) => AlterarCalculoPreco();
        //    //panelPropriedades.Controls.Add(cmbOperadorCalculoPreco);

        //    numValorCalculoPreco = new NumericUpDown
        //    {
        //        Location = new Point(90, yPos),
        //        Size = new Size(80, 23),
        //        Minimum = 0m,
        //        Maximum = 999999m,
        //        DecimalPlaces = 4,
        //        Increment = 0.01m,
        //        Value = 0m,
        //        Visible = false
        //    };
        //    numValorCalculoPreco.ValueChanged += (s, e) => AlterarCalculoPreco();
        //    //panelPropriedades.Controls.Add(numValorCalculoPreco);
        //    //yPos += 35;

        //    lblExpressaoFormula = new Label
        //    {
        //        Text = "Expressão:",
        //        Location = new Point(10, yPos),
        //        Size = new Size(160, 20),
        //        Font = new Font("Segoe UI", 8, FontStyle.Bold),
        //        ForeColor = Color.Gray,
        //        Visible = false
        //    };
        //    panelPropriedades.Controls.Add(lblExpressaoFormula);
        //    yPos += 25;

        //    txtExpressaoFormula = new TextBox
        //    {
        //        Location = new Point(10, yPos),
        //        Size = new Size(160, 25),
        //        Font = new Font("Segoe UI", 9),
        //        Visible = false,
        //        AutoCompleteMode = AutoCompleteMode.SuggestAppend,
        //        AutoCompleteSource = AutoCompleteSource.CustomSource
        //    };
        //    txtExpressaoFormula.AutoCompleteCustomSource.AddRange(CampoEtiquetaResolver.ObterCamposDisponiveis().ToArray());
        //    txtExpressaoFormula.TextChanged += (s, e) => AlterarExpressao();
        //    txtExpressaoFormula.Leave += (s, e) => ValidarExpressaoSelecionada(true);
        //    panelPropriedades.Controls.Add(txtExpressaoFormula);
        //    yPos += 32;

        //    btnInserirCampoExpressao = new Button
        //    {
        //        Text = "Inserir Campo",
        //        Location = new Point(10, yPos),
        //        Size = new Size(110, 28),
        //        Font = new Font("Segoe UI", 8),
        //        BackColor = Color.FromArgb(236, 240, 241),
        //        FlatStyle = FlatStyle.Flat,
        //        Cursor = Cursors.Hand,
        //        Visible = false
        //    };
        //    btnInserirCampoExpressao.Click += (s, e) => AbrirMenuCamposExpressao();
        //    panelPropriedades.Controls.Add(btnInserirCampoExpressao);

        //    btnEditorExpressao = new Button
        //    {
        //        Text = "...",
        //        Location = new Point(130, yPos),
        //        Size = new Size(40, 28),
        //        Font = new Font("Segoe UI", 8, FontStyle.Bold),
        //        BackColor = Color.FromArgb(236, 240, 241),
        //        FlatStyle = FlatStyle.Flat,
        //        Cursor = Cursors.Hand,
        //        Visible = false
        //    };
        //    btnEditorExpressao.Click += (s, e) => AbrirEditorExpressao();
        //    panelPropriedades.Controls.Add(btnEditorExpressao);
        //    yPos += 38;

        //    Label lblAlinhamento = new Label
        //    {
        //        Text = "Alinhamento:",
        //        Location = new Point(10, yPos),
        //        Size = new Size(160, 20),
        //        Font = new Font("Segoe UI", 8, FontStyle.Bold),
        //        ForeColor = Color.Gray
        //    };
        //    panelPropriedades.Controls.Add(lblAlinhamento);
        //    yPos += 25;

        //    btnAlinharEsquerda = new Button
        //    {
        //        Text = "←",
        //        Location = new Point(10, yPos),
        //        Size = new Size(50, 35),
        //        Font = new Font("Segoe UI", 12),
        //        BackColor = Color.FromArgb(236, 240, 241),
        //        FlatStyle = FlatStyle.Flat,
        //        Cursor = Cursors.Hand
        //    };
        //    btnAlinharEsquerda.FlatAppearance.BorderColor = Color.FromArgb(189, 195, 199);
        //    btnAlinharEsquerda.Click += (s, e) => AlterarAlinhamento(StringAlignment.Near);
        //    panelPropriedades.Controls.Add(btnAlinharEsquerda);

        //    btnAlinharCentro = new Button
        //    {
        //        Text = "←→",
        //        Location = new Point(65, yPos),
        //        Size = new Size(50, 35),
        //        Font = new Font("Segoe UI", 12),
        //        BackColor = Color.FromArgb(236, 240, 241),
        //        FlatStyle = FlatStyle.Flat,
        //        Cursor = Cursors.Hand
        //    };
        //    btnAlinharCentro.FlatAppearance.BorderColor = Color.FromArgb(189, 195, 199);
        //    btnAlinharCentro.Click += (s, e) => AlterarAlinhamento(StringAlignment.Center);
        //    panelPropriedades.Controls.Add(btnAlinharCentro);

        //    btnAlinharDireita = new Button
        //    {
        //        Text = "→",
        //        Location = new Point(120, yPos),
        //        Size = new Size(50, 35),
        //        Font = new Font("Segoe UI", 12),
        //        BackColor = Color.FromArgb(236, 240, 241),
        //        FlatStyle = FlatStyle.Flat,
        //        Cursor = Cursors.Hand
        //    };
        //    btnAlinharDireita.FlatAppearance.BorderColor = Color.FromArgb(189, 195, 199);
        //    btnAlinharDireita.Click += (s, e) => AlterarAlinhamento(StringAlignment.Far);
        //    panelPropriedades.Controls.Add(btnAlinharDireita);
        //    yPos += 45;

        //    Label lblFamilia = new Label
        //    {
        //        Text = "Família da Fonte:",
        //        Location = new Point(10, yPos),
        //        Size = new Size(160, 20),
        //        Font = new Font("Segoe UI", 8, FontStyle.Bold),
        //        ForeColor = Color.Gray
        //    };
        //    panelPropriedades.Controls.Add(lblFamilia);
        //    yPos += 25;

        //    cmbFonte = new ComboBox
        //    {
        //        Location = new Point(10, yPos),
        //        Size = new Size(160, 23),
        //        DropDownStyle = ComboBoxStyle.DropDownList,
        //        Font = new Font("Segoe UI", 9)
        //    };
        //    foreach (var fontFamily in FontFamily.Families)
        //    {
        //        cmbFonte.Items.Add(fontFamily.Name);
        //    }
        //    cmbFonte.SelectedIndexChanged += CmbFonte_SelectedIndexChanged;
        //    panelPropriedades.Controls.Add(cmbFonte);
        //    yPos += 35;

        //    Label lblFonte = new Label
        //    {
        //        Text = "Tamanho da Fonte:",
        //        Location = new Point(10, yPos),
        //        Size = new Size(160, 20),
        //        Font = new Font("Segoe UI", 8, FontStyle.Bold),
        //        ForeColor = Color.Gray
        //    };
        //    panelPropriedades.Controls.Add(lblFonte);
        //    yPos += 25;

        //    numTamanhoFonte = new NumericUpDown
        //    {
        //        Location = new Point(10, yPos),
        //        Size = new Size(70, 23),
        //        Minimum = 3,
        //        Maximum = 72,
        //        Value = 10
        //    };
        //    numTamanhoFonte.ValueChanged += (s, e) => AlterarTamanhoFonte();
        //    panelPropriedades.Controls.Add(numTamanhoFonte);
        //    yPos += 35;

        //    Label lblEstilo = new Label
        //    {
        //        Text = "Estilo:",
        //        Location = new Point(10, yPos),
        //        Size = new Size(160, 20),
        //        Font = new Font("Segoe UI", 8, FontStyle.Bold),
        //        ForeColor = Color.Gray
        //    };
        //    panelPropriedades.Controls.Add(lblEstilo);
        //    yPos += 25;

        //    chkNegrito = new CheckBox
        //    {
        //        Text = "Negrito",
        //        Location = new Point(10, yPos),
        //        Size = new Size(80, 20),
        //        Font = new Font("Segoe UI", 8, FontStyle.Bold)
        //    };
        //    chkNegrito.CheckedChanged += (s, e) => AlterarEstiloFonte();
        //    panelPropriedades.Controls.Add(chkNegrito);

        //    chkItalico = new CheckBox
        //    {
        //        Text = "Itálico",
        //        Location = new Point(95, yPos),
        //        Size = new Size(75, 20),
        //        Font = new Font("Segoe UI", 8, FontStyle.Italic)
        //    };
        //    chkItalico.CheckedChanged += (s, e) => AlterarEstiloFonte();
        //    panelPropriedades.Controls.Add(chkItalico);
        //    yPos += 35;

        //    Label lblCor = new Label
        //    {
        //        Text = "Cor do Texto:",
        //        Location = new Point(10, yPos),
        //        Size = new Size(160, 20),
        //        Font = new Font("Segoe UI", 8, FontStyle.Bold),
        //        ForeColor = Color.Gray
        //    };
        //    panelPropriedades.Controls.Add(lblCor);
        //    yPos += 25;

        //    btnCor = new Button
        //    {
        //        Text = "Escolher Cor",
        //        Location = new Point(10, yPos),
        //        Size = new Size(160, 30),
        //        BackColor = Color.Black,
        //        ForeColor = Color.White,
        //        FlatStyle = FlatStyle.Flat,
        //        Cursor = Cursors.Hand
        //    };
        //    btnCor.Click += BtnCor_Click;
        //    panelPropriedades.Controls.Add(btnCor);
        //    yPos += 35;

        //    Label lblCoresRapidas = new Label
        //    {
        //        Text = "Atalhos:",
        //        Location = new Point(10, yPos),
        //        Size = new Size(70, 20),
        //        Font = new Font("Segoe UI", 8, FontStyle.Bold),
        //        ForeColor = Color.Gray
        //    };
        //    panelPropriedades.Controls.Add(lblCoresRapidas);

        //    Button btnTextoPreto = new Button
        //    {
        //        Text = "T▓",
        //        Location = new Point(85, yPos),
        //        Size = new Size(40, 25),
        //        BackColor = Color.White,
        //        ForeColor = Color.Black,
        //        FlatStyle = FlatStyle.Flat,
        //        Cursor = Cursors.Hand,
        //        Font = new Font("Segoe UI", 9, FontStyle.Bold)
        //    };
        //    btnTextoPreto.Click += (s, e) => AplicarCorTexto(Color.Black);
        //    panelPropriedades.Controls.Add(btnTextoPreto);

        //    Button btnTextoBranco = new Button
        //    {
        //        Text = "T▓",
        //        Location = new Point(130, yPos),
        //        Size = new Size(40, 25),
        //        BackColor = Color.Black,
        //        ForeColor = Color.White,
        //        FlatStyle = FlatStyle.Flat,
        //        Cursor = Cursors.Hand,
        //        Font = new Font("Segoe UI", 9, FontStyle.Bold)
        //    };
        //    btnTextoBranco.Click += (s, e) => AplicarCorTexto(Color.White);
        //    panelPropriedades.Controls.Add(btnTextoBranco);
        //    yPos += 35;

        //    Label lblCorFundo = new Label
        //    {
        //        Text = "Cor de Fundo:",
        //        Location = new Point(10, yPos),
        //        Size = new Size(160, 20),
        //        Font = new Font("Segoe UI", 8, FontStyle.Bold),
        //        ForeColor = Color.Gray
        //    };
        //    panelPropriedades.Controls.Add(lblCorFundo);
        //    yPos += 25;

        //    btnCorFundo = new Button
        //    {
        //        Text = "Escolher Fundo",
        //        Location = new Point(10, yPos),
        //        Size = new Size(160, 30),
        //        BackColor = Color.Transparent,
        //        ForeColor = Color.Black,
        //        FlatStyle = FlatStyle.Flat,
        //        Cursor = Cursors.Hand
        //    };
        //    btnCorFundo.Click += BtnCorFundo_Click;
        //    panelPropriedades.Controls.Add(btnCorFundo);
        //    yPos += 35;

        //    Label lblFundoRapido = new Label
        //    {
        //        Text = "Atalhos:",
        //        Location = new Point(10, yPos),
        //        Size = new Size(50, 20),
        //        Font = new Font("Segoe UI", 8, FontStyle.Bold),
        //        ForeColor = Color.Gray
        //    };
        //    panelPropriedades.Controls.Add(lblFundoRapido);

        //    btnFundoPreto = new Button
        //    {
        //        Location = new Point(65, yPos),
        //        Size = new Size(30, 25),
        //        BackColor = Color.Black,
        //        ForeColor = Color.White,
        //        FlatStyle = FlatStyle.Flat,
        //        Cursor = Cursors.Hand,
        //        Font = new Font("Segoe UI", 10, FontStyle.Bold)
        //    };
        //    btnFundoPreto.Click += (s, e) => AplicarCorFundo(Color.Black);
        //    panelPropriedades.Controls.Add(btnFundoPreto);

        //    btnFundoBranco = new Button
        //    {
        //        Location = new Point(100, yPos),
        //        Size = new Size(30, 25),
        //        BackColor = Color.White,
        //        ForeColor = Color.Black,
        //        FlatStyle = FlatStyle.Flat,
        //        Cursor = Cursors.Hand,
        //        Font = new Font("Segoe UI", 10, FontStyle.Bold)
        //    };
        //    btnFundoBranco.Click += (s, e) => AplicarCorFundo(Color.White);
        //    panelPropriedades.Controls.Add(btnFundoBranco);

        //    btnFundoTransparente = new Button
        //    {
        //        Text = "Ø",
        //        Location = new Point(135, yPos),
        //        Size = new Size(30, 25),
        //        BackColor = Color.LightGray,
        //        ForeColor = Color.Black,
        //        FlatStyle = FlatStyle.Flat,
        //        Cursor = Cursors.Hand,
        //        Font = new Font("Segoe UI", 10, FontStyle.Bold)
        //    };
        //    btnFundoTransparente.Click += (s, e) => AplicarCorFundo(null);
        //    panelPropriedades.Controls.Add(btnFundoTransparente);
        //    yPos += 35;

        //    Label lblBorda = new Label
        //    {
        //        Text = "Borda:",
        //        Location = new Point(10, yPos),
        //        Size = new Size(160, 20),
        //        Font = new Font("Segoe UI", 8, FontStyle.Bold),
        //        ForeColor = Color.Gray
        //    };
        //    panelPropriedades.Controls.Add(lblBorda);
        //    yPos += 25;

        //    cmbBordaElemento = new ComboBox
        //    {
        //        Location = new Point(10, yPos),
        //        Size = new Size(160, 23),
        //        DropDownStyle = ComboBoxStyle.DropDownList,
        //        Font = new Font("Segoe UI", 9)
        //    };
        //    cmbBordaElemento.Items.AddRange(new object[] { "Transparente", "Sólida Preta" });
        //    cmbBordaElemento.SelectedIndexChanged += (s, e) => AlterarBordaElementosSelecionados();
        //    panelPropriedades.Controls.Add(cmbBordaElemento);
        //    yPos += 35;

        //    Label lblEspessuraBorda = new Label
        //    {
        //        Text = "Espessura da Borda:",
        //        Location = new Point(10, yPos),
        //        Size = new Size(160, 20),
        //        Font = new Font("Segoe UI", 8, FontStyle.Bold),
        //        ForeColor = Color.Gray
        //    };
        //    panelPropriedades.Controls.Add(lblEspessuraBorda);
        //    yPos += 25;

        //    numEspessuraBorda = new NumericUpDown
        //    {
        //        Location = new Point(10, yPos),
        //        Size = new Size(70, 23),
        //        Minimum = 0.1m,
        //        Maximum = 20,
        //        DecimalPlaces = 1,
        //        Increment = 0.1m,
        //        Value = 1,
        //        Enabled = false
        //    };
        //    numEspessuraBorda.ValueChanged += (s, e) => AlterarEspessuraBordaElementosSelecionados();
        //    panelPropriedades.Controls.Add(numEspessuraBorda);
        //}
        //} //final do Else 

        private Button CriarBotaoElemento(string texto, int yPos, Action onClick)
        {
            if (ModuloAppHelper.EstaEmModuloDistribuidoraWeb())
            {
                Button btn = new Button
                {

                    Text = texto,
                    Location = new Point(10, yPos),
                    Size = new Size(180, 35),
                    Font = new Font("Segoe UI", 9),
                    BackColor = Color.FromArgb(255, 143, 0),
                    ForeColor = Color.Black,
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand,
                    TextAlign = ContentAlignment.MiddleLeft


                };
                btn.FlatAppearance.BorderSize = 0;
                btn.Click += (s, e) => onClick();
                ThemeManager.StyleToolCard(btn);
                panelToolbox.Controls.Add(btn);
                return btn;
            }
            else
            {
                Button btn = new Button
                {

                    Text = texto,
                    Location = new Point(200, yPos),
                    Size = new Size(180, 35),
                    Font = new Font("Segoe UI", 9),
                    BackColor = Color.FromArgb(255, 143, 0),
                    ForeColor = Color.Black,
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand,
                    TextAlign = ContentAlignment.MiddleLeft


                };
                btn.FlatAppearance.BorderSize = 0;
                btn.Click += (s, e) => onClick();
                ThemeManager.StyleToolCard(btn);
                panelToolbox.Controls.Add(btn);
                return btn;

            }
        }

        #endregion

        #region Adicionar Elementos

        private void AdicionarElemento(TipoElemento tipo)
        {
            SalvarEstadoHistorico();
            var elemento = new ElementoEtiqueta
            {
                Tipo = tipo,
                NomeFonte = "Arial",
                Fonte = new Font("Arial", 10),
                Cor = Color.Black
            };

            if (tipo == TipoElemento.Texto)
            {
                elemento.Conteudo = "Texto";

                using (Graphics g = Graphics.FromImage(new Bitmap(1, 1)))
                {
                    SizeF tamanhoTexto = g.MeasureString(elemento.Conteudo, elemento.Fonte);
                    int largura = Math.Min((int)(tamanhoTexto.Width / MM_PARA_PIXEL) + 2, (int)template.Largura - 2);
                    int altura = Math.Min((int)(tamanhoTexto.Height / MM_PARA_PIXEL) + 2, (int)template.Altura - 2);
                    elemento.Bounds = new Rectangle(1, 1, Math.Max(3, largura), Math.Max(2, altura));
                }
            }
            else if (tipo == TipoElemento.Expressao)
            {
                elemento.Conteudo = "PrecoVenda * 1.10";
                elemento.Fonte = new Font("Arial", 8);

                using (Graphics g = Graphics.FromImage(new Bitmap(1, 1)))
                {
                    SizeF tamanhoTexto = g.MeasureString("[Expressão]", elemento.Fonte);
                    int largura = Math.Min((int)(tamanhoTexto.Width / MM_PARA_PIXEL) + 4, (int)template.Largura - 2);
                    int altura = Math.Min((int)(tamanhoTexto.Height / MM_PARA_PIXEL) + 2, (int)template.Altura - 2);
                    elemento.Bounds = new Rectangle(1, 1, Math.Max(12, largura), Math.Max(3, altura));
                }
            }

            template.Elementos.Add(elemento);
            elementoSelecionado = elemento;
            elementosSelecionados.Clear();
            SalvarEstadoHistorico();
            AtualizarPainelPropriedades();
            pbCanvas.Invalidate();
            
        }

        private void AdicionarCampo(string campo)
        {
            SalvarEstadoHistorico();
            var elemento = new ElementoEtiqueta
            {
                Tipo = TipoElemento.Campo,
                Conteudo = campo,
                Fonte = new Font("Arial", 8),
                Cor = Color.Black
            };

            string textoExemplo = "[" + campo + "]";
            using (Graphics g = Graphics.FromImage(new Bitmap(1, 1)))
            {
                SizeF tamanhoTexto = g.MeasureString(textoExemplo, elemento.Fonte);
                int largura = Math.Min((int)(tamanhoTexto.Width / MM_PARA_PIXEL) + 2, (int)template.Largura - 2);
                int altura = Math.Min((int)(tamanhoTexto.Height / MM_PARA_PIXEL) + 2, (int)template.Altura - 2);
                elemento.Bounds = new Rectangle(1, 1, Math.Max(3, largura), Math.Max(2, altura));
            }

            template.Elementos.Add(elemento);
            elementoSelecionado = elemento;
            elementosSelecionados.Clear();
            SalvarEstadoHistorico();
            AtualizarPainelPropriedades();
            pbCanvas.Invalidate();
            
        }

        private void AdicionarCodigoBarras(string campoCodigo)
        {
            SalvarEstadoHistorico();
            var elemento = new ElementoEtiqueta
            {
                Tipo = TipoElemento.CodigoBarras,
                Conteudo = campoCodigo,
                Fonte = new Font("Arial", 8),
                Cor = Color.Black
            };

            int largura = Math.Max(10, Math.Min((int)(template.Largura * 0.8f), (int)template.Largura - 2));
            int altura = Math.Max(5, Math.Min((int)(template.Altura * 0.4f), (int)template.Altura - 2));

            elemento.Bounds = new Rectangle(1, 1, largura, altura);
            template.Elementos.Add(elemento);
            elementoSelecionado = elemento;
            elementosSelecionados.Clear();
            SalvarEstadoHistorico();
            AtualizarPainelPropriedades();
            pbCanvas.Invalidate();
            
        }

        private void AdicionarImagem()
        {
            SalvarEstadoHistorico();
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Imagens|*.jpg;*.jpeg;*.png;*.bmp;*.gif";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    var elemento = new ElementoEtiqueta
                    {
                        Tipo = TipoElemento.Imagem,
                        Imagem = Image.FromFile(ofd.FileName),
                        Bounds = new Rectangle(1, 1, 20, 20)
                    };

                    template.Elementos.Add(elemento);
                    elementoSelecionado = elemento;
                    elementosSelecionados.Clear();
                    SalvarEstadoHistorico();
                    AtualizarPainelPropriedades();
                    pbCanvas.Invalidate();
                }
            }
           
        }

        private void RemoverElementoSelecionado()
        {
            SalvarEstadoHistorico();
            var elementosParaRemover = ObterElementosParaEdicao();
            if (elementosParaRemover.Count > 0)
            {
                var resultado = MessageBox.Show(
                    elementosParaRemover.Count == 1
                        ? "Deseja remover o elemento selecionado?"
                        : $"Deseja remover os {elementosParaRemover.Count} elementos selecionados?",
                    "Confirmar Remoção",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (resultado == DialogResult.Yes)
                {
                    foreach (var elemento in elementosParaRemover)
                        template.Elementos.Remove(elemento);

                    elementoSelecionado = null;
                    elementosSelecionados.Clear();
                    SalvarEstadoHistorico();
                    AtualizarPainelPropriedades();
                    pbCanvas.Invalidate();
                }
            }
            else
            {
                MessageBox.Show("Nenhum elemento selecionado!", "Atenção",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void AtualizarPainelPropriedades()
        {
            var elementosEdicao = ObterElementosParaEdicao();
            AtualizarStatusBarSelecao();
            if (elementosEdicao.Count == 0)
            {
                panelPropriedades.Visible = false;
                return;
            }

            atualizandoPropriedades = true;
            try
            {
                panelPropriedades.Visible = true;

                bool selecaoMultipla = elementosEdicao.Count > 1;
                bool edicaoIndividual = elementoSelecionado != null && elementosEdicao.Count == 1;
                ElementoEtiqueta elementoReferencia = edicaoIndividual ? elementoSelecionado : elementosEdicao[0];

                var lblNomeAtual = panelPropriedades.Controls.Find("lblNomeElementoAtual", false).FirstOrDefault() as Label;
                if (lblNomeAtual != null)
                {
                    string tipoNome;
                    if (selecaoMultipla)
                    {
                        tipoNome = $"{elementosEdicao.Count} elementos selecionados";
                    }
                    else
                    {
                        switch (elementoReferencia.Tipo)
                        {
                            case TipoElemento.Texto:
                                string textoResumido = elementoReferencia.Conteudo ?? "";
                                if (textoResumido.Length > 18) textoResumido = textoResumido.Substring(0, 18) + "…";
                                tipoNome = "📝 Texto: \"" + textoResumido + "\"";
                                break;
                            case TipoElemento.Campo:
                                tipoNome = "🏷️ Campo: " + (elementoReferencia.Conteudo ?? "");
                                break;
                            case TipoElemento.CodigoBarras:
                                tipoNome = "▌ ▌ ▌ Cód.Barras: " + (elementoReferencia.Conteudo ?? "");
                                break;
                            case TipoElemento.Imagem:
                                tipoNome = "🖼️ Imagem";
                                break;
                            case TipoElemento.Expressao:
                                string expressaoResumida = elementoReferencia.Conteudo ?? "";
                                if (expressaoResumida.Length > 18) expressaoResumida = expressaoResumida.Substring(0, 18) + "…";
                                tipoNome = "∑ Expressão: " + expressaoResumida;
                                break;
                            default:
                                tipoNome = elementoReferencia.Tipo.ToString();
                                break;
                        }
                    }

                    lblNomeAtual.Text = tipoNome;
                    lblNomeAtual.Visible = true;
                }

                DefinirValorNumerico(numElementoLargura, Math.Max(1, elementoReferencia.Bounds.Width));
                DefinirValorNumerico(numElementoAltura, Math.Max(1, elementoReferencia.Bounds.Height));

                btnAlinharEsquerda.Enabled = edicaoIndividual;
                btnAlinharCentro.Enabled = edicaoIndividual;
                btnAlinharDireita.Enabled = edicaoIndividual;
                cmbFonte.Enabled = edicaoIndividual;
                numTamanhoFonte.Enabled = edicaoIndividual;
                chkNegrito.Enabled = edicaoIndividual;
                chkItalico.Enabled = edicaoIndividual;

                if (edicaoIndividual && elementoSelecionado.Fonte != null)
                {
                    DefinirValorNumerico(numTamanhoFonte, (decimal)elementoSelecionado.Fonte.Size);
                    chkNegrito.Checked = elementoSelecionado.Fonte.Bold;
                    chkItalico.Checked = elementoSelecionado.Fonte.Italic;

                    string nomeFonte = elementoSelecionado.NomeFonte ?? elementoSelecionado.Fonte.FontFamily.Name;
                    if (cmbFonte.Items.Contains(nomeFonte))
                        cmbFonte.SelectedItem = nomeFonte;
                    else
                        cmbFonte.SelectedIndex = -1;
                }

                btnCor.BackColor = elementoReferencia.Cor;
                btnCor.ForeColor = elementoReferencia.Cor.GetBrightness() > 0.5 ? Color.Black : Color.White;

                if (elementoReferencia.CorFundo.HasValue)
                {
                    btnCorFundo.BackColor = elementoReferencia.CorFundo.Value;
                    btnCorFundo.ForeColor = elementoReferencia.CorFundo.Value.GetBrightness() > 0.5 ? Color.Black : Color.White;
                }
                else
                {
                    btnCorFundo.BackColor = Color.Transparent;
                    btnCorFundo.ForeColor = Color.Black;
                }

                if (cmbBordaElemento != null)
                    cmbBordaElemento.SelectedIndex = elementoReferencia.Borda == TipoBordaElemento.SolidaPreta ? 1 : 0;

                if (numEspessuraBorda != null)
                {
                    DefinirValorNumerico(numEspessuraBorda, (decimal)Math.Max(0.1f, elementoReferencia.EspessuraBorda));
                    numEspessuraBorda.Enabled = elementoReferencia.Borda == TipoBordaElemento.SolidaPreta;
                }

                bool codigoBarrasIndividual = edicaoIndividual &&
                    elementoReferencia.Tipo == TipoElemento.CodigoBarras;
                bool ean13Selecionado = codigoBarrasIndividual &&
                    elementoReferencia.SimbologiaCodigoBarras == TipoSimbologiaCodigoBarras.Ean13;
                if (lblSimbologiaCodigoBarras != null)
                    lblSimbologiaCodigoBarras.Visible = codigoBarrasIndividual;
                if (cmbSimbologiaCodigoBarras != null)
                {
                    cmbSimbologiaCodigoBarras.Visible = codigoBarrasIndividual;
                    cmbSimbologiaCodigoBarras.SelectedIndex = ean13Selecionado ? 1 : 0;
                }
                bool code128Selecionado = codigoBarrasIndividual && !ean13Selecionado;
                if (lblExibirNumeracaoCodigoBarras != null)
                    lblExibirNumeracaoCodigoBarras.Visible = code128Selecionado;
                if (chkExibirNumeracaoCodigoBarras != null)
                {
                    chkExibirNumeracaoCodigoBarras.Visible = code128Selecionado;
                    chkExibirNumeracaoCodigoBarras.Checked = code128Selecionado &&
                        elementoReferencia.ExibirNumeracaoCodigoBarras;
                }
                bool exibirConfiguracaoAgrupamento = codigoBarrasIndividual &&
                    (ean13Selecionado || elementoReferencia.ExibirNumeracaoCodigoBarras);
                if (lblNumeracaoAgrupada != null)
                    lblNumeracaoAgrupada.Visible = exibirConfiguracaoAgrupamento;
                if (chkNumeracaoAgrupada != null)
                {
                    chkNumeracaoAgrupada.Visible = exibirConfiguracaoAgrupamento;
                    chkNumeracaoAgrupada.Checked = codigoBarrasIndividual &&
                        (elementoReferencia.NumeracaoAgrupada ||
                         elementoReferencia.RenderizacaoLinear1D || ean13Selecionado);
                    chkNumeracaoAgrupada.Enabled = codigoBarrasIndividual &&
                        !elementoReferencia.RenderizacaoLinear1D && !ean13Selecionado;
                }
                if (lblRenderizacaoLinear1D != null)
                    lblRenderizacaoLinear1D.Visible = false;
                if (chkRenderizacaoLinear1D != null)
                {
                    chkRenderizacaoLinear1D.Visible = false;
                    chkRenderizacaoLinear1D.Checked = codigoBarrasIndividual &&
                        elementoReferencia.RenderizacaoLinear1D;
                }

                AtualizarBotoesAlinhamento();
                panelToolbox.ScrollControlIntoView(panelPropriedades);

                var txtConteudo = panelPropriedades.Controls.Find("txtConteudoElemento", false).FirstOrDefault() as TextBox;
                var lblConteudo = panelPropriedades.Controls.Find("lblConteudoTexto", false).FirstOrDefault() as Label;

                if (edicaoIndividual && elementoSelecionado.Tipo == TipoElemento.Texto)
                {
                    if (txtConteudo != null) { txtConteudo.Visible = true; txtConteudo.Text = elementoSelecionado.Conteudo ?? "Texto"; }
                    if (lblConteudo != null) lblConteudo.Visible = true;
                }
                else
                {
                    if (txtConteudo != null) txtConteudo.Visible = false;
                    if (lblConteudo != null) lblConteudo.Visible = false;
                }

                bool campoPreco = edicaoIndividual
                    && elementoSelecionado.Tipo == TipoElemento.Campo
                    && CalculadoraCamposEtiqueta.CampoPermiteCalculo(elementoSelecionado.Conteudo);

                if (lblCalculoPreco != null) lblCalculoPreco.Visible = campoPreco;
                if (cmbOperadorCalculoPreco != null)
                {
                    cmbOperadorCalculoPreco.Visible = campoPreco;
                    string operador = string.IsNullOrWhiteSpace(elementoSelecionado?.OperadorCalculoPreco)
                        ? "Nenhum"
                        : elementoSelecionado.OperadorCalculoPreco;
                    cmbOperadorCalculoPreco.SelectedItem = cmbOperadorCalculoPreco.Items.Contains(operador)
                        ? operador
                        : "Nenhum";
                }

                if (numValorCalculoPreco != null)
                {
                    numValorCalculoPreco.Visible = campoPreco;
                    DefinirValorNumerico(numValorCalculoPreco, elementoSelecionado?.ValorCalculoPreco ?? 0m);
                    numValorCalculoPreco.Enabled = campoPreco
                        && cmbOperadorCalculoPreco != null
                        && cmbOperadorCalculoPreco.SelectedItem != null
                        && cmbOperadorCalculoPreco.SelectedItem.ToString() != "Nenhum";
                }

                bool expressao = edicaoIndividual && elementoSelecionado.Tipo == TipoElemento.Expressao;
                if (lblExpressaoFormula != null) lblExpressaoFormula.Visible = expressao;
                if (txtExpressaoFormula != null)
                {
                    txtExpressaoFormula.Visible = expressao;
                    txtExpressaoFormula.Enabled = expressao;
                    txtExpressaoFormula.Text = expressao ? (elementoSelecionado.Conteudo ?? "") : "";
                    txtExpressaoFormula.BackColor = Color.White;
                }
                if (btnInserirCampoExpressao != null) btnInserirCampoExpressao.Visible = expressao;
                if (btnEditorExpressao != null) btnEditorExpressao.Visible = expressao;
            }
            finally
            {
                atualizandoPropriedades = false;
            }
        }

        private List<ElementoEtiqueta> ObterElementosParaEdicao()
        {
            if (elementosSelecionados.Count > 0)
                return elementosSelecionados.Where(el => el != null).Distinct().ToList();

            if (elementoSelecionado != null)
                return new List<ElementoEtiqueta> { elementoSelecionado };

            return new List<ElementoEtiqueta>();
        }

        private void DefinirValorNumerico(NumericUpDown controle, decimal valor)
        {
            if (controle == null) return;

            if (valor < controle.Minimum) valor = controle.Minimum;
            if (valor > controle.Maximum) valor = controle.Maximum;
            controle.Value = valor;
        }

        private void AlterarCalculoPreco()
        {
            if (atualizandoPropriedades) return;
            if (elementoSelecionado == null || elementoSelecionado.Tipo != TipoElemento.Campo) return;
            if (!CalculadoraCamposEtiqueta.CampoPermiteCalculo(elementoSelecionado.Conteudo)) return;
            if (cmbOperadorCalculoPreco == null || numValorCalculoPreco == null) return;

            string operadorSelecionado = cmbOperadorCalculoPreco.SelectedItem?.ToString() ?? "Nenhum";
            string novoOperador = operadorSelecionado == "Nenhum" ? string.Empty : operadorSelecionado;
            decimal novoValor = numValorCalculoPreco.Value;

            numValorCalculoPreco.Enabled = operadorSelecionado != "Nenhum";

            string operadorAtual = elementoSelecionado.OperadorCalculoPreco ?? string.Empty;
            if (operadorAtual == novoOperador && elementoSelecionado.ValorCalculoPreco == novoValor)
                return;

            elementoSelecionado.OperadorCalculoPreco = novoOperador;
            elementoSelecionado.ValorCalculoPreco = novoValor;

            SalvarEstadoHistorico();
            pbCanvas.Invalidate();
        }

        private void AlterarExpressao()
        {
            if (atualizandoPropriedades) return;
            if (elementoSelecionado == null || elementoSelecionado.Tipo != TipoElemento.Expressao) return;
            if (txtExpressaoFormula == null) return;

            string novaExpressao = txtExpressaoFormula.Text ?? string.Empty;
            if ((elementoSelecionado.Conteudo ?? string.Empty) == novaExpressao)
                return;

            elementoSelecionado.Conteudo = novaExpressao;
            txtExpressaoFormula.BackColor = Color.White;
            SalvarEstadoHistorico();
            pbCanvas.Invalidate();
        }

        private bool ValidarExpressaoSelecionada(bool mostrarMensagem)
        {
            if (atualizandoPropriedades) return true;
            if (elementoSelecionado == null || elementoSelecionado.Tipo != TipoElemento.Expressao) return true;

            ResultadoExpressao resultado = ValidarExpressaoDesigner(elementoSelecionado.Conteudo);
            bool valida = resultado.Sucesso;

            if (txtExpressaoFormula != null)
                txtExpressaoFormula.BackColor = valida ? Color.White : Color.MistyRose;

            if (!valida && mostrarMensagem)
            {
                MessageBox.Show(
                    resultado.MensagemErro,
                    "Expressão inválida",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }

            return valida;
        }

        private void AbrirMenuCamposExpressao()
        {
            if (txtExpressaoFormula == null || !txtExpressaoFormula.Visible)
                return;

            ContextMenuStrip menu = new ContextMenuStrip();
            IEnumerable<string> campos = CampoEtiquetaResolver.ObterCamposDisponiveis();
            if (ModuloAppHelper.EstaEmModuloDistribuidoraWeb())
                campos = campos.Concat(EtiquetaDistribuidoraResolver.ObterNovosCamposExpressao());

            foreach (string campo in campos.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                ToolStripMenuItem item = new ToolStripMenuItem(campo);
                item.Click += (s, e) => InserirTextoNaExpressao(campo);
                menu.Items.Add(item);
            }

            menu.Show(btnInserirCampoExpressao, new Point(0, btnInserirCampoExpressao.Height));
        }

        private void InserirTextoNaExpressao(string texto)
        {
            if (txtExpressaoFormula == null)
                return;

            int inicio = txtExpressaoFormula.SelectionStart;
            txtExpressaoFormula.SelectedText = texto;
            txtExpressaoFormula.Focus();
            txtExpressaoFormula.SelectionStart = Math.Min(txtExpressaoFormula.Text.Length, inicio + texto.Length);
        }

        private void AbrirEditorExpressao()
        {
            if (elementoSelecionado == null || elementoSelecionado.Tipo != TipoElemento.Expressao)
                return;

            using (Form editor = new Form())
            using (TextBox txtFormula = new TextBox())
            using (ListBox lstCampos = new ListBox())
            using (Button btnOk = new Button())
            using (Button btnCancelar = new Button())
            {
                editor.Text = "Editor de Expressão";
                editor.StartPosition = FormStartPosition.CenterParent;
                editor.FormBorderStyle = FormBorderStyle.FixedDialog;
                editor.MinimizeBox = false;
                editor.MaximizeBox = false;
                editor.ClientSize = new Size(520, 300);

                txtFormula.Multiline = true;
                txtFormula.ScrollBars = ScrollBars.Vertical;
                txtFormula.Font = new Font("Consolas", 10);
                txtFormula.Location = new Point(12, 12);
                txtFormula.Size = new Size(340, 230);
                txtFormula.Text = elementoSelecionado.Conteudo ?? string.Empty;
                editor.Controls.Add(txtFormula);

                lstCampos.Location = new Point(365, 12);
                lstCampos.Size = new Size(140, 230);
                lstCampos.Font = new Font("Segoe UI", 9);
                IEnumerable<string> campos = CampoEtiquetaResolver.ObterCamposDisponiveis();
                if (ModuloAppHelper.EstaEmModuloDistribuidoraWeb())
                    campos = campos.Concat(EtiquetaDistribuidoraResolver.ObterNovosCamposExpressao());

                foreach (string campo in campos.Distinct(StringComparer.OrdinalIgnoreCase))
                    lstCampos.Items.Add(campo);
                lstCampos.DoubleClick += (s, e) =>
                {
                    if (lstCampos.SelectedItem == null) return;
                    int inicio = txtFormula.SelectionStart;
                    string campo = lstCampos.SelectedItem.ToString();
                    txtFormula.SelectedText = campo;
                    txtFormula.Focus();
                    txtFormula.SelectionStart = Math.Min(txtFormula.Text.Length, inicio + campo.Length);
                };
                editor.Controls.Add(lstCampos);

                btnOk.Text = "OK";
                btnOk.Location = new Point(330, 258);
                btnOk.Size = new Size(80, 28);
                btnOk.DialogResult = DialogResult.OK;
                editor.Controls.Add(btnOk);

                btnCancelar.Text = "Cancelar";
                btnCancelar.Location = new Point(425, 258);
                btnCancelar.Size = new Size(80, 28);
                btnCancelar.DialogResult = DialogResult.Cancel;
                editor.Controls.Add(btnCancelar);

                editor.AcceptButton = btnOk;
                editor.CancelButton = btnCancelar;

                if (editor.ShowDialog(this) != DialogResult.OK)
                    return;

                ResultadoExpressao validacao = ValidarExpressaoDesigner(txtFormula.Text);
                if (!validacao.Sucesso)
                {
                    MessageBox.Show(
                        validacao.MensagemErro,
                        "Expressão inválida",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                if ((elementoSelecionado.Conteudo ?? string.Empty) != txtFormula.Text)
                {
                    elementoSelecionado.Conteudo = txtFormula.Text;
                    if (txtExpressaoFormula != null)
                        txtExpressaoFormula.Text = txtFormula.Text;

                    SalvarEstadoHistorico();
                    pbCanvas.Invalidate();
                }
            }
        }

        private void AlterarLarguraElementosSelecionados()
        {
            if (atualizandoPropriedades) return;

            var elementosEdicao = ObterElementosParaEdicao();
            if (elementosEdicao.Count == 0) return;

            int novaLargura = (int)numElementoLargura.Value;
            bool alterou = false;

            foreach (var elemento in elementosEdicao)
            {
                if (elemento.Bounds.Width == novaLargura) continue;

                var bounds = elemento.Bounds;
                bounds.Width = novaLargura;
                elemento.Bounds = bounds;
                alterou = true;
            }

            if (alterou)
            {
                SalvarEstadoHistorico();
                pbCanvas.Invalidate();
            }
        }

        private void AlterarAlturaElementosSelecionados()
        {
            if (atualizandoPropriedades) return;

            var elementosEdicao = ObterElementosParaEdicao();
            if (elementosEdicao.Count == 0) return;

            int novaAltura = (int)numElementoAltura.Value;
            bool alterou = false;

            foreach (var elemento in elementosEdicao)
            {
                if (elemento.Bounds.Height == novaAltura) continue;

                var bounds = elemento.Bounds;
                bounds.Height = novaAltura;
                elemento.Bounds = bounds;
                alterou = true;
            }

            if (alterou)
            {
                SalvarEstadoHistorico();
                pbCanvas.Invalidate();
            }
        }

        private void AtualizarBotoesAlinhamento()
        {
            btnAlinharEsquerda.BackColor = Color.FromArgb(236, 240, 241);
            btnAlinharCentro.BackColor = Color.FromArgb(236, 240, 241);
            btnAlinharDireita.BackColor = Color.FromArgb(236, 240, 241);

            if (elementoSelecionado == null) return;

            StringAlignment alinhamento = elementoSelecionado.Alinhamento;
            if (alinhamento == StringAlignment.Near)
                btnAlinharEsquerda.BackColor = Color.FromArgb(52, 152, 219);
            else if (alinhamento == StringAlignment.Center)
                btnAlinharCentro.BackColor = Color.FromArgb(52, 152, 219);
            else if (alinhamento == StringAlignment.Far)
                btnAlinharDireita.BackColor = Color.FromArgb(52, 152, 219);
        }

        private void AlterarAlinhamento(StringAlignment novoAlinhamento)
        {
            if (atualizandoPropriedades) return;
            if (elementoSelecionado == null) return;
            if (elementoSelecionado.Alinhamento == novoAlinhamento) return;

            elementoSelecionado.Alinhamento = novoAlinhamento;
            AtualizarBotoesAlinhamento();
            SalvarEstadoHistorico();
            pbCanvas.Invalidate();
            
        }

        private void CmbFonte_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (atualizandoPropriedades) return;
            if (elementoSelecionado == null || elementoSelecionado.Fonte == null) return;
            if (cmbFonte.SelectedItem == null) return;

            string nomeFonte = cmbFonte.SelectedItem.ToString();
            string nomeAtual = elementoSelecionado.NomeFonte ?? elementoSelecionado.Fonte.FontFamily.Name;
            if (string.Equals(nomeAtual, nomeFonte, StringComparison.OrdinalIgnoreCase)) return;

            float tamanho = elementoSelecionado.Fonte.Size;
            FontStyle estilo = elementoSelecionado.Fonte.Style;

            try
            {
                elementoSelecionado.NomeFonte = nomeFonte;
                elementoSelecionado.Fonte = new Font(nomeFonte, tamanho, estilo);
                SalvarEstadoHistorico();
                pbCanvas.Invalidate();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao aplicar fonte: {ex.Message}", "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }            
        }

        private void AlterarTamanhoFonte()
        {
            if (atualizandoPropriedades) return;
            if (elementoSelecionado == null || elementoSelecionado.Fonte == null) return;
            float novoTamanho = (float)numTamanhoFonte.Value;
            if (Math.Abs(elementoSelecionado.Fonte.Size - novoTamanho) < 0.01f) return;

            FontStyle estilo = elementoSelecionado.Fonte.Style;
            string nomeFonte = elementoSelecionado.NomeFonte ?? elementoSelecionado.Fonte.FontFamily.Name;
            elementoSelecionado.Fonte = new Font(nomeFonte, novoTamanho, estilo);
            SalvarEstadoHistorico();
            pbCanvas.Invalidate();            
        }

        private void AlterarEstiloFonte()
        {
            if (atualizandoPropriedades) return;
            if (elementoSelecionado == null || elementoSelecionado.Fonte == null) return;

            FontStyle estilo = FontStyle.Regular;
            if (chkNegrito.Checked) estilo |= FontStyle.Bold;
            if (chkItalico.Checked) estilo |= FontStyle.Italic;
            if (elementoSelecionado.Fonte.Style == estilo &&
                elementoSelecionado.Negrito == chkNegrito.Checked &&
                elementoSelecionado.Italico == chkItalico.Checked) return;

            string nomeFonte = elementoSelecionado.NomeFonte ?? elementoSelecionado.Fonte.FontFamily.Name;
            elementoSelecionado.Fonte = new Font(nomeFonte, elementoSelecionado.Fonte.Size, estilo);
            elementoSelecionado.Negrito = chkNegrito.Checked;
            elementoSelecionado.Italico = chkItalico.Checked;
            SalvarEstadoHistorico();
            pbCanvas.Invalidate();            
        }

        private void BtnCor_Click(object sender, EventArgs e)
        {
            if (elementoSelecionado == null && elementosSelecionados.Count == 0) return;
            using (ColorDialog colorDialog = new ColorDialog())
            {
                colorDialog.Color = elementoSelecionado?.Cor ?? Color.Black;
                if (colorDialog.ShowDialog() == DialogResult.OK)
                    AplicarCorTexto(colorDialog.Color);
            }
            
        }

        private void BtnCorFundo_Click(object sender, EventArgs e)
        {
            if (elementoSelecionado == null && elementosSelecionados.Count == 0) return;
            using (ColorDialog colorDialog = new ColorDialog())
            {
                colorDialog.Color = elementoSelecionado?.CorFundo ?? Color.Transparent;
                if (colorDialog.ShowDialog() == DialogResult.OK)
                    AplicarCorFundo(colorDialog.Color);
            }
            
        }

        private void AplicarCorTexto(Color cor)
        {
            if (elementosSelecionados.Count > 0)
                foreach (var elem in elementosSelecionados) elem.Cor = cor;
            else if (elementoSelecionado != null)
                elementoSelecionado.Cor = cor;
            else
                return;

            btnCor.BackColor = cor;
            btnCor.ForeColor = cor.GetBrightness() > 0.5 ? Color.Black : Color.White;
            SalvarEstadoHistorico();
            pbCanvas.Invalidate();
            
        }

        private void AplicarCorFundo(Color? cor)
        {
            if (elementosSelecionados.Count > 0)
                foreach (var elem in elementosSelecionados) elem.CorFundo = cor;
            else if (elementoSelecionado != null)
                elementoSelecionado.CorFundo = cor;
            else
                return;

            if (cor.HasValue)
            {
                btnCorFundo.BackColor = cor.Value;
                btnCorFundo.ForeColor = cor.Value.GetBrightness() > 0.5 ? Color.Black : Color.White;
            }
            else
            {
                btnCorFundo.BackColor = Color.Transparent;
                btnCorFundo.ForeColor = Color.Black;
            }
            SalvarEstadoHistorico();
            pbCanvas.Invalidate();            
        }

        private void AlterarBordaElementosSelecionados()
        {
            if (atualizandoPropriedades) return;

            var elementosEdicao = ObterElementosParaEdicao();
            if (elementosEdicao.Count == 0 || cmbBordaElemento == null) return;

            TipoBordaElemento novaBorda = cmbBordaElemento.SelectedIndex == 1
                ? TipoBordaElemento.SolidaPreta
                : TipoBordaElemento.Transparente;

            if (numEspessuraBorda != null)
                numEspessuraBorda.Enabled = novaBorda == TipoBordaElemento.SolidaPreta;

            bool alterou = false;
            foreach (var elemento in elementosEdicao)
            {
                if (elemento.Borda != novaBorda)
                {
                    elemento.Borda = novaBorda;
                    alterou = true;
                }

                if (elemento.EspessuraBorda < 0.1f)
                {
                    elemento.EspessuraBorda = 1;
                    alterou = true;
                }
            }

            if (alterou)
            {
                SalvarEstadoHistorico();
                pbCanvas.Invalidate();
            }
        }

        private void AlterarEspessuraBordaElementosSelecionados()
        {
            if (atualizandoPropriedades) return;
            if (numEspessuraBorda == null || !numEspessuraBorda.Enabled) return;

            var elementosEdicao = ObterElementosParaEdicao();
            if (elementosEdicao.Count == 0) return;

            float novaEspessura = (float)numEspessuraBorda.Value;
            bool alterou = false;

            foreach (var elemento in elementosEdicao)
            {
                if (elemento.EspessuraBorda == novaEspessura) continue;

                elemento.EspessuraBorda = novaEspessura;
                alterou = true;
            }

            if (alterou)
            {
                SalvarEstadoHistorico();
                pbCanvas.Invalidate();
            }
        }

        #endregion

        #region Régua de Alinhamento (Snap Lines)

        /// <summary>
        /// Calcula as linhas guia de alinhamento durante o arrasto de um elemento.
        /// Compara bordas/centros do elemento arrastado com bordas/centros dos demais
        /// elementos e das bordas da etiqueta. Quando dentro do limiar SNAP_THRESHOLD_PX,
        /// adiciona a linha guia à lista para ser desenhada no Paint.
        /// </summary>
        //private void AtualizarLinhasGuia(RectangleF rectEtiqueta)
        //{
        //    linhasGuiaAtivas.Clear();

        //    if (!snapAtivo) return;
        //    if (elementoSelecionado == null && elementosSelecionados.Count == 0) return;

        //    float scale = MM_PARA_PIXEL * zoom;
        //    float thresholdMM = SNAP_THRESHOLD_PX / scale;

        //    // ------------------------------------------------------------------
        //    // Calcular bounds atual do elemento sendo arrastado (em MM, com delta)
        //    // ------------------------------------------------------------------
        //    float deltaXMM = deltaArrasto.X / scale;
        //    float deltaYMM = deltaArrasto.Y / scale;

        //    RectangleF b;
        //    if (elementoSelecionado != null)
        //    {
        //        b = new RectangleF(
        //            elementoSelecionado.Bounds.X + deltaXMM,
        //            elementoSelecionado.Bounds.Y + deltaYMM,
        //            elementoSelecionado.Bounds.Width,
        //            elementoSelecionado.Bounds.Height);
        //    }
        //    else
        //    {
        //        // seleção múltipla: usar o bounding box do conjunto
        //        float minX = elementosSelecionados.Min(el => el.Bounds.X) + deltaXMM;
        //        float minY = elementosSelecionados.Min(el => el.Bounds.Y) + deltaYMM;
        //        float maxX = elementosSelecionados.Max(el => el.Bounds.Right) + deltaXMM;
        //        float maxY = elementosSelecionados.Max(el => el.Bounds.Bottom) + deltaYMM;
        //        b = new RectangleF(minX, minY, maxX - minX, maxY - minY);
        //    }

        //    // Pontos de interesse do elemento arrastado (em MM)
        //    float[] bX = { b.Left, b.Left + b.Width / 2f, b.Right };
        //    float[] bY = { b.Top,  b.Top  + b.Height / 2f, b.Bottom };

        //    // ------------------------------------------------------------------
        //    // Referências: bordas da etiqueta + bordas dos outros elementos
        //    // ------------------------------------------------------------------
        //    var refX = new List<float> { 0f, configuracao.LarguraEtiqueta / 2f, configuracao.LarguraEtiqueta };
        //    var refY = new List<float> { 0f, configuracao.AlturaEtiqueta / 2f,  configuracao.AlturaEtiqueta };

        //    foreach (var outro in template.Elementos)
        //    {
        //        // Ignorar o próprio elemento (ou os elementos da seleção múltipla)
        //        if (outro == elementoSelecionado) continue;
        //        if (elementosSelecionados.Contains(outro)) continue;

        //        refX.Add(outro.Bounds.X);
        //        refX.Add(outro.Bounds.X + outro.Bounds.Width / 2f);
        //        refX.Add(outro.Bounds.Right);

        //        refY.Add(outro.Bounds.Y);
        //        refY.Add(outro.Bounds.Y + outro.Bounds.Height / 2f);
        //        refY.Add(outro.Bounds.Bottom);
        //    }

        //    // ------------------------------------------------------------------
        //    // Detectar alinhamentos e converter para pixels para desenho
        //    // ------------------------------------------------------------------
        //    var adicionadasX = new HashSet<float>();
        //    var adicionadasY = new HashSet<float>();

        //    foreach (float bx in bX)
        //    {
        //        foreach (float rx in refX)
        //        {
        //            if (Math.Abs(bx - rx) <= thresholdMM)
        //            {
        //                float px = rectEtiqueta.X + rx * scale;
        //                // Evitar linhas duplicadas (arredondado a 1px)
        //                float chave = (float)Math.Round(px);
        //                if (adicionadasX.Add(chave))
        //                    linhasGuiaAtivas.Add((true, px));
        //            }
        //        }
        //    }

        //    foreach (float by in bY)
        //    {
        //        foreach (float ry in refY)
        //        {
        //            if (Math.Abs(by - ry) <= thresholdMM)
        //            {
        //                float py = rectEtiqueta.Y + ry * scale;
        //                float chave = (float)Math.Round(py);
        //                if (adicionadasY.Add(chave))
        //                    linhasGuiaAtivas.Add((false, py));
        //            }
        //        }
        //    }
        //}
        private void AtualizarLinhasGuia(RectangleF rectEtiqueta)
        {
            linhasGuiaAtivas.Clear();

            if (!snapAtivo) return;
            if (elementoSelecionado == null && elementosSelecionados.Count == 0) return;

            float scale = MM_PARA_PIXEL * zoom;
            float thresholdMM = SNAP_THRESHOLD_PX / scale;

            float deltaXMM = deltaArrasto.X / scale;
            float deltaYMM = deltaArrasto.Y / scale;

            // Calculamos o bounding box atual (com o movimento do mouse)
            RectangleF b;
            if (elementoSelecionado != null)
            {
                b = new RectangleF(
                    elementoSelecionado.Bounds.X + deltaXMM,
                    elementoSelecionado.Bounds.Y + deltaYMM,
                    elementoSelecionado.Bounds.Width,
                    elementoSelecionado.Bounds.Height);
            }
            else
            {
                float minX = elementosSelecionados.Min(el => el.Bounds.X) + deltaXMM;
                float minY = elementosSelecionados.Min(el => el.Bounds.Y) + deltaYMM;
                float maxX = elementosSelecionados.Max(el => el.Bounds.Right) + deltaXMM;
                float maxY = elementosSelecionados.Max(el => el.Bounds.Bottom) + deltaYMM;
                b = new RectangleF(minX, minY, maxX - minX, maxY - minY);
            }

            // Referências da etiqueta e outros elementos
            var refX = new List<float> { 0f, configuracao.LarguraEtiqueta / 2f, configuracao.LarguraEtiqueta };
            var refY = new List<float> { 0f, configuracao.AlturaEtiqueta / 2f, configuracao.AlturaEtiqueta };
            // ... (seu foreach que popula refX e refY continua igual)

            // VARIÁVEIS PARA O SNAP (MAGNÉTICO)
            float melhorDeltaX = float.MaxValue;
            float melhorDeltaY = float.MaxValue;
            bool snapX = false;
            bool snapY = false;

            // Pontos do elemento arrastado
            float[] bX = { b.Left, b.Left + b.Width / 2f, b.Right };
            float[] bY = { b.Top, b.Top + b.Height / 2f, b.Bottom };

            // --- SNAP VERTICAL (Eixo X) ---
            foreach (float bx in bX)
            {
                foreach (float rx in refX)
                {
                    float diff = rx - bx;
                    if (Math.Abs(diff) <= thresholdMM)
                    {
                        if (Math.Abs(diff) < Math.Abs(melhorDeltaX))
                        {
                            melhorDeltaX = diff;
                            snapX = true;
                            // Guardamos para desenhar a linha depois
                            float px = rectEtiqueta.X + rx * scale;
                            linhasGuiaAtivas.Add((true, px));
                        }
                    }
                }
            }

            // --- SNAP HORIZONTAL (Eixo Y) ---
            foreach (float by in bY)
            {
                foreach (float ry in refY)
                {
                    float diff = ry - by;
                    if (Math.Abs(diff) <= thresholdMM)
                    {
                        if (Math.Abs(diff) < Math.Abs(melhorDeltaY))
                        {
                            melhorDeltaY = diff;
                            snapY = true;
                            float py = rectEtiqueta.Y + ry * scale;
                            linhasGuiaAtivas.Add((false, py));
                        }
                    }
                }
            }

            // APLICAR O MAGNETISMO NO DELTA
            if (snapX)
            {
                // Ajustamos o deltaArrasto para compensar a distância até a linha
                deltaArrasto.X += (int)(melhorDeltaX * scale);
            }
            if (snapY)
            {
                deltaArrasto.Y += (int)(melhorDeltaY * scale);
            }
        }

        /// <summary>
        /// Desenha as linhas guia ativas sobre o canvas.
        /// Chamado no final do PbCanvas_Paint.
        /// </summary>
        private void DesenharLinhasGuia(Graphics g, RectangleF rectEtiqueta)
        {
            if (!snapAtivo || linhasGuiaAtivas.Count == 0) return;

            // Cor magenta semitransparente — visível sobre qualquer fundo
            using (Pen penGuia = new Pen(Color.FromArgb(210, 255, 0, 128), 1f))
            {
                penGuia.DashStyle = DashStyle.Dash;

                foreach (var (isVertical, posPx) in linhasGuiaAtivas)
                {
                    if (isVertical)
                    {
                        // Linha vertical — percorre toda a altura da etiqueta
                        g.DrawLine(penGuia,
                            posPx, rectEtiqueta.Top - 4,
                            posPx, rectEtiqueta.Bottom + 4);
                    }
                    else
                    {
                        // Linha horizontal — percorre toda a largura da etiqueta
                        g.DrawLine(penGuia,
                            rectEtiqueta.Left - 4, posPx,
                            rectEtiqueta.Right + 4, posPx);
                    }
                }
            }

            // Círculos nos cruzamentos de linhas guia para destaque extra
            var verticais = linhasGuiaAtivas.Where(l => l.isVertical).Select(l => l.posicaoPx).ToList();
            var horizontais = linhasGuiaAtivas.Where(l => !l.isVertical).Select(l => l.posicaoPx).ToList();

            if (verticais.Count > 0 && horizontais.Count > 0)
            {
                using (SolidBrush brushPonto = new SolidBrush(Color.FromArgb(230, 255, 0, 128)))
                {
                    foreach (float vx in verticais)
                        foreach (float hy in horizontais)
                            g.FillEllipse(brushPonto, vx - 3, hy - 3, 6, 6);
                }
            }
        }

        #endregion

        #region Desenhar Canvas

        private void PbCanvas_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.White);

            RectangleF rectEtiqueta = new RectangleF(
                25, 25,
                configuracao.LarguraEtiqueta * MM_PARA_PIXEL * zoom,
                configuracao.AlturaEtiqueta * MM_PARA_PIXEL * zoom
            );

            g.FillRectangle(Brushes.White, rectEtiqueta);
            g.DrawRectangle(new Pen(Color.FromArgb(230, 126, 34), 2), Rectangle.Round(rectEtiqueta));

            if (configuracao.NumColunas > 1 || configuracao.NumLinhas > 1)
                DesenharGrid(g, rectEtiqueta);

            DesenharElementos(g, rectEtiqueta);

            // Retângulo de seleção múltipla
            if (selecionandoComRetangulo)
            {
                using (Pen penSelecao = new Pen(Color.DodgerBlue, 1))
                using (SolidBrush brushSelecao = new SolidBrush(Color.FromArgb(30, Color.DodgerBlue)))
                {
                    penSelecao.DashStyle = DashStyle.Dot;
                    g.FillRectangle(brushSelecao, retanguloSelecao);
                    g.DrawRectangle(penSelecao, retanguloSelecao);
                }
            }

            // =========================================================
            // DESENHAR LINHAS GUIA DE ALINHAMENTO (por último, sobre tudo)
            // =========================================================
            if (arrastando)
                DesenharLinhasGuia(g, rectEtiqueta);            
        }

        private void DesenharGrid(Graphics g, RectangleF rect)
        {
            using (Pen penGrid = new Pen(Color.FromArgb(100, 189, 195, 199), 1))
            {
                penGrid.DashStyle = DashStyle.Dash;

                if (configuracao.NumColunas > 1)
                {
                    float larguraColuna = configuracao.LarguraEtiqueta / configuracao.NumColunas;
                    for (int i = 1; i < configuracao.NumColunas; i++)
                    {
                        float x = rect.X + (larguraColuna * i * MM_PARA_PIXEL);
                        g.DrawLine(penGrid, x, rect.Y, x, rect.Bottom);
                    }
                }

                if (configuracao.NumLinhas > 1)
                {
                    float alturaLinha = configuracao.AlturaEtiqueta / configuracao.NumLinhas;
                    for (int i = 1; i < configuracao.NumLinhas; i++)
                    {
                        float y = rect.Y + (alturaLinha * i * MM_PARA_PIXEL);
                        g.DrawLine(penGrid, rect.X, y, rect.Right, y);
                    }
                }
            }
            
        }

        private void DesenharElementos(Graphics g, RectangleF rectEtiqueta)
        {
            if (template.Elementos.Count == 0)
            {
                string texto = "Adicione elementos usando a toolbox ←";
                using (Font fonteComZoom = new Font(this.Font.FontFamily, this.Font.Size * zoom, this.Font.Style))
                {
                    SizeF tamanho = g.MeasureString(texto, fonteComZoom);
                    g.DrawString(texto, fonteComZoom, Brushes.Gray,
                        rectEtiqueta.X + (rectEtiqueta.Width - tamanho.Width) / 2,
                        rectEtiqueta.Y + (rectEtiqueta.Height - tamanho.Height) / 2);
                }
                return;
            }

            foreach (var elem in template.Elementos)
            {
                DesenharElemento(g, elem, rectEtiqueta, null);

                bool estaSelecionado = (elem == elementoSelecionado) || elementosSelecionados.Contains(elem);

                if (estaSelecionado)
                {
                    Rectangle bounds = ConverterParaPixels(elem.Bounds, rectEtiqueta);

                    if (arrastando)
                    {
                        bounds.X += deltaArrasto.X;
                        bounds.Y += deltaArrasto.Y;
                    }

                    GraphicsState selectionState = null;
                    if (elem.Rotacao != 0)
                    {
                        selectionState = g.Save();
                        PointF centro = new PointF(bounds.X + bounds.Width / 2f, bounds.Y + bounds.Height / 2f);
                        g.TranslateTransform(centro.X, centro.Y);
                        g.RotateTransform(elem.Rotacao);
                        g.TranslateTransform(-centro.X, -centro.Y);
                    }

                    using (Pen penSelecao = new Pen(Color.Blue, 2))
                    {
                        penSelecao.DashStyle = DashStyle.Dash;
                        Rectangle boundsSelecao = bounds;
                        boundsSelecao.Inflate(2, 2);
                        g.DrawRectangle(penSelecao, boundsSelecao);
                    }

                    if (elementoSelecionado == elem && elementosSelecionados.Count == 0)
                        DesenharHandles(g, bounds);

                    if (selectionState != null)
                        g.Restore(selectionState);
                }
            }            
        }

        private void DesenharElemento(Graphics g, ElementoEtiqueta elem, RectangleF rectEtiqueta, Produto produto)
        {
            Rectangle bounds = ConverterParaPixels(elem.Bounds, rectEtiqueta);
            if (elem == elementoSelecionado && arrastando && deltaArrasto != Point.Empty)
            {
                bounds.X += deltaArrasto.X;
                bounds.Y += deltaArrasto.Y;
            }

            GraphicsState state = null;
            if (elem.Rotacao != 0)
            {
                state = g.Save();
                PointF centro = new PointF(bounds.X + bounds.Width / 2f, bounds.Y + bounds.Height / 2f);
                g.TranslateTransform(centro.X, centro.Y);
                g.RotateTransform(elem.Rotacao);
                g.TranslateTransform(-centro.X, -centro.Y);
            }

            if (elem.CorFundo.HasValue && elem.CorFundo.Value != Color.Transparent)
            {
                using (SolidBrush fundoBrush = new SolidBrush(elem.CorFundo.Value))
                    g.FillRectangle(fundoBrush, bounds);
            }

            switch (elem.Tipo)
            {
                case TipoElemento.Texto:
                    using (SolidBrush brush = new SolidBrush(elem.Cor))
                    using (Font fonteComZoom = new Font(elem.Fonte.FontFamily, elem.Fonte.Size * zoom, elem.Fonte.Style))
                    {
                        StringFormat sf = new StringFormat
                        {
                            Alignment = elem.Alinhamento,
                            LineAlignment = StringAlignment.Center,
                            Trimming = StringTrimming.EllipsisCharacter,
                            FormatFlags = StringFormatFlags.LineLimit
                        };
                        g.DrawString(elem.Conteudo ?? "Texto", fonteComZoom, brush, bounds, sf);
                    }
                    break;

                case TipoElemento.Campo:
                    string valor = ObterValorCampo(elem.Conteudo, produto, elem);
                    using (SolidBrush brush = new SolidBrush(elem.Cor))
                    using (Font fonteComZoom = new Font(elem.Fonte.FontFamily, elem.Fonte.Size * zoom, elem.Fonte.Style))
                    {
                        StringFormat sf = new StringFormat
                        {
                            Alignment = elem.Alinhamento,
                            LineAlignment = StringAlignment.Center,
                            Trimming = StringTrimming.EllipsisCharacter,
                            FormatFlags = StringFormatFlags.LineLimit
                        };
                        g.DrawString(valor, fonteComZoom, brush, bounds, sf);
                    }
                    break;

                case TipoElemento.Expressao:
                    string valorExpressao = ObterValorExpressao(elem.Conteudo, produto);
                    using (SolidBrush brush = new SolidBrush(elem.Cor))
                    using (Font fonteComZoom = new Font(elem.Fonte.FontFamily, elem.Fonte.Size * zoom, elem.Fonte.Style))
                    {
                        StringFormat sf = new StringFormat
                        {
                            Alignment = elem.Alinhamento,
                            LineAlignment = StringAlignment.Center,
                            Trimming = StringTrimming.EllipsisCharacter,
                            FormatFlags = StringFormatFlags.LineLimit
                        };
                        g.DrawString(valorExpressao, fonteComZoom, brush, bounds, sf);
                    }
                    break;

                case TipoElemento.CodigoBarras:
                    // No canvas ainda nao existe um produto real. Uma amostra
                    // numerica evita enviar nomes descritivos (inclusive com
                    // acentos) ao codificador Code128.
                    string codigoBarras = produto == null
                        ? "7891222153918"
                        : ObterValorCampo(elem.Conteudo, produto);
                    if (elem.SimbologiaCodigoBarras == TipoSimbologiaCodigoBarras.Ean13)
                    {
                        string codigoPreparado;
                        string mensagemErro;
                        if (!CodigoBarrasRenderer.TryPrepararCodigo(
                            codigoBarras,
                            TipoSimbologiaCodigoBarras.Ean13,
                            out codigoPreparado,
                            out mensagemErro))
                        {
                            codigoBarras = "7891222153918";
                        }
                    }
                    using (Font fonteComZoom = new Font(
                        elem.Fonte.FontFamily,
                        elem.Fonte.Size * zoom,
                        elem.Fonte.Style))
                    {
                        CodigoBarrasRenderer.Renderizar(
                            g,
                            codigoBarras,
                            bounds,
                            false,
                            elem.SimbologiaCodigoBarras,
                            elem.ExibirNumeracaoCodigoBarras,
                            elem.RenderizacaoLinear1D,
                            elem.NumeracaoAgrupada,
                            fonteComZoom,
                            elem.Cor);
                    }
                    break;

                case TipoElemento.Imagem:
                    if (elem.Imagem != null)
                        g.DrawImage(elem.Imagem, bounds);
                    else
                    {
                        g.FillRectangle(Brushes.LightGray, bounds);
                        using (Font fonteComZoom = new Font("Arial", 8 * zoom, FontStyle.Regular))
                            g.DrawString("Imagem", fonteComZoom, Brushes.Black, bounds);
                    }
                    break;
            }

            DesenharBordaElemento(g, bounds, elem, MM_PARA_PIXEL * zoom);

            if (state != null)
                g.Restore(state);
            
        }

        private void DesenharBordaElemento(Graphics g, RectangleF bounds, ElementoEtiqueta elem, float escala)
        {
            if (elem == null || elem.Borda != TipoBordaElemento.SolidaPreta)
                return;

            if (bounds.Width <= 0 || bounds.Height <= 0)
                return;

            float espessura = Math.Max(0.1f, elem.EspessuraBorda) * Math.Max(0.1f, escala);
            using (Pen penBorda = new Pen(Color.Black, espessura))
            {
                penBorda.Alignment = PenAlignment.Inset;
                g.DrawRectangle(penBorda, bounds.X, bounds.Y, bounds.Width, bounds.Height);
            }
        }

        #endregion

        #region Métodos Auxiliares de Desenho

        private Rectangle ConverterParaPixels(Rectangle boundsEmMM, RectangleF rectEtiqueta)
        {
            return new Rectangle(
                (int)(rectEtiqueta.X + boundsEmMM.X * MM_PARA_PIXEL * zoom),
                (int)(rectEtiqueta.Y + boundsEmMM.Y * MM_PARA_PIXEL * zoom),
                (int)(boundsEmMM.Width * MM_PARA_PIXEL * zoom),
                (int)(boundsEmMM.Height * MM_PARA_PIXEL * zoom)
            );
            
        }

        private Rectangle ConverterParaMM(Rectangle boundsEmPixels, RectangleF rectEtiqueta)
        {
            const float pixelParaMM = 1f / MM_PARA_PIXEL;
            return new Rectangle(
                (int)((boundsEmPixels.X - rectEtiqueta.X) * pixelParaMM / zoom),
                (int)((boundsEmPixels.Y - rectEtiqueta.Y) * pixelParaMM / zoom),
                (int)(boundsEmPixels.Width * pixelParaMM / zoom),
                (int)(boundsEmPixels.Height * pixelParaMM / zoom)
            );
        }

        private string ObterValorCampo(string campo, Produto produto, ElementoEtiqueta elemento = null)
        {
            if (ModuloAppHelper.EstaEmModuloDistribuidoraWeb() && EtiquetaDistribuidoraResolver.CampoExiste(campo))
                return $"[{campo}]";

            if (produto == null) return $"[{CalculadoraCamposEtiqueta.ObterDescricaoCalculo(campo, elemento)}]";

            decimal valorCalculado;
            if (CalculadoraCamposEtiqueta.CalculoAtivo(elemento)
                && CalculadoraCamposEtiqueta.TryCalcularValorCampo(produto, campo, elemento, out valorCalculado))
            {
                return FormatadorMonetario.Formatar(valorCalculado);
            }

            switch (campo)
            {
                case "Nome":
                case "Mercadoria":
                case "Descricao":
                case "Descrição":        return DescricaoMercadoriaDisplayHelper.ObterDescricaoVisivel(produto);
                case "Codigo":
                case "CodigoMercadoria": return produto.Codigo ?? "";
                case "Referencia":
                case "CodFabricante":    return produto.CodFabricante ?? "";
                case "CodBarras":        return produto.CodBarras ?? "";
                case "Preco":
                case "PrecoCusto":       return FormatadorMonetario.Formatar(produto.Preco);
                case "PrecoVenda":       return FormatadorMonetario.Formatar(produto.PrecoVenda > 0 ? produto.PrecoVenda : produto.Preco);
                case "VendaA":           return produto.VendaA > 0 ? FormatadorMonetario.Formatar(produto.VendaA) : "-";
                case "VendaB":           return produto.VendaB > 0 ? FormatadorMonetario.Formatar(produto.VendaB) : "-";
                case "VendaC":           return produto.VendaC > 0 ? FormatadorMonetario.Formatar(produto.VendaC) : "-";
                case "VendaD":           return produto.VendaD > 0 ? FormatadorMonetario.Formatar(produto.VendaD) : "-";
                case "VendaE":           return produto.VendaE > 0 ? FormatadorMonetario.Formatar(produto.VendaE) : "-";
                case "Quantidade":       return produto.Quantidade.ToString();
                case "Fornecedor":       return produto.Fornecedor ?? "";
                case "Fabricante":       return produto.Fabricante ?? "";
                case "Grupo":            return produto.Grupo ?? "";
                case "SubGrupo":         return produto.SubGrupo ?? "";
                case "Marca":            return produto.Marca ?? "";
                case "Observacao":       return produto.Observacao ?? "";
                case "Prateleira":       return produto.Prateleira ?? "";
                case "Garantia":         return produto.Garantia ?? "";
                case "Tam":              return produto.Tam ?? "";
                case "Cores":            return produto.Cores ?? "";
                case "CodBarras_Grade":  return produto.CodBarras_Grade ?? "";
                case "PrecoOriginal":
                    return FormatadorMonetario.Formatar(produto.PrecoOriginal ?? produto.Preco);
                case "PrecoPromocional":
                    return FormatadorMonetario.Formatar(produto.PrecoPromocional ?? produto.Preco);
                default: return "";
            }
        }

        private string ObterValorExpressao(string expressao, Produto produto)
        {
            if (ModuloAppHelper.EstaEmModuloDistribuidoraWeb() && EtiquetaDistribuidoraResolver.NovoCampoTextualEmExpressao(expressao))
                return $"[{expressao}]";

            if (produto == null)
                return $"[{(string.IsNullOrWhiteSpace(expressao) ? "Expressão" : expressao)}]";

            ResultadoExpressao resultado = ExpressionEngine.Calcular(expressao, produto);
            return resultado.Sucesso ? FormatadorMonetario.Formatar(resultado.Valor) : "[Erro]";
        }

        private void AlterarNumeracaoAgrupada()
        {
            if (atualizandoPropriedades || elementoSelecionado == null ||
                elementoSelecionado.Tipo != TipoElemento.CodigoBarras ||
                chkNumeracaoAgrupada == null)
            {
                return;
            }

            bool novoValor = chkNumeracaoAgrupada.Checked;
            if (elementoSelecionado.NumeracaoAgrupada == novoValor)
                return;

            elementoSelecionado.NumeracaoAgrupada = novoValor;
            SalvarEstadoHistorico();
            pbCanvas.Invalidate();
        }

        private void AlterarSimbologiaCodigoBarras()
        {
            if (atualizandoPropriedades || elementoSelecionado == null ||
                elementoSelecionado.Tipo != TipoElemento.CodigoBarras ||
                cmbSimbologiaCodigoBarras == null ||
                cmbSimbologiaCodigoBarras.SelectedIndex < 0)
            {
                return;
            }

            TipoSimbologiaCodigoBarras novaSimbologia =
                cmbSimbologiaCodigoBarras.SelectedIndex == 1
                    ? TipoSimbologiaCodigoBarras.Ean13
                    : TipoSimbologiaCodigoBarras.Code128;
            if (elementoSelecionado.SimbologiaCodigoBarras == novaSimbologia)
                return;

            elementoSelecionado.SimbologiaCodigoBarras = novaSimbologia;
            SalvarEstadoHistorico();
            AtualizarPainelPropriedades();
            pbCanvas.Invalidate();
        }

        private void AlterarExibicaoNumeracaoCodigoBarras()
        {
            if (atualizandoPropriedades || elementoSelecionado == null ||
                elementoSelecionado.Tipo != TipoElemento.CodigoBarras ||
                elementoSelecionado.SimbologiaCodigoBarras != TipoSimbologiaCodigoBarras.Code128 ||
                chkExibirNumeracaoCodigoBarras == null)
            {
                return;
            }

            bool novoValor = chkExibirNumeracaoCodigoBarras.Checked;
            if (elementoSelecionado.ExibirNumeracaoCodigoBarras == novoValor)
                return;

            elementoSelecionado.ExibirNumeracaoCodigoBarras = novoValor;
            SalvarEstadoHistorico();
            pbCanvas.Invalidate();
        }

        private void AlterarRenderizacaoLinear1D()
        {
            if (atualizandoPropriedades || elementoSelecionado == null ||
                elementoSelecionado.Tipo != TipoElemento.CodigoBarras ||
                chkRenderizacaoLinear1D == null)
            {
                return;
            }

            bool novoValor = chkRenderizacaoLinear1D.Checked;
            if (elementoSelecionado.RenderizacaoLinear1D == novoValor)
                return;

            elementoSelecionado.RenderizacaoLinear1D = novoValor;
            SalvarEstadoHistorico();
            AtualizarPainelPropriedades();
            pbCanvas.Invalidate();
        }

        private static ResultadoExpressao ValidarExpressaoDesigner(string expressao)
        {
            string campo = (expressao ?? string.Empty).Trim();
            if (ModuloAppHelper.EstaEmModuloDistribuidoraWeb() && EtiquetaDistribuidoraResolver.NovoCampoTextualEmExpressao(campo))
                return ResultadoExpressao.Ok(0m, new[] { campo });

            return ExpressionEngine.Validar(expressao);
        }

        private bool PontoEmRetanguloRotacionado(Point ponto, Rectangle bounds, float rotacao)
        {
            if (rotacao == 0) return bounds.Contains(ponto);

            PointF centro = new PointF(bounds.X + bounds.Width / 2f, bounds.Y + bounds.Height / 2f);
            double anguloRad = -rotacao * Math.PI / 180.0;
            float dx = ponto.X - centro.X;
            float dy = ponto.Y - centro.Y;

            float pontoRotacionadoX = centro.X + (float)(dx * Math.Cos(anguloRad) - dy * Math.Sin(anguloRad));
            float pontoRotacionadoY = centro.Y + (float)(dx * Math.Sin(anguloRad) + dy * Math.Cos(anguloRad));

            return bounds.Contains((int)pontoRotacionadoX, (int)pontoRotacionadoY);
        }

        private float ObterEscalaAlcaRotacao()
        {
            float escalaDpi = Math.Max(1f, DeviceDpi / 96f);
            float escalaZoom = (float)Math.Sqrt(Math.Max(0.3f, Math.Min(3f, zoom)));
            escalaZoom = Math.Max(0.8f, Math.Min(1.65f, escalaZoom));
            return escalaDpi * escalaZoom;
        }

        private void ObterGeometriaAlcasRotacao(
            Rectangle bounds,
            out PointF[] posicoes,
            out float raioVisual,
            out float raioAtivacao)
        {
            float escala = ObterEscalaAlcaRotacao();
            float afastamento = 12f * escala;
            raioVisual = 8f * escala;
            raioAtivacao = 10f * escala;

            posicoes = new[]
            {
                new PointF(bounds.Left  - afastamento, bounds.Top    - afastamento),
                new PointF(bounds.Right + afastamento, bounds.Top    - afastamento),
                new PointF(bounds.Right + afastamento, bounds.Bottom + afastamento),
                new PointF(bounds.Left  - afastamento, bounds.Bottom + afastamento)
            };
        }

        private void DesenharAlcaRotacao(
            Graphics g,
            PointF centro,
            float raio,
            bool sentidoHorario,
            bool emOperacao)
        {
            GraphicsState estado = g.Save();
            try
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.CompositingQuality = CompositingQuality.HighQuality;

                float escala = ObterEscalaAlcaRotacao();
                float deslocamentoSombra = Math.Max(1f, escala);
                RectangleF circulo = new RectangleF(
                    centro.X - raio,
                    centro.Y - raio,
                    raio * 2f,
                    raio * 2f);
                RectangleF sombra = circulo;
                sombra.Offset(deslocamentoSombra, deslocamentoSombra);

                Color corDestaque = emOperacao
                    ? Color.FromArgb(0, 92, 184)
                    : Color.FromArgb(0, 120, 215);
                Color corFundo = emOperacao
                    ? Color.FromArgb(226, 240, 253)
                    : Color.White;

                using (SolidBrush brushSombra = new SolidBrush(Color.FromArgb(45, Color.Black)))
                using (SolidBrush brushFundo = new SolidBrush(corFundo))
                using (Pen penContorno = new Pen(Color.FromArgb(185, corDestaque), Math.Max(1f, escala)))
                {
                    g.FillEllipse(brushSombra, sombra);
                    g.FillEllipse(brushFundo, circulo);
                    g.DrawEllipse(penContorno, circulo);
                }

                float raioArco = raio * 0.53f;
                RectangleF arco = new RectangleF(
                    centro.X - raioArco,
                    centro.Y - raioArco,
                    raioArco * 2f,
                    raioArco * 2f);
                float inicio = sentidoHorario ? -55f : 235f;
                float varredura = sentidoHorario ? 255f : -255f;

                using (GraphicsPath caminho = new GraphicsPath())
                using (Pen penSeta = new Pen(corDestaque, Math.Max(1.6f, 1.65f * escala)))
                {
                    caminho.AddArc(arco, inicio, varredura);
                    penSeta.StartCap = LineCap.Round;
                    penSeta.EndCap = LineCap.ArrowAnchor;
                    penSeta.LineJoin = LineJoin.Round;
                    g.DrawPath(penSeta, caminho);
                }
            }
            finally
            {
                g.Restore(estado);
            }
        }

        private void DesenharHandles(Graphics g, Rectangle bounds)
        {
            const int handleSize = 6;
            Color handleColor    = Color.FromArgb(0, 120, 215);

            Point[] handlePositions = new Point[]
            {
                new Point(bounds.Left,                        bounds.Top),
                new Point(bounds.Right,                       bounds.Top),
                new Point(bounds.Right,                       bounds.Bottom),
                new Point(bounds.Left,                        bounds.Bottom),
                new Point(bounds.Left + bounds.Width / 2,    bounds.Top),
                new Point(bounds.Right,                       bounds.Top + bounds.Height / 2),
                new Point(bounds.Left + bounds.Width / 2,    bounds.Bottom),
                new Point(bounds.Left,                        bounds.Top + bounds.Height / 2)
            };

            using (Brush handleBrush = new SolidBrush(handleColor))
            {
                foreach (var pos in handlePositions)
                {
                    g.FillRectangle(handleBrush, pos.X - handleSize / 2, pos.Y - handleSize / 2, handleSize, handleSize);
                    g.DrawRectangle(Pens.White, pos.X - handleSize / 2, pos.Y - handleSize / 2, handleSize, handleSize);
                }
            }

            bool exibirAlcaRotacao = handleSobMouse >= 8 && handleSobMouse <= 11 &&
                (rotacionando || ReferenceEquals(elementoHoverRotacao, elementoSelecionado));

            if (exibirAlcaRotacao)
            {
                PointF[] rotatePositions;
                float raioVisual;
                float raioAtivacao;
                ObterGeometriaAlcasRotacao(bounds, out rotatePositions, out raioVisual, out raioAtivacao);
                bool[] sentidosHorarios = new bool[] { false, true, false, true };
                int indexSeta = handleSobMouse - 8;
                DesenharAlcaRotacao(
                    g,
                    rotatePositions[indexSeta],
                    raioVisual,
                    sentidosHorarios[indexSeta],
                    rotacionando);
            }
        }

        private int ObterHandleClicado(Point mouse, Rectangle bounds, float rotacao = 0)
        {
            const int handleSize = 6;
            const int tolerance  = 3;

            Point mouseTransformado = mouse;
            if (rotacao != 0)
            {
                PointF centro = new PointF(bounds.X + bounds.Width / 2f, bounds.Y + bounds.Height / 2f);
                double anguloRad = -rotacao * Math.PI / 180.0;
                float dx = mouse.X - centro.X;
                float dy = mouse.Y - centro.Y;
                mouseTransformado = new Point(
                    (int)(centro.X + dx * Math.Cos(anguloRad) - dy * Math.Sin(anguloRad)),
                    (int)(centro.Y + dx * Math.Sin(anguloRad) + dy * Math.Cos(anguloRad))
                );
            }

            PointF[] rotatePositions;
            float raioVisual;
            float rotateClickRadius;
            ObterGeometriaAlcasRotacao(bounds, out rotatePositions, out raioVisual, out rotateClickRadius);

            for (int i = 0; i < rotatePositions.Length; i++)
            {
                float dx = mouseTransformado.X - rotatePositions[i].X;
                float dy = mouseTransformado.Y - rotatePositions[i].Y;
                if ((float)Math.Sqrt(dx * dx + dy * dy) <= rotateClickRadius)
                    return 8 + i;
            }

            Point[] handlePositions = new Point[]
            {
                new Point(bounds.Left,                     bounds.Top),
                new Point(bounds.Right,                    bounds.Top),
                new Point(bounds.Right,                    bounds.Bottom),
                new Point(bounds.Left,                     bounds.Bottom),
                new Point(bounds.Left + bounds.Width / 2,  bounds.Top),
                new Point(bounds.Right,                    bounds.Top + bounds.Height / 2),
                new Point(bounds.Left + bounds.Width / 2,  bounds.Bottom),
                new Point(bounds.Left,                     bounds.Top + bounds.Height / 2)
            };

            for (int i = 0; i < handlePositions.Length; i++)
            {
                Rectangle handleRect = new Rectangle(
                    handlePositions[i].X - handleSize / 2 - tolerance,
                    handlePositions[i].Y - handleSize / 2 - tolerance,
                    handleSize + tolerance * 2,
                    handleSize + tolerance * 2
                );
                if (handleRect.Contains(mouse)) return i;
            }

            return -1;

        }

        private static bool EhHandleRotacao(int handle)
        {
            return handle >= 8 && handle <= 11;
        }

        private Rectangle ObterRegiaoFeedbackRotacao(Rectangle bounds, float rotacao)
        {
            double angulo = rotacao * Math.PI / 180d;
            float larguraRotacionada = (float)(
                Math.Abs(bounds.Width * Math.Cos(angulo)) +
                Math.Abs(bounds.Height * Math.Sin(angulo)));
            float alturaRotacionada = (float)(
                Math.Abs(bounds.Width * Math.Sin(angulo)) +
                Math.Abs(bounds.Height * Math.Cos(angulo)));
            float centroX = bounds.Left + bounds.Width / 2f;
            float centroY = bounds.Top + bounds.Height / 2f;
            float margem = 26f * ObterEscalaAlcaRotacao();

            RectangleF regiao = new RectangleF(
                centroX - larguraRotacionada / 2f - margem,
                centroY - alturaRotacionada / 2f - margem,
                larguraRotacionada + margem * 2f,
                alturaRotacionada + margem * 2f);
            return Rectangle.Ceiling(regiao);
        }

        private void InvalidarFeedbackRotacao(Rectangle bounds, float rotacao)
        {
            pbCanvas?.Invalidate(ObterRegiaoFeedbackRotacao(bounds, rotacao));
        }

        private void PbCanvas_MouseLeave(object sender, EventArgs e)
        {
            statusBarManager?.SetCursor(0, 0, false);
            if (rotacionando)
                return;

            if (elementoHoverRotacao != null && elementoSelecionado != null)
            {
                RectangleF rectEtiqueta = new RectangleF(25, 25,
                    configuracao.LarguraEtiqueta * MM_PARA_PIXEL * zoom,
                    configuracao.AlturaEtiqueta * MM_PARA_PIXEL * zoom);
                Rectangle bounds = ConverterParaPixels(elementoSelecionado.Bounds, rectEtiqueta);
                InvalidarFeedbackRotacao(bounds, elementoSelecionado.Rotacao);
            }

            elementoHoverRotacao = null;
            handleSobMouse = -1;
            if (!arrastando && !redimensionando)
                pbCanvas.Cursor = Cursors.Default;
        }

        #endregion

        #region Eventos do Mouse

        private void PbCanvas_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                SelecionarElementoParaMenuContexto(e.Location);
                menuContextoCanvas?.Show(pbCanvas, e.Location);
                return;
            }

            if (e.Button != MouseButtons.Left) return;
            SalvarEstadoHistorico();
            RectangleF rectEtiqueta = new RectangleF(25, 25,
                configuracao.LarguraEtiqueta * MM_PARA_PIXEL * zoom,
                configuracao.AlturaEtiqueta * MM_PARA_PIXEL * zoom);

            if (elementoSelecionado != null)
            {
                Rectangle bounds = ConverterParaPixels(elementoSelecionado.Bounds, rectEtiqueta);
                handleSelecionado = ObterHandleClicado(e.Location, bounds, elementoSelecionado.Rotacao);

                if (handleSelecionado >= 0)
                {
                    if (handleSelecionado >= 8 && handleSelecionado <= 11)
                    {
                        rotacionando = true;
                        handleSobMouse = handleSelecionado;
                        elementoHoverRotacao = elementoSelecionado;
                        pontoInicialMouse = e.Location;
                        anguloInicial = elementoSelecionado.Rotacao;
                        centroRotacao = new PointF(bounds.X + bounds.Width / 2f, bounds.Y + bounds.Height / 2f);
                    }
                    else
                    {
                        redimensionando = true;
                        pontoInicialMouse = e.Location;
                        boundsIniciais = bounds;
                        boundsIniciaisEmMM = elementoSelecionado.Bounds;
                    }
                    return;
                }

                if (PontoEmRetanguloRotacionado(e.Location, bounds, elementoSelecionado.Rotacao))
                {
                    arrastando = true;
                    pontoInicialMouse = e.Location;
                    boundsIniciais = bounds;
                    return;
                }
            }

            foreach (var elem in elementosSelecionados)
            {
                Rectangle bounds = ConverterParaPixels(elem.Bounds, rectEtiqueta);
                if (PontoEmRetanguloRotacionado(e.Location, bounds, elem.Rotacao))
                {
                    arrastando = true;
                    pontoInicialMouse = e.Location;
                    return;
                }
            }

            ElementoEtiqueta elementoClicado = null;
            for (int i = template.Elementos.Count - 1; i >= 0; i--)
            {
                Rectangle bounds = ConverterParaPixels(template.Elementos[i].Bounds, rectEtiqueta);
                if (PontoEmRetanguloRotacionado(e.Location, bounds, template.Elementos[i].Rotacao))
                {
                    elementoClicado = template.Elementos[i];
                    break;
                }
            }

            if (elementoClicado != null)
            {
                if (ModifierKeys == Keys.Control)
                {
                    if (elementosSelecionados.Contains(elementoClicado))
                        elementosSelecionados.Remove(elementoClicado);
                    else
                    {
                        elementosSelecionados.Add(elementoClicado);
                        elementoSelecionado = null;
                    }
                }
                else
                {
                    elementoSelecionado = elementoClicado;
                    elementosSelecionados.Clear();
                    elementoHoverRotacao = null;
                    handleSobMouse = -1;
                    pontoInicialMouse = e.Location;

                    Rectangle bounds = ConverterParaPixels(elementoClicado.Bounds, rectEtiqueta);
                    boundsIniciais = bounds;
                    handleSelecionado = ObterHandleClicado(e.Location, bounds, elementoSelecionado.Rotacao);

                    if (handleSelecionado >= 0) redimensionando = true;
                    else arrastando = true;
                }

                AtualizarPainelPropriedades();
                pbCanvas.Invalidate();
                return;
            }

            elementoSelecionado = null;
            elementosSelecionados.Clear();
            elementoHoverRotacao = null;
            handleSobMouse = -1;
            selecionandoComRetangulo = true;
            pontoInicialSelecao = e.Location;
            retanguloSelecao = new Rectangle(e.Location, Size.Empty);

            AtualizarPainelPropriedades();
            pbCanvas.Invalidate();
        }

        private void PbCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            
            RectangleF rectEtiqueta = new RectangleF(25, 25,
                configuracao.LarguraEtiqueta * MM_PARA_PIXEL * zoom,
                configuracao.AlturaEtiqueta * MM_PARA_PIXEL * zoom);
            bool cursorNaEtiqueta = rectEtiqueta.Contains(e.Location);
            statusBarManager?.SetCursor(
                (e.X - rectEtiqueta.X) / (MM_PARA_PIXEL * zoom),
                (e.Y - rectEtiqueta.Y) / (MM_PARA_PIXEL * zoom),
                cursorNaEtiqueta);

            if (selecionandoComRetangulo)
            {
                int x = Math.Min(pontoInicialSelecao.X, e.X);
                int y = Math.Min(pontoInicialSelecao.Y, e.Y);
                retanguloSelecao = new Rectangle(x, y,
                    Math.Abs(e.X - pontoInicialSelecao.X),
                    Math.Abs(e.Y - pontoInicialSelecao.Y));
                pbCanvas.Invalidate();
                return;
            }

            if (redimensionando && elementoSelecionado != null)
            {
                float deltaXPixels = e.X - pontoInicialMouse.X;
                float deltaYPixels = e.Y - pontoInicialMouse.Y;

                if (elementoSelecionado.Rotacao != 0)
                {
                    double anguloRad = -elementoSelecionado.Rotacao * Math.PI / 180.0;
                    float dxR = (float)(deltaXPixels * Math.Cos(anguloRad) - deltaYPixels * Math.Sin(anguloRad));
                    float dyR = (float)(deltaXPixels * Math.Sin(anguloRad) + deltaYPixels * Math.Cos(anguloRad));
                    deltaXPixels = dxR;
                    deltaYPixels = dyR;
                }

                float deltaXMM = deltaXPixels / (MM_PARA_PIXEL * zoom);
                float deltaYMM = deltaYPixels / (MM_PARA_PIXEL * zoom);
                RectangleF newBoundsMM = boundsIniciaisEmMM;

                switch (handleSelecionado)
                {
                    case 0: newBoundsMM = new RectangleF(boundsIniciaisEmMM.X + deltaXMM, boundsIniciaisEmMM.Y + deltaYMM, boundsIniciaisEmMM.Width - deltaXMM, boundsIniciaisEmMM.Height - deltaYMM); break;
                    case 1: newBoundsMM = new RectangleF(boundsIniciaisEmMM.X, boundsIniciaisEmMM.Y + deltaYMM, boundsIniciaisEmMM.Width + deltaXMM, boundsIniciaisEmMM.Height - deltaYMM); break;
                    case 2: newBoundsMM = new RectangleF(boundsIniciaisEmMM.X, boundsIniciaisEmMM.Y, boundsIniciaisEmMM.Width + deltaXMM, boundsIniciaisEmMM.Height + deltaYMM); break;
                    case 3: newBoundsMM = new RectangleF(boundsIniciaisEmMM.X + deltaXMM, boundsIniciaisEmMM.Y, boundsIniciaisEmMM.Width - deltaXMM, boundsIniciaisEmMM.Height + deltaYMM); break;
                    case 4: newBoundsMM = new RectangleF(boundsIniciaisEmMM.X, boundsIniciaisEmMM.Y + deltaYMM, boundsIniciaisEmMM.Width, boundsIniciaisEmMM.Height - deltaYMM); break;
                    case 5: newBoundsMM = new RectangleF(boundsIniciaisEmMM.X, boundsIniciaisEmMM.Y, boundsIniciaisEmMM.Width + deltaXMM, boundsIniciaisEmMM.Height); break;
                    case 6: newBoundsMM = new RectangleF(boundsIniciaisEmMM.X, boundsIniciaisEmMM.Y, boundsIniciaisEmMM.Width, boundsIniciaisEmMM.Height + deltaYMM); break;
                    case 7: newBoundsMM = new RectangleF(boundsIniciaisEmMM.X + deltaXMM, boundsIniciaisEmMM.Y, boundsIniciaisEmMM.Width - deltaXMM, boundsIniciaisEmMM.Height); break;
                    case 8: case 9: case 10: case 11: break;
                }

                float minWidthMM  = 10 / (MM_PARA_PIXEL * zoom);
                float minHeightMM = 5  / (MM_PARA_PIXEL * zoom);

                if (newBoundsMM.Width >= minWidthMM && newBoundsMM.Height >= minHeightMM)
                {
                    elementoSelecionado.Bounds = Rectangle.Round(newBoundsMM);
                    pbCanvas.Invalidate();
                }
            }
            else if (arrastando)
            {
                deltaArrasto = new Point(
                    e.X - pontoInicialMouse.X,
                    e.Y - pontoInicialMouse.Y
                );

                // =========================================================
                // ATUALIZAR LINHAS GUIA A CADA MOVIMENTO
                // =========================================================
                AtualizarLinhasGuia(rectEtiqueta);
                // =========================================================

                pbCanvas.Invalidate();
            }
            else if (rotacionando && elementoSelecionado != null)
            {
                pbCanvas.Cursor = Cursors.Hand;
                float dx = e.X - centroRotacao.X;
                float dy = e.Y - centroRotacao.Y;
                float anguloAtual = (float)(Math.Atan2(dy, dx) * 180 / Math.PI);

                float dxInicial       = pontoInicialMouse.X - centroRotacao.X;
                float dyInicial       = pontoInicialMouse.Y - centroRotacao.Y;
                float anguloMouseInicial = (float)(Math.Atan2(dyInicial, dxInicial) * 180 / Math.PI);

                float deltaAngulo = anguloAtual - anguloMouseInicial;
                float novaRotacao = anguloInicial + deltaAngulo;

                while (novaRotacao < 0)   novaRotacao += 360;
                while (novaRotacao >= 360) novaRotacao -= 360;

                elementoSelecionado.Rotacao = novaRotacao;
                pbCanvas.Invalidate();
            }
            else
            {
                if (elementoSelecionado != null)
                {
                    Rectangle bounds = ConverterParaPixels(elementoSelecionado.Bounds, rectEtiqueta);
                    int handle = ObterHandleClicado(e.Location, bounds, elementoSelecionado.Rotacao);
                    bool alterouFeedbackRotacao =
                        EhHandleRotacao(handleSobMouse) ||
                        EhHandleRotacao(handle) ||
                        !ReferenceEquals(
                            elementoHoverRotacao,
                            EhHandleRotacao(handle) ? elementoSelecionado : null);

                    if (handleSobMouse != handle)
                    {
                        handleSobMouse = handle;
                        if (alterouFeedbackRotacao)
                            InvalidarFeedbackRotacao(bounds, elementoSelecionado.Rotacao);
                    }

                    elementoHoverRotacao = EhHandleRotacao(handle)
                        ? elementoSelecionado
                        : null;

                    if (handle >= 8 && handle <= 11)
                        pbCanvas.Cursor = Cursors.Hand;
                    else if (handle >= 0)
                    {
                        switch (handle)
                        {
                            case 0: case 2: pbCanvas.Cursor = Cursors.SizeNWSE; break;
                            case 1: case 3: pbCanvas.Cursor = Cursors.SizeNESW; break;
                            case 4: case 6: pbCanvas.Cursor = Cursors.SizeNS;   break;
                            case 5: case 7: pbCanvas.Cursor = Cursors.SizeWE;   break;
                        }
                    }
                    else if (PontoEmRetanguloRotacionado(e.Location, bounds, elementoSelecionado.Rotacao))
                        pbCanvas.Cursor = Cursors.SizeAll;
                    else
                        pbCanvas.Cursor = Cursors.Default;
                }
                else
                {
                    if (elementoHoverRotacao != null)
                        pbCanvas.Invalidate();
                    elementoHoverRotacao = null;
                    handleSobMouse = -1;
                    pbCanvas.Cursor = Cursors.Cross;
                }
            }
        }

        //private void PbCanvas_MouseUp(object sender, MouseEventArgs e)
        //{

        //    RectangleF rectEtiqueta = new RectangleF(25, 25,
        //        configuracao.LarguraEtiqueta * MM_PARA_PIXEL * zoom,
        //        configuracao.AlturaEtiqueta * MM_PARA_PIXEL * zoom);

        //    if (selecionandoComRetangulo)
        //    {
        //        selecionandoComRetangulo = false;
        //        elementosSelecionados.Clear();
        //        foreach (var elemento in template.Elementos)
        //        {
        //            Rectangle bounds = ConverterParaPixels(elemento.Bounds, rectEtiqueta);
        //            if (retanguloSelecao.IntersectsWith(bounds))
        //                elementosSelecionados.Add(elemento);
        //        }
        //        pbCanvas.Invalidate();
        //        return;
        //    }

        //    if (arrastando && deltaArrasto != Point.Empty)
        //    {
        //        SalvarEstadoHistorico();
        //        float scale    = MM_PARA_PIXEL * zoom;
        //        float thresholdMM = SNAP_THRESHOLD_PX / scale;

        //        float deltaXMM = deltaArrasto.X / scale;
        //        float deltaYMM = deltaArrasto.Y / scale;

        //        // =========================================================
        //        // SNAP MAGNÉTICO: ajusta a posição final se houver linha guia ativa
        //        // =========================================================
        //        if (snapAtivo && linhasGuiaAtivas.Count > 0 && elementoSelecionado != null)
        //        {
        //            float novoX = elementoSelecionado.Bounds.X + deltaXMM;
        //            float novoY = elementoSelecionado.Bounds.Y + deltaYMM;
        //            float w     = elementoSelecionado.Bounds.Width;
        //            float h     = elementoSelecionado.Bounds.Height;

        //            float[] bX = { novoX, novoX + w / 2f, novoX + w };
        //            float[] bY = { novoY, novoY + h / 2f, novoY + h };

        //            var refX = new List<float> { 0f, configuracao.LarguraEtiqueta / 2f, configuracao.LarguraEtiqueta };
        //            var refY = new List<float> { 0f, configuracao.AlturaEtiqueta  / 2f, configuracao.AlturaEtiqueta  };
        //            foreach (var outro in template.Elementos)
        //            {
        //                if (outro == elementoSelecionado) continue;
        //                refX.AddRange(new[] { (float)outro.Bounds.X, outro.Bounds.X + outro.Bounds.Width / 2f, (float)outro.Bounds.Right });
        //                refY.AddRange(new[] { (float)outro.Bounds.Y, outro.Bounds.Y + outro.Bounds.Height / 2f, (float)outro.Bounds.Bottom });
        //            }

        //            // Encontrar o snap mais próximo em X
        //            float melhorDX = float.MaxValue;
        //            float ajusteX  = 0;
        //            for (int i = 0; i < bX.Length; i++)
        //            {
        //                foreach (float rx in refX)
        //                {
        //                    float diff = bX[i] - rx;
        //                    if (Math.Abs(diff) < Math.Abs(melhorDX) && Math.Abs(diff) <= thresholdMM)
        //                    {
        //                        melhorDX = diff;
        //                        // offset para mover novoX de modo que bX[i] == rx
        //                        ajusteX = -(diff);
        //                        if (i == 1) ajusteX += 0; // centro já alinha
        //                    }
        //                }
        //            }

        //            // Encontrar o snap mais próximo em Y
        //            float melhorDY = float.MaxValue;
        //            float ajusteY  = 0;
        //            for (int i = 0; i < bY.Length; i++)
        //            {
        //                foreach (float ry in refY)
        //                {
        //                    float diff = bY[i] - ry;
        //                    if (Math.Abs(diff) < Math.Abs(melhorDY) && Math.Abs(diff) <= thresholdMM)
        //                    {
        //                        melhorDY = diff;
        //                        ajusteY  = -(diff);
        //                    }
        //                }
        //            }

        //            if (Math.Abs(melhorDX) <= thresholdMM) deltaXMM += ajusteX;
        //            if (Math.Abs(melhorDY) <= thresholdMM) deltaYMM += ajusteY;
        //        }
        //        // =========================================================

        //        if (elementoSelecionado != null)
        //        {
        //            elementoSelecionado.Bounds = new Rectangle(
        //                (int)(elementoSelecionado.Bounds.X + deltaXMM),
        //                (int)(elementoSelecionado.Bounds.Y + deltaYMM),
        //                elementoSelecionado.Bounds.Width,
        //                elementoSelecionado.Bounds.Height
        //            );
        //        }

        //        foreach (var elemento in elementosSelecionados)
        //        {
        //            elemento.Bounds = new Rectangle(
        //                (int)(elemento.Bounds.X + deltaXMM),
        //                (int)(elemento.Bounds.Y + deltaYMM),
        //                elemento.Bounds.Width,
        //                elemento.Bounds.Height
        //            );
        //        }

        //        deltaArrasto = Point.Empty;
        //        pbCanvas.Invalidate();
        //    }

        //    // =========================================================
        //    // LIMPAR LINHAS GUIA AO SOLTAR O MOUSE
        //    // =========================================================
        //    linhasGuiaAtivas.Clear();
        //    // =========================================================

        //    arrastando     = false;
        //    redimensionando = false;
        //    rotacionando   = false;
        //    handleSelecionado = -1;
        //    pbCanvas.Cursor = Cursors.Default;
        //    pbCanvas.Invalidate();
        //}
        private void PbCanvas_MouseUp(object sender, MouseEventArgs e)
        {
            RectangleF rectEtiqueta = new RectangleF(25, 25,
                configuracao.LarguraEtiqueta * MM_PARA_PIXEL * zoom,
                configuracao.AlturaEtiqueta * MM_PARA_PIXEL * zoom);
            bool finalizouRedimensionamentoOuRotacao = redimensionando || rotacionando;

            if (selecionandoComRetangulo)
            {
                selecionandoComRetangulo = false;
                elementosSelecionados.Clear();
                foreach (var elemento in template.Elementos)
                {
                    Rectangle bounds = ConverterParaPixels(elemento.Bounds, rectEtiqueta);
                    if (retanguloSelecao.IntersectsWith(bounds))
                        elementosSelecionados.Add(elemento);
                }
                elementoSelecionado = null;
                AtualizarPainelPropriedades();
                pbCanvas.Invalidate();
                return;
            }

            if (arrastando && deltaArrasto != Point.Empty)
            {
                float scale = MM_PARA_PIXEL * zoom;

                // 1. Calculamos quanto o mouse moveu em MM
                float moveXMM = deltaArrasto.X / scale;
                float moveYMM = deltaArrasto.Y / scale;

                // 2. Aplicamos aos elementos usando Round (evita o pulo de 1px)
                if (elementoSelecionado != null)
                {
                    elementoSelecionado.Bounds = new Rectangle(
                        (int)Math.Round(elementoSelecionado.Bounds.X + moveXMM),
                        (int)Math.Round(elementoSelecionado.Bounds.Y + moveYMM),
                        elementoSelecionado.Bounds.Width,
                        elementoSelecionado.Bounds.Height
                    );
                }

                foreach (var el in elementosSelecionados)
                {
                    el.Bounds = new Rectangle(
                        (int)Math.Round(el.Bounds.X + moveXMM),
                        (int)Math.Round(el.Bounds.Y + moveYMM),
                        el.Bounds.Width,
                        el.Bounds.Height
                    );
                }

                // 3. SALVAR HISTÓRICO (Aqui o Ctrl+Z registra a posição final correta)
                SalvarEstadoHistorico();

                // 4. ZERAR O DELTA (Essencial para não somar de novo no próximo Paint)
                deltaArrasto = Point.Empty;
                arrastando = false;
                linhasGuiaAtivas.Clear();

                pbCanvas.Invalidate();
                AtualizarStatusBarSelecao();
            }

            if (finalizouRedimensionamentoOuRotacao)
            {
                SalvarEstadoHistorico();
                AtualizarPainelPropriedades();
            }

            // Limpeza final
            linhasGuiaAtivas.Clear();
            arrastando = false;
            redimensionando = false;
            rotacionando = false;
            handleSelecionado = -1;
            elementoHoverRotacao = null;
            handleSobMouse = -1;
            pbCanvas.Cursor = Cursors.Default;

            if (elementoSelecionado != null)
            {
                Rectangle boundsAtuais = ConverterParaPixels(elementoSelecionado.Bounds, rectEtiqueta);
                int handleAtual = ObterHandleClicado(e.Location, boundsAtuais, elementoSelecionado.Rotacao);
                if (EhHandleRotacao(handleAtual))
                {
                    handleSobMouse = handleAtual;
                    elementoHoverRotacao = elementoSelecionado;
                    pbCanvas.Cursor = Cursors.Hand;
                }
            }

            pbCanvas.Invalidate();
        }

        private void PbCanvas_MouseWheel(object sender, MouseEventArgs e)
        {
            if (ModifierKeys == Keys.Control)
            {
                AlterarZoom(e.Delta > 0 ? 0.1f : -0.1f);
                HandledMouseEventArgs handled = e as HandledMouseEventArgs;
                if (handled != null)
                    handled.Handled = true;
            }
        }
        private void FormEtiqueta_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.Z)
            {
                Desfazer();
                e.SuppressKeyPress = true; // Impede o "beep" do Windows
            }
        }

        #endregion

        #region Eventos de Configuração

        private void CarregarDadosNaInterface()
        {
            cmbImpressora.Items.Clear();
            foreach (string printer in System.Drawing.Printing.PrinterSettings.InstalledPrinters)
                cmbImpressora.Items.Add(printer);

            if (!string.IsNullOrEmpty(configuracao.ImpressoraPadrao) &&
                cmbImpressora.Items.Contains(configuracao.ImpressoraPadrao))
                cmbImpressora.SelectedItem = configuracao.ImpressoraPadrao;
            else if (cmbImpressora.Items.Count > 0)
                cmbImpressora.SelectedIndex = 0;

            CarregarPapeisDaImpressora();
        }

        private void CmbImpressora_SelectedIndexChanged(object sender, EventArgs e)
        {
            CarregarPapeisDaImpressora();
            AtualizarConfiguracao();
        }

        private void CmbPapel_SelectedIndexChanged(object sender, EventArgs e)
        {
            AtualizarConfiguracao();
        }

        private void CarregarPapeisDaImpressora()
        {
            cmbPapel.Items.Clear();
            if (cmbImpressora.SelectedItem == null) return;

            try
            {
                var printerSettings = new System.Drawing.Printing.PrinterSettings
                {
                    PrinterName = cmbImpressora.SelectedItem.ToString()
                };

                foreach (System.Drawing.Printing.PaperSize papel in printerSettings.PaperSizes)
                    cmbPapel.Items.Add(papel.PaperName);

                if (!string.IsNullOrEmpty(configuracao.PapelPadrao) &&
                    cmbPapel.Items.Cast<object>().Any(x => x.ToString() == configuracao.PapelPadrao))
                    cmbPapel.SelectedItem = configuracao.PapelPadrao;
                else if (cmbPapel.Items.Count > 0)
                    cmbPapel.SelectedIndex = 0;
            }
            catch
            {
                cmbPapel.Items.Add("(Erro ao carregar papéis)");
                cmbPapel.SelectedIndex = 0;
            }
        }

        private void ChkPadraoDesativar_CheckedChanged(object sender, EventArgs e)
        {
            AtualizarEstadoMargens();
        }

        private void AtualizarEstadoMargens()
        {
            bool desabilitado = chkPadraoDesativar.Checked;
            numMargemSuperior.Enabled = !desabilitado;
            numMargemInferior.Enabled = !desabilitado;
            numMargemEsquerda.Enabled = !desabilitado;
            numMargemDireita.Enabled  = !desabilitado;

            if (desabilitado)
            {
                numMargemSuperior.Value = 0;
                numMargemInferior.Value = 0;
                numMargemEsquerda.Value = 0;
                numMargemDireita.Value  = 0;
            }
        }

        private void AtualizarConfiguracao()
        {
            configuracao.LarguraEtiqueta       = (float)numLargura.Value;
            configuracao.AlturaEtiqueta        = (float)numAltura.Value;
            configuracao.ImpressoraPadrao      = cmbImpressora.SelectedItem?.ToString() ?? "";
            configuracao.PapelPadrao           = cmbPapel.SelectedItem?.ToString() ?? "";
            configuracao.NumColunas            = (int)numColunas.Value;
            configuracao.NumLinhas             = (int)numLinhas.Value;
            configuracao.EspacamentoColunas    = (float)numEspacamentoColunas.Value;
            configuracao.EspacamentoLinhas     = (float)numEspacamentoLinhas.Value;
            configuracao.MargemSuperior        = (float)numMargemSuperior.Value;
            configuracao.MargemInferior        = (float)numMargemInferior.Value;
            configuracao.MargemEsquerda        = (float)numMargemEsquerda.Value;
            configuracao.MargemDireita         = (float)numMargemDireita.Value;

            template.Largura = configuracao.LarguraEtiqueta;
            template.Altura  = configuracao.AlturaEtiqueta;

            AtualizarTamanhoCanvas();
            pbCanvas?.Invalidate();
            AtualizarStatusDocumento();
        }

        private void AtualizarTamanhoCanvas()
        {
            if (pbCanvas == null || panelCanvas == null) return;

            int larguraPixels = (int)(configuracao.LarguraEtiqueta * MM_PARA_PIXEL * zoom);
            int alturaPixels  = (int)(configuracao.AlturaEtiqueta  * MM_PARA_PIXEL * zoom);

            pbCanvas.Size = new Size(larguraPixels + 50, alturaPixels + 50);

            int posX = pbCanvas.Width  < panelCanvas.Width  ? (panelCanvas.Width  - pbCanvas.Width)  / 2 : 0;
            int posY = pbCanvas.Height < panelCanvas.Height ? (panelCanvas.Height - pbCanvas.Height) / 2 : 0;

            pbCanvas.Location = new Point(posX, posY);
        }

        #endregion

        #region Eventos de Teclado

        private void FormDesignNovo_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.S)
            {
                BtnSalvar_Click(this, EventArgs.Empty);
                e.Handled = true;
                e.SuppressKeyPress = true;
                return;
            }

            if (e.Control && e.KeyCode == Keys.Z)
            {
                Desfazer();
                e.Handled = true;
                e.SuppressKeyPress = true; // Evita o som de "erro" do Windows
                return;
            }

            if (e.Control && !ControleEditavelComFoco())
            {
                if (e.KeyCode == Keys.C)
                    CopiarElementosSelecionados();
                else if (e.KeyCode == Keys.V)
                    ColarElementosCopiados();
                else if (e.KeyCode == Keys.X)
                    RecortarElementosSelecionados();
                else if (e.KeyCode == Keys.D)
                    DuplicarElementosSelecionados();
                else
                    goto ProcessarMovimento;

                e.Handled = true;
                e.SuppressKeyPress = true;
                return;
            }

        ProcessarMovimento:

            List<ElementoEtiqueta> elementosParaMover = new List<ElementoEtiqueta>();

            if (elementosSelecionados.Count > 0) elementosParaMover.AddRange(elementosSelecionados);
            else if (elementoSelecionado != null) elementosParaMover.Add(elementoSelecionado);

            if (elementosParaMover.Count == 0) return;

            bool houveAlteracao = false;
            int passo = 1;

            switch (e.KeyCode)
            {
                case Keys.Left:  foreach (var el in elementosParaMover) { var p = el.Bounds; p.X -= passo; el.Bounds = p; } houveAlteracao = true; break;
                case Keys.Right: foreach (var el in elementosParaMover) { var p = el.Bounds; p.X += passo; el.Bounds = p; } houveAlteracao = true; break;
                case Keys.Up:    foreach (var el in elementosParaMover) { var p = el.Bounds; p.Y -= passo; el.Bounds = p; } houveAlteracao = true; break;
                case Keys.Down:  foreach (var el in elementosParaMover) { var p = el.Bounds; p.Y += passo; el.Bounds = p; } houveAlteracao = true; break;
                case Keys.Delete:
                    ExcluirElementosSelecionados();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    return;
            }

            if (houveAlteracao)
            {
                SalvarEstadoHistorico();
                pbCanvas.Invalidate();
                AtualizarStatusBarSelecao();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            
        }

        #endregion

        #region Botões de Ação

        private void BtnSalvar_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(nomeTemplateAtual))
            {
                using (var formNome = new FormNomeTemplate())
                {
                    if (formNome.ShowDialog() == DialogResult.OK)
                    {
                        nomeTemplateAtual = formNome.NomeTemplate;
                        configuracao.NomeEtiqueta = nomeTemplateAtual;
                    }
                    else return;
                }
            }

            if (TemplateManager.SalvarTemplate(template, nomeTemplateAtual))
            {
                ConfiguracaoManager.SalvarConfiguracao(nomeTemplateAtual, configuracao);
                MessageBox.Show(
                    $"Template '{nomeTemplateAtual}' salvo com sucesso!\n\n" +
                    $"✅ Template: {configuracao.LarguraEtiqueta:F1} x {configuracao.AlturaEtiqueta:F1} mm\n" +
                    $"✅ Elementos: {template.Elementos.Count}\n" +
                    $"✅ Layout: {configuracao.NumColunas} col x {configuracao.NumLinhas} lin\n" +
                    $"✅ Configuração vinculada",
                    "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
            }
            else
            {
                MessageBox.Show("Erro ao salvar template!", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnNovo_Click(object sender, EventArgs e)
        {
            var resultado = MessageBox.Show(
                "Deseja criar um novo template?\n\nAs alterações não salvas serão perdidas.",
                "Novo Template", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (resultado == DialogResult.Yes)
            {
                template = new TemplateEtiqueta { Largura = 100, Altura = 30 };
                configuracao = new ConfiguracaoEtiqueta { LarguraEtiqueta = 100, AlturaEtiqueta = 30, NumColunas = 1, NumLinhas = 1 };
                nomeTemplateAtual = null;
                elementoSelecionado = null;
                CarregarDadosNaInterface();
                AtualizarConfiguracao();
            }
        }

        private void BtnPreview_Click(object sender, EventArgs e)
        {
            float larguraPapel = 210;
            float alturaPapel  = 297;
            string nomePapel   = cmbPapel.SelectedItem?.ToString() ?? "A4";

            if (cmbImpressora.SelectedItem != null)
            {
                try
                {
                    PrinterSettings printerSettings = new PrinterSettings
                    {
                        PrinterName = cmbImpressora.SelectedItem.ToString()
                    };
                    foreach (PaperSize paperSize in printerSettings.PaperSizes)
                    {
                        if (paperSize.PaperName == nomePapel)
                        {
                            larguraPapel = (paperSize.Width  / 100f) * 25.4f;
                            alturaPapel  = (paperSize.Height / 100f) * 25.4f;
                            break;
                        }
                    }
                }
                catch { }
            }

            FormPreview formPreview = new FormPreview(template, configuracao, nomePapel, larguraPapel, alturaPapel);
            formPreview.ShowDialog(this);
        }

        private void BtnFechar_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        private void SalvarEstadoHistorico()
        {
            try
            {
                // Agora o template.SalvarParaXml() existe e retorna uma string JSON
                string snapshot = template.SalvarParaXml();

                if (historicoUndo.Count > 0 && historicoUndo.Peek() == snapshot)
                    return;

                historicoUndo.Push(snapshot);
                if (toolbarInferior?.UndoButton != null)
                    toolbarInferior.UndoButton.Enabled = historicoUndo.Count > 1;

                if (historicoUndo.Count > Max_Undo_Steps)
                {
                    var snapshots = historicoUndo.Reverse().ToList();
                    while (snapshots.Count > Max_Undo_Steps)
                        snapshots.RemoveAt(0);

                    historicoUndo = new Stack<string>(snapshots);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro ao salvar histórico: " + ex.Message);
            }
        }

        private void Desfazer()
        {
            // 1. Verificamos se há estados suficientes para voltar
            // Se só tiver 1, é o estado inicial, não há o que desfazer.
            if (historicoUndo.Count <= 1) return;

            // 2. Removemos o estado ATUAL da pilha (o que está na tela agora)
            historicoUndo.Pop();

            // 3. Pegamos o estado ANTERIOR sem removê-lo (usando Peek)
            // Assim, se apertar Ctrl+Z de novo, o processo se repete
            string snapshotAnterior = historicoUndo.Peek();

            try
            {
                // 4. Restauramos o template com o estado recuperado
                this.template = TemplateEtiqueta.CarregarDeSnapshot(snapshotAnterior);

                // 5. IMPORTANTE: Limpar seleções
                // Como o objeto 'template' mudou, as referências antigas de 'elementoSelecionado' 
                // agora são de um objeto que não existe mais na memória ativa.
                elementoSelecionado = null;
                elementosSelecionados.Clear();

                // 6. Atualizar a interface
                pbCanvas.Invalidate();
                AtualizarPainelPropriedades();
                CarregarDadosNaInterface();
                if (toolbarInferior?.UndoButton != null)
                    toolbarInferior.UndoButton.Enabled = historicoUndo.Count > 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro ao restaurar snapshot: " + ex.Message);
            }
        }

        #endregion

        #region Responsividade e produtividade

        private void FormDesignNovo_Shown(object sender, EventArgs e)
        {
            Screen tela = Owner != null ? Screen.FromControl(Owner) : Screen.PrimaryScreen;
            Rectangle area = tela == null ? Screen.PrimaryScreen.WorkingArea : tela.WorkingArea;

            int largura = Math.Min(EscalarDpi(1400), (int)(area.Width * 0.94f));
            int altura = Math.Min(EscalarDpi(850), (int)(area.Height * 0.92f));
            int centroX = Owner != null ? Owner.Bounds.Left + Owner.Bounds.Width / 2 : area.Left + area.Width / 2;
            int centroY = Owner != null ? Owner.Bounds.Top + Owner.Bounds.Height / 2 : area.Top + area.Height / 2;

            MinimumSize = new Size(
                Math.Min(EscalarDpi(900), (int)(area.Width * 0.75f)),
                Math.Min(EscalarDpi(600), (int)(area.Height * 0.75f)));

            Bounds = new Rectangle(
                Math.Max(area.Left, Math.Min(centroX - largura / 2, area.Right - largura)),
                Math.Max(area.Top, Math.Min(centroY - altura / 2, area.Bottom - altura)),
                largura,
                altura);

            if (!filtroMouseAtivo)
            {
                Application.AddMessageFilter(this);
                filtroMouseAtivo = true;
            }

            AjustarLayoutResponsivo();
        }

        private void RemoverFiltroMouse()
        {
            if (!filtroMouseAtivo)
                return;

            Application.RemoveMessageFilter(this);
            filtroMouseAtivo = false;
        }

        public bool PreFilterMessage(ref Message m)
        {
            const int WmMouseWheel = 0x020A;
            if (m.Msg != WmMouseWheel || ModifierKeys != Keys.Control || pbCanvas == null || !ContainsFocus)
                return false;

            if (!pbCanvas.RectangleToScreen(pbCanvas.ClientRectangle).Contains(Cursor.Position))
                return false;

            short delta = unchecked((short)((m.WParam.ToInt64() >> 16) & 0xffff));
            AlterarZoom(delta > 0 ? 0.1f : -0.1f);
            return true;
        }

        private int EscalarDpi(int valor)
        {
            return Math.Max(1, (int)Math.Round(valor * DeviceDpi / 96f));
        }

        private int ObterLarguraPainelDireito()
        {
            int desejada = EscalarDpi(350);
            int minima = EscalarDpi(260);
            return Math.Min(desejada, Math.Max(minima, (int)(ClientSize.Width * 0.26f)));
        }

        private void AjustarLayoutResponsivo()
        {
            if (panelConfiguracao == null || panelToolbox == null)
                return;

            SuspendLayout();
            try
            {
                if (panelConfiguracao.Width > 0)
                    panelConfiguracao.Width = ObterLarguraPainelDireito();

                int larguraToolbox = Math.Min(
                    EscalarDpi(390),
                    Math.Max(EscalarDpi(220), (int)(ClientSize.Width * 0.28f)));
                panelToolbox.Width = larguraToolbox;

                if (panelPropriedades != null)
                {
                    panelPropriedades.Width = Math.Max(EscalarDpi(190), panelToolbox.ClientSize.Width - EscalarDpi(12));
                    panelPropriedades.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                }

                if (btnToggleConfig != null)
                {
                    int margem = EscalarDpi(5);
                    int x = panelConfiguracao.Width > 0
                        ? ClientSize.Width - panelConfiguracao.Width - btnToggleConfig.Width
                        : ClientSize.Width - btnToggleConfig.Width - margem;
                    btnToggleConfig.Location = new Point(Math.Max(0, x), Math.Max(0, (ClientSize.Height - btnToggleConfig.Height) / 2));
                }

                AtualizarTamanhoCanvas();
            }
            finally
            {
                ResumeLayout(true);
            }
        }

        private void AlterarZoom(float variacao)
        {
            float novoZoom = Math.Max(0.3f, Math.Min(3.0f, zoom + variacao));
            if (Math.Abs(novoZoom - zoom) < 0.001f)
                return;

            elementoHoverRotacao = null;
            handleSobMouse = -1;
            zoom = novoZoom;
            AtualizarControleZoom();
            AtualizarTamanhoCanvas();
            pbCanvas?.Invalidate();
        }

        private void AtualizarControleZoom()
        {
            if (lblZoomPercentual != null)
                lblZoomPercentual.Text = string.Format("{0:0}%", zoom * 100f);

            if (btnZoomMenos != null)
                btnZoomMenos.Enabled = zoom > 0.3f;
            if (btnZoomMais != null)
                btnZoomMais.Enabled = zoom < 3.0f;

            if (toolbarInferior != null)
            {
                toolbarInferior.ZoomLabel.Text = string.Format("{0:0}%", zoom * 100f);
                toolbarInferior.ZoomOutButton.Enabled = zoom > 0.3f;
                toolbarInferior.ZoomInButton.Enabled = zoom < 3.0f;
            }
            statusBarManager?.SetZoom(zoom);
        }

        private void AtualizarStatusDocumento()
        {
            if (configuracao == null) return;
            statusBarManager?.SetDocument(configuracao.LarguraEtiqueta, configuracao.AlturaEtiqueta);
            statusBarManager?.SetZoom(zoom);
        }

        private void AtualizarStatusBarSelecao()
        {
            if (statusBarManager == null) return;
            var selecionados = ObterElementosParaEdicao();
            if (selecionados.Count == 0)
            {
                statusBarManager.SetSelection(string.Empty, Rectangle.Empty, 0);
                return;
            }

            ElementoEtiqueta elemento = selecionados[0];
            string tipo;
            switch (elemento.Tipo)
            {
                case TipoElemento.Texto: tipo = "Texto"; break;
                case TipoElemento.Expressao: tipo = "Expressão"; break;
                case TipoElemento.Imagem: tipo = "Imagem"; break;
                case TipoElemento.Campo: tipo = "Campo"; break;
                case TipoElemento.CodigoBarras: tipo = "Código de barras"; break;
                default: tipo = elemento.Tipo.ToString(); break;
            }
            statusBarManager.SetSelection(tipo, elemento.Bounds, selecionados.Count);
        }

        private void CriarMenuContextoCanvas()
        {
            menuContextoCanvas = new ContextMenuStrip();
            menuContextoCanvas.Items.Add("Copiar", null, (s, e) => CopiarElementosSelecionados());
            menuContextoCanvas.Items.Add("Colar", null, (s, e) => ColarElementosCopiados());
            menuContextoCanvas.Items.Add("Recortar", null, (s, e) => RecortarElementosSelecionados());
            menuContextoCanvas.Items.Add("Duplicar", null, (s, e) => DuplicarElementosSelecionados());
            menuContextoCanvas.Items.Add("Excluir", null, (s, e) => ExcluirElementosSelecionados());
            menuContextoCanvas.Items.Add(new ToolStripSeparator());
            menuContextoCanvas.Items.Add("Trazer para Frente", null, (s, e) => AlterarCamada(CamadaAcao.TrazerFrente));
            menuContextoCanvas.Items.Add("Enviar para Trás", null, (s, e) => AlterarCamada(CamadaAcao.EnviarTras));
            menuContextoCanvas.Items.Add("Avançar uma Camada", null, (s, e) => AlterarCamada(CamadaAcao.Avancar));
            menuContextoCanvas.Items.Add("Recuar uma Camada", null, (s, e) => AlterarCamada(CamadaAcao.Recuar));
            menuContextoCanvas.Opening += (s, e) =>
            {
                bool possuiSelecao = ObterElementosSelecionadosOrdenados().Count > 0;
                foreach (ToolStripItem item in menuContextoCanvas.Items)
                {
                    if (!(item is ToolStripSeparator) && !string.Equals(item.Text, "Colar", StringComparison.OrdinalIgnoreCase))
                        item.Enabled = possuiSelecao;
                }
            };
        }

        private void SelecionarElementoParaMenuContexto(Point ponto)
        {
            ElementoEtiqueta clicado = ObterElementoNoPonto(ponto);
            if (clicado == null || ObterElementosSelecionadosOrdenados().Contains(clicado))
                return;

            elementoSelecionado = clicado;
            elementosSelecionados.Clear();
            AtualizarPainelPropriedades();
            pbCanvas.Invalidate();
        }

        private ElementoEtiqueta ObterElementoNoPonto(Point ponto)
        {
            RectangleF rectEtiqueta = new RectangleF(25, 25,
                configuracao.LarguraEtiqueta * MM_PARA_PIXEL * zoom,
                configuracao.AlturaEtiqueta * MM_PARA_PIXEL * zoom);

            for (int i = template.Elementos.Count - 1; i >= 0; i--)
            {
                ElementoEtiqueta elemento = template.Elementos[i];
                if (PontoEmRetanguloRotacionado(ponto, ConverterParaPixels(elemento.Bounds, rectEtiqueta), elemento.Rotacao))
                    return elemento;
            }

            return null;
        }

        private List<ElementoEtiqueta> ObterElementosSelecionadosOrdenados()
        {
            var selecionados = new HashSet<ElementoEtiqueta>(ObterElementosParaEdicao());
            return template.Elementos.Where(selecionados.Contains).ToList();
        }

        private bool CopiarElementosSelecionados()
        {
            List<ElementoEtiqueta> selecionados = ObterElementosSelecionadosOrdenados();
            if (selecionados.Count == 0)
                return false;

            var temporario = new TemplateEtiqueta
            {
                Largura = template.Largura,
                Altura = template.Altura,
                Elementos = selecionados
            };
            elementosCopiados = temporario.SalvarParaXml();
            sequenciaColagem = 0;

            try
            {
                Clipboard.SetData("SmartPrint.ElementosEtiqueta", elementosCopiados);
            }
            catch
            {
                // O clipboard interno continua disponível em sessões remotas ou bloqueadas.
            }

            return true;
        }

        private string ObterElementosCopiados()
        {
            try
            {
                if (Clipboard.ContainsData("SmartPrint.ElementosEtiqueta"))
                    return Clipboard.GetData("SmartPrint.ElementosEtiqueta") as string ?? elementosCopiados;
            }
            catch
            {
            }

            return elementosCopiados;
        }

        private void ColarElementosCopiados()
        {
            string snapshot = ObterElementosCopiados();
            if (string.IsNullOrWhiteSpace(snapshot))
                return;

            TemplateEtiqueta origem;
            try
            {
                origem = TemplateEtiqueta.CarregarDeSnapshot(snapshot);
            }
            catch
            {
                return;
            }

            if (origem?.Elementos == null || origem.Elementos.Count == 0)
                return;

            SalvarEstadoHistorico();
            sequenciaColagem++;
            int deslocamento = Math.Max(1, sequenciaColagem * 2);
            var novos = new List<ElementoEtiqueta>();

            foreach (ElementoEtiqueta elemento in origem.Elementos)
            {
                Rectangle limites = elemento.Bounds;
                limites.X = Math.Max(0, Math.Min((int)template.Largura - limites.Width, limites.X + deslocamento));
                limites.Y = Math.Max(0, Math.Min((int)template.Altura - limites.Height, limites.Y + deslocamento));
                elemento.Bounds = limites;
                template.Elementos.Add(elemento);
                novos.Add(elemento);
            }

            DefinirSelecao(novos);
            SalvarEstadoHistorico();
            AtualizarPainelPropriedades();
            pbCanvas.Invalidate();
        }

        private void RecortarElementosSelecionados()
        {
            if (CopiarElementosSelecionados())
                ExcluirElementosSelecionados();
        }

        private void DuplicarElementosSelecionados()
        {
            if (CopiarElementosSelecionados())
                ColarElementosCopiados();
        }

        private void ExcluirElementosSelecionados()
        {
            List<ElementoEtiqueta> selecionados = ObterElementosSelecionadosOrdenados();
            if (selecionados.Count == 0)
                return;

            SalvarEstadoHistorico();
            foreach (ElementoEtiqueta elemento in selecionados)
                template.Elementos.Remove(elemento);

            elementoSelecionado = null;
            elementosSelecionados.Clear();
            SalvarEstadoHistorico();
            AtualizarPainelPropriedades();
            pbCanvas.Invalidate();
        }

        private void DefinirSelecao(List<ElementoEtiqueta> elementos)
        {
            elementoSelecionado = elementos.Count == 1 ? elementos[0] : null;
            elementosSelecionados.Clear();
            if (elementos.Count > 1)
                elementosSelecionados.AddRange(elementos);
        }

        private enum CamadaAcao
        {
            TrazerFrente,
            EnviarTras,
            Avancar,
            Recuar
        }

        private void AlterarCamada(CamadaAcao acao)
        {
            List<ElementoEtiqueta> selecionados = ObterElementosSelecionadosOrdenados();
            if (selecionados.Count == 0)
                return;

            SalvarEstadoHistorico();
            var conjunto = new HashSet<ElementoEtiqueta>(selecionados);

            if (acao == CamadaAcao.TrazerFrente || acao == CamadaAcao.EnviarTras)
            {
                template.Elementos.RemoveAll(conjunto.Contains);
                if (acao == CamadaAcao.TrazerFrente)
                    template.Elementos.AddRange(selecionados);
                else
                    template.Elementos.InsertRange(0, selecionados);
            }
            else if (acao == CamadaAcao.Avancar)
            {
                for (int i = template.Elementos.Count - 2; i >= 0; i--)
                {
                    if (conjunto.Contains(template.Elementos[i]) && !conjunto.Contains(template.Elementos[i + 1]))
                    {
                        ElementoEtiqueta temporario = template.Elementos[i];
                        template.Elementos[i] = template.Elementos[i + 1];
                        template.Elementos[i + 1] = temporario;
                    }
                }
            }
            else
            {
                for (int i = 1; i < template.Elementos.Count; i++)
                {
                    if (conjunto.Contains(template.Elementos[i]) && !conjunto.Contains(template.Elementos[i - 1]))
                    {
                        ElementoEtiqueta temporario = template.Elementos[i];
                        template.Elementos[i] = template.Elementos[i - 1];
                        template.Elementos[i - 1] = temporario;
                    }
                }
            }

            SalvarEstadoHistorico();
            pbCanvas.Invalidate();
        }

        private bool ControleEditavelComFoco()
        {
            Control controle = ActiveControl;
            while (controle is ContainerControl container && container.ActiveControl != null)
                controle = container.ActiveControl;

            return controle is TextBoxBase || controle is ComboBox || controle is NumericUpDown;
        }

        private sealed class DoubleBufferedPictureBox : PictureBox
        {
            public DoubleBufferedPictureBox()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.UserPaint |
                         ControlStyles.OptimizedDoubleBuffer |
                         ControlStyles.ResizeRedraw, true);
                UpdateStyles();
            }
        }

        private sealed class DoubleBufferedPanel : Panel
        {
            public DoubleBufferedPanel()
            {
                DoubleBuffered = true;
                ResizeRedraw = true;
            }
        }

        #endregion

        #region Propriedades Públicas

        public TemplateEtiqueta ObterTemplate() => template;
        public ConfiguracaoEtiqueta ObterConfiguracao() => configuracao;

        #endregion
    }
}
