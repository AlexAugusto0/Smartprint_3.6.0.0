using System.Globalization;

namespace EtiquetaFORNew
{
    public static class FormatadorMonetario
    {
        private static readonly CultureInfo CulturaBrasil = CultureInfo.GetCultureInfo("pt-BR");
        private static readonly CultureInfo CulturaInvariante = CultureInfo.InvariantCulture;

        public static string Formatar(decimal valor)
        {
            return valor.ToString("N2", CulturaBrasil);
        }

        public static string Formatar(decimal? valor, string valorVazio = "")
        {
            return valor.HasValue ? Formatar(valor.Value) : valorVazio;
        }

        public static bool TryConverter(string texto, out decimal valor)
        {
            valor = 0m;

            if (string.IsNullOrWhiteSpace(texto))
                return false;

            texto = texto.Trim();
            var estilos = NumberStyles.Number | NumberStyles.AllowCurrencySymbol;

            if (decimal.TryParse(texto, estilos, CulturaBrasil, out valor))
                return true;

            return decimal.TryParse(texto, estilos, CulturaInvariante, out valor);
        }
    }
}
