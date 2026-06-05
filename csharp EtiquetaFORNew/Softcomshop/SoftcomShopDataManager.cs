// Dentro do método ProcessarProdutosNotaFiscal, logo no início do foreach:
foreach (var produto in produtos)
{
    // ⭐ LOG CIRÚRGICO - Ver o que vem do JSON
    long produtoId = produto["produto_id"].ToObject<long>();
    string codBarrasGrade = produto["codigo_barras_grade"]?.ToString() ?? "";
    int quantidade = produto["compra_item_quantidade"]?.ToObject<int>() ?? 1;
    
    System.Diagnostics.Debug.WriteLine($"[ProcessarProdutosNotaFiscal] Item lido: ID={produtoId}, CodBarrasGrade={codBarrasGrade}, Qtd={quantidade}");

    // Resto do código existente...
    if (ProdutoExiste(conn, produtoId, codBarrasGrade))
    {
        System.Diagnostics.Debug.WriteLine($"[ProcessarProdutosNotaFiscal] Produto já existe (ID={produtoId}). Atualizando...");
        AtualizarProdutoNF(conn, produto, versao);
    }
    else
    {
        System.Diagnostics.Debug.WriteLine($"[ProcessarProdutosNotaFiscal] Produto NOVO (ID={produtoId}). Inserindo...");
        InserirProduto(conn, produto, versao);
        
        // ⭐ LOG CIRÚRGICO - Ver se MarcarParaImpressao foi chamado
        System.Diagnostics.Debug.WriteLine($"[ProcessarProdutosNotaFiscal] Marcando para impressão: ID={produtoId}");
        MarcarParaImpressao(conn, produtoId, codBarrasGrade, quantidade);
    }

    count++;
}