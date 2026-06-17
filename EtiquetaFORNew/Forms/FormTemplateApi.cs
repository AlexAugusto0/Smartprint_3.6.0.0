using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace EtiquetaFORNew
{
    public partial class FormTemplateApi : Form
    {
        private TextBox txtCodigoCliente;
        private TextBox txtDescricao;
        private Button btnBackup;
        private Button btnRestaurar;

        // URL da sua API local (Ajuste a porta se a sua for diferente)
        //private const string UrlApi = "https://localhost:7106/api/ConfigEtiqueta";
        private const string UrlApi = "https://templateapi-0e8j.onrender.com/api/ConfigEtiqueta";

        public FormTemplateApi()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Sincronização Nuvem - SmartPrint";
            this.Size = new Size(400, 320);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Código do Cliente
            Label lblCodigo = new Label
            {
                Text = "Código do Cliente:",
                Location = new Point(20, 20),
                Size = new Size(200, 20),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            txtCodigoCliente = new TextBox
            {
                Location = new Point(20, 45),
                Size = new Size(340, 25),
                Font = new Font("Segoe UI", 10)
            };

            // Descrição do Terminal
            Label lblDescricao = new Label
            {
                Text = "Nome do Cliente (Ex: Softcom Tecnologia):",
                Location = new Point(20, 85),
                Size = new Size(340, 20),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            txtDescricao = new TextBox
            {
                Location = new Point(20, 110),
                Size = new Size(340, 25),
                Font = new Font("Segoe UI", 10)
            };

            // Botão Fazer Backup (Enviar)
            btnBackup = new Button
            {
                Text = "📤 Enviar Configurações para Nuvem",
                Location = new Point(20, 170),
                Size = new Size(340, 35),
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            btnBackup.FlatAppearance.BorderSize = 0;
            btnBackup.Click += BtnBackup_Click;

            // Botão Restaurar Backup (Puxar)
            btnRestaurar = new Button
            {
                Text = "📥 Baixar Configurações da Nuvem",
                Location = new Point(20, 215),
                Size = new Size(340, 35),
                BackColor = Color.FromArgb(255, 143, 0),
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            btnRestaurar.FlatAppearance.BorderSize = 0;
            btnRestaurar.Click += BtnRestaurar_Click;

            // Adiciona os controles na tela
            this.Controls.AddRange(new Control[] {
                lblCodigo, txtCodigoCliente, lblDescricao, txtDescricao, btnBackup, btnRestaurar
            });
        }

        // ==========================================
        // LÓGICA 1: FAZER BACKUP (POST)
        // ==========================================
        private async void BtnBackup_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtCodigoCliente.Text) || string.IsNullOrWhiteSpace(txtDescricao.Text))
            {
                MessageBox.Show("Preencha todos os campos antes de continuar.", "Validação", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            MudarEstadoBotoes(false);
            await ExecutarFluxoBackup(txtCodigoCliente.Text.Trim(), txtDescricao.Text.Trim(), false);
            MudarEstadoBotoes(true);
        }

        private async Task ExecutarFluxoBackup(string codigo, string descricao, bool sobrescrever)
        {
            try
            {
                string rotaBase = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "sistemaEtiquetas");

                if (!Directory.Exists(rotaBase))
                {
                    MessageBox.Show("A pasta local 'sistemaEtiquetas' não foi encontrada.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var pacote = new PacoteConfiguracao();
                string[] arquivosLocais = Directory.GetFiles(rotaBase, "*.json", SearchOption.AllDirectories);

                foreach (string arquivo in arquivosLocais)
                {
                    string nomeRelativo = arquivo.Substring(rotaBase.Length + 1).Replace("\\", "/");
                    string conteudoJson = File.ReadAllText(arquivo);
                    pacote.Arquivos.Add(new ArquivoEtiqueta { Nome = nomeRelativo, Conteudo = conteudoJson });
                }

                var dto = new ConfigEtiquetaDTO
                {
                    CodigoCliente = codigo,
                    Descricao = descricao,
                    ConteudoConfig = JsonConvert.SerializeObject(pacote)
                };

                using (var client = new HttpClient())
                {
                    string url = $"{UrlApi}/salvar?sobrescrever={sobrescrever}";
                    var content = new StringContent(JsonConvert.SerializeObject(dto), Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(url, content);

                    if (response.IsSuccessStatusCode)
                    {
                        MessageBox.Show("Backup realizado com sucesso na nuvem!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    if (response.StatusCode == HttpStatusCode.Conflict)
                    {
                        var result = MessageBox.Show(
                            $"Já existe uma configuração para o cliente {codigo}.\nDeseja substituir?",
                            "Substituir Configuração", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                        if (result == DialogResult.Yes)
                        {
                            await ExecutarFluxoBackup(codigo, descricao, true);
                        }
                        return;
                    }

                    MessageBox.Show($"Erro na API: {response.StatusCode}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro no processo: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ==========================================
        // LÓGICA 2: RESTAURAR BACKUP (GET)
        // ==========================================
        private async void BtnRestaurar_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtCodigoCliente.Text))
            {
                MessageBox.Show("Informe o Código do Cliente para buscar a configuração.", "Validação", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var confirma = MessageBox.Show("A restauração irá sobrescrever os arquivos locais atuais. Deseja continuar?", "Confirmação", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirma != DialogResult.Yes) return;

            MudarEstadoBotoes(false);

            try
            {
                using (var client = new HttpClient())
                {
                    string url = $"{UrlApi}/buscar/{txtCodigoCliente.Text.Trim()}";
                    var response = await client.GetAsync(url);

                    if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        MessageBox.Show("Nenhuma configuração encontrada para este cliente.", "Não Encontrado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        MessageBox.Show($"Erro ao buscar dados: {response.StatusCode}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    var dto = JsonConvert.DeserializeObject<ConfigEtiquetaDTO>(jsonResponse);
                    var pacote = JsonConvert.DeserializeObject<PacoteConfiguracao>(dto.ConteudoConfig);

                    string rotaBase = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "sistemaEtiquetas");

                    foreach (var arquivo in pacote.Arquivos)
                    {
                        string caminhoCompleto = Path.Combine(rotaBase, arquivo.Nome.Replace("/", "\\"));
                        string diretorio = Path.GetDirectoryName(caminhoCompleto);

                        if (!Directory.Exists(diretorio))
                            Directory.CreateDirectory(diretorio);

                        File.WriteAllText(caminhoCompleto, arquivo.Conteudo);
                    }

                    MessageBox.Show("Configurações e templates restaurados com sucesso na sua máquina!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // === ATUALIZAÇÃO DA LISTA EM SEGUNDO PLANO ===
                    // Verifica se o formulário que abriu este é o FormListaTemplates e força o "requery"
                    if (this.Owner is FormListaTemplates formLista)
                    {
                        formLista.CarregarLista();
                    }
                    // ============================================
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro na restauração: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                MudarEstadoBotoes(true);
            }
        }

        private void MudarEstadoBotoes(bool ativo)
        {
            btnBackup.Enabled = ativo;
            btnRestaurar.Enabled = ativo;
        }
    }

    // Classes de mapeamento de dados (DTOs) no escopo correto
    public class ArquivoEtiqueta { public string Nome { get; set; } public string Conteudo { get; set; } }
    public class PacoteConfiguracao { public List<ArquivoEtiqueta> Arquivos { get; set; } = new List<ArquivoEtiqueta>(); }
    public class ConfigEtiquetaDTO { public string CodigoCliente { get; set; } public string Descricao { get; set; } public string ConteudoConfig { get; set; } }
}