using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace EtiquetaFORNew
{
    /// <summary>
    /// Classe para representar uma impressora
    /// </summary>
    public class ImpressoraInfo
    {
        [JsonProperty("nome")]
        public string Nome { get; set; }

        [JsonProperty("imagemRecurso")]
        public string ImagemRecurso { get; set; }

        [JsonProperty("driverUrl")]
        public string DriverUrl { get; set; }

        [JsonProperty("fabricante")]
        public string Fabricante { get; set; }

        [JsonProperty("aliases")]
        public List<string> Aliases { get; set; }

        [JsonProperty("hardwareIds")]
        public List<string> HardwareIds { get; set; }

        [JsonProperty("deviceIds")]
        public List<string> DeviceIds { get; set; }

        /// <summary>
        /// Obtém a imagem da impressora a partir dos recursos
        /// </summary>
        public Image ObterImagem()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();

                // Tenta diferentes formatos de nome de recurso
                string[] possiveisNomes = new string[]
                {
                    // Para imagens na pasta Resources/Impressoras
                    $"EtiquetaFORNew.Resources.Impressoras.{ImagemRecurso}",
                    // Para imagens na pasta Impressoras (raiz)
                    $"EtiquetaFORNew.Impressoras.{ImagemRecurso}",
                    // Para imagens direto na pasta Resources
                    $"EtiquetaFORNew.Resources.{ImagemRecurso}",
                    // Sem namespace
                    $"{ImagemRecurso}",
                };

                // Primeiro tenta os nomes exatos
                foreach (string nomeTentativa in possiveisNomes)
                {
                    using (Stream stream = assembly.GetManifestResourceStream(nomeTentativa))
                    {
                        if (stream != null)
                        {
                            return Image.FromStream(stream);
                        }
                    }
                }

                // Se não encontrou, tenta buscar por nome parcial
                string[] recursos = assembly.GetManifestResourceNames();
                string nomeArquivo = Path.GetFileName(ImagemRecurso);
                string recursoEncontrado = recursos.FirstOrDefault(r =>
                    r.EndsWith(nomeArquivo, StringComparison.OrdinalIgnoreCase) ||
                    r.EndsWith(ImagemRecurso, StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrEmpty(recursoEncontrado))
                {
                    using (Stream stream = assembly.GetManifestResourceStream(recursoEncontrado))
                    {
                        if (stream != null)
                        {
                            return Image.FromStream(stream);
                        }
                    }
                }

                // FALLBACK: Se não encontrar nos recursos, tenta carregar de arquivo externo
                // Tenta na pasta Resources/Impressoras
                string caminhoExterno = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Impressoras", ImagemRecurso);
                if (File.Exists(caminhoExterno))
                {
                    return Image.FromFile(caminhoExterno);
                }

                // Tenta na pasta Impressoras (raiz)
                caminhoExterno = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Impressoras", ImagemRecurso);
                if (File.Exists(caminhoExterno))
                {
                    return Image.FromFile(caminhoExterno);
                }

                // Tenta diretamente na pasta do executável
                caminhoExterno = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ImagemRecurso);
                if (File.Exists(caminhoExterno))
                {
                    return Image.FromFile(caminhoExterno);
                }

                return null;
            }
            catch (Exception ex)
            {
                // Log do erro (opcional)
                System.Diagnostics.Debug.WriteLine($"Erro ao carregar imagem {ImagemRecurso}: {ex.Message}");
                return null;
            }
        }
    }

    /// <summary>
    /// Classe para representar a estrutura do JSON
    /// </summary>
    public class ImpressorasConfig
    {
        [JsonProperty("impressoras")]
        public List<ImpressoraInfo> Impressoras { get; set; }
    }

    /// <summary>
    /// Gerenciador de impressoras - carrega a partir do JSON
    /// </summary>
    public static class ImpressoraManager
    {
        private static List<ImpressoraInfo> _impressoras;

        /// <summary>
        /// Carrega a lista de impressoras do arquivo JSON
        /// </summary>
        public static List<ImpressoraInfo> CarregarImpressoras()
        {
            if (_impressoras != null)
                return _impressoras;

            try
            {
                // Tenta carregar do recurso embarcado primeiro
                string json = CarregarJsonDoRecurso();

                // Se não encontrar no recurso, tenta carregar de arquivo externo
                if (string.IsNullOrEmpty(json))
                {
                    // Tenta na pasta Resources
                    string caminhoJson = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "impressoras.json");
                    if (File.Exists(caminhoJson))
                    {
                        json = File.ReadAllText(caminhoJson);
                    }
                    else
                    {
                        // Tenta na raiz
                        caminhoJson = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "impressoras.json");
                        if (File.Exists(caminhoJson))
                        {
                            json = File.ReadAllText(caminhoJson);
                        }
                    }
                }

                if (!string.IsNullOrEmpty(json))
                {
                    var config = JsonConvert.DeserializeObject<ImpressorasConfig>(json);
                    _impressoras = config?.Impressoras ?? new List<ImpressoraInfo>();
                }
                else
                {
                    _impressoras = new List<ImpressoraInfo>();
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(
                    $"Erro ao carregar configuração de impressoras: {ex.Message}",
                    "Erro",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error);

                _impressoras = new List<ImpressoraInfo>();
            }

            return _impressoras;
        }

        /// <summary>
        /// Tenta carregar o JSON dos recursos embarcados
        /// </summary>
        private static string CarregarJsonDoRecurso()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();

                // Tenta diferentes nomes possíveis (agora incluindo pasta Resources)
                string[] possiveisNomes = new string[]
                {
                    // JSON na pasta Resources
                    "EtiquetaFORNew.Resources.impressoras.json",
                    // JSON na raiz do projeto
                    "EtiquetaFORNew.impressoras.json",
                    // Sem namespace
                    "impressoras.json"
                };

                foreach (string nomeRecurso in possiveisNomes)
                {
                    using (Stream stream = assembly.GetManifestResourceStream(nomeRecurso))
                    {
                        if (stream != null)
                        {
                            using (StreamReader reader = new StreamReader(stream))
                            {
                                return reader.ReadToEnd();
                            }
                        }
                    }
                }

                // Se não encontrou pelos nomes exatos, procura por nome parcial
                string[] recursos = assembly.GetManifestResourceNames();
                string recursoEncontrado = recursos.FirstOrDefault(r =>
                    r.EndsWith("impressoras.json", StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrEmpty(recursoEncontrado))
                {
                    using (Stream stream = assembly.GetManifestResourceStream(recursoEncontrado))
                    {
                        if (stream != null)
                        {
                            using (StreamReader reader = new StreamReader(stream))
                            {
                                return reader.ReadToEnd();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao carregar JSON do recurso: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Recarrega a lista de impressoras
        /// </summary>
        public static void Recarregar()
        {
            _impressoras = null;
            CarregarImpressoras();
        }

        /// <summary>
        /// Busca uma impressora pelo nome
        /// </summary>
        public static ImpressoraInfo BuscarPorNome(string nome)
        {
            return CarregarImpressoras().FirstOrDefault(i =>
                i.Nome.Equals(nome, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Obtém todos os nomes de impressoras
        /// </summary>
        public static List<string> ObterNomes()
        {
            return CarregarImpressoras().Select(i => i.Nome).ToList();
        }

        /// <summary>
        /// Lista todos os recursos embarcados (para debug)
        /// </summary>
        public static string[] ListarRecursosEmbarcados()
        {
            var assembly = Assembly.GetExecutingAssembly();
            return assembly.GetManifestResourceNames();
        }
    }
}
