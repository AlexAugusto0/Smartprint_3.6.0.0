using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace EtiquetaFORNew
{
    public static class Funcoes
    {
        // ==========================
        // 🔹 VARIÁVEIS GLOBAIS
        // ==========================
        public static int EtiquetaIdOrdem { get; set; } = 1; // Substitui basVariaveisUsuario.EtiquetaIdOrdem
        public static int UsuarioIdLogado { get; set; } = 0; // Substitui getUsuarioLogadoID
        public static string NomeUsuarioLogado { get; set; } = ""; // Opcional
        public static string StringConexao { get; set; } = "Server=SEU_SERVIDOR;Database=SEU_BANCO;Trusted_Connection=True;"; // Ajuste aqui


        // ==========================
        // 🔹 1. GetEtiquetaOrdemImpressao()
        // ==========================
        public static int GetEtiquetaOrdemImpressao()
        {
            // Retorna o identificador de ordem de impressão atual do usuário
            return EtiquetaIdOrdem;
        }


        // ==========================
        // 🔹 2. Util_getCampoEmpresa()
        // ==========================
        public static object Util_getCampoEmpresa(string campo, bool retNuloCasoErro = false)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(StringConexao))
                using (SqlCommand cmd = new SqlCommand($"SELECT TOP 1 [{campo}] FROM [Empresa]", conn))
                {
                    conn.Open();
                    object valor = cmd.ExecuteScalar();

                    // Se o campo não existir ou vier nulo
                    if (valor == null || valor == DBNull.Value)
                    {
                        return retNuloCasoErro ? null : "";
                    }

                    return valor;
                }
            }
            catch (Exception ex)
            {
                if (retNuloCasoErro)
                    return null;
                else
                    MessageBox.Show($"Erro no método Util_getCampoEmpresa: {ex.Message}",
                        "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }


        // ==========================
        // 🔹 3. getUsuarioLogadoID()
        // ==========================
        public static int getUsuarioLogadoID()
        {
            // Retorna o ID do usuário logado (salvo após o login)
            return UsuarioIdLogado;
        }


        // ==========================
        // 🔹 4. Executar SELECT genérico
        // ==========================
        public static DataTable ExecutarConsulta(string query)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(StringConexao))
                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    DataTable dt = new DataTable();
                    conn.Open();
                    da.Fill(dt);
                    return dt;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao executar consulta: {ex.Message}", "Erro SQL",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return new DataTable();
            }
        }


        // ==========================
        // 🔹 5. Exemplo: montar consulta com funções integradas
        // ==========================
        public static DataTable ObterEtiquetasDoUsuario()
        {
            int usuarioId = getUsuarioLogadoID();
            int ordem = GetEtiquetaOrdemImpressao();
            var origem = Util_getCampoEmpresa("EtiquetasOrigem")?.ToString() ?? "";

            string campoCodigo;

            switch (origem)
            {
                case "Codigo":
                    campoCodigo = "[Cadastro de Mercadorias].[Código da Mercadoria]";
                    break;
                case "BarrasNFe":
                    campoCodigo = "[Cadastro de Mercadorias].[Cód Barra]";
                    break;
                case "BarrasGrade":
                    campoCodigo = "[CodBarras]";
                    break;
                default:
                    campoCodigo = "[Cód Fabricante]";
                    break;
            }

            string query = $@"
                SELECT 
                    [Cadastro de Mercadorias].[Cód Fabricante], 
                    [Cadastro de Mercadorias].Mercadoria, 
                    CASE WHEN {ordem} = 1 THEN [Registro] ELSE [Gerador Etiquetas].[Código da Mercadoria] END AS Ordem,
                    [Gerador Etiquetas].*, 
                    {campoCodigo} AS CodigoImpressao,
                    CASE WHEN ISNULL({campoCodigo}, '') = '' THEN '*' ELSE '' END AS SemCampoPreenchido
                FROM [Gerador Etiquetas]
                INNER JOIN [Cadastro de Mercadorias]
                    ON [Gerador Etiquetas].[Código da Mercadoria] = [Cadastro de Mercadorias].[Código da Mercadoria]
                WHERE [Gerador Etiquetas].UsuarioId = {usuarioId}
                ORDER BY CASE WHEN {ordem} = 1 THEN [Registro] ELSE [Gerador Etiquetas].[Código da Mercadoria] END;
            ";

            return ExecutarConsulta(query);
        }
    }
}
