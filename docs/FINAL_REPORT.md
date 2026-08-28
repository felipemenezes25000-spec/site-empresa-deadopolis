# Relatório final de engenharia

Atualizado em 28/08/2026. Fonte de verdade: código do `main`, evidência versionada e workflows do GitHub Actions.

## Estado

- **Portal/administrativo:** pronto para POC reproduzível.
- **Código controlável:** implementado e coberto pelos gates automatizados descritos abaixo.
- **Produção:** pronta para preparação, condicionada exclusivamente às dependências externas documentadas.

## Capacidades entregues

- multi-tenancy, RBAC/capabilities, MFA, auditoria e correlation ID;
- portal público, serviços, secretarias, CMS, notícias e busca global;
- transparência, Dados Abertos e acervo documental governado;
- licitações, legislação e famílias históricas de prestação de contas;
- Diário Oficial determinístico, hash, QR/código e verificação pública;
- Ouvidoria/tickets/SLA, e-mail governado e estados honestos de provider;
- crawler SSRF-safe, importação, evidências e redirects auditáveis;
- link health, backup evidence, compliance e Presentation Mode;
- acompanhamento público de manifestação da Ouvidoria por protocolo e código;
- enquadramento editorial de mídia com prévia visual do recorte e do ponto focal.

## Migração do legado

| Métrica final | Valor |
|---|---:|
| URLs únicas | 14.373 |
| Documentos | 8.155 |
| PDFs | 7.356 |
| Office | 799 |
| Imagens | 3.372 |
| Documentos de licitações | 5.336 |
| Prestação de contas | 610 |
| Informativos | 1.404 |
| Fila restante | 0 |
| Truncado por limite | não |

O inventário não equivale a publicação. Conteúdo e documentos permanecem sujeitos a storage, scanner e aprovação. Metodologia, divergências e falhas estão em `LEGACY_MIGRATION_REPORT.md`.

## Verificação automatizada

- backend: build com zero warnings, 180 testes de domínio/contrato, migrations, banco vazio, idempotência, script e snapshot EF;
- frontend: lint, TypeScript strict, 61 testes Vitest e build de produção;
- E2E: 48 cenários Playwright aprovados em execução serial equivalente à CI, cobrindo a POC executiva, Ouvidoria ponta a ponta, mídia governada, redirects, busca acentuada, compliance, acessibilidade pública e administrativa, responsividade em quatro pontos de quebra, status 404 e crawl interno;
- segurança: secret scan, rejeição de chaves/certificados, audit de dependências e Trivy;
- containers: publish da API, build API/Web não-root, PostgreSQL 17 limpo e validação do Compose com credenciais efêmeras;
- documentação/reset: 23 arquivos obrigatórios verificados e reset que recusa Production e ausência de opt-in.

A validação local de 28/08/2026 comprovou os números acima contra a stack Compose real: 8 migrations aplicadas, 40 tabelas públicas restauradas em contêiner isolado e busca executada no PostgreSQL. Audit npm e Trivy permanecem gates do runner Linux porque o host Windows local não dispõe de Trivy e sua conexão npm usa uma cadeia CA não reconhecida pelo Node.

## Correções desta rodada

Cada item abaixo foi reproduzido no runtime empacotado antes da correção e coberto por teste automatizado depois dela.

| Falha encontrada | Efeito real | Cobertura permanente |
|---|---|---|
| `.NET` inicia em modo globalization-invariant na imagem Alpine | busca acentuada retornava zero e `/buscar` respondia 500 no contêiner | dobra de acentos independente de ICU, contrato de busca e solução inteira compilada/testada em modo invariante |
| `loading.tsx` na raiz abria streaming antes de resolver o recurso | todo `notFound()` era confirmado como HTTP 200 | cenário que exige 404 real para recurso inexistente |
| destino de redirect exigia apenas `/` inicial | `//host` saía do domínio municipal (open redirect) | recusa no domínio, no middleware, no resolvedor público e na importação do legado |
| proxy repassava cabeçalhos hop-by-hop e não tinha limite de espera | resposta podia travar no navegador sem retorno | proxy com limite de 20s que responde 504, cliente com limite próprio e testes de unidade |
| estado de integração serializado como enum | painel exibia o literal `3` | vocabulário único verificado em três superfícies |
| `StatusBadge` só tratava `NOT_`/`FAILED`/`UNAVAILABLE` como risco | `DEMO_ONLY`, `DEGRADED` e `QUARANTINED` apareciam em verde | mapa de severidade explícito e cenário de compliance |
| cartões de conta não quebravam linha | `/admin/usuarios` rolava lateralmente em 375px | cenário responsivo em 375, 768, 1024 e 1440 pixels |
| Ouvidoria não expunha acompanhamento ao cidadão | protocolo e código eram emitidos sem tela de consulta | fluxo completo cidadão/servidor em E2E e contrato |

O baseline anterior a este fechamento passou no [workflow CI 33049689836](https://github.com/felipemenezes25000-spec/site-empresa-deadopolis/actions/runs/33049689836) e no [schema preview 33049689905](https://github.com/felipemenezes25000-spec/site-empresa-deadopolis/actions/runs/33049689905). Cada commit posterior precisa repetir os mesmos gates; um relatório não substitui o status do SHA implantado.

## Dependências externas restantes

Storage/scanner, ICP-Brasil/timestamp, e-mail/DNS, integrações oficiais, conteúdo institucional, backup/monitoramento, TLS/reverse proxy, pentest, LGPD e aceite de acessibilidade humana. A lista e os critérios de aceite estão em `EXTERNAL_DEPENDENCIES.md`.

## Status factual

**READY FOR POC. READY FOR PRODUCTION PREPARATION WITH EXTERNAL DEPENDENCIES.**
