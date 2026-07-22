using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using System.Net;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EtiquetaFORNew
{
    /// <summary>
    /// Gerenciador de sincronização entre SoftcomShop API e SQLite local
    /// </summary>
    public class SoftcomShopDataManager
    {
        private readonly SoftcomShopService _service;
        private readonly SoftcomShopConfig _config;
        private readonly string _connectionString;
        private readonly SoftcomShopService _softcomShopService;

        public SoftcomShopDataManager(SoftcomShopConfig config, string sqliteConnectionString)
        {
            _config = config;
            _service = new SoftcomShopService(config);
            _connectionString = sqliteConnectionString;
            _softcomShopService = new SoftcomShopService(config);
        }

        #region Sincronização de Produtos

        /// <summary>
        /// Sincroniza todos os produtos do catálogo
        /// </summary>
        public async Task<SyncResult> SincronizarProdutosAsync(string versao = "v2", IProgress<string> progress = null)
        {
            var result = new SyncResult();
            int paginaAtual = 1;
            bool temMaisPaginas = true;

            try
            {
                progress?.Report("Iniciando sincronização de produtos...");

                // Limpar tabelas na primeira página
                if (paginaAtual == 1)
                {
                    LimparTabelasProdutos();
                }

                while (temMaisPaginas)
                {
                    progress?.Report($"Sincronizando página {paginaAtual}...");

                    string jsonResponse = await _service.GetProdutosAsync(paginaAtual, versao);
                    var response = JObject.Parse(jsonResponse);

                    // Verificar se há produtos
                    var produtos = response["data"] as JArray;
                    if (produtos == null || produtos.Count == 0)
                    {
                        temMaisPaginas = false;
                        continue;
                    }

                    // Processar produtos
                    result.ProdutosAdicionados += ProcessarProdutos(produtos, versao);

                    // Atualizar timestamp
                    if (response["date_sync"] != null)
                    {
                        AtualizarTimestamp(response["date_sync"].ToString());
                    }

                    // Verificar se tem mais páginas
                    if (versao == "v2")
                    {
                        int totalPaginas = response["meta"]["last_page"].ToObject<int>();
                        temMaisPaginas = paginaAtual < totalPaginas;
                    }
                    else
                    {
                        int totalPaginas = response["meta"]["page"]["count"].ToObject<int>();
                        temMaisPaginas = paginaAtual < totalPaginas;
                    }

                    paginaAtual++;
                }

                progress?.Report("Sincronização concluída!");
                result.Sucesso = true;
            }
            catch (Exception ex)
            {
                result.Sucesso = false;
                result.MensagemErro = ex.Message;
                progress?.Report($"Erro: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Processa e insere produtos no banco local
        /// </summary>
        private int ProcessarProdutos(JArray produtos, string versao)
        {
            int count = 0;

            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();

                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        foreach (var produto in produtos)
                        {
                            InserirProduto(conn, produto, versao);
                            
                            // Processar tabela de preços (se houver)
                            if (produto["tabela_precos"] != null)
                            {
                                ProcessarTabelaPrecos(conn, produto);
                            }

                            // Processar atributos (TAM/COR) se versão v2
                            if (versao == "v2" && produto["sku_atributo"] != null)
                            {
                                ProcessarAtributos(conn, produto);
                            }

                            count++;
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }

            return count;
        }

        /// <summary>
        /// Insere um produto no banco local
        /// </summary>
        // ================================================================================
        // CORREÇÃO - SoftcomShopDataManager.cs
        // SUBSTITUIR MÉTODO InserirProduto (LINHA ~148)
        // ================================================================================
        // PROBLEMA: Estava preenchendo "Referencia" mas ComboBox usa "CodFabricante"
        // SOLUÇÃO: Preencher AMBOS os campos
        // ================================================================================

        private void InserirProduto(SQLiteConnection conn, JToken produto, string versao)
        {
            var cmd = new SQLiteCommand(@"
        INSERT INTO Mercadorias (
            ID_SoftcomShop, CodigoMercadoria, CodFabricante, CodBarras, CodBarras_Grade, 
            Mercadoria, PrecoVenda, Fabricante, Grupo, Observacao, 
            UltimaAtualizacao, Ativo, Tam, Cores, Origem,
            GerarEtiqueta, QuantidadeEtiqueta
        ) VALUES (
            @id, @codMerc, @codFabricante, @codBarras, @codBarrasGrade, 
            @mercadoria, @preco, @fabricante, @grupo, @observacao,
            @dataAtualizacao, @ativo, @tam, @cor, 'SOFTCOMSHOP',
            0, 1
        )", conn);

            long produtoId = LerLong(produto["produto_id"]);
            string codigoMercadoria = ObterCodigoMercadoria(produto, produtoId);

            cmd.Parameters.AddWithValue("@id", produtoId);
            cmd.Parameters.AddWithValue("@codMerc", codigoMercadoria);

            // Referência
            string referencia = produto["referencia"]?.ToString() ?? "";
            if (string.IsNullOrWhiteSpace(referencia))
            {
                referencia = codigoMercadoria;
            }
            cmd.Parameters.AddWithValue("@codFabricante", referencia);

            cmd.Parameters.AddWithValue("@codBarras", produto["codigo_barras"]?.ToString() ?? "");
            cmd.Parameters.AddWithValue("@codBarrasGrade", produto["codigo_barras_grade"]?.ToString() ?? "");

            string nomeProduto = ObterNomeMercadoria(produto, referencia, codigoMercadoria);

            cmd.Parameters.AddWithValue("@mercadoria", nomeProduto);

            // Preço
            decimal preco = LerDecimal(produto["preco_venda"], 0m);
            cmd.Parameters.AddWithValue("@preco", preco);

            cmd.Parameters.AddWithValue("@fabricante", ObterFabricanteProduto(produto));
            cmd.Parameters.AddWithValue("@grupo", ObterGrupoProduto(produto));
            cmd.Parameters.AddWithValue("@observacao", ObterObservacaoProduto(produto));
            cmd.Parameters.AddWithValue("@dataAtualizacao", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("@ativo", 1);

            cmd.Parameters.AddWithValue("@tam", "");
            cmd.Parameters.AddWithValue("@cor", "");

            cmd.ExecuteNonQuery();
        }

        private string ObterNomeMercadoria(JToken produto, string referencia, string codigoMercadoria)
        {
            string nomeProduto = PrimeiroTexto(
                produto?["produto_nome"],
                produto?["descricao"],
                produto?["nome"],
                produto?["produto_descricao"]);

            if (string.IsNullOrWhiteSpace(nomeProduto))
            {
                nomeProduto = !string.IsNullOrWhiteSpace(referencia)
                    ? referencia
                    : $"Produto {codigoMercadoria}";
            }

            return nomeProduto;
        }

        /// <summary>
        /// Processa tabelas de preço A, B, C, D, E
        /// </summary>
        //private void ProcessarTabelaPrecos(SQLiteConnection conn, JToken produto)
        //{
        //    long produtoId = produto["produto_id"].ToObject<long>();
        //    string codBarrasGrade = produto["codigo_barras_grade"]?.ToString() ?? "";
        //    var tabelaPrecos = produto["tabela_precos"] as JArray;

        //    if (tabelaPrecos == null) return;

        //    foreach (var preco in tabelaPrecos)
        //    {
        //        string tipo = preco["descricao"]?.ToString() ?? "";
        //        decimal valor = decimal.TryParse(preco["preco"]?.ToString().Replace(".", ","), out decimal v) ? v : 0;

        //        string campo = null;
        //        switch (tipo)
        //        {
        //            case "A":
        //                campo = "VendaA";
        //                break;
        //            case "B":
        //                campo = "VendaB";
        //                break;
        //            case "C":
        //                campo = "VendaC";
        //                break;
        //            case "D":
        //                campo = "VendaD";
        //                break;
        //            case "E":
        //                campo = "VendaE";
        //                break;
        //        }

        //        if (campo != null)
        //        {
        //            var cmd = new SQLiteCommand($@"
        //                UPDATE Mercadorias 
        //                SET {campo} = @valor
        //                WHERE ID_SoftcomShop = @id 
        //                {(string.IsNullOrEmpty(codBarrasGrade) ? "" : "OR CodBarras_Grade = @codBarrasGrade")}
        //            ", conn);

        //            cmd.Parameters.AddWithValue("@valor", valor);
        //            cmd.Parameters.AddWithValue("@id", produtoId);
        //            if (!string.IsNullOrEmpty(codBarrasGrade))
        //                cmd.Parameters.AddWithValue("@codBarrasGrade", codBarrasGrade);

        //            cmd.ExecuteNonQuery();
        //        }
        //    }
        //}
        private void ProcessarTabelaPrecos(SQLiteConnection conn, JToken produto)
        {
            long produtoId = produto["produto_id"].ToObject<long>();
            // IMPORTANTE: Se o produto não tem grade, não devemos tentar filtrar por ela no OR
            string codBarrasGrade = produto["codigo_barras_grade"]?.ToString();
            var tabelaPrecos = produto["tabela_precos"] as JArray;

            if (tabelaPrecos == null) return;

            foreach (var preco in tabelaPrecos)
            {
                string tipo = preco["descricao"]?.ToString() ?? "";
                decimal valor = decimal.TryParse(preco["preco"]?.ToString().Replace(".", ","), out decimal v) ? v : 0;

                string campo = null;
                switch (tipo)
                {
                    case "A": campo = "VendaA"; break;
                    case "B": campo = "VendaB"; break;
                    case "C": campo = "VendaC"; break;
                    case "D": campo = "VendaD"; break;
                    case "E": campo = "VendaE"; break;
                }

                if (campo != null)
                {
                    // Melhorei a lógica do WHERE para ser mais restritiva
                    string sql = $@"UPDATE Mercadorias SET {campo} = @valor WHERE ID_SoftcomShop = @id";

                    // Só adiciona o filtro de grade se ele realmente existir e não for vazio
                    if (!string.IsNullOrWhiteSpace(codBarrasGrade))
                    {
                        sql += " AND CodBarras_Grade = @codBarrasGrade";
                    }

                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@valor", valor);
                        cmd.Parameters.AddWithValue("@id", produtoId);

                        if (!string.IsNullOrWhiteSpace(codBarrasGrade))
                            cmd.Parameters.AddWithValue("@codBarrasGrade", codBarrasGrade);

                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }
        /// <summary>
        /// Processa atributos de grade (TAM/COR)
        /// </summary>
        //private void ProcessarAtributos(SQLiteConnection conn, JToken produto)
        //{
        //    long produtoId = produto["produto_id"].ToObject<long>();
        //    string codBarrasGrade = produto["codigo_barras_grade"]?.ToString() ?? "";
        //    var atributos = produto["sku_atributo"] as JArray;

        //    if (atributos == null) return;

        //    string tam = "";
        //    string cor = "";

        //    foreach (var atributo in atributos)
        //    {
        //        string nome = atributo["nome"]?.ToString() ?? "";
        //        string itemNome = atributo["item_nome"]?.ToString() ?? "";

        //        if (nome.StartsWith("TAM"))
        //            tam = itemNome;
        //        else if (nome.StartsWith("COR"))
        //            cor = itemNome;
        //    }

        //    if (!string.IsNullOrEmpty(tam) || !string.IsNullOrEmpty(cor))
        //    {
        //        var cmd = new SQLiteCommand(@"
        //            UPDATE Mercadorias 
        //            SET Tam = @tam, Cores = @cor
        //            WHERE ID_SoftcomShop = @id 
        //            " + (string.IsNullOrEmpty(codBarrasGrade) ? "" : "OR CodBarras_Grade = @codBarrasGrade"), conn);

        //        cmd.Parameters.AddWithValue("@tam", tam);
        //        cmd.Parameters.AddWithValue("@cor", cor);
        //        cmd.Parameters.AddWithValue("@id", produtoId);
        //        if (!string.IsNullOrEmpty(codBarrasGrade))
        //            cmd.Parameters.AddWithValue("@codBarrasGrade", codBarrasGrade);

        //        cmd.ExecuteNonQuery();
        //    }
        //}

        private void ProcessarAtributos(SQLiteConnection conn, JToken produto)
        {
            // IMPORTANTE: Para grade, o 'id' do JSON (produto_empresa_grade_id) 
            // ou o 'codigo_barras_grade' são as únicas chaves seguras.
            string codBarrasGrade = produto["codigo_barras_grade"]?.ToString() ?? "";
            var atributos = produto["sku_atributo"] as JArray;

            if (atributos == null || string.IsNullOrEmpty(codBarrasGrade)) return;

            string valorTam = "";
            string valorCor = "";

            foreach (var atributo in atributos)
            {
                string nomeAttr = atributo["nome"]?.ToString().ToUpper() ?? "";
                string itemNome = atributo["item_nome"]?.ToString() ?? "";

                // Mesma lógica de limpeza que funcionou anteriormente
                if (nomeAttr.StartsWith("TAM"))
                    valorTam = itemNome.Contains(":") ? itemNome.Split(':')[1].Trim() : itemNome;
                else if (nomeAttr.StartsWith("COR"))
                    valorCor = itemNome.Contains(":") ? itemNome.Split(':')[1].Trim() : itemNome;
            }

            if (!string.IsNullOrEmpty(valorTam) || !string.IsNullOrEmpty(valorCor))
            {
                // MUDANÇA CRUCIAL: O WHERE agora é estritamente pelo CodBarras_Grade.
                // Isso impede que o Tamanho 'P' sobrescreva o 'M' no banco local.
                var cmd = new SQLiteCommand(@"
            UPDATE Mercadorias 
            SET Tam = @tam, 
                Cores = @cor 
            WHERE CodBarras_Grade = @codBarrasGrade", conn);

                cmd.Parameters.AddWithValue("@tam", valorTam);
                cmd.Parameters.AddWithValue("@cor", valorCor);
                cmd.Parameters.AddWithValue("@codBarrasGrade", codBarrasGrade);

                cmd.ExecuteNonQuery();
            }
        }


        #endregion

        #region Busca por Nota Fiscal

        /// <summary>
        /// Busca produtos por nota fiscal
        /// </summary>
        public async Task<SyncResult> BuscarPorNotaFiscalAsync(DateTime dataEntrada, int numeroNota = 0, string versao = "v2", IProgress<string> progress = null)
        {
            var result = new SyncResult();

            try
            {
                progress?.Report("Buscando nota fiscal...");

                string dataFormatada = dataEntrada.ToString("yyyy-MM-dd");
                string jsonResponse = await _service.GetNotaFiscalAsync(dataFormatada, numeroNota, 1, versao);
                
                var response = JObject.Parse(jsonResponse);
                var produtos = ExtrairProdutosNotaFiscal(response);

                if (produtos == null || produtos.Count == 0)
                {
                    result.Sucesso = false;
                    result.MensagemErro = "A nota fiscal foi consultada, mas a resposta da API nao contem itens de produto no formato esperado.";
                    return result;
                }

                // Limpar etiquetas anteriores
                LimparEtiquetas();

                // Processar produtos marcando para impressão
                result.ProdutosAdicionados = ProcessarProdutosNotaFiscal(produtos, versao);

                if (result.ProdutosAdicionados == 0)
                {
                    result.Sucesso = false;
                    result.MensagemErro = "A nota fiscal foi consultada, mas nenhum item possuia identificador suficiente para atualizar ou inserir no banco local.";
                    return result;
                }

                progress?.Report($"{result.ProdutosAdicionados} produtos carregados!");
                result.Sucesso = true;
            }
            catch (Exception ex)
            {
                result.Sucesso = false;
                result.MensagemErro = ex.Message;
            }

            return result;
        }

        private int ProcessarProdutosNotaFiscal(JArray produtos, string versao)
        {
            int count = 0;

            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();

                foreach (var produto in produtos)
                {
                    // Verificar se produto já existe
                    long produtoId = LerLong(produto["produto_id"]);
                    string codigoMercadoria = ObterCodigoMercadoria(produto, produtoId);
                    string codBarras = ObterTexto(produto["codigo_barras"]);
                    string codBarrasGrade = ObterTexto(produto["codigo_barras_grade"]);
                    int quantidade = Math.Max(1, LerInteiro(produto["compra_item_quantidade"], 1));

                    if (!TemIdentificacaoProduto(produtoId, codigoMercadoria, codBarras, codBarrasGrade))
                    {
                        System.Diagnostics.Debug.WriteLine("[ProcessarProdutosNotaFiscal] Item ignorado: sem produto_id, codigo de mercadoria ou codigo de barras.");
                        continue;
                    }

                    if (ProdutoExiste(conn, produtoId, codBarrasGrade, codigoMercadoria, codBarras))
                    {
                        // Atualizar e marcar para impressão
                        AtualizarProdutoNF(conn, produto, versao);
                    }
                    else
                    {
                        // Inserir novo produto
                        InserirProduto(conn, produto, versao);
                        MarcarParaImpressao(conn, produtoId, codBarrasGrade, quantidade, codigoMercadoria, codBarras);
                    }

                    count++;
                }
            }

            return count;
        }

        private JArray ExtrairProdutosNotaFiscal(JObject response)
        {
            var produtos = new JArray();
            AdicionarProdutosNotaFiscal(produtos, response["data"]);
            return produtos;
        }

        private void AdicionarProdutosNotaFiscal(JArray destino, JToken token)
        {
            if (token == null || token.Type == JTokenType.Null)
                return;

            if (token is JArray array)
            {
                foreach (var item in array)
                    AdicionarProdutosNotaFiscal(destino, item);

                return;
            }

            if (!(token is JObject obj))
                return;

            if (TemCampoProdutoNotaFiscal(obj))
            {
                destino.Add(NormalizarItemNotaFiscal(obj));
                return;
            }

            var itens = obj["itens"] ?? obj["items"] ?? obj["produtos"] ?? obj["mercadorias"];
            if (itens != null)
                AdicionarProdutosNotaFiscal(destino, itens);
        }

        private bool TemCampoProdutoNotaFiscal(JObject item)
        {
            return item["produto_id"] != null ||
                   item["codigo_barras_grade"] != null ||
                   item["compra_item_quantidade"] != null ||
                   item["quantidade_item"] != null ||
                   item["produto"] is JObject ||
                   item["mercadoria"] is JObject;
        }

        private JObject NormalizarItemNotaFiscal(JObject item)
        {
            var produto = item["produto"] as JObject
                          ?? item["mercadoria"] as JObject
                          ?? item["produto_empresa"] as JObject
                          ?? item["produto_grade"] as JObject
                          ?? item["sku"] as JObject;

            var normalizado = new JObject();

            CopiarCampos(normalizado, produto);
            CopiarCampos(normalizado, item);

            DefinirSeVazio(normalizado, "produto_id",
                item["produto_id"],
                item["id_produto"],
                item["mercadoria_id"],
                item["codigo_produto"],
                produto?["produto_id"],
                produto?["id"],
                produto?["codigo"]);

            DefinirSeVazio(normalizado, "codigo_mercadoria",
                item["codigo_mercadoria"],
                item["codigo_produto"],
                item["mercadoria_codigo"],
                item["codigo"],
                produto?["codigo_mercadoria"],
                produto?["codigo_produto"],
                produto?["mercadoria_codigo"],
                produto?["codigo"]);

            DefinirSeVazio(normalizado, "codigo_barras_grade",
                item["codigo_barras_grade"],
                item["cod_barras_grade"],
                item["codigo_barras_sku"],
                produto?["codigo_barras_grade"],
                produto?["cod_barras_grade"],
                produto?["codigo_barras_sku"]);

            DefinirSeVazio(normalizado, "codigo_barras",
                item["codigo_barras"],
                item["cod_barras"],
                produto?["codigo_barras"],
                produto?["cod_barras"]);

            DefinirSeVazio(normalizado, "compra_item_quantidade",
                item["compra_item_quantidade"],
                item["quantidade"],
                item["qtd"],
                item["quantidade_item"],
                item["qtde"]);

            DefinirSeVazio(normalizado, "produto_nome",
                item["produto_nome"],
                item["descricao"],
                item["nome"],
                item["produto_descricao"],
                produto?["produto_nome"],
                produto?["descricao"],
                produto?["nome"],
                produto?["produto_descricao"]);

            DefinirSeVazio(normalizado, "preco_venda",
                item["preco_venda"],
                item["valor_unitario"],
                item["preco_unitario"],
                produto?["preco_venda"],
                produto?["valor_unitario"],
                produto?["preco"]);

            DefinirSeVazio(normalizado, "referencia",
                item["referencia"],
                item["codigo_referencia"],
                produto?["referencia"],
                produto?["codigo_referencia"]);

            DefinirSeVazio(normalizado, "marca_nome",
                item["marca_nome"],
                item["fabricante"],
                item["fabricante_nome"],
                item["nome_fabricante"],
                item["marca"],
                item["marca_descricao"],
                item["descricao_marca"],
                produto?["marca_nome"],
                produto?["fabricante"],
                produto?["fabricante_nome"],
                produto?["nome_fabricante"],
                produto?["marca"],
                produto?["marca_descricao"],
                produto?["descricao_marca"]);

            DefinirSeVazio(normalizado, "grupo_nome",
                item["grupo_nome"],
                item["grupo_descricao"],
                item["descricao_grupo"],
                item["nome_grupo"],
                item["grupo_produto"],
                item["grupo"],
                item["categoria"],
                item["categoria_nome"],
                item["departamento"],
                produto?["grupo_nome"],
                produto?["grupo_descricao"],
                produto?["descricao_grupo"],
                produto?["nome_grupo"],
                produto?["grupo_produto"],
                produto?["grupo"],
                produto?["categoria"],
                produto?["categoria_nome"],
                produto?["departamento"]);

            DefinirSeVazio(normalizado, "observacao",
                item["observacao"],
                item["observacao_produto"],
                item["produto_observacao"],
                item["observação"],
                item["observacoes"],
                item["obs"],
                item["OBSERVACAO"],
                item["OBSERVAÇÃO"],
                produto?["observacao"],
                produto?["observacao_produto"],
                produto?["produto_observacao"],
                produto?["observação"],
                produto?["observacoes"],
                produto?["obs"],
                produto?["OBSERVACAO"],
                produto?["OBSERVAÇÃO"]);

            return normalizado;
        }

        private void CopiarCampos(JObject destino, JObject origem)
        {
            if (origem == null)
                return;

            foreach (var prop in origem.Properties())
            {
                if (destino[prop.Name] == null)
                    destino[prop.Name] = prop.Value.DeepClone();
            }
        }

        private void DefinirSeVazio(JObject destino, string campo, params JToken[] valores)
        {
            if (!string.IsNullOrWhiteSpace(destino[campo]?.ToString()))
                return;

            foreach (var valor in valores)
            {
                if (valor == null || valor.Type == JTokenType.Null)
                    continue;

                string texto = valor.ToString();
                if (string.IsNullOrWhiteSpace(texto))
                    continue;

                destino[campo] = valor.DeepClone();
                return;
            }
        }

        /// <summary>
        /// Busca produtos por numero da nota fiscal de entrada no SoftcomShop.
        /// Usado pelo fluxo principal de carregamento para retornar dados ao painel de impressao.
        /// </summary>
        public async Task<SyncResult> BuscarPorNumeroNotaFiscalAsync(int numeroNota, IProgress<string> progress = null)
        {
            var result = new SyncResult();
            int paginaAtual = 1;
            bool temMaisPaginas = true;

            try
            {
                progress?.Report($"Buscando nota fiscal {numeroNota}...");

                LimparEtiquetas();

                while (temMaisPaginas)
                {
                    string jsonResponse = await _service.GetNotaFiscalPorNumeroAsync(numeroNota, paginaAtual);
                    var response = JObject.Parse(jsonResponse);
                    var produtos = ExtrairProdutosNotaFiscal(response);

                    if (produtos == null || produtos.Count == 0)
                    {
                        temMaisPaginas = false;
                        continue;
                    }

                    result.ProdutosAdicionados += ProcessarProdutosNotaFiscal(produtos, "v2");

                    int totalPaginas = 1;
                    if (response["meta"]?["last_page"] != null)
                    {
                        totalPaginas = response["meta"]["last_page"].ToObject<int>();
                    }
                    else if (response["meta"]?["page"]?["count"] != null)
                    {
                        totalPaginas = response["meta"]["page"]["count"].ToObject<int>();
                    }

                    temMaisPaginas = paginaAtual < totalPaginas;
                    paginaAtual++;
                }

                if (result.ProdutosAdicionados == 0)
                {
                    result.Sucesso = false;
                    result.MensagemErro = "A nota fiscal foi consultada, mas a resposta da API nao contem itens de produto no formato esperado.";
                    return result;
                }

                progress?.Report($"{result.ProdutosAdicionados} produtos carregados!");
                result.Sucesso = true;
            }
            catch (Exception ex)
            {
                result.Sucesso = false;
                result.MensagemErro = ex.Message;
            }

            return result;
        }

        #endregion

        #region Busca por Precos Alterados

        public async Task<SyncResult> BuscarPrecosAlteradosAsync(DateTime dataInicial, IProgress<string> progress = null)
        {
            var result = new SyncResult();
            int paginaAtual = 1;
            bool temMaisPaginas = true;
            long timestamp = ConverterParaUnixTimestamp(dataInicial.Date);

            try
            {
                progress?.Report("Buscando precos alterados...");

                LimparEtiquetas();

                while (temMaisPaginas)
                {
                    string jsonResponse = await _service.GetPrecosAlteradosAsync(timestamp, paginaAtual);
                    var response = JObject.Parse(jsonResponse);
                    var produtos = ExtrairProdutosPrecosAlterados(response);

                    if (produtos == null || produtos.Count == 0)
                    {
                        temMaisPaginas = false;
                        continue;
                    }

                    result.ProdutosAdicionados += ProcessarProdutosMovimento(produtos, "quantidade", "v2");

                    if (response["date_sync"] != null)
                    {
                        AtualizarTimestamp(response["date_sync"].ToString());
                    }

                    int totalPaginas = ObterTotalPaginas(response);
                    temMaisPaginas = paginaAtual < totalPaginas;
                    paginaAtual++;
                }

                if (result.ProdutosAdicionados == 0)
                {
                    result.Sucesso = false;
                    result.MensagemErro = "Nenhum produto com preco alterado foi encontrado no periodo informado.";
                    return result;
                }

                progress?.Report($"{result.ProdutosAdicionados} produtos carregados!");
                result.Sucesso = true;
            }
            catch (Exception ex)
            {
                result.Sucesso = false;
                result.MensagemErro = ex.Message;
            }

            return result;
        }

        private JArray ExtrairProdutosPrecosAlterados(JObject response)
        {
            var produtos = new JArray();
            var data = response["data"] as JArray;
            if (data == null)
                return produtos;

            foreach (var item in data.OfType<JObject>())
            {
                var normalizado = NormalizarItemNotaFiscal(item);
                DefinirSeVazio(normalizado, "quantidade", item["quantidade"], item["qtd"], item["estoque"]);
                produtos.Add(normalizado);
            }

            return produtos;
        }

        private static long ConverterParaUnixTimestamp(DateTime data)
        {
            return Convert.ToInt64((data.Date - new DateTime(1970, 1, 1)).TotalSeconds);
        }

        private int ObterTotalPaginas(JObject response)
        {
            int totalPaginas = 1;

            if (response["meta"]?["page"]?["count"] != null)
            {
                totalPaginas = LerInteiro(response["meta"]["page"]["count"], 1);
            }
            else if (response["meta"]?["last_page"] != null)
            {
                totalPaginas = LerInteiro(response["meta"]["last_page"], 1);
            }

            return Math.Max(1, totalPaginas);
        }

        #endregion

        #region Busca por Venda

        /// <summary>
        /// Busca produtos por venda
        /// </summary>
        public async Task<SyncResult> BuscarPorVendaAsync(int numeroVenda, IProgress<string> progress = null)
        {
            var result = new SyncResult();

            try
            {
                progress?.Report($"Buscando venda {numeroVenda}...");

                string jsonResponse = await _service.GetVendaAsync(numeroVenda);
                var response = JObject.Parse(jsonResponse);

                if (response["code"]?.ToObject<int>() != 1)
                {
                    result.Sucesso = false;
                    result.MensagemErro = response["human"]?.ToString() ?? "Erro na consulta.";
                    return result;
                }

                var produtos = ExtrairProdutosVenda(response);

                if (produtos == null || produtos.Count == 0)
                {
                    result.Sucesso = false;
                    result.MensagemErro = "Nenhum produto encontrado nesta venda.";
                    return result;
                }

                // Limpar etiquetas anteriores
                LimparEtiquetas();

                // Processar produtos
                result.ProdutosAdicionados = ProcessarProdutosMovimento(produtos, "quantidade", "v1");

                if (result.ProdutosAdicionados == 0)
                {
                    result.Sucesso = false;
                    result.MensagemErro = "A venda foi consultada, mas nenhum item possuia identificador suficiente para atualizar ou inserir no banco local.";
                    return result;
                }

                if (response["date_sync"] != null)
                {
                    AtualizarTimestamp(response["date_sync"].ToString());
                }

                progress?.Report($"{result.ProdutosAdicionados} produtos carregados!");
                result.Sucesso = true;
            }
            catch (Exception ex)
            {
                result.Sucesso = false;
                result.MensagemErro = ex.Message;
            }

            return result;
        }

        private JArray ExtrairProdutosVenda(JObject response)
        {
            var produtos = new JArray();

            JToken data = response["data"];

            // A API de vendas devolve o "data" como JSON serializado em string.
            if (data != null && data.Type == JTokenType.String)
            {
                data = JObject.Parse(data.ToString());
            }

            var itens = data?["itens"] as JArray;
            if (itens == null)
                return produtos;

            foreach (var item in itens.OfType<JObject>())
            {
                var normalizado = NormalizarItemNotaFiscal(item);

                DefinirSeVazio(normalizado, "preco_venda",
                    item["preco"],
                    item["valor_unitario"],
                    item["preco_unitario"]);

                DefinirSeVazio(normalizado, "quantidade",
                    item["quantidade"],
                    item["qtd"],
                    item["quantidade_item"],
                    item["qtde"]);

                produtos.Add(normalizado);
            }

            return produtos;
        }

        private int ProcessarProdutosMovimento(JArray produtos, string campoQuantidade, string versao)
        {
            int count = 0;

            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();

                foreach (var produto in produtos)
                {
                    long produtoId = LerLong(produto["produto_id"]);
                    string codigoMercadoria = ObterCodigoMercadoria(produto, produtoId);
                    string codBarras = ObterTexto(produto["codigo_barras"]);
                    string codBarrasGrade = ObterTexto(produto["codigo_barras_grade"]);
                    int quantidade = Math.Max(1, LerInteiro(produto[campoQuantidade], 1));

                    if (!TemIdentificacaoProduto(produtoId, codigoMercadoria, codBarras, codBarrasGrade))
                    {
                        System.Diagnostics.Debug.WriteLine("[ProcessarProdutosMovimento] Item ignorado: sem produto_id, codigo de mercadoria ou codigo de barras.");
                        continue;
                    }

                    if (ProdutoExiste(conn, produtoId, codBarrasGrade, codigoMercadoria, codBarras))
                    {
                        AtualizarProdutoMovimento(conn, produto, campoQuantidade);
                    }
                    else
                    {
                        InserirProduto(conn, produto, versao);
                        MarcarParaImpressao(conn, produtoId, codBarrasGrade, quantidade, codigoMercadoria, codBarras);
                    }

                    if (produto["tabela_precos"] != null)
                    {
                        ProcessarTabelaPrecos(conn, produto);
                    }

                    if (versao == "v2" && produto["sku_atributo"] != null)
                    {
                        ProcessarAtributos(conn, produto);
                    }

                    count++;
                }
            }

            return count;
        }

        #endregion

        #region NFe / Volumes - Distribuidora

        public async Task<DistribuidoraDocumentoLogisticoResult> BuscarDocumentoLogisticoAsync(string numeroNfe, IProgress<string> progress = null)
        {
            var result = new DistribuidoraDocumentoLogisticoResult();

            try
            {
                numeroNfe = (numeroNfe ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(numeroNfe))
                {
                    result.Sucesso = false;
                    result.MensagemErro = "Informe o numero da NF-e.";
                    return result;
                }

                progress?.Report("Consultando venda pela NF-e...");

                string jsonVendas = await _service.GetVendasFiltroDistribuidoraAsync(numeroNfe);
                JArray vendas = ExtrairVendasFiltroDistribuidora(ParseJsonDistribuidora(jsonVendas));
                JObject venda = SelecionarVendaPorNumeroNfeDistribuidora(vendas, numeroNfe);

                if (venda == null)
                {
                    result.Sucesso = false;
                    result.MensagemErro = "Não foi localizada nenhuma Venda/Pedido vinculada à Nota Fiscal informada.";
                    return result;
                }

                long clienteId = LerLong(PrimeiroCampoDistribuidora(venda, "cliente_id", "cliente.id"));
                JObject clienteVenda = ObterObjetoRelacionadoDistribuidora(venda, "cliente");

                if (clienteId <= 0)
                {
                    result.Sucesso = false;
                    result.MensagemErro = "A venda foi localizada, mas nao possui cliente_id para consulta do destinatario.";
                    return result;
                }

                JArray itens = ObterItensVendaDistribuidora(venda);
                if (itens.Count == 0)
                {
                    result.Sucesso = false;
                    result.MensagemErro = "A venda foi localizada, mas nao retornou itens para impressao logistica.";
                    return result;
                }

                progress?.Report("Consultando cliente da venda...");

                string jsonCliente = await _service.GetClienteDistribuidoraAsync(clienteId);
                JObject cliente = ObterClienteApiDistribuidora(ParseJsonDistribuidora(jsonCliente), clienteId, out string mensagemErroCliente);
                if (cliente == null)
                {
                    result.Sucesso = false;
                    result.MensagemErro = mensagemErroCliente;
                    return result;
                }

                cliente = MesclarObjetosDistribuidora(cliente, clienteVenda);

                result.Etiqueta = MontarEtiquetaDistribuidora(venda, cliente, itens, numeroNfe);
                result.Sucesso = true;

                progress?.Report("Documento logistico carregado para impressao.");
            }
            catch (Exception ex)
            {
                result.Sucesso = false;
                result.MensagemErro = ObterMensagemErroDistribuidora(ex);
                progress?.Report($"Erro: {result.MensagemErro}");
            }

            return result;
        }

        private static JArray ExtrairVendasFiltroDistribuidora(JToken response)
        {
            var vendas = new JArray();
            JToken data = ObterTokenDataDistribuidora(response);

            if (data is JArray array)
            {
                foreach (JObject item in array.OfType<JObject>())
                {
                    JObject venda = ObterObjetoRelacionadoDistribuidora(item, "venda") ?? item;
                    vendas.Add(venda);
                }

                return vendas;
            }

            if (data is JObject obj)
            {
                JObject venda = ObterObjetoRelacionadoDistribuidora(obj, "venda") ?? obj;
                if (venda != null)
                    vendas.Add(venda);
            }

            return vendas;
        }

        private static JObject SelecionarVendaPorNumeroNfeDistribuidora(JArray vendas, string numeroNfe)
        {
            if (vendas == null || vendas.Count == 0)
                return null;

            string numeroNormalizado = NormalizarDocumentoDistribuidora(numeroNfe);

            foreach (JObject venda in vendas.OfType<JObject>())
            {
                string[] candidatos =
                {
                    TextoCampoDistribuidora(venda, "numero_documento"),
                    TextoCampoDistribuidora(venda, "numero_nfe", "numero_nf", "nota_fiscal", "nfe.numero", "nfe.numero_nfe"),
                    TextoCampoDistribuidora(venda, "nfe_id")
                };

                foreach (string candidato in candidatos)
                {
                    string candidatoNormalizado = NormalizarDocumentoDistribuidora(candidato);
                    if (!string.IsNullOrWhiteSpace(candidatoNormalizado) && candidatoNormalizado == numeroNormalizado)
                        return venda;
                }
            }

            return vendas.Count == 1 ? vendas.OfType<JObject>().FirstOrDefault() : null;
        }

        private static JArray ObterItensVendaDistribuidora(JObject venda)
        {
            JToken itens = PrimeiroCampoDistribuidora(venda, "itens");

            if (itens is JArray array)
                return array;

            return new JArray();
        }

        private EtiquetaDistribuidora MontarEtiquetaDistribuidora(JObject venda, JObject cliente, JArray itens, string numeroNfeInformado)
        {
            var etiqueta = new EtiquetaDistribuidora
            {
                Venda = new DadosVendaDistribuidora
                {
                    Id = LerLong(PrimeiroCampoDistribuidora(venda, "id")),
                    ClienteId = LerLong(PrimeiroCampoDistribuidora(venda, "cliente_id", "cliente.id")),
                    EmpresaId = LerLong(PrimeiroCampoDistribuidora(venda, "empresa_id", "empresa.id")),
                    NumeroDocumento = TextoCampoDistribuidora(venda, "numero_documento"),
                    NumeroNf = PrimeiroTextoValor(
                        TextoCampoDistribuidora(venda, "numero_nfe", "numero_nf", "nota_fiscal", "nfe.numero", "nfe.numero_nfe"),
                        numeroNfeInformado),
                    DataEmissao = DataCampoDistribuidora(venda, "api_data_hora_venda", "data_hora_venda", "created_at"),
                    Observacao = TextoCampoDistribuidora(venda, "observacao"),
                    NfeId = LerLong(PrimeiroCampoDistribuidora(venda, "nfe_id"))
                },
                Empresa = MontarEmpresaDistribuidora(ObterObjetoRelacionadoDistribuidora(venda, "empresa")),
                Destinatario = MontarDestinatarioEtiquetaDistribuidora(cliente),
                Endereco = MontarEnderecoEtiquetaDistribuidora(cliente),
                Produtos = MontarProdutosVendaDistribuidora(itens, LerLong(PrimeiroCampoDistribuidora(venda, "id")))
            };

            return etiqueta;
        }

        private static DadosEmpresaDistribuidora MontarEmpresaDistribuidora(JObject empresa)
        {
            return new DadosEmpresaDistribuidora
            {
                Id = LerLong(PrimeiroCampoDistribuidora(empresa, "id")),
                Nome = TextoCampoDistribuidora(empresa, "nome"),
                Fantasia = TextoCampoDistribuidora(empresa, "fantasia"),
                RazaoSocial = TextoCampoDistribuidora(empresa, "razao_social"),
                Cnpj = TextoCampoDistribuidora(empresa, "cnpj")
            };
        }

        private static DadosDestinatarioEtiquetaDistribuidora MontarDestinatarioEtiquetaDistribuidora(JObject cliente)
        {
            return new DadosDestinatarioEtiquetaDistribuidora
            {
                Id = LerLong(PrimeiroCampoDistribuidora(cliente, "id")),
                Nome = TextoCampoDistribuidora(cliente, "nome"),
                RazaoSocial = TextoCampoDistribuidora(cliente, "razao_social"),
                Documento = TextoCampoDistribuidora(cliente, "cpf_cnpj", "documento", "cnpj", "cpf")
            };
        }

        private static DadosEnderecoEtiquetaDistribuidora MontarEnderecoEtiquetaDistribuidora(JObject cliente)
        {
            return new DadosEnderecoEtiquetaDistribuidora
            {
                Endereco = TextoCampoDistribuidora(cliente, "endereco", "logradouro"),
                Numero = TextoCampoDistribuidora(cliente, "numero"),
                Complemento = TextoCampoDistribuidora(cliente, "complemento"),
                Bairro = TextoCampoDistribuidora(cliente, "bairro"),
                Cidade = TextoCampoDistribuidora(cliente, "cidade"),
                Uf = TextoCampoDistribuidora(cliente, "uf"),
                Cep = TextoCampoDistribuidora(cliente, "cep")
            };
        }

        private static List<ProdutoVendaDistribuidora> MontarProdutosVendaDistribuidora(JArray itens, long vendaId)
        {
            var produtos = new List<ProdutoVendaDistribuidora>();

            foreach (JObject item in itens.OfType<JObject>())
            {
                produtos.Add(new ProdutoVendaDistribuidora
                {
                    VendaId = LerLong(PrimeiroCampoDistribuidora(item, "venda_id"), vendaId),
                    ProdutoId = LerLong(PrimeiroCampoDistribuidora(item, "produto_id")),
                    Quantidade = DecimalCampoDistribuidora(item, "quantidade") ?? 0m,
                    Preco = DecimalCampoDistribuidora(item, "preco") ?? 0m,
                    Peso = DecimalCampoDistribuidora(item, "peso") ?? 0m
                });
            }

            return produtos;
        }

        private static string NormalizarDocumentoDistribuidora(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return string.Empty;

            string digitos = SomenteDigitos(valor);
            if (string.IsNullOrWhiteSpace(digitos))
                return valor.Trim().ToUpperInvariant();

            string semZerosEsquerda = digitos.TrimStart('0');
            return string.IsNullOrWhiteSpace(semZerosEsquerda) ? "0" : semZerosEsquerda;
        }

        public async Task<DistribuidoraNFeVolumesResult> BuscarVolumesDistribuidoraAsync(DateTime dataEmissao, int numeroNota, IProgress<string> progress = null)
        {
            var result = new DistribuidoraNFeVolumesResult();

            try
            {
                progress?.Report("Consultando cabecalho da NFe...");

                string jsonCabecalho = await _service.GetVendaCompletaDistribuidoraAsync(dataEmissao, numeroNota);
                JToken response = ParseJsonDistribuidora(jsonCabecalho);
                JObject nota = ObterObjetoNotaDistribuidora(response, numeroNota);

                if (nota == null)
                {
                    result.Sucesso = false;
                    result.MensagemErro = "A NFe foi consultada, mas o cabecalho nao foi encontrado no formato esperado.";
                    return result;
                }

                DadosNotaDistribuidora dadosNota = MontarDadosNotaDistribuidora(nota, numeroNota, dataEmissao);

                progress?.Report("Consultando cliente da NFe...");
                JObject cliente = ObterObjetoRelacionadoDistribuidora(nota, "cliente", "destinatario", "pessoa", "consumidor");
                long clienteId = ObterClienteIdDistribuidora(nota, cliente);
                dadosNota.ClienteId = clienteId;

                if (clienteId > 0)
                {
                    try
                    {
                        string jsonCliente = await _service.GetClienteDistribuidoraAsync(clienteId);
                        JObject clienteApi = ObterPrimeiroObjetoDataDistribuidora(ParseJsonDistribuidora(jsonCliente));
                        if (clienteApi != null)
                            cliente = MesclarObjetosDistribuidora(clienteApi, cliente);
                    }
                    catch (SoftcomShopApiException ex)
                    {
                        if (ex.StatusCode != HttpStatusCode.NotFound || cliente == null)
                            throw;
                    }
                }

                if (cliente == null)
                {
                    result.Sucesso = false;
                    result.MensagemErro = "A NFe foi encontrada, mas os dados do cliente/destinatario nao foram retornados.";
                    return result;
                }

                progress?.Report("Consultando bairro do destinatario...");
                JObject bairro = ObterObjetoRelacionadoDistribuidora(cliente, "bairro", "endereco_bairro");
                long bairroId = ObterBairroIdDistribuidora(cliente, nota);

                if (bairroId > 0)
                {
                    try
                    {
                        string jsonBairro = await _service.GetBairroDistribuidoraAsync(bairroId);
                        JObject bairroApi = ObterPrimeiroObjetoDataDistribuidora(ParseJsonDistribuidora(jsonBairro));
                        if (bairroApi != null)
                            bairro = MesclarObjetosDistribuidora(bairroApi, bairro);
                    }
                    catch (SoftcomShopApiException ex)
                    {
                        if (ex.StatusCode != HttpStatusCode.NotFound || bairro == null)
                            throw;
                    }
                }

                DadosDestinatarioDistribuidora destinatario = MontarDadosDestinatarioDistribuidora(cliente, nota);
                DadosEnderecoDistribuidora endereco = MontarDadosEnderecoDistribuidora(cliente, bairro, bairroId);

                progress?.Report("Consultando itens da NFe...");
                JArray itensJson = ExtrairItensNotaDistribuidora(nota);
                if (itensJson.Count == 0)
                    itensJson = ExtrairItensNotaDistribuidora(response);

                if (itensJson.Count == 0)
                {
                    result.Sucesso = false;
                    result.MensagemErro = "A NFe foi consultada, mas nenhum item foi retornado para gerar volumes.";
                    return result;
                }

                List<ItemNotaDistribuidora> itens = MontarItensDistribuidora(itensJson);
                List<VolumeDistribuidora> volumes = MontarVolumesDistribuidora(dadosNota, itens);

                if (volumes.Count == 0)
                {
                    result.Sucesso = false;
                    result.MensagemErro = "Os itens da NFe foram consultados, mas nenhum volume logistico pode ser gerado.";
                    return result;
                }

                result.DadosNota = dadosNota;
                result.DadosDestinatario = destinatario;
                result.DadosEndereco = endereco;
                result.Itens = itens;
                result.Volumes = volumes;
                result.Etiquetas = MontarEtiquetasVolumesDistribuidora(dadosNota, destinatario, endereco, itens, volumes);
                result.Sucesso = true;

                progress?.Report($"{result.TotalVolumes} volumes gerados para impressao.");
            }
            catch (Exception ex)
            {
                result.Sucesso = false;
                result.MensagemErro = ObterMensagemErroDistribuidora(ex);
                progress?.Report($"Erro: {result.MensagemErro}");
            }

            return result;
        }

        private DadosNotaDistribuidora MontarDadosNotaDistribuidora(JObject nota, int numeroNota, DateTime dataEmissaoInformada)
        {
            return new DadosNotaDistribuidora
            {
                NumeroDocumento = TextoCampoDistribuidora(nota, "numero_documento", "documento", "numero", "venda_numero", "id"),
                NumeroNFe = PrimeiroTextoValor(
                    TextoCampoDistribuidora(nota, "numero_nfe", "nfe_numero", "numero_nota_fiscal", "nota_fiscal", "numero_nf"),
                    numeroNota > 0 ? numeroNota.ToString(CultureInfo.InvariantCulture) : string.Empty),
                Serie = TextoCampoDistribuidora(nota, "serie", "serie_nfe", "nfe_serie"),
                Modelo = TextoCampoDistribuidora(nota, "modelo", "modelo_nfe", "nfe_modelo"),
                DataEmissao = DataCampoDistribuidora(nota, "data_emissao", "data_hora_emissao", "emissao", "data") ?? dataEmissaoInformada,
                ValorTotal = DecimalCampoDistribuidora(nota, "valor_total", "total", "venda_total", "valor_nota", "valor_total_nota"),
                Observacao = TextoCampoDistribuidora(nota, "observacao", "observacoes", "obs"),
                ChaveAcesso = TextoCampoDistribuidora(nota, "chave", "chave_acesso", "chave_nfe", "nfe_chave", "chave_acesso_nfe")
            };
        }

        private DadosDestinatarioDistribuidora MontarDadosDestinatarioDistribuidora(JObject cliente, JObject nota)
        {
            return new DadosDestinatarioDistribuidora
            {
                RazaoSocial = PrimeiroTextoValor(
                    TextoCampoDistribuidora(cliente, "razao_social", "nome_razao_social", "nome", "cliente_nome"),
                    TextoCampoDistribuidora(nota, "cliente_nome", "razao_social")),
                NomeFantasia = PrimeiroTextoValor(
                    TextoCampoDistribuidora(cliente, "nome_fantasia", "fantasia", "apelido"),
                    TextoCampoDistribuidora(nota, "cliente_fantasia", "nome_fantasia")),
                Documento = PrimeiroTextoValor(
                    TextoCampoDistribuidora(cliente, "documento", "cpf_cnpj", "cnpj", "cpf"),
                    TextoCampoDistribuidora(nota, "cliente_documento", "cpf_cnpj", "cnpj", "cpf")),
                Telefone = TextoCampoDistribuidora(cliente, "telefone", "fone", "celular", "telefone1"),
                Email = TextoCampoDistribuidora(cliente, "email", "e_mail")
            };
        }

        private DadosEnderecoDistribuidora MontarDadosEnderecoDistribuidora(JObject cliente, JObject bairro, long bairroId)
        {
            return new DadosEnderecoDistribuidora
            {
                Logradouro = TextoCampoDistribuidora(cliente, "logradouro", "endereco", "rua", "endereco_logradouro"),
                Numero = TextoCampoDistribuidora(cliente, "numero", "endereco_numero", "num"),
                Complemento = TextoCampoDistribuidora(cliente, "complemento", "endereco_complemento"),
                Cep = TextoCampoDistribuidora(cliente, "cep", "codigo_postal"),
                BairroId = bairroId,
                Bairro = PrimeiroTextoValor(
                    TextoCampoDistribuidora(bairro, "nome", "descricao", "bairro", "nome_bairro"),
                    TextoCampoDistribuidora(cliente, "bairro", "bairro_nome", "nome_bairro")),
                Cidade = TextoCampoDistribuidora(cliente, "cidade", "cidade_nome", "municipio", "nome_municipio"),
                Uf = TextoCampoDistribuidora(cliente, "uf", "estado", "estado_sigla")
            };
        }

        private List<ItemNotaDistribuidora> MontarItensDistribuidora(JArray itensJson)
        {
            var itens = new List<ItemNotaDistribuidora>();

            foreach (JObject item in itensJson.OfType<JObject>())
            {
                JObject produto = ObterObjetoRelacionadoDistribuidora(item, "produto", "mercadoria", "produto_empresa", "sku");

                decimal quantidade = DecimalCampoDistribuidora(item, "quantidade", "qtd", "quantidade_item", "qtde") ?? 1m;
                int quantidadeVolumes = Math.Max(1, LerInteiro(PrimeiroCampoDistribuidora(item,
                    "quantidade_volumes",
                    "qtd_volumes",
                    "volume_quantidade",
                    "quantidade_volume",
                    "volumes_quantidade"), 1));

                itens.Add(new ItemNotaDistribuidora
                {
                    Codigo = PrimeiroTextoValor(
                        TextoCampoDistribuidora(item, "codigo", "codigo_mercadoria", "codigo_produto", "produto_id"),
                        TextoCampoDistribuidora(produto, "codigo", "codigo_mercadoria", "codigo_produto", "produto_id")),
                    Descricao = PrimeiroTextoValor(
                        TextoCampoDistribuidora(item, "descricao", "produto_nome", "nome", "mercadoria"),
                        TextoCampoDistribuidora(produto, "descricao", "produto_nome", "nome", "mercadoria")),
                    Quantidade = quantidade <= 0 ? 1m : quantidade,
                    QuantidadeVolumes = quantidadeVolumes,
                    Peso = DecimalCampoDistribuidora(item, "peso_volume", "peso_bruto", "peso_liquido", "peso", "peso_total")
                           ?? DecimalCampoDistribuidora(produto, "peso_volume", "peso_bruto", "peso_liquido", "peso", "peso_total")
                });
            }

            return itens;
        }

        private List<VolumeDistribuidora> MontarVolumesDistribuidora(DadosNotaDistribuidora dadosNota, List<ItemNotaDistribuidora> itens)
        {
            var volumes = new List<VolumeDistribuidora>();
            string numeroNFe = PrimeiroTextoValor(dadosNota?.NumeroNFe, dadosNota?.NumeroDocumento, "NFE");
            string empresaEmitente = _config?.CompanyName ?? string.Empty;
            string operador = Environment.UserName ?? string.Empty;

            foreach (ItemNotaDistribuidora item in itens)
            {
                int quantidadeVolumes = Math.Max(1, item.QuantidadeVolumes);

                for (int i = 0; i < quantidadeVolumes; i++)
                {
                    int sequencial = volumes.Count + 1;
                    volumes.Add(new VolumeDistribuidora
                    {
                        VolumeAtual = sequencial,
                        CodigoVolume = CriarCodigoVolumeDistribuidora(numeroNFe, sequencial),
                        Peso = item.Peso,
                        DataImpressao = DateTime.Now,
                        Operador = operador,
                        EmpresaEmitente = empresaEmitente
                    });
                }
            }

            int totalVolumes = volumes.Count;
            foreach (VolumeDistribuidora volume in volumes)
                volume.TotalVolumes = totalVolumes;

            return volumes;
        }

        private List<EtiquetaVolumeDistribuidora> MontarEtiquetasVolumesDistribuidora(
            DadosNotaDistribuidora dadosNota,
            DadosDestinatarioDistribuidora destinatario,
            DadosEnderecoDistribuidora endereco,
            List<ItemNotaDistribuidora> itens,
            List<VolumeDistribuidora> volumes)
        {
            var etiquetas = new List<EtiquetaVolumeDistribuidora>();

            foreach (VolumeDistribuidora volume in volumes)
            {
                etiquetas.Add(new EtiquetaVolumeDistribuidora
                {
                    DadosNota = dadosNota,
                    DadosDestinatario = destinatario,
                    DadosEndereco = endereco,
                    Itens = itens,
                    Volumes = volumes,
                    Volume = volume
                });
            }

            return etiquetas;
        }

        private static JToken ParseJsonDistribuidora(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new JsonReaderException("Resposta vazia da API.");

            return JToken.Parse(json);
        }

        private static JObject ObterObjetoNotaDistribuidora(JToken response, int numeroNota)
        {
            JToken data = ObterTokenDataDistribuidora(response);

            if (data is JArray array)
            {
                JObject primeiro = null;

                foreach (JObject item in array.OfType<JObject>())
                {
                    if (primeiro == null)
                        primeiro = item;

                    string numero = TextoCampoDistribuidora(item, "numero_nfe", "numero_nota_fiscal", "nota_fiscal", "numero_nf", "numero_documento", "numero");
                    if (numeroNota > 0 && string.Equals(SomenteDigitos(numero), numeroNota.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase))
                        return item;
                }

                return primeiro;
            }

            return data as JObject;
        }

        private static JObject ObterPrimeiroObjetoDataDistribuidora(JToken response)
        {
            JToken data = ObterTokenDataDistribuidora(response);

            if (data is JObject obj)
                return obj;

            if (data is JArray array)
                return array.OfType<JObject>().FirstOrDefault();

            return null;
        }

        private static JObject ObterClienteApiDistribuidora(JToken response, long clienteId, out string mensagemErro)
        {
            mensagemErro = null;

            JObject envelope = response as JObject;
            if (envelope != null)
            {
                JToken code = ObterCampoIgnoreCaseDistribuidora(envelope, "code");
                if (ValorCampoPreenchidoDistribuidora(code))
                {
                    string codigo = ObterTextoClassificacao(code);
                    if (!string.Equals(codigo, "1", StringComparison.OrdinalIgnoreCase))
                    {
                        mensagemErro = PrimeiroTextoValor(
                            TextoCampoDistribuidora(envelope, "human"),
                            TextoCampoDistribuidora(envelope, "message"),
                            $"A API de clientes retornou code {codigo}."
                        );
                        return null;
                    }
                }
            }

            JObject cliente = ObterPrimeiroObjetoDataDistribuidora(response);
            if (cliente == null)
            {
                mensagemErro = $"Cliente {clienteId} nao foi localizado na API SoftcomShop.";
                return null;
            }

            mensagemErro = string.Empty;
            return cliente;
        }

        private static JToken ObterTokenDataDistribuidora(JToken response)
        {
            if (response == null || response.Type == JTokenType.Null)
                return null;

            if (response.Type == JTokenType.String)
                return ParseJsonDistribuidora(response.ToString());

            JObject obj = response as JObject;
            if (obj == null)
                return response;

            JToken data = ObterCampoIgnoreCaseDistribuidora(obj, "data");
            if (data == null)
                return response;

            if (data.Type == JTokenType.String)
                return ParseJsonDistribuidora(data.ToString());

            return data;
        }

        private static JArray ExtrairItensNotaDistribuidora(JToken token)
        {
            var itens = new JArray();
            AdicionarItensNotaDistribuidora(itens, token);
            return itens;
        }

        private static void AdicionarItensNotaDistribuidora(JArray destino, JToken token)
        {
            if (token == null || token.Type == JTokenType.Null)
                return;

            if (token.Type == JTokenType.String)
            {
                AdicionarItensNotaDistribuidora(destino, ParseJsonDistribuidora(token.ToString()));
                return;
            }

            if (token is JArray array)
            {
                foreach (JToken item in array)
                    AdicionarItensNotaDistribuidora(destino, item);

                return;
            }

            JObject obj = token as JObject;
            if (obj == null)
                return;

            JToken itens = PrimeiroCampoDistribuidora(obj, "itens", "items", "produtos", "mercadorias", "venda_itens", "nota_itens");
            if (itens != null)
            {
                AdicionarItensNotaDistribuidora(destino, itens);
                return;
            }

            if (TemCampoItemNotaDistribuidora(obj))
                destino.Add(obj);
        }

        private static bool TemCampoItemNotaDistribuidora(JObject item)
        {
            return PrimeiroCampoDistribuidora(item,
                "produto_id",
                "codigo_produto",
                "codigo_mercadoria",
                "descricao",
                "produto_nome",
                "quantidade",
                "qtd",
                "quantidade_item") != null ||
                ObterObjetoRelacionadoDistribuidora(item, "produto", "mercadoria", "sku") != null;
        }

        private static long ObterClienteIdDistribuidora(JObject nota, JObject cliente)
        {
            long clienteId = LerLong(PrimeiroCampoDistribuidora(nota,
                "cliente_id",
                "id_cliente",
                "destinatario_id",
                "pessoa_id",
                "cliente.id",
                "destinatario.id"));

            if (clienteId <= 0)
                clienteId = LerLong(PrimeiroCampoDistribuidora(cliente, "id", "cliente_id", "pessoa_id"));

            return clienteId;
        }

        private static long ObterBairroIdDistribuidora(JObject cliente, JObject nota)
        {
            long bairroId = LerLong(PrimeiroCampoDistribuidora(cliente,
                "bairro_id",
                "id_bairro",
                "endereco_bairro_id",
                "bairro.id"));

            if (bairroId <= 0)
            {
                bairroId = LerLong(PrimeiroCampoDistribuidora(nota,
                    "bairro_id",
                    "id_bairro",
                    "cliente.bairro_id",
                    "destinatario.bairro_id",
                    "cliente.bairro.id",
                    "destinatario.bairro.id"));
            }

            return bairroId;
        }

        private static JObject ObterObjetoRelacionadoDistribuidora(JObject obj, params string[] campos)
        {
            JToken token = PrimeiroCampoDistribuidora(obj, campos);
            return token as JObject;
        }

        private static JObject MesclarObjetosDistribuidora(JObject principal, JObject fallback)
        {
            if (principal == null)
                return fallback;

            if (fallback == null)
                return principal;

            var resultado = new JObject();
            CopiarCamposDistribuidora(resultado, fallback);
            CopiarCamposDistribuidora(resultado, principal);
            return resultado;
        }

        private static void CopiarCamposDistribuidora(JObject destino, JObject origem)
        {
            if (destino == null || origem == null)
                return;

            foreach (var prop in origem.Properties())
                destino[prop.Name] = prop.Value.DeepClone();
        }

        private static JToken PrimeiroCampoDistribuidora(JObject obj, params string[] campos)
        {
            if (obj == null || campos == null)
                return null;

            foreach (string campo in campos)
            {
                JToken valor = ObterCampoCaminhoDistribuidora(obj, campo);
                if (ValorCampoPreenchidoDistribuidora(valor))
                    return valor;
            }

            return null;
        }

        private static JToken ObterCampoCaminhoDistribuidora(JObject obj, string caminho)
        {
            if (obj == null || string.IsNullOrWhiteSpace(caminho))
                return null;

            string[] partes = caminho.Split('.');
            JToken atual = obj;

            foreach (string parte in partes)
            {
                JObject atualObj = atual as JObject;
                if (atualObj == null)
                    return null;

                atual = ObterCampoIgnoreCaseDistribuidora(atualObj, parte);
                if (atual == null)
                    return null;
            }

            return atual;
        }

        private static JToken ObterCampoIgnoreCaseDistribuidora(JObject obj, string nomeCampo)
        {
            if (obj == null || string.IsNullOrWhiteSpace(nomeCampo))
                return null;

            foreach (var prop in obj.Properties())
            {
                if (string.Equals(prop.Name, nomeCampo, StringComparison.OrdinalIgnoreCase))
                    return prop.Value;
            }

            return null;
        }

        private static bool ValorCampoPreenchidoDistribuidora(JToken valor)
        {
            if (valor == null || valor.Type == JTokenType.Null)
                return false;

            if (valor is JValue)
                return !string.IsNullOrWhiteSpace(valor.ToString());

            if (valor is JArray array)
                return array.Count > 0;

            if (valor is JObject obj)
                return obj.Properties().Any();

            return true;
        }

        private static string TextoCampoDistribuidora(JObject obj, params string[] campos)
        {
            return ObterTextoClassificacao(PrimeiroCampoDistribuidora(obj, campos));
        }

        private static decimal? DecimalCampoDistribuidora(JObject obj, params string[] campos)
        {
            JToken valor = PrimeiroCampoDistribuidora(obj, campos);
            if (!ValorCampoPreenchidoDistribuidora(valor))
                return null;

            return LerDecimal(valor, 0m);
        }

        private static DateTime? DataCampoDistribuidora(JObject obj, params string[] campos)
        {
            string texto = TextoCampoDistribuidora(obj, campos);
            if (string.IsNullOrWhiteSpace(texto))
                return null;

            if (long.TryParse(texto, NumberStyles.Integer, CultureInfo.InvariantCulture, out long unix))
            {
                if (unix > 100000000000)
                    unix = unix / 1000;

                return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                    .AddSeconds(unix)
                    .ToLocalTime();
            }

            DateTime data;
            if (DateTime.TryParse(texto, CultureInfo.GetCultureInfo("pt-BR"), DateTimeStyles.AssumeLocal, out data))
                return data;

            if (DateTime.TryParse(texto, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out data))
                return data;

            return null;
        }

        private static string CriarCodigoVolumeDistribuidora(string numeroNFe, int sequencial)
        {
            string numero = SomenteLetrasNumeros(PrimeiroTextoValor(numeroNFe, "NFE"));
            return $"{numero}-VOL{sequencial.ToString("000", CultureInfo.InvariantCulture)}";
        }

        private static string SomenteDigitos(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return string.Empty;

            return new string(texto.Where(char.IsDigit).ToArray());
        }

        private static string SomenteLetrasNumeros(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return string.Empty;

            return new string(texto.Where(char.IsLetterOrDigit).ToArray());
        }

        private static string ObterMensagemErroDistribuidora(Exception ex)
        {
            if (ex is SoftcomShopApiException apiEx)
            {
                switch (apiEx.StatusCode)
                {
                    case HttpStatusCode.Unauthorized:
                        return "A API recusou a autenticacao. Verifique Client ID, Client Secret e dispositivo.";
                    case HttpStatusCode.Forbidden:
                        return "A API negou acesso a esta rotina. Verifique as permissoes do cliente SoftcomShop.";
                    case HttpStatusCode.NotFound:
                        return "A NFe, cliente ou bairro informado nao foi encontrado na API SoftcomShop.";
                    case HttpStatusCode.InternalServerError:
                        return "A API SoftcomShop retornou erro interno. Tente novamente ou acione o suporte.";
                    default:
                        return $"A API SoftcomShop retornou HTTP {(int)apiEx.StatusCode}.";
                }
            }

            if (ex is TimeoutException)
                return "Tempo limite excedido ao comunicar com a API SoftcomShop. Tente novamente.";

            if (ex is JsonReaderException || ex is JsonSerializationException)
                return "A API retornou um JSON invalido ou em formato inesperado.";

            return $"Falha ao consultar NFe / Volumes: {ex.Message}";
        }

        #endregion

        #region Métodos Auxiliares

        private void LimparTabelasProdutos()
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();

                using (var cmd = conn.CreateCommand())
                {
                    // ⭐ CORREÇÃO: Limpar TODOS os produtos
                    // Quando está em modo SoftcomShop, só deve ter produtos do SoftcomShop
                    // Não faz sentido misturar SQL Server + SoftcomShop
                    cmd.CommandText = "DELETE FROM Mercadorias";
                    cmd.ExecuteNonQuery();

                    // Também limpar ProdutosSelecionados para evitar referências quebradas
                    cmd.CommandText = "DELETE FROM ProdutosSelecionados";
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void LimparEtiquetas()
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "UPDATE Mercadorias SET GerarEtiqueta = 0, QuantidadeEtiqueta = 1";
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private bool ProdutoExiste(SQLiteConnection conn, long produtoId, string codBarrasGrade)
        {
            return ProdutoExiste(conn, produtoId, codBarrasGrade, null, null);
        }

        private bool ProdutoExiste(SQLiteConnection conn, long produtoId, string codBarrasGrade, string codigoMercadoria, string codBarras)
        {
            using (var cmd = new SQLiteCommand(conn))
            {
                var condicoes = new List<string>();
                AdicionarCondicoesProduto(cmd, condicoes, produtoId, codBarrasGrade, codigoMercadoria, codBarras);

                if (condicoes.Count == 0)
                    return false;

                cmd.CommandText = "SELECT COUNT(*) FROM Mercadorias WHERE " + string.Join(" OR ", condicoes);
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        private int MarcarParaImpressao(SQLiteConnection conn, long produtoId, string codBarrasGrade, int quantidade)
        {
            return MarcarParaImpressao(conn, produtoId, codBarrasGrade, quantidade, null, null);
        }

        private int MarcarParaImpressao(SQLiteConnection conn, long produtoId, string codBarrasGrade, int quantidade, string codigoMercadoria, string codBarras)
        {
            using (var cmd = new SQLiteCommand(conn))
            {
                var condicoes = new List<string>();
                AdicionarCondicoesProduto(cmd, condicoes, produtoId, codBarrasGrade, codigoMercadoria, codBarras);

                if (condicoes.Count == 0)
                    return 0;

                cmd.CommandText = @"
                UPDATE Mercadorias 
                SET GerarEtiqueta = 1, QuantidadeEtiqueta = @qtd
                WHERE " + string.Join(" OR ", condicoes);

                cmd.Parameters.AddWithValue("@qtd", Math.Max(1, quantidade));
                return cmd.ExecuteNonQuery();
            }
        }

        private int AtualizarProdutoMovimento(SQLiteConnection conn, JToken produto, string campoQuantidade)
        {
            long produtoId = LerLong(produto["produto_id"]);
            string codigoMercadoria = ObterCodigoMercadoria(produto, produtoId);
            string codBarras = ObterTexto(produto["codigo_barras"]);
            string codBarrasGrade = ObterTexto(produto["codigo_barras_grade"]);
            int quantidade = Math.Max(1, LerInteiro(produto[campoQuantidade], 1));
            string textoPreco = ObterTexto(produto["preco_venda"]);
            bool temPreco = !string.IsNullOrWhiteSpace(textoPreco);
            decimal preco = LerDecimal(produto["preco_venda"], 0m);

            using (var cmd = new SQLiteCommand(conn))
            {
                var condicoes = new List<string>();
                AdicionarCondicoesProduto(cmd, condicoes, produtoId, codBarrasGrade, codigoMercadoria, codBarras);

                if (condicoes.Count == 0)
                    return 0;

                var campos = new List<string>
                {
                    "UltimaAtualizacao = @data",
                    "Origem = 'SOFTCOMSHOP'",
                    "GerarEtiqueta = 1",
                    "QuantidadeEtiqueta = @qtd"
                };

                if (temPreco)
                {
                    campos.Insert(0, "PrecoVenda = @preco");
                    cmd.Parameters.AddWithValue("@preco", preco);
                }

                AdicionarCamposClassificacaoProduto(cmd, campos, produto);

                cmd.CommandText = @"
                UPDATE Mercadorias 
                SET " + string.Join(", ", campos) + @"
                WHERE " + string.Join(" OR ", condicoes);

                cmd.Parameters.AddWithValue("@data", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@qtd", quantidade);

                return cmd.ExecuteNonQuery();
            }
        }

        private int AtualizarProdutoNF(SQLiteConnection conn, JToken produto, string versao)
        {
            long produtoId = LerLong(produto["produto_id"]);
            string codigoMercadoria = ObterCodigoMercadoria(produto, produtoId);
            string codBarras = ObterTexto(produto["codigo_barras"]);
            string codBarrasGrade = ObterTexto(produto["codigo_barras_grade"]);
            int quantidade = Math.Max(1, LerInteiro(produto["compra_item_quantidade"], 1));
            decimal preco = LerDecimal(produto["preco_venda"], 0m);

            using (var cmd = new SQLiteCommand(conn))
            {
                var condicoes = new List<string>();
                AdicionarCondicoesProduto(cmd, condicoes, produtoId, codBarrasGrade, codigoMercadoria, codBarras);

                if (condicoes.Count == 0)
                    return 0;

                var campos = new List<string>
                {
                    "PrecoVenda = @preco",
                    "UltimaAtualizacao = @data",
                    "GerarEtiqueta = 1",
                    "QuantidadeEtiqueta = @qtd"
                };

                AdicionarCamposClassificacaoProduto(cmd, campos, produto);

                cmd.CommandText = @"
                UPDATE Mercadorias 
                SET " + string.Join(", ", campos) + @"
                WHERE " + string.Join(" OR ", condicoes);

                cmd.Parameters.AddWithValue("@preco", preco);
                cmd.Parameters.AddWithValue("@data", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@qtd", quantidade);

                return cmd.ExecuteNonQuery();
            }
        }

        private static bool TemIdentificacaoProduto(long produtoId, string codigoMercadoria, string codBarras, string codBarrasGrade)
        {
            return produtoId > 0 ||
                   !string.IsNullOrWhiteSpace(codigoMercadoria) ||
                   !string.IsNullOrWhiteSpace(codBarras) ||
                   !string.IsNullOrWhiteSpace(codBarrasGrade);
        }

        private static string ObterCodigoMercadoria(JToken produto, long produtoId)
        {
            string codigo = PrimeiroTexto(
                produto?["codigo_mercadoria"],
                produto?["codigo_produto"],
                produto?["mercadoria_codigo"],
                produto?["produto_id"],
                produto?["id_produto"],
                produto?["mercadoria_id"],
                produto?["id"]);

            if (string.IsNullOrWhiteSpace(codigo) && produtoId > 0)
                codigo = produtoId.ToString(CultureInfo.InvariantCulture);

            return codigo ?? "";
        }

        private static string PrimeiroTexto(params JToken[] valores)
        {
            foreach (var valor in valores)
            {
                string texto = ObterTexto(valor);
                if (!string.IsNullOrWhiteSpace(texto))
                    return texto;
            }

            return "";
        }

        private static string PrimeiroTextoValor(params string[] valores)
        {
            foreach (var valor in valores)
            {
                if (!string.IsNullOrWhiteSpace(valor))
                    return valor.Trim();
            }

            return "";
        }

        private static string ObterFabricanteProduto(JToken produto)
        {
            return PrimeiroTextoValor(
                ObterCampoComoTexto(produto, "marca_nome"),
                ObterCampoComoTexto(produto, "fabricante"),
                ObterCampoComoTexto(produto, "fabricante_nome"),
                ObterCampoComoTexto(produto, "nome_fabricante"),
                ObterCampoComoTexto(produto, "marca"),
                ObterCampoComoTexto(produto, "marca_descricao"),
                ObterCampoComoTexto(produto, "descricao_marca"),
                ObterCampoComoTexto(produto, "brand"),
                ObterCampoComoTexto(produto, "brand_name"),
                ObterCampoComoTexto(produto, "FABRICANTE"));
        }

        private static string ObterGrupoProduto(JToken produto)
        {
            return PrimeiroTextoValor(
                ObterCampoComoTexto(produto, "grupo_nome"),
                ObterCampoComoTexto(produto, "grupo_descricao"),
                ObterCampoComoTexto(produto, "descricao_grupo"),
                ObterCampoComoTexto(produto, "nome_grupo"),
                ObterCampoComoTexto(produto, "grupo_produto"),
                ObterCampoComoTexto(produto, "grupo"),
                ObterCampoComoTexto(produto, "categoria"),
                ObterCampoComoTexto(produto, "categoria_nome"),
                ObterCampoComoTexto(produto, "departamento"),
                ObterCampoComoTexto(produto, "GRUPO"));
        }

        private static string ObterObservacaoProduto(JToken produto)
        {
            return PrimeiroTextoValor(
                ObterCampoComoTexto(produto, "observacao"),
                ObterCampoComoTexto(produto, "observacao_produto"),
                ObterCampoComoTexto(produto, "produto_observacao"),
                ObterCampoComoTexto(produto, "observação"),
                ObterCampoComoTexto(produto, "observacoes"),
                ObterCampoComoTexto(produto, "obs"),
                ObterCampoComoTexto(produto, "OBSERVACAO"),
                ObterCampoComoTexto(produto, "OBSERVAÇÃO"));
        }

        private static void AdicionarCamposClassificacaoProduto(SQLiteCommand cmd, List<string> campos, JToken produto)
        {
            string fabricante = ObterFabricanteProduto(produto);
            if (!string.IsNullOrWhiteSpace(fabricante))
            {
                campos.Add("Fabricante = @fabricante");
                cmd.Parameters.AddWithValue("@fabricante", fabricante);
            }

            string grupo = ObterGrupoProduto(produto);
            if (!string.IsNullOrWhiteSpace(grupo))
            {
                campos.Add("Grupo = @grupo");
                cmd.Parameters.AddWithValue("@grupo", grupo);
            }

            string observacao = ObterObservacaoProduto(produto);
            if (!string.IsNullOrWhiteSpace(observacao))
            {
                campos.Add("Observacao = @observacao");
                cmd.Parameters.AddWithValue("@observacao", observacao);
            }
        }

        private static string ObterCampoComoTexto(JToken token, string nomeCampo)
        {
            return ObterTextoClassificacao(ObterCampo(token, nomeCampo));
        }

        private static JToken ObterCampo(JToken token, string nomeCampo)
        {
            var obj = token as JObject;
            if (obj == null || string.IsNullOrWhiteSpace(nomeCampo))
                return null;

            foreach (var prop in obj.Properties())
            {
                if (string.Equals(prop.Name, nomeCampo, StringComparison.OrdinalIgnoreCase))
                    return prop.Value;
            }

            return null;
        }

        private static string ObterTextoClassificacao(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null)
                return "";

            if (token is JValue)
                return token.ToString().Trim();

            var array = token as JArray;
            if (array != null)
            {
                foreach (var item in array)
                {
                    string texto = ObterTextoClassificacao(item);
                    if (!string.IsNullOrWhiteSpace(texto))
                        return texto;
                }

                return "";
            }

            var obj = token as JObject;
            if (obj != null)
            {
                return PrimeiroTextoValor(
                    ObterCampoComoTexto(obj, "nome"),
                    ObterCampoComoTexto(obj, "descricao"),
                    ObterCampoComoTexto(obj, "descrição"),
                    ObterCampoComoTexto(obj, "GRP_DESCRI"),
                    ObterCampoComoTexto(obj, "valor"),
                    ObterCampoComoTexto(obj, "value"),
                    ObterCampoComoTexto(obj, "name"),
                    ObterCampoComoTexto(obj, "description"));
            }

            return token.ToString().Trim();
        }

        private static string ObterTexto(JToken token)
        {
            return token == null || token.Type == JTokenType.Null
                ? ""
                : token.ToString().Trim();
        }

        private static long LerLong(JToken token, long valorPadrao = 0)
        {
            string texto = ObterTexto(token);
            if (string.IsNullOrWhiteSpace(texto))
                return valorPadrao;

            if (long.TryParse(texto, NumberStyles.Integer, CultureInfo.InvariantCulture, out long valor))
                return valor;

            if (decimal.TryParse(NormalizarNumeroDecimal(texto), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal valorDecimal) &&
                valorDecimal >= long.MinValue &&
                valorDecimal <= long.MaxValue)
            {
                return Convert.ToInt64(Math.Truncate(valorDecimal));
            }

            return valorPadrao;
        }

        private static int LerInteiro(JToken token, int valorPadrao = 0)
        {
            decimal valorDecimal = LerDecimal(token, valorPadrao);
            if (valorDecimal <= 0)
                return valorPadrao;

            if (valorDecimal > int.MaxValue)
                return int.MaxValue;

            return Convert.ToInt32(Math.Ceiling(valorDecimal));
        }

        private static decimal LerDecimal(JToken token, decimal valorPadrao = 0m)
        {
            string texto = ObterTexto(token);
            if (string.IsNullOrWhiteSpace(texto))
                return valorPadrao;

            string normalizado = NormalizarNumeroDecimal(texto);
            return decimal.TryParse(normalizado, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal valor)
                ? valor
                : valorPadrao;
        }

        private static string NormalizarNumeroDecimal(string texto)
        {
            texto = (texto ?? "").Trim();
            int ultimoPonto = texto.LastIndexOf('.');
            int ultimaVirgula = texto.LastIndexOf(',');

            if (ultimoPonto >= 0 && ultimaVirgula >= 0)
            {
                return ultimoPonto > ultimaVirgula
                    ? texto.Replace(",", "")
                    : texto.Replace(".", "").Replace(",", ".");
            }

            if (ultimaVirgula >= 0)
                return texto.Replace(".", "").Replace(",", ".");

            return texto;
        }

        private static void AdicionarCondicoesProduto(SQLiteCommand cmd, List<string> condicoes, long produtoId, string codBarrasGrade, string codigoMercadoria, string codBarras)
        {
            if (!string.IsNullOrWhiteSpace(codBarrasGrade))
            {
                condicoes.Add("CodBarras_Grade = @codBarrasGrade");
                cmd.Parameters.AddWithValue("@codBarrasGrade", codBarrasGrade.Trim());
                return;
            }

            if (produtoId > 0)
            {
                condicoes.Add("ID_SoftcomShop = @id");
                cmd.Parameters.AddWithValue("@id", produtoId);
            }

            if (!string.IsNullOrWhiteSpace(codigoMercadoria))
            {
                condicoes.Add("CAST(CodigoMercadoria AS TEXT) = @codigoMercadoria");
                cmd.Parameters.AddWithValue("@codigoMercadoria", codigoMercadoria.Trim());

                long codigoNumerico = LerLong(new JValue(codigoMercadoria));
                if (codigoNumerico > 0)
                {
                    condicoes.Add("CodigoMercadoria = @codigoMercadoriaNumero");
                    cmd.Parameters.AddWithValue("@codigoMercadoriaNumero", codigoNumerico);
                }
            }

            if (!string.IsNullOrWhiteSpace(codBarras))
            {
                condicoes.Add("CodBarras = @codBarras");
                cmd.Parameters.AddWithValue("@codBarras", codBarras.Trim());
            }
        }

        private void AtualizarTimestamp(string timestamp)
        {
            // Salvar timestamp da última sincronização
            var config = ConfiguracaoSistema.Carregar();
            config.SoftcomShop.DataSync = timestamp;
            config.Salvar();
        }

        //public async Task SincronizarPromocoesAtivasAsync()
        //{
        //    try
        //    {
        //        string jsonResponse = await _service.GetPromocoesAsync();
        //        if (string.IsNullOrEmpty(jsonResponse)) return;

        //        var response = JToken.Parse(jsonResponse);
        //        // No seu JSON, os dados estão dentro de "data"
        //        var listaPromocoes = response["data"] ?? response["produtos"] ?? response;

        //        using (var conn = new SQLiteConnection(_connectionString))
        //        {
        //            await conn.OpenAsync();

        //            // 1. GARANTIR ESTRUTURA (Paridade com Access)
        //            using (var cmdTable = new SQLiteCommand(@"
        //        CREATE TABLE IF NOT EXISTS Promocoes (
        //            ID_Promocao INTEGER PRIMARY KEY,
        //            Descricao TEXT,
        //            Data_Hora_Inicio TEXT,
        //            Data_Hora_Fim TEXT,
        //            Status TEXT
        //        )", conn))
        //            {
        //                await cmdTable.ExecuteNonQueryAsync();
        //            }

        //            using (var transaction = conn.BeginTransaction())
        //            {
        //                // 2. LIMPEZA PARA ATUALIZAÇÃO
        //                using (var cmdResetMerc = new SQLiteCommand("UPDATE Mercadorias SET EmPromocao = 0, PrecoPromocional = 0", conn))
        //                    await cmdResetMerc.ExecuteNonQueryAsync();

        //                using (var cmdClearPromo = new SQLiteCommand("DELETE FROM Promocoes", conn))
        //                    await cmdClearPromo.ExecuteNonQueryAsync();

        //                // 3. ALIMENTAR AS TABELAS
        //                foreach (var item in listaPromocoes)
        //                {
        //                    // --- A: SALVAR NA TABELA PROMOCOES (Para a ComboBox) ---
        //                    string promoId = item["id"]?.ToString() ?? "0";
        //                    string promoDesc = item["descricao"]?.ToString()?.ToUpper() ?? "PROMOÇÃO SEM NOME";

        //                    using (var cmdPromo = new SQLiteCommand(@"
        //                INSERT INTO Promocoes (ID_Promocao, Descricao, Data_Hora_Inicio, Data_Hora_Fim, Status) 
        //                VALUES (@id, @desc, @ini, @fim, @status)", conn))
        //                    {
        //                        cmdPromo.Parameters.AddWithValue("@id", promoId);
        //                        cmdPromo.Parameters.AddWithValue("@desc", promoDesc);
        //                        cmdPromo.Parameters.AddWithValue("@ini", item["data_hora_inicio"]?.ToString());
        //                        cmdPromo.Parameters.AddWithValue("@fim", item["data_hora_fim"]?.ToString());
        //                        cmdPromo.Parameters.AddWithValue("@status", item["status"]?.ToString());
        //                        await cmdPromo.ExecuteNonQueryAsync();
        //                    }

        //                    // --- B: ATUALIZAR MERCADORIAS (Para o Grid/Etiquetas) ---
        //                    var subItens = item["itens"] as JArray ?? new JArray(item);
        //                    foreach (var promo in subItens)
        //                    {
        //                        string idProdApi = (promo["produto_id"] ?? promo["id"])?.ToString() ?? "";
        //                        string precoBruto = (promo["valor_promocional_unidade"] ?? promo["preco_venda"])?.ToString() ?? "0";

        //                        decimal preco = 0;
        //                        decimal.TryParse(precoBruto.Replace(",", "."),
        //                            System.Globalization.NumberStyles.Any,
        //                            System.Globalization.CultureInfo.InvariantCulture, out preco);

        //                        if (preco > 0)
        //                        {
        //                            //    string sqlUpdate = @"
        //                            //UPDATE Mercadorias 
        //                            //SET EmPromocao = 1, PrecoPromocional = @p, Origem = 'PROMOCAO'
        //                            //WHERE ID_SoftcomShop = @id OR CodigoMercadoria = @id";
        //                            string sqlUpdate = @"
        //                                UPDATE Mercadorias 
        //                                SET EmPromocao = 1, 
        //                                    PrecoPromocional = @p, 
        //                                    Origem = 'PROMOCAO',
        //                                    ID_Promocao = @idPromo 
        //                                WHERE ID_SoftcomShop = @id OR CodigoMercadoria = @id";

        //                            using (var cmdUpd = new SQLiteCommand(sqlUpdate, conn))
        //                            {
        //                                cmdUpd.Parameters.AddWithValue("@p", preco);
        //                                cmdUpd.Parameters.AddWithValue("@id", idProdApi);
        //                                await cmdUpd.ExecuteNonQueryAsync();
        //                            }
        //                        }
        //                    }
        //                }
        //                transaction.Commit();
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        System.Diagnostics.Debug.WriteLine("Erro Sincronização: " + ex.Message);
        //    }
        //}

        public async Task SincronizarPromocoesAtivasAsync()
        {
            try
            {
                string jsonResponse = await _service.GetPromocoesAsync();
                if (string.IsNullOrEmpty(jsonResponse)) return;

                var response = JToken.Parse(jsonResponse);
                var listaPromocoes = response["data"] ?? response["produtos"] ?? response;

                using (var conn = new SQLiteConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    // --- NOVO: GARANTIR QUE A COLUNA ID_Promocao EXISTE NA TABELA MERCADORIAS ---
                    try
                    {
                        using (var cmdAlter = new SQLiteCommand("ALTER TABLE Mercadorias ADD COLUMN ID_Promocao INTEGER", conn))
                        {
                            await cmdAlter.ExecuteNonQueryAsync();
                        }
                    }
                    catch { /* Coluna já existe, ignora o erro */ }

                    // 1. GARANTIR ESTRUTURA DA TABELA DE PROMOÇÕES
                    using (var cmdTable = new SQLiteCommand(@"
                CREATE TABLE IF NOT EXISTS Promocoes (
                    ID_Promocao INTEGER PRIMARY KEY,
                    Descricao TEXT,
                    Data_Hora_Inicio TEXT,
                    Data_Hora_Fim TEXT,
                    Status TEXT
                )", conn))
                    {
                        await cmdTable.ExecuteNonQueryAsync();
                    }

                    using (var transaction = conn.BeginTransaction())
                    {
                        // 2. LIMPEZA PARA ATUALIZAÇÃO (Resetamos o ID_Promocao também)
                        using (var cmdResetMerc = new SQLiteCommand("UPDATE Mercadorias SET EmPromocao = 0, PrecoPromocional = 0, ID_Promocao = NULL", conn))
                        {
                            await cmdResetMerc.ExecuteNonQueryAsync();
                        }

                        using (var cmdClearPromo = new SQLiteCommand("DELETE FROM Promocoes", conn))
                        {
                            await cmdClearPromo.ExecuteNonQueryAsync();
                        }

                        // 3. ALIMENTAR AS TABELAS
                        foreach (var item in listaPromocoes)
                        {
                            string promoId = item["id"]?.ToString() ?? "0";
                            string promoDesc = item["descricao"]?.ToString()?.ToUpper() ?? "PROMOÇÃO SEM NOME";

                            // A: SALVAR NA TABELA PROMOCOES
                            using (var cmdPromo = new SQLiteCommand(@"
                        INSERT INTO Promocoes (ID_Promocao, Descricao, Data_Hora_Inicio, Data_Hora_Fim, Status) 
                        VALUES (@id, @desc, @ini, @fim, @status)", conn))
                            {
                                cmdPromo.Parameters.AddWithValue("@id", promoId);
                                cmdPromo.Parameters.AddWithValue("@desc", promoDesc);
                                cmdPromo.Parameters.AddWithValue("@ini", item["data_hora_inicio"]?.ToString());
                                cmdPromo.Parameters.AddWithValue("@fim", item["data_hora_fim"]?.ToString());
                                cmdPromo.Parameters.AddWithValue("@status", item["status"]?.ToString());
                                await cmdPromo.ExecuteNonQueryAsync();
                            }

                            // B: ATUALIZAR MERCADORIAS VINCULANDO AO ID DA PROMOÇÃO
                            var subItens = item["itens"] as JArray ?? new JArray(item);
                            foreach (var promo in subItens)
                            {
                                string idProdApi = (promo["produto_id"] ?? promo["id"])?.ToString() ?? "";
                                string precoBruto = (promo["valor_promocional_unidade"] ?? promo["preco_venda"])?.ToString() ?? "0";

                                decimal preco = 0;
                                decimal.TryParse(precoBruto.Replace(",", "."),
                                    System.Globalization.NumberStyles.Any,
                                    System.Globalization.CultureInfo.InvariantCulture, out preco);

                                if (preco > 0)
                                {
                                    string sqlUpdate = @"
                                UPDATE Mercadorias 
                                SET EmPromocao = 1, 
                                    PrecoPromocional = @p, 
                                    Origem = 'PROMOCAO',
                                    ID_Promocao = @idPromo 
                                WHERE ID_SoftcomShop = @id OR CodigoMercadoria = @id";

                                    using (var cmdUpd = new SQLiteCommand(sqlUpdate, conn))
                                    {
                                        cmdUpd.Parameters.AddWithValue("@p", preco);
                                        cmdUpd.Parameters.AddWithValue("@id", idProdApi);
                                        cmdUpd.Parameters.AddWithValue("@idPromo", promoId); // <-- Vínculo corrigido
                                        await cmdUpd.ExecuteNonQueryAsync();
                                    }
                                }
                            }
                        }
                        transaction.Commit();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Erro Sincronização: " + ex.Message);
            }
        }

        public async Task SincronizarPromocoesAsync()
        {
            string json = await _service.GetPromocoesAsync();
            var response = JObject.Parse(json);
            var promocoes = response["data"] as JArray;

            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();

                using (var cmd = conn.CreateCommand())
                {
                    // Limpa promoções antigas
                    cmd.CommandText = "DELETE FROM Promocoes";
                    cmd.ExecuteNonQuery();
                }

                foreach (var promo in promocoes)
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
                    INSERT INTO Promocoes (ID_Promocao, Descricao)
                    VALUES (@id, @desc)";

                        cmd.Parameters.AddWithValue("@id", promo["id"]?.ToObject<int>() ?? 0);
                        cmd.Parameters.AddWithValue("@desc", promo["descricao"]?.ToString() ?? "");

                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }


        #endregion
    }

    /// <summary>
    /// Resultado de uma operação de sincronização
    /// </summary>
    public class SyncResult
    {
        public bool Sucesso { get; set; }
        public int ProdutosAdicionados { get; set; }
        public int ProdutosAtualizados { get; set; }
        public string MensagemErro { get; set; }
    }
}
