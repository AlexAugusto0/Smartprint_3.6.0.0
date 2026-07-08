using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace EtiquetaFORNew
{
    public static class CampoEtiquetaResolver
    {
        private sealed class CampoDefinicao
        {
            public string Nome { get; set; }
            public Func<Produto, string> ObterTexto { get; set; }
            public Func<Produto, decimal?> ObterNumero { get; set; }
        }

        private static readonly Dictionary<string, CampoDefinicao> Campos =
            new Dictionary<string, CampoDefinicao>(StringComparer.OrdinalIgnoreCase);

        private static readonly string[] CamposPreferenciais =
        {
            "Codigo",
            "CodigoMercadoria",
            "Descricao",
            "Mercadoria",
            "Referencia",
            "CodFabricante",
            "CodBarras",
            "CodBarras_Grade",
            "Preco",
            "PrecoCusto",
            "PrecoVenda",
            "VendaA",
            "VendaB",
            "VendaC",
            "VendaD",
            "VendaE",
            "Quantidade",
            "Fornecedor",
            "Fabricante",
            "Grupo",
            "SubGrupo",
            "Marca",
            "Prateleira",
            "Garantia",
            "Tam",
            "Cores",
            "PrecoOriginal",
            "PrecoPromocional"
        };

        static CampoEtiquetaResolver()
        {
            RegistrarTexto("Nome", p => p.Nome, "Mercadoria", "Descricao", "Descrição");
            RegistrarTexto("Codigo", p => p.Codigo, "CodigoMercadoria", "Código", "CódigoMercadoria");
            RegistrarTexto("Referencia", p => p.CodFabricante, "CodFabricante", "Referência");
            RegistrarTexto("CodBarras", p => p.CodBarras, "CodigoBarras", "CódigoBarras");
            RegistrarTexto("CodBarras_Grade", p => p.CodBarras_Grade, "CodigoBarrasGrade");

            RegistrarNumero("Preco", p => p.Preco, "PrecoCusto", "Preço", "PreçoCusto");
            RegistrarNumero("PrecoVenda", p => p.PrecoVenda > 0m ? p.PrecoVenda : p.Preco, "PreçoVenda", "Preco de Venda", "Preço de Venda");
            RegistrarNumero("VendaA", p => p.VendaA);
            RegistrarNumero("VendaB", p => p.VendaB);
            RegistrarNumero("VendaC", p => p.VendaC);
            RegistrarNumero("VendaD", p => p.VendaD);
            RegistrarNumero("VendaE", p => p.VendaE);
            RegistrarNumero("Quantidade", p => p.Quantidade);
            RegistrarNumero("PrecoOriginal", p => p.PrecoOriginal.HasValue ? p.PrecoOriginal.Value : p.Preco, "PreçoOriginal");
            RegistrarNumero("PrecoPromocional", p => p.PrecoPromocional.HasValue ? p.PrecoPromocional.Value : p.Preco, "PreçoPromocional");

            RegistrarTexto("Fornecedor", p => p.Fornecedor);
            RegistrarTexto("Fabricante", p => p.Fabricante);
            RegistrarTexto("Grupo", p => p.Grupo);
            RegistrarTexto("SubGrupo", p => p.SubGrupo, "Sub Grupo");
            RegistrarTexto("Marca", p => p.Marca);
            RegistrarTexto("Prateleira", p => p.Prateleira);
            RegistrarTexto("Garantia", p => p.Garantia);
            RegistrarTexto("Tam", p => p.Tam, "Tamanho");
            RegistrarTexto("Cores", p => p.Cores, "Cor");
        }

        public static IReadOnlyList<string> ObterCamposDisponiveis()
        {
            return CamposPreferenciais;
        }

        public static bool CampoExiste(string campo)
        {
            return TryObterDefinicao(campo, out _);
        }

        public static bool TryObterValorDecimal(Produto produto, string campo, out decimal valor, out string mensagemErro)
        {
            valor = 0m;
            mensagemErro = null;

            if (!TryObterDefinicao(campo, out CampoDefinicao definicao))
            {
                mensagemErro = $"Campo \"{campo}\" não encontrado.";
                return false;
            }

            if (produto == null)
            {
                mensagemErro = "Nenhum produto disponível para calcular a expressão.";
                return false;
            }

            if (definicao.ObterNumero != null)
            {
                decimal? numero = definicao.ObterNumero(produto);
                if (numero.HasValue)
                {
                    valor = numero.Value;
                    return true;
                }
            }

            string texto = definicao.ObterTexto != null ? definicao.ObterTexto(produto) : null;
            if (FormatadorMonetario.TryConverter(texto, out valor))
                return true;

            mensagemErro = $"Campo \"{campo}\" não possui valor numérico.";
            return false;
        }

        public static string ObterTexto(Produto produto, string campo)
        {
            if (produto == null)
                return string.Empty;

            if (!TryObterDefinicao(campo, out CampoDefinicao definicao))
                return string.Empty;

            if (definicao.ObterTexto != null)
                return definicao.ObterTexto(produto) ?? string.Empty;

            if (definicao.ObterNumero != null)
            {
                decimal? numero = definicao.ObterNumero(produto);
                return numero.HasValue ? FormatadorMonetario.Formatar(numero.Value) : string.Empty;
            }

            return string.Empty;
        }

        public static string ObterCodigoBarras(Produto produto, string campo, string valorPadrao)
        {
            if (produto == null)
                return valorPadrao;

            string valor = ObterTexto(produto, campo);
            return string.IsNullOrWhiteSpace(valor) ? valorPadrao : valor;
        }

        private static void RegistrarTexto(string nome, Func<Produto, string> obterTexto, params string[] aliases)
        {
            var definicao = new CampoDefinicao
            {
                Nome = nome,
                ObterTexto = obterTexto
            };

            Registrar(definicao, aliases);
        }

        private static void RegistrarNumero(string nome, Func<Produto, decimal?> obterNumero, params string[] aliases)
        {
            var definicao = new CampoDefinicao
            {
                Nome = nome,
                ObterNumero = obterNumero
            };

            Registrar(definicao, aliases);
        }

        private static void Registrar(CampoDefinicao definicao, params string[] aliases)
        {
            Adicionar(definicao.Nome, definicao);

            if (aliases == null)
                return;

            foreach (string alias in aliases.Where(a => !string.IsNullOrWhiteSpace(a)))
                Adicionar(alias, definicao);
        }

        private static void Adicionar(string nome, CampoDefinicao definicao)
        {
            string chave = NormalizarCampo(nome);
            if (!Campos.ContainsKey(chave))
                Campos.Add(chave, definicao);
        }

        private static bool TryObterDefinicao(string campo, out CampoDefinicao definicao)
        {
            definicao = null;

            if (string.IsNullOrWhiteSpace(campo))
                return false;

            return Campos.TryGetValue(NormalizarCampo(campo), out definicao);
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

                if (char.IsWhiteSpace(c) || c == '_' || c == '-' || c == '.')
                    continue;

                sb.Append(char.ToUpperInvariant(c));
            }

            return sb.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
