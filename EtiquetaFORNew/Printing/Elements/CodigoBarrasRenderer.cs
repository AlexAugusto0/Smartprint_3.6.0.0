using BarcodeStandard;
using SkiaSharp;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using BarcodeType = BarcodeStandard.Type;

namespace EtiquetaFORNew
{
    /// <summary>
    /// Renderer unico usado pelo Designer, Preview e impressao.
    /// A simbologia selecionada no elemento e sempre a fonte de verdade.
    /// </summary>
    public static class CodigoBarrasRenderer
    {
        private const float ProporcaoMaximaFonteEan13 = 0.34F;

        public static void Renderizar(
            Graphics graphics,
            string codigo,
            RectangleF areaTotal,
            bool areaEmMilimetros,
            TipoSimbologiaCodigoBarras simbologiaSelecionada,
            bool exibirNumeracaoCodigoBarras,
            bool renderizacaoLinear1D,
            bool numeracaoAgrupada,
            Font fonte,
            Color cor)
        {
            if (graphics == null)
                throw new ArgumentNullException(nameof(graphics));

            string codigoPreparado;
            string mensagemErro;
            if (!TryPrepararCodigo(
                codigo,
                simbologiaSelecionada,
                out codigoPreparado,
                out mensagemErro))
            {
                DesenharMensagem(graphics, mensagemErro, areaTotal, fonte, Color.Red);
                return;
            }

            try
            {
                if (simbologiaSelecionada == TipoSimbologiaCodigoBarras.Ean13)
                {
                    RenderizarEan13Nativo(
                        graphics,
                        codigoPreparado,
                        areaTotal,
                        areaEmMilimetros,
                        fonte);
                    return;
                }

                RectangleF areaBarras;
                RectangleF areaNumeracao;
                if (exibirNumeracaoCodigoBarras)
                {
                    CodigoBarrasHumanReadableHelper.CalcularAreas(
                        areaTotal,
                        out areaBarras,
                        out areaNumeracao);
                }
                else
                {
                    areaBarras = areaTotal;
                    areaNumeracao = RectangleF.Empty;
                }

                CalcularDimensoesImagem(
                    graphics,
                    areaBarras,
                    areaEmMilimetros,
                    out int larguraPixels,
                    out int alturaPixels);

                BarcodeType simbologia = ObterSimbologia(simbologiaSelecionada);
                using (SKImage imagem = CodificarResiliente(
                    codigoPreparado,
                    simbologia,
                    larguraPixels,
                    alturaPixels))
                {
                    if (imagem == null)
                        throw new InvalidOperationException("A biblioteca nao gerou a imagem do codigo de barras.");

                    DesenharImagem(graphics, imagem, areaBarras);
                }

                if (exibirNumeracaoCodigoBarras)
                {
                    bool agruparNumeracao = numeracaoAgrupada || renderizacaoLinear1D;
                    CodigoBarrasHumanReadableHelper.DesenharNumeracao(
                        graphics,
                        codigoPreparado,
                        agruparNumeracao,
                        areaNumeracao,
                        fonte,
                        cor);
                }
            }
            catch
            {
                DesenharMensagem(graphics, "ERR BARCODE", areaTotal, fonte, Color.Red);
            }
        }

        public static BarcodeType ObterSimbologia(TipoSimbologiaCodigoBarras simbologia)
        {
            return simbologia == TipoSimbologiaCodigoBarras.Ean13
                ? BarcodeType.Ean13
                : BarcodeType.Code128;
        }

        public static bool TryPrepararCodigo(
            string codigo,
            TipoSimbologiaCodigoBarras simbologia,
            out string codigoPreparado,
            out string mensagemErro)
        {
            codigoPreparado = LimparCodigo(codigo);
            mensagemErro = string.Empty;

            if (string.IsNullOrEmpty(codigoPreparado))
            {
                mensagemErro = "[SEM CODIGO]";
                return false;
            }

            if (simbologia != TipoSimbologiaCodigoBarras.Ean13)
                return true;

            if (codigoPreparado.Any(c => !char.IsDigit(c)) ||
                (codigoPreparado.Length != 12 && codigoPreparado.Length != 13))
            {
                mensagemErro = "EAN-13 INVALIDO";
                return false;
            }

            string baseDozeDigitos = codigoPreparado.Substring(0, 12);
            char digitoCalculado = CalcularDigitoVerificadorEan13(baseDozeDigitos);

            if (codigoPreparado.Length == 12)
            {
                codigoPreparado += digitoCalculado;
                return true;
            }

            if (codigoPreparado[12] != digitoCalculado)
            {
                mensagemErro = "CHECKSUM EAN-13 INVALIDO";
                return false;
            }

            return true;
        }

        private static string LimparCodigo(string codigo)
        {
            return new string((codigo ?? string.Empty)
                .Where(c => !char.IsControl(c))
                .ToArray());
        }

        private static char CalcularDigitoVerificadorEan13(string baseDozeDigitos)
        {
            int soma = 0;
            for (int indice = 0; indice < baseDozeDigitos.Length; indice++)
            {
                int digito = baseDozeDigitos[indice] - '0';
                soma += indice % 2 == 0 ? digito : digito * 3;
            }

            return (char)('0' + ((10 - (soma % 10)) % 10));
        }

        private static void CalcularDimensoesImagem(
            Graphics graphics,
            RectangleF area,
            bool areaEmMilimetros,
            out int larguraPixels,
            out int alturaPixels)
        {
            if (areaEmMilimetros)
            {
                larguraPixels = (int)Math.Round((area.Width / 25.4F) * graphics.DpiX);
                alturaPixels = (int)Math.Round((area.Height / 25.4F) * graphics.DpiY);
            }
            else
            {
                larguraPixels = (int)Math.Round(area.Width);
                alturaPixels = (int)Math.Round(area.Height);
            }

            // Um bitmap nao pode ter dimensao zero. Este e apenas o limite
            // tecnico da API grafica, nao um tamanho minimo visual do elemento.
            larguraPixels = Math.Max(1, larguraPixels);
            alturaPixels = Math.Max(1, alturaPixels);
        }

        private static void RenderizarEan13Nativo(
            Graphics graphics,
            string codigo,
            RectangleF area,
            bool areaEmMilimetros,
            Font fonte)
        {
            CalcularDimensoesImagem(
                graphics,
                area,
                areaEmMilimetros,
                out int larguraPixels,
                out int alturaPixels);

            float tamanhoFontePixels = (fonte?.SizeInPoints ?? 10F) *
                graphics.DpiY / 72F;
            float tamanhoMaximoPelaAltura = alturaPixels * ProporcaoMaximaFonteEan13;
            tamanhoFontePixels = Math.Max(
                0.1F,
                Math.Min(tamanhoFontePixels, tamanhoMaximoPelaAltura));

            using (SKImage imagem = CodificarResiliente(
                codigo,
                BarcodeType.Ean13,
                larguraPixels,
                alturaPixels,
                true,
                fonte?.FontFamily?.Name,
                tamanhoFontePixels))
            {
                if (imagem == null)
                    throw new InvalidOperationException("A biblioteca nao gerou o EAN-13.");

                DesenharImagem(graphics, imagem, area);
            }
        }

        private static SKImage CodificarResiliente(
            string codigo,
            BarcodeType simbologia,
            int largura,
            int altura,
            bool incluirRotulo = false,
            string familiaFonte = null,
            float tamanhoFonte = 10F)
        {
            try
            {
                SKImage imagem = Codificar(
                    codigo,
                    simbologia,
                    largura,
                    altura,
                    incluirRotulo,
                    familiaFonte,
                    tamanhoFonte);
                if (imagem != null)
                    return imagem;

                throw new InvalidOperationException(
                    "A biblioteca nao gerou a imagem na dimensao solicitada.");
            }
            catch (Exception ex)
            {
                // Algumas versoes da biblioteca recusam imagens menores que a
                // quantidade de modulos. O fallback preserva a renderizacao e
                // reduz uma unica vez somente nesse caso extremo.
                const int alturaFallback = 60;
                int larguraGeracao = Math.Max(
                    largura,
                    CalcularLarguraFallback(codigo, simbologia));
                int alturaGeracao = Math.Max(altura, alturaFallback);
                float escalaVertical = altura > 0 ? (float)alturaGeracao / altura : 1F;

                System.Diagnostics.Debug.WriteLine(
                    $"Codigo de barras renderizado por fallback em {largura}x{altura}: {ex.Message}");

                return Codificar(
                    codigo,
                    simbologia,
                    larguraGeracao,
                    alturaGeracao,
                    incluirRotulo,
                    familiaFonte,
                    Math.Max(0.1F, tamanhoFonte * escalaVertical));
            }
        }

        private static int CalcularLarguraFallback(
            string codigo,
            BarcodeType simbologia)
        {
            if (simbologia == BarcodeType.Ean13)
                return 240;

            // No pior caso do Code128, cada caractere ocupa um simbolo de
            // 11 modulos. Somam-se inicio, checksum e parada. Dois pixels por
            // modulo fornecem uma origem segura para a unica reducao final.
            long quantidadeCaracteres = Math.Max(1, (codigo ?? string.Empty).Length);
            long modulosEstimados = (quantidadeCaracteres * 11L) + 35L;
            long larguraCalculada = modulosEstimados * 2L;

            return (int)Math.Min(
                32767L,
                Math.Max(240L, larguraCalculada));
        }

        private static SKImage Codificar(
            string codigo,
            BarcodeType simbologia,
            int largura,
            int altura,
            bool incluirRotulo = false,
            string familiaFonte = null,
            float tamanhoFonte = 10F)
        {
            var barcode = new Barcode
            {
                Width = largura,
                Height = altura,
                IncludeLabel = incluirRotulo,
                Alignment = AlignmentPositions.Center,
                ForeColor = SKColors.Black,
                BackColor = SKColors.White
            };

            if (!incluirRotulo)
                return barcode.Encode(simbologia, codigo);

            using (SKTypeface typeface = SKTypeface.FromFamilyName(
                string.IsNullOrWhiteSpace(familiaFonte) ? "Arial" : familiaFonte))
            using (var labelFont = new SKFont(typeface, tamanhoFonte))
            {
                barcode.LabelFont = labelFont;
                return barcode.Encode(simbologia, codigo);
            }
        }

        private static void DesenharImagem(Graphics graphics, SKImage imagem, RectangleF area)
        {
            using (SKData dados = imagem.Encode(SKEncodedImageFormat.Png, 100))
            using (var stream = new MemoryStream(dados.ToArray()))
            using (Image imagemGdi = Image.FromStream(stream))
            {
                GraphicsState estado = graphics.Save();
                try
                {
                    graphics.SmoothingMode = SmoothingMode.None;
                    graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                    graphics.PixelOffsetMode = PixelOffsetMode.Half;
                    graphics.DrawImage(imagemGdi, area);
                }
                finally
                {
                    graphics.Restore(estado);
                }
            }
        }

        private static void DesenharMensagem(
            Graphics graphics,
            string mensagem,
            RectangleF area,
            Font fonte,
            Color cor)
        {
            using (var brush = new SolidBrush(cor))
            using (var formato = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            })
            {
                graphics.DrawString(mensagem, fonte ?? SystemFonts.DefaultFont, brush, area, formato);
            }
        }
    }
}
