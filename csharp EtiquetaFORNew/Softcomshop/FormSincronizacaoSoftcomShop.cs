// Dentro de btnBuscarNotaFiscal_Click, logo antes do MessageBox ou depois da busca:

if (syncResult.Sucesso)
{
    MessageBox.Show(
        $"Produtos carregados com sucesso!\n\n" +
        $"Total: {syncResult.ProdutosAdicionados} produtos\n\n" +
        $"Os produtos foram marcados para impressão de etiquetas.",
        "Sucesso",
        MessageBoxButtons.OK,
        MessageBoxIcon.Information);

    // ⭐ DIAGNÓSTICO - Comente ou remova depois
    CarregadorDados.DiagnosticarNotaFiscalSoftcomShop(numeroNota);

    lblStatus.Text = "Pronto";
}