using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace EtiquetaFORNew.Data
{
    public class DatabaseConfig

    {
        private static readonly string ConfigFilePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "config.json");
        private const int RegistroUsoTimeoutMs = 15000;
        private const int RegistroUsoMaxTentativas = 3;
        private const int RegistroUsoIntervaloTentativasMs = 1500;
        private static readonly SemaphoreSlim RegistroUsoLock = new SemaphoreSlim(1, 1);


        public class ConfigData
        {
            public string Servidor { get; set; }
            public string Porta { get; set; }
            public string Banco { get; set; }
            public string Usuario { get; set; }
            public string Senha { get; set; }
            public string Timeout { get; set; }
            public string Loja { get; set; }
            public string ModuloApp { get; set; }
            public string ModuloAppWeb { get; set; } // Valor vindo da sua ComboBox na tela Web
        }

        public class RegistroUsoResultado
        {
            public bool Sucesso { get; set; }
            public string Resposta { get; set; }
            public string MensagemErro { get; set; }
            public int Tentativas { get; set; }
        }

        public static bool IsConfigured()
        {
            try
            {
                return File.Exists(ConfigFilePath) && !string.IsNullOrEmpty(GetConnectionString());
            }
            catch
            {
                return false;
            }
        }

        public static string GetConnectionString()
        {
            try
            {
                if (!File.Exists(ConfigFilePath))
                    return string.Empty;

                string json = File.ReadAllText(ConfigFilePath);
                ConfigData config = JsonConvert.DeserializeObject<ConfigData>(json);

                if (config == null || string.IsNullOrEmpty(config.Servidor) || string.IsNullOrEmpty(config.Banco))
                    return string.Empty;

                string servidor = config.Servidor;

                // Adicionar porta se informada e diferente da padrão
                if (!string.IsNullOrEmpty(config.Porta) && config.Porta != "1433")
                {
                    servidor = $"{servidor},{config.Porta}";
                }

                string connStr = $"Server={servidor};Database={config.Banco};";

                // Autenticação
                if (!string.IsNullOrEmpty(config.Usuario))
                {
                    connStr += $"User Id={config.Usuario};Password={config.Senha};";
                }
                else
                {
                    connStr += "Integrated Security=true;";
                }

                // Timeout
                if (!string.IsNullOrEmpty(config.Timeout) && config.Timeout != "15")
                {
                    connStr += $"Connection Timeout={config.Timeout};";
                }

                return connStr;
            }
            catch
            {
                return string.Empty;
            }
        }

        public static void SaveConnectionString(string connectionString)
        {
            // Este método mantido para compatibilidade, mas não faz nada
            // Use SaveConfiguration ao invés
        }

        public static void SaveConfiguration(string servidor, string porta, string bancoDados,
            string usuario, string senha, string timeout, string loja = "", string moduloApp = "", string moduloAppWeb = "")
        {
            try
            {
                ConfigData configExistente = LoadConfiguration();
                ConfigData config = new ConfigData
                {
                    Servidor = servidor,
                    Porta = porta,
                    Banco = bancoDados,
                    Usuario = usuario,
                    Senha = senha,
                    Timeout = timeout,
                    Loja = loja,
                    ModuloApp = moduloApp,
                    ModuloAppWeb = string.IsNullOrEmpty(moduloAppWeb) ? configExistente.ModuloAppWeb : moduloAppWeb
                };

                string json = JsonConvert.SerializeObject(config, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(ConfigFilePath, json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao salvar configuração: {ex.Message}");
            }
        }

        public static ConfigData LoadConfiguration()
        {
            try
            {
                if (!File.Exists(ConfigFilePath))
                    return new ConfigData();

                string json = File.ReadAllText(ConfigFilePath);
                return JsonConvert.DeserializeObject<ConfigData>(json) ?? new ConfigData();
            }
            catch
            {
                return new ConfigData();
            }
        }

        public static string GetConfigFilePath()
        {
            return ConfigFilePath;
        }

        public static async Task<RegistroUsoResultado> RegistrarUsoSistemaAsync(
            string codigoSuporte,
            string cnpj,
            string fantasia,
            string origem = "")
        {
            codigoSuporte = (codigoSuporte ?? string.Empty).Trim();
            cnpj = (cnpj ?? string.Empty).Trim();
            fantasia = (fantasia ?? string.Empty).Trim();
            origem = string.IsNullOrWhiteSpace(origem) ? "RegistroUso" : origem.Trim();

            if (string.IsNullOrWhiteSpace(codigoSuporte) ||
                string.IsNullOrWhiteSpace(cnpj) ||
                string.IsNullOrWhiteSpace(fantasia))
            {
                string mensagem = $"[{origem}] Dados incompletos para registrar uso. " +
                                  $"CodigoSuporte vazio: {string.IsNullOrWhiteSpace(codigoSuporte)}, " +
                                  $"CNPJ vazio: {string.IsNullOrWhiteSpace(cnpj)}, " +
                                  $"Fantasia vazia: {string.IsNullOrWhiteSpace(fantasia)}";

                Debug.WriteLine(mensagem);
                return new RegistroUsoResultado
                {
                    Sucesso = false,
                    MensagemErro = mensagem,
                    Tentativas = 0
                };
            }

            await RegistroUsoLock.WaitAsync();
            try
            {
                Exception ultimaExcecao = null;

                for (int tentativa = 1; tentativa <= RegistroUsoMaxTentativas; tentativa++)
                {
                    try
                    {
                        Debug.WriteLine($"[{origem}] Registrando uso. Tentativa {tentativa}/{RegistroUsoMaxTentativas}. CNPJ: {cnpj}");

                        string resposta = await GetSetRegistroJsonAsync(codigoSuporte, cnpj, fantasia);
                        string motivoRespostaInvalida;

                        if (!RespostaRegistroUsoValida(resposta, out motivoRespostaInvalida))
                        {
                            throw new InvalidOperationException(motivoRespostaInvalida);
                        }

                        Debug.WriteLine($"[{origem}] Registro de uso concluido. Tentativa: {tentativa}. Resposta: {resposta}");
                        return new RegistroUsoResultado
                        {
                            Sucesso = true,
                            Resposta = resposta,
                            Tentativas = tentativa
                        };
                    }
                    catch (Exception ex)
                    {
                        ultimaExcecao = ex;
                        Debug.WriteLine($"[{origem}] Falha ao registrar uso na tentativa {tentativa}/{RegistroUsoMaxTentativas}: {ex}");

                        if (tentativa < RegistroUsoMaxTentativas)
                        {
                            await Task.Delay(RegistroUsoIntervaloTentativasMs);
                        }
                    }
                }

                return new RegistroUsoResultado
                {
                    Sucesso = false,
                    MensagemErro = ultimaExcecao?.Message ?? "Falha desconhecida ao registrar uso.",
                    Tentativas = RegistroUsoMaxTentativas
                };
            }
            finally
            {
                RegistroUsoLock.Release();
            }
        }

        public static async Task<RegistroUsoResultado> RegistrarUsoSoftcomShopAsync(
            EtiquetaFORNew.ConfiguracaoSistema config,
            string origem = "RegistroUso SoftcomShop")
        {
            if (config == null || config.TipoConexaoAtiva != EtiquetaFORNew.TipoConexao.SoftcomShop || config.SoftcomShop == null)
            {
                string mensagem = $"[{origem}] Configuracao SoftcomShop ausente ou inativa para registrar uso.";
                Debug.WriteLine(mensagem);
                return new RegistroUsoResultado
                {
                    Sucesso = false,
                    MensagemErro = mensagem,
                    Tentativas = 0
                };
            }

            return await RegistrarUsoSistemaAsync(
                config.SoftcomShop.ClientId,
                config.SoftcomShop.CompanyCNPJ,
                config.SoftcomShop.CompanyName,
                origem);
        }

        public static async Task<string> GetSetRegistroJsonAsync(string codigoSuporte, string cnpj, string fantasia)
        {
            string url = "http://softcomdevelop.com.br/webService/wsRegistro.asmx";

            // Obter a versão do sistema

            var assembly = Assembly.GetExecutingAssembly();
            var fileInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
        

            string versaoSistema = fileInfo.FileVersion;//System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            string nomeAppComVersao = $"Smart Print v{versaoSistema}";

            string parametrosJson = JsonConvert.SerializeObject(new
            {
                CNPJ = cnpj ?? string.Empty,
                Empresa = fantasia ?? string.Empty,
                CodigoApp = "41",
                App = nomeAppComVersao,
                Token = "{EFAC2E35-9FCC-480E-80AC-5EDDA66E8A9F}",
                CodigoSuporte = codigoSuporte ?? string.Empty,
                ConfigApp = "{}"
            });


            //string parametrosJson = "{"
            //    + "\"CNPJ\":\"" + cnpj.Replace("\"", "\\\"") + "\","
            //    + "\"Empresa\":\"" + fantasia.Replace("\"", "\\\"") + "\","
            //    + "\"CodigoApp\":\"41\","
            //    + "\"App\":\"Smart Print\","
            //    + "\"Token\":\"{EFAC2E35-9FCC-480E-80AC-5EDDA66E8A9F}\","
            //    + "\"CodigoSuporte\":\"" + codigoSuporte.Replace("\"", "\\\"") + "\","
            //    + "\"ConfigApp\":\"{}\"" // <- Colocar aqui o link de download do backup em nuvem
            //    + "}";

            string jsonEscapadoXml = System.Security.SecurityElement.Escape(parametrosJson);

            string soap =
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>"
              + "<soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\">"
              + "  <soap:Body>"
              + "    <getSetRegistroJson xmlns=\"http://tempuri.org/\">"
              + "      <parametrosJson>" + jsonEscapadoXml + "</parametrosJson>"
              + "    </getSetRegistroJson>"
              + "  </soap:Body>"
              + "</soap:Envelope>";

            var req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "POST";
            req.ContentType = "text/xml; charset=utf-8";
            req.Timeout = RegistroUsoTimeoutMs;
            req.ReadWriteTimeout = RegistroUsoTimeoutMs;
            req.Headers.Add("SOAPAction", "http://tempuri.org/getSetRegistroJson");

            byte[] data = Encoding.UTF8.GetBytes(soap);
            using (var reqStream = await req.GetRequestStreamAsync())
                await reqStream.WriteAsync(data, 0, data.Length);

            using (var resp = (HttpWebResponse)await req.GetResponseAsync())
            using (var reader = new StreamReader(resp.GetResponseStream()))
                return await reader.ReadToEndAsync();
        }

        private static bool RespostaRegistroUsoValida(string resposta, out string motivo)
        {
            if (string.IsNullOrWhiteSpace(resposta))
            {
                motivo = "Resposta vazia do wsRegistro.";
                return false;
            }

            if (resposta.IndexOf("<soap:Fault", StringComparison.OrdinalIgnoreCase) >= 0 ||
                resposta.IndexOf("<faultstring", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                motivo = "Resposta SOAP indica falha no wsRegistro.";
                return false;
            }

            motivo = null;
            return true;
        }

        public static void SaveConfiguration(ConfigData config)
        {
            // Ele apenas repassa os dados para o método acima
            SaveConfiguration(
                config.Servidor,
                config.Porta,
                config.Banco,
                config.Usuario,
                config.Senha,
                config.Timeout,
                config.Loja,
                config.ModuloApp,
                config.ModuloAppWeb // Aqui garantimos que o valor do combo Web seja passado
            );
        }

    }
}
