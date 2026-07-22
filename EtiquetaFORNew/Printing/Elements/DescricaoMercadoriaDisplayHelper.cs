using System;
using System.Linq;
using System.Text.RegularExpressions;
using EtiquetaFORNew.Data;

namespace EtiquetaFORNew
{
    internal static class DescricaoMercadoriaDisplayHelper
    {
        public static string ObterDescricaoVisivel(Produto produto)
        {
            if (produto == null)
                return "";

            return ObterDescricaoVisivel(produto.Nome, produto.Tam, produto.Cores, produto.CodBarras_Grade);
        }

        public static string ObterDescricaoVisivel(string descricao, string tam, string cores)
        {
            return ObterDescricaoVisivel(descricao, tam, cores, null);
        }

        private static string ObterDescricaoVisivel(string descricao, string tam, string cores, string codBarrasGrade)
        {
            if (!EstaEmModoSoftcomShopConfeccao())
                return descricao ?? "";

            return ObterDescricaoComGradeInvisivel(descricao, tam, cores, codBarrasGrade);
        }

        public static bool CampoDescricaoMercadoria(string campo)
        {
            return string.Equals(campo, "Nome", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(campo, "Mercadoria", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(campo, "Descricao", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(campo, "Descrição", StringComparison.OrdinalIgnoreCase);
        }

        private static bool EstaEmModoSoftcomShopConfeccao()
        {
            try
            {
                var configSistema = ConfiguracaoSistema.Carregar();
                if (configSistema == null || configSistema.TipoConexaoAtiva != TipoConexao.SoftcomShop)
                    return false;

                var configDb = DatabaseConfig.LoadConfiguration();
                return EhModuloConfeccao(configDb?.ModuloAppWeb) ||
                       EhModuloConfeccao(configDb?.ModuloApp);
            }
            catch
            {
                return false;
            }
        }

        private static bool EhModuloConfeccao(string modulo)
        {
            if (string.IsNullOrWhiteSpace(modulo))
                return false;

            string texto = modulo.Trim();
            return texto.Equals("CONFECCAO", StringComparison.OrdinalIgnoreCase) ||
                   texto.Equals("CONFECÇÃO", StringComparison.OrdinalIgnoreCase);
        }

        private static string ObterDescricaoComGradeInvisivel(string descricao, string tam, string cores, string codBarrasGrade)
        {
            if (string.IsNullOrWhiteSpace(descricao))
                return descricao ?? "";

            string original = descricao.Trim();
            string valorTam = NormalizarValorGrade(tam);
            string valorCor = NormalizarValorGrade(cores);
            bool itemComGrade = TemGradeInformada(codBarrasGrade) ||
                                !string.IsNullOrWhiteSpace(valorTam) ||
                                !string.IsNullOrWhiteSpace(valorCor);

            if (!itemComGrade)
                return original;

            string visivel = original;

            if (!string.IsNullOrWhiteSpace(valorTam) && !string.IsNullOrWhiteSpace(valorCor))
            {
                visivel = OcultarSequenciaGradeFinal(visivel, "TAM", valorTam, "COR", valorCor);
                visivel = OcultarSequenciaGradeFinal(visivel, "COR", valorCor, "TAM", valorTam);
            }

            string anterior;
            do
            {
                anterior = visivel;

                if (!string.IsNullOrWhiteSpace(valorCor))
                    visivel = OcultarAtributoGradeFinal(visivel, "COR", valorCor);

                if (!string.IsNullOrWhiteSpace(valorTam))
                    visivel = OcultarAtributoGradeFinal(visivel, "TAM", valorTam);

            } while (!string.Equals(anterior, visivel, StringComparison.Ordinal));

            bool removeuPorValor = !string.Equals(visivel, original, StringComparison.Ordinal);
            if (!removeuPorValor && itemComGrade)
            {
                string visivelPorRotulo = OcultarGradeRotuladaFinal(original);
                if (!string.Equals(visivelPorRotulo, original, StringComparison.Ordinal))
                    visivel = visivelPorRotulo;
            }

            visivel = NormalizarDescricaoVisivel(visivel);
            return string.IsNullOrWhiteSpace(visivel) ? original : visivel;
        }

        private static string NormalizarValorGrade(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return "";

            string texto = valor.Trim();
            if (texto.Equals("PADRAO", StringComparison.OrdinalIgnoreCase) ||
                texto.Equals("PADRÃO", StringComparison.OrdinalIgnoreCase))
            {
                return "";
            }

            return texto;
        }

        private static bool TemGradeInformada(string codBarrasGrade)
        {
            return !string.IsNullOrWhiteSpace(codBarrasGrade);
        }

        private static string OcultarSequenciaGradeFinal(
            string descricao,
            string primeiroRotulo,
            string primeiroValor,
            string segundoRotulo,
            string segundoValor)
        {
            string padrao = SeparadorGradeObrigatorio() +
                            AberturaGradeOpcional() +
                            PrefixoGradeOpcional() +
                            RotuloGradeOpcional(primeiroRotulo) +
                            CriarPadraoValorGrade(primeiroValor) +
                            SeparadorGradeObrigatorio() +
                            RotuloGradeOpcional(segundoRotulo) +
                            CriarPadraoValorGrade(segundoValor) +
                            FechamentoGradeOpcional() +
                            @"\s*$";

            return Regex.Replace(descricao, padrao, "", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant).Trim();
        }

        private static string OcultarAtributoGradeFinal(string descricao, string rotulo, string valor)
        {
            string padrao = SeparadorGradeObrigatorio() +
                            AberturaGradeOpcional() +
                            PrefixoGradeOpcional() +
                            RotuloGradeObrigatorio(rotulo) +
                            CriarPadraoValorGrade(valor) +
                            FechamentoGradeOpcional() +
                            @"\s*$";

            return Regex.Replace(descricao, padrao, "", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant).Trim();
        }

        private static string OcultarGradeRotuladaFinal(string descricao)
        {
            string visivel = descricao;

            string anterior;
            do
            {
                anterior = visivel;
                visivel = OcultarSequenciaGradeRotuladaFinal(visivel, "TAM", "COR");
                visivel = OcultarSequenciaGradeRotuladaFinal(visivel, "COR", "TAM");
                visivel = OcultarAtributoGradeRotuladoFinal(visivel, "COR");
                visivel = OcultarAtributoGradeRotuladoFinal(visivel, "TAM");

            } while (!string.Equals(anterior, visivel, StringComparison.Ordinal));

            return visivel.Trim();
        }

        private static string OcultarSequenciaGradeRotuladaFinal(string descricao, string primeiroRotulo, string segundoRotulo)
        {
            string padrao = SeparadorGradeObrigatorio() +
                            AberturaGradeOpcional() +
                            PrefixoGradeOpcional() +
                            RotuloGradeObrigatorio(primeiroRotulo) +
                            CriarPadraoValorGradeRotulado() +
                            SeparadorGradeObrigatorio() +
                            RotuloGradeObrigatorio(segundoRotulo) +
                            CriarPadraoValorGradeRotulado() +
                            FechamentoGradeOpcional() +
                            @"\s*$";

            return Regex.Replace(descricao, padrao, "", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant).Trim();
        }

        private static string OcultarAtributoGradeRotuladoFinal(string descricao, string rotulo)
        {
            string padrao = SeparadorGradeObrigatorio() +
                            AberturaGradeOpcional() +
                            PrefixoGradeOpcional() +
                            RotuloGradeObrigatorio(rotulo) +
                            CriarPadraoValorGradeRotulado() +
                            FechamentoGradeOpcional() +
                            @"\s*$";

            return Regex.Replace(descricao, padrao, "", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant).Trim();
        }

        private static string CriarPadraoValorGrade(string valor)
        {
            var partes = Regex.Split(valor.Trim(), @"[\s/\-]+")
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(Regex.Escape);

            return string.Join(@"[\s/\-]+", partes);
        }

        private static string CriarPadraoValorGradeRotulado()
        {
            return @".+?";
        }

        private static string SeparadorGradeObrigatorio()
        {
            return @"(?:\s*(?:[-/,;|]\s*)|\s+)";
        }

        private static string AberturaGradeOpcional()
        {
            return @"(?:[\(\[\{]\s*)?";
        }

        private static string FechamentoGradeOpcional()
        {
            return @"(?:\s*[\)\]\}])?";
        }

        private static string PrefixoGradeOpcional()
        {
            return @"(?:(?:GRADE)\b\.?\s*[:=-]?\s*)?";
        }

        private static string RotuloGradeOpcional(string rotulo)
        {
            return RotuloGrade(rotulo, true);
        }

        private static string RotuloGradeObrigatorio(string rotulo)
        {
            return RotuloGrade(rotulo, false);
        }

        private static string RotuloGrade(string rotulo, bool opcional)
        {
            string padraoRotulo = string.Equals(rotulo, "TAM", StringComparison.OrdinalIgnoreCase)
                ? @"\b(?:TAM|TAMANHO)\b\.?\s*[:=-]?\s*"
                : @"\bCOR\b\.?\s*[:=-]?\s*";

            return opcional
                ? "(?:" + padraoRotulo + ")?"
                : padraoRotulo;
        }

        private static string NormalizarDescricaoVisivel(string descricao)
        {
            if (string.IsNullOrWhiteSpace(descricao))
                return "";

            string texto = Regex.Replace(descricao.Trim(), @"\s+", " ");
            texto = Regex.Replace(texto, @"[\(\[\{]\s*$", "");
            texto = Regex.Replace(texto, @"(?:\s*[-/,;|:]\s*)+$", "");
            texto = Regex.Replace(texto, @"\s+", " ").Trim();

            return texto.Trim(' ', '-', '/', ',', ';', '|', ':');
        }
    }
}
