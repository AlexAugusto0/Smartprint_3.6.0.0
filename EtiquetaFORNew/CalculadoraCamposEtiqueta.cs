using System;
using System.Collections.Generic;

namespace EtiquetaFORNew
{
    public static class CalculadoraCamposEtiqueta
    {
        private static readonly HashSet<string> CamposPreco = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "PRECO",
            "PRECOCUSTO",
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

            string valor = FormatadorMonetario.Formatar(elemento.ValorCalculoPreco);
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
            string mensagemErro;
            return CampoEtiquetaResolver.TryObterValorDecimal(produto, campo, out valor, out mensagemErro);
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
