# Auditoria do portal atual de Deodápolis/MS

**Data da coleta:** 25 de agosto de 2026  
**Domínio auditado:** `https://deodapolis.ms.gov.br/` e variante `www`  
**Escopo:** navegação pública, profundidade máxima 2, limite de 180 respostas HTML, inventário de links e documentos descobertos. Nenhum formulário foi submetido e nenhum dado autenticado foi acessado.

## Evidências e método

- O repositório de destino estava vazio, sem arquivos e sem commits.
- `robots.txt` e `sitemap.xml` retornaram 404; portanto, a descoberta partiu da home, e-SIC, Serviços, Licitações, Carta de Serviços e Dados Abertos.
- Foram consultadas 180 páginas: 173 responderam 200 e 7 responderam 404.
- Foram descobertas 1.238 URLs internas únicas e 663 referências a arquivos legados (`PDF`, `DOCX` e afins) no recorte controlado.
- O portal expõe ao menos 74 páginas de contratos, 71 de editais, 56 de avisos, 51 de resultados e 57 notícias no grafo imediatamente alcançável. Esses números são páginas/URLs descobertas, não quantidade definitiva de registros.
- A coleta automatizada com validação TLS padrão falhou no host `www`; uma segunda coleta somente leitura, sem cookies de autenticação, foi usada apenas para inventário. A correção do certificado/encadeamento deve preceder o cutover.
- O HTML declara UTF-8, porém múltiplas páginas entregam mojibake (`Servi�o`, `Educa��o`, `Transpar�nia`) e títulos incorretos como “Deodapolis - MG”.

## Inventário de recursos

| Tipo | Nome | URL atual | Origem | Destino proposto | Conteúdo | Status atual | Migrar? | Redirecionar? | Integração externa? | Risco |
|---|---|---|---|---|---|---|---|---|---|---|
| Portal | Home | `https://www.deodapolis.ms.gov.br/` | CMS legado | `/` | Notícias, banners, atalhos e serviços | 200 | Sim | Canonicalizar host | Não | Alto: TLS, cache desabilitado, mojibake, imagens sem ALT |
| Institucional | Sobre o município | `/institucional/sobre` | CMS legado | `/municipio` | História e dados municipais | 200 | Sim | 301 | Não | Médio: revisar atualidade |
| Institucional | Missão | `/institucional/missao` | CMS legado | `/municipio/gestao` | Missão institucional | 200 | Sim | 301 | Não | Baixo |
| Governo | Secretarias e agências | `/sec.php?tipo={10..34}` | CMS legado | `/secretarias/{slug}` | 12 áreas na home e 17 no e-SIC | 200 | Sim | 301 por ID | Não | Alto: duplicação entre portal/e-SIC |
| Notícias | Listas por órgão | `/noticias25.php?tipo={17..26}` | CMS legado | `/noticias?secretaria={slug}` | Informativos por secretaria/agência | 200 | Sim | 301 | Não | Alto: paginação e encoding |
| Notícias | Notícia individual | `/exibe23.php?id={id}` | CMS legado | `/noticias/{slug}` | Matéria, imagens e metadados | 200 | Sim | 301 por ID | Não | Alto: preservar SEO e mídia |
| Notícias | Lista unificada | `/noticias.php?page={n}&q=&tipo=all` | CMS legado | `/noticias?page={n}` | Notícias e documentos de licitação misturados | 200 | Sim | 301 | Não | Alto: taxonomia misturada |
| Serviços | Serviços gerais legados | `/servicos-2.php` | Portal 2018 | `/servicos` | IPTU, concursos, licitações e webmail | 200 | Sim | 301 | Parcial | Alto: página antiga e mojibake |
| Serviços | Servidor | `/servicos/servidor` | CMS legado | `/servicos/servidor` | Acessos do servidor | 200 | Sim | Preservar | Parcial | Médio |
| Serviços | Educação | `/servicos/educacao` | CMS legado | `/servicos?area=educacao` | Atalhos de educação | 200 | Sim | 301 | Parcial | Médio: mojibake |
| Serviços | Saúde | `/servicos/saude` | CMS legado | `/servicos?area=saude` | Saúde transparente e protocolos | 200 | Sim | 301 | Sim | Médio |
| Serviços | LGPD | `/servicos/lgpd` | CMS legado | `/privacidade` | Informações LGPD | 200 | Sim | 301 | Não | Alto: conteúdo e contato precisam validação jurídica |
| Serviços | Licenciamento ambiental | `/servicos/licenciamento-ambiental` | CMS legado | `/servicos/licenciamento-ambiental` | Formulários, orientações e arquivos | 200 | Sim | Preservar/301 | Parcial | Alto: 36 imagens/arquivos e requisitos legais |
| Serviços | Emissão de IPTU | `https://e-gov.betha.com.br/cdweb/03114-489/contribuinte/main.faces` | Betha | `/servicos/emitir-iptu` | Guia de IPTU | Ativo externo | Não | Não | Sim, LINKAR | Médio: disponibilidade externa |
| Serviços | ISSWEB | `https://e-gov.betha.com.br/livroeletronico2/02020-008/login.faces` | Betha | `/servicos/issweb` | Livro eletrônico ISS | Ativo externo | Não | Não | Sim, LINKAR | Médio |
| Serviços | NFSe | `https://e-gov.betha.com.br/e-nota/login.faces` | Betha | `/servicos/nfse` | Nota fiscal eletrônica | Ativo externo | Não | Não | Sim, LINKAR | Médio |
| Serviços | Cidadão Web/Tributação | `https://e-gov.betha.com.br/cdweb/03114-489/contribuinte/main.faces` | Betha | `/servicos/cidadao-web` | Serviços tributários | Ativo externo | Não | Não | Sim, LINKAR | Médio: atalhos duplicados |
| Serviços | Matrícula Digital | `https://educacaodeodapolis.genesis.tec.br/matriculadigital/` | Genesis | `/servicos/matricula-digital` | Matrícula escolar | Ativo externo | Não | Não | Sim, LINKAR | Médio |
| Saúde | Saúde transparente | `https://deodapolis.esaude.genesiscloud.tec.br/publico/saude-transparente` | Genesis | `/transparencia/saude` | Dados públicos de saúde | Ativo externo | Não | Não | Sim, LINKAR | Médio |
| Saúde | Protocolos | `https://deodapolis.esaude.genesiscloud.tec.br/protocolos` | Genesis | `/servicos/protocolos-saude` | Consulta de protocolos | Ativo externo | Não | Não | Sim, LINKAR | Médio |
| Assistência | Transparência SUAS | `https://deodapolis.esuas.genesiscloud.tec.br/transparencia` | Genesis | `/transparencia/assistencia` | Dados públicos de assistência | Ativo externo | Não | Não | Sim, LINKAR | Médio |
| Transparência | Portal Betha | `https://transparencia.betha.cloud/#/whlvWPlYqIFeODuQD0dKgA==i` | Betha | `/transparencia` | Receitas, despesas, pessoal | Ativo externo | Não | Não | Sim, LINKAR | Alto: fragmento opaco e monitoramento especial |
| Acesso à informação | e-SIC | `/e-sic/` | Aplicação PHP separada | `/acesso-a-informacao` | Login, cadastro, legislação e atendimento | 200 | Parcial | 301/hub | Parcial | Crítico: login legado e links quebrados |
| Acesso à informação | Cadastro e-SIC | `/e-sic/cadastro.php` | e-SIC | `/acesso-a-informacao` | Cadastro do cidadão | 404 | Não sem autorização | 301 | Sim/legado | Crítico: link morto no menu |
| Acesso à informação | Estatísticas e-SIC | `/e-sic/estatisticas.php` | e-SIC | `/acesso-a-informacao/estatisticas` | Estatísticas | 200 | Sim | 301 | Não | Médio |
| Acesso à informação | Perguntas e respostas | `/e-sic/perguntas-respostas.php` | e-SIC | `/acesso-a-informacao/perguntas` | FAQ | 200 | Sim | 301 | Não | Médio |
| Acesso à informação | Fala.BR/Ouvidoria | `https://falabr.cgu.gov.br/web/home` | CGU | `/ouvidoria` | Reclamação, denúncia, sugestão, elogio | Ativo externo | Não | Não | Sim, LINKAR | Médio: orientar anonimato e redirecionamento |
| Carta de Serviços | Catálogo atual | `/carta-servicos` | CMS legado | `/servicos` | 11 categorias/áreas | 200 | Sim, estruturar | 301 | Não | Alto: imagem remota repetida de outro ente |
| Carta de Serviços | Detalhes por tipo | `/carta-servicos-detalhes.php?tipo={id}` | CMS legado | `/servicos?area={slug}` | Serviços por categoria | 200 | Sim, estruturar | 301 | Não | Alto: parametrização opaca |
| Dados abertos | Catálogo atual | `/dados-abertos` | CMS legado | `/dados-abertos` | 26 categorias descobertas | 200 | Sim | Preservar | Parcial | Alto: ícones hotlinkados do Flaticon |
| Dados abertos | Categoria | `/dados-abertos-detalhes.php?tipo={id}` | CMS legado | `/dados-abertos/{slug}` | Arquivos e metadados | 200 | Sim | 301 | Parcial | Alto: normalizar formatos/licenças |
| Licitações | Portal por modalidade | `/licitacoes/` | Aplicação PHP separada | `/licitacoes` | 7 modalidades e contratos | 200 | Parcial/indexar | Preservar/301 | Parcial | Alto: identidade e navegação independentes |
| Licitações | Avisos | `/e-sic/avisos-licitacoes.php?tipo=4` | e-SIC | `/licitacoes?fase=aviso` | Avisos paginados | 200 | Sim se autorizado | 301 | Não | Alto: 56 URLs de paginação descobertas |
| Licitações | Editais | `/e-sic/editais_licitacoes.php?tipo=1` | e-SIC | `/licitacoes?fase=edital` | Editais e PDFs | 200 | Sim se autorizado | 301 | Não | Alto: 71 URLs de paginação descobertas |
| Licitações | Resultados | `/e-sic/resultados_licitacoes.php?tipo=2` | e-SIC | `/licitacoes?fase=resultado` | Resultados e PDFs | 200 | Sim se autorizado | 301 | Não | Alto: 51 URLs de paginação descobertas |
| Licitações | Contratos | `/e-sic/contratos.php?tipo=3` | e-SIC | `/contratos` | Contratos e fornecedores | 200 | Sim se autorizado | 301 | Não | Alto: 74 URLs de paginação descobertas |
| Licitações | Contratos no subportal | `/licitacoes/contratos.php?tipo=3` | Portal de licitações | `/contratos` | Atalho de contratos | 404 | Não | 301 | Não | Alto: link quebrado |
| Licitações | Calendário | `/e-sic/calendario.php` | e-SIC | `/licitacoes/calendario` | Calendário | 200 | Sim | 301 | Não | Médio |
| Fornecedores | Cadastro | `/cadastro-fornecedor/` | Aplicação separada | `/licitacoes/fornecedores` | Orientação e formulário | 200 | Sim/Integrar | 301 | Parcial | Alto: validar tratamento de dados |
| Obras | GEO-OBRAS | `/GEO-OBRAS/` | Aplicação separada | `/obras` | Gestão/consulta de obras | 200 | Integrar | 301/hub | Parcial | Alto: login/formulário e mojibake |
| Legislação | Leis Municipais | `https://leismunicipais.com.br/prefeitura/ms/deodapolis` | Leis Municipais | `/legislacao` | Legislação consolidada | Ativo externo | Não | Não | Sim, LINKAR | Baixo/médio |
| Legislação | Índices e-SIC | `/e-sic/legislacao.php?tipo={1..30}` | e-SIC | `/legislacao?tipo={slug}` | Leis, decretos, resoluções e regulamentos | 200 | Sim | 301 | Parcial | Alto: 29 categorias e muitos arquivos |
| Prestação de contas | PPA | `/e-sic/prestacao_contas.php?tipo=9` | e-SIC | `/transparencia/orcamento/ppa` | PPA e anexos | 200 | Sim | 301 | Não | Alto: preservar versões |
| Prestação de contas | LDO | `/e-sic/prestacao_contas.php?tipo=10` | e-SIC | `/transparencia/orcamento/ldo` | LDO e anexos | 200 | Sim | 301 | Não | Alto |
| Prestação de contas | LOA | `/e-sic/prestacao_contas.php?tipo=11` | e-SIC | `/transparencia/orcamento/loa` | LOA e anexos | 200 | Sim | 301 | Não | Alto |
| Prestação de contas | Demais categorias | `/e-sic/prestacao_contas.php?tipo={4..22}` | e-SIC | `/transparencia/{slug}` | RREO, RGF, convênios, balancetes, SUS, COSIP, balanços, UFID | 200 | Sim | 301 | Parcial | Alto: 18 categorias |
| Diário Oficial | Acervo legado | `https://imprensaoficialmunicipal.com.br/deodapolis` | Imprensa Oficial Municipal | `/diario-oficial` | Edições publicadas | Ativo externo | Importar metadados/PDFs autorizados | Não | Sim, INTEGRAR | Crítico: autenticidade e continuidade |
| Diário Oficial | Rota e-SIC antiga | `/e-sic/diario.php?tipo=1` | e-SIC | `/diario-oficial` | Atalho antigo | 404 | Não | 301 | Sim | Crítico: link morto visível |
| Concursos | Listagem | `/e-sic/concursos.php?tipo=1` | e-SIC | `/concursos` | Editais e resultados | 200 | Sim | 301 | Não | Alto: 6 páginas descobertas |
| Conselhos | Central de Conselhos | `/pages/central-conselhos/` | CMS legado | `/conselhos` | Conselhos e documentos | 200 | Sim | 301 | Não | Médio: H1 incorreto |
| Licitações | Nova Lei de Licitações | `/pages/nova-lei-licitacoes/` | CMS legado | `/licitacoes/nova-lei` | Orientações e documentos | 200 | Sim | 301 | Não | Médio: H1 incorreto |
| Rural | VTN/ITR | `/pages/vpn/` | CMS legado | `/servicos/vtn-itr` | Relatório de preço de terra nua | 200 | Sim | 301 | Não | Médio: slug incoerente |
| Dados abertos | Pro Rural | `/dados-abertos-detalhes.php?tipo=29` | CMS legado | `/dados-abertos/pro-rural` | Consulta de processos de maquinário | 200 | Integrar | 301 | Parcial | Médio |
| Webmail | Correio institucional | `http://webmail.deodapolis.ms.gov.br` | Provedor atual | `/admin/email` | Acesso ao webmail | HTTP/link externo | Não | Não | Sim, LINKAR | Alto: HTTP e fornecedor a validar |
| Social | Facebook | `https://www.facebook.com/nossadeodapolis` | Meta | Configuração municipal | Perfil oficial | Ativo externo | Não | Não | Sim | Baixo |
| Social | Instagram | `https://www.instagram.com/nossadeodapolis` | Meta | Configuração municipal | Perfil oficial | Ativo externo | Não | Não | Sim | Baixo |
| Social | YouTube | `https://www.youtube.com/@nossadeodapolis` | Google | Configuração municipal | Canal oficial | Ativo externo | Não | Não | Sim | Baixo |
| Documento | Acervo `/e-sic/uploads/` | `/e-sic/uploads/**` | Storage legado | Storage S3 + `/documentos/{id}` | 663 referências no recorte | Parcialmente acessível | Sim | 301 por arquivo | Não | Crítico: checksum, malware, metadados e volume |
| Marca | Logo 2025 | `/imagens/logo-2025.png` | Portal atual | Branding municipal | Identidade oficial usada no cabeçalho | 200 | Sim | Preservar ativo | Não | Médio: corrigir ALT “MG” duplicado |
| Ativo quebrado | Logo por IP legado | `http://192.163.209.3/~pmdeo/imagens/logo.png` | Host/IP legado | Remover após importar | Logo carregado no rodapé/subportais | HTTP/IP privado público | Sim | Não | Não | Crítico: hotlink e conteúdo misto |
| Contato | Telefone útil antigo | `/pagina/97_Telefones-Uteis.html` | CMS legado | `/contatos` | Telefones úteis | 404 | Reconstruir de fonte validada | 301 | Não | Alto |
| Contato | Página e-SIC | `/e-sic/contato.php` | e-SIC | `/acesso-a-informacao` | Atendimento presencial | 404 | Sim, por fonte validada | 301 | Não | Alto |

## Problemas transversais confirmados

1. **Continuidade:** ausência de sitemap, muitas URLs parametrizadas e acervo amplo exigem importação idempotente com checksum e evidência.
2. **Encoding:** mojibake aparece em títulos, alternativas de imagem e conteúdo; o migrador deve detectar a origem e normalizar para UTF-8 sem alterar o original arquivado.
3. **Acessibilidade:** muitas imagens não têm texto alternativo; páginas legadas usam títulos/H1 incorretos e controles pouco descritivos.
4. **Segurança/operação:** TLS inconsistente no host `www`, webmail em HTTP e conteúdo carregado por IP/terceiros.
5. **Governança:** Carta de Serviços e Dados Abertos estão organizados por IDs opacos; notícias e licitações aparecem misturadas na listagem geral.
6. **Disponibilidade:** integrações Betha, Genesis, Leis Municipais, Fala.BR e Imprensa Oficial devem ser linkadas/monitoradas sem derrubar o portal quando indisponíveis.
7. **Conteúdo:** telefones divergentes (`2180-0805` e `3448-1925`) exigem validação humana antes de produção.

## Estratégia de continuidade

- **MIGRAR:** páginas institucionais, notícias, secretarias, Carta de Serviços, Dados Abertos, legislação/documentos autorizados, concursos, conselhos e metadados do Diário.
- **INTEGRAR:** Diário histórico, GEO-OBRAS, dados públicos de saúde/assistência e licitações quando houver API ou autorização.
- **LINKAR:** Betha, Matrícula Digital, Fala.BR, Leis Municipais e webmail, sempre com aviso de saída e health status.
- **SUBSTITUIR:** busca, catálogo de serviços, CMS, mídia, menus, home, monitor de links e novo Diário para futuras edições.
- **REDIRECIONAR:** todas as rotas parametrizadas relevantes para slugs estáveis, com 301 e evidência.
- **ARQUIVAR:** HTML original e checksums de itens migrados; nenhum ato oficial será reescrito silenciosamente.

## Limites da auditoria

O crawl foi deliberadamente limitado para não sobrecarregar o portal. A relação completa de milhares de registros paginados e a validação individual de todos os 663 arquivos exigem o `MigrationJob` em modo `DISCOVER/FETCH/VERIFY`, executado com janela autorizada. Nenhum dado pessoal, área autenticada ou formulário foi coletado.
