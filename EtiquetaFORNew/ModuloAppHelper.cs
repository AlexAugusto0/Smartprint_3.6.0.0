using EtiquetaFORNew.Data;
using System;
using System.Globalization;
using System.Text;

namespace EtiquetaFORNew
{
    public static class ModuloAppHelper
    {
        public const string ModuloPadrao = "Padrao";
        public const string ModuloConfeccao = "Confeccao";
        public const string ModuloDistribuidora = "Distribuidora";

        public static bool EstaEmModuloDistribuidoraWeb()
        {
            var config = DatabaseConfig.LoadConfiguration();
            return EhModuloDistribuidora(config?.ModuloAppWeb);
        }

        public static bool EhModuloDistribuidora(string modulo)
        {
            return string.Equals(Normalizar(modulo), "DISTRIBUIDORA", StringComparison.OrdinalIgnoreCase);
        }

        public static bool EhModuloConfeccao(string modulo)
        {
            return string.Equals(Normalizar(modulo), "CONFECCAO", StringComparison.OrdinalIgnoreCase);
        }

        public static string Normalizar(string modulo)
        {
            if (string.IsNullOrWhiteSpace(modulo))
                return string.Empty;

            string texto = modulo.Trim().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(texto.Length);

            foreach (char c in texto)
            {
                UnicodeCategory categoria = CharUnicodeInfo.GetUnicodeCategory(c);
                if (categoria != UnicodeCategory.NonSpacingMark)
                    sb.Append(char.ToUpperInvariant(c));
            }

            return sb.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
