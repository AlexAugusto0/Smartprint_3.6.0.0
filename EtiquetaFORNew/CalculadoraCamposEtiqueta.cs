using System;
using System.Collections.Generic;
using System.Globalization;

namespace EtiquetaFORNew
{
    public static class CalculadoraCamposEtiqueta
    {
        private static readonly HashSet<string> CamposPreco = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "PRECO",
            "PRECOVENDA",
            "VENDAA",
            "VENDAB",
            "VENDAC",
            "VENDAD",
            "VENDAE",
            "PRECOORIGINAL",
            "PRECOPROMOCIONAL"
        };

        public static bool CampoPermiteCalculo(string campo)
        {
            return !string.IsNullOrWhiteSpace(campo) && CamposPreco.Contains(NormalizarCampo(campo));
        }

        public static bool CalculoAtivo(ElementoEtiqueta elemento)
        {
            return elemento != null
                && CampoPermiteCalculo(elemento.Conteudo)
                && !string.IsNullOrWhiteSpace(elemento.OperadorCalculoPreco)
                && OperadorValido(elemento.OperadorCalculoPreco);
        }

        public static bool TryCalcularValorCampo(Produto produto, string campo, ElementoEtiqueta elemento, out decimal valor)
        {
            valor = 0m;

            if (produto == null || !CampoPermiteCalculo(campo))
                return false;

            if (!TryObterValorPreco(produto, campo, out valor))
                return false;

            AplicarCalculo(valor, elemento?.OperadorCalculoPreco, elemento?.ValorCalculoPreco ?? 0m, out valor);
            return true;
        }

        public static string ObterDescricaoCalculo(string campo, ElementoEtiqueta elemento)
        {
            if (!CalculoAtivo(elemento))
                return campo ?? "";

            string valor = elemento.ValorCalculoPreco.ToString("0.####", CultureInfo.CurrentCulture);
            return $"{campo} {elemento.OperadorCalculoPreco} {valor}";
        }

        private static bool OperadorValido(string operador)
        {
            return operador == "+"
                || operador == "-"
                || operador == "*"
                || operador == "/";
        }

        private static void AplicarCalculo(decimal valorBase, string operador, decimal operando, out decimal resultado)
        {
            resultado = valorBase;

            switch (operador)
            {
                case "+":
                    resultado = valorBase + operando;
                    break;
                case "-":
                    resultado = valorBase - operando;
                    break;
                case "*":
                    resultado = valorBase * operando;
                    break;
                case "/":
                    if (operando != 0m)
                        resultado = valorBase / operando;
                    break;
            }
        }

        private static bool TryObterValorPreco(Produto produto, string campo, out decimal valor)
        {
            valor = 0m;

            switch (NormalizarCampo(campo))
            {
                case "PRECO":
                    valor = produto.Preco;
                    return true;
                case "PRECOVENDA":
                    valor = produto.PrecoVenda > 0m ? produto.PrecoVenda : produto.Preco;
                    return true;
                case "VENDAA":
                    valor = produto.VendaA;
                    return true;
                case "VENDAB":
                    valor = produto.VendaB;
                    return true;
                case "VENDAC":
                    valor = produto.VendaC;
                    return true;
                case "VENDAD":
                    valor = produto.VendaD;
                    return true;
                case "VENDAE":
                    valor = produto.VendaE;
                    return true;
                case "PRECOORIGINAL":
                    valor = produto.PrecoOriginal.HasValue ? produto.PrecoOriginal.Value : produto.Preco;
                    return true;
                case "PRECOPROMOCIONAL":
                    valor = produto.PrecoPromocional.HasValue ? produto.PrecoPromocional.Value : produto.Preco;
                    return true;
                default:
                    return false;
            }
        }

        private static string NormalizarCampo(string campo)
        {
            return (campo ?? "")
                .Replace(" ", "")
                .Replace("_", "")
                .Replace("-", "")
                .ToUpperInvariant();
        }
    }
}
