using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

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
        private string _currentToken;

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
        public async Task<string> GetTokenAsync()
        {
            try
            {
                var parameters = new Dictionary<string, string>
                {
                    { "client_secret", _config.ClientSecret },
                    { "client_id", _config.ClientId },
                    { "grant_type", "client_credentials" }
                };

                var content = new FormUrlEncodedContent(parameters);

                var response = await _httpClient.PostAsync(_router.TokenRouter, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonConvert.DeserializeObject<dynamic>(responseContent);
                    _currentToken = result.data.token;
                    return _currentToken;
                }

                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao obter token: {ex.Message}", ex);
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
                    var result = JsonConvert.DeserializeObject<dynamic>(responseContent);
                    return result.data.client_secret;
                }

                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao cadastrar dispositivo: {ex.Message}", ex);
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
                if (string.IsNullOrEmpty(_currentToken))
                {
                    await GetTokenAsync();
                }

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_currentToken}");

                string url;
                if (versao == "v2")
                {
                    _httpClient.DefaultRequestHeaders.Add("Api-Version", "v2");
                    url = $"{_router.ProductsRouterV2}?page={page}";
                }
                else
                {
                    url = $"{_router.ProductsRouter}/page/{page}";
                }

                var response = await _httpClient.GetAsync(url);
                return await response.Content.ReadAsStringAsync();
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
                if (string.IsNullOrEmpty(_currentToken))
                {
                    await GetTokenAsync();
                }

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_currentToken}");

                string url;
                if (versao == "v2")
                {
                    _httpClient.DefaultRequestHeaders.Add("Api-Version", "v2");
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

                var response = await _httpClient.GetAsync(url);
                return await response.Content.ReadAsStringAsync();
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
                if (string.IsNullOrEmpty(_currentToken))
                {
                    await GetTokenAsync();
                }

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_currentToken}");
                _httpClient.DefaultRequestHeaders.Add("Api-Version", "v2");

                string url = $"{_router.ComprasV2Router}?numero_nota_fiscal={numeroNota}&page={page}";
                var response = await _httpClient.GetAsync(url);
                return await response.Content.ReadAsStringAsync();
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
                if (string.IsNullOrEmpty(_currentToken))
                    await GetTokenAsync();

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _currentToken);

                string url = $"{_router.VendasRouter}{numeroVenda}?bloquear=False";

                System.Diagnostics.Debug.WriteLine("=======================================");
                System.Diagnostics.Debug.WriteLine($"URL........: {url}");
                System.Diagnostics.Debug.WriteLine($"TOKEN......: {_currentToken}");
                System.Diagnostics.Debug.WriteLine("=======================================");

                var response = await _httpClient.GetAsync(url);

                string retorno = await response.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine($"STATUS HTTP: {(int)response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"REASON.....: {response.ReasonPhrase}");
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
        /// Obtem produtos com preco alterado a partir do timestamp informado.
        /// </summary>
        public async Task<string> GetPrecosAlteradosAsync(long timestamp, int page = 1)
        {
            try
            {
                if (string.IsNullOrEmpty(_currentToken))
                {
                    await GetTokenAsync();
                }

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_currentToken}");

                string url = $"{_router.AtualizacaoPrecoRouter}{timestamp}/page/{page}";

                var response = await _httpClient.GetAsync(url);
                return await response.Content.ReadAsStringAsync();
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
                if (string.IsNullOrEmpty(_currentToken))
                {
                    await GetTokenAsync();
                }

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_currentToken}");

                var response = await _httpClient.GetAsync(_router.PromocaoRouter);
                return await response.Content.ReadAsStringAsync();
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
                if (string.IsNullOrEmpty(_currentToken))
                {
                    await GetTokenAsync();
                }

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_currentToken}");

                var response = await _httpClient.GetAsync(_router.CompanyRouter);
                return await response.Content.ReadAsStringAsync();
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
                // 1. Garante que o token está atualizado
                string token = await GetTokenAsync();
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                // 2. Monta a URL da rota de vendas (ajusta para a tua rota real de vendas se necessário)
                string url = $"{_config.BaseURL}/softauth/api/v2/produtos/vendas?numero_venda={numeroVenda}";

                // 3. Faz a requisição HTTP
                HttpResponseMessage response = await _httpClient.GetAsync(url);

                // 4. Lê o conteúdo bruto como STRING (o JSON puro)
                string jsonBruto = await response.Content.ReadAsStringAsync();

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
                string token = await GetTokenAsync();
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                // Altere a rota abaixo caso o nome da propriedade no seu SoftcomShopRouter seja diferente
                string url = $"{_config.BaseURL}/softauth/api/v2/produtos/vendas?numero_venda={numeroVenda}";

                var response = await _httpClient.GetAsync(url);
                string jsonBruto = await response.Content.ReadAsStringAsync();

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
}
