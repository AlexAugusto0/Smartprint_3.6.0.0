using System;
using System.Windows.Forms;
using System.Threading.Tasks;
using EtiquetaFORNew.Data;

namespace EtiquetaFORNew
{
    public partial class FormSincronizacaoSoftcomShop : Form
    {
        private ConfiguracaoSistema _config;
        private SoftcomShopDataManager _dataManager;
        private string _connectionString;

        public FormSincronizacaoSoftcomShop()
        {
            InitializeComponent();
        }

        private void FormSincronizacaoSoftcomShop_Load(object sender, EventArgs e)
        {
            // Carregar configuraÃ§Ãµes
            _config = ConfiguracaoSistema.Carregar();

            // Verificar se SoftcomShop estÃ¡ configurado
            if (!_config.SoftcomShopConfigurado())
            {
                MessageBox.Show(
                    "SoftcomShop não está¡ configurado!\n\n" +
                    "Configure em: Menu > Configurações",
                    "Atenção",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                this.Close();
                return;
            }

            // Obter connection string do SQLite
            _connectionString = LocalDatabaseManager.GetConnectionString();
            //_connectionString = $"Data Source={_config.SoftcomShop.CaminhoBancoDados};Version=3;";

            // Criar gerenciador de dados
            _dataManager = new SoftcomShopDataManager(_config.SoftcomShop, _connectionString);

            // Atualizar status
            AtualizarStatus();
        }

        private void AtualizarStatus()
        {
            if (_config.SoftcomShop.DataSync != null && !string.IsNullOrEmpty(_config.SoftcomShop.DataSync))
            {
                string dataFormatada = FormatarDataUnix(_config.SoftcomShop.DataSync);
                lblUltimaSinc.Text = $"última sincronização: {dataFormatada}";
            }
            else
            {
                lblUltimaSinc.Text = "Nenhuma sincronização realizada";
            }
        }
        private string FormatarDataUnix(string unixTimeStamp)
        {
            try
            {
                if (double.TryParse(unixTimeStamp, out double seconds))
                {
                    // Converte segundos para DateTime (UTC)
                    DateTime data = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(seconds);

                    // Converte para o horário local (Brasília) e formata
                    return data.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
                }
                return unixTimeStamp; // Retorna o original se não for número
            }
            catch
            {
                return "Data inválida";
            }
        }

        #region Eventos dos Botões

        private async void btnSincronizarProdutos_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "Deseja sincronizar TODOS os produtos?\n\n" +
                "Isso pode demorar alguns minutos dependendo da quantidade de produtos.",
                "Confirmar Sincronização",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
                return;

            await SincronizarProdutosAsync();
        }

        private async Task SincronizarProdutosAsync()
        {
            try
            {
                // Desabilitar botÃµes
                HabilitarBotoes(false);

                // Criar progress
                var progress = new Progress<string>(mensagem =>
                {
                    lblStatus.Text = mensagem;
                    Application.DoEvents();
                });

                lblStatus.Text = "Sincronizando produtos...";
                progressBar.Style = ProgressBarStyle.Marquee;
                progressBar.Visible = true;

                // Sincronizar
                //var syncResult = await _dataManager.SincronizarProdutosAsync("v2", progress);

                var syncResult = await _dataManager.SincronizarProdutosAsync("v2", progress);

                // ⭐ NOVO - sincronizar promoções logo após produtos
                //progress.Report("Sincronizando promoções...");                
                if (syncResult.Sucesso)
                {
                    // ⭐ ISSO AQUI É O QUE ESTAVA FALTANDO CHAMAR!
                    lblStatus.Text = "Sincronizando Promoções Ativas...";
                    await _dataManager.SincronizarPromocoesAtivasAsync();

                    // Define OK para o form principal saber que deve recarregar os combos
                    this.DialogResult = DialogResult.OK;
                }

                // â­ REGISTRAR USO DO SISTEMA
                await RegistrarUsoSistemaAsync();

                // Mostrar resultado
                progressBar.Visible = false;

                if (syncResult.Sucesso)
                {
                    MessageBox.Show(
                        $"Sincronização concluída com sucesso!\n\n" +
                        $"Produtos sincronizados: {syncResult.ProdutosAdicionados}",
                        "Sucesso",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);

                    AtualizarStatus();
                    lblStatus.Text = "Pronto";
                }
                else
                {
                    MessageBox.Show(
                        $"Erro ao sincronizar:\n\n{syncResult.MensagemErro}",
                        "Erro",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    lblStatus.Text = "Erro na sincronização";
                }
            }
            catch (Exception ex)
            {
                progressBar.Visible = false;
                MessageBox.Show(
                    $"Erro inesperado:\n\n{ex.Message}",
                    "Erro",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                lblStatus.Text = "Erro";
            }
            finally
            {
                HabilitarBotoes(true);
            }
        }

        private async void btnBuscarNotaFiscal_Click(object sender, EventArgs e)
        {
            // Criar formulÃ¡rio de entrada
            using (var form = new FormBuscarNotaFiscal())
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    await BuscarNotaFiscalAsync(form.DataEntrada, form.NumeroNota);
                }
            }
        }

        private async Task BuscarNotaFiscalAsync(DateTime dataEntrada, int numeroNota)
        {
            try
            {
                HabilitarBotoes(false);

                var progress = new Progress<string>(mensagem =>
                {
                    lblStatus.Text = mensagem;
                    Application.DoEvents();
                });

                lblStatus.Text = "Buscando nota fiscal...";
                progressBar.Style = ProgressBarStyle.Marquee;
                progressBar.Visible = true;

                var syncResult = await _dataManager.BuscarPorNotaFiscalAsync(dataEntrada, numeroNota, "v2", progress);

                progressBar.Visible = false;

                if (syncResult.Sucesso)
                {
                    MessageBox.Show(
                        $"Produtos carregados com sucesso!\n\n" +
                        $"Total: {syncResult.ProdutosAdicionados} produtos\n\n" +
                        $"Os produtos foram marcados para impressão de etiquetas.",
                        "Sucesso",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);

                    lblStatus.Text = "Pronto";
                }
                else
                {
                    MessageBox.Show(
                        syncResult.MensagemErro,
                        "Atenção",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    lblStatus.Text = "Nenhum produto encontrado";
                }
            }
            catch (Exception ex)
            {
                //progressBar.Visible = false;
                //MessageBox.Show(
                //    $"Erro ao buscar nota fiscal:\n\n{ex.Message}",
                //    "Erro",
                //    MessageBoxButtons.OK,
                //    MessageBoxIcon.Error);

                //lblStatus.Text = "Erro";

                progressBar.Visible = false;
                // ex.ToString() traz a StackTrace detalhada com a linha do erro
                MessageBox.Show(
                    $"Erro detalhado:\n\n{ex.ToString()}",
                    "Erro de Conversão",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                lblStatus.Text = "Erro";
            }
            finally
            {
                HabilitarBotoes(true);
            }
        }

        private async void btnBuscarVenda_Click(object sender, EventArgs e)
        {
            string input = Prompt.ShowDialog("Informe o número da venda:", "Buscar Venda");

            if (string.IsNullOrWhiteSpace(input))
                return;

            if (!int.TryParse(input, out int numeroVenda))
            {
                MessageBox.Show("Número inválido!", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            await BuscarVendaAsync(numeroVenda);
        }

        private async Task BuscarVendaAsync(int numeroVenda)
        {
            try
            {
                HabilitarBotoes(false);

                var progress = new Progress<string>(mensagem =>
                {
                    lblStatus.Text = mensagem;
                    Application.DoEvents();
                });

                lblStatus.Text = $"Buscando venda {numeroVenda}...";
                progressBar.Style = ProgressBarStyle.Marquee;
                progressBar.Visible = true;

                var syncResult = await _dataManager.BuscarPorVendaAsync(numeroVenda, progress);

                progressBar.Visible = false;

                if (syncResult.Sucesso)
                {
                    MessageBox.Show(
                        $"Produtos da venda carregados com sucesso!\n\n" +
                        $"Total: {syncResult.ProdutosAdicionados} produtos\n\n" +
                        $"Os produtos foram marcados para impressão de etiquetas.",
                        "Sucesso",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);

                    lblStatus.Text = "Pronto";
                }
                else
                {
                    MessageBox.Show(
                        syncResult.MensagemErro,
                        "Atenção",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    lblStatus.Text = "Venda não encontrada";
                }
            }
            catch (Exception ex)
            {
                progressBar.Visible = false;
                MessageBox.Show(
                    $"Erro ao buscar venda:\n\n{ex.Message}",
                    "Erro",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                lblStatus.Text = "Erro";
            }
            finally
            {
                HabilitarBotoes(true);
            }
        }

        private void btnFechar_Click(object sender, EventArgs e)
        {
            // â­ Define DialogResult como OK para sinalizar sucesso
            // Isso permite que FormPrincipal recarregue as comboboxes automaticamente
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        #endregion

        #region Métodos Auxiliares

        private void HabilitarBotoes(bool habilitar)
        {
            btnSincronizarProdutos.Enabled = habilitar;
            btnBuscarNotaFiscal.Enabled = true;
            btnBuscarVenda.Enabled = false;
            btnFechar.Enabled = habilitar;
        }

        /// <summary>
        /// ­ NOVO: Registra o uso do sistema no servidor (contabilizão de clientes)
        /// </summary>
        private async Task RegistrarUsoSistemaAsync()
        {
            try
            {
                if (_config == null || _config.SoftcomShop == null)
                    return;

                string cnpj = _config.SoftcomShop.CompanyCNPJ ?? "";
                string fantasia = _config.SoftcomShop.CompanyName ?? "";
                // ClientId é o identificador real da empresa no servidor Softcom.
                // DeviceId é apenas um ID local de máquina gerado automaticamente e não é reconhecido
                // pelo wsRegistro como CodigoSuporte válido (ao contrário do modo SQL Server
                // que busca CodigoSuporte direto da tabela Integrar_Lojas).
                string codigoSuporte = _config.SoftcomShop.ClientId ?? "";

                // Validar dados mí­nimos
                if (string.IsNullOrEmpty(cnpj) || string.IsNullOrEmpty(fantasia) || string.IsNullOrEmpty(codigoSuporte))
                {
                    System.Diagnostics.Debug.WriteLine("[REGISTRO SOFTCOMSHOP] Dados incompletos para registro");
                    return;
                }

                // Chamar funÃ§Ã£o de registro
                System.Diagnostics.Debug.WriteLine($"[REGISTRO SOFTCOMSHOP] Registrando uso: {fantasia} - CNPJ: {cnpj}");

                string resultado = await DatabaseConfig.GetSetRegistroJsonAsync(codigoSuporte, cnpj, fantasia);

                System.Diagnostics.Debug.WriteLine($"[REGISTRO SOFTCOMSHOP] Resultado: {resultado}");
            }
            catch (Exception ex)
            {
                // NÃ£o exibir erro ao usuÃ¡rio, apenas logar
                System.Diagnostics.Debug.WriteLine($"[REGISTRO SOFTCOMSHOP] Erro ao registrar uso: {ex.Message}");
            }
        }

        #endregion
    }

    /// <summary>
    /// Classe auxiliar para InputBox simples
    /// </summary>
    public static class Prompt
    {
        public static string ShowDialog(string text, string caption)
        {
            Form prompt = new Form()
            {
                Width = 400,
                Height = 150,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = caption,
                StartPosition = FormStartPosition.CenterScreen,
                MaximizeBox = false,
                MinimizeBox = false
            };

            Label textLabel = new Label() { Left = 20, Top = 20, Text = text, Width = 350 };
            TextBox textBox = new TextBox() { Left = 20, Top = 50, Width = 350 };
            Button confirmation = new Button() { Text = "OK", Left = 220, Width = 70, Top = 80, DialogResult = DialogResult.OK };
            Button cancel = new Button() { Text = "Cancelar", Left = 300, Width = 70, Top = 80, DialogResult = DialogResult.Cancel };

            confirmation.Click += (sender, e) => { prompt.Close(); };
            cancel.Click += (sender, e) => { prompt.Close(); };

            prompt.Controls.Add(textLabel);
            prompt.Controls.Add(textBox);
            prompt.Controls.Add(confirmation);
            prompt.Controls.Add(cancel);
            prompt.AcceptButton = confirmation;
            prompt.CancelButton = cancel;

            return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : "";
        }
    }

    /// <summary>
    /// FormulÃ¡rio auxiliar para entrada de dados da nota fiscal
    /// </summary>
    public class FormBuscarNotaFiscal : Form
    {
        private DateTimePicker dtpDataEntrada;
        private TextBox txtNumeroNota;
        private Button btnOK;
        private Button btnCancelar;
        private Label lblData;
        private Label lblNumero;

        public DateTime DataEntrada { get; private set; }
        public int NumeroNota { get; private set; }

        public FormBuscarNotaFiscal()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            // Form
            this.Text = "Buscar Nota Fiscal";
            this.Size = new System.Drawing.Size(400, 200);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Label Data
            lblData = new Label();
            lblData.Text = "Data de Entrada:";
            lblData.Location = new System.Drawing.Point(20, 20);
            lblData.Size = new System.Drawing.Size(120, 20);
            this.Controls.Add(lblData);

            // DateTimePicker
            dtpDataEntrada = new DateTimePicker();
            dtpDataEntrada.Location = new System.Drawing.Point(20, 45);
            dtpDataEntrada.Size = new System.Drawing.Size(340, 20);
            dtpDataEntrada.Format = DateTimePickerFormat.Short;
            this.Controls.Add(dtpDataEntrada);

            // Label Numero
            lblNumero = new Label();
            lblNumero.Text = "Número da Nota (opcional):";
            lblNumero.Location = new System.Drawing.Point(20, 80);
            lblNumero.Size = new System.Drawing.Size(200, 20);
            this.Controls.Add(lblNumero);

            // TextBox Numero
            txtNumeroNota = new TextBox();
            txtNumeroNota.Location = new System.Drawing.Point(20, 105);
            txtNumeroNota.Size = new System.Drawing.Size(200, 20);
            this.Controls.Add(txtNumeroNota);

            // BotÃ£o OK
            btnOK = new Button();
            btnOK.Text = "OK";
            btnOK.Location = new System.Drawing.Point(195, 135);
            btnOK.Size = new System.Drawing.Size(80, 25);
            btnOK.DialogResult = DialogResult.OK;
            btnOK.Click += BtnOK_Click;
            this.Controls.Add(btnOK);

            // BotÃ£o Cancelar
            btnCancelar = new Button();
            btnCancelar.Text = "Cancelar";
            btnCancelar.Location = new System.Drawing.Point(280, 135);
            btnCancelar.Size = new System.Drawing.Size(80, 25);
            btnCancelar.DialogResult = DialogResult.Cancel;
            this.Controls.Add(btnCancelar);

            this.AcceptButton = btnOK;
            this.CancelButton = btnCancelar;
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            DataEntrada = dtpDataEntrada.Value;

            if (string.IsNullOrWhiteSpace(txtNumeroNota.Text))
            {
                MessageBox.Show("Por favor, informe o número da Nota Fiscal.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None; // Impede o fechamento do Form
                return;
            }

            if (int.TryParse(txtNumeroNota.Text, out int numero))
            {
                NumeroNota = numero;
            }
            else
            {
                MessageBox.Show("O número da nota deve conter apenas algarismos numéricos.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
                return;
            }
        }
    }
}