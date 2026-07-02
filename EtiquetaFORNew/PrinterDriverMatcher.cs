using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace EtiquetaFORNew
{
    public class PrinterDeviceInfo
    {
        public string Nome { get; set; }
        public string Fabricante { get; set; }
        public string DeviceId { get; set; }
        public string Caption { get; set; }
        public string Description { get; set; }
        public string Service { get; set; }
        public string StatusDriver { get; set; }
        public List<string> HardwareIds { get; set; }
        public List<string> CompatibleIds { get; set; }
        public Dictionary<string, string> RegistryValues { get; set; }

        public PrinterDeviceInfo()
        {
            HardwareIds = new List<string>();
            CompatibleIds = new List<string>();
            RegistryValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public static PrinterDeviceInfo FromManagementObject(ManagementObject device, string fabricante)
        {
            var info = new PrinterDeviceInfo
            {
                Nome = ReadString(device, "Name"),
                Fabricante = fabricante,
                DeviceId = ReadString(device, "DeviceID"),
                Caption = ReadString(device, "Caption"),
                Description = ReadString(device, "Description"),
                Service = ReadString(device, "Service"),
                HardwareIds = ReadStringList(device, "HardwareID"),
                CompatibleIds = ReadStringList(device, "CompatibleID")
            };

            foreach (var compatibleId in ReadStringList(device, "CompatibleIDs"))
            {
                if (!info.CompatibleIds.Any(i => i.Equals(compatibleId, StringComparison.OrdinalIgnoreCase)))
                    info.CompatibleIds.Add(compatibleId);
            }

            EnriquecerComRegistro(info);
            return info;
        }

        public IEnumerable<string> GetEvidenceValues()
        {
            var values = new List<string>
            {
                Nome,
                Fabricante,
                DeviceId,
                Caption,
                Description,
                Service
            };

            values.AddRange(HardwareIds);
            values.AddRange(CompatibleIds);
            values.AddRange(RegistryValues.Values);

            return values
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase);
        }

        public string HardwareIdsResumo()
        {
            return HardwareIds != null && HardwareIds.Count > 0
                ? string.Join(" | ", HardwareIds)
                : "-";
        }

        public string CompatibleIdsResumo()
        {
            return CompatibleIds != null && CompatibleIds.Count > 0
                ? string.Join(" | ", CompatibleIds)
                : "-";
        }

        private static string ReadString(ManagementBaseObject obj, string propertyName)
        {
            try
            {
                return obj[propertyName]?.ToString() ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static List<string> ReadStringList(ManagementBaseObject obj, string propertyName)
        {
            var result = new List<string>();

            try
            {
                object value = obj[propertyName];
                if (value == null)
                    return result;

                if (value is string text)
                {
                    if (!string.IsNullOrWhiteSpace(text))
                        result.Add(text);

                    return result;
                }

                if (value is string[] array)
                {
                    result.AddRange(array.Where(v => !string.IsNullOrWhiteSpace(v)));
                    return result;
                }

                if (value is Array genericArray)
                {
                    foreach (object item in genericArray)
                    {
                        string textItem = item?.ToString();
                        if (!string.IsNullOrWhiteSpace(textItem))
                            result.Add(textItem);
                    }
                }
            }
            catch
            {
                // Algumas propriedades WMI podem nao existir dependendo do driver.
            }

            return result.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static void EnriquecerComRegistro(PrinterDeviceInfo info)
        {
            if (string.IsNullOrWhiteSpace(info.DeviceId))
                return;

            try
            {
                string keyPath = @"SYSTEM\CurrentControlSet\Enum\" + info.DeviceId;
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(keyPath))
                {
                    if (key == null)
                        return;

                    ReadRegistryValue(key, "FriendlyName", info.RegistryValues);
                    ReadRegistryValue(key, "DeviceDesc", info.RegistryValues);
                    ReadRegistryValue(key, "Mfg", info.RegistryValues);
                    ReadRegistryValue(key, "Service", info.RegistryValues);
                    ReadRegistryValue(key, "HardwareID", info.RegistryValues);
                    ReadRegistryValue(key, "CompatibleIDs", info.RegistryValues);
                }
            }
            catch (Exception ex)
            {
                PrinterDetectionLogger.Log("Falha ao ler registro do dispositivo " + info.DeviceId + ": " + ex.Message);
            }
        }

        private static void ReadRegistryValue(RegistryKey key, string valueName, Dictionary<string, string> target)
        {
            object value = key.GetValue(valueName);
            if (value == null)
                return;

            if (value is string[] array)
            {
                target[valueName] = string.Join(" | ", array.Where(v => !string.IsNullOrWhiteSpace(v)));
                return;
            }

            string text = value.ToString();
            if (!string.IsNullOrWhiteSpace(text))
                target[valueName] = text;
        }
    }

    public class PrinterMatchCandidate
    {
        public ImpressoraInfo Impressora { get; set; }
        public int Pontuacao { get; set; }
        public string Motivo { get; set; }
        public string TextoCorrespondente { get; set; }
    }

    public class PrinterMatchResult
    {
        public ImpressoraInfo Impressora { get; set; }
        public int Pontuacao { get; set; }
        public string Motivo { get; set; }
        public bool Confiavel { get; set; }
        public bool Ambiguo { get; set; }
        public bool UsouFallback { get; set; }
        public List<PrinterMatchCandidate> Candidatos { get; set; }

        public PrinterMatchResult()
        {
            Candidatos = new List<PrinterMatchCandidate>();
        }
    }

    public static class PrinterDriverMatcher
    {
        private const int PontuacaoMinimaConfiavel = 260;
        private const int DistanciaMinimaEntreCandidatos = 35;

        private static readonly HashSet<string> StopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "USB", "USBPRINT", "PRINT", "PRINTER", "IMPRESSORA", "IMPRESSORAS",
            "SUPPORT", "SUPORTE", "DEVICE", "DISPOSITIVO", "SERIES", "SERIE",
            "CLASS", "PORT", "PORTA", "WINDOWS", "DRIVER"
        };

        private static readonly HashSet<string> VariantTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "PRO", "PLUS", "FULL", "DT", "D", "T", "ETHERNET", "WIFI",
            "WIRELESS", "II", "III", "IV", "V"
        };

        public static PrinterMatchResult IdentificarMelhorDriver(PrinterDeviceInfo dispositivo, IEnumerable<ImpressoraInfo> impressoras)
        {
            var result = new PrinterMatchResult();
            var lista = impressoras?.Where(i => i != null).ToList() ?? new List<ImpressoraInfo>();

            if (dispositivo == null || lista.Count == 0)
            {
                result.Motivo = "Sem dados suficientes para comparar.";
                return result;
            }

            foreach (var impressora in lista)
            {
                var candidato = AvaliarCandidato(dispositivo, impressora);
                result.Candidatos.Add(candidato);
            }

            result.Candidatos = result.Candidatos
                .OrderByDescending(c => c.Pontuacao)
                .ThenByDescending(c => Tokenize(c.Impressora?.Nome).Count)
                .ThenBy(c => c.Impressora?.Nome)
                .ToList();

            var melhor = result.Candidatos.FirstOrDefault();
            var segundo = result.Candidatos.Skip(1).FirstOrDefault();

            if (melhor == null)
            {
                result.Motivo = "Nenhum candidato avaliado.";
                return result;
            }

            int distanciaSegundo = segundo == null ? melhor.Pontuacao : melhor.Pontuacao - segundo.Pontuacao;
            result.Ambiguo = segundo != null &&
                              melhor.Pontuacao >= PontuacaoMinimaConfiavel &&
                              distanciaSegundo < DistanciaMinimaEntreCandidatos;

            result.Impressora = melhor.Impressora;
            result.Pontuacao = melhor.Pontuacao;
            result.Motivo = melhor.Motivo;
            result.Confiavel = melhor.Pontuacao >= PontuacaoMinimaConfiavel && !result.Ambiguo;

            if (!result.Confiavel)
            {
                result.Impressora = null;
                if (result.Ambiguo && segundo != null)
                {
                    result.Motivo = "Resultado ambiguo: " + melhor.Impressora.Nome +
                                    " (" + melhor.Pontuacao + ") ficou proximo de " +
                                    segundo.Impressora.Nome + " (" + segundo.Pontuacao + ").";
                }
                else
                {
                    result.Motivo = "Pontuacao insuficiente para selecao automatica. Melhor candidato: " +
                                    melhor.Impressora.Nome + " (" + melhor.Pontuacao + ").";
                }
            }

            return result;
        }

        public static bool ContemNomeNormalizado(string texto, string modelo)
        {
            var textoNormalizado = Normalize(texto);
            var modeloNormalizado = Normalize(modelo);

            if (string.IsNullOrWhiteSpace(textoNormalizado.Compact) ||
                string.IsNullOrWhiteSpace(modeloNormalizado.Compact))
                return false;

            return textoNormalizado.Normalized.Contains(modeloNormalizado.Normalized) ||
                   textoNormalizado.Compact.Contains(modeloNormalizado.Compact) ||
                   modeloNormalizado.Compact.Contains(textoNormalizado.Compact);
        }

        public static string NormalizarParaLog(string text)
        {
            return Normalize(text).Normalized;
        }

        private static PrinterMatchCandidate AvaliarCandidato(PrinterDeviceInfo dispositivo, ImpressoraInfo impressora)
        {
            var signatures = ObterAssinaturas(impressora);
            var best = new PrinterMatchCandidate
            {
                Impressora = impressora,
                Pontuacao = int.MinValue,
                Motivo = "Sem assinatura comparavel."
            };

            foreach (string signature in signatures)
            {
                var candidate = AvaliarAssinatura(dispositivo, impressora, signature);
                if (candidate.Pontuacao > best.Pontuacao)
                    best = candidate;
            }

            best.Impressora = impressora;
            return best;
        }

        private static PrinterMatchCandidate AvaliarAssinatura(PrinterDeviceInfo dispositivo, ImpressoraInfo impressora, string signature)
        {
            var assinatura = Normalize(signature);
            var evidence = dispositivo.GetEvidenceValues()
                .Select(v => new EvidenceText { Original = v, Normalized = Normalize(v) })
                .ToList();

            var todosTokensDispositivo = evidence
                .SelectMany(e => e.Normalized.Tokens)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            int score = 0;
            var reasons = new List<string>();
            string matchedText = string.Empty;

            AplicarPontuacaoPorIds(dispositivo, impressora, assinatura, evidence, ref score, reasons, ref matchedText);
            AplicarPontuacaoPorTexto(assinatura, evidence, todosTokensDispositivo, ref score, reasons, ref matchedText);
            AplicarPontuacaoPorFabricante(dispositivo, impressora, evidence, ref score, reasons);
            AplicarPontuacaoPorVariantes(assinatura, todosTokensDispositivo, ref score, reasons);

            score += assinatura.Tokens.Count * 7;

            if (score < 0)
                score = 0;

            return new PrinterMatchCandidate
            {
                Impressora = impressora,
                Pontuacao = score,
                Motivo = reasons.Count > 0 ? string.Join("; ", reasons) : "Sem correspondencia forte.",
                TextoCorrespondente = matchedText
            };
        }

        private static void AplicarPontuacaoPorIds(
            PrinterDeviceInfo dispositivo,
            ImpressoraInfo impressora,
            NormalizedText assinatura,
            List<EvidenceText> evidence,
            ref int score,
            List<string> reasons,
            ref string matchedText)
        {
            if (impressora.HardwareIds != null && dispositivo.HardwareIds != null)
            {
                foreach (string configuredHardwareId in impressora.HardwareIds.Where(v => !string.IsNullOrWhiteSpace(v)))
                {
                    if (dispositivo.HardwareIds.Any(id => id.Equals(configuredHardwareId, StringComparison.OrdinalIgnoreCase)))
                    {
                        score += 1000;
                        reasons.Add("Hardware ID exato");
                        matchedText = configuredHardwareId;
                    }
                }
            }

            if (impressora.DeviceIds != null && !string.IsNullOrWhiteSpace(dispositivo.DeviceId))
            {
                foreach (string configuredDeviceId in impressora.DeviceIds.Where(v => !string.IsNullOrWhiteSpace(v)))
                {
                    if (dispositivo.DeviceId.Equals(configuredDeviceId, StringComparison.OrdinalIgnoreCase))
                    {
                        score += 900;
                        reasons.Add("Device ID exato");
                        matchedText = configuredDeviceId;
                    }
                }
            }

            foreach (var item in evidence)
            {
                bool hardwareEvidence = dispositivo.HardwareIds.Any(id => id.Equals(item.Original, StringComparison.OrdinalIgnoreCase));
                bool deviceEvidence = !string.IsNullOrWhiteSpace(dispositivo.DeviceId) &&
                                      dispositivo.DeviceId.Equals(item.Original, StringComparison.OrdinalIgnoreCase);

                if (hardwareEvidence && item.Normalized.Compact.Contains(assinatura.Compact))
                {
                    score += 280;
                    reasons.Add("Modelo encontrado no Hardware ID");
                    matchedText = item.Original;
                }
                else if (deviceEvidence && item.Normalized.Compact.Contains(assinatura.Compact))
                {
                    score += 260;
                    reasons.Add("Modelo encontrado no Device ID");
                    matchedText = item.Original;
                }
            }
        }

        private static void AplicarPontuacaoPorTexto(
            NormalizedText assinatura,
            List<EvidenceText> evidence,
            List<string> todosTokensDispositivo,
            ref int score,
            List<string> reasons,
            ref string matchedText)
        {
            foreach (var item in evidence)
            {
                if (item.Normalized.Normalized == assinatura.Normalized)
                {
                    score += 360;
                    reasons.Add("Nome normalizado exato");
                    matchedText = item.Original;
                }

                if (item.Normalized.Compact == assinatura.Compact)
                {
                    score += 330;
                    reasons.Add("Nome compacto exato");
                    matchedText = item.Original;
                }

                if (!string.IsNullOrWhiteSpace(assinatura.Normalized) &&
                    item.Normalized.Normalized.Contains(assinatura.Normalized))
                {
                    score += 210 + (assinatura.Tokens.Count * 12);
                    reasons.Add("Nome completo contido na descricao");
                    matchedText = item.Original;
                }

                if (!string.IsNullOrWhiteSpace(assinatura.Compact) &&
                    item.Normalized.Compact.Contains(assinatura.Compact))
                {
                    score += 200 + (assinatura.Tokens.Count * 10);
                    reasons.Add("Nome compacto contido na descricao");
                    matchedText = item.Original;
                }

                if (TokensEmSequencia(assinatura.Tokens, item.Normalized.Tokens))
                {
                    score += 110 + (assinatura.Tokens.Count * 15);
                    reasons.Add("Tokens do modelo em sequencia");
                    matchedText = item.Original;
                }
            }

            int matchedTokens = assinatura.Tokens.Count(t =>
                todosTokensDispositivo.Any(dt => dt.Equals(t, StringComparison.OrdinalIgnoreCase)));

            if (assinatura.Tokens.Count > 0 && matchedTokens > 0)
            {
                double ratio = matchedTokens / (double)assinatura.Tokens.Count;
                score += (int)Math.Round(170 * ratio) + (matchedTokens * 15);
                reasons.Add(matchedTokens + "/" + assinatura.Tokens.Count + " tokens coincidentes");
            }

            var modelTokens = assinatura.Tokens.Where(t => t.Any(char.IsDigit)).ToList();
            if (modelTokens.Count > 0)
            {
                int modelMatches = modelTokens.Count(t =>
                    todosTokensDispositivo.Any(dt => dt.Equals(t, StringComparison.OrdinalIgnoreCase)));

                if (modelMatches > 0)
                {
                    score += modelMatches * 90;
                    reasons.Add("Codigo de modelo numerico coincidente");
                }
                else
                {
                    score -= 120;
                    reasons.Add("Codigo de modelo numerico divergente");
                }
            }
        }

        private static void AplicarPontuacaoPorFabricante(
            PrinterDeviceInfo dispositivo,
            ImpressoraInfo impressora,
            List<EvidenceText> evidence,
            ref int score,
            List<string> reasons)
        {
            string fabricanteCatalogo = ObterFabricanteCatalogo(impressora);
            string fabricanteDispositivo = dispositivo.Fabricante ?? string.Empty;

            if (string.IsNullOrWhiteSpace(fabricanteCatalogo))
                return;

            bool fabricanteEncontradoNoTexto = evidence.Any(e =>
                e.Normalized.Tokens.Any(t => t.Equals(fabricanteCatalogo, StringComparison.OrdinalIgnoreCase)));

            if (!string.IsNullOrWhiteSpace(fabricanteDispositivo) &&
                !fabricanteDispositivo.Equals("Desconhecido", StringComparison.OrdinalIgnoreCase) &&
                Normalize(fabricanteDispositivo).Tokens.Contains(fabricanteCatalogo, StringComparer.OrdinalIgnoreCase))
            {
                score += 90;
                reasons.Add("Fabricante coincidente");
            }
            else if (fabricanteEncontradoNoTexto)
            {
                score += 60;
                reasons.Add("Fabricante encontrado no nome/descricao");
            }
            else if (!string.IsNullOrWhiteSpace(fabricanteDispositivo) &&
                     !fabricanteDispositivo.Equals("Desconhecido", StringComparison.OrdinalIgnoreCase))
            {
                score -= 80;
                reasons.Add("Fabricante diferente do catalogo");
            }
        }

        private static void AplicarPontuacaoPorVariantes(
            NormalizedText assinatura,
            List<string> todosTokensDispositivo,
            ref int score,
            List<string> reasons)
        {
            var variantesDispositivo = todosTokensDispositivo
                .Where(t => VariantTokens.Contains(t))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var variantesAssinatura = assinatura.Tokens
                .Where(t => VariantTokens.Contains(t))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (string variante in variantesDispositivo)
            {
                if (!variantesAssinatura.Contains(variante, StringComparer.OrdinalIgnoreCase))
                {
                    score -= 180;
                    reasons.Add("Variante detectada ausente no candidato: " + variante);
                }
            }

            foreach (string variante in variantesAssinatura)
            {
                if (!variantesDispositivo.Contains(variante, StringComparer.OrdinalIgnoreCase))
                {
                    score -= 95;
                    reasons.Add("Variante do candidato nao detectada: " + variante);
                }
                else
                {
                    score += 80;
                    reasons.Add("Variante coincidente: " + variante);
                }
            }
        }

        private static List<string> ObterAssinaturas(ImpressoraInfo impressora)
        {
            var signatures = new List<string>();

            if (!string.IsNullOrWhiteSpace(impressora.Nome))
                signatures.Add(impressora.Nome);

            if (impressora.Aliases != null)
                signatures.AddRange(impressora.Aliases.Where(a => !string.IsNullOrWhiteSpace(a)));

            return signatures.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static string ObterFabricanteCatalogo(ImpressoraInfo impressora)
        {
            string fabricante = impressora.Fabricante;

            if (string.IsNullOrWhiteSpace(fabricante))
                fabricante = Tokenize(impressora.Nome).FirstOrDefault();

            return string.IsNullOrWhiteSpace(fabricante)
                ? string.Empty
                : Normalize(fabricante).Tokens.FirstOrDefault() ?? string.Empty;
        }

        private static bool TokensEmSequencia(List<string> modeloTokens, List<string> textoTokens)
        {
            if (modeloTokens.Count == 0 || textoTokens.Count == 0 || modeloTokens.Count > textoTokens.Count)
                return false;

            for (int i = 0; i <= textoTokens.Count - modeloTokens.Count; i++)
            {
                bool match = true;
                for (int j = 0; j < modeloTokens.Count; j++)
                {
                    if (!textoTokens[i + j].Equals(modeloTokens[j], StringComparison.OrdinalIgnoreCase))
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                    return true;
            }

            return false;
        }

        private static NormalizedText Normalize(string text)
        {
            var tokens = Tokenize(text);
            return new NormalizedText
            {
                Original = text ?? string.Empty,
                Tokens = tokens,
                Normalized = string.Join(" ", tokens),
                Compact = string.Concat(tokens)
            };
        }

        private static List<string> Tokenize(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new List<string>();

            string normalized = RemoveDiacritics(text).ToUpperInvariant();
            normalized = normalized.Replace("WI-FI", "WIFI").Replace("WI FI", "WIFI");
            normalized = normalized.Replace("USBPRINT", " ");
            normalized = Regex.Replace(normalized, @"[^A-Z0-9]+", " ");

            var tokens = new List<string>();
            foreach (Match match in Regex.Matches(normalized, @"[A-Z0-9]+"))
            {
                string token = match.Value;
                if (string.IsNullOrWhiteSpace(token) || StopWords.Contains(token))
                    continue;

                var expanded = ExpandModelToken(token);
                foreach (string expandedToken in expanded)
                {
                    if (!StopWords.Contains(expandedToken))
                        tokens.Add(expandedToken);
                }
            }

            return tokens;
        }

        private static IEnumerable<string> ExpandModelToken(string token)
        {
            Match suffixMatch = Regex.Match(token, @"^([A-Z]+\d+)([A-Z]{2,})$");
            if (suffixMatch.Success)
            {
                yield return suffixMatch.Groups[1].Value;
                yield return suffixMatch.Groups[2].Value;
                yield break;
            }

            yield return token;
        }

        private static string RemoveDiacritics(string text)
        {
            string formD = text.Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(formD.Length);

            foreach (char ch in formD)
            {
                UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (category != UnicodeCategory.NonSpacingMark)
                    builder.Append(ch);
            }

            return builder.ToString().Normalize(NormalizationForm.FormC);
        }

        private class NormalizedText
        {
            public string Original { get; set; }
            public List<string> Tokens { get; set; }
            public string Normalized { get; set; }
            public string Compact { get; set; }
        }

        private class EvidenceText
        {
            public string Original { get; set; }
            public NormalizedText Normalized { get; set; }
        }
    }

    public static class PrinterDetectionLogger
    {
        private static readonly object SyncRoot = new object();

        public static string LogPath
        {
            get
            {
                string folder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "SmartPrint",
                    "Logs");

                return Path.Combine(folder, "printer-detection.log");
            }
        }

        public static void LogDeviceFound(PrinterDeviceInfo device)
        {
            if (device == null)
                return;

            Log("Impressora encontrada | Nome=" + Safe(device.Nome) +
                " | Fabricante=" + Safe(device.Fabricante) +
                " | StatusDriver=" + Safe(device.StatusDriver) +
                " | DeviceID=" + Safe(device.DeviceId) +
                " | HardwareID=" + Safe(device.HardwareIdsResumo()) +
                " | CompatibleID=" + Safe(device.CompatibleIdsResumo()));
        }

        public static void LogMatchResult(PrinterDeviceInfo device, PrinterMatchResult result)
        {
            if (result == null)
                return;

            var builder = new StringBuilder();
            builder.AppendLine("Resultado de identificacao automatica");
            if (device != null)
            {
                builder.AppendLine("  Dispositivo: " + Safe(device.Nome));
                builder.AppendLine("  Fabricante: " + Safe(device.Fabricante));
                builder.AppendLine("  DeviceID: " + Safe(device.DeviceId));
                builder.AppendLine("  HardwareID: " + Safe(device.HardwareIdsResumo()));
            }

            builder.AppendLine("  Selecionado: " + (result.Impressora != null ? result.Impressora.Nome : "-"));
            builder.AppendLine("  Confiavel: " + result.Confiavel);
            builder.AppendLine("  Ambiguo: " + result.Ambiguo);
            builder.AppendLine("  Fallback: " + result.UsouFallback);
            builder.AppendLine("  Pontuacao: " + result.Pontuacao);
            builder.AppendLine("  Motivo: " + Safe(result.Motivo));
            builder.AppendLine("  Candidatos:");

            foreach (var candidato in result.Candidatos.OrderByDescending(c => c.Pontuacao))
            {
                builder.AppendLine("    - " + Safe(candidato.Impressora?.Nome) +
                                   " | Score=" + candidato.Pontuacao +
                                   " | Motivo=" + Safe(candidato.Motivo) +
                                   " | Evidencia=" + Safe(candidato.TextoCorrespondente));
            }

            Log(builder.ToString().TrimEnd());
        }

        public static void Log(string message)
        {
            try
            {
                lock (SyncRoot)
                {
                    string path = LogPath;
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    File.AppendAllText(path,
                        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture) +
                        " | " + message + Environment.NewLine,
                        Encoding.UTF8);
                }

                Debug.WriteLine(message);
            }
            catch
            {
                // Logging nunca deve impedir deteccao ou instalacao.
            }
        }

        private static string Safe(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "-" : value.Replace(Environment.NewLine, " ");
        }
    }
}
