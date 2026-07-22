using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EtiquetaFORNew
{
    /// <summary>
    /// serviço para comunicação com a API SoftcomShop
    /// </summary>
    public class SoftcomShopService
    {
        private readonly HttpClient _httpClient;
        private readonly SoftcomShopConfig _config;
        private readonly SoftcomShopRouter _router;
        private readonly SemaphoreSlim _tokenLock = new SemaphoreSlim(1, 1);
        private const int TokenRefreshWindowMinutes = 2;
        private const int MaxTentativasAutenticacao = 2;
        private string _currentToken;
        private DateTime _tokenExpiraEmUtc = DateTime.MinValue;

        public SoftcomShopService(SoftcomShopConfig config)
        {
            _config = config;
            _router = new SoftcomShopRouter(config.BaseURL);
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromMinutes(5);
        }

        #region Autentição

        /// <summary>
        /// obtem token de autenticação
        /// </summary>
        public async Task<string> GetTokenAsync(bool tentarVincularDispositivo = true, bool forcarRenovacao = false)
        {
            await _tokenLock.WaitAsync();
            try
            {
                if (!forcarRenovacao && TokenAtualValido())
                    return _currentToken;

                return await GetTokenSemLockAsync(tentarVincularDispositivo);
            }
            catch (SoftcomShopApiException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao obter token: {ex.Message}", ex);
            }
            finally
            {
                _tokenLock.Release();
            }
        }

        /// <summary>
        /// Cadastra o dispositivo no servidor
        /// </summary>
        public async Task<string> CadastrarDispositivoAsync()
        {
            try
            {
                var parameters = new Dictionary<string, string>
                {
                    { "client_id", _config.ClientId },
                    { "empresa_name", _config.CompanyName },
                    { "empresa_cnpj", _config.CompanyCNPJ },
                    { "device_name", _config.DeviceName },
                    { "device_id", _config.DeviceId }
                };

                var content = new FormUrlEncodedContent(parameters);

                var response = await _httpClient.PostAsync(_router.DeviceRouter, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return ExtrairClientSecret(responseContent);
                }

                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao cadastrar dispositivo: {ex.Message}", ex);
            }
        }

        private async Task<string> GetTokenSemLockAsync(bool tentarVincularDispositivo)
        {
            if (string.IsNullOrWhiteSpace(_config.ClientSecret) && tentarVincularDispositivo)
            {
                bool vinculoAtualizado = await RenovarVinculoDispositivoAsync();
                if (!vinculoAtualizado)
                    throw new Exception("Nao foi possivel vincular o dispositivo para obter o Client Secret.");
            }

            if (string.IsNullOrWhiteSpace(_config.ClientSecret))
                throw new Exception("Client Secret nao informado. Cadastre o dispositivo antes de sincronizar.");

            var parameters = new Dictionary<string, string>
            {
                { "client_secret", _config.ClientSecret },
                { "client_id", _config.ClientId },
                { "grant_type", "client_credentials" }
            };

            var content = new FormUrlEncodedContent(parameters);

            var response = await _httpClient.PostAsync(_router.TokenRouter, content);
            string responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                _currentToken = ExtrairToken(responseContent);
                if (string.IsNullOrWhiteSpace(_currentToken))
                    throw new JsonSerializationException("Resposta de autenticacao nao contem token.");

                AtualizarExpiracaoToken(responseContent);
                return _currentToken;
            }

            if (tentarVincularDispositivo && DeveRenovarVinculoDispositivo(response.StatusCode))
            {
                bool vinculoAtualizado = await RenovarVinculoDispositivoAsync();
                if (vinculoAtualizado)
                    return await GetTokenSemLockAsync(false);
            }

            throw new SoftcomShopApiException(response.StatusCode, responseContent, response.ReasonPhrase);
        }

        private async Task GarantirTokenValidoAsync()
        {
            if (!TokenAtualValido())
            {
                await GetTokenAsync();
            }
        }

        private bool TokenAtualValido()
        {
            return !string.IsNullOrWhiteSpace(_currentToken) &&
                   _tokenExpiraEmUtc != DateTime.MinValue &&
                   DateTime.UtcNow.AddMinutes(TokenRefreshWindowMinutes) < _tokenExpiraEmUtc;
        }

        private void InvalidarTokenEmMemoria()
        {
            _currentToken = null;
            _tokenExpiraEmUtc = DateTime.MinValue;
        }

        private static bool DeveRenovarVinculoDispositivo(HttpStatusCode statusCode)
        {
            return statusCode == HttpStatusCode.BadRequest ||
                   statusCode == HttpStatusCode.Unauthorized ||
                   statusCode == HttpStatusCode.Forbidden;
        }

        private async Task<bool> RenovarVinculoDispositivoAsync()
        {
            string clientSecret = await CadastrarDispositivoAsync();
            if (string.IsNullOrWhiteSpace(clientSecret))
                return false;

            _config.ClientSecret = clientSecret;
            InvalidarTokenEmMemoria();
            PersistirClientSecretDispositivo(clientSecret);
            return true;
        }

        private void PersistirClientSecretDispositivo(string clientSecret)
        {
            try
            {
                ConfiguracaoSistema configSistema = ConfiguracaoSistema.Carregar();
                if (configSistema?.SoftcomShop == null)
                    return;

                bool mesmaConfiguracao =
                    string.Equals(configSistema.SoftcomShop.BaseURL, _config.BaseURL, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(configSistema.SoftcomShop.ClientId, _config.ClientId, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(configSistema.SoftcomShop.DeviceId, _config.DeviceId, StringComparison.OrdinalIgnoreCase);

                if (!mesmaConfiguracao)
                    return;

                configSistema.SoftcomShop.ClientSecret = clientSecret;
                configSistema.Salvar();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SoftcomShop] Falha ao persistir Client Secret renovado: {ex.Message}");
            }
        }

        private static string ExtrairToken(string responseContent)
        {
            JToken root = JToken.Parse(responseContent);
            JToken data = ObterContainerData(root);

            string token = ObterTextoPorCampo(data, "access_token", "accessToken", "bearer_token", "bearerToken");
            if (!string.IsNullOrWhiteSpace(token))
                return token;

            token = ObterTextoPorCampo(data, "token");
            if (!string.IsNullOrWhiteSpace(token))
                return token;

            return ObterPrimeiroTokenNaoAntigo(data);
        }

        private static string ExtrairClientSecret(string responseContent)
        {
            JToken root = JToken.Parse(responseContent);
            JToken data = ObterContainerData(root);
            return ObterTextoPorCampo(data, "client_secret", "clientSecret");
        }

        private static JToken ObterContainerData(JToken root)
        {
            JObject obj = root as JObject;
            if (obj == null)
                return root;

            return ObterCampoIgnoreCase(obj, "data") ?? root;
        }

        private static string ObterTextoPorCampo(JToken token, params string[] campos)
        {
            JArray array = token as JArray;
            if (array != null)
            {
                foreach (JToken item in array)
                {
                    string textoItem = ObterTextoPorCampo(item, campos);
                    if (!string.IsNullOrWhiteSpace(textoItem))
                        return textoItem;
                }

                return string.Empty;
            }

            JObject obj = token as JObject;
            if (obj == null || campos == null)
                return string.Empty;

            foreach (string campo in campos)
            {
                JToken valor = ObterCampoIgnoreCase(obj, campo);
                if (valor != null && valor.Type != JTokenType.Null)
                {
                    string texto = valor.ToString().Trim();
                    if (!string.IsNullOrWhiteSpace(texto))
                        return texto;
                }
            }

            return string.Empty;
        }

        private static JToken ObterCampoIgnoreCase(JObject obj, string campo)
        {
            if (obj == null || string.IsNullOrWhiteSpace(campo))
                return null;

            foreach (JProperty prop in obj.Properties())
            {
                if (string.Equals(prop.Name, campo, StringComparison.OrdinalIgnoreCase))
                    return prop.Value;
            }

            return null;
        }

        private static string ObterPrimeiroTokenNaoAntigo(JToken token)
        {
            JArray array = token as JArray;
            if (array != null)
            {
                foreach (JToken item in array)
                {
                    string tokenItem = ObterPrimeiroTokenNaoAntigo(item);
                    if (!string.IsNullOrWhiteSpace(tokenItem))
                        return tokenItem;
                }

                return string.Empty;
            }

            JObject obj = token as JObject;
            if (obj == null)
                return string.Empty;

            foreach (JProperty prop in obj.Properties())
            {
                string nome = prop.Name ?? string.Empty;
                if (nome.IndexOf("token", StringComparison.OrdinalIgnoreCase) < 0 ||
                    nome.IndexOf("old", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    continue;
                }

                if (prop.Value != null && prop.Value.Type != JTokenType.Null)
                {
                    string valor = prop.Value.ToString().Trim();
                    if (!string.IsNullOrWhiteSpace(valor))
                        return valor;
                }
            }

            return string.Empty;
        }

        private void AtualizarExpiracaoToken(string responseContent)
        {
            _tokenExpiraEmUtc = DateTime.UtcNow.AddMinutes(50);

            try
            {
                JToken root = JToken.Parse(responseContent);
                JToken data = root["data"] ?? root;
                JToken expiresInToken = data["expires_in"] ?? data["expires"];

                if (expiresInToken != null &&
                    int.TryParse(expiresInToken.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int segundos) &&
                    segundos > 0)
                {
                    _tokenExpiraEmUtc = DateTime.UtcNow.AddSeconds(Math.Max(30, segundos));
                    return;
                }

                JToken expiresAtToken = data["expires_at"] ?? data["expiration"] ?? data["expiresAt"];
                if (expiresAtToken != null &&
                    DateTime.TryParse(expiresAtToken.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime expiraEm))
                {
                    _tokenExpiraEmUtc = expiraEm.ToUniversalTime();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SoftcomShop] Nao foi possivel identificar expiracao do token: {ex.Message}");
            }
        }

        #endregion

        #region Produtos

        /// <summary>
        /// Obtem produtos do catálogo (paginado)
        /// </summary>
        public async Task<string> GetProdutosAsync(int page = 1, string versao = "v2")
        {
            try
            {
                string url;
                string apiVersion = null;

                if (versao == "v2")
                {
                    apiVersion = "v2";
                    url = $"{_router.ProductsRouterV2}?page={page}";
                }
                else
                {
                    url = $"{_router.ProductsRouter}/page/{page}";
                }

                return await GetAutenticadoAsync(url, apiVersion);
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao obter produtos: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Obtem por nota fiscal
        /// </summary>
        public async Task<string> GetNotaFiscalAsync(string dataEntrada, int numeroNota = 0, int page = 1, string versao = "v2")
        {
            try
            {
                string url;
                string apiVersion = null;

                if (versao == "v2")
                {
                    apiVersion = "v2";
                    url = $"{_router.DataEntradaNotaFiscalV2}{dataEntrada}";

                    if (numeroNota > 0)
                        url += $"&numero_nota_fiscal={numeroNota}";

                    url += $"&page={page}";
                }
                else
                {
                    url = $"{_router.DataEntradaNotaFiscal}{dataEntrada}";

                    if (numeroNota > 0)
                        url += $"?numero_nota={numeroNota}";

                    url += $"/page/{page}";
                }

                return await GetAutenticadoAsync(url, apiVersion);
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao obter nota fiscal: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Obtem produtos de uma nota fiscal de entrada pelo numero informado.
        /// Mantem o fluxo do FormFiltrosCarregamento sem exigir data de entrada.
        /// </summary>
        public async Task<string> GetNotaFiscalPorNumeroAsync(int numeroNota, int page = 1)
        {
            try
            {
                string url = $"{_router.ComprasV2Router}?numero_nota_fiscal={numeroNota}&page={page}";
                return await GetAutenticadoAsync(url, "v2");
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao obter nota fiscal por numero: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Obtem produtos por venda
        /// </summary>
        public async Task<string> GetVendaAsync(int numeroVenda)
        {
            try
            {
                string url = $"{_router.VendasRouter}{numeroVenda}?bloquear=False";

                System.Diagnostics.Debug.WriteLine("=======================================");
                System.Diagnostics.Debug.WriteLine($"URL........: {url}");
                System.Diagnostics.Debug.WriteLine("TOKEN......: gerenciado por SoftcomShopService");
                System.Diagnostics.Debug.WriteLine("=======================================");

                string retorno = await GetAutenticadoAsync(url);
                System.Diagnostics.Debug.WriteLine($"RETORNO....: {retorno}");
                System.Diagnostics.Debug.WriteLine("=======================================");

                return retorno;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                throw;
            }
        }

        /// <summary>
        /// Consulta o cabecalho completo da NFe usado exclusivamente pelo modulo Distribuidora.
        /// </summary>
        public async Task<string> GetVendaCompletaDistribuidoraAsync(DateTime dataEmissao, int numeroNota)
        {
            string dataFormatada = dataEmissao.ToString("yyyy-MM-dd");
            string numero = Uri.EscapeDataString(numeroNota.ToString());
            string data = Uri.EscapeDataString(dataFormatada);

            string url = $"{_router.VendasCompletaDistribuidoraRouter}" +
                         $"?numero_nfe={numero}" +
                         $"&numero_nota_fiscal={numero}" +
                         $"&data_emissao={data}" +
                         "&bloquear=False";

            return await GetDistribuidoraAsync(url);
        }

        /// <summary>
        /// Consulta vendas pelo filtro oficial da API, usado exclusivamente pelo modulo Distribuidora.
        /// O schema exposto para a venda contem numero_documento, usado como filtro da NF/documento.
        /// </summary>
        public async Task<string> GetVendasFiltroDistribuidoraAsync(string numeroNfe)
        {
            string url = _router.VendasFiltroDistribuidoraRouter;

            if (!string.IsNullOrWhiteSpace(numeroNfe))
                url += $"?numero_documento={Uri.EscapeDataString(numeroNfe.Trim())}";

            return await GetDistribuidoraAsync(url);
        }

        /// <summary>
        /// Consulta dados cadastrais do cliente para etiquetas logisticas da Distribuidora.
        /// </summary>
        public async Task<string> GetClienteDistribuidoraAsync(long clienteId)
        {
            if (clienteId <= 0)
                throw new ArgumentException("ID do cliente invalido para consulta na API SoftcomShop.", nameof(clienteId));

            string idRota = Uri.EscapeDataString(clienteId.ToString(CultureInfo.InvariantCulture));
            string url = $"{_router.ClientesRouterV2}/{idRota}";

            System.Diagnostics.Debug.WriteLine($"[SoftcomShop][Distribuidora] Consultando cliente por ID: {url}");

            return await GetAutenticadoAsync(url, "v2", true);
        }

        /// <summary>
        /// Consulta o bairro do endereco do cliente para etiquetas logisticas da Distribuidora.
        /// </summary>
        public async Task<string> GetBairroDistribuidoraAsync(long bairroId)
        {
            string url = $"{_router.BairroRouter}/{bairroId}";
            return await GetDistribuidoraAsync(url);
        }

        private async Task<string> GetDistribuidoraAsync(string url, bool tentarRenovarToken = true)
        {
            return await GetAutenticadoAsync(url, null, true, tentarRenovarToken);
        }

        private async Task<string> GetAutenticadoAsync(string url, string apiVersion = null, bool lancarErroHttp = false, bool tentarRenovarToken = true)
        {
            try
            {
                int totalTentativas = tentarRenovarToken ? MaxTentativasAutenticacao : 1;

                for (int tentativa = 1; tentativa <= totalTentativas; tentativa++)
                {
                    await GarantirTokenValidoAsync();

                    HttpStatusCode statusCode;
                    string reasonPhrase;
                    bool sucessoHttp;
                    string retorno;

                    using (var request = new HttpRequestMessage(HttpMethod.Get, url))
                    {
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _currentToken);

                        if (!string.IsNullOrWhiteSpace(apiVersion))
                            request.Headers.Add("Api-Version", apiVersion);

                        using (var response = await _httpClient.SendAsync(request))
                        {
                            statusCode = response.StatusCode;
                            reasonPhrase = response.ReasonPhrase;
                            sucessoHttp = response.IsSuccessStatusCode;
                            retorno = await response.Content.ReadAsStringAsync();
                        }
                    }

                    GravarJsonDiagnosticoDistribuidora(url, retorno);

                    bool tokenExpiradoOuInvalido = DeveRenovarTokenPorResposta(statusCode, retorno);

                    if (tokenExpiradoOuInvalido && tentativa < totalTentativas)
                    {
                        RegistrarLogAutenticacao(url, statusCode, tentativa);
                        await RenovarTokenAutenticacaoAsync();
                        continue;
                    }

                    if (tokenExpiradoOuInvalido)
                        throw new SoftcomShopApiException(HttpStatusCode.Unauthorized, retorno, "Token expirado ou invalido.");

                    if (!sucessoHttp && lancarErroHttp)
                        throw new SoftcomShopApiException(statusCode, retorno, reasonPhrase);

                    return retorno;
                }

                throw new Exception("Nao foi possivel concluir a chamada autenticada da API SoftcomShop.");
            }
            catch (TaskCanceledException ex)
            {
                throw new TimeoutException("Tempo limite excedido ao comunicar com a API SoftcomShop.", ex);
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Falha de comunicacao com a API SoftcomShop: {ex.Message}", ex);
            }
        }

        private async Task RenovarTokenAutenticacaoAsync()
        {
            await _tokenLock.WaitAsync();
            try
            {
                InvalidarTokenEmMemoria();
                await GetTokenSemLockAsync(true);
            }
            finally
            {
                _tokenLock.Release();
            }
        }

        private static void GravarJsonDiagnosticoDistribuidora(string url, string retorno)
        {
            try
            {
                if (url.IndexOf("/softauth/api/vendas/filtro", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    File.WriteAllText(
                        Path.Combine(Application.StartupPath, "VendaFiltro.json"),
                        retorno
                    );
                }

                if (url.IndexOf("/softauth/api/clientes/clientes/", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    url.IndexOf("/softauth/api/v2/clientes/clientes/", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    File.WriteAllText(
                        Path.Combine(Application.StartupPath, "ClienteDistribuidora.json"),
                        retorno
                    );
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SoftcomShop] Falha ao gravar JSON diagnostico: {ex.Message}");
            }
        }

        private static void RegistrarLogAutenticacao(string url, HttpStatusCode statusCode, int tentativa)
        {
            try
            {
                string mensagem =
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | HTTP {(int)statusCode} | tentativa {tentativa} | " +
                    $"acao=renovar-token | url={url}";

                File.AppendAllText(
                    Path.Combine(Application.StartupPath, "SoftcomShopAuth.log"),
                    mensagem + Environment.NewLine);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SoftcomShop] Falha ao registrar log de autenticacao: {ex.Message}");
            }
        }

        private static bool DeveRenovarTokenPorResposta(HttpStatusCode statusCode, string retorno)
        {
            if (statusCode == HttpStatusCode.Unauthorized)
                return true;

            if (string.IsNullOrWhiteSpace(retorno))
                return false;

            string texto = retorno.ToLowerInvariant();
            string normalizado = texto
                .Replace("_", string.Empty)
                .Replace("-", string.Empty)
                .Replace(" ", string.Empty);

            bool mencionaToken =
                texto.Contains("access token") ||
                texto.Contains("access_token") ||
                texto.Contains("bearer") ||
                normalizado.Contains("tokenold");

            bool tokenInvalidoOuExpirado =
                texto.Contains("expired") ||
                texto.Contains("expirado") ||
                texto.Contains("expirou") ||
                texto.Contains("invalid") ||
                texto.Contains("unauthorized") ||
                texto.Contains("nao autorizado") ||
                texto.Contains("não autorizado");

            return mencionaToken && tokenInvalidoOuExpirado;
        }

        /// <summary>
        /// Obtem produtos com preco alterado a partir do timestamp informado.
        /// </summary>
        public async Task<string> GetPrecosAlteradosAsync(long timestamp, int page = 1)
        {
            try
            {
                string url = $"{_router.AtualizacaoPrecoRouter}{timestamp}/page/{page}";
                return await GetAutenticadoAsync(url);
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao obter precos alterados: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Obtem Promoções ativas
        /// </summary>
        public async Task<string> GetPromocoesAsync()
        {
            try
            {
                return await GetAutenticadoAsync(_router.PromocaoRouter);
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao obter promoçõess: {ex.Message}", ex);
            }

        }

        /// <summary>
        /// Obtem informações da empresa
        /// </summary>
        public async Task<string> GetEmpresaAsync()
        {
            try
            {
                return await GetAutenticadoAsync(_router.CompanyRouter);
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao obter empresa: {ex.Message}", ex);
            }
        }

        #endregion

        #region Testes

        /// <summary>
        /// Testa a conexÃ£o com a API
        /// </summary>
        /// <summary>
        /// Testa se a API está acessível e credenciais básicas estão corretas
        /// NÃO requer Client Secret (que só existe após cadastrar dispositivo)
        /// </summary>
        public async Task<bool> TestarConexaoAsync()
        {
            try
            {
                // ⭐ CORREÇÃO: Testar conexão sem ClientSecret
                // ClientSecret só existe APÓS cadastrar o dispositivo

                // Apenas verificar se consegue acessar a API
                var response = await _httpClient.GetAsync(_config.BaseURL);
                return response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.Unauthorized;
            }
            catch
            {
                return false;
            }
        }

        public async Task<string> BuscarVendaRawJsonAsync(string numeroVenda)
        {
            try
            {
                string url = $"{_config.BaseURL}/softauth/api/v2/produtos/vendas?numero_venda={numeroVenda}";
                string jsonBruto = await GetAutenticadoAsync(url, "v2");

                // ⭐ O PULO DO GATO: Grava o JSON em um arquivo na pasta "logs" para tu analisares
                string pastaLogs = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                if (!Directory.Exists(pastaLogs))
                    Directory.CreateDirectory(pastaLogs);

                string caminhoArquivo = Path.Combine(pastaLogs, $"venda_{numeroVenda}.json");
                File.WriteAllText(caminhoArquivo, jsonBruto);

                // Mostra um aviso rápido no console ou debug para saberes onde foi salvo
                System.Diagnostics.Debug.WriteLine($"[JSON LOG] Salvo com sucesso em: {caminhoArquivo}");

                return jsonBruto;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao capturar JSON: {ex.Message}");
                throw;
            }
        }

        public async Task<string> ObterJsonBrutoVendaAsync(int numeroVenda)
        {
            try
            {
                // Altere a rota abaixo caso o nome da propriedade no seu SoftcomShopRouter seja diferente
                string url = $"{_config.BaseURL}/softauth/api/v2/produtos/vendas?numero_venda={numeroVenda}";
                string jsonBruto = await GetAutenticadoAsync(url, "v2");

                // Salva direto na pasta do seu executável (bin/Debug)
                string caminhoArquivo = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"venda_{numeroVenda}.json");
                File.WriteAllText(caminhoArquivo, jsonBruto);

                return jsonBruto;
            }
            catch (Exception ex)
            {
                string caminhoErro = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "erro_requisicao.txt");
                File.WriteAllText(caminhoErro, ex.ToString());
                throw;
            }
        }

        #endregion

        /// <summary>
        /// Libera recursos
        /// </summary>
        public void Dispose()
        {
            _httpClient?.Dispose();
        }


    }

    public class SoftcomShopApiException : Exception
    {
        public HttpStatusCode StatusCode { get; private set; }
        public string ResponseContent { get; private set; }
        public string ReasonPhrase { get; private set; }

        public SoftcomShopApiException(HttpStatusCode statusCode, string responseContent, string reasonPhrase)
            : base($"API SoftcomShop retornou HTTP {(int)statusCode} ({reasonPhrase}).")
        {
            StatusCode = statusCode;
            ResponseContent = responseContent;
            ReasonPhrase = reasonPhrase;
        }
    }
}
