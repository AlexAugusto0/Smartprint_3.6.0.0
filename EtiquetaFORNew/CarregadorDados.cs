using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace EtiquetaFORNew.Data
{
    /// <summary>
    /// Gerenciador de carregamento de produtos por diferentes tipos
    /// Equivalente ÃƒÆ’Ã‚Â s queries do SoftShop: GeradordeEtiquetas_Carregar*
    /// </summary>
    public static class CarregadorDados
    {
        // ÃƒÂ¢Ã‚Â­Ã‚Â ConnectionString local (mesmo do LocalDatabaseManager)
        private static readonly string DbPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "LocalData.db");
        private static readonly string ConnectionString = $"Data Source={DbPath};Version=3;";
        // ========================================
        // ÃƒÂ°Ã…Â¸Ã¢â‚¬ÂÃ‚Â¹ CARREGAMENTO POR TIPO
        // ========================================

        /// <summary>
        /// Carrega produtos baseado no tipo e filtros fornecidos
        /// </summary>
        public static DataTable CarregarProdutosPorTipo(
            string tipo,
            string documento = null,
            DateTime? dataInicial = null,
            DateTime? dataFinal = null,
            string grupo = null,
            string subGrupo = null,
            string fabricante = null,
            string fornecedor = null,
            string produto = null,
            bool isConfeccao = false,
            int? idPromocao = null, // ÃƒÂ¢Ã‚Â­Ã‚Â NOVO parÃƒÆ’Ã‚Â¢metro
            bool usarQuantidadeEstoque = false)
        {
            switch (tipo.ToUpper())
            {
                case "AJUSTES":
                    return CarregarAjustes(documento, dataInicial, dataFinal);

                case "BALANÇOS":
                    return CarregarBalancos(documento, dataInicial, dataFinal);

                case "NOTAS ENTRADA":
                    if (EstaEmModoSoftcomShop())
                    {
                        return CarregarNotasEntradaSoftcomShop(documento, dataInicial);
                    }

                    return CarregarNotasEntrada(documento, dataInicial, dataFinal);

                case "PREÇOS ALTERADOS":
                    if (!dataInicial.HasValue)
                        throw new Exception("Informe a data inicial para carregar preços alterados.");

                    if (EstaEmModoSoftcomShop())
                    {
                        return CarregarPrecosAlteradosSoftcomShop(dataInicial.Value);
                    }

                    if (!dataFinal.HasValue)
                        throw new Exception("Informe o período para carregar preços alterados.");

                    return CarregarPrecosAlterados(dataInicial.Value, dataFinal.Value, usarQuantidadeEstoque);

                case "VENDAS":
                    if (!EstaEmModoSoftcomShop())
                    {
                        throw new Exception("A importacao por vendas esta disponivel somente para SoftcomShop.");
                    }

                    return CarregarVendaSoftcomShop(documento);

                //case "PROMOÇÕES":
                //    // ÃƒÂ¢Ã‚Â­Ã‚Â Usa o mÃƒÆ’Ã‚Â©todo do PromocoesManager com ID da promoÃ§ÃƒÆ’Ã‚Â£o
                //    if (idPromocao.HasValue)
                //    {
                //        return PromocoesManager.BuscarProdutosDaPromocao(
                //            idPromocao.Value,
                //            null, // loja (usa padrÃƒÆ’Ã‚Â£o)
                //            produto,
                //            grupo,
                //            subGrupo,
                //            fabricante,
                //            fornecedor);
                //    }
                //    else
                //    {
                //        throw new Exception("ID da promoÃ§ÃƒÆ’Ã‚Â£o nÃƒÆ’Ã‚Â£o foi informado!");
                //    }
                case "PROMOÇÕES":
                    // 1. Prioridade para busca Local/Web (SQLite) se não houver um ID de promoção específico
                    // Ou se a lógica de negócio exigir que o "EmPromocao" do banco local seja verificado
                    if (!idPromocao.HasValue)
                    {
                        // Chama o método que criamos para o SQLite (Web)
                        // Passando os filtros e o isConfeccao que você precisa
                        return CarregarPromocoesSoftcomShopLocal(grupo, fabricante, produto);
                    }

                    // 2. Se houver ID, segue para o SQL Server via PromocoesManager
                    // Adicionamos o isConfeccao no final da assinatura para não quebrar a lógica
                    return PromocoesManager.BuscarProdutosDaPromocao(
                        idPromocao.Value,
                        null, // loja (usa padrão)
                        produto,
                        grupo,
                        subGrupo,
                        fabricante,
                        fornecedor,
                        isConfeccao // <--- O parâmetro que você precisava incluir
                    );

                case "FILTROS MANUAIS":
                default:


                    grupo = LimparFiltro(grupo);
                    fabricante = LimparFiltro(fabricante);
                    fornecedor = LimparFiltro(fornecedor);

                    //MessageBox.Show(
                    //$"Grupo=[{grupo}]\n" +
                    //$"Fabricante=[{fabricante}]\n" +
                    //$"Fornecedor=[{fornecedor}]");

                    // Para filtros manuais, usa o mÃƒÆ’Ã‚Â©todo existente do LocalDatabaseManager
                    // que aceita: grupo, fabricante, fornecedor, isConfeccao
                    return LocalDatabaseManager.BuscarMercadoriasPorFiltrosManuais(
                        grupo,
                        fabricante,
                        fornecedor,
                        isConfeccao);
            }
        }

        // ========================================
        // ÃƒÂ°Ã…Â¸Ã¢â‚¬ÂÃ‚Â¹ AJUSTES DE ESTOQUE
        // ========================================
        /// <summary>
        /// Carrega produtos de ajustes de estoque
        /// Equivalente: GeradordeEtiquetas_CarregarAjustes
        /// </summary>
        private static DataTable CarregarAjustes(string numeroAjuste, DateTime? dataInicial, DateTime? dataFinal)
        {
            try
            {
                return CarregarMovimentoEstoqueSql(
                    descricao: "ajustes",
                    numeroDocumento: numeroAjuste,
                    dataInicial: dataInicial,
                    dataFinal: dataFinal,
                    tabelasCabecalho: new[] { "Inventário", "Inventario" },
                    tabelasItens: new[] { "Sub Inventário", "Sub Inventario" },
                    colunasDocumentoCabecalho: new[]
                    {
                        "Código da Compra", "Codigo da Compra", "Código do Inventário", "Codigo do Inventario",
                        "Número do Ajuste", "Numero do Ajuste", "NumeroAjuste", "Ajuste"
                    },
                    colunasDocumentoItens: new[]
                    {
                        "Código da Compra", "Codigo da Compra", "Código do Inventário", "Codigo do Inventario",
                        "Número do Ajuste", "Numero do Ajuste", "NumeroAjuste", "Ajuste"
                    },
                    colunasData: new[]
                    {
                        "Data", "Data da Compra", "Data Compra", "Data do Inventário", "Data do Inventario",
                        "Data do Ajuste", "DataAjuste", "Emissão", "Emissao"
                    });
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao carregar ajustes: {ex.Message}", ex);
            }
        }

        // ========================================
        // ÃƒÂ°Ã…Â¸Ã¢â‚¬ÂÃ‚Â¹ BALANÃ‡OS
        // ========================================
        /// <summary>
        /// Carrega produtos de balanÃ§os de estoque
        /// Equivalente: GeradordeEtiquetas_CarregarBalancos
        /// </summary>
        private static DataTable CarregarBalancos(string numeroBalanco, DateTime? dataInicial, DateTime? dataFinal)
        {
            try
            {
                return CarregarMovimentoEstoqueSql(
                    descricao: "balanços",
                    numeroDocumento: numeroBalanco,
                    dataInicial: dataInicial,
                    dataFinal: dataFinal,
                    tabelasCabecalho: new[] { "Balanço", "Balanco" },
                    tabelasItens: new[] { "Sub Balanço", "Sub Balanco" },
                    colunasDocumentoCabecalho: new[]
                    {
                        "Código da Compra", "Codigo da Compra", "Código do Balanço", "Codigo do Balanco",
                        "Número do Balanço", "Numero do Balanco", "NumeroBalanco", "Balanço", "Balanco"
                    },
                    colunasDocumentoItens: new[]
                    {
                        "Código da Compra", "Codigo da Compra", "Código do Balanço", "Codigo do Balanco",
                        "Número do Balanço", "Numero do Balanco", "NumeroBalanco", "Balanço", "Balanco"
                    },
                    colunasData: new[]
                    {
                        "Data", "Data da Compra", "Data Compra", "Data do Balanço", "Data do Balanco",
                        "DataBalanco", "Emissão", "Emissao"
                    });
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao carregar balanÃ§os: {ex.Message}", ex);
            }
        }

        private sealed class TabelaSqlInfo
        {
            public string Schema { get; set; }
            public string Nome { get; set; }

            public string NomeQualificado
            {
                get { return DelimitarIdentificador(Schema) + "." + DelimitarIdentificador(Nome); }
            }
        }

        private static DataTable CarregarMovimentoEstoqueSql(
            string descricao,
            string numeroDocumento,
            DateTime? dataInicial,
            DateTime? dataFinal,
            string[] tabelasCabecalho,
            string[] tabelasItens,
            string[] colunasDocumentoCabecalho,
            string[] colunasDocumentoItens,
            string[] colunasData)
        {
            string connectionStringSQLServer = DatabaseConfig.GetConnectionString();
            if (string.IsNullOrEmpty(connectionStringSQLServer))
                throw new Exception("Conexão SQL Server não configurada!");

            string loja = ObterLojaConfigurada();

            using (var conn = new SqlConnection(connectionStringSQLServer))
            {
                conn.Open();

                TabelaSqlInfo tabelaCabecalho = ObterTabelaSql(conn, true, tabelasCabecalho);
                TabelaSqlInfo tabelaItens = ObterTabelaSql(conn, true, tabelasItens);
                TabelaSqlInfo tabelaProdutos = ObterTabelaSql(conn, true, "memoria_MercadoriasLojas");

                Dictionary<string, string> colCabecalho = ObterMapaColunasSql(conn, tabelaCabecalho);
                Dictionary<string, string> colItens = ObterMapaColunasSql(conn, tabelaItens);
                Dictionary<string, string> colProdutos = ObterMapaColunasSql(conn, tabelaProdutos);

                string docCabecalho = ObterColunaSql(tabelaCabecalho, colCabecalho, true, colunasDocumentoCabecalho);
                string docItens = ObterColunaSql(tabelaItens, colItens, true, colunasDocumentoItens);
                string lojaCabecalho = ObterColunaSql(tabelaCabecalho, colCabecalho, false,
                    "Loja Origem", "LojaOrigem", "Loja", "Código da Loja", "Codigo da Loja");
                string dataCabecalho = ObterColunaSql(tabelaCabecalho, colCabecalho,
                    dataInicial.HasValue || dataFinal.HasValue, colunasData);

                string itemCodigo = ObterColunaSql(tabelaItens, colItens, true,
                    "Código da Mercadoria", "Codigo da Mercadoria", "CodigoMercadoria", "CodMercadoria");
                string itemCodBarras = ObterColunaSql(tabelaItens, colItens, false,
                    "Código de Barras", "Codigo de Barras", "Cód Barra", "Cod Barra", "CodBarras", "CodBarras_Grade");
                string itemTam = ObterColunaSql(tabelaItens, colItens, false, "Tam", "Tamanho");
                string itemCores = ObterColunaSql(tabelaItens, colItens, false, "Cores", "Cor");
                string itemQuantidade = ObterColunaSql(tabelaItens, colItens, false,
                    "Quantidade", "Qtd", "Qtde", "Quantidade_Item");

                string prodCodigo = ObterColunaSql(tabelaProdutos, colProdutos, true,
                    "Código da Mercadoria", "Codigo da Mercadoria", "CodigoMercadoria");
                string prodLoja = ObterColunaSql(tabelaProdutos, colProdutos, false, "Loja");
                string prodDesativado = ObterColunaSql(tabelaProdutos, colProdutos, false, "Desativado");
                string prodCodFabricante = ObterColunaSql(tabelaProdutos, colProdutos, false,
                    "Cód Fabricante", "Cod Fabricante", "CodFabricante");
                string prodCodBarras = ObterColunaSql(tabelaProdutos, colProdutos, false,
                    "Cód Barra", "Cod Barra", "CodBarras", "Código de Barras", "Codigo de Barras");
                string prodMercadoria = ObterColunaSql(tabelaProdutos, colProdutos, true, "Mercadoria");
                string prodPrecoVenda = ObterColunaSql(tabelaProdutos, colProdutos, false,
                    "Preço de Venda", "Preco de Venda", "PrecoVenda");
                string prodVendaA = ObterColunaSql(tabelaProdutos, colProdutos, false, "VendaA");
                string prodVendaB = ObterColunaSql(tabelaProdutos, colProdutos, false, "VendaB");
                string prodVendaC = ObterColunaSql(tabelaProdutos, colProdutos, false, "VendaC");
                string prodVendaD = ObterColunaSql(tabelaProdutos, colProdutos, false, "VendaD");
                string prodVendaE = ObterColunaSql(tabelaProdutos, colProdutos, false, "VendaE");
                string prodFornecedor = ObterColunaSql(tabelaProdutos, colProdutos, false, "Fornecedor");
                string prodFabricante = ObterColunaSql(tabelaProdutos, colProdutos, false, "Fabricante");
                string prodGrupo = ObterColunaSql(tabelaProdutos, colProdutos, false, "Grupo");
                string prodSubGrupo = ObterColunaSql(tabelaProdutos, colProdutos, false, "SubGrupo", "Sub Grupo");
                string prodPrateleira = ObterColunaSql(tabelaProdutos, colProdutos, false, "Prateleira");
                string prodGarantia = ObterColunaSql(tabelaProdutos, colProdutos, false, "Garantia");
                string prodTam = ObterColunaSql(tabelaProdutos, colProdutos, false, "Tam", "Tamanho");
                string prodCores = ObterColunaSql(tabelaProdutos, colProdutos, false, "Cores", "Cor");
                string prodCodBarrasGrade = ObterColunaSql(tabelaProdutos, colProdutos, false,
                    "CodBarras", "CodBarras_Grade", "Código de Barras", "Codigo de Barras");

                List<string> condicoes = new List<string>();
                if (!string.IsNullOrWhiteSpace(loja))
                {
                    if (string.IsNullOrEmpty(lojaCabecalho))
                        throw new Exception($"A tabela {tabelaCabecalho.Nome} não possui coluna de loja para filtrar {descricao}.");

                    condicoes.Add("CAST(h." + DelimitarIdentificador(lojaCabecalho) + " AS NVARCHAR(50)) = @loja");
                }

                if (!string.IsNullOrWhiteSpace(numeroDocumento))
                {
                    condicoes.Add("CAST(h." + DelimitarIdentificador(docCabecalho) + " AS NVARCHAR(50)) = @numeroDocumento");
                }

                if (dataInicial.HasValue)
                {
                    condicoes.Add("CAST(h." + DelimitarIdentificador(dataCabecalho) + " AS DATE) >= @dataInicial");
                }

                if (dataFinal.HasValue)
                {
                    condicoes.Add("CAST(h." + DelimitarIdentificador(dataCabecalho) + " AS DATE) <= @dataFinal");
                }

                string whereMovimento = condicoes.Count > 0
                    ? "WHERE " + string.Join(" AND ", condicoes)
                    : "";

                string codigoMovimento = "CAST(s." + DelimitarIdentificador(itemCodigo) + " AS NVARCHAR(50))";
                string codBarrasMovimento = string.IsNullOrEmpty(itemCodBarras)
                    ? "CAST('' AS NVARCHAR(100))"
                    : "ISNULL(CAST(s." + DelimitarIdentificador(itemCodBarras) + " AS NVARCHAR(100)), '')";
                string tamMovimento = string.IsNullOrEmpty(itemTam)
                    ? "CAST('' AS NVARCHAR(50))"
                    : "ISNULL(CAST(s." + DelimitarIdentificador(itemTam) + " AS NVARCHAR(50)), '')";
                string coresMovimento = string.IsNullOrEmpty(itemCores)
                    ? "CAST('' AS NVARCHAR(50))"
                    : "ISNULL(CAST(s." + DelimitarIdentificador(itemCores) + " AS NVARCHAR(50)), '')";

                string quantidadeMovimento = "CAST(1 AS INT)";
                if (!string.IsNullOrEmpty(itemQuantidade))
                {
                    string colQtd = "s." + DelimitarIdentificador(itemQuantidade);
                    string somaQtd = "SUM(CASE WHEN " + colQtd + " IS NULL THEN 0 WHEN " + colQtd +
                                     " > 0 THEN CAST(" + colQtd + " AS INT) ELSE 0 END)";
                    quantidadeMovimento = "CASE WHEN " + somaQtd + " > 0 THEN " + somaQtd + " ELSE 1 END";
                }

                string produtoLojaFiltro = "";
                if (!string.IsNullOrWhiteSpace(loja) && !string.IsNullOrEmpty(prodLoja))
                {
                    produtoLojaFiltro = " AND CAST(p." + DelimitarIdentificador(prodLoja) + " AS NVARCHAR(50)) = @loja";
                }

                string produtoAtivoFiltro = "";
                if (!string.IsNullOrEmpty(prodDesativado))
                {
                    produtoAtivoFiltro = " AND ISNULL(p." + DelimitarIdentificador(prodDesativado) + ", 0) = 0";
                }

                string query = @"
                    WITH Movimento AS
                    (
                        SELECT
                            " + codigoMovimento + @" AS CodigoMercadoria,
                            " + codBarrasMovimento + @" AS CodBarras,
                            " + tamMovimento + @" AS Tam,
                            " + coresMovimento + @" AS Cores,
                            " + quantidadeMovimento + @" AS Quantidade
                        FROM " + tabelaCabecalho.NomeQualificado + @" h
                        INNER JOIN " + tabelaItens.NomeQualificado + @" s
                            ON h." + DelimitarIdentificador(docCabecalho) + @" = s." + DelimitarIdentificador(docItens) + @"
                        " + whereMovimento + @"
                        GROUP BY
                            " + codigoMovimento + @",
                            " + codBarrasMovimento + @",
                            " + tamMovimento + @",
                            " + coresMovimento + @"
                    )
                    SELECT
                        mv.CodigoMercadoria,
                        " + TextoSql("p", prodMercadoria) + @" AS Mercadoria,
                        " + DecimalSql("p", prodPrecoVenda) + @" AS PrecoVenda,
                        " + TextoSql("p", prodGrupo) + @" AS Grupo,
                        " + TextoSql("p", prodSubGrupo) + @" AS SubGrupo,
                        " + TextoSql("p", prodFabricante) + @" AS Fabricante,
                        " + TextoSql("p", prodFornecedor) + @" AS Fornecedor,
                        COALESCE(NULLIF(mv.CodBarras, ''), " + TextoSql("p", prodCodBarras) + @") AS CodBarras,
                        " + TextoSql("p", prodCodFabricante) + @" AS CodFabricante,
                        COALESCE(NULLIF(mv.Tam, ''), " + TextoSql("p", prodTam) + @") AS Tam,
                        COALESCE(NULLIF(mv.Cores, ''), " + TextoSql("p", prodCores) + @") AS Cores,
                        COALESCE(NULLIF(mv.CodBarras, ''), " + TextoSql("p", prodCodBarrasGrade) + @") AS CodBarras_Grade,
                        CAST(0 AS INT) AS Registro,
                        mv.Quantidade AS Quantidade,
                        " + DecimalSql("p", prodVendaA) + @" AS VendaA,
                        " + DecimalSql("p", prodVendaB) + @" AS VendaB,
                        " + DecimalSql("p", prodVendaC) + @" AS VendaC,
                        " + DecimalSql("p", prodVendaD) + @" AS VendaD,
                        " + DecimalSql("p", prodVendaE) + @" AS VendaE,
                        " + TextoSql("p", prodPrateleira) + @" AS Prateleira,
                        " + TextoSql("p", prodGarantia) + @" AS Garantia
                    FROM Movimento mv
                    INNER JOIN " + tabelaProdutos.NomeQualificado + @" p
                        ON CAST(p." + DelimitarIdentificador(prodCodigo) + @" AS NVARCHAR(50)) = mv.CodigoMercadoria
                        " + produtoLojaFiltro + @"
                        " + produtoAtivoFiltro + @"
                    ORDER BY Mercadoria";

                using (var cmd = new SqlCommand(query, conn))
                {
                    if (!string.IsNullOrWhiteSpace(loja))
                        cmd.Parameters.AddWithValue("@loja", loja);

                    if (!string.IsNullOrWhiteSpace(numeroDocumento))
                        cmd.Parameters.AddWithValue("@numeroDocumento", numeroDocumento.Trim());

                    if (dataInicial.HasValue)
                        cmd.Parameters.Add("@dataInicial", SqlDbType.Date).Value = dataInicial.Value.Date;

                    if (dataFinal.HasValue)
                        cmd.Parameters.Add("@dataFinal", SqlDbType.Date).Value = dataFinal.Value.Date;

                    using (var adapter = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = CriarTabelaResultadoPadrao();
                        dt.Clear();
                        adapter.Fill(dt);
                        return dt;
                    }
                }
            }
        }

        private static string ObterLojaConfigurada()
        {
            try
            {
                var config = DatabaseConfig.LoadConfiguration();
                return config?.Loja?.Trim() ?? "";
            }
            catch
            {
                return "";
            }
        }

        private static TabelaSqlInfo ObterTabelaSql(SqlConnection conn, bool obrigatoria, params string[] candidatos)
        {
            List<TabelaSqlInfo> tabelas = new List<TabelaSqlInfo>();

            using (var cmd = new SqlCommand(@"
                SELECT TABLE_SCHEMA, TABLE_NAME
                FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_TYPE IN ('BASE TABLE', 'VIEW')", conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    tabelas.Add(new TabelaSqlInfo
                    {
                        Schema = reader["TABLE_SCHEMA"].ToString(),
                        Nome = reader["TABLE_NAME"].ToString()
                    });
                }
            }

            foreach (string candidato in candidatos.Where(c => !string.IsNullOrWhiteSpace(c)))
            {
                TabelaSqlInfo tabela = tabelas.FirstOrDefault(t =>
                    string.Equals(t.Nome, candidato, StringComparison.OrdinalIgnoreCase));

                if (tabela != null)
                    return tabela;
            }

            if (obrigatoria)
                throw new Exception("Tabela SQL não encontrada: " + string.Join(", ", candidatos));

            return null;
        }

        private static Dictionary<string, string> ObterMapaColunasSql(SqlConnection conn, TabelaSqlInfo tabela)
        {
            Dictionary<string, string> colunas = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            using (var cmd = new SqlCommand(@"
                SELECT COLUMN_NAME
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_SCHEMA = @schema AND TABLE_NAME = @table", conn))
            {
                cmd.Parameters.AddWithValue("@schema", tabela.Schema);
                cmd.Parameters.AddWithValue("@table", tabela.Nome);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string nome = reader["COLUMN_NAME"].ToString();
                        if (!colunas.ContainsKey(nome))
                            colunas.Add(nome, nome);
                    }
                }
            }

            return colunas;
        }

        private static string ObterColunaSql(
            TabelaSqlInfo tabela,
            Dictionary<string, string> colunas,
            bool obrigatoria,
            params string[] candidatos)
        {
            foreach (string candidato in candidatos.Where(c => !string.IsNullOrWhiteSpace(c)))
            {
                string coluna;
                if (colunas.TryGetValue(candidato, out coluna))
                    return coluna;
            }

            if (obrigatoria)
                throw new Exception("Coluna SQL não encontrada em " + tabela.Nome + ": " + string.Join(", ", candidatos));

            return null;
        }

        private static string DelimitarIdentificador(string nome)
        {
            return "[" + (nome ?? "").Replace("]", "]]") + "]";
        }

        private static string TextoSql(string alias, string coluna, string padrao = "''")
        {
            if (string.IsNullOrEmpty(coluna))
                return padrao;

            return "ISNULL(CAST(" + alias + "." + DelimitarIdentificador(coluna) + " AS NVARCHAR(255)), " + padrao + ")";
        }

        private static string DecimalSql(string alias, string coluna)
        {
            if (string.IsNullOrEmpty(coluna))
                return "CAST(0 AS DECIMAL(18, 2))";

            return "ISNULL(" + alias + "." + DelimitarIdentificador(coluna) + ", 0)";
        }


        // ========================================
        // 🔹 NOTAS DE ENTRADA
        // ========================================
        /// <summary>
        /// Carrega produtos de notas fiscais de entrada
        /// Equivalente: GeradordeEtiquetas_CarregarCompras
        /// </summary>
        private static DataTable CarregarNotasEntrada(string numeroNF, DateTime? dataInicial, DateTime? dataFinal)
        {
            DataTable resultado = CriarTabelaResultadoPadrao();

            try
            {
                string connectionStringSQLServer = DatabaseConfig.GetConnectionString();
                if (string.IsNullOrEmpty(connectionStringSQLServer))
                    throw new Exception("Conexão SQL Server não configurada!");

                using (var connSQL = new System.Data.SqlClient.SqlConnection(connectionStringSQLServer))
                using (var connLocal = new SQLiteConnection(ConnectionString))
                {
                    connSQL.Open();
                    connLocal.Open();

                    // Normaliza número de nota
                    string numeroNotaParam = numeroNF?.Trim();

                    // BUSCAR ITENS DA NF
                    string queryNF = @"
                        SELECT 
                            [Código da Mercadoria] AS Codigo_Mercadoria,
                            CODBARRAS AS CodBarras,
                            Quantidade_Item
                        FROM memoria_NF_Entrada
                        WHERE [Nº Nota Fiscal] = @numeroNota";

                    using (var cmd = new System.Data.SqlClient.SqlCommand(queryNF, connSQL))
                    {
                        cmd.Parameters.AddWithValue("@numeroNota", numeroNotaParam);

                        using (var reader = cmd.ExecuteReader())
                        {
                            int linhasLidas = 0;
                            while (reader.Read())
                            {
                                linhasLidas++;
                                string codigoMercadoria = reader["Codigo_Mercadoria"]?.ToString()?.Trim() ?? "";
                                string codBarras = reader["CodBarras"]?.ToString()?.Trim() ?? "";
                                int qtd = ConverterQuantidadeEtiqueta(reader["Quantidade_Item"]);

                                // Log temporário para depuração
                                System.Diagnostics.Debug.WriteLine($"[CarregarNotasEntrada] NF={numeroNotaParam} Item #{linhasLidas}: Codigo='{codigoMercadoria}' CodBarras='{codBarras}' Qtd={qtd}");

                                bool encontrado = BuscarEAdicionarMercadoriaLocal(connLocal, resultado, codigoMercadoria, codBarras, qtd);

                                if (!encontrado)
                                {
                                    System.Diagnostics.Debug.WriteLine($"[CarregarNotasEntrada] NÃO encontrado localmente: Codigo='{codigoMercadoria}' CodBarras='{codBarras}'");
                                }
                            }

                            if (linhasLidas == 0)
                            {
                                System.Diagnostics.Debug.WriteLine($"[CarregarNotasEntrada] A consulta SQL Server não retornou itens para a nota '{numeroNotaParam}'. Verifique no banco remoto.");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao carregar NF {numeroNF}: {ex.Message}", ex);
            }

            return resultado;
        }

        // Método auxiliar para evitar repetição e erros de ambiguidade
        private static DataTable CarregarNotasEntradaSoftcomShop(string numeroNF, DateTime? dataEntrada)
        {
            DataTable resultado = CriarTabelaResultadoPadrao();

            if (!int.TryParse(numeroNF, out int numeroNota) || numeroNota <= 0)
            {
                throw new Exception("Numero da nota fiscal invalido para consulta no SoftcomShop.");
            }

            if (!dataEntrada.HasValue)
            {
                throw new Exception("Data de entrada obrigatoria para consulta da nota fiscal no SoftcomShop.");
            }

            try
            {
                var config = ConfiguracaoSistema.Carregar();
                if (config == null || config.TipoConexaoAtiva != TipoConexao.SoftcomShop || !config.SoftcomShopConfigurado())
                {
                    throw new Exception("SoftcomShop nao esta configurado como conexao ativa.");
                }

                var dataManager = new SoftcomShopDataManager(config.SoftcomShop, LocalDatabaseManager.GetConnectionString());

                // Evita deadlock na UI: executa a chamada async em uma Task separada em vez de bloquear o contexto atual.
                var syncResult = System.Threading.Tasks.Task.Run(() => dataManager.BuscarPorNotaFiscalAsync(dataEntrada.Value, numeroNota))
                                        .GetAwaiter()
                                        .GetResult();

                if (!syncResult.Sucesso)
                {
                    throw new Exception(syncResult.MensagemErro ?? "Nenhum produto encontrado para esta nota fiscal.");
                }

                using (var conn = new SQLiteConnection(ConnectionString))
                {
                    conn.Open();

                    // 1) Tentativa normal: buscar registros marcados para impressão
                    string query = @"
                        SELECT *
                        FROM Mercadorias
                        WHERE GerarEtiqueta = 1
                        ORDER BY Mercadoria";

                    using (var cmd = new SQLiteCommand(query, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                int quantidade = ConverterQuantidadeEtiqueta(reader["QuantidadeEtiqueta"]);

                                AdicionarRowCompleto(resultado, reader, Math.Max(1, quantidade));
                            }
                        }
                    }

                    // 2) Se nada encontrado com GerarEtiqueta=1, executar fallback não invasivo:
                    // Buscar produtos com Origem = 'SOFTCOMSHOP' (inseridos/atualizados pela rotina) e
                    // marcar alguns como GerarEtiqueta = 1 se a sincronização anterior inseriu registros sem marcar.
                    // Nao executar fallback por origem: ele pode marcar produtos que nao pertencem a nota.
                    if (resultado.Rows.Count > 0)
                    {
                        return resultado;
                    }

                    if (resultado.Rows.Count == 0)
                    {
                        throw new Exception("A nota foi consultada, mas nenhum produto ficou marcado para impressao no banco local.");
                    }

                    try
                    {
                        // Verifica se existem registros de origem SOFTCOMSHOP
                        string checkSql = "SELECT COUNT(*) FROM Mercadorias WHERE Origem = 'SOFTCOMSHOP'";
                        int totalSoft = 0;
                        using (var chkCmd = new SQLiteCommand(checkSql, conn))
                        {
                            object r = chkCmd.ExecuteScalar();
                            totalSoft = r != null && r != DBNull.Value ? Convert.ToInt32(r) : 0;
                        }

                        if (totalSoft > 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"[CarregarNotasEntradaSoftcomShop] Nenhuma linha com GerarEtiqueta=1. Encontrados {totalSoft} registros Origem='SOFTCOMSHOP'. Aplicando fallback de marcação.");

                            // Marca os N registros mais recentes (conservador) para impressão.
                            // Não modifica registros que já tenham GerarEtiqueta = 1 (já verificado).
                            string markSql = @"
                                UPDATE Mercadorias
                                SET GerarEtiqueta = 1, QuantidadeEtiqueta = 1
                                WHERE Origem = 'SOFTCOMSHOP'
                                AND (GerarEtiqueta IS NULL OR GerarEtiqueta = 0)
                                AND rowid IN (
                                    SELECT rowid FROM Mercadorias
                                    WHERE Origem = 'SOFTCOMSHOP'
                                    ORDER BY UltimaAtualizacao DESC
                                    LIMIT 200
                                )";
                            using (var markCmd = new SQLiteCommand(markSql, conn))
                            {
                                int updated = markCmd.ExecuteNonQuery();
                                System.Diagnostics.Debug.WriteLine($"[CarregarNotasEntradaSoftcomShop] Fallback: registros atualizados (GerarEtiqueta=1): {updated}");
                            }

                            // Ler novamente os marcados
                            using (var cmd2 = new SQLiteCommand(query, conn))
                            using (var reader2 = cmd2.ExecuteReader())
                            {
                                while (reader2.Read())
                                {
                                    int quantidade = ConverterQuantidadeEtiqueta(reader2["QuantidadeEtiqueta"]);

                                    AdicionarRowCompleto(resultado, reader2, Math.Max(1, quantidade));
                                }
                            }

                            // Se ainda assim não houve retorno, grava DEBUG para diagnóstico
                            if (resultado.Rows.Count == 0)
                            {
                                System.Diagnostics.Debug.WriteLine("[CarregarNotasEntradaSoftcomShop] Fallback executado, mas não foram retornadas linhas. Verificar banco local.");
                                try
                                {
                                    string filename = "SoftcomShop_Debug.log";
                                    string msg = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\tNota:{numeroNota}\tSyncResultProdutos:{syncResult.ProdutosAdicionados}\tTotalSoftRecords:{totalSoft}{Environment.NewLine}";

                                    var candidates = new[]
                                    {
                                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filename),
                                        Path.Combine(Path.GetTempPath(), filename),
                                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EtiquetaFornew", "logs", filename)
                                    };

                                    bool wrote = false;
                                    foreach (var path in candidates)
                                    {
                                        try
                                        {
                                            var dir = Path.GetDirectoryName(path);
                                            if (!Directory.Exists(dir))
                                                Directory.CreateDirectory(dir);

                                            File.AppendAllText(path, msg);
                                            System.Diagnostics.Debug.WriteLine($"[CarregarNotasEntradaSoftcomShop] Log escrito em: {path}");
                                            wrote = true;
                                            break;
                                        }
                                        catch (Exception exWrite)
                                        {
                                            System.Diagnostics.Debug.WriteLine($"[CarregarNotasEntradaSoftcomShop] não foi possível gravar em '{path}': {exWrite.Message}");
                                        }
                                    }

                                    if (!wrote)
                                    {
                                        // fallback para Debug
                                        System.Diagnostics.Debug.WriteLine("[CarregarNotasEntradaSoftcomShop] Falha ao gravar SoftcomShop_Debug.log em todos os locais possíveis.");
                                        System.Diagnostics.Debug.WriteLine(msg);
                                    }
                                }
                                catch { /* não bloquear o fluxo */ }
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("[CarregarNotasEntradaSoftcomShop] Não há registros com Origem='SOFTCOMSHOP' no banco local após sincronização.");
                        }
                    }
                    catch (Exception exFallback)
                    {
                        // Logar sem interromper fluxo
                        System.Diagnostics.Debug.WriteLine($"[CarregarNotasEntradaSoftcomShop] Erro no fallback: {exFallback.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao carregar NF SoftcomShop {numeroNF}: {ex.Message}", ex);
            }

            return resultado;
        }

        private static DataTable CarregarPrecosAlteradosSoftcomShop(DateTime dataInicial)
        {
            try
            {
                var config = ConfiguracaoSistema.Carregar();
                if (config == null || config.TipoConexaoAtiva != TipoConexao.SoftcomShop || !config.SoftcomShopConfigurado())
                {
                    throw new Exception("SoftcomShop nao esta configurado como conexao ativa.");
                }

                var dataManager = new SoftcomShopDataManager(config.SoftcomShop, LocalDatabaseManager.GetConnectionString());
                var syncResult = System.Threading.Tasks.Task.Run(() => dataManager.BuscarPrecosAlteradosAsync(dataInicial))
                                        .GetAwaiter()
                                        .GetResult();

                if (!syncResult.Sucesso)
                {
                    throw new Exception(syncResult.MensagemErro ?? "Nenhum produto encontrado com preco alterado.");
                }

                return CarregarMercadoriasMarcadasSoftcomShop("precos alterados");
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao carregar precos alterados SoftcomShop: {ex.Message}", ex);
            }
        }

        private static DataTable CarregarVendaSoftcomShop(string numeroVendaTexto)
        {
            if (!int.TryParse(numeroVendaTexto, out int numeroVenda) || numeroVenda <= 0)
            {
                throw new Exception("Numero da venda invalido para consulta no SoftcomShop.");
            }

            try
            {
                var config = ConfiguracaoSistema.Carregar();
                if (config == null || config.TipoConexaoAtiva != TipoConexao.SoftcomShop || !config.SoftcomShopConfigurado())
                {
                    throw new Exception("SoftcomShop nao esta configurado como conexao ativa.");
                }

                var dataManager = new SoftcomShopDataManager(config.SoftcomShop, LocalDatabaseManager.GetConnectionString());
                var syncResult = System.Threading.Tasks.Task.Run(() => dataManager.BuscarPorVendaAsync(numeroVenda))
                                        .GetAwaiter()
                                        .GetResult();

                if (!syncResult.Sucesso)
                {
                    throw new Exception(syncResult.MensagemErro ?? "Nenhum produto encontrado para esta venda.");
                }

                return CarregarMercadoriasMarcadasSoftcomShop("venda");
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao carregar venda SoftcomShop {numeroVendaTexto}: {ex.Message}", ex);
            }
        }

        private static DataTable CarregarMercadoriasMarcadasSoftcomShop(string descricao)
        {
            DataTable resultado = CriarTabelaResultadoPadrao();

            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();

                string query = @"
                    SELECT *
                    FROM Mercadorias
                    WHERE GerarEtiqueta = 1
                    ORDER BY Mercadoria";

                using (var cmd = new SQLiteCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int quantidade = ConverterQuantidadeEtiqueta(reader["QuantidadeEtiqueta"]);
                        AdicionarRowCompleto(resultado, reader, Math.Max(1, quantidade));
                    }
                }
            }

            if (resultado.Rows.Count == 0)
            {
                throw new Exception($"A consulta de {descricao} foi concluida, mas nenhum produto ficou marcado para impressao no banco local.");
            }

            return resultado;
        }

        private static bool EstaEmModoSoftcomShop()
        {
            try
            {
                var config = ConfiguracaoSistema.Carregar();
                return config != null &&
                       config.TipoConexaoAtiva == TipoConexao.SoftcomShop &&
                       config.SoftcomShopConfigurado();
            }
            catch
            {
                return false;
            }
        }

        private static void AdicionarRow(DataTable dt, SQLiteDataReader reader, string cbNF, int qtd, decimal preco)
        {
            DataRow row = dt.NewRow();
            row["CodigoMercadoria"] = reader["CodigoMercadoria"]?.ToString() ?? "";
            row["Mercadoria"] = reader["Mercadoria"]?.ToString() ?? "";
            row["Tam"] = reader["Tam"]?.ToString() ?? "PADRAO";
            row["Cores"] = reader["Cores"]?.ToString() ?? "PADRAO";
            row["CodBarras"] = !string.IsNullOrEmpty(cbNF) ? cbNF : (reader["CodBarras"]?.ToString() ?? "");
            row["Quantidade"] = qtd;
            row["PrecoVenda"] = preco;
            row["CodFabricante"] = reader["CodFabricante"]?.ToString() ?? "";
            row["Fabricante"] = reader["Fabricante"]?.ToString() ?? "";

            if (dt.Columns.Contains("CodBarras_Grade"))
                row["CodBarras_Grade"] = reader["CodBarras_Grade"]?.ToString() ?? "";

            dt.Rows.Add(row);
        }

        // Método NOVO para Notas de Entrada - pega TUDO do banco local
        private static void AdicionarRowCompleto(DataTable dt, SQLiteDataReader reader, int quantidade)
        {
            DataRow row = dt.NewRow();

            // Função helper
            T GetValue<T>(string columnName, T defaultValue = default(T))
            {
                try
                {
                    if (reader[columnName] != DBNull.Value)
                        return (T)Convert.ChangeType(reader[columnName], typeof(T));
                }
                catch { }
                return defaultValue;
            }

            row["CodigoMercadoria"] = GetValue<string>("CodigoMercadoria", "");
            row["Mercadoria"] = GetValue<string>("Mercadoria", "");
            row["PrecoVenda"] = GetValue<decimal>("PrecoVenda", 0m);
            row["VendaA"] = GetValue<decimal>("VendaA", 0m);
            row["VendaB"] = GetValue<decimal>("VendaB", 0m);
            row["VendaC"] = GetValue<decimal>("VendaC", 0m);
            row["VendaD"] = GetValue<decimal>("VendaD", 0m);
            row["VendaE"] = GetValue<decimal>("VendaE", 0m);
            row["Grupo"] = GetValue<string>("Grupo", "");
            row["SubGrupo"] = GetValue<string>("SubGrupo", "");
            row["Fabricante"] = GetValue<string>("Fabricante", "");
            row["Fornecedor"] = GetValue<string>("Fornecedor", "");
            row["CodBarras"] = GetValue<string>("CodBarras", "");
            row["CodFabricante"] = GetValue<string>("CodFabricante", "");
            row["Tam"] = GetValue<string>("Tam", "");
            row["Cores"] = GetValue<string>("Cores", "");
            row["CodBarras_Grade"] = GetValue<string>("CodBarras_Grade", "");
            row["Prateleira"] = GetValue<string>("Prateleira", "");
            row["Garantia"] = GetValue<string>("Garantia", "");
            row["Registro"] = GetValue<int>("Registro", 0);
            row["Quantidade"] = Math.Max(1, quantidade);

            dt.Rows.Add(row);
        }


        private static DataTable CriarTabelaResultadoPadrao()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("CodigoMercadoria", typeof(string));
            dt.Columns.Add("Mercadoria", typeof(string));
            dt.Columns.Add("PrecoVenda", typeof(decimal));
            dt.Columns.Add("Grupo", typeof(string));
            dt.Columns.Add("SubGrupo", typeof(string));
            dt.Columns.Add("Fabricante", typeof(string));
            dt.Columns.Add("Fornecedor", typeof(string));
            dt.Columns.Add("CodBarras", typeof(string));
            dt.Columns.Add("CodFabricante", typeof(string));
            dt.Columns.Add("Tam", typeof(string));
            dt.Columns.Add("Cores", typeof(string));
            dt.Columns.Add("CodBarras_Grade", typeof(string));
            dt.Columns.Add("Registro", typeof(int));
            dt.Columns.Add("Quantidade", typeof(int));
            dt.Columns.Add("VendaA", typeof(decimal));
            dt.Columns.Add("VendaB", typeof(decimal));
            dt.Columns.Add("VendaC", typeof(decimal));
            dt.Columns.Add("VendaD", typeof(decimal));
            dt.Columns.Add("VendaE", typeof(decimal));
            dt.Columns.Add("Prateleira", typeof(string));
            dt.Columns.Add("Garantia", typeof(string));
            return dt;
        }

        // ========================================
        // ÃƒÂ°Ã…Â¸Ã¢â‚¬ÂÃ‚Â¹ PREÃ‡OS ALTERADOS
        // ========================================
        /// <summary>
        /// Carrega produtos com preÃ§os alterados no perÃƒÆ’Ã‚Â­odo
        /// Equivalente: GeradordeEtiquetas_CarregarAlteracaoPrecos
        /// </summary>
        private static DataTable CarregarPrecosAlterados(DateTime dataInicial, DateTime dataFinal, bool usarQuantidadeEstoque = false)
        {
            try
            {
                string connectionStringSQLServer = DatabaseConfig.GetConnectionString();
                if (string.IsNullOrEmpty(connectionStringSQLServer))
                    throw new Exception("Conexão SQL Server não configurada!");

                string loja = ObterLojaConfigurada();

                using (var conn = new SqlConnection(connectionStringSQLServer))
                {
                    conn.Open();

                    TabelaSqlInfo tabelaMercadorias = ObterTabelaSql(conn, true, "Cadastro de Mercadorias", "Cadastro de mercadorias");
                    TabelaSqlInfo tabelaLojas = ObterTabelaSql(conn, true, "Cadastro de mercadoriasLojas", "Cadastro de MercadoriasLojas");

                    Dictionary<string, string> colMerc = ObterMapaColunasSql(conn, tabelaMercadorias);
                    Dictionary<string, string> colLojas = ObterMapaColunasSql(conn, tabelaLojas);

                    string cmCodigo = ObterColunaSql(tabelaMercadorias, colMerc, true,
                        "Código da Mercadoria", "Codigo da Mercadoria", "CodigoMercadoria");
                    string cmlCodigo = ObterColunaSql(tabelaLojas, colLojas, true,
                        "Código da Mercadoria", "Codigo da Mercadoria", "CodigoMercadoria");
                    string cmlLoja = ObterColunaSql(tabelaLojas, colLojas, !string.IsNullOrWhiteSpace(loja), "Loja");

                    string dataCml = ObterColunaSql(tabelaLojas, colLojas, false,
                        "DataAlteracaoPreco", "Data Alteracao Preco", "Data Alteração Preço",
                        "Data de Alteração de Preço", "Data da Alteração de Preço");
                    string dataCm = ObterColunaSql(tabelaMercadorias, colMerc, false,
                        "DataAlteracaoPreco", "Data Alteracao Preco", "Data Alteração Preço",
                        "Data de Alteração de Preço", "Data da Alteração de Preço");

                    if (string.IsNullOrEmpty(dataCml) && string.IsNullOrEmpty(dataCm))
                        throw new Exception("Coluna DataAlteracaoPreco não encontrada em Cadastro de Mercadorias ou Cadastro de mercadoriasLojas.");

                    string aliasData = string.IsNullOrEmpty(dataCml) ? "cm" : "cml";
                    string colunaData = string.IsNullOrEmpty(dataCml) ? dataCm : dataCml;

                    string cmCodFabricante = ObterColunaSql(tabelaMercadorias, colMerc, false,
                        "Cód Fabricante", "Cod Fabricante", "CodFabricante");
                    string cmCodBarras = ObterColunaSql(tabelaMercadorias, colMerc, false,
                        "Cód Barra", "Cod Barra", "CodBarras", "Código de Barras", "Codigo de Barras");
                    string cmMercadoria = ObterColunaSql(tabelaMercadorias, colMerc, true, "Mercadoria");
                    string cmPrecoVenda = ObterColunaSql(tabelaMercadorias, colMerc, false,
                        "Preço de Venda", "Preco de Venda", "PrecoVenda");
                    string cmlPrecoVenda = ObterColunaSql(tabelaLojas, colLojas, false,
                        "Preço de Venda", "Preco de Venda", "PrecoVenda");
                    string cmVendaA = ObterColunaSql(tabelaMercadorias, colMerc, false, "VendaA");
                    string cmVendaB = ObterColunaSql(tabelaMercadorias, colMerc, false, "VendaB");
                    string cmVendaC = ObterColunaSql(tabelaMercadorias, colMerc, false, "VendaC");
                    string cmVendaD = ObterColunaSql(tabelaMercadorias, colMerc, false, "VendaD");
                    string cmVendaE = ObterColunaSql(tabelaMercadorias, colMerc, false, "VendaE");
                    string cmlVendaA = ObterColunaSql(tabelaLojas, colLojas, false, "VendaA");
                    string cmlVendaB = ObterColunaSql(tabelaLojas, colLojas, false, "VendaB");
                    string cmlVendaC = ObterColunaSql(tabelaLojas, colLojas, false, "VendaC");
                    string cmlVendaD = ObterColunaSql(tabelaLojas, colLojas, false, "VendaD");
                    string cmlVendaE = ObterColunaSql(tabelaLojas, colLojas, false, "VendaE");
                    string cmFornecedor = ObterColunaSql(tabelaMercadorias, colMerc, false, "Fornecedor");
                    string cmFabricante = ObterColunaSql(tabelaMercadorias, colMerc, false, "Fabricante");
                    string cmGrupo = ObterColunaSql(tabelaMercadorias, colMerc, false, "Grupo");
                    string cmSubGrupo = ObterColunaSql(tabelaMercadorias, colMerc, false, "SubGrupo", "Sub Grupo");
                    string cmPrateleira = ObterColunaSql(tabelaMercadorias, colMerc, false, "Prateleira");
                    string cmGarantia = ObterColunaSql(tabelaMercadorias, colMerc, false, "Garantia");
                    string cmlTam = ObterColunaSql(tabelaLojas, colLojas, false, "Tam", "Tamanho");
                    string cmlCores = ObterColunaSql(tabelaLojas, colLojas, false, "Cores", "Cor");
                    string cmlCodBarras = ObterColunaSql(tabelaLojas, colLojas, false,
                        "CodBarras", "CodBarras_Grade", "Código de Barras", "Codigo de Barras");
                    string cmlEstoque = ObterColunaSql(tabelaLojas, colLojas, false,
                        "Estoque", "Saldo", "Saldo_Estoque", "EstoqueAtual", "Quantidade");
                    string cmlDesativado = ObterColunaSql(tabelaLojas, colLojas, false, "Desativado");
                    string cmDesativado = ObterColunaSql(tabelaMercadorias, colMerc, false, "Desativado");
                    string cmlAtivo = ObterColunaSql(tabelaLojas, colLojas, false, "Ativo");
                    string cmAtivo = ObterColunaSql(tabelaMercadorias, colMerc, false, "Ativo");

                    List<string> condicoes = new List<string>
                    {
                        "CAST(" + aliasData + "." + DelimitarIdentificador(colunaData) + " AS DATE) >= @dataInicial",
                        "CAST(" + aliasData + "." + DelimitarIdentificador(colunaData) + " AS DATE) <= @dataFinal"
                    };

                    if (!string.IsNullOrWhiteSpace(loja))
                        condicoes.Add("CAST(cml." + DelimitarIdentificador(cmlLoja) + " AS NVARCHAR(50)) = @loja");

                    if (!string.IsNullOrEmpty(cmlDesativado))
                        condicoes.Add("ISNULL(cml." + DelimitarIdentificador(cmlDesativado) + ", 0) = 0");
                    if (!string.IsNullOrEmpty(cmDesativado))
                        condicoes.Add("ISNULL(cm." + DelimitarIdentificador(cmDesativado) + ", 0) = 0");
                    if (!string.IsNullOrEmpty(cmlAtivo))
                        condicoes.Add("ISNULL(cml." + DelimitarIdentificador(cmlAtivo) + ", 1) = 1");
                    if (!string.IsNullOrEmpty(cmAtivo))
                        condicoes.Add("ISNULL(cm." + DelimitarIdentificador(cmAtivo) + ", 1) = 1");

                    string precoVenda = !string.IsNullOrEmpty(cmlPrecoVenda)
                        ? DecimalSql("cml", cmlPrecoVenda)
                        : DecimalSql("cm", cmPrecoVenda);
                    string vendaA = !string.IsNullOrEmpty(cmlVendaA) ? DecimalSql("cml", cmlVendaA) : DecimalSql("cm", cmVendaA);
                    string vendaB = !string.IsNullOrEmpty(cmlVendaB) ? DecimalSql("cml", cmlVendaB) : DecimalSql("cm", cmVendaB);
                    string vendaC = !string.IsNullOrEmpty(cmlVendaC) ? DecimalSql("cml", cmlVendaC) : DecimalSql("cm", cmVendaC);
                    string vendaD = !string.IsNullOrEmpty(cmlVendaD) ? DecimalSql("cml", cmlVendaD) : DecimalSql("cm", cmVendaD);
                    string vendaE = !string.IsNullOrEmpty(cmlVendaE) ? DecimalSql("cml", cmlVendaE) : DecimalSql("cm", cmVendaE);

                    string quantidade = "CAST(1 AS INT)";
                    if (usarQuantidadeEstoque && !string.IsNullOrEmpty(cmlEstoque))
                    {
                        string estoque = "cml." + DelimitarIdentificador(cmlEstoque);
                        quantidade = "CASE WHEN " + estoque + " IS NULL OR " + estoque +
                                     " <= 0 THEN 1 ELSE CAST(" + estoque + " AS INT) END";
                    }

                    string query = @"
                        SELECT DISTINCT
                            CAST(cm." + DelimitarIdentificador(cmCodigo) + @" AS NVARCHAR(50)) AS CodigoMercadoria,
                            " + TextoSql("cm", cmMercadoria) + @" AS Mercadoria,
                            " + precoVenda + @" AS PrecoVenda,
                            " + TextoSql("cm", cmGrupo) + @" AS Grupo,
                            " + TextoSql("cm", cmSubGrupo) + @" AS SubGrupo,
                            " + TextoSql("cm", cmFabricante) + @" AS Fabricante,
                            " + TextoSql("cm", cmFornecedor) + @" AS Fornecedor,
                            " + TextoSql("cm", cmCodBarras) + @" AS CodBarras,
                            " + TextoSql("cm", cmCodFabricante) + @" AS CodFabricante,
                            " + TextoSql("cml", cmlTam) + @" AS Tam,
                            " + TextoSql("cml", cmlCores) + @" AS Cores,
                            " + TextoSql("cml", cmlCodBarras) + @" AS CodBarras_Grade,
                            CAST(0 AS INT) AS Registro,
                            " + quantidade + @" AS Quantidade,
                            " + vendaA + @" AS VendaA,
                            " + vendaB + @" AS VendaB,
                            " + vendaC + @" AS VendaC,
                            " + vendaD + @" AS VendaD,
                            " + vendaE + @" AS VendaE,
                            " + TextoSql("cm", cmPrateleira) + @" AS Prateleira,
                            " + TextoSql("cm", cmGarantia) + @" AS Garantia
                        FROM " + tabelaMercadorias.NomeQualificado + @" cm
                        INNER JOIN " + tabelaLojas.NomeQualificado + @" cml
                            ON cm." + DelimitarIdentificador(cmCodigo) + @" = cml." + DelimitarIdentificador(cmlCodigo) + @"
                        WHERE " + string.Join(" AND ", condicoes) + @"
                        ORDER BY Mercadoria";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.Add("@dataInicial", SqlDbType.Date).Value = dataInicial.Date;
                        cmd.Parameters.Add("@dataFinal", SqlDbType.Date).Value = dataFinal.Date;

                        if (!string.IsNullOrWhiteSpace(loja))
                            cmd.Parameters.AddWithValue("@loja", loja);

                        using (var adapter = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = CriarTabelaResultadoPadrao();
                            dt.Clear();
                            adapter.Fill(dt);
                            return dt;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao carregar preÃ§os alterados: {ex.Message}", ex);
            }
        }

        // ========================================
        // ÃƒÂ°Ã…Â¸Ã¢â‚¬ÂÃ‚Â¹ PROMOÃ‡Ã•ES
        // ========================================
        /// <summary>
        /// Carrega produtos em promoÃ§ÃƒÆ’Ã‚Â£o com filtros especÃƒÆ’Ã‚Â­ficos
        /// Equivalente: Promocoes_GeradorEtiquetasAnexar
        /// </summary>
        private static DataTable CarregarPromocoes(
            string grupo = null,
            string subGrupo = null,
            string fabricante = null,
            string fornecedor = null,
            string produto = null)
        {
            try
            {
                using (var conn = new SQLiteConnection(ConnectionString))
                {
                    conn.Open();

                    string query = @"
                        SELECT DISTINCT
                            m.CodigoMercadoria,
                            m.Mercadoria,
                            m.PrecoVenda,
                            m.Grupo,
                            m.SubGrupo,
                            m.Fabricante,
                            m.Fornecedor,
                            m.CodBarras,
                            m.CodFabricante,
                            m.Tam,
                            m.Cores,
                            m.CodBarras_Grade,
                            m.Registro,
                            1 as Quantidade
                        FROM Mercadorias m
                        WHERE 1=1
                    ";

                    // TODO: Quando houver tabela de promoÃ§ÃƒÆ’Ã‚Âµes:
                    // query += " INNER JOIN Promocoes p ON m.CodigoMercadoria = p.CodigoMercadoria";
                    // query += " WHERE p.Ativa = 1";

                    List<string> condicoes = new List<string>();
                    var parametros = new List<SQLiteParameter>();

                    if (!string.IsNullOrEmpty(grupo))
                    {
                        condicoes.Add("m.Grupo = @grupo");
                        parametros.Add(new SQLiteParameter("@grupo", grupo));
                    }

                    if (!string.IsNullOrEmpty(subGrupo))
                    {
                        condicoes.Add("m.SubGrupo = @subGrupo");
                        parametros.Add(new SQLiteParameter("@subGrupo", subGrupo));
                    }

                    if (!string.IsNullOrEmpty(fabricante))
                    {
                        condicoes.Add("m.Fabricante = @fabricante");
                        parametros.Add(new SQLiteParameter("@fabricante", fabricante));
                    }

                    if (!string.IsNullOrEmpty(fornecedor))
                    {
                        condicoes.Add("m.Fornecedor = @fornecedor");
                        parametros.Add(new SQLiteParameter("@fornecedor", fornecedor));
                    }

                    if (!string.IsNullOrEmpty(produto))
                    {
                        condicoes.Add("m.Mercadoria LIKE @produto");
                        parametros.Add(new SQLiteParameter("@produto", $"%{produto}%"));
                    }

                    if (condicoes.Count > 0)
                    {
                        query += " AND " + string.Join(" AND ", condicoes);
                    }

                    query += " ORDER BY m.Mercadoria";

                    using (var cmd = new SQLiteCommand(query, conn))
                    {
                        foreach (var param in parametros)
                        {
                            cmd.Parameters.Add(param);
                        }

                        using (var adapter = new SQLiteDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            adapter.Fill(dt);
                            return dt;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao carregar promoÃ§ÃƒÆ’Ã‚Âµes: {ex.Message}", ex);
            }
        }

        // ========================================
        // ÃƒÂ°Ã…Â¸Ã¢â‚¬ÂÃ‚Â¹ LIMPAR ETIQUETAS EXISTENTES
        // ========================================
        /// <summary>
        /// Limpa produtos jÃƒÆ’Ã‚Â¡ carregados (equivalente ao DELETE no SoftShop)
        /// </summary>
        public static bool LimparEtiquetasCarregadas()
        {
            // Esta funcionalidade pode ser implementada se houver
            // uma "ÃƒÆ’Ã‚Â¡rea de staging" para produtos carregados
            // Por enquanto, apenas retorna true
            return true;
        }

        // ========================================
        // Ã°Å¸â€Â¹ MÃƒâ€°TODOS AUXILIARES - LEITURA SEGURA
        // ========================================

        /// <summary>
        /// LÃƒÂª campo string do reader verificando se existe
        /// </summary>
        private static object LerCampoSeguro(SQLiteDataReader reader, string nomeCampo)
        {
            try
            {
                int ordinal = reader.GetOrdinal(nomeCampo);
                if (reader.IsDBNull(ordinal))
                    return DBNull.Value;
                return reader.GetValue(ordinal);
            }
            catch
            {
                return DBNull.Value;
            }
        }

        /// <summary>
        /// LÃƒÂª campo decimal do reader verificando se existe
        /// </summary>
        private static object LerCampoDecimal(SQLiteDataReader reader, string nomeCampo)
        {
            try
            {
                int ordinal = reader.GetOrdinal(nomeCampo);
                if (reader.IsDBNull(ordinal))
                    return DBNull.Value;
                return reader.GetDecimal(ordinal);
            }
            catch
            {
                return DBNull.Value;
            }
        }

        /// <summary>
        /// LÃƒÂª campo int do reader verificando se existe
        /// </summary>
        private static object LerCampoInt(SQLiteDataReader reader, string nomeCampo)
        {
            try
            {
                int ordinal = reader.GetOrdinal(nomeCampo);
                if (reader.IsDBNull(ordinal))
                    return DBNull.Value;
                return reader.GetInt32(ordinal);
            }
            catch
            {
                return DBNull.Value;
            }
        }

        private static int ConverterQuantidadeEtiqueta(object valor, int padrao = 1)
        {
            try
            {
                if (valor == null || valor == DBNull.Value)
                    return padrao;

                int quantidade = Convert.ToInt32(valor);
                return quantidade > 0 ? quantidade : padrao;
            }
            catch
            {
                return padrao;
            }
        }

        private static DataTable CarregarPromocoesSoftcomShopLocal(string grupo, string fabricante, string produto)
        {
            DataTable dt = CriarTabelaResultadoPadrao();
            try
            {
                using (var conn = new SQLiteConnection(ConnectionString))
                {
                    conn.Open();
                    string sql = "SELECT * FROM Mercadorias WHERE EmPromocao = 1";
                    if (!string.IsNullOrEmpty(grupo)) sql += " AND Grupo = @g";
                    if (!string.IsNullOrEmpty(fabricante)) sql += " AND Fabricante = @f";
                    if (!string.IsNullOrEmpty(produto)) sql += " AND Mercadoria LIKE @p";

                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        if (!string.IsNullOrEmpty(grupo)) cmd.Parameters.AddWithValue("@g", grupo);
                        if (!string.IsNullOrEmpty(fabricante)) cmd.Parameters.AddWithValue("@f", fabricante);
                        if (!string.IsNullOrEmpty(produto)) cmd.Parameters.AddWithValue("@p", $"%{produto}%");

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                AdicionarRowCompleto(dt, reader, 1);

                                // Ajuste do preço promocional
                                if (reader["PrecoPromocional"] != DBNull.Value)
                                {
                                    decimal pPromo = Convert.ToDecimal(reader["PrecoPromocional"]);
                                    if (pPromo > 0) dt.Rows[dt.Rows.Count - 1]["PrecoVenda"] = pPromo;
                                }
                            }
                        }
                    }
                }
            }
            catch { }
            return dt;
        }

        private static string LimparFiltro(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return null;

            valor = valor.Trim();

            if (valor.Equals("TODOS", StringComparison.OrdinalIgnoreCase))
                return null;

            if (valor.Equals("SELECIONE", StringComparison.OrdinalIgnoreCase))
                return null;

            if (valor.Contains("Selecione"))
                return null;

            return valor;
        }

        // Novo helper para procurar localmente usando várias estratégias
        private static bool BuscarEAdicionarMercadoriaLocal(SQLiteConnection connLocal, DataTable resultado, string codigoMercadoria, string codBarras, int qtd)
        {
            if (connLocal == null) return false;

            // Normalizações auxiliares
            string codBarrasDigits = string.IsNullOrEmpty(codBarras) ? null : Regex.Replace(codBarras, @"\D", "");
            string codigoSemZeros = string.IsNullOrEmpty(codigoMercadoria) ? null : codigoMercadoria.TrimStart('0');
            string codigoTrim = string.IsNullOrEmpty(codigoMercadoria) ? null : codigoMercadoria.Trim();

            var tentativas = new List<(string sql, string paramValue)>();

            // Estratégias originais (mantidas)
            tentativas.Add(("SELECT * FROM Mercadorias WHERE CodBarras_Grade = @cod LIMIT 1", codBarras));
            tentativas.Add(("SELECT * FROM Mercadorias WHERE CodBarras = @cod LIMIT 1", codBarras));
            tentativas.Add(("SELECT * FROM Mercadorias WHERE CodigoMercadoria = @cod LIMIT 1", codigoMercadoria));
            tentativas.Add(("SELECT * FROM Mercadorias WHERE CodigoMercadoria LIKE @cod LIMIT 1", string.IsNullOrEmpty(codigoMercadoria) ? null : codigoMercadoria + "%"));

            // Novas estratégias não invasivas
            if (!string.IsNullOrEmpty(codBarrasDigits) && codBarrasDigits != codBarras)
            {
                tentativas.Add(("SELECT * FROM Mercadorias WHERE CodBarras_Grade = @cod LIMIT 1", codBarrasDigits));
                tentativas.Add(("SELECT * FROM Mercadorias WHERE CodBarras = @cod LIMIT 1", codBarrasDigits));
                tentativas.Add(("SELECT * FROM Mercadorias WHERE CodBarras LIKE @cod LIMIT 1", "%" + codBarrasDigits + "%"));
            }

            if (!string.IsNullOrEmpty(codigoSemZeros) && codigoSemZeros != codigoTrim)
            {
                tentativas.Add(("SELECT * FROM Mercadorias WHERE CodigoMercadoria = @cod LIMIT 1", codigoSemZeros));
                tentativas.Add(("SELECT * FROM Mercadorias WHERE CodigoMercadoria LIKE @cod LIMIT 1", codigoSemZeros + "%"));
            }

            // Tentar por CodFabricante (às vezes o campo do fornecedor/sku pode ser usado)
            if (!string.IsNullOrEmpty(codBarras))
            {
                tentativas.Add(("SELECT * FROM Mercadorias WHERE CodFabricante = @cod LIMIT 1", codBarras));
            }
            if (!string.IsNullOrEmpty(codigoMercadoria))
            {
                tentativas.Add(("SELECT * FROM Mercadorias WHERE CodFabricante = @cod LIMIT 1", codigoMercadoria));
            }

            // Executa tentativas na ordem definida
            foreach (var t in tentativas)
            {
                if (string.IsNullOrEmpty(t.paramValue)) continue;

                try
                {
                    using (var cmdLocal = new SQLiteCommand(t.sql, connLocal))
                    {
                        cmdLocal.Parameters.AddWithValue("@cod", t.paramValue);
                        using (var readerLocal = cmdLocal.ExecuteReader())
                        {
                            if (readerLocal.Read())
                            {
                                // Adiciona a primeira correspondência encontrada
                                AdicionarRowCompleto(resultado, readerLocal, Math.Max(1, qtd));
                                System.Diagnostics.Debug.WriteLine($"[BuscarEAdicionarMercadoriaLocal] Encontrado via query: {t.sql} param='{t.paramValue}'");
                                return true;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Não propaga - apenas loga debug para não quebrar fluxo
                    System.Diagnostics.Debug.WriteLine($"[BuscarEAdicionarMercadoriaLocal] erro na query '{t.sql}' com param '{t.paramValue}': {ex.Message}");
                }
            }

            // Não encontrado: grava log de diagnóstico para análise posterior
            try
            {
                string filename = "NotasNaoEncontradas.log";
                string content = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\tCodigoMercadoria='{codigoMercadoria}'\tCodBarras='{codBarras}'\tQtd={qtd}{Environment.NewLine}";

                // Locais candidatos onde tentaremos gravar (ordem: pasta da aplicação, temp, AppData\EtiquetaFornew\logs)
                var candidates = new[]
                {
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filename),
                    Path.Combine(Path.GetTempPath(), filename),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EtiquetaFornew", "logs", filename)
                };

                bool wrote = false;
                foreach (var path in candidates)
                {
                    try
                    {
                        var dir = Path.GetDirectoryName(path);
                        if (!Directory.Exists(dir))
                            Directory.CreateDirectory(dir);

                        File.AppendAllText(path, content);
                        System.Diagnostics.Debug.WriteLine($"[BuscarEAdicionarMercadoriaLocal] Log escrito em: {path}");
                        wrote = true;
                        break;
                    }
                    catch (Exception exWrite)
                    {
                        System.Diagnostics.Debug.WriteLine($"[BuscarEAdicionarMercadoriaLocal] não foi possível gravar em '{path}': {exWrite.Message}");
                        // tenta próximo
                    }
                }

                if (!wrote)
                {
                    // fallback: escrever apenas no Debug (não falhar)
                    System.Diagnostics.Debug.WriteLine("[BuscarEAdicionarMercadoriaLocal] Falha ao gravar NotasNaoEncontradas.log em todos os locais possíveis.");
                    System.Diagnostics.Debug.WriteLine(content);
                }
            }
            catch { /* não falhar se não puder escrever */ }

            // Não encontrado
            return false;
        }

        // ⭐ MÉTODO DE DIAGNÓSTICO TEMPORÁRIO - Adicione no início da classe CarregadorDados
        public static void DiagnosticarNotaFiscalSoftcomShop(int numeroNota)
        {
            System.Diagnostics.Debug.WriteLine($"\n\n===== DIAGNÓSTICO NOTA FISCAL SOFTCOMSHOP #{numeroNota} =====");

            try
            {
                using (var conn = new SQLiteConnection(ConnectionString))
                {
                    conn.Open();

                    // 1. Total de registros SOFTCOMSHOP
                    using (var cmd = new SQLiteCommand("SELECT COUNT(*) FROM Mercadorias WHERE Origem = 'SOFTCOMSHOP'", conn))
                    {
                        int total = (int)(long)cmd.ExecuteScalar();
                        System.Diagnostics.Debug.WriteLine($"1️⃣ Total registros Origem='SOFTCOMSHOP': {total}");
                    }

                    // 2. Total com GerarEtiqueta=1
                    using (var cmd = new SQLiteCommand("SELECT COUNT(*) FROM Mercadorias WHERE GerarEtiqueta = 1", conn))
                    {
                        int total = (int)(long)cmd.ExecuteScalar();
                        System.Diagnostics.Debug.WriteLine($"2️⃣ Total com GerarEtiqueta=1: {total}");
                    }

                    // 3. Últimas 5 linhas inseridas (SOFTCOMSHOP)
                    using (var cmd = new SQLiteCommand(@"
                        SELECT CodigoMercadoria, Mercadoria, Origem, GerarEtiqueta, QuantidadeEtiqueta, UltimaAtualizacao 
                        FROM Mercadorias 
                        WHERE Origem = 'SOFTCOMSHOP' 
                        ORDER BY UltimaAtualizacao DESC 
                        LIMIT 5", conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        System.Diagnostics.Debug.WriteLine($"3️⃣ Últimas 5 linhas SOFTCOMSHOP:");
                        int row = 0;
                        while (reader.Read())
                        {
                            row++;
                            string cod = reader["CodigoMercadoria"]?.ToString() ?? "NULL";
                            string merc = reader["Mercadoria"]?.ToString() ?? "NULL";
                            string gerar = reader["GerarEtiqueta"]?.ToString() ?? "NULL";
                            string qtd = reader["QuantidadeEtiqueta"]?.ToString() ?? "NULL";
                            string data = reader["UltimaAtualizacao"]?.ToString() ?? "NULL";
                            System.Diagnostics.Debug.WriteLine($"   [{row}] Cod={cod}, Merc={merc}, GerarEtiqueta={gerar}, Qtd={qtd}, Data={data}");
                        }
                        if (row == 0) System.Diagnostics.Debug.WriteLine("   (NENHUM REGISTRO)");
                    }

                    // 4. Estrutura da tabela (verificar se colunas existem)
                    using (var cmd = new SQLiteCommand("PRAGMA table_info(Mercadorias)", conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        System.Diagnostics.Debug.WriteLine($"4️⃣ Colunas na tabela Mercadorias:");
                        while (reader.Read())
                        {
                            string colName = reader["name"]?.ToString();
                            string colType = reader["type"]?.ToString();
                            System.Diagnostics.Debug.WriteLine($"   - {colName} ({colType})");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erro no diagnóstico: {ex.Message}\n{ex.StackTrace}");
            }

            System.Diagnostics.Debug.WriteLine($"===== FIM DIAGNÓSTICO =====\n\n");
        }
    }
}
