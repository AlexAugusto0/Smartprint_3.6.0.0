using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace EtiquetaFORNew
{
    public sealed class ResultadoExpressao
    {
        public bool Sucesso { get; private set; }
        public decimal Valor { get; private set; }
        public string MensagemErro { get; private set; }
        public int PosicaoErro { get; private set; }
        public IReadOnlyList<string> CamposUtilizados { get; private set; }

        public static ResultadoExpressao Ok(decimal valor, IEnumerable<string> campos)
        {
            return new ResultadoExpressao
            {
                Sucesso = true,
                Valor = valor,
                PosicaoErro = -1,
                CamposUtilizados = campos.Distinct(StringComparer.OrdinalIgnoreCase).ToList()
            };
        }

        public static ResultadoExpressao Erro(string mensagem, int posicao, IEnumerable<string> campos)
        {
            return new ResultadoExpressao
            {
                Sucesso = false,
                MensagemErro = mensagem,
                PosicaoErro = posicao,
                CamposUtilizados = campos.Distinct(StringComparer.OrdinalIgnoreCase).ToList()
            };
        }
    }

    public static class ExpressionEngine
    {
        public delegate bool TryResolverVariavel(string nome, out decimal valor, out string mensagemErro);

        public static ResultadoExpressao Validar(string expressao)
        {
            return Avaliar(expressao, ResolverCampoParaValidacao);
        }

        public static ResultadoExpressao Calcular(string expressao, Produto produto)
        {
            return Avaliar(expressao, (string nome, out decimal valor, out string mensagemErro) =>
                CampoEtiquetaResolver.TryObterValorDecimal(produto, nome, out valor, out mensagemErro));
        }

        public static ResultadoExpressao Avaliar(string expressao, TryResolverVariavel resolver)
        {
            var parser = new Parser(expressao, resolver);
            return parser.Avaliar();
        }

        private static bool ResolverCampoParaValidacao(string nome, out decimal valor, out string mensagemErro)
        {
            valor = 1m;
            mensagemErro = null;

            if (CampoEtiquetaResolver.CampoExiste(nome))
                return true;

            mensagemErro = $"Campo \"{nome}\" não encontrado.";
            return false;
        }

        private struct ValorExpressao
        {
            public decimal Valor { get; private set; }
            public bool Percentual { get; private set; }

            public ValorExpressao(decimal valor, bool percentual)
            {
                Valor = valor;
                Percentual = percentual;
            }

            public ValorExpressao ComoNumero()
            {
                return Percentual
                    ? new ValorExpressao(Valor / 100m, false)
                    : this;
            }
        }

        private sealed class Parser
        {
            private readonly string texto;
            private readonly TryResolverVariavel resolver;
            private readonly List<string> campos = new List<string>();
            private int posicao;

            public Parser(string texto, TryResolverVariavel resolver)
            {
                this.texto = texto ?? string.Empty;
                this.resolver = resolver;
            }

            public ResultadoExpressao Avaliar()
            {
                if (string.IsNullOrWhiteSpace(texto))
                    return ResultadoExpressao.Erro("Expressão vazia.", 0, campos);

                try
                {
                    ValorExpressao resultado = ParseAdicaoSubtracao().ComoNumero();
                    IgnorarEspacos();

                    if (!Fim)
                        throw CriarErro($"Operador ou caractere inválido \"{Atual}\"", posicao);

                    return ResultadoExpressao.Ok(resultado.Valor, campos);
                }
                catch (ExpressionParseException ex)
                {
                    return ResultadoExpressao.Erro(ex.Message, ex.Posicao, campos);
                }
            }

            private ValorExpressao ParseAdicaoSubtracao()
            {
                ValorExpressao esquerda = ParseMultiplicacaoDivisao();

                while (true)
                {
                    IgnorarEspacos();
                    if (!Aceitar('+') && !Aceitar('-'))
                        break;

                    char operador = texto[posicao - 1];
                    int posicaoOperador = posicao - 1;
                    esquerda = esquerda.ComoNumero();

                    ValorExpressao direita = ParseMultiplicacaoDivisao();
                    if (direita.Percentual)
                    {
                        decimal variacao = esquerda.Valor * direita.Valor / 100m;
                        esquerda = new ValorExpressao(
                            operador == '+' ? esquerda.Valor + variacao : esquerda.Valor - variacao,
                            false);
                    }
                    else
                    {
                        direita = direita.ComoNumero();
                        esquerda = new ValorExpressao(
                            operador == '+' ? esquerda.Valor + direita.Valor : esquerda.Valor - direita.Valor,
                            false);
                    }

                    if (posicao == posicaoOperador + 1)
                        throw CriarErro("Valor esperado", posicao);
                }

                return esquerda;
            }

            private ValorExpressao ParseMultiplicacaoDivisao()
            {
                ValorExpressao esquerda = ParseUnario();

                while (true)
                {
                    IgnorarEspacos();
                    if (!Aceitar('*') && !Aceitar('/'))
                        break;

                    char operador = texto[posicao - 1];
                    esquerda = esquerda.ComoNumero();
                    ValorExpressao direita = ParseUnario().ComoNumero();

                    if (operador == '*')
                    {
                        esquerda = new ValorExpressao(esquerda.Valor * direita.Valor, false);
                    }
                    else
                    {
                        if (direita.Valor == 0m)
                            throw CriarErro("Divisão por zero", posicao - 1);

                        esquerda = new ValorExpressao(esquerda.Valor / direita.Valor, false);
                    }
                }

                return esquerda;
            }

            private ValorExpressao ParseUnario()
            {
                IgnorarEspacos();

                if (Aceitar('+'))
                    return ParseUnario();

                if (Aceitar('-'))
                {
                    ValorExpressao valor = ParseUnario();
                    return new ValorExpressao(-valor.Valor, valor.Percentual);
                }

                return ParsePrimario();
            }

            private ValorExpressao ParsePrimario()
            {
                IgnorarEspacos();

                if (Fim)
                    throw CriarErro("Valor esperado", posicao);

                int inicio = posicao;

                if (Aceitar('('))
                {
                    ValorExpressao valor = ParseAdicaoSubtracao();
                    IgnorarEspacos();

                    if (!Aceitar(')'))
                        throw CriarErro("Parêntese ')' esperado", posicao);

                    return AplicarPercentualOpcional(valor.ComoNumero());
                }

                if (char.IsDigit(Atual) || Atual == ',' || Atual == '.')
                {
                    return AplicarPercentualOpcional(ParseNumero(inicio));
                }

                if (EhInicioIdentificador(Atual))
                {
                    string nome = LerIdentificador();
                    IgnorarEspacos();

                    if (Aceitar('('))
                        throw CriarErro($"Função \"{nome}\" ainda não suportada", inicio);

                    campos.Add(nome);

                    decimal valor;
                    string mensagemErro;
                    if (!resolver(nome, out valor, out mensagemErro))
                        throw new ExpressionParseException(mensagemErro, inicio);

                    return AplicarPercentualOpcional(new ValorExpressao(valor, false));
                }

                throw CriarErro("Valor esperado", posicao);
            }

            private ValorExpressao ParseNumero(int inicio)
            {
                while (!Fim && (char.IsDigit(Atual) || Atual == ',' || Atual == '.'))
                    posicao++;

                string literal = texto.Substring(inicio, posicao - inicio);
                decimal valor;
                if (!TryParseNumero(literal, out valor))
                    throw CriarErro($"Número inválido \"{literal}\"", inicio);

                return new ValorExpressao(valor, false);
            }

            private ValorExpressao AplicarPercentualOpcional(ValorExpressao valor)
            {
                IgnorarEspacos();
                return Aceitar('%')
                    ? new ValorExpressao(valor.Valor, true)
                    : valor;
            }

            private string LerIdentificador()
            {
                int inicio = posicao;
                posicao++;

                while (!Fim && EhParteIdentificador(Atual))
                    posicao++;

                return texto.Substring(inicio, posicao - inicio);
            }

            private bool Aceitar(char esperado)
            {
                IgnorarEspacos();

                if (Fim || texto[posicao] != esperado)
                    return false;

                posicao++;
                return true;
            }

            private void IgnorarEspacos()
            {
                while (!Fim && char.IsWhiteSpace(texto[posicao]))
                    posicao++;
            }

            private bool Fim => posicao >= texto.Length;

            private char Atual => Fim ? '\0' : texto[posicao];

            private static bool EhInicioIdentificador(char c)
            {
                return c == '_' || char.IsLetter(c);
            }

            private static bool EhParteIdentificador(char c)
            {
                return c == '_' || char.IsLetterOrDigit(c);
            }

            private static bool TryParseNumero(string literal, out decimal valor)
            {
                valor = 0m;
                if (string.IsNullOrWhiteSpace(literal))
                    return false;

                string normalizado = literal.Trim();
                int ultimaVirgula = normalizado.LastIndexOf(',');
                int ultimoPonto = normalizado.LastIndexOf('.');

                if (ultimaVirgula >= 0 && ultimoPonto >= 0)
                {
                    if (ultimaVirgula > ultimoPonto)
                        normalizado = normalizado.Replace(".", "").Replace(',', '.');
                    else
                        normalizado = normalizado.Replace(",", "");
                }
                else if (ultimaVirgula >= 0)
                {
                    normalizado = normalizado.Replace(',', '.');
                }

                return decimal.TryParse(
                    normalizado,
                    NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture,
                    out valor);
            }

            private ExpressionParseException CriarErro(string mensagem, int posicaoErro)
            {
                int posicaoMensagem = Math.Max(1, posicaoErro + 1);
                return new ExpressionParseException($"{mensagem} próximo ao caractere {posicaoMensagem}.", posicaoErro);
            }
        }

        private sealed class ExpressionParseException : Exception
        {
            public int Posicao { get; private set; }

            public ExpressionParseException(string mensagem, int posicao)
                : base(mensagem)
            {
                Posicao = posicao;
            }
        }
    }
}
