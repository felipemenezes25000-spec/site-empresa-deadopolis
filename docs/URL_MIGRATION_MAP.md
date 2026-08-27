# Mapa de migração de URLs — Portal legado de Deodápolis

Este documento é a matriz de continuidade entre o portal legado e a nova plataforma. Ele **não autoriza redirects automaticamente**: um `301` só pode ser ativado quando o destino existir, responder corretamente e o conteúdo/integração correspondente estiver validado.

## Convenções

- `PRESERVAR`: a rota nova já existe com o mesmo caminho e pode permanecer canônica.
- `301_APOS_DESTINO`: o destino planejado ainda precisa existir/ser validado antes do redirect.
- `MIGRAR_CONTEUDO`: o conteúdo histórico precisa entrar no pipeline de inventário/importação antes do cutover.
- `MANTER_EXTERNO`: é um sistema de terceiro ou integração separada; não copiar como se fosse conteúdo local.
- `ARQUIVAR`: preservar evidência/acervo, sem promover como conteúdo editorial ativo.
- `EXISTE`: rota top-level confirmada no frontend novo.
- `IMPLEMENTAR`: destino planejado ainda não existe no frontend novo.
- `DINAMICO`: destino depende de mapeamento item-a-item (slug, documento, processo etc.).

## Escala do acervo observado

A auditoria pública automatizada concluída em 26/08/2026 mostrou que o legado não é apenas um conjunto pequeno de páginas institucionais. O dry-run completo encontrou **14.373 URLs únicas**, incluindo 8.155 documentos e 3.372 imagens. A reconciliação independente encontrou inicialmente 5.332 documentos de licitações; o grafo final capturou 5.336 após quatro referências surgirem durante a janela. Prestação de contas permaneceu em 610 e informativos em 1.404 páginas. Consulte `docs/LEGACY_MIGRATION_REPORT.md`.

O cutover deve, portanto, ser tratado como **migração de acervo**. O `MigrationJob` precisa produzir a contagem definitiva por crawl e evidência; os números observados manualmente não substituem esse inventário automatizado.

Famílias de grande volume observadas:

- licitações: avisos, editais, resultados e contratos;
- prestação de contas: 19 famílias conhecidas no e-SIC;
- informativos/notícias por secretaria/tema;
- PDFs e outros arquivos em `/e-sic/uploads/...`;
- notícias acessíveis por mais de uma família de URL (`exibe.php` e `exibe23.php`).

## Rotas novas confirmadas

Rotas top-level existentes hoje na nova aplicação: `/`, `/acessibilidade`, `/acesso-a-informacao`, `/agenda`, `/buscar`, `/conselhos`, `/contatos`, `/dados-abertos`, `/diario-oficial`, `/legislacao`, `/licitacoes`, `/locais`, `/noticias`, `/obras`, `/ouvidoria`, `/privacidade`, `/secretarias`, `/servicos`, `/transparencia` e `/verificar`.

Importante: `/secretarias` é um diretório administrável, `/licitacoes` combina fontes operacionais declaradas com o acervo histórico filtrável, e `/legislacao` usa o acervo documental governado com filtro por espécie normativa. A existência da rota não substitui a migração e validação do conteúdo oficial.

## Institucional e páginas históricas

| URL antiga | Destino planejado | Ação | Estado do destino | Observação |
|---|---|---|---|---|
| `/` | `/` | PRESERVAR | EXISTE | Home canônica. |
| `/index.php` | `/` | 301_APOS_DESTINO | EXISTE | Remover duplicidade da home. |
| `/sobre.php` | `/municipio` | MIGRAR_CONTEUDO | ROTA EXISTE; CONTEÚDO PENDENTE | Página governada pelo CMS; validar conteúdo antes do 301. |
| `/institucional/sobre` | `/municipio` | MIGRAR_CONTEUDO | ROTA EXISTE; CONTEÚDO PENDENTE | Mesma necessidade de validação editorial. |
| `/missao.php` | `/municipio/gestao` | MIGRAR_CONTEUDO | ROTA EXISTE; CONTEÚDO PENDENTE | Preservar missão/visão/gestão no CMS antes do 301. |
| `/institucional/missao` | `/municipio/gestao` | MIGRAR_CONTEUDO | ROTA EXISTE; CONTEÚDO PENDENTE | Destino administrável confirmado. |
| `/pagina/97_Telefones-Uteis.html` | `/contatos` | 301_APOS_DESTINO | EXISTE | Validar que todos os telefones úteis foram migrados. |
| `/pages/central-conselhos/` | `/conselhos` | MIGRAR_CONTEUDO | ROTA EXISTE; CONTEÚDO PENDENTE | Página própria governada pelo CMS; composição oficial ainda precisa ser migrada. |
| `/pages/nova-lei-licitacoes/` | `/licitacoes` | MIGRAR_CONTEUDO | EXISTE | Conteúdo deve ser incorporado ao hub/acervo antes do redirect. |
| `/pages/vpn/` | `/servicos/vtn-itr` | MIGRAR_CONTEUDO | DINAMICO | Confirmar serviço/slug antes do redirect. |

## Estrutura de governo e secretarias (`sec.php`)

O legado usa IDs fixos para governo/órgãos. A nova aplicação possui `/secretarias/[slug]`, mas cada slug só responde quando o cadastro administrativo correspondente existe; por isso os redirects permanecem inativos até a validação dos registros.

| URL antiga | Entidade observada | Destino planejado | Ação | Estado |
|---|---|---|---|---|
| `/sec.php?tipo=10` | Prefeito | `/governo/prefeito` | MIGRAR_CONTEUDO | ROTA EXISTE; CONTEÚDO PENDENTE |
| `/sec.php?tipo=12` | Vice-prefeito | `/governo/vice-prefeito` | MIGRAR_CONTEUDO | ROTA EXISTE; CONTEÚDO PENDENTE |
| `/sec.php?tipo=11` | Gabinete | `/secretarias/gabinete` | MIGRAR_CONTEUDO | ROTA DINÂMICA; CADASTRO PENDENTE |
| `/sec.php?tipo=13` | Procuradoria | `/secretarias/procuradoria` | MIGRAR_CONTEUDO | ROTA DINÂMICA; CADASTRO PENDENTE |
| `/sec.php?tipo=22` | Controladoria | `/secretarias/controladoria` | MIGRAR_CONTEUDO | ROTA DINÂMICA; CADASTRO PENDENTE |
| `/sec.php?tipo=23` | Ouvidoria | `/ouvidoria` | MIGRAR_CONTEUDO | EXISTE |
| `/sec.php?tipo=24` | Saúde | `/secretarias/saude` | MIGRAR_CONTEUDO | ROTA DINÂMICA; VALIDAR CADASTRO |
| `/sec.php?tipo=25` | Assistência Social / Habitação / Cidadania | `/secretarias/assistencia-social` | MIGRAR_CONTEUDO | ROTA DINÂMICA; CADASTRO PENDENTE |
| `/sec.php?tipo=26` | Obras / Infraestrutura / Produção / Meio Ambiente | `/secretarias/obras-infraestrutura` | MIGRAR_CONTEUDO | ROTA DINÂMICA; CADASTRO PENDENTE |
| `/sec.php?tipo=27` | Educação | `/secretarias/educacao` | MIGRAR_CONTEUDO | ROTA DINÂMICA; VALIDAR CADASTRO |
| `/sec.php?tipo=28` | Gestão Financeira / Administrativa | `/secretarias/administracao` | MIGRAR_CONTEUDO | ROTA DINÂMICA; VALIDAR CADASTRO |
| `/sec.php?tipo=29` | Esportes / Cultura / Turismo | `/secretarias/esportes-cultura-turismo` | MIGRAR_CONTEUDO | ROTA DINÂMICA; CADASTRO PENDENTE |
| `/e-sic/sec.php?tipo={id}` | Duplicata da estrutura administrativa | mesmo destino de `sec.php?tipo={id}` | 301_APOS_DESTINO | DINAMICO |

## Notícias e informativos

A página nova `/noticias` possui filtro por área editorial e a API persiste essa classificação. As famílias de listagem continuam dependendo da migração item a item: uma notícia antiga só deve receber categoria após a correspondência ser confirmada.

| URL antiga | Categoria observada | Destino planejado | Ação | Estado |
|---|---|---|---|---|
| `/noticias.php?page={n}&q=&tipo=all` | Feed histórico misto | `/noticias` + importação item-a-item | MIGRAR_CONTEUDO | EXISTE/DINAMICO |
| `/noticias25.php?tipo=17` | Prefeitura | `/noticias?category=PREFEITURA` | MIGRAR_CONTEUDO | FILTRO EXISTE; MIGRAÇÃO PENDENTE |
| `/noticias25.php?tipo=18` | Educação | `/noticias?category=EDUCACAO` | MIGRAR_CONTEUDO | FILTRO EXISTE; MIGRAÇÃO PENDENTE |
| `/noticias25.php?tipo=19` | Infraestrutura | `/noticias?category=INFRAESTRUTURA` | MIGRAR_CONTEUDO | FILTRO EXISTE; MIGRAÇÃO PENDENTE |
| `/noticias25.php?tipo=20` | Saúde | `/noticias?category=SAUDE` | MIGRAR_CONTEUDO | FILTRO EXISTE; MIGRAÇÃO PENDENTE |
| `/noticias25.php?tipo=21` | Esporte | `/noticias?category=ESPORTE` | MIGRAR_CONTEUDO | FILTRO EXISTE; MIGRAÇÃO PENDENTE |
| `/noticias25.php?tipo=22` | Meio Ambiente | `/noticias?category=MEIO_AMBIENTE` | MIGRAR_CONTEUDO | FILTRO EXISTE; MIGRAÇÃO PENDENTE |
| `/noticias25.php?tipo=23` | Cultura | `/noticias?category=CULTURA` | MIGRAR_CONTEUDO | FILTRO EXISTE; MIGRAÇÃO PENDENTE |
| `/noticias25.php?tipo=24` | Assistência Social | `/noticias?category=ASSISTENCIA_SOCIAL` | MIGRAR_CONTEUDO | FILTRO EXISTE; MIGRAÇÃO PENDENTE |
| `/noticias25.php?tipo=25` | AMHAD / Habitação | `/noticias?category=HABITACAO` | MIGRAR_CONTEUDO | FILTRO EXISTE; MIGRAÇÃO PENDENTE |
| `/noticias25.php?tipo=26` | Covid-19 | `/noticias?category=COVID_19` | MIGRAR_CONTEUDO | FILTRO EXISTE; MIGRAÇÃO PENDENTE |
| `/exibe.php?id={id}` | Notícia individual | `/noticias/{slug}` | MIGRAR_CONTEUDO | DINAMICO |
| `/exibe23.php?id={id}` | Notícia individual/duplicata | `/noticias/{slug}` | MIGRAR_CONTEUDO | DINAMICO |

Regra obrigatória: quando `exibe.php?id=X` e `exibe23.php?id=X` representarem o mesmo conteúdo, os dois caminhos devem resolver para o **mesmo slug canônico** e nunca gerar duas notícias novas.

## Serviços e Carta de Serviços

| URL antiga | Destino planejado | Ação | Estado | Observação |
|---|---|---|---|---|
| `/servicos1.php` | `/servicos` | MIGRAR_CONTEUDO | EXISTE | Inventariar atalhos NFSe, concursos, licitações e webmail. |
| `/servicos-2.php` | `/servicos` | MIGRAR_CONTEUDO | EXISTE | Inventariar cartilha ISSQN/obra e guia IPTU. |
| `/carta-servicos` | `/servicos` | MIGRAR_CONTEUDO | EXISTE | Catálogo novo deve cobrir todas as categorias antigas. |
| `/carta-servicos-detalhes.php?tipo={id}` | `/servicos` + categoria/serviços correspondentes | MIGRAR_CONTEUDO | DINAMICO | Não reduzir categoria antiga a uma página vazia. |
| `/servicos/servidor` | serviço/área do servidor | MIGRAR_CONTEUDO | DINAMICO | Conteúdo antigo inclui holerite, webmail, compras/almoxarifado e protocolo. |
| `/compras-almoxarifado.php{?query}` | área de servidor/compras | MIGRAR_CONTEUDO | DINAMICO | Preservar integrações, não copiar autenticação externa. |
| `/servicos/lgpd` | `/privacidade` | MIGRAR_CONTEUDO | EXISTE | Migrar DPO, documentos e políticas antes do 301. |
| `/servicos/licenciamento-ambiental` | `/servicos/licenciamento-ambiental` | MIGRAR_CONTEUDO | DINAMICO | Página antiga possui diversos formulários/downloads. |
| `/servicos/educacao` | `/servicos` | MIGRAR_CONTEUDO | EXISTE | Criar filtro/área equivalente antes de perder contexto. |
| `/servicos/saude` | `/servicos` | MIGRAR_CONTEUDO | EXISTE | Criar filtro/área equivalente antes de perder contexto. |

Categorias observadas na Carta de Serviços e que precisam de cobertura no catálogo novo: Assistência Social/Habitação/Cidadania; Obras/Infraestrutura/Produção/Meio Ambiente; Educação; Saúde; Esportes/Cultura/Turismo; Gestão Financeira/Administrativa; Agência de Meio Ambiente; Agência de Trânsito; Agência de Habitação; Defesa Civil; Gabinete.

## Dados Abertos

| URL antiga | Destino planejado | Ação | Estado |
|---|---|---|---|
| `/dados-abertos` | `/dados-abertos` | PRESERVAR | EXISTE |
| `/dados-abertos-detalhes.php?tipo={id}` | `/dados-abertos/{slug}` | MIGRAR_CONTEUDO | DINAMICO |

O domínio novo `Dataset/DatasetVersion` é a fonte de verdade. Não recriar os datasets históricos como `PortalResource(DATASET)`.

Categorias observadas que precisam virar Dataset/categoria ou registro de migração: Procon; Procuradoria; Agência de Meio Ambiente; Habitação; Trânsito; Educação; Saúde; Obras/Produção/Infraestrutura; Gestão Financeira/Administração; Cultura/Esporte/Turismo; Assistência Social/Cidadania; Defesa Civil; Gabinete; FUNDEB; Conselho Municipal de Saúde; CMDCA; CMAS; Nova Lei de Licitações/Decretos; PCA; OCP; COMADE; FHIS; VTN; lista de espera de creche; Dengue; Pro Rural.

## e-SIC / Acesso à Informação

| URL antiga | Destino planejado | Ação | Estado |
|---|---|---|---|
| `/e-sic/` | `/acesso-a-informacao` | MIGRAR_CONTEUDO | EXISTE |
| `/e-sic/estatisticas.php` | `/acesso-a-informacao/estatisticas` | MIGRAR_CONTEUDO | ROTA EXISTE; CONTEÚDO PENDENTE |
| `/e-sic/perguntas-respostas.php` | `/acesso-a-informacao/perguntas` | MIGRAR_CONTEUDO | ROTA EXISTE; CONTEÚDO PENDENTE |
| `/e-sic/cadastro.php` | `/acesso-a-informacao` / provider de solicitação | 301_APOS_DESTINO | EXISTE/INTEGRAÇÃO |
| `/e-sic/contato.php` | `/acesso-a-informacao` ou `/contatos` | 301_APOS_DESTINO | EXISTE |
| `/e-sic/diario.php?tipo=1` | `/diario-oficial` | 301_APOS_DESTINO | EXISTE |

O legado do e-SIC é um subsistema separado. Login/sessão antigos **não devem ser migrados** como credenciais para a nova plataforma.

## Prestação de contas (`/e-sic/prestacao_contas.php?tipo=`)

A nova `/transparencia` é um hub e as categorias verificadas abaixo possuem páginas próprias, pesquisa, filtro de tipo/ano, paginação, origem, hash e download governado. Um registro só aparece publicamente depois de o arquivo ser aprovado e o `PublicDocument` ser publicado.

| Tipo | Categoria observada | Destino planejado | Estado |
|---:|---|---|---|
| 4 | RREO | `/transparencia/rreo` | EXISTE — ACERVO FILTRADO |
| 6 | RGF | `/transparencia/rgf` | EXISTE — ACERVO FILTRADO |
| 7 | Contratos de Convênios | `/transparencia/convenios` | EXISTE — ACERVO FILTRADO |
| 8 | Dados Gerais | `/transparencia/dados-gerais` | EXISTE — ACERVO FILTRADO |
| 9 | PPA | `/transparencia/ppa` | EXISTE — ACERVO FILTRADO |
| 10 | LDO | `/transparencia/ldo` | EXISTE — ACERVO FILTRADO |
| 11 | LOA | `/transparencia/loa` | EXISTE — ACERVO FILTRADO |
| 12 | Balancetes | `/transparencia/balancetes` | EXISTE — ACERVO FILTRADO |
| 13 | Relatório de Gestão | `/transparencia/relatorios-gestao` | EXISTE — ACERVO FILTRADO |
| 14 | Recursos Federais | `/transparencia/recursos-federais` | EXISTE — ACERVO FILTRADO |
| 15 | Estrutura Organizacional | `/secretarias` | EXISTE, validar conteúdo |
| 16 | Relatório de Gestão SUS | `/transparencia/relatorios-gestao-sus` | EXISTE — ACERVO FILTRADO |
| 17 | Decretos | `/legislacao?subcategory=DECRETOS` | EXISTE — ACERVO FILTRADO |
| 18 | Receitas e Despesas COSIP | `/transparencia/cosip` | EXISTE — ACERVO FILTRADO |
| 19 | Balanços | `/transparencia/balancos` | EXISTE — ACERVO FILTRADO |
| 20 | UFID | `/transparencia/ufid` | EXISTE — ACERVO FILTRADO |
| 21 | Mais oportunidades / Editais | `/licitacoes` ou área própria | EXISTE/DINAMICO |
| 22 | Documentos e arquivos para download | `/transparencia/documentos` | EXISTE — ACERVO FILTRADO |

O tipo 5 não foi confirmado na auditoria manual; não inventar significado. O crawler deverá registrar qualquer ocorrência real.

## Legislação do e-SIC

Família: `/e-sic/legislacao.php?tipo={id}` → acervo legislativo novo. O frontend novo possui `/legislacao` com busca, paginação, filtro por espécie, origem, hash e download governado. A associação exata entre cada `tipo` legado e a subcategoria nova continua dependente do inventário e da aprovação editorial.

Taxonomias observadas no legado e que precisam de correspondência: Decretos; Leis Complementares; Leis Ordinárias; Lei Orgânica; Portarias; Editais; Resoluções; Plano de Cargos e Carreira; Estatuto do Servidor; Legislação Tributária; Lei Municipal; PPA; Regimento Interno; Plano Diretor Participativo; Recomendações do Ministério Público; Código de Ética do Servidor; Controladoria Geral do Município; Instrução Normativa; Plano Integrado de Saneamento/Resíduos; Requerimentos; LDO; Código Sanitário; Dados da Dengue; Conselho Municipal de Saúde; Coleta de Galhos; Organograma Institucional; Plano Municipal de Educação; Plano Municipal de Saúde; Manifestação de Interesse Social.

A associação exata `tipo -> taxonomia` deve ser produzida pelo crawl/inventário; não preencher IDs por inferência.

## Licitações e contratos

| URL antiga | Destino planejado | Ação | Estado |
|---|---|---|---|
| `/licitacoes/` | `/licitacoes` | 301_APOS_DESTINO | EXISTE |
| `/e-sic/avisos-licitacoes.php?tipo=4` | `/licitacoes?subcategory=AVISOS` | MIGRAR_CONTEUDO | ACERVO EXISTE |
| `/e-sic/editais_licitacoes.php?tipo=1` | `/licitacoes?subcategory=EDITAIS` | MIGRAR_CONTEUDO | ACERVO EXISTE |
| `/e-sic/resultados_licitacoes.php?tipo=2` | `/licitacoes?subcategory=RESULTADOS` | MIGRAR_CONTEUDO | ACERVO EXISTE |
| `/e-sic/contratos.php?tipo=3` | `/licitacoes?subcategory=CONTRATOS` | MIGRAR_CONTEUDO | ACERVO EXISTE |
| `/e-sic/calendario.php` | `/licitacoes/calendario` | MIGRAR_CONTEUDO | ROTA EXISTE; CONTEÚDO PENDENTE |
| `/licitacoes/contratos.php?tipo=3` | `/licitacoes?subcategory=CONTRATOS` | MIGRAR_CONTEUDO | ACERVO EXISTE; origem observada em 404 |
| `/cadastro-fornecedor/` | fluxo/provider de fornecedores | MANTER_EXTERNO ou integrar | EXTERNO/DINAMICO |

O hub novo `/licitacoes` não é, por si só, substituto para milhares de registros históricos. Antes do cutover, cada processo/documento precisa ter uma decisão explícita: importar metadados, preservar documento, apontar para sistema oficial externo ou arquivar com evidência.

Modalidades observadas no subsistema legado incluem Pregão Presencial, Tomada de Preços, Dispensa, Inexigibilidade, Chamada Pública, Pregão Eletrônico e Convite.

## Diário Oficial

| URL antiga | Destino planejado | Ação | Estado |
|---|---|---|---|
| `/e-sic/diario.php?tipo=1` | `/diario-oficial` | MIGRAR_CONTEUDO | EXISTE |
| `/e-sic/uploads/{arquivo-diario}.pdf` | edição/documento legado correspondente | MIGRAR_CONTEUDO | DINAMICO |

Edições históricas devem ser marcadas como legado e manter origem/hash/evidência. Não aplicar assinatura demo a documento histórico como se fosse assinatura oficial.

## Arquivos e downloads

Famílias conhecidas a inventariar por URL + hash + tamanho + content type:

- `/e-sic/uploads/{path}`;
- `/e-sic/uploads/editais/{path}`;
- `/e-sic/uploads/avisos/{path}`;
- `/e-sic/uploads/carta/{path}`;
- PDFs do Diário e demais documentos diretamente abaixo de `/e-sic/uploads/`;
- downloads ligados a licenciamento ambiental, LGPD, prestação de contas, legislação, licitações e Dados Abertos.

Destino público: listagem em `/transparencia/documentos` (ou categoria dedicada) e download governado em `/api/v1/public/documents/{id}/download`. Não ativar 301 para o download antes de o asset estar aprovado e o documento publicado.

## Sistemas externos — não importar cegamente

Integrações/atalhos externos observados no portal incluem tributação/serviços financeiros, NFSe/ISS, matrícula digital, legislação em serviço externo, webmail e outros sistemas especializados.

Regra:

1. registrar URL e finalidade;
2. verificar proprietário/provider e disponibilidade;
3. criar `IntegrationStatus`/link governado quando aplicável;
4. manter externo até haver decisão explícita de integração;
5. nunca coletar ou migrar credenciais do sistema legado.

## GEO-OBRAS e mapas

| URL antiga | Destino planejado | Ação | Estado |
|---|---|---|---|
| `/GEO-OBRAS/` | `/obras` + integração equivalente validada | MANTER_EXTERNO / MIGRAR_METADADOS | ROTA EXISTE; INTEGRAÇÃO PENDENTE |

Não ativar o redirect para `/obras` enquanto a fonte externa, o conteúdo oficial e a responsabilidade pela atualização não forem validados. A rota existe e expõe estado vazio administrável, mas não simula mapa nem andamento de obra.

## Requisitos do crawler para fechar o inventário

O crawl de produção deve usar:

- host permitido exatamente `www.deodapolis.ms.gov.br`/host canônico definido no job;
- SSRF-safe DNS/IP policy já existente;
- `MaxPages` alto o suficiente para todo o acervo;
- query strings significativas preservadas;
- parâmetros de tracking removidos;
- paginação no mesmo caminho sem consumir profundidade estrutural;
- arquivos classificados e hasheados;
- evidência `DRY_RUN_SUMMARY` com `truncatedByLimit=false` para um inventário considerado completo;
- execução adicional para hosts/subsistemas que não pertençam ao mesmo host autorizado, quando houver decisão explícita.

O crawler não deve ser considerado completo se `truncatedByLimit=true`, se houver paginação conhecida não percorrida ou se uma família de URL conhecida não aparecer no inventário.

## Checklist antes do cutover

- [x] Executar crawl completo do host legado com `truncatedByLimit=false`.
- [x] Reconciliar total do crawler com as famílias públicas conhecidas.
- [x] Exportar todas as `LegacyUrl` com estado/classificação/hash pelo relatório CSV administrativo.
- [ ] Deduplicar `exibe.php` e `exibe23.php` por conteúdo/ID/hash.
- [ ] Mapear todas as páginas de secretaria/governo.
- [ ] Mapear 100% das categorias de Carta de Serviços.
- [ ] Mapear 100% dos datasets/documentos de Dados Abertos.
- [ ] Mapear todas as famílias de prestação de contas.
- [ ] Mapear taxonomia completa da legislação.
- [x] Inventariar avisos, editais, resultados e contratos.
- [x] Inventariar PDFs/planilhas/downloads por hash.
- [x] Separar integrações externas de conteúdo a migrar.
- [x] Implementar todos os destinos previamente marcados `IMPLEMENTAR`; conteúdo e integrações pendentes continuam bloqueando os respectivos 301.
- [ ] Validar que cada redirect de produção aponta para HTTP 200/rota válida.
- [ ] Gerar evidência final de origem → destino → ação → razão → status.

## Regra de conclusão

Este mapa só pode ser marcado como **100% coberto** quando o inventário automatizado tiver terminado sem truncamento e cada URL descoberta tiver uma decisão persistida (`MIGRAR`, `REDIRECT`, `EXTERNAL`, `ARCHIVE` ou equivalente). A auditoria manual serve para definir famílias e detectar lacunas; ela não substitui o crawl item-a-item.
