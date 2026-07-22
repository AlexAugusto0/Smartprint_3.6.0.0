using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace EtiquetaFORNew
{
    /// <summary>
    /// Gerenciador de configurações de etiquetas para uso no form principal
    /// </summary>
    public static class GerenciadorConfiguracoesEtiqueta
    {
        private static readonly string CAMINHO_CONFIGURACOES =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "EtiquetaFornew", "configuracoes.xml");

        private static readonly string CAMINHO_LISTA_PAPEIS =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "EtiquetaFornew", "modelos_papel.xml");

        /// <summary>
        /// Carrega a configuração padrão (última usada)
        /// </summary>
        public static ConfiguracaoEtiqueta CarregarConfiguracaoPadrao()
        {
            try
            {
                if (!File.Exists(CAMINHO_CONFIGURACOES))
                {
                    return CriarConfiguracaoPadrao();
                }

                XmlSerializer serializer = new XmlSerializer(typeof(ConfiguracaoEtiqueta));
                using (StreamReader reader = new StreamReader(CAMINHO_CONFIGURACOES))
                {
                    return (ConfiguracaoEtiqueta)serializer.Deserialize(reader);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao carregar configuração: {ex.Message}");
                return CriarConfiguracaoPadrao();
            }
        }

        /// <summary>
        /// Salva a configuração como padrão
        /// </summary>
        public static bool SalvarConfiguracaoPadrao(ConfiguracaoEtiqueta config)
        {
            try
            {
                string diretorio = Path.GetDirectoryName(CAMINHO_CONFIGURACOES);
                if (!Directory.Exists(diretorio))
                {
                    Directory.CreateDirectory(diretorio);
                }

                XmlSerializer serializer = new XmlSerializer(typeof(ConfiguracaoEtiqueta));
                using (StreamWriter writer = new StreamWriter(CAMINHO_CONFIGURACOES))
                {
                    serializer.Serialize(writer, config);
                }
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao salvar configuração: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Carrega todas as configurações de papel salvas
        /// </summary>
        public static List<ConfiguracaoPapel> CarregarTodasConfiguracoes()
        {
            List<ConfiguracaoPapel> papeis = new List<ConfiguracaoPapel>();

            try
            {
                if (!File.Exists(CAMINHO_LISTA_PAPEIS))
                    return papeis;

                XmlSerializer serializer = new XmlSerializer(typeof(List<ConfiguracaoPapel>));
                using (StreamReader reader = new StreamReader(CAMINHO_LISTA_PAPEIS))
                {
                    papeis = (List<ConfiguracaoPapel>)serializer.Deserialize(reader);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao carregar papéis: {ex.Message}");
            }

            return papeis;
        }

        /// <summary>
        /// Abre o diálogo de configuração e retorna a configuração selecionada
        /// </summary>
        public static ConfiguracaoEtiqueta AbrirDialogoConfiguracao(Form parent, ConfiguracaoEtiqueta configAtual = null)
        {
            using (FormConfigEtiqueta formConfig = new FormConfigEtiqueta(configAtual))
            {
                if (formConfig.ShowDialog(parent) == DialogResult.OK)
                {
                    return formConfig.Configuracao;
                }
            }
            return configAtual;
        }

        /// <summary>
        /// Converte ConfiguracaoPapel para ConfiguracaoEtiqueta
        /// </summary>
        public static ConfiguracaoEtiqueta ConverterPapelParaConfig(ConfiguracaoPapel papel, string impressora = null)
        {
            return new ConfiguracaoEtiqueta
            {
                NomeEtiqueta = papel.NomeEtiqueta,
                ImpressoraPadrao = impressora ?? "",
                PapelPadrao = papel.NomePapel,
                LarguraEtiqueta = papel.Largura,
                AlturaEtiqueta = papel.Altura,
                NumColunas = papel.NumColunas,
                NumLinhas = papel.NumLinhas,
                EspacamentoColunas = papel.EspacamentoColunas,
                EspacamentoLinhas = papel.EspacamentoLinhas,
                MargemSuperior = papel.MargemSuperior,
                MargemInferior = papel.MargemInferior,
                MargemEsquerda = papel.MargemEsquerda,
                MargemDireita = papel.MargemDireita
            };
        }

        /// <summary>
        /// Cria uma configuração padrão
        /// </summary>
        private static ConfiguracaoEtiqueta CriarConfiguracaoPadrao()
        {
            return new ConfiguracaoEtiqueta
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
        private static bool SalvarTodasConfiguracoes(List<ConfiguracaoPapel> papeis)
        {
            try
            {
                string diretorio = Path.GetDirectoryName(CAMINHO_LISTA_PAPEIS);
                if (!Directory.Exists(diretorio))
                {
                    Directory.CreateDirectory(diretorio);
                }

                XmlSerializer serializer = new XmlSerializer(typeof(List<ConfiguracaoPapel>));
                using (StreamWriter writer = new StreamWriter(CAMINHO_LISTA_PAPEIS))
                {
                    serializer.Serialize(writer, papeis);
                }
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao salvar lista de papéis: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Lista apenas os nomes de todas as configurações de papel salvas.
        /// </summary>
        public static List<string> ListarNomesConfiguracoes()
        {
            // Usa o CarregarTodasConfiguracoes existente para obter a lista completa
            return CarregarTodasConfiguracoes()
                .Select(c => c.NomePapel)
                .ToList();
        }

        /// <summary>
        /// Carrega uma configuração de papel específica pelo nome.
        /// </summary>
        public static ConfiguracaoPapel CarregarConfiguracao(string nomePapel)
        {
            return CarregarTodasConfiguracoes()
                .FirstOrDefault(c => c.NomePapel.Equals(nomePapel, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Adiciona ou atualiza uma configuração de papel na lista salva.
        /// </summary>
        public static bool SalvarConfiguracao(ConfiguracaoPapel config)
        {
            List<ConfiguracaoPapel> papeis = CarregarTodasConfiguracoes();

            int index = papeis.FindIndex(c => c.NomePapel.Equals(config.NomePapel, StringComparison.OrdinalIgnoreCase));

            if (index >= 0)
            {
                papeis[index] = config; // Atualiza
            }
            else
            {
                config.DataCriacao = DateTime.Now; // Adiciona data de criação para novos
                papeis.Add(config); // Adiciona
            }

            return SalvarTodasConfiguracoes(papeis);
        }

        /// <summary>
        /// Exclui uma configuração de papel da lista salva.
        /// </summary>
        public static bool ExcluirConfiguracao(string nomePapel)
        {
            List<ConfiguracaoPapel> papeis = CarregarTodasConfiguracoes();
            int countAntes = papeis.Count;

            // Remove o item que corresponde ao nome
            papeis.RemoveAll(c => c.NomePapel.Equals(nomePapel, StringComparison.OrdinalIgnoreCase));

            if (papeis.Count < countAntes)
            {
                return SalvarTodasConfiguracoes(papeis);
            }

            return false;
        }

        /// <summary>
        /// Converte ConfiguracaoEtiqueta para ConfiguracaoPapel
        /// </summary>
        public static ConfiguracaoPapel ConverterConfigParaPapel(ConfiguracaoEtiqueta config)
        {
            // A data de criação será definida no método SalvarConfiguracao se for uma nova.
            return new ConfiguracaoPapel
            {
                NomeEtiqueta = config.NomeEtiqueta,
                NomePapel = config.PapelPadrao, // Usa PapelPadrao como NomePapel na lista de configs
                Largura = config.LarguraEtiqueta,
                Altura = config.AlturaEtiqueta,
                NumColunas = config.NumColunas,
                NumLinhas = config.NumLinhas,
                EspacamentoColunas = config.EspacamentoColunas,
                EspacamentoLinhas = config.EspacamentoLinhas,
                MargemSuperior = config.MargemSuperior,
                MargemInferior = config.MargemInferior,
                MargemEsquerda = config.MargemEsquerda,
                MargemDireita = config.MargemDireita,
            };
        }

    }
}