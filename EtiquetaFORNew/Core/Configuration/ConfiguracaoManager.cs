using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;

namespace EtiquetaFORNew
{
    /// <summary>
    /// Gerenciador moderno de configurações usando JSON
    /// Mantém compatibilidade com sistema XML antigo
    /// </summary>
    public static class ConfiguracaoManager
    {
        private static readonly string PASTA_BASE = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "SistemaEtiquetas"
        );

        private static readonly string PASTA_CONFIGURACOES = Path.Combine(PASTA_BASE, "Configuracoes");
        private static readonly string ARQUIVO_ULTIMO_TEMPLATE = Path.Combine(PASTA_CONFIGURACOES, "_ultimo_template.txt");

        // Compatibilidade com sistema antigo
        private static readonly string CAMINHO_XML_ANTIGO = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "EtiquetaFornew", "configuracoes.xml"
        );

        static ConfiguracaoManager()
        {
            // Cria estrutura de pastas
            if (!Directory.Exists(PASTA_CONFIGURACOES))
            {
                Directory.CreateDirectory(PASTA_CONFIGURACOES);
            }

            // Migra configuração XML antiga se existir
            MigrarConfiguracaoXMLSeNecessario();
        }

        #region Salvar/Carregar Configurações

        /// <summary>
        /// Salva configuração vinculada a um template específico
        /// </summary>
        public static bool SalvarConfiguracao(string nomeTemplate, ConfiguracaoEtiqueta config)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(nomeTemplate))
                    throw new ArgumentException("Nome do template não pode ser vazio");

                // Define o nome da etiqueta igual ao template se não estiver definido
                if (string.IsNullOrEmpty(config.NomeEtiqueta))
                    config.NomeEtiqueta = nomeTemplate;

                string caminhoArquivo = ObterCaminhoConfiguracao(nomeTemplate);
                string pastaConfig = Path.GetDirectoryName(caminhoArquivo);

                if (!Directory.Exists(pastaConfig))
                    Directory.CreateDirectory(pastaConfig);

                // Serializa com formatação bonita
                var settings = new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    NullValueHandling = NullValueHandling.Ignore
                };

                string json = JsonConvert.SerializeObject(config, settings);
                File.WriteAllText(caminhoArquivo, json);

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao salvar configuração: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Carrega configuração vinculada a um template específico
        /// </summary>
        public static ConfiguracaoEtiqueta CarregarConfiguracao(string nomeTemplate)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(nomeTemplate))
                    return null;

                string caminhoArquivo = ObterCaminhoConfiguracao(nomeTemplate);

                if (!File.Exists(caminhoArquivo))
                {
                    // Tenta buscar no sistema antigo
                    return TentarCarregarDoSistemaAntigoXML(nomeTemplate);
                }

                string json = File.ReadAllText(caminhoArquivo);
                var config = JsonConvert.DeserializeObject<ConfiguracaoEtiqueta>(json);

                return config;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao carregar configuração: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Verifica se existe configuração salva para um template
        /// </summary>
        public static bool ExisteConfiguracao(string nomeTemplate)
        {
            if (string.IsNullOrWhiteSpace(nomeTemplate))
                return false;

            string caminhoArquivo = ObterCaminhoConfiguracao(nomeTemplate);
            return File.Exists(caminhoArquivo);
        }

        /// <summary>
        /// Exclui configuração de um template
        /// </summary>
        public static bool ExcluirConfiguracao(string nomeTemplate)
        {
            try
            {
                string caminhoArquivo = ObterCaminhoConfiguracao(nomeTemplate);

                if (File.Exists(caminhoArquivo))
                {
                    File.Delete(caminhoArquivo);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao excluir configuração: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Lista todas as configurações salvas
        /// </summary>
        public static List<string> ListarConfiguracoes()
        {
            var configuracoes = new List<string>();

            try
            {
                if (!Directory.Exists(PASTA_CONFIGURACOES))
                    return configuracoes;

                var arquivos = Directory.GetFiles(PASTA_CONFIGURACOES, "*.json", SearchOption.AllDirectories);

                foreach (var arquivo in arquivos)
                {
                    // Ignora arquivo especial
                    if (Path.GetFileName(arquivo).StartsWith("_"))
                        continue;

                    string nomeConfig = Path.GetFileNameWithoutExtension(arquivo);
                    configuracoes.Add(nomeConfig);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao listar configurações: {ex.Message}");
            }

            return configuracoes.OrderBy(x => x).ToList();
        }

        #endregion

        #region Gerenciar Último Template Usado

        /// <summary>
        /// Salva qual foi o último template usado
        /// </summary>
        public static void SalvarUltimoTemplateUsado(string nomeTemplate)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(nomeTemplate))
                {
                    File.WriteAllText(ARQUIVO_ULTIMO_TEMPLATE, nomeTemplate);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao salvar último template: {ex.Message}");
            }
        }

        /// <summary>
        /// Carrega qual foi o último template usado
        /// </summary>
        public static string CarregarUltimoTemplateUsado()
        {
            try
            {
                if (File.Exists(ARQUIVO_ULTIMO_TEMPLATE))
                {
                    return File.ReadAllText(ARQUIVO_ULTIMO_TEMPLATE).Trim();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao carregar último template: {ex.Message}");
            }

            return null;
        }

        #endregion

        #region Compatibilidade com Sistema Antigo XML

        /// <summary>
        /// Migra configuração do sistema XML antigo para JSON (uma única vez)
        /// </summary>
        private static void MigrarConfiguracaoXMLSeNecessario()
        {
            try
            {
                // Se já existe configuração JSON, não precisa migrar
                var configsExistentes = ListarConfiguracoes();
                if (configsExistentes.Count > 0)
                    return;

                // Tenta carregar do XML antigo
                if (File.Exists(CAMINHO_XML_ANTIGO))
                {
                    var configAntiga = GerenciadorConfiguracoesEtiqueta.CarregarConfiguracaoPadrao();

                    if (configAntiga != null)
                    {
                        // Salva no novo formato
                        string nomeTemplate = string.IsNullOrEmpty(configAntiga.NomeEtiqueta)
                            ? "Configuração Migrada"
                            : configAntiga.NomeEtiqueta;

                        SalvarConfiguracao(nomeTemplate, configAntiga);
                        SalvarUltimoTemplateUsado(nomeTemplate);

                        System.Diagnostics.Debug.WriteLine($"Configuração migrada de XML para JSON: {nomeTemplate}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro na migração XML: {ex.Message}");
            }
        }

        /// <summary>
        /// Tenta carregar configuração do sistema antigo XML
        /// </summary>
        private static ConfiguracaoEtiqueta TentarCarregarDoSistemaAntigoXML(string nomeTemplate)
        {
            try
            {
                // Carrega todas as configurações antigas
                var configsAntigas = GerenciadorConfiguracoesEtiqueta.CarregarTodasConfiguracoes();

                // Procura por nome
                var configAntiga = configsAntigas.FirstOrDefault(c =>
                    c.NomePapel.Equals(nomeTemplate, StringComparison.OrdinalIgnoreCase) ||
                    c.NomeEtiqueta.Equals(nomeTemplate, StringComparison.OrdinalIgnoreCase));

                if (configAntiga != null)
                {
                    // Converte para novo formato
                    var config = GerenciadorConfiguracoesEtiqueta.ConverterPapelParaConfig(
                        configAntiga,
                        configAntiga.NomeEtiqueta);

                    // Salva no novo formato para próximas vezes
                    SalvarConfiguracao(nomeTemplate, config);

                    return config;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao carregar do sistema antigo: {ex.Message}");
            }

            return null;
        }

        #endregion

        #region Métodos Auxiliares

        /// <summary>
        /// Obtém caminho do arquivo de configuração para um template
        /// </summary>
        private static string ObterCaminhoConfiguracao(string nomeTemplate)
        {
            // Sanitiza nome do template para nome de arquivo válido
            string nomeArquivo = SanitizarNomeArquivo(nomeTemplate);
            return Path.Combine(PASTA_CONFIGURACOES, $"{nomeArquivo}.json");
        }

        /// <summary>
        /// Remove caracteres inválidos de nome de arquivo
        /// </summary>
        private static string SanitizarNomeArquivo(string nome)
        {
            var caracteresInvalidos = Path.GetInvalidFileNameChars();

            foreach (char c in caracteresInvalidos)
            {
                nome = nome.Replace(c, '_');
            }

            return nome;
        }

        /// <summary>
        /// Cria uma configuração padrão
        /// </summary>
        public static ConfiguracaoEtiqueta CriarConfiguracaoPadrao(string nomeTemplate)
        {
            return new ConfiguracaoEtiqueta
            {
                NomeEtiqueta = nomeTemplate,
                ImpressoraPadrao = "BTP-L42(D)",
                PapelPadrao = "Tamanho do papel-SoftcomBar",
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


        #endregion

        #region Backup e Restauração

        /// <summary>
        /// Cria backup de todas as configurações
        /// </summary>
        public static bool CriarBackup(string caminhoDestino)
        {
            try
            {
                if (!Directory.Exists(PASTA_CONFIGURACOES))
                    return false;

                // Cria pasta de backup
                string pastaBackup = Path.Combine(caminhoDestino, $"Backup_Configs_{DateTime.Now:yyyyMMdd_HHmmss}");
                Directory.CreateDirectory(pastaBackup);

                // Copia todos os arquivos JSON
                var arquivos = Directory.GetFiles(PASTA_CONFIGURACOES, "*.json", SearchOption.AllDirectories);

                foreach (var arquivo in arquivos)
                {
                    string nomeArquivo = Path.GetFileName(arquivo);
                    string destino = Path.Combine(pastaBackup, nomeArquivo);
                    File.Copy(arquivo, destino, true);
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao criar backup: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Restaura configurações de um backup
        /// </summary>
        public static bool RestaurarBackup(string caminhoBackup)
        {
            try
            {
                if (!Directory.Exists(caminhoBackup))
                    return false;

                var arquivos = Directory.GetFiles(caminhoBackup, "*.json");

                foreach (var arquivo in arquivos)
                {
                    string nomeArquivo = Path.GetFileName(arquivo);
                    string destino = Path.Combine(PASTA_CONFIGURACOES, nomeArquivo);
                    File.Copy(arquivo, destino, true);
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao restaurar backup: {ex.Message}");
                return false;
            }
        }

        #endregion

    }
}