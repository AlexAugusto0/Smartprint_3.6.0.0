using Newtonsoft.Json;
using System;
using System.IO;

namespace EtiquetaFORNew
{
    /// <summary>
    /// Configurações do SoftcomShop
    /// </summary>
    public class SoftcomShopConfig
    {
        public string BaseURL { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string CompanyName { get; set; }
        public string CompanyCNPJ { get; set; }
        public string DeviceName { get; set; }
        public string DeviceId { get; set; }
        public string DataSync { get; set; }
        public string CaminhoBancoDados { get; set; }

        /// <summary>
        /// Construtor padrão com valores iniciais
        /// </summary>
        public SoftcomShopConfig()
        {
            BaseURL = "https://api.softcomshop.com.br";
            ClientId = "";
            ClientSecret = "";
            CompanyName = "";
            CompanyCNPJ = "";
            DeviceName = Environment.MachineName;
            DeviceId = GenerateDeviceId();
            DataSync = "";
            CaminhoBancoDados = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "database", "softcom_shop.db");
        }

        /// <summary>
        /// Gera um Device ID único baseado no nome do computador e timestamp
        /// </summary>
        private string GenerateDeviceId()
        {
            string machineName = Environment.MachineName;
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            return $"{machineName}_{timestamp}";
        }

        /// <summary>
        /// Valida se existem dados suficientes para vincular o dispositivo e autenticar.
        /// O ClientSecret e derivado do cadastro do dispositivo e pode ser renovado.
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(BaseURL) &&
                   !string.IsNullOrWhiteSpace(ClientId) &&
                   !string.IsNullOrWhiteSpace(CompanyName) &&
                   !string.IsNullOrWhiteSpace(CompanyCNPJ) &&
                   !string.IsNullOrWhiteSpace(DeviceName) &&
                   !string.IsNullOrWhiteSpace(DeviceId);
        }

        /// <summary>
        /// Serializa a configuração para JSON
        /// </summary>
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        /// <summary>
        /// Desserializa a configuração do JSON
        /// </summary>
        public static SoftcomShopConfig FromJson(string json)
        {
            try
            {
                return JsonConvert.DeserializeObject<SoftcomShopConfig>(json);
            }
            catch
            {
                return new SoftcomShopConfig();
            }
        }
    }

    /// <summary>
    /// Rotas da API SoftcomShop
    /// </summary>
    public class SoftcomShopRouter
    {
        private readonly string _baseUrl;

        public SoftcomShopRouter(string baseUrl)
        {
            _baseUrl = baseUrl;
        }

        public string DeviceRouter => $"{_baseUrl}/softauth/device/add";
        public string ProductsRouter => $"{_baseUrl}/softauth/api/produtos/produtos";
        public string ProductsRouterV2 => $"{_baseUrl}/softauth/api/v2/produtos/produtos";
        public string CompanyRouter => $"{_baseUrl}/softauth/api/empresa";
        public string TokenRouter => $"{_baseUrl}/softauth/authentication/token";
        public string PromocaoRouter => $"{_baseUrl}/softauth/api/produtos/promocao";
        public string AtualizacaoPrecoRouter => $"{_baseUrl}/softauth/api/produtos/produtos/data_atualizacao_preco/";
        public string DataEntradaNotaFiscal => $"{_baseUrl}/softauth/api/produtos/produtos/data_entrada/";
        public string ComprasV2Router => $"{_baseUrl}/softauth/api/v2/produtos/compras";
        public string DataEntradaNotaFiscalV2 => $"{_baseUrl}/softauth/api/v2/produtos/compras?data_hora_entrada=";
        public string VendasRouter => $"{_baseUrl}/softauth/api/vendas/vendas/completa/";
        public string VendasFiltroDistribuidoraRouter => $"{_baseUrl}/softauth/api/vendas/filtro";
        public string VendasCompletaDistribuidoraRouter => $"{_baseUrl}/softauth/api/vendas/vendas/completa";
        public string ClientesRouter => $"{_baseUrl}/softauth/api/clientes/clientes";
        public string ClientesRouterV2 => $"{_baseUrl}/softauth/api/v2/clientes/clientes";
        public string BairroRouter => $"{_baseUrl}/softauth/api/endereco/bairro";
    }
}
