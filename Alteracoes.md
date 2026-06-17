# Alteracoes

## 2026-06-16

- Criada a rotina de calculo para campos de preco em `EtiquetaFORNew/CalculadoraCamposEtiqueta.cs`.
  - Campos suportados: `Preco`, `PrecoVenda`, `VendaA`, `VendaB`, `VendaC`, `VendaD`, `VendaE`, `PrecoOriginal` e `PrecoPromocional`.
  - Operadores suportados: adicao (`+`), subtracao (`-`), multiplicacao (`*`) e divisao (`/`).
  - A divisao por zero nao gera excecao; nesse caso o valor base e mantido.
  - O reconhecimento do campo tolera variacoes como `precovenda`, `preco_venda` e `Venda A`.

- Atualizado `EtiquetaFORNew/Classes.cs`.
  - Adicionadas as propriedades `OperadorCalculoPreco` e `ValorCalculoPreco` em `ElementoEtiqueta`.
  - O padrao continua sem calculo para preservar templates existentes.

- Atualizado `EtiquetaFORNew/TemplateManager.cs`.
  - As novas propriedades de calculo agora sao salvas e carregadas no JSON do template.
  - O historico de desfazer/refazer tambem passa a preservar a configuracao do calculo.

- Atualizado `EtiquetaFORNew/Forms/FormDesignNovo.cs`.
  - O painel de propriedades passa a exibir a secao "Calculo de Preco" quando o elemento selecionado e um campo de preco.
  - Adicionado seletor de operador com `Nenhum`, `+`, `-`, `*` e `/`.
  - Adicionado campo numerico para o valor usado no calculo.
  - A visualizacao do designer mostra o campo com a operacao quando nao ha produto carregado.

- Atualizados `EtiquetaFORNew/Forms/FormPreview.cs` e `EtiquetaFORNew/Forms/FormImpressao.cs`.
  - O valor calculado e aplicado ao desenhar campos de preco no preview e na impressao.
  - Sem operador configurado, a exibicao anterior dos campos foi mantida.

- Atualizado `EtiquetaFORNew/EtiquetaFORNew.csproj`.
  - Incluido `CalculadoraCamposEtiqueta.cs` na compilacao do projeto.

- Validacao:
  - `dotnet build EtiquetaFORNew.sln /p:GenerateResourceMSBuildArchitecture=CurrentArchitecture /p:GenerateResourceUsePreserializedResources=true`
  - Resultado: compilacao com exito, 0 erros.
  - Permaneceram warnings ja existentes no projeto, sem relacao com a rotina nova.
