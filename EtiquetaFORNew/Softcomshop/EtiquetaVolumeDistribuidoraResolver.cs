using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace EtiquetaFORNew
{
    public static class EtiquetaVolumeDistribuidoraResolver
    {
        public static readonly string[] CamposNotaFiscal =
        {
            "Numero Documento",
            "Numero NFe",
            "Data Emissao",
            "Serie",
            "Modelo",
            "Valor Total",
            "Chave de Acesso",
            "Observacao"
        };

        public static readonly string[] CamposDestinatario =
        {
            "Razao Social",
            "Nome Fantasia",
            "CPF/CNPJ",
            "Telefone",
            "Email",
            "Logradouro",
            "Numero",
            "Complemento",
            "CEP",
            "Bairro",
            "Cidade",
            "UF"
        };

        public static readonly string[] CamposVolumes =
        {
            "Volume Atual",
            "Total de Volumes",
            "Codigo do Volume",
            "Peso",
            "Data Impressao",
            "Operador",
            "Empresa Emitente"
        };

        public static string ObterValorCampo(EtiquetaVolumeDistribuidora etiqueta, string campo)
        {
            if (etiqueta == null || string.IsNullOrWhiteSpace(campo))
                return string.Empty;

            DadosNotaDistribuidora nota = etiqueta.DadosNota ?? new DadosNotaDistribuidora();
            DadosDestinatarioDistribuidora destinatario = etiqueta.DadosDestinatario ?? new DadosDestinatarioDistribuidora();
            DadosEnderecoDistribuidora endereco = etiqueta.DadosEndereco ?? new DadosEnderecoDistribuidora();
            VolumeDistribuidora volume = etiqueta.Volume ?? new VolumeDistribuidora();

            switch (NormalizarCampo(campo))
            {
                case "NUMERODOCUMENTO":
                    return nota.NumeroDocumento ?? string.Empty;
                case "NUMERONFE":
                    return nota.NumeroNFe ?? string.Empty;
                case "DATAEMISSAO":
                    return nota.DataEmissao.HasValue ? nota.DataEmissao.Value.ToString("dd/MM/yyyy") : string.Empty;
                case "SERIE":
                    return nota.Serie ?? string.Empty;
                case "MODELO":
                    return nota.Modelo ?? string.Empty;
                case "VALORTOTAL":
                    return FormatadorMonetario.Formatar(nota.ValorTotal);
                case "CHAVEDEACESSO":
                case "CHAVEACESSO":
                    return nota.ChaveAcesso ?? string.Empty;
                case "OBSERVACAO":
                    return nota.Observacao ?? string.Empty;
                case "RAZAOSOCIAL":
                    return destinatario.RazaoSocial ?? string.Empty;
                case "NOMEFANTASIA":
                    return destinatario.NomeFantasia ?? string.Empty;
                case "CPFCNPJ":
                case "DOCUMENTO":
                    return destinatario.Documento ?? string.Empty;
                case "TELEFONE":
                    return destinatario.Telefone ?? string.Empty;
                case "EMAIL":
                    return destinatario.Email ?? string.Empty;
                case "LOGRADOURO":
                    return endereco.Logradouro ?? string.Empty;
                case "NUMERO":
                    return endereco.Numero ?? string.Empty;
                case "COMPLEMENTO":
                    return endereco.Complemento ?? string.Empty;
                case "CEP":
                    return endereco.Cep ?? string.Empty;
                case "BAIRRO":
                    return endereco.Bairro ?? string.Empty;
                case "CIDADE":
                    return endereco.Cidade ?? string.Empty;
                case "UF":
                    return endereco.Uf ?? string.Empty;
                case "VOLUMEATUAL":
                    return volume.VolumeAtual > 0 ? volume.VolumeAtual.ToString(CultureInfo.InvariantCulture) : string.Empty;
                case "TOTALDEVOLUMES":
                case "TOTALVOLUMES":
                    return volume.TotalVolumes > 0 ? volume.TotalVolumes.ToString(CultureInfo.InvariantCulture) : string.Empty;
                case "CODIGODOVOLUME":
                case "CODIGOVOLUME":
                    return volume.CodigoVolume ?? string.Empty;
                case "PESO":
                    return volume.Peso.HasValue ? volume.Peso.Value.ToString("N3", CultureInfo.GetCultureInfo("pt-BR")) : string.Empty;
                case "DATAIMPRESSAO":
                    return volume.DataImpressao == DateTime.MinValue ? string.Empty : volume.DataImpressao.ToString("dd/MM/yyyy HH:mm");
                case "OPERADOR":
                    return volume.Operador ?? string.Empty;
                case "EMPRESAEMITENTE":
                    return volume.EmpresaEmitente ?? string.Empty;
                default:
                    return string.Empty;
            }
        }

        public static bool TryObterValorDecimal(EtiquetaVolumeDistribuidora etiqueta, string campo, out decimal valor, out string mensagemErro)
        {
            valor = 0m;
            mensagemErro = null;

            if (etiqueta == null)
            {
                mensagemErro = "Nenhum volume disponivel.";
                return false;
            }

            string campoNormalizado = NormalizarCampo(campo);
            VolumeDistribuidora volume = etiqueta.Volume ?? new VolumeDistribuidora();

            switch (campoNormalizado)
            {
                case "VALORTOTAL":
                    if (etiqueta.DadosNota != null && etiqueta.DadosNota.ValorTotal.HasValue)
                    {
                        valor = etiqueta.DadosNota.ValorTotal.Value;
                        return true;
                    }
                    break;
                case "PESO":
                    if (volume.Peso.HasValue)
                    {
                        valor = volume.Peso.Value;
                        return true;
                    }
                    break;
                case "VOLUMEATUAL":
                    valor = volume.VolumeAtual;
                    return volume.VolumeAtual > 0;
                case "TOTALDEVOLUMES":
                case "TOTALVOLUMES":
                    valor = volume.TotalVolumes;
                    return volume.TotalVolumes > 0;
            }

            mensagemErro = $"Campo \"{campo}\" nao possui valor numerico.";
            return false;
        }

        public static IEnumerable<string> ObterCamposCodigoBarras()
        {
            yield return "Codigo do Volume";
            yield return "Chave de Acesso";
            yield return "Numero NFe";
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
