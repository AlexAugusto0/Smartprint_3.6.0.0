# Arquitetura da Solucao SmartPrint

## Visao geral

A solucao `EtiquetaFORNew.sln` implementa o SmartPrint, uma aplicacao desktop Windows Forms para configuracao, selecao, visualizacao e impressao de etiquetas de produtos.

O sistema foi construido em C# sobre .NET Framework 4.7.2. A aplicacao trabalha com dados de mercadorias vindos de duas origens principais:

- SQL Server, usado como origem legada/ERP.
- API SoftcomShop, usada como origem web via autenticacao por dispositivo e token.

As duas origens sao normalizadas em um banco SQLite local (`LocalData.db`) no diretorio da aplicacao. A tela principal consome principalmente esse cache local para busca, filtros, selecao de produtos e impressao.

## Projetos existentes

### EtiquetaFORNew

Projeto principal da aplicacao.

- Tipo: C# Windows Forms (`WinExe`).
- Target: .NET Framework 4.7.2.
- Arquivo: `EtiquetaFORNew/EtiquetaFORNew.csproj`.
- Responsabilidades:
  - Inicializacao da aplicacao.
  - Login e configuracao.
  - Sincronizacao de mercadorias.
  - Integracao com SQL Server e SoftcomShop.
  - Cache local SQLite.
  - Designer de templates de etiqueta.
  - Configuracao de papel/impressora.
  - Preview e impressao.
  - Suporte a importacao externa por JSON.

### SmartPrint

Projeto de instalador/setup da solucao.

- Tipo: Visual Studio Setup Project (`.vdproj`).
- Arquivo: `SmartPrint/SmartPrint.vdproj`.
- Responsabilidade: empacotamento/distribuicao do executavel SmartPrint.

## Dependencias principais

As dependencias sao gerenciadas via `packages.config` e pasta `packages`.

- `System.Data.SQLite`: banco local.
- `Microsoft.Data.SqlClient` e `System.Data.SqlClient`: acesso ao SQL Server.
- `Newtonsoft.Json`: serializacao de configuracoes, templates e payloads externos.
- `BarcodeLib`/`BarcodeStandard` e `SkiaSharp`: suporte a codigo de barras e renderizacao.
- `EntityFramework`: referenciado, embora a maior parte do acesso a dados atual seja feita com ADO.NET.
- `Fody` e `Costura.Fody`: empacotamento de dependencias no assembly.
- Bibliotecas Microsoft/Azure/Identity: referenciadas para suporte a dependencias modernas, autenticacao e runtime.

## Estrutura logica

### Entrada da aplicacao

O ponto de entrada fica em `Program.cs`.

Fluxo inicial:

1. Inicializa estilos WinForms.
2. Inicializa o banco SQLite local via `LocalDatabaseManager.InicializarBanco()`.
3. Verifica se o sistema esta configurado em modo SoftcomShop.
4. Decide o modo de execucao:
   - modo normal: abre `Main` com login;
   - modo SoftcomShop: abre `FormPrincipal` direto;
   - modo importacao JSON/XML/API: processa dados externos e abre `FormPrincipal` com itens importados.

### Interface

A interface esta concentrada em `EtiquetaFORNew/Forms` e `EtiquetaFORNew/Softcomshop`.

Telas principais:

- `Main`: tela inicial/login e inicializacao de conexao.
- `FormPrincipal`: tela central de operacao; busca produtos, carrega filtros, seleciona itens, sincroniza, escolhe template/configuracao e dispara impressao.
- `FormDesignNovo`: designer visual de etiquetas.
- `FormImpressao`: preview paginado e impressao real.
- `FormPreview`: preview de template usando produto real ou ficticio.
- `FormFiltrosCarregamento`: filtros para carregar produtos por tipo de movimento.
- `ConfigForm`, `FormConfiguracao`, `FormMenuConfiguracao`, `FormListaConfiguracoes`: configuracoes de banco, sistema e papel.
- `telaTecnico` e `calibracao`: suporte tecnico, drivers e calibracao de impressoras.
- `FormSincronizacaoSoftcomShop`: sincronizacao com SoftcomShop.

### Dominio de etiquetas

Os modelos centrais ficam em `Classes.cs`.

- `Produto`: representa a mercadoria selecionada para etiqueta, com codigo, referencia, barras, preco, grupos, fabricante, tamanho/cor e informacoes de promocao.
- `TemplateEtiqueta`: define largura, altura e lista de elementos da etiqueta.
- `ElementoEtiqueta`: define cada elemento desenhado no template.
- `TipoElemento`: texto fixo, campo dinamico, codigo de barras ou imagem.

O template e serializado em JSON pelo `TemplateManager`, incluindo fonte, cor, posicao, rotacao, alinhamento, cor de fundo e imagens em Base64.

### Templates e configuracoes

Ha dois eixos de configuracao:

- Template visual da etiqueta, gerenciado por `TemplateManager`.
- Configuracao de papel/impressora, gerenciada por `ConfiguracaoManager` e `GerenciadorConfiguracoesEtiqueta`.

Locais de persistencia:

- Templates: `Documentos/SistemaEtiquetas/Templates`.
- Configuracoes modernas: `Documentos/SistemaEtiquetas/Configuracoes`.
- Compatibilidade antiga: `%AppData%/EtiquetaFornew/configuracoes.xml` e `modelos_papel.xml`.

O sistema tambem instala templates predefinidos via `TemplatesPreDefinidos`/`TemplatePadraoManager`.

### Persistencia local

O SQLite local e gerenciado por `LocalDatabaseManager`.

Arquivo:

- `LocalData.db`, criado no diretorio base da aplicacao.

Tabelas principais:

- `Mercadorias`: cache normalizado de produtos vindos de SQL Server ou SoftcomShop.
- `ProdutosSelecionados`: produtos escolhidos para impressao.
- `ConfiguracaoSync`: controle de ultima sincronizacao.
- `Promocoes`: criada/atualizada durante sincronizacao de promocoes.

Campos importantes em `Mercadorias`:

- identificacao: `CodigoMercadoria`, `ID_SoftcomShop`, `CodFabricante`, `CodBarras`, `CodBarras_Grade`;
- descricao/preco: `Mercadoria`, `PrecoVenda`, `VendaA` a `VendaE`;
- classificacao: `Fornecedor`, `Fabricante`, `Grupo`;
- grade: `Tam`, `Cores`;
- origem: `Origem`;
- impressao: `GerarEtiqueta`, `QuantidadeEtiqueta`;
- promocao: `EmPromocao`, `PrecoPromocional`, `ID_Promocao`.

### Integracao com SQL Server

Configuracao em `DatabaseConfig`.

Arquivo:

- `config.json`, no diretorio base da aplicacao.

Responsabilidades:

- montar connection string;
- salvar servidor, porta, banco, usuario, senha, timeout, loja e modulo;
- consultar registro remoto via SOAP (`wsRegistro.asmx`);
- expor dados de modulo, incluindo `CONFECCAO`.

O cache local e alimentado por `LocalDatabaseManager.SincronizarMercadorias()`, que consulta `memoria_MercadoriasLojas` no SQL Server e grava os produtos no SQLite.

`SmartPrintRepository` tambem atua sobre SQL Server para criar/verificar estrutura relacionada a etiquetas:

- tabela `MercEtiqueta`;
- view `vw_GeradorSmartPrint`;
- insercao/limpeza de itens para etiqueta.

### Integracao com SoftcomShop

Configuracao em `ConfiguracaoSistema` e `SoftcomShopConfig`.

Arquivo:

- `config_sistema.json`, no diretorio base da aplicacao.

Componentes:

- `SoftcomShopService`: comunicacao HTTP com a API.
- `SoftcomShopRouter`: rotas de token, dispositivo, produtos, empresa, promocoes, notas fiscais e vendas.
- `SoftcomShopDataManager`: sincroniza dados da API para o SQLite local.

Fluxos suportados:

- cadastro de dispositivo;
- obtencao de token;
- sincronizacao paginada de produtos;
- busca por nota fiscal;
- busca por venda;
- sincronizacao de promocoes ativas.

Quando o modo ativo e SoftcomShop, o sistema pode abrir direto em `FormPrincipal` sem passar pelo login normal.

### Importacao externa

`IntegracaoExterna` permite iniciar o SmartPrint com parametros externos.

Modos:

- sem argumentos: uso normal;
- arquivo `.json`: importa itens externos, atualmente associado ao Softshop Access;
- arquivo `.xml`: reservado, ainda nao implementado;
- `--api-import:`: reservado para futura API.

O JSON e desserializado para `DadosImportacao`, com lista de `ItemImportacao` e configuracao opcional de autoimpressao.

## Principais fluxos de negocio

### 1. Inicializacao e selecao do modo de operacao

1. `Program.Main` inicializa o SQLite.
2. O sistema carrega `config_sistema.json`.
3. Se `TipoConexaoAtiva` for SoftcomShop e a configuracao estiver valida, abre `FormPrincipal`.
4. Caso contrario, verifica argumentos de importacao.
5. Sem importacao, abre `Main` para login/configuracao.

### 2. Configuracao de origem dos dados

SQL Server:

1. Usuario informa servidor, banco, loja e credenciais.
2. `DatabaseConfig` salva `config.json`.
3. `LocalDatabaseManager.SincronizarMercadorias()` importa mercadorias para SQLite.

SoftcomShop:

1. Usuario configura URL, client id, empresa, CNPJ e dispositivo.
2. O dispositivo e cadastrado para obter `ClientSecret`.
3. `SoftcomShopService` obtem token.
4. `SoftcomShopDataManager` sincroniza produtos para `Mercadorias`.

### 3. Sincronizacao de mercadorias

SQL Server:

1. Le `memoria_MercadoriasLojas` filtrando pela loja configurada.
2. Limpa `Mercadorias` e `ProdutosSelecionados`.
3. Insere dados normalizados com `Origem = 'SQL'`.
4. Atualiza `ConfiguracaoSync`.

SoftcomShop:

1. Busca produtos paginados na API.
2. Limpa cache local antes da sincronizacao.
3. Insere produtos com `Origem = 'SOFTCOMSHOP'`.
4. Processa tabela de precos.
5. Processa atributos de grade, principalmente tamanho e cor.
6. Atualiza timestamp de sincronizacao.

### 4. Busca, filtro e carregamento de produtos

O usuario pode montar a lista de etiquetas por:

- busca manual por nome, referencia ou codigo em `FormPrincipal`;
- filtros manuais por grupo, fabricante e fornecedor;
- tipos de carregamento em `CarregadorDados`:
  - ajustes;
  - balancos;
  - notas de entrada;
  - precos alterados;
  - promocoes;
  - filtros manuais.

Em modulo `CONFECCAO`, o fluxo tambem considera grade com tamanho, cor e codigo de barras de grade.

### 5. Promocoes

SQL Server:

1. `PromocoesManager` busca promocoes ativas e produtos vinculados.
2. `CarregadorDados` pode carregar produtos por `ID_Promocao`.

SoftcomShop:

1. `SoftcomShopDataManager.SincronizarPromocoesAtivasAsync()` consulta a API.
2. Cria/atualiza tabela `Promocoes`.
3. Marca mercadorias com `EmPromocao`, `PrecoPromocional` e `ID_Promocao`.
4. O carregamento por promocoes usa esses dados locais.

### 6. Importacao externa de itens

1. O SmartPrint e iniciado com o caminho de um JSON.
2. `IntegracaoExterna` valida e desserializa os itens.
3. `FormPrincipal` recebe `DadosImportacao`.
4. Cada item e complementado com dados do SQLite quando possivel.
5. A lista e preparada para impressao.
6. O arquivo temporario pode ser removido apos o processamento.

### 7. Criacao e manutencao de templates

1. Usuario abre o designer.
2. Adiciona elementos de texto, campo, codigo de barras ou imagem.
3. Define posicao, tamanho, fonte, cor, alinhamento e rotacao.
4. `TemplateManager` salva o template em JSON.
5. O ultimo template usado pode ser restaurado automaticamente.

### 8. Configuracao de papel e impressora

1. Usuario escolhe ou cria configuracao de etiqueta/papel.
2. Define impressora padrao, dimensoes, colunas, linhas, espacamentos e margens.
3. A configuracao e salva em JSON.
4. O sistema ainda migra configuracoes XML antigas quando necessario.

### 9. Preview e impressao

1. `FormPrincipal` transforma linhas selecionadas em `Produto`.
2. Abre `FormImpressao` com lista de produtos, template e configuracao.
3. `FormImpressao` expande produtos por quantidade.
4. Calcula paginacao conforme linhas/colunas.
5. Renderiza preview em bitmap.
6. Na impressao, configura `PrintDocument`, papel customizado em milimetros convertido para centesimos de polegada e margens zeradas.
7. Desenha cada etiqueta com campos dinamicos, codigos de barras, textos e imagens.

### 10. Suporte tecnico, drivers e calibracao

1. `telaTecnico` lista dispositivos/impressoras via recursos do Windows.
2. `ImpressoraManager` carrega catalogo de impressoras a partir de recurso JSON.
3. `DriverInstaller` baixa, extrai e executa instaladores.
4. `CalibracaoManager` carrega orientacoes de calibracao a partir de recurso JSON.

## Observacoes de arquitetura

- A arquitetura e monolitica WinForms: UI, regras de negocio e acesso a dados estao no mesmo projeto.
- Ha separacao por arquivos e pastas, mas nao por projetos/camadas compiladas.
- O SQLite local e o ponto central de desacoplamento entre origens externas e tela de impressao.
- O codigo mistura algumas abordagens: ADO.NET direto, arquivos JSON, XML legado e referencias a Entity Framework.
- Muitos managers sao estaticos, o que simplifica o fluxo atual, mas dificulta testes automatizados e injecao de dependencias.
- Ha nomes e comentarios com problemas de encoding em alguns arquivos, indicando historico de conversao de caracteres.
- Existem arquivos antigos (`*_old.cs`, `Program_old.cs`, `TemplateManager_old.cs`) que nao aparecem compilados no `.csproj`, mas devem ser tratados com cuidado antes de remover.

## Resumo da arquitetura em camadas praticas

```text
Usuario
  -> WinForms (Main, FormPrincipal, Designer, Preview, Configuracoes)
      -> Managers de negocio (LocalDatabaseManager, CarregadorDados, TemplateManager, ConfiguracaoManager)
          -> Persistencia local (SQLite LocalData.db, JSON/XML de configuracao)
          -> Integracoes externas
              -> SQL Server / ERP
              -> API SoftcomShop
              -> Importacao JSON externa
      -> Impressao Windows (PrintDocument, RawPrinterHelper, drivers)
```

