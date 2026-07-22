using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace EtiquetaFORNew.Data
{
    public class SmartPrintRepository
    {
        private readonly string _connectionString;

        public SmartPrintRepository(string connectionString)
        {
            _connectionString = connectionString;
        }
        
        public bool TestConnection()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Cria a view vw_GeradorSmartPrint e a tabela MercEtiqueta caso não existam.
        /// </summary>
        public void VerificarECriarEstrutura()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    // 1️⃣ Criar tabela MercEtiqueta
                    string criarTabela = @"
            IF OBJECT_ID('dbo.MercEtiqueta', 'U') IS NULL
            BEGIN
                CREATE TABLE dbo.MercEtiqueta (
                    Registro INT IDENTITY(1,1) PRIMARY KEY,
                    [Código da Mercadoria] INT NOT NULL,
                    UsuarioId INT NOT NULL,
                    Tam NVARCHAR(50) NULL,
                    Cores NVARCHAR(50) NULL,
                    OutrasInformacoes NVARCHAR(MAX) NULL
                );
            END";
                    using (SqlCommand cmd = new SqlCommand(criarTabela, conn))
                        cmd.ExecuteNonQuery();

                    // 2️⃣ Criar função GetEtiquetaOrdemImpressao (fora do EXEC)
                    string criarFuncao = @"
            IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'GetEtiquetaOrdemImpressao' AND type = 'FN')
            BEGIN
                CREATE FUNCTION dbo.GetEtiquetaOrdemImpressao()
                RETURNS INT
                AS
                BEGIN
                    RETURN 1; -- Valor padrão, pode ser alterado conforme configuração
                END;
            END";
                    //        using (SqlCommand cmd = new SqlCommand(criarFuncao, conn))
                    //            cmd.ExecuteNonQuery();

                    //        // 3️⃣ Criar função dummy getUsuarioLogadoId para evitar erro (opcional)
                    //        string criarFuncaoUsuario = @"
                    //IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'getUsuarioLogadoId' AND type = 'FN')
                    //BEGIN
                    //    CREATE FUNCTION dbo.getUsuarioLogadoId()
                    //    RETURNS INT
                    //    AS
                    //    BEGIN
                    //        RETURN 1; -- ID fixo para testes
                    //    END;
                    //END";
                    //        using (SqlCommand cmd = new SqlCommand(criarFuncaoUsuario, conn))
                    //            cmd.ExecuteNonQuery();

                    // 4️⃣ Criar view após funções existirem
                    string criarView = @"
IF NOT EXISTS (SELECT * FROM sys.views WHERE name = 'vw_GeradorSmartPrint')
BEGIN
    EXEC('
        CREATE VIEW dbo.vw_GeradorSmartPrint AS
        SELECT 
            CASE WHEN 1 = 1 THEN M.Registro ELSE M.[Código da Mercadoria] END AS Ordem,
            M.[Código da Mercadoria] AS Codigo,
            CM.[Cód Fabricante] AS Referencia,
            CM.Mercadoria,
            CM.[Preço de Venda] AS Preco,
            M.Tam AS Tamanho,
            M.Cores AS Cor,
            E.TextoEtiqueta,
            CM.url,
            M.Registro,
            M.OutrasInformacoes
        FROM dbo.MercEtiqueta AS M
        INNER JOIN [Cadastro de mercadorias] AS CM
            ON CM.[Código da Mercadoria] = M.[Código da Mercadoria]
        INNER JOIN Empresa AS E ON 1=1
    ');
END";

                    using (SqlCommand cmd = new SqlCommand(criarView, conn))
                        cmd.ExecuteNonQuery();
                }

                MessageBox.Show("✅ Estrutura criada com sucesso (tabela, funções e view)!", "SmartPrint", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao verificar ou criar estrutura: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        /// <summary>
        /// Insere uma mercadoria na tabela MercEtiqueta.
        /// </summary>
        public void InserirItemEtiqueta(int codigoMercadoria, int usuarioId)
        {
            string query = @"
                INSERT INTO MercEtiqueta ([Código da Mercadoria], UsuarioId)
                VALUES (@Codigo, @UsuarioId)";
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Codigo", codigoMercadoria);
                        cmd.Parameters.AddWithValue("@UsuarioId", usuarioId);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao inserir mercadoria: {ex.Message}");
            }
        }

        /// <summary>
        /// Exclui todos os registros da tabela MercEtiqueta.
        /// </summary>
        public void LimparEtiquetas()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("DELETE FROM MercEtiqueta", conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao limpar tabela MercEtiqueta: {ex.Message}");
            }
        }

        /// <summary>
        /// Retorna todas as mercadorias na view vw_GeradorSmartPrint.
        /// </summary>
        public DataTable GetAllSmartPrintData()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = "SELECT * FROM vw_GeradorSmartPrint";
                    using (SqlDataAdapter da = new SqlDataAdapter(query, conn))
                    {
                        DataTable dt = new DataTable();
                        da.Fill(dt);
                        return dt;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao buscar dados da view SmartPrint: {ex.Message}");
            }
        }
    }
}
