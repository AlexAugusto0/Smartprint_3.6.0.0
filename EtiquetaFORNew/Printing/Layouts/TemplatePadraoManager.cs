using System;
using System.IO;
using Newtonsoft.Json;

namespace EtiquetaFORNew
{
    /// <summary>
    /// Gerenciador para marcar e obter template padrão
    /// </summary>
    public static class TemplatePadraoManager
    {
        private static readonly string ARQUIVO_TEMPLATE_PADRAO = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "SistemaEtiquetas",
            "Templates",
            "_template_padrao.txt"
        );

        /// <summary>
        /// Define um template como padrão
        /// </summary>
        public static bool DefinirTemplatePadrao(string nomeTemplate)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(nomeTemplate))
                    return false;

                // Cria pasta se não existir
                string pasta = Path.GetDirectoryName(ARQUIVO_TEMPLATE_PADRAO);
                if (!Directory.Exists(pasta))
                    Directory.CreateDirectory(pasta);

                // Salva nome do template padrão
                File.WriteAllText(ARQUIVO_TEMPLATE_PADRAO, nomeTemplate);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao definir template padrão: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Obtém o nome do template padrão
        /// </summary>
        public static string ObterTemplatePadrao()
        {
            try
            {
                if (File.Exists(ARQUIVO_TEMPLATE_PADRAO))
                {
                    string nomeTemplate = File.ReadAllText(ARQUIVO_TEMPLATE_PADRAO).Trim();

                    // Verifica se o template ainda existe
                    if (!string.IsNullOrEmpty(nomeTemplate))
                    {
                        var templates = TemplateManager.ListarTemplates();
                        if (templates.Contains(nomeTemplate))
                        {
                            return nomeTemplate;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao obter template padrão: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Remove a marcação de template padrão
        /// </summary>
        public static bool RemoverTemplatePadrao()
        {
            try
            {
                if (File.Exists(ARQUIVO_TEMPLATE_PADRAO))
                {
                    File.Delete(ARQUIVO_TEMPLATE_PADRAO);
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao remover template padrão: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// Verifica se um template é o padrão
        /// </summary>
        public static bool EhTemplatePadrao(string nomeTemplate)
        {
            if (string.IsNullOrWhiteSpace(nomeTemplate))
                return false;

            string templatePadrao = ObterTemplatePadrao();
            return !string.IsNullOrEmpty(templatePadrao) &&
                   templatePadrao.Equals(nomeTemplate, StringComparison.OrdinalIgnoreCase);
        }
    }
}