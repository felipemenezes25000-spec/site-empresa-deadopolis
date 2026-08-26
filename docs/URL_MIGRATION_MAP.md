# Mapa inicial de migração de URLs

Este mapa é versionado e deve ser expandido pelo `MigrationJob` antes do cutover. `VALIDADO` significa apenas que a rota antiga foi observada na auditoria; a coluna não afirma que o redirect novo já está implantado.

| URL antiga | URL nova | Tipo | Redirect | Conteúdo migrado | Validado |
|---|---|---|---|---|---|
| `/` | `/` | Home | 301 apenas para canonical host | Não | Sim, origem |
| `/index.php` | `/` | Home | 301 | Não | Sim, origem |
| `/institucional/sobre` | `/municipio` | Página | 301 | Não | Sim, origem |
| `/institucional/missao` | `/municipio/gestao` | Página | 301 | Não | Sim, origem |
| `/sec.php?tipo={id}` | `/secretarias/{slug}` | Secretaria | 301 por mapeamento | Não | Sim, padrão |
| `/e-sic/sec.php?tipo={id}` | `/secretarias/{slug}` | Secretaria duplicada | 301 por mapeamento | Não | Sim, padrão |
| `/noticias25.php?tipo={id}` | `/noticias?secretaria={slug}` | Lista de notícias | 301 | Não | Sim, padrão |
| `/noticias.php?page={n}&q=&tipo=all` | `/noticias?page={n}` | Lista de notícias | 301 | Não | Sim, padrão |
| `/exibe23.php?id={id}` | `/noticias/{slug}` | Notícia | 301 por conteúdo | Não | Sim, padrão |
| `/servicos-2.php` | `/servicos` | Serviços | 301 | Não | Sim, origem |
| `/servicos/servidor` | `/servicos/servidor` | Serviço | Preservar | Não | Sim, origem |
| `/servicos/educacao` | `/servicos?area=educacao` | Serviço | 301 | Não | Sim, origem |
| `/servicos/saude` | `/servicos?area=saude` | Serviço | 301 | Não | Sim, origem |
| `/servicos/lgpd` | `/privacidade` | LGPD | 301 | Não | Sim, origem |
| `/servicos/licenciamento-ambiental` | `/servicos/licenciamento-ambiental` | Serviço | Preservar | Não | Sim, origem |
| `/carta-servicos` | `/servicos` | Carta de Serviços | 301 | Não | Sim, origem |
| `/carta-servicos-detalhes.php?tipo={id}` | `/servicos?area={slug}` | Categoria de serviço | 301 | Não | Sim, padrão |
| `/dados-abertos` | `/dados-abertos` | Catálogo | Preservar | Não | Sim, origem |
| `/dados-abertos-detalhes.php?tipo={id}` | `/dados-abertos/{slug}` | Dataset/categoria | 301 | Não | Sim, padrão |
| `/e-sic/` | `/acesso-a-informacao` | e-SIC Hub | 301 após integração | Não | Sim, origem |
| `/e-sic/estatisticas.php` | `/acesso-a-informacao/estatisticas` | e-SIC | 301 | Não | Sim, origem |
| `/e-sic/perguntas-respostas.php` | `/acesso-a-informacao/perguntas` | e-SIC | 301 | Não | Sim, origem |
| `/e-sic/cadastro.php` | `/acesso-a-informacao` | e-SIC quebrado | 301 imediato | N/A | Sim, 404 |
| `/e-sic/contato.php` | `/acesso-a-informacao` | Contato quebrado | 301 imediato | Não | Sim, 404 |
| `/e-sic/diario.php?tipo=1` | `/diario-oficial` | Diário quebrado | 301 imediato | N/A | Sim, 404 |
| `/e-sic/legislacao.php?tipo={id}` | `/legislacao?tipo={slug}` | Legislação | 301 | Não | Sim, padrão |
| `/e-sic/prestacao_contas.php?tipo=9` | `/transparencia/orcamento/ppa` | PPA | 301 | Não | Sim, origem |
| `/e-sic/prestacao_contas.php?tipo=10` | `/transparencia/orcamento/ldo` | LDO | 301 | Não | Sim, origem |
| `/e-sic/prestacao_contas.php?tipo=11` | `/transparencia/orcamento/loa` | LOA | 301 | Não | Sim, origem |
| `/e-sic/avisos-licitacoes.php?tipo=4` | `/licitacoes?fase=aviso` | Licitação | 301 | Não | Sim, origem |
| `/e-sic/editais_licitacoes.php?tipo=1` | `/licitacoes?fase=edital` | Licitação | 301 | Não | Sim, origem |
| `/e-sic/resultados_licitacoes.php?tipo=2` | `/licitacoes?fase=resultado` | Licitação | 301 | Não | Sim, origem |
| `/e-sic/contratos.php?tipo=3` | `/contratos` | Contrato | 301 | Não | Sim, origem |
| `/e-sic/calendario.php` | `/licitacoes/calendario` | Calendário | 301 | Não | Sim, origem |
| `/licitacoes/` | `/licitacoes` | Hub | 301/canonical | Não | Sim, origem |
| `/licitacoes/contratos.php?tipo=3` | `/contratos` | Link quebrado | 301 imediato | N/A | Sim, 404 |
| `/cadastro-fornecedor/` | `/licitacoes/fornecedores` | Fornecedor | 301 | Não | Sim, origem |
| `/GEO-OBRAS/` | `/obras` | Obras | 301/hub | Não | Sim, origem |
| `/pages/central-conselhos/` | `/conselhos` | Conselhos | 301 | Não | Sim, origem |
| `/pages/nova-lei-licitacoes/` | `/licitacoes/nova-lei` | Página | 301 | Não | Sim, origem |
| `/pages/vpn/` | `/servicos/vtn-itr` | VTN/ITR | 301 | Não | Sim, origem |
| `/pagina/97_Telefones-Uteis.html` | `/contatos` | Telefones úteis | 301 | Não | Sim, 404 |
| `/e-sic/uploads/{path}` | `/documentos/{id}` | Arquivo | 301 por checksum/mapeamento | Não | Sim, padrão |
