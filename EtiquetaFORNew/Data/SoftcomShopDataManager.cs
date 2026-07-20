using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
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
        private readonly string _connectionString;
        private readonly SoftcomShopService _softcomShopService;

        public SoftcomShopDataManager(SoftcomShopConfig config, string sqliteConnectionString)
        {
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

            // ⭐ CORREÇÃO: Tentar múltiplos campos para o nome do produto
            string nomeProduto = null;

            // Tentar campo "produto_nome"
            if (!string.IsNullOrWhiteSpace(produto["produto_nome"]?.ToString()))
            {
                nomeProduto = produto["produto_nome"].ToString();
            }
            // Se vazio, tentar "descricao"
            else if (!string.IsNullOrWhiteSpace(produto["descricao"]?.ToString()))
            {
                nomeProduto = produto["descricao"].ToString();
            }
            // Se vazio, tentar "nome"
            else if (!string.IsNullOrWhiteSpace(produto["nome"]?.ToString()))
            {
                nomeProduto = produto["nome"].ToString();
            }
            // Se vazio, tentar "produto_descricao"
            else if (!string.IsNullOrWhiteSpace(produto["produto_descricao"]?.ToString()))
            {
                nomeProduto = produto["produto_descricao"].ToString();
            }
            // Fallback: usar referência ou ID
            else
            {
                nomeProduto = !string.IsNullOrWhiteSpace(referencia)
                    ? referencia
                    : $"Produto {codigoMercadoria}";
            }

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
