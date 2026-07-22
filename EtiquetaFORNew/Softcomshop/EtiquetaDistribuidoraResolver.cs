using System;
using System.Globalization;
using System.Text;

namespace EtiquetaFORNew
{
    public static class EtiquetaDistribuidoraResolver
    {
        public static readonly string[] CamposNotaFiscal =
        {
            "Numero NF",
            "Numero Documento",
            "Data Emissao"
        };

        public static readonly string[] CamposDestinatario =
        {
            "Razao Social",
            "Nome",
            "CPF/CNPJ"
        };

        public static readonly string[] CamposEndereco =
        {
            "Endereco",
            "Numero",
            "Complemento",
            "Bairro",
            "Cidade",
            "UF",
            "CEP"
        };

        public static readonly string[] CamposEmpresa =
        {
            "Nome Empresa",
            "Fantasia",
            "CNPJ"
        };

        public static readonly string[] CamposVolumes =
        {
            "Volume",
            "Volume Total"
        };

        public static string ObterValorCampo(EtiquetaDistribuidora etiqueta, string campo)
        {
            if (etiqueta == null || string.IsNullOrWhiteSpace(campo))
                return string.Empty;

            DadosVendaDistribuidora venda = etiqueta.Venda ?? new DadosVendaDistribuidora();
            DadosEmpresaDistribuidora empresa = etiqueta.Empresa ?? new DadosEmpresaDistribuidora();
            DadosDestinatarioEtiquetaDistribuidora destinatario = etiqueta.Destinatario ?? new DadosDestinatarioEtiquetaDistribuidora();
            DadosEnderecoEtiquetaDistribuidora endereco = etiqueta.Endereco ?? new DadosEnderecoEtiquetaDistribuidora();

            switch (NormalizarCampo(campo))
            {
                case "NUMERONF":
                    return venda.NumeroNf ?? string.Empty;
                case "NUMERODOCUMENTO":
                    return venda.NumeroDocumento ?? string.Empty;
                case "DATAEMISSAO":
                    return venda.DataEmissao.HasValue ? venda.DataEmissao.Value.ToString("dd/MM/yyyy") : string.Empty;
                case "RAZAOSOCIAL":
                    return destinatario.RazaoSocial ?? string.Empty;
                case "NOME":
                    return destinatario.Nome ?? string.Empty;
                case "CPFCNPJ":
                case "DOCUMENTO":
                    return destinatario.Documento ?? string.Empty;
                case "ENDERECO":
                    return endereco.Endereco ?? string.Empty;
                case "NUMERO":
                    return endereco.Numero ?? string.Empty;
                case "COMPLEMENTO":
                    return endereco.Complemento ?? string.Empty;
                case "BAIRRO":
                    return endereco.Bairro ?? string.Empty;
                case "CIDADE":
                    return endereco.Cidade ?? string.Empty;
                case "UF":
                    return endereco.Uf ?? string.Empty;
                case "CEP":
                    return endereco.Cep ?? string.Empty;
                case "NOMEEMPRESA":
                    return empresa.Nome ?? string.Empty;
                case "FANTASIA":
                    return empresa.Fantasia ?? string.Empty;
                case "CNPJ":
                    return empresa.Cnpj ?? string.Empty;
                case "VOLUME":
                case "VOLUMEATUAL":
                    return etiqueta.Volume > 0 ? etiqueta.Volume.ToString(CultureInfo.InvariantCulture) : string.Empty;
                case "VOLUMETOTAL":
                case "TOTALDEVOLUMES":
                case "TOTALVOLUMES":
                    return etiqueta.VolumeTotal > 0 ? etiqueta.VolumeTotal.ToString(CultureInfo.InvariantCulture) : string.Empty;
                default:
                    return string.Empty;
            }
        }

        public static bool TryObterValorDecimal(EtiquetaDistribuidora etiqueta, string campo, out decimal valor, out string mensagemErro)
        {
            valor = 0m;
            mensagemErro = null;

            if (etiqueta == null)
            {
                mensagemErro = "Nenhum documento logistico disponivel.";
                return false;
            }

            switch (NormalizarCampo(campo))
            {
                case "NUMERONF":
                    return decimal.TryParse(etiqueta.Venda?.NumeroNf, NumberStyles.Number, CultureInfo.InvariantCulture, out valor);
                case "NUMERODOCUMENTO":
                    return decimal.TryParse(etiqueta.Venda?.NumeroDocumento, NumberStyles.Number, CultureInfo.InvariantCulture, out valor);
                case "VOLUME":
                case "VOLUMEATUAL":
                    valor = etiqueta.Volume;
                    return etiqueta.Volume > 0;
                case "VOLUMETOTAL":
                case "TOTALDEVOLUMES":
                case "TOTALVOLUMES":
                    valor = etiqueta.VolumeTotal;
                    return etiqueta.VolumeTotal > 0;
                default:
                    mensagemErro = $"Campo \"{campo}\" nao possui valor numerico.";
                    return false;
            }
        }

        public static string[] ObterCamposCodigoBarras()
        {
            return new[] { "Numero NF", "Numero Documento" };
        }

        private static string NormalizarCampo(string campo)
        {
            if (string.IsNullOrWhiteSpace(campo))
                return string.Empty;

            string texto = campo.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(texto.Length);

            foreach (char c in texto)
            {
                UnicodeCategory categoria = CharUnicodeInfo.GetUnicodeCategory(c);
                if (categoria == UnicodeCategory.NonSpacingMark)
                    continue;

                if (char.IsWhiteSpace(c) || c == '_' || c == '-' || c == '.' || c == '/')
                    continue;

                sb.Append(char.ToUpperInvariant(c));
            }

            return sb.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
