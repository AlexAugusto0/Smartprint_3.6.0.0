# Historico de Alteracoes

Este arquivo registra as alteracoes realizadas no projeto, com objetivo, arquivos modificados, resumo tecnico e impactos.

## 2026-06-04 13:05:07 -03:00

### Objetivo da alteracao

Implementar o carregamento de Notas de Entrada no fluxo SoftcomShop, mantendo o uso do `FormFiltrosCarregamento` e preservando os fluxos existentes de Promocoes e SQL Server.

### Arquivos modificados

- `Alteracoes.md`
- `EtiquetaFORNew/CarregadorDados.cs`
- `EtiquetaFORNew/Data/SoftcomShopDataManager.cs`
- `EtiquetaFORNew/Softcomshop/SoftcomShopConfig.cs`
- `EtiquetaFORNew/Softcomshop/SoftcomShopService.cs`

### Metodos criados ou alterados

- Alterado `CarregadorDados.CarregarProdutosPorTipo(...)`
  - Passa a direcionar `NOTAS ENTRADA` para SoftcomShop apenas quando `ConfiguracaoSistema.TipoConexaoAtiva` for `SoftcomShop` e a configuracao estiver valida.
  - Mantem a chamada SQL Server existente quando o modo ativo nao for SoftcomShop.

- Criado `CarregadorDados.CarregarNotasEntradaSoftcomShop(string numeroNF)`
  - Valida o numero da NF.
  - Chama o gerenciador SoftcomShop para localizar os itens da nota.
  - Retorna uma `DataTable` no mesmo formato consumido por `FormPrincipal.AdicionarProdutoAoPanel`.
  - Usa os produtos marcados no SQLite com `GerarEtiqueta = 1` e respeita `QuantidadeEtiqueta`.

- Criado `CarregadorDados.EstaEmModoSoftcomShop()`
  - Centraliza a deteccao segura da origem ativa antes de desviar o fluxo de Notas de Entrada.

- Criado `SoftcomShopDataManager.BuscarPorNumeroNotaFiscalAsync(int numeroNota, IProgress<string> progress = null)`
  - Busca a NF por numero na API SoftcomShop.
  - Limpa marcacoes anteriores de etiqueta.
  - Processa paginas retornadas pela API.
  - Reaproveita `ProcessarProdutosNotaFiscal(...)` para inserir/atualizar produtos e marcar quantidade para impressao.

- Criado `SoftcomShopService.GetNotaFiscalPorNumeroAsync(int numeroNota, int page = 1)`
  - Consulta o endpoint v2 de compras filtrando por `numero_nota_fiscal`.

- Adicionado `SoftcomShopRouter.ComprasV2Router`
  - Expõe a rota base `/softauth/api/v2/produtos/compras`.

### Resumo tecnico

O carregamento de `NOTAS ENTRADA` agora segue dois caminhos:

- SQL Server: permanece usando `CarregarNotasEntrada(...)`, sem alteracao na consulta existente.
- SoftcomShop: usa a API para localizar a nota pelo numero informado, marca os produtos retornados no SQLite e monta uma `DataTable` com os campos ja utilizados pelo fluxo atual do `FormPrincipal`.

O fluxo da tela principal nao foi movido nem reestruturado. `FormPrincipal.btnCarregar_Click` continua chamando `CarregadorDados.CarregarProdutosPorTipo(...)` e adicionando os produtos por `AdicionarProdutoAoPanel(...)`.

### Impactos identificados

- Promocoes nao tiveram o caminho alterado.
- SQL Server nao teve a rotina de notas modificada.
- A implementacao depende de a API SoftcomShop aceitar consulta de compras pelo parametro `numero_nota_fiscal` sem data de entrada.
- A consulta final usa `GerarEtiqueta = 1`, portanto respeita a mesma marcacao ja usada pelas rotinas de nota/venda do `SoftcomShopDataManager`.

### Validacao realizada

- Build executado com MSBuild:
  - Comando: `MSBuild.exe EtiquetaFORNew\EtiquetaFORNew.csproj /t:Build /p:Configuration=Debug /p:Platform=AnyCPU /v:minimal`
  - Resultado: build concluido com sucesso.
  - Observacao: permaneceram warnings preexistentes de `using` duplicado, variaveis nao usadas e metodo async sem await.

### Pendencias ou pontos para validacao

- Validar em ambiente com credenciais SoftcomShop se o endpoint v2 retorna itens apenas com `numero_nota_fiscal`.
- Validar uma NF com grade/tamanho/cor para confirmar se `CodBarras_Grade`, `Tam` e `Cores` chegam corretamente no painel.
- Validar uma NF com mais de uma pagina de itens.

## 2026-06-04 13:14:00 -03:00

### Objetivo planejado

Corrigir o carregamento de Notas de Entrada no SoftcomShop para utilizar obrigatoriamente numero da nota e data de entrada, usando o filtro de data do `FormFiltrosCarregamento`.

### Analise antes da alteracao

- A alteracao anterior criou busca por numero da NF sem data, mas o historico ja registrava que dependia de validacao do endpoint.
- O usuario validou que a busca ficou em carregamento infinito e nao retornou itens nem mensagem final.
- O fluxo atual bloqueia uma chamada `async` usando `.GetAwaiter().GetResult()` dentro do carregamento acionado pela UI, o que pode causar deadlock/travamento em WinForms.
- Ja existe no `SoftcomShopDataManager` um metodo de referencia que busca NF por data de entrada e numero da nota: `BuscarPorNotaFiscalAsync(DateTime dataEntrada, int numeroNota, ...)`.

### Plano tecnico

- Ajustar `FormFiltrosCarregamento` para exibir e exigir filtro de data quando `NOTAS ENTRADA` estiver em modo SoftcomShop.
- Ajustar `CarregadorDados.CarregarNotasEntradaSoftcomShop` para receber a data de entrada e consultar SoftcomShop com data + numero da NF.
- Evitar bloqueio direto de chamada assincrona no contexto da UI, executando a busca SoftcomShop fora do contexto de sincronizacao da tela.
- Preservar o fluxo SQL Server existente para `NOTAS ENTRADA`.
- Preservar o fluxo atual de `PROMOCOES`.

## 2026-06-04 17:05:00 -03:00

### Objetivo da alteracao

Corrigir o carregamento de Notas de Entrada no modo SoftcomShop quando a API localiza a nota, mas nenhum produto aparece no painel.

### Arquivos modificados

- `Alteracoes.md`
- `EtiquetaFORNew/CarregadorDados.cs`
- `EtiquetaFORNew/Data/SoftcomShopDataManager.cs`

### Metodos alterados

- Alterado `CarregadorDados.CarregarProdutosPorTipo(...)`
  - Para `NOTAS ENTRADA` em modo SoftcomShop, passa a enviar a data inicial do filtro para a rotina especifica.
  - O caminho SQL Server permanece chamando `CarregarNotasEntrada(documento, dataInicial, dataFinal)`.

- Alterado `CarregadorDados.CarregarNotasEntradaSoftcomShop(string numeroNF, DateTime? dataEntrada)`
  - A data de entrada passou a ser obrigatoria no fluxo SoftcomShop.
  - A consulta agora usa `SoftcomShopDataManager.BuscarPorNotaFiscalAsync(dataEntrada, numeroNota)`, ou seja, data de entrada + numero da NF.
  - O fallback antigo que podia marcar produtos por `Origem = 'SOFTCOMSHOP'` fica bloqueado antes de executar, evitando carregar produtos que nao pertencem a nota.

- Revisado `SoftcomShopDataManager`
  - Restauradas as variaveis de paginacao da sincronizacao geral de produtos, removidas por engano durante a edicao.
  - Mantida a rotina existente `BuscarPorNotaFiscalAsync(...)`, que ja marca produtos existentes via `AtualizarProdutoNF(...)` e produtos novos via `MarcarParaImpressao(...)`.

### Resumo tecnico

O problema estava no desvio SoftcomShop para Notas de Entrada: o carregador ignorava a data informada no filtro e chamava a busca somente pelo numero da NF. Agora o fluxo usa o endpoint por `data_hora_entrada` + `numero_nota_fiscal`, que era a rotina ja existente para esse cenario.

Tambem foi impedida a execucao do fallback que marcava ate 200 registros recentes de origem SoftcomShop quando nada ficava com `GerarEtiqueta = 1`. Esse fallback poderia esconder o erro e adicionar produtos incorretos no painel.

### Impactos identificados

- Promocoes nao tiveram o fluxo alterado.
- SQL Server nao teve a rotina de notas modificada.
- O formulario ja exigia data quando em modo SoftcomShop; a mudanca principal foi fazer o carregador usar essa data.

### Validacao realizada

- Build executado com MSBuild:
  - Comando: `C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe EtiquetaFORNew\EtiquetaFORNew.csproj /t:Build /p:Configuration=Debug /p:Platform=AnyCPU /v:minimal`
  - Resultado: build concluido com sucesso.
  - Observacao: permaneceram apenas warnings preexistentes de `using` duplicado, variaveis/campos nao usados e metodo async sem await.

### Pendencias ou pontos para validacao

- Validar em ambiente SoftcomShop real uma NF informando exatamente a data de entrada usada pela API.
- Se a API retornar uma nota com itens em mais de uma pagina para a busca por data, avaliar paginacao tambem em `BuscarPorNotaFiscalAsync(...)`.

## 2026-06-04 17:57:18 -03:00

### Objetivo da alteracao

Corrigir o erro "A cadeia de caracteres de entrada nao estava no formato correto" ao carregar Notas de Entrada no modo SoftcomShop.

### Arquivos modificados

- `Alteracoes.md`
- `EtiquetaFORNew/Data/SoftcomShopDataManager.cs`

### Metodos alterados

- Alterado `SoftcomShopDataManager.BuscarPorNotaFiscalAsync(...)`
  - Quando a API retorna itens, mas nenhum item possui identificador suficiente para gravar ou marcar no SQLite, passa a retornar uma mensagem especifica.

- Alterado `SoftcomShopDataManager.ProcessarProdutosNotaFiscal(...)`
  - Remove conversoes diretas com `ToObject<long>()` e `ToObject<int>()` no fluxo de Nota Fiscal.
  - Passa a ler `produto_id`, quantidade, codigo da mercadoria e codigos de barras com helpers tolerantes a texto vazio, numeros decimais e formatos com ponto/virgula.
  - Passa a procurar produto local por `ID_SoftcomShop`, `CodigoMercadoria`, `CodBarras` ou `CodBarras_Grade`.

- Alterado `SoftcomShopDataManager.InserirProduto(...)`
  - Usa leitura segura para `produto_id` e `preco_venda`.
  - Preserva codigo de mercadoria textual quando a API nao envia um `produto_id` numerico.

- Alterados `ProdutoExiste(...)`, `MarcarParaImpressao(...)` e `AtualizarProdutoNF(...)`
  - Mantidas as assinaturas usadas por fluxos existentes.
  - Adicionadas sobrecargas internas para localizar/marcar produtos por multiplos identificadores.
  - Quando `CodBarras_Grade` existe, ele passa a ser usado como identificador prioritario para evitar marcar outras grades do mesmo produto.

### Resumo tecnico

O erro era compativel com conversoes numericas diretas em campos vindos da API SoftcomShop. Agora o carregamento de Nota nao depende mais de `produto_id` obrigatoriamente numerico nem de quantidade em formato inteiro puro.

### Impactos identificados

- O fluxo de `NOTAS ENTRADA` em SoftcomShop ficou mais tolerante a formatos reais da API.
- A sincronizacao geral e os demais fluxos continuam chamando os mesmos metodos privados ja existentes.
- O fluxo SQL Server de Notas de Entrada nao foi alterado.

### Validacao realizada

- Build executado com MSBuild:
  - Comando: `C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe EtiquetaFORNew\EtiquetaFORNew.csproj /t:Build /p:Configuration=Debug /p:Platform=AnyCPU /v:minimal`
  - Resultado: build concluido com sucesso.
  - Observacao: permaneceram apenas warnings preexistentes de `using` duplicado, variaveis/campos nao usados e metodo async sem await.

### Pendencias ou pontos para validacao

- Validar novamente uma NF real do SoftcomShop informando numero da nota e data de entrada.
- Se uma nota real retornar itens paginados na busca por data, implementar paginacao em `BuscarPorNotaFiscalAsync(...)`.

## 2026-06-05 11:29:35 -03:00

### Objetivo da alteracao

Corrigir a quantidade de etiquetas carregadas por Nota de Entrada/SQL e ampliar o `FormDesignNovo` para redimensionamento em lote, historico de desfazer mais estavel e fonte minima 3.

### Arquivos modificados

- `Alteracoes.md`
- `EtiquetaFORNew/CarregadorDados.cs`
- `EtiquetaFORNew/Forms/FormPrincipal.cs`
- `EtiquetaFORNew/Forms/FormDesignNovo.cs`

### Metodos criados ou alterados

- Alterado `CarregadorDados.CarregarNotasEntrada(...)`
  - Passa a converter `Quantidade_Item` por helper centralizado antes de montar a linha de retorno.

- Alterado `CarregadorDados.CarregarNotasEntradaSoftcomShop(...)`
  - Passa a ler `QuantidadeEtiqueta` pelo mesmo helper, preservando a quantidade marcada pela rotina SoftcomShop.

- Alterado `CarregadorDados.AdicionarRowCompleto(...)`
  - Garante que a coluna `Quantidade` do `DataTable` receba a quantidade carregada e nunca seja menor que 1.

- Criado `CarregadorDados.ConverterQuantidadeEtiqueta(...)`
  - Centraliza leitura tolerante de quantidade numerica para carregamento de etiquetas.

- Alterado `FormPrincipal.AdicionarProdutoAoPanel(...)`
  - Deixa de forcar `Quantidade = 1`.
  - Usa a coluna `Quantidade` quando presente no `DataTable`, mantendo fallback 1 para origens que nao informam quantidade.

- Alterado `FormPrincipal.ConverterDataRowParaProduto(...)`
  - Passa a preservar a quantidade informada na linha carregada.

- Criado `FormPrincipal.ObterQuantidadeDoCarregamento(...)`
  - Le a quantidade da origem carregada de forma segura e positiva.

- Alterado `FormDesignNovo`
  - Adicionados controles de largura e altura do elemento no painel de propriedades.
  - O painel de propriedades agora permanece ativo para selecao multipla e aplica largura/altura a todos os elementos selecionados.
  - A selecao por retangulo atualiza o painel de propriedades para permitir edicao em lote.
  - O botao de remover passa a remover tambem multiplos elementos selecionados.
  - `Ctrl + Z` passa a funcionar mesmo sem elemento selecionado.
  - Movimentacao por teclado, exclusao, redimensionamento, rotacao, propriedades, inclusao e remocao passam a registrar historico apos a alteracao real.
  - Removidas gravacoes de historico durante rotinas de desenho/repaint.
  - Implementada poda real do historico com limite de 50 snapshots.
  - O tamanho minimo visual da fonte passou de 6 para 3.

### Resumo tecnico

O carregamento de Nota de Entrada SQL ja consultava `Quantidade_Item`, mas a quantidade era perdida no momento em que `FormPrincipal.AdicionarProdutoAoPanel(...)` recriava o produto com `Quantidade = 1`. Agora a quantidade segue do carregador ate a lista de impressao.

No SoftcomShop, a rotina que marca `QuantidadeEtiqueta` no SQLite continua preservada. O carregador agora le essa coluna por helper centralizado e o painel respeita a quantidade recebida.

No designer, a edicao em lote usa a lista `elementosSelecionados`. Ao alterar largura ou altura no painel, todos os elementos selecionados recebem o mesmo valor em `Bounds`, que ja e persistido pelo `TemplateManager`.

O historico de desfazer foi estabilizado para armazenar snapshots apos mudancas reais e nao mais durante desenho do canvas. O limite permanece maior que o solicitado, com 50 snapshots.

### Impactos identificados

- Fluxos que nao informam coluna `Quantidade` continuam usando fallback 1.
- O comportamento de selecao individual no designer foi mantido.
- A serializacao existente de layouts ja grava `Largura`, `Altura` e `FonteTamanho`, portanto as novas dimensoes em lote e fontes 3, 4 e 5 usam o fluxo de salvamento existente.

### Validacao realizada

- Build executado com MSBuild:
  - Comando: `C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe EtiquetaFORNew\EtiquetaFORNew.csproj /t:Build /p:Configuration=Debug /p:Platform=AnyCPU /v:minimal`
  - Resultado: build concluido com sucesso.
  - Observacao: permaneceram warnings preexistentes de `using` duplicado, variaveis/campos nao usados e metodo async sem await.

### Pendencias ou pontos para validacao

- Validar em ambiente com banco SQL real uma Nota de Entrada com quantidades diferentes de 1.
- Validar em ambiente SoftcomShop real uma Nota de Entrada com `QuantidadeEtiqueta` maior que 1.
- Validar manualmente no `FormDesignNovo` selecao multipla, alteracao de largura/altura, `Ctrl + Z` em sequencia, salvamento/carregamento de layout e fontes 3, 4 e 5.
