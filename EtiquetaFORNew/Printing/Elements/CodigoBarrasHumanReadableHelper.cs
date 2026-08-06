using System;
using System.Drawing;
using System.Linq;

namespace EtiquetaFORNew
{
    public static class CodigoBarrasHumanReadableHelper
    {
        private const float ProporcaoAlturaNumeracao = 0.22F;

        public static string FormatarNumeracao(string valor, bool agrupar)
        {
            string texto = new string((valor ?? string.Empty)
                .Where(c => !char.IsControl(c))
                .ToArray());

            if (!agrupar || texto.Length == 0 || texto.Any(c => !char.IsDigit(c)))
                return texto;

            switch (texto.Length)
            {
                case 8:
                    return texto.Substring(0, 4) + " " + texto.Substring(4, 4);
                case 12:
                    return texto.Substring(0, 1) + " " + texto.Substring(1, 5) + " " +
                           texto.Substring(6, 5) + " " + texto.Substring(11, 1);
                case 13:
                    return texto.Substring(0, 1) + " " + texto.Substring(1, 6) + " " +
                           texto.Substring(7, 6);
                case 14:
                    return texto.Substring(0, 1) + " " + texto.Substring(1, 6) + " " +
                           texto.Substring(7, 6) + " " + texto.Substring(13, 1);
                default:
                    return AgruparEmBlocos(texto, 4);
            }
        }

        public static void CalcularAreas(
            RectangleF areaTotal,
            out RectangleF areaBarras,
            out RectangleF areaNumeracao)
        {
            if (areaTotal.Width <= 0F || areaTotal.Height <= 0F)
            {
                areaBarras = RectangleF.Empty;
                areaNumeracao = RectangleF.Empty;
                return;
            }

            // Somente proporcao: o mesmo calculo funciona quando a unidade do
            // Graphics e pixel (Designer/Preview) ou milimetro (impressao).
            float alturaNumeracao = areaTotal.Height * ProporcaoAlturaNumeracao;
            float alturaBarras = areaTotal.Height - alturaNumeracao;

            areaBarras = new RectangleF(
                areaTotal.X,
                areaTotal.Y,
                areaTotal.Width,
                alturaBarras);
            areaNumeracao = new RectangleF(
                areaTotal.X,
                areaBarras.Bottom,
                areaTotal.Width,
                alturaNumeracao);
        }

        public static void DesenharNumeracao(
            Graphics graphics,
            string valor,
            bool agrupar,
            RectangleF area,
            Font fonte,
            Color cor)
        {
            string texto = FormatarNumeracao(valor, agrupar);
            if (string.IsNullOrEmpty(texto) || area.Width <= 0 || area.Height <= 0)
                return;

            using (var fundo = new SolidBrush(Color.White))
            using (var brush = new SolidBrush(cor))
            using (var formato = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.None,
                FormatFlags = StringFormatFlags.NoWrap
            })
            {
                graphics.FillRectangle(fundo, area);

                Font fonteBase = fonte ?? SystemFonts.DefaultFont;
                SizeF tamanhoTexto = graphics.MeasureString(
                    texto,
                    fonteBase,
                    new SizeF(float.MaxValue, float.MaxValue),
                    formato);
                float escalaHorizontal = tamanhoTexto.Width > 0F
                    ? area.Width / tamanhoTexto.Width
                    : 1F;
                float escalaVertical = tamanhoTexto.Height > 0F
                    ? area.Height / tamanhoTexto.Height
                    : 1F;
                float escalaFonte = Math.Min(
                    1F,
                    Math.Min(escalaHorizontal, escalaVertical));

                if (escalaFonte < 0.999F)
                {
                    using (var fonteAjustada = new Font(
                        fonteBase.FontFamily,
                        Math.Max(0.1F, fonteBase.SizeInPoints * escalaFonte),
                        fonteBase.Style,
                        GraphicsUnit.Point))
                    {
                        graphics.DrawString(texto, fonteAjustada, brush, area, formato);
                    }
                }
                else
                {
                    graphics.DrawString(texto, fonteBase, brush, area, formato);
                }
            }
        }

        private static string AgruparEmBlocos(string texto, int tamanhoBloco)
        {
            return string.Join(" ", Enumerable.Range(0, (texto.Length + tamanhoBloco - 1) / tamanhoBloco)
                .Select(indice => texto.Substring(
                    indice * tamanhoBloco,
                    Math.Min(tamanhoBloco, texto.Length - indice * tamanhoBloco))));
        }
    }
}
