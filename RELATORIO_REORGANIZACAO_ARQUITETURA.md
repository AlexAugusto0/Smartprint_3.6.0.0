# Relatorio de reorganizacao de arquitetura

Data: 2026-07-22

## Escopo

Esta reorganizacao foi limitada a estrutura fisica de arquivos e ao arquivo de projeto `EtiquetaFORNew.csproj`.

Nao foram alterados:

- regras de negocio;
- consultas SQL;
- chamadas de API;
- autenticacao;
- eventos dos Forms;
- nomes de classes publicas;
- assinaturas de metodos;
- namespaces existentes.

Os namespaces foram mantidos de proposito para preservar compatibilidade binaria e reduzir risco em Forms, `resx`, serializacao e referencias existentes.

## Pastas criadas

- `EtiquetaFORNew/Core/API`
- `EtiquetaFORNew/Core/Business`
- `EtiquetaFORNew/Core/Configuration`
- `EtiquetaFORNew/Core/Models`
- `EtiquetaFORNew/Core/Repositories`
- `EtiquetaFORNew/Core/Utilities`
- `EtiquetaFORNew/Forms/Carregamento`
- `EtiquetaFORNew/Forms/Configuracao`
- `EtiquetaFORNew/Forms/Design`
- `EtiquetaFORNew/Forms/Impressao`
- `EtiquetaFORNew/Printing/Drivers`
- `EtiquetaFORNew/Printing/Elements`
- `EtiquetaFORNew/Printing/Layouts`
- `EtiquetaFORNew/SmartPrint/Etiqueta`
- `EtiquetaFORNew/SmartPrint/Volumes`

## Arquivos movidos

### Core/API

- `EtiquetaFORNew/Softcomshop/SoftcomShopService.cs` -> `EtiquetaFORNew/Core/API/SoftcomShopService.cs`

### Core/Business

- `EtiquetaFORNew/CarregadorDados.cs` -> `EtiquetaFORNew/Core/Business/CarregadorDados.cs`
- `EtiquetaFORNew/Data/Promocoesmanager.cs` -> `EtiquetaFORNew/Core/Business/Promocoesmanager.cs`
- `EtiquetaFORNew/IntegracaoExterna.cs` -> `EtiquetaFORNew/Core/Business/IntegracaoExterna.cs`

### Core/Configuration

- `EtiquetaFORNew/ConfiguracaoManager.cs` -> `EtiquetaFORNew/Core/Configuration/ConfiguracaoManager.cs`
- `EtiquetaFORNew/Data/DatabaseConfig.cs` -> `EtiquetaFORNew/Core/Configuration/DatabaseConfig.cs`
- `EtiquetaFORNew/GerenciadorConfiguracoesEtiqueta.cs` -> `EtiquetaFORNew/Core/Configuration/GerenciadorConfiguracoesEtiqueta.cs`
- `EtiquetaFORNew/Softcomshop/ConfiguracaoSistema.cs` -> `EtiquetaFORNew/Core/Configuration/ConfiguracaoSistema.cs`
- `EtiquetaFORNew/Softcomshop/SoftcomShopConfig.cs` -> `EtiquetaFORNew/Core/Configuration/SoftcomShopConfig.cs`

### Core/Models

- `EtiquetaFORNew/Classes.cs` -> `EtiquetaFORNew/Core/Models/Classes.cs`

### Core/Repositories

- `EtiquetaFORNew/Data/SmartPrintRepository.cs` -> `EtiquetaFORNew/Core/Repositories/SmartPrintRepository.cs`
- `EtiquetaFORNew/Data/SoftcomShopDataManager.cs` -> `EtiquetaFORNew/Core/Repositories/SoftcomShopDataManager.cs`
- `EtiquetaFORNew/Localdatabasemanager .cs` -> `EtiquetaFORNew/Core/Repositories/LocalDatabaseManager.cs`
- `EtiquetaFORNew/LocalDatabaseManagerExtensions.cs` -> `EtiquetaFORNew/Core/Repositories/LocalDatabaseManagerExtensions.cs`

### Core/Utilities

- `EtiquetaFORNew/Data/Funcoes.cs` -> `EtiquetaFORNew/Core/Utilities/Funcoes.cs`
- `EtiquetaFORNew/FormatadorMonetario.cs` -> `EtiquetaFORNew/Core/Utilities/FormatadorMonetario.cs`
- `EtiquetaFORNew/ModuloAppHelper.cs` -> `EtiquetaFORNew/Core/Utilities/ModuloAppHelper.cs`
- `EtiquetaFORNew/Properties/VersaoHelper.cs` -> `EtiquetaFORNew/Core/Utilities/VersaoHelper.cs`

### Printing/Drivers

- `EtiquetaFORNew/CalibracaoManager.cs` -> `EtiquetaFORNew/Printing/Drivers/CalibracaoManager.cs`
- `EtiquetaFORNew/Driverinstaller.cs` -> `EtiquetaFORNew/Printing/Drivers/Driverinstaller.cs`
- `EtiquetaFORNew/Impressoramanager.cs` -> `EtiquetaFORNew/Printing/Drivers/Impressoramanager.cs`
- `EtiquetaFORNew/PrinterDriverMatcher.cs` -> `EtiquetaFORNew/Printing/Drivers/PrinterDriverMatcher.cs`
- `EtiquetaFORNew/Data/RawPrinterHelper.cs` -> `EtiquetaFORNew/Printing/Drivers/RawPrinterHelper.cs`

### Printing/Elements

- `EtiquetaFORNew/CalculadoraCamposEtiqueta.cs` -> `EtiquetaFORNew/Printing/Elements/CalculadoraCamposEtiqueta.cs`
- `EtiquetaFORNew/CampoEtiquetaResolver.cs` -> `EtiquetaFORNew/Printing/Elements/CampoEtiquetaResolver.cs`
- `EtiquetaFORNew/DescricaoMercadoriaDisplayHelper.cs` -> `EtiquetaFORNew/Printing/Elements/DescricaoMercadoriaDisplayHelper.cs`
- `EtiquetaFORNew/ExpressionEngine.cs` -> `EtiquetaFORNew/Printing/Elements/ExpressionEngine.cs`

### Printing/Layouts

- `EtiquetaFORNew/TemplateManager.cs` -> `EtiquetaFORNew/Printing/Layouts/TemplateManager.cs`
- `EtiquetaFORNew/TemplatePadraoManager.cs` -> `EtiquetaFORNew/Printing/Layouts/TemplatePadraoManager.cs`
- `EtiquetaFORNew/TemplatesPreDefinidos.cs` -> `EtiquetaFORNew/Printing/Layouts/TemplatesPreDefinidos.cs`

### Forms/Carregamento

- `EtiquetaFORNew/Forms/FormPrincipal.cs` -> `EtiquetaFORNew/Forms/Carregamento/FormPrincipal.cs`
- `EtiquetaFORNew/Forms/FormPrincipal.Designer.cs` -> `EtiquetaFORNew/Forms/Carregamento/FormPrincipal.Designer.cs`
- `EtiquetaFORNew/Forms/FormPrincipal.resx` -> `EtiquetaFORNew/Forms/Carregamento/FormPrincipal.resx`
- `EtiquetaFORNew/Forms/FormFiltrosCarregamento.cs` -> `EtiquetaFORNew/Forms/Carregamento/FormFiltrosCarregamento.cs`
- `EtiquetaFORNew/Softcomshop/FormSincronizacaoSoftcomShop.cs` -> `EtiquetaFORNew/Forms/Carregamento/FormSincronizacaoSoftcomShop.cs`
- `EtiquetaFORNew/Softcomshop/FormSincronizacaoSoftcomShop.Designer.cs` -> `EtiquetaFORNew/Forms/Carregamento/FormSincronizacaoSoftcomShop.Designer.cs`
- `EtiquetaFORNew/Softcomshop/FormSincronizacaoSoftcomShop.resx` -> `EtiquetaFORNew/Forms/Carregamento/FormSincronizacaoSoftcomShop.resx`

### Forms/Configuracao

- `EtiquetaFORNew/Forms/ConfigForm.cs` -> `EtiquetaFORNew/Forms/Configuracao/ConfigForm.cs`
- `EtiquetaFORNew/Forms/ConfigForm.Designer.cs` -> `EtiquetaFORNew/Forms/Configuracao/ConfigForm.Designer.cs`
- `EtiquetaFORNew/Forms/ConfigForm.resx` -> `EtiquetaFORNew/Forms/Configuracao/ConfigForm.resx`
- `EtiquetaFORNew/Forms/Formconfigetiqueta.cs` -> `EtiquetaFORNew/Forms/Configuracao/Formconfigetiqueta.cs`
- `EtiquetaFORNew/Forms/Formconfigetiqueta.Designer.cs` -> `EtiquetaFORNew/Forms/Configuracao/Formconfigetiqueta.Designer.cs`
- `EtiquetaFORNew/Forms/FormListaConfiguracoes.cs` -> `EtiquetaFORNew/Forms/Configuracao/FormListaConfiguracoes.cs`
- `EtiquetaFORNew/Forms/FormListaConfiguracoes.Designer.cs` -> `EtiquetaFORNew/Forms/Configuracao/FormListaConfiguracoes.Designer.cs`
- `EtiquetaFORNew/Forms/FormMenuConfiguracao.cs` -> `EtiquetaFORNew/Forms/Configuracao/FormMenuConfiguracao.cs`
- `EtiquetaFORNew/Forms/FormMenuConfiguracao.Designer.cs` -> `EtiquetaFORNew/Forms/Configuracao/FormMenuConfiguracao.Designer.cs`
- `EtiquetaFORNew/Forms/FormMenuConfiguracao.resx` -> `EtiquetaFORNew/Forms/Configuracao/FormMenuConfiguracao.resx`
- `EtiquetaFORNew/Softcomshop/FormConfiguracao.cs` -> `EtiquetaFORNew/Forms/Configuracao/FormConfiguracao.cs`
- `EtiquetaFORNew/Softcomshop/FormConfiguracao.Designer.cs` -> `EtiquetaFORNew/Forms/Configuracao/FormConfiguracao.Designer.cs`
- `EtiquetaFORNew/Softcomshop/FormConfiguracao.resx` -> `EtiquetaFORNew/Forms/Configuracao/FormConfiguracao.resx`

### Forms/Design

- `EtiquetaFORNew/Forms/FormDesignNovo.cs` -> `EtiquetaFORNew/Forms/Design/FormDesignNovo.cs`
- `EtiquetaFORNew/Forms/FormDesignNovo.Designer.cs` -> `EtiquetaFORNew/Forms/Design/FormDesignNovo.Designer.cs`
- `EtiquetaFORNew/Forms/FormDesignNovo.resx` -> `EtiquetaFORNew/Forms/Design/FormDesignNovo.resx`
- `EtiquetaFORNew/Forms/FormListaTemplates.cs` -> `EtiquetaFORNew/Forms/Design/FormListaTemplates.cs`
- `EtiquetaFORNew/Forms/FormNomeTemplate.cs` -> `EtiquetaFORNew/Forms/Design/FormNomeTemplate.cs`
- `EtiquetaFORNew/FormNomeTemplate.cs` -> `EtiquetaFORNew/Forms/Design/FormNomeTemplateDialog.cs`
- `EtiquetaFORNew/Forms/FormPreview.cs` -> `EtiquetaFORNew/Forms/Design/FormPreview.cs`
- `EtiquetaFORNew/Forms/FormTemplateApi.cs` -> `EtiquetaFORNew/Forms/Design/FormTemplateApi.cs`
- `EtiquetaFORNew/Forms/FormTemplateApi.Designer.cs` -> `EtiquetaFORNew/Forms/Design/FormTemplateApi.Designer.cs`
- `EtiquetaFORNew/Forms/FormTemplateApi.resx` -> `EtiquetaFORNew/Forms/Design/FormTemplateApi.resx`

### Forms/Impressao

- `EtiquetaFORNew/Forms/FormImpressao.cs` -> `EtiquetaFORNew/Forms/Impressao/FormImpressao.cs`
- `EtiquetaFORNew/Forms/FormImpressao.Designer.cs` -> `EtiquetaFORNew/Forms/Impressao/FormImpressao.Designer.cs`
- `EtiquetaFORNew/Forms/FormImpressao.resx` -> `EtiquetaFORNew/Forms/Impressao/FormImpressao.resx`
- `EtiquetaFORNew/Forms/FormSelecaoImpressao.cs` -> `EtiquetaFORNew/Forms/Impressao/FormSelecaoImpressao.cs`
- `EtiquetaFORNew/Forms/calibracao.cs` -> `EtiquetaFORNew/Forms/Impressao/calibracao.cs`
- `EtiquetaFORNew/Forms/calibracao.Designer.cs` -> `EtiquetaFORNew/Forms/Impressao/calibracao.Designer.cs`
- `EtiquetaFORNew/Forms/calibracao.resx` -> `EtiquetaFORNew/Forms/Impressao/calibracao.resx`

### SmartPrint

- `EtiquetaFORNew/Softcomshop/EtiquetaDistribuidora.cs` -> `EtiquetaFORNew/SmartPrint/Etiqueta/EtiquetaDistribuidora.cs`
- `EtiquetaFORNew/Softcomshop/EtiquetaDistribuidoraResolver.cs` -> `EtiquetaFORNew/SmartPrint/Etiqueta/EtiquetaDistribuidoraResolver.cs`
- `EtiquetaFORNew/Softcomshop/EtiquetaVolumeDistribuidora.cs` -> `EtiquetaFORNew/SmartPrint/Volumes/EtiquetaVolumeDistribuidora.cs`
- `EtiquetaFORNew/Softcomshop/EtiquetaVolumeDistribuidoraResolver.cs` -> `EtiquetaFORNew/SmartPrint/Volumes/EtiquetaVolumeDistribuidoraResolver.cs`

## Classes reorganizadas

- Classes de API SoftcomShop foram concentradas em `Core/API`.
- Classes de configuracao foram concentradas em `Core/Configuration`.
- Classes de acesso a dados/repositorios foram concentradas em `Core/Repositories`.
- Utilitarios gerais foram concentrados em `Core/Utilities`.
- Drivers, deteccao de impressora, calibracao e envio raw para impressora foram concentrados em `Printing/Drivers`.
- Resolvedores e calculos de elementos de etiqueta foram concentrados em `Printing/Elements`.
- Templates e layouts de etiqueta foram concentrados em `Printing/Layouts`.
- Forms foram agrupados por fluxo funcional: carregamento, configuracao, design e impressao.
- Classes especificas de etiqueta/volumes da distribuidora foram separadas em `SmartPrint/Etiqueta` e `SmartPrint/Volumes`.

## Metodos extraidos

Nenhum metodo foi extraido nesta etapa.

Motivo: os maiores candidatos estao em Forms e servicos com alto acoplamento a eventos, estado visual, SQL/API e recursos. Para manter 100% do comportamento atual, esta revisao priorizou reorganizacao fisica e deixou extracoes internas para uma etapa posterior com testes especificos por fluxo.

## Arquivos grandes e oportunidades de extracao

Arquivos identificados como candidatos a quebra futura, sem alteracao interna nesta etapa:

- `EtiquetaFORNew/Forms/Carregamento/FormPrincipal.cs`: God Object de carregamento, importacao, filtros, sincronizacao, impressao e configuracao.
- `EtiquetaFORNew/Forms/Design/FormDesignNovo.cs`: Designer visual com manipulacao de elementos, templates, edicao e preview.
- `EtiquetaFORNew/Core/Repositories/SoftcomShopDataManager.cs`: sincronizacao local/remota, cache e persistencia em um unico arquivo.
- `EtiquetaFORNew/Core/Business/CarregadorDados.cs`: carregamento SQL, filtros, mapeamento e montagem de tabelas.
- `EtiquetaFORNew/Forms/Configuracao/Formconfigetiqueta.cs`: configuracao visual, serializacao XML e modelos auxiliares.
- `EtiquetaFORNew/Forms/Impressao/FormImpressao.cs`: renderizacao, preview e fluxo de impressao.
- `EtiquetaFORNew/Forms/telaTecnico.cs`: deteccao, instalacao de driver, UI tecnica e selecao manual.
- `EtiquetaFORNew/Printing/Drivers/Driverinstaller.cs`: download, extracao, execucao de instalador e Forms auxiliares.
- `EtiquetaFORNew/Printing/Drivers/PrinterDriverMatcher.cs`: modelo, scoring, normalizacao e logging.
- `EtiquetaFORNew/Core/API/SoftcomShopService.cs`: autenticacao/token, chamadas HTTP e excecao de API no mesmo arquivo.

## Codigo duplicado ou legado encontrado

- `EtiquetaFORNew/TemplateManager_old.cs` contem versao antiga de `TemplateManager` e nao esta compilado pelo projeto.
- `EtiquetaFORNew/Program_old.cs` contem versao antiga de `Program` e nao esta compilado pelo projeto.
- `EtiquetaFORNew/Forms/FormFiltrosCarregamento_OLD.cs` contem versao antiga de `FormFiltrosCarregamento` e nao esta compilado pelo projeto.
- Existem duas classes `FormNomeTemplate` em namespaces diferentes:
  - `EtiquetaFORNew.Forms.FormNomeTemplate`, agora em `Forms/Design/FormNomeTemplateDialog.cs`;
  - `EtiquetaFORNew.FormNomeTemplate`, agora em `Forms/Design/FormNomeTemplate.cs`.
- Ha padroes repetidos de manipulacao de excecao, montagem de UI programatica e serializacao XML em Forms, mantidos sem alteracao.

## Melhorias de organizacao realizadas

- A raiz de `EtiquetaFORNew` ficou menos carregada por classes de dominio, dados, impressao e utilitarios.
- O antigo agrupamento generico `Data` foi distribuido em `Core/Repositories`, `Core/Business`, `Core/Configuration`, `Core/Utilities` e `Printing/Drivers`.
- O antigo agrupamento `Softcomshop` foi separado entre API, configuracao, Forms e dominio SmartPrint.
- As pastas antigas vazias `EtiquetaFORNew/Data` e `EtiquetaFORNew/Softcomshop` foram removidas.
- Forms com `.Designer.cs` e `.resx` foram movidos junto com seus arquivos principais.
- `EtiquetaFORNew.csproj` foi atualizado com os novos caminhos de `Compile Include` e `EmbeddedResource Include`.
- `DependentUpon` foi preservado para manter o agrupamento de partial classes e resources no Visual Studio.
- O arquivo `Localdatabasemanager .cs` teve apenas o nome fisico corrigido para `LocalDatabaseManager.cs`; a classe `LocalDatabaseManager` nao foi alterada.

## Arquivos que permaneceram sem alteracao

Permaneceram em seus locais originais:

- `EtiquetaFORNew/Program.cs`
- `EtiquetaFORNew/App.config`
- `EtiquetaFORNew/packages.config`
- `EtiquetaFORNew/FodyWeavers.xml`
- `EtiquetaFORNew/OPS_HUB/Ops_Hub.cs`
- `EtiquetaFORNew/Properties/AssemblyInfo.cs`
- `EtiquetaFORNew/Properties/Resources.resx`
- `EtiquetaFORNew/Properties/Resources.Designer.cs`
- `EtiquetaFORNew/Properties/Settings.settings`
- `EtiquetaFORNew/Properties/Settings.Designer.cs`
- `EtiquetaFORNew/Forms/Main.cs`
- `EtiquetaFORNew/Forms/Main.Designer.cs`
- `EtiquetaFORNew/Forms/Main.resx`
- `EtiquetaFORNew/Forms/telaEntrada.cs`
- `EtiquetaFORNew/Forms/telaEntrada.Designer.cs`
- `EtiquetaFORNew/Forms/telaEntrada.resx`
- `EtiquetaFORNew/Forms/telaTecnico.cs`
- `EtiquetaFORNew/Forms/telaTecnico.Designer.cs`
- `EtiquetaFORNew/Forms/telaTecnico.resx`
- `EtiquetaFORNew/Resources/*`
- `EtiquetaFORNew/Resources/Impressoras/*`
- arquivos antigos nao compilados: `Program_old.cs`, `TemplateManager_old.cs`, `Forms/FormFiltrosCarregamento_OLD.cs`

## Validacao executada

Build executado antes da reorganizacao:

- Com MSBuild do Visual Studio 2022: sucesso.
- Com MSBuild do .NET Framework 4.x: falha preexistente em `packages/Fody.6.8.2/build/Fody.targets`, por incompatibilidade do executor antigo com o formato do target. Este caminho nao foi usado como validacao final.

Build executado depois da reorganizacao:

- Comando: `C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe EtiquetaFORNew.sln /p:Configuration=Debug`
- Resultado: sucesso.
- Erros: 0.
- Avisos: 10 avisos ja relacionados a codigo existente, sem bloqueio de build.

## Confirmacao de compatibilidade

- Nenhuma regra de negocio foi alterada.
- Nenhuma consulta SQL foi alterada.
- Nenhuma chamada de API foi alterada.
- Nenhum fluxo de autenticacao foi alterado.
- Nenhum evento de Form foi alterado.
- Nenhum arquivo `.Designer.cs` foi editado internamente.
- Nenhum `.resx` foi editado internamente.
- Nenhum namespace publico foi alterado.
- Nenhuma assinatura publica foi alterada.
- A aplicacao compila apos a reorganizacao com 0 erros.
