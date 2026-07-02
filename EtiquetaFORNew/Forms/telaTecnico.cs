using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Management;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EtiquetaFORNew
{
    public partial class telaTecnico : Form
    {
        private List<ImpressoraInfo> impressoras = new List<ImpressoraInfo>();
        private DriverInstaller driverInstaller;
        private Timer timerAtualizacao;
        private Timer timerAnimacaoPainel;
        private Label lblStatusProcessamento;
        private ProgressBar progressBarProcessamento;
        private Color corOriginalGroupBoxDeteccao;
        private Color corOriginalPanelConteudo;
        private bool painelProcessando;
        private bool atualizacaoListaEmAndamento;
        private int passoAnimacaoPainel;

        public telaTecnico()
        {
            InitializeComponent();

            // Inicializa o instalador de drivers
            driverInstaller = new DriverInstaller(this);
            driverInstaller.StatusAtualizado += DriverInstaller_StatusAtualizado;

            CarregarImpressoras();
            VersaoHelper.DefinirTituloComVersao(this, "Instalação de Drivers");
            InicializarListView();
            InicializarPainelProcessamento();
            InicializarAnimacaoPainel();

            // Timer para atualizar lista periodicamente
            InicializarTimer();

            // Aplica efeitos hover nos botões
            AplicarEfeitosHover();
        }

        private void InicializarTimer()
        {
            // Timer para atualizar a lista a cada 3 segundos (quando visível)
            timerAtualizacao = new Timer();
            timerAtualizacao.Interval = 3000; // 3 segundos
            timerAtualizacao.Tick += TimerAtualizacao_Tick;
        }

        private void InicializarPainelProcessamento()
        {
            corOriginalGroupBoxDeteccao = groupBoxDeteccao.BackColor;
            corOriginalPanelConteudo = panelConteudo.BackColor;

            lblStatusProcessamento = new Label
            {
                Text = "Aguardando detecção.",
                AutoEllipsis = true,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(52, 73, 94),
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };

            progressBarProcessamento = new ProgressBar
            {
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                Style = ProgressBarStyle.Continuous,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };

            groupBoxDeteccao.Controls.Add(lblStatusProcessamento);
            groupBoxDeteccao.Controls.Add(progressBarProcessamento);
            groupBoxDeteccao.Resize += (s, e) => AjustarLayoutPainelProcessamento();
            AjustarLayoutPainelProcessamento();
        }

        private void AjustarLayoutPainelProcessamento()
        {
            int margem = 15;
            int largura = Math.Max(100, groupBoxDeteccao.ClientSize.Width - (margem * 2));
            int topoBotoes = Math.Max(285, groupBoxDeteccao.ClientSize.Height - 67);

            btnProcurar.Top = topoBotoes;
            btnInstalarDriver.Top = topoBotoes;

            int alturaLista = Math.Max(120, topoBotoes - listViewDispositivos.Top - 42);
            listViewDispositivos.Height = alturaLista;
            listViewDispositivos.Width = largura;

            lblStatusProcessamento.Location = new Point(margem, listViewDispositivos.Bottom + 6);
            lblStatusProcessamento.Size = new Size(largura, 18);

            progressBarProcessamento.Location = new Point(margem, lblStatusProcessamento.Bottom + 3);
            progressBarProcessamento.Size = new Size(largura, 10);
        }

        private void InicializarAnimacaoPainel()
        {
            timerAnimacaoPainel = new Timer();
            timerAnimacaoPainel.Interval = 420;
            timerAnimacaoPainel.Tick += (s, e) => AlternarDestaquePainel();
        }

        private void DriverInstaller_StatusAtualizado(object sender, DriverInstallStatusEventArgs e)
        {
            AtualizarStatusProcessamento(e.Mensagem, e.Progresso, !e.Finalizado);
        }

        private void AtualizarStatusProcessamento(string mensagem, int? progresso = null, bool manterProcessando = true)
        {
            if (IsDisposed)
                return;

            if (InvokeRequired)
            {
                if (IsHandleCreated)
                    BeginInvoke(new Action(() => AtualizarStatusProcessamento(mensagem, progresso, manterProcessando)));
                return;
            }

            if (lblStatusProcessamento != null && !string.IsNullOrWhiteSpace(mensagem))
                lblStatusProcessamento.Text = mensagem;

            if (progressBarProcessamento != null && progresso.HasValue)
                progressBarProcessamento.Value = Math.Max(0, Math.Min(100, progresso.Value));

            if (manterProcessando)
            {
                IniciarAnimacaoPainel();
                AlternarDestaquePainel();
            }
            else
            {
                FinalizarAnimacaoPainel();
            }
        }

        private void IniciarAnimacaoPainel()
        {
            painelProcessando = true;
            if (timerAnimacaoPainel != null && !timerAnimacaoPainel.Enabled)
                timerAnimacaoPainel.Start();
        }

        private void FinalizarAnimacaoPainel()
        {
            painelProcessando = false;
            if (timerAnimacaoPainel != null)
                timerAnimacaoPainel.Stop();

            groupBoxDeteccao.BackColor = corOriginalGroupBoxDeteccao;
            panelConteudo.BackColor = corOriginalPanelConteudo;
        }

        private void AlternarDestaquePainel()
        {
            if (!painelProcessando)
                return;

            passoAnimacaoPainel++;
            bool destacar = passoAnimacaoPainel % 2 == 0;

            groupBoxDeteccao.BackColor = destacar
                ? Color.FromArgb(255, 248, 222)
                : corOriginalGroupBoxDeteccao;

            panelConteudo.BackColor = destacar
                ? Color.FromArgb(255, 253, 242)
                : corOriginalPanelConteudo;
        }

        private void TimerAtualizacao_Tick(object sender, EventArgs e)
        {
            // Só atualiza se a lista estiver visível e tiver itens
            if (groupBoxDeteccao.Visible && listViewDispositivos.Items.Count > 0 && !atualizacaoListaEmAndamento)
            {
                AtualizarListaDispositivos();
            }
        }

        private void AplicarEfeitosHover()
        {
            // Efeito hover para checkBox1 (Detecção Automática)
            checkBox1.MouseEnter += (s, e) =>
            {
                if (!checkBox1.Checked)
                    checkBox1.BackColor = Color.FromArgb(189, 224, 254);
            };
            checkBox1.MouseLeave += (s, e) =>
            {
                if (!checkBox1.Checked)
                    checkBox1.BackColor = Color.FromArgb(236, 240, 241);
            };

            // Efeito hover para checkBox2 (Instalação Manual)
            checkBox2.MouseEnter += (s, e) =>
            {
                if (!checkBox2.Checked)
                    checkBox2.BackColor = Color.FromArgb(162, 238, 195);
            };
            checkBox2.MouseLeave += (s, e) =>
            {
                if (!checkBox2.Checked)
                    checkBox2.BackColor = Color.FromArgb(236, 240, 241);
            };

            // Efeito hover para botões
            //AplicarHoverBotao(btnProcurar, Color.FromArgb(52, 152, 219), Color.FromArgb(41, 128, 185));
            //AplicarHoverBotao(btnInstalarDriver, Color.FromArgb(46, 204, 113), Color.FromArgb(39, 174, 96));
            //AplicarHoverBotao(btnDownloadDriver, Color.FromArgb(46, 204, 113), Color.FromArgb(39, 174, 96));
        }

        private void AplicarHoverBotao(Button btn, Color corNormal, Color corHover)
        {
            btn.MouseEnter += (s, e) => { btn.BackColor = corHover; };
            btn.MouseLeave += (s, e) => { btn.BackColor = corNormal; };
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                checkBox2.Checked = false;

                // Mostra modo de detecção automática
                groupBoxDeteccao.Visible = true;
                groupBoxManual.Visible = false;

                // Atualiza cor do botão selecionado
                checkBox1.ForeColor = Color.White;
                checkBox2.ForeColor = Color.FromArgb(52, 73, 94);

                // Inicia timer de atualização
                timerAtualizacao.Start();
            }
            else
            {
                groupBoxDeteccao.Visible = false;
                checkBox1.ForeColor = Color.FromArgb(52, 73, 94);

                // Para timer
                timerAtualizacao.Stop();
            }
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox2.Checked)
            {
                checkBox1.Checked = false;

                // Mostra modo manual
                groupBoxManual.Visible = true;
                groupBoxDeteccao.Visible = false;

                // Atualiza cor do botão selecionado
                checkBox2.ForeColor = Color.White;
                checkBox1.ForeColor = Color.FromArgb(52, 73, 94);

                // Para timer
                timerAtualizacao.Stop();
            }
            else
            {
                groupBoxManual.Visible = false;
                checkBox2.ForeColor = Color.FromArgb(52, 73, 94);
            }
        }

        private void CarregarImpressoras()
        {
            try
            {
                impressoras = ImpressoraManager.CarregarImpressoras();

                if (impressoras == null || impressoras.Count == 0)
                {
                    MessageBox.Show(
                        "Nenhuma impressora foi carregada. Verifique o arquivo impressoras.json",
                        "Aviso",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    impressoras = new List<ImpressoraInfo>();
                    return;
                }

                comboBox1.Items.Clear();
                foreach (var imp in impressoras)
                    comboBox1.Items.Add(imp.Nome);

                comboBox1.SelectedIndexChanged -= comboBox1_SelectedIndexChanged;
                comboBox1.SelectedIndexChanged += comboBox1_SelectedIndexChanged;

                if (comboBox1.Items.Count > 0)
                    comboBox1.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erro ao carregar impressoras: {ex.Message}",
                    "Erro",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox1.SelectedItem == null) return;

            string selecionada = comboBox1.SelectedItem.ToString();
            var info = impressoras.Find(i => i.Nome == selecionada);

            if (info != null)
            {
                try
                {
                    if (pictureBox1.Image != null)
                    {
                        var imagemAnterior = pictureBox1.Image;
                        pictureBox1.Image = null;
                        imagemAnterior.Dispose();
                    }

                    pictureBox1.Image = info.ObterImagem();

                    if (pictureBox1.Image == null)
                    {
                        pictureBox1.SizeMode = PictureBoxSizeMode.CenterImage;
                    }
                    else
                    {
                        pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Erro ao carregar imagem: {ex.Message}",
                        "Aviso",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }

                btnDownloadDriver.Tag = info;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (btnDownloadDriver.Tag is ImpressoraInfo impressora)
            {
                AtualizarStatusProcessamento($"Instalando driver selecionado manualmente: {impressora.Nome}", 20);
                driverInstaller.BaixarEInstalarDriver(impressora);
            }
            else
            {
                MessageBox.Show(
                    "Selecione uma impressora primeiro.",
                    "Aviso",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }

        private void InicializarListView()
        {
            listViewDispositivos.View = View.Details;
            listViewDispositivos.FullRowSelect = true;
            listViewDispositivos.GridLines = true;
            listViewDispositivos.Columns.Clear();
            listViewDispositivos.Columns.Add("Nome", 280);
            listViewDispositivos.Columns.Add("Fabricante", 120);
            listViewDispositivos.Columns.Add("Status do Driver", 140);
            listViewDispositivos.Columns.Add("Device ID", 240);
        }

        /// <summary>
        /// Verifica o status detalhado do driver de um dispositivo
        /// </summary>
        private string VerificarStatusDriver(ManagementObject device)
        {
            try
            {
                string deviceId = device["DeviceID"]?.ToString() ?? "";
                string nome = device["Name"]?.ToString() ?? "";

                // Verifica se a impressora está instalada no Windows
                bool impressoraInstalada = VerificarImpressoraInstalada(nome, deviceId);

                // ConfigManagerErrorCode: 0 = OK, outros valores = problema
                object errorCodeObj = device["ConfigManagerErrorCode"];
                int? errorCode = null;

                if (errorCodeObj != null)
                {
                    if (errorCodeObj is int)
                        errorCode = (int)errorCodeObj;
                    else if (errorCodeObj is uint)
                        errorCode = Convert.ToInt32((uint)errorCodeObj);
                }

                // Status do dispositivo
                string status = device["Status"]?.ToString();

                // Nome do driver
                string driverName = device["Service"]?.ToString();

                // Verifica se tem driver instalado
                bool temDriverName = !string.IsNullOrEmpty(driverName);

                // Se a impressora está instalada no Windows, considera instalado
                if (impressoraInstalada)
                {
                    return "✓ Instalado";
                }

                // Análise detalhada
                if (errorCode.HasValue && errorCode.Value == 0 && status == "OK" && temDriverName)
                {
                    return "✓ Instalado";
                }
                else if (errorCode.HasValue && errorCode.Value == 28) // Driver não instalado
                {
                    return "✗ Sem driver";
                }
                else if (errorCode.HasValue && errorCode.Value == 1) // Configuração incorreta
                {
                    return "⚠ Configuração incorreta";
                }
                else if (errorCode.HasValue && errorCode.Value == 10) // Dispositivo não iniciado
                {
                    return "⚠ Não iniciado";
                }
                else if (errorCode.HasValue && errorCode.Value == 22) // Desabilitado
                {
                    return "⊘ Desabilitado";
                }
                else if (errorCode.HasValue && errorCode.Value != 0)
                {
                    return $"✗ Erro ({errorCode.Value})";
                }
                else if (!temDriverName)
                {
                    return "✗ Driver ausente";
                }
                else if (!errorCode.HasValue)
                {
                    // Se errorCode é null, verifica outros indicadores
                    if (temDriverName && status == "OK")
                        return "✓ Instalado";
                    else if (!temDriverName)
                        return "✗ Sem driver";
                    else
                        return "⚠ Status desconhecido";
                }
                else
                {
                    return "⚠ Status desconhecido";
                }
            }
            catch
            {
                return "? Indeterminado";
            }
        }

        /// <summary>
        /// Verifica se a impressora está instalada consultando Win32_Printer
        /// </summary>
        private bool VerificarImpressoraInstalada(string nomeDispositivo, string deviceId)
        {
            try
            {
                // Remove "(U)" e outros sufixos do nome para comparação
                string nomeParaComparar = nomeDispositivo.Replace("(U)", "").Trim();

                // Consulta impressoras instaladas
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                    "SELECT * FROM Win32_Printer");

                foreach (ManagementObject printer in searcher.Get())
                {
                    string printerName = printer["Name"]?.ToString() ?? "";
                    string portName = printer["PortName"]?.ToString() ?? "";

                    // Verifica se o nome corresponde
                    if (printerName.IndexOf(nomeParaComparar, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return true;
                    }

                    // Verifica se a porta USB corresponde ao device ID
                    if (portName.StartsWith("USB", StringComparison.OrdinalIgnoreCase) &&
                        deviceId.IndexOf(portName, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return true;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Obtém o fabricante do dispositivo
        /// </summary>
        private string ObterFabricante(ManagementObject device)
        {
            try
            {
                string manufacturer = device["Manufacturer"]?.ToString();

                if (!string.IsNullOrEmpty(manufacturer))
                {
                    // Remove "(Standard printer)" e textos comuns
                    manufacturer = manufacturer.Replace("(Standard printer)", "").Trim();
                    return manufacturer;
                }

                // Tenta extrair do nome do dispositivo
                string nome = device["Name"]?.ToString() ?? "";
                string[] marcas = { "Elgin", "Zebra", "Argox", "Epson", "HP", "Canon",
                                   "Brother", "Samsung", "Xerox", "Lexmark", "Dell",
                                   "Kyocera", "Ricoh", "Toshiba", "C3Tech", "Tanca",
                                   "Tomate", "SNBC", "Bematech", "Knup", "Coibel" };

                foreach (var marca in marcas)
                {
                    if (nome.IndexOf(marca, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return marca;
                    }
                }

                return "Desconhecido";
            }
            catch
            {
                return "Desconhecido";
            }
        }

        /// <summary>
        /// Obtém cor baseada no status
        /// </summary>
        private Color ObterCorStatus(string status)
        {
            if (status.StartsWith("✓"))
                return Color.FromArgb(39, 174, 96);  // Verde - OK
            else if (status.StartsWith("✗"))
                return Color.FromArgb(231, 76, 60);  // Vermelho - Erro
            else if (status.StartsWith("⚠"))
                return Color.FromArgb(243, 156, 18); // Laranja - Aviso
            else if (status.StartsWith("⊘"))
                return Color.FromArgb(149, 165, 166); // Cinza - Desabilitado
            else
                return Color.FromArgb(52, 73, 94);   // Azul escuro - Desconhecido
        }

        private async Task BuscarDispositivosDeImpressorasAsync()
        {
            if (atualizacaoListaEmAndamento)
                return;

            try
            {
                atualizacaoListaEmAndamento = true;
                btnProcurar.Enabled = false;
                AtualizarStatusProcessamento("Procurando impressoras USB conectadas...", 10);

                List<PrinterDeviceInfo> dispositivos = await Task.Run(() => EnumerarDispositivosUsb());
                PopularListaDispositivos(dispositivos, null);

                int encontrados = dispositivos.Count;
                int comProblema = dispositivos.Count(d =>
                    (d.StatusDriver ?? string.Empty).StartsWith("✗") ||
                    (d.StatusDriver ?? string.Empty).StartsWith("⚠"));

                AtualizarStatusProcessamento(
                    encontrados == 0
                        ? "Nenhuma impressora USB detectada."
                        : $"{encontrados} impressora(s) USB encontrada(s).",
                    100,
                    false);

                if (encontrados == 0)
                {
                    MessageBox.Show(
                        "Nenhuma impressora USB detectada.\n\n" +
                        "Verifique se:\n" +
                        "• A impressora está ligada\n" +
                        "• O cabo USB está conectado\n" +
                        "• O Windows detectou o dispositivo",
                        "Nenhuma Impressora Encontrada",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                else
                {
                    string mensagem = $"{encontrados} impressora(s) USB encontrada(s).";

                    if (comProblema > 0)
                    {
                        mensagem += $"\n\n{comProblema} com problema de driver.";
                    }

                    MessageBox.Show(mensagem, "Busca Concluída",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                AtualizarStatusProcessamento("Erro ao buscar impressoras USB.", 0, false);
                PrinterDetectionLogger.Log("Erro ao buscar impressoras USB: " + ex.Message);
                MessageBox.Show(
                    $"Erro ao buscar impressoras: {ex.Message}",
                    "Erro",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                btnProcurar.Enabled = true;
                atualizacaoListaEmAndamento = false;
            }
        }

        /// <summary>
        /// Atualiza lista sem mostrar mensagens (para timer)
        /// </summary>
        private void AtualizarListaDispositivos()
        {
            _ = AtualizarListaDispositivosAsync(false);
        }

        private async Task AtualizarListaDispositivosAsync(bool mostrarStatus)
        {
            if (atualizacaoListaEmAndamento)
                return;

            try
            {
                atualizacaoListaEmAndamento = true;

                string deviceIdSelecionado = ObterDeviceIdSelecionado();
                if (mostrarStatus)
                    AtualizarStatusProcessamento("Atualizando lista de impressoras...", 15);

                List<PrinterDeviceInfo> dispositivos = await Task.Run(() => EnumerarDispositivosUsb());
                PopularListaDispositivos(dispositivos, deviceIdSelecionado);

                if (mostrarStatus)
                    AtualizarStatusProcessamento("Lista de impressoras atualizada.", 100, false);
            }
            catch (Exception ex)
            {
                PrinterDetectionLogger.Log("Erro ao atualizar lista de impressoras: " + ex.Message);
                if (mostrarStatus)
                    AtualizarStatusProcessamento("Erro ao atualizar lista de impressoras.", 0, false);
            }
            finally
            {
                atualizacaoListaEmAndamento = false;
            }
        }

        private List<PrinterDeviceInfo> EnumerarDispositivosUsb()
        {
            var dispositivos = new List<PrinterDeviceInfo>();

            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE DeviceID LIKE '%USB%'"))
            using (ManagementObjectCollection results = searcher.Get())
            {
                foreach (ManagementObject obj in results)
                {
                    string deviceId = obj["DeviceID"]?.ToString() ?? "-";

                    // Filtra apenas impressoras USB
                    if (!deviceId.StartsWith("USBPRINT", StringComparison.OrdinalIgnoreCase))
                        continue;

                    string fabricante = ObterFabricante(obj);
                    string statusDriver = VerificarStatusDriver(obj);
                    var dispositivo = PrinterDeviceInfo.FromManagementObject(obj, fabricante);
                    dispositivo.StatusDriver = statusDriver;

                    if (string.IsNullOrWhiteSpace(dispositivo.Nome))
                        dispositivo.Nome = "Desconhecido";

                    if (string.IsNullOrWhiteSpace(dispositivo.DeviceId))
                        dispositivo.DeviceId = deviceId;

                    dispositivos.Add(dispositivo);
                    PrinterDetectionLogger.LogDeviceFound(dispositivo);
                }
            }

            return dispositivos;
        }

        private void PopularListaDispositivos(IEnumerable<PrinterDeviceInfo> dispositivos, string deviceIdSelecionado)
        {
            listViewDispositivos.BeginUpdate();
            try
            {
                listViewDispositivos.Items.Clear();

                foreach (var dispositivo in dispositivos)
                {
                    var item = new ListViewItem(new[]
                    {
                        string.IsNullOrWhiteSpace(dispositivo.Nome) ? "Desconhecido" : dispositivo.Nome,
                        string.IsNullOrWhiteSpace(dispositivo.Fabricante) ? "Desconhecido" : dispositivo.Fabricante,
                        string.IsNullOrWhiteSpace(dispositivo.StatusDriver) ? "? Indeterminado" : dispositivo.StatusDriver,
                        string.IsNullOrWhiteSpace(dispositivo.DeviceId) ? "-" : dispositivo.DeviceId
                    });

                    item.Tag = dispositivo;

                    Color corStatus = ObterCorStatus(dispositivo.StatusDriver ?? string.Empty);
                    item.ForeColor = corStatus;

                    if ((dispositivo.StatusDriver ?? string.Empty).StartsWith("✗") ||
                        (dispositivo.StatusDriver ?? string.Empty).StartsWith("⚠"))
                    {
                        item.Font = new Font(listViewDispositivos.Font, FontStyle.Bold);
                    }

                    listViewDispositivos.Items.Add(item);

                    if (!string.IsNullOrWhiteSpace(deviceIdSelecionado) &&
                        dispositivo.DeviceId.Equals(deviceIdSelecionado, StringComparison.OrdinalIgnoreCase))
                    {
                        item.Selected = true;
                    }
                }
            }
            finally
            {
                listViewDispositivos.EndUpdate();
            }
        }

        private string ObterDeviceIdSelecionado()
        {
            if (listViewDispositivos.SelectedItems.Count == 0)
                return null;

            if (listViewDispositivos.SelectedItems[0].Tag is PrinterDeviceInfo dispositivo)
                return dispositivo.DeviceId;

            return listViewDispositivos.SelectedItems[0].SubItems.Count > 3
                ? listViewDispositivos.SelectedItems[0].SubItems[3].Text
                : null;
        }

        private async void btnProcurar_Click(object sender, EventArgs e)
        {
            listViewDispositivos.Items.Clear();
            await BuscarDispositivosDeImpressorasAsync();
        }

        private void btnInstalarDriver_Click(object sender, EventArgs e)
        {
            if (listViewDispositivos.SelectedItems.Count == 0)
            {
                MessageBox.Show(
                    "Selecione um dispositivo na lista para instalar o driver.",
                    "Nenhum Dispositivo Selecionado",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var itemSelecionado = listViewDispositivos.SelectedItems[0];
            var dispositivo = itemSelecionado.Tag as PrinterDeviceInfo ?? CriarDispositivoAPartirDoItem(itemSelecionado);
            string nomeDispositivo = string.IsNullOrWhiteSpace(dispositivo.Nome) ? "Desconhecido" : dispositivo.Nome;
            string statusAtual = dispositivo.StatusDriver ?? string.Empty;

            // Avisa se já está instalado
            if (statusAtual.StartsWith("✓"))
            {
                DialogResult continuar = MessageBox.Show(
                    $"O driver desta impressora já está instalado.\n\n" +
                    $"Dispositivo: {nomeDispositivo}\n" +
                    $"Status: {statusAtual}\n\n" +
                    $"Deseja reinstalar mesmo assim?",
                    "Driver Já Instalado",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (continuar != DialogResult.Yes)
                    return;
            }

            AtualizarStatusProcessamento("Identificando modelo e escolhendo melhor driver...", 25);
            var resultadoIdentificacao = TentarIdentificarImpressora(dispositivo);
            var impressoraEncontrada = resultadoIdentificacao.Impressora;

            if (impressoraEncontrada != null)
            {
                AtualizarStatusProcessamento($"Driver selecionado: {impressoraEncontrada.Nome}", 45);

                DialogResult resultado = MessageBox.Show(
                    $"Dispositivo: {nomeDispositivo}\n\n" +
                    $"Impressora identificada: {impressoraEncontrada.Nome}\n" +
                    $"Pontuação: {resultadoIdentificacao.Pontuacao}\n" +
                    $"Motivo: {resultadoIdentificacao.Motivo}\n\n" +
                    $"Deseja baixar e instalar o driver?",
                    "Driver Identificado",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (resultado == DialogResult.Yes)
                {
                    driverInstaller.BaixarEInstalarDriver(impressoraEncontrada);

                    // Aguarda 10 segundos e atualiza lista
                    Timer timerAtualizarAposInstalar = new Timer();
                    timerAtualizarAposInstalar.Interval = 10000;
                    timerAtualizarAposInstalar.Tick += (s, ev) =>
                    {
                        AtualizarListaDispositivos();
                        timerAtualizarAposInstalar.Stop();
                        timerAtualizarAposInstalar.Dispose();
                    };
                    timerAtualizarAposInstalar.Start();
                }
                else
                {
                    AtualizarStatusProcessamento("Instalação cancelada pelo usuário.", 0, false);
                }
            }
            else
            {
                AtualizarStatusProcessamento("Não foi possível identificar com segurança. Abrindo seleção manual.", 0, false);
                MostrarSelecaoManualDriver(nomeDispositivo);
            }
        }

        private PrinterDeviceInfo CriarDispositivoAPartirDoItem(ListViewItem item)
        {
            return new PrinterDeviceInfo
            {
                Nome = item.SubItems.Count > 0 ? item.SubItems[0].Text : string.Empty,
                Fabricante = item.SubItems.Count > 1 ? item.SubItems[1].Text : string.Empty,
                StatusDriver = item.SubItems.Count > 2 ? item.SubItems[2].Text : string.Empty,
                DeviceId = item.SubItems.Count > 3 ? item.SubItems[3].Text : string.Empty
            };
        }

        private PrinterMatchResult TentarIdentificarImpressora(PrinterDeviceInfo dispositivo)
        {
            var resultado = PrinterDriverMatcher.IdentificarMelhorDriver(dispositivo, impressoras);

            if (resultado.Confiavel)
            {
                PrinterDetectionLogger.LogMatchResult(dispositivo, resultado);
                return resultado;
            }

            var fallback = TentarIdentificarImpressoraLegado(dispositivo.Nome);
            if (fallback != null)
            {
                resultado.Impressora = fallback;
                resultado.UsouFallback = true;
                resultado.Pontuacao = Math.Max(resultado.Pontuacao, 1);
                resultado.Motivo = "Fallback legado aplicado após baixa confiança do matcher novo. " + resultado.Motivo;
            }

            PrinterDetectionLogger.LogMatchResult(dispositivo, resultado);
            return resultado;
        }

        private ImpressoraInfo TentarIdentificarImpressoraLegado(string nomeDispositivo)
        {
            if (string.IsNullOrWhiteSpace(nomeDispositivo))
                return null;

            string nomeNormalizado = nomeDispositivo.ToLower().Replace(" ", "");

            foreach (var impressora in impressoras)
            {
                string nomeImpressoraNormalizado = impressora.Nome.ToLower().Replace(" ", "");

                if (nomeNormalizado.Contains(nomeImpressoraNormalizado) ||
                    nomeImpressoraNormalizado.Contains(nomeNormalizado))
                {
                    return impressora;
                }

                string[] partesDispositivo = nomeDispositivo.ToLower().Split(' ');
                string[] partesImpressora = impressora.Nome.ToLower().Split(' ');

                int correspondencias = partesDispositivo.Count(pd =>
                    partesImpressora.Any(pi => pi.Contains(pd) || pd.Contains(pi)));

                if (correspondencias >= 2)
                {
                    return impressora;
                }
            }

            return null;
        }

        private void MostrarSelecaoManualDriver(string nomeDispositivo)
        {
            using (FormSelecaoDriver formSelecao = new FormSelecaoDriver(impressoras, nomeDispositivo))
            {
                if (formSelecao.ShowDialog(this) == DialogResult.OK)
                {
                    var impressoraSelecionada = formSelecao.ImpressoraSelecionada;
                    if (impressoraSelecionada != null)
                    {
                        AtualizarStatusProcessamento($"Instalando driver selecionado manualmente: {impressoraSelecionada.Nome}", 20);
                        PrinterDetectionLogger.Log("Driver selecionado manualmente | Dispositivo=" + nomeDispositivo + " | Modelo=" + impressoraSelecionada.Nome);
                        driverInstaller.BaixarEInstalarDriver(impressoraSelecionada);

                        // Aguarda e atualiza lista
                        Timer timerAtualizarAposInstalar = new Timer();
                        timerAtualizarAposInstalar.Interval = 2000;
                        timerAtualizarAposInstalar.Tick += (s, ev) =>
                        {
                            AtualizarListaDispositivos();
                            timerAtualizarAposInstalar.Stop();
                            timerAtualizarAposInstalar.Dispose();
                        };
                        timerAtualizarAposInstalar.Start();
                    }
                }
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Para timer
            if (timerAtualizacao != null)
            {
                timerAtualizacao.Stop();
                timerAtualizacao.Dispose();
            }

            if (timerAnimacaoPainel != null)
            {
                timerAnimacaoPainel.Stop();
                timerAnimacaoPainel.Dispose();
            }

            // Libera imagem
            if (pictureBox1.Image != null)
            {
                pictureBox1.Image.Dispose();
                pictureBox1.Image = null;
            }

            base.OnFormClosing(e);
        }
    }

    /// <summary>
    /// Formulário para seleção manual do driver
    /// </summary>
    public class FormSelecaoDriver : Form
    {
        private ComboBox comboImpressoras;
        private Button btnOK;
        private Button btnCancelar;
        private Label lblInfo;
        private PictureBox picturePreview;

        public ImpressoraInfo ImpressoraSelecionada { get; private set; }

        public FormSelecaoDriver(List<ImpressoraInfo> impressoras, string nomeDispositivo)
        {
            InitializeComponent(nomeDispositivo);
            CarregarImpressoras(impressoras);
        }

        private void InitializeComponent(string nomeDispositivo)
        {
            this.Text = "Selecionar Driver Manualmente";
            this.Size = new Size(550, 420);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(240, 240, 240);

            Label lblTitulo = new Label
            {
                Text = "Seleção Manual de Driver",
                Location = new Point(20, 20),
                Size = new Size(500, 25),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 73, 94)
            };

            lblInfo = new Label
            {
                Text = $"Dispositivo detectado:\n{nomeDispositivo}\n\nSelecione o modelo correto da impressora:",
                Location = new Point(20, 55),
                Size = new Size(500, 60),
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Color.FromArgb(52, 73, 94)
            };

            Label lblModelo = new Label
            {
                Text = "Modelo da Impressora:",
                Location = new Point(20, 125),
                Size = new Size(200, 20),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 73, 94)
            };

            comboImpressoras = new ComboBox
            {
                Location = new Point(20, 150),
                Size = new Size(500, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10F)
            };
            comboImpressoras.SelectedIndexChanged += ComboImpressoras_SelectedIndexChanged;

            picturePreview = new PictureBox
            {
                Location = new Point(20, 190),
                Size = new Size(500, 150),
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.White
            };

            btnOK = new Button
            {
                Text = "Baixar e Instalar",
                Location = new Point(310, 355),
                Size = new Size(130, 35),
                DialogResult = DialogResult.OK,
                BackColor = Color.FromArgb(46, 204, 113),
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnOK.FlatAppearance.BorderSize = 0;
            btnOK.Click += BtnOK_Click;
            btnOK.MouseEnter += (s, e) => btnOK.BackColor = Color.FromArgb(39, 174, 96);
            btnOK.MouseLeave += (s, e) => btnOK.BackColor = Color.FromArgb(46, 204, 113);

            btnCancelar = new Button
            {
                Text = "Cancelar",
                Location = new Point(450, 355),
                Size = new Size(80, 35),
                DialogResult = DialogResult.Cancel,
                BackColor = Color.FromArgb(149, 165, 166),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCancelar.FlatAppearance.BorderSize = 0;
            btnCancelar.MouseEnter += (s, e) => btnCancelar.BackColor = Color.FromArgb(127, 140, 141);
            btnCancelar.MouseLeave += (s, e) => btnCancelar.BackColor = Color.FromArgb(149, 165, 166);

            this.Controls.AddRange(new Control[] {
                lblTitulo, lblInfo, lblModelo, comboImpressoras, picturePreview, btnOK, btnCancelar
            });

            this.AcceptButton = btnOK;
            this.CancelButton = btnCancelar;
        }

        private void CarregarImpressoras(List<ImpressoraInfo> impressoras)
        {
            comboImpressoras.Items.Clear();
            foreach (var imp in impressoras)
            {
                comboImpressoras.Items.Add(imp);
            }
            comboImpressoras.DisplayMember = "Nome";

            if (comboImpressoras.Items.Count > 0)
                comboImpressoras.SelectedIndex = 0;
        }

        private void ComboImpressoras_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboImpressoras.SelectedItem is ImpressoraInfo impressora)
            {
                if (picturePreview.Image != null)
                {
                    picturePreview.Image.Dispose();
                }
                picturePreview.Image = impressora.ObterImagem();
            }
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            ImpressoraSelecionada = comboImpressoras.SelectedItem as ImpressoraInfo;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (picturePreview.Image != null)
            {
                picturePreview.Image.Dispose();
            }
            base.OnFormClosing(e);
        }
    }
}
