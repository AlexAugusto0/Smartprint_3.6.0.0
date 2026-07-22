using System;
using System.Data;
using System.Data.SQLite;
using System.IO;

namespace EtiquetaFORNew.Data
{
    /// <summary>
    /// Extensões do LocalDatabaseManager para suportar novo sistema de carregamento
    /// </summary>
    public static class LocalDatabaseManagerExtensions
    {
        // ⭐ ConnectionString local (mesmo do LocalDatabaseManager)
        private static readonly string DbPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "LocalData.db");
        private static readonly string ConnectionString = $"Data Source={DbPath};Version=3;";

        /// <summary>
        /// Busca subgrupos de um grupo específico
        /// NOTA: Campo SubGrupo não existe na tabela atual
        /// Retorna DataTable vazio até que o campo seja adicionado
        /// </summary>
        public static DataTable ObterSubGruposPorGrupo(string grupo)
        {
            // TODO: Implementar quando campo SubGrupo for adicionado à tabela
            return new DataTable();

            /* CÓDIGO PARA QUANDO O CAMPO EXISTIR:
            try
            {
                using (var conn = new SQLiteConnection(ConnectionString))
                {
                    conn.Open();

                    string query = @"
                        SELECT DISTINCT SubGrupo
                        FROM Mercadorias
                        WHERE Grupo = @grupo
                        AND SubGrupo IS NOT NULL
                        AND SubGrupo != ''
                        ORDER BY SubGrupo
                    ";

                    using (var cmd = new SQLiteCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@grupo", grupo);

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
                throw new Exception($"Erro ao obter subgrupos do grupo '{grupo}': {ex.Message}", ex);
            }
            */
        }

        /// <summary>
        /// Versão estendida do BuscarMercadoriasPorFiltros
        /// Por enquanto é um wrapper do método existente
        /// </summary>
        public static DataTable BuscarMercadoriasPorFiltrosExtendido(
            string grupo = null,
            string fabricante = null,
            string fornecedor = null,
            bool isConfeccao = false,
            string subGrupo = null,
            string produto = null,
            DateTime? dataInicial = null,
            DateTime? dataFinal = null,
            int limite = 10000)
        {
            // Por enquanto, ignora subGrupo, produto e datas
            // e chama o método existente do LocalDatabaseManager
            return LocalDatabaseManager.BuscarMercadoriasPorFiltrosManuais(
                grupo,
                fabricante,
                fornecedor,
                isConfeccao
            );
        }

        /// <summary>
        /// Busca produtos específicos pelo nome/código
        /// </summary>
        public static DataTable BuscarProdutos(string termo, int limite = 100)
        {
            try
            {
                using (var conn = new SQLiteConnection(ConnectionString))
                {
                    conn.Open();

                    string query = @"
                        SELECT 
                            CodigoMercadoria,
                            Mercadoria,
                            CodBarras,
                            PrecoVenda
                        FROM Mercadorias
                        WHERE Mercadoria LIKE @termo
                        OR CodigoMercadoria LIKE @termo
                        OR CodBarras LIKE @termo
                        ORDER BY Mercadoria
                        LIMIT @limite
                    ";

                    using (var cmd = new SQLiteCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@termo", $"%{termo}%");
                        cmd.Parameters.AddWithValue("@limite", limite);

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
                throw new Exception($"Erro ao buscar produtos: {ex.Message}", ex);
            }
        }
    }
}