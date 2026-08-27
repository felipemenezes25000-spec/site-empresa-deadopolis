# Relatório final de engenharia

Atualizado em 27/08/2026. Fonte de verdade: código do `main`, evidência versionada e workflows do GitHub Actions.

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
- link health, backup evidence, compliance e Presentation Mode.

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

- backend: build com zero warnings, 106 testes de domínio/contrato, migrations, banco vazio, idempotência, script e snapshot EF;
- frontend: lint, TypeScript strict, 12 testes Vitest e build de produção;
- E2E: 22 cenários Playwright aprovados em execução serial equivalente à CI, incluindo POC, acessibilidade e crawl interno;
- segurança: secret scan, rejeição de chaves/certificados, audit de dependências e Trivy;
- containers: publish da API, build API/Web não-root, PostgreSQL 17 limpo e validação do Compose com credenciais efêmeras;
- documentação/reset: 14 arquivos obrigatórios verificados e reset que recusa Production e ausência de opt-in.

A validação local de 27/08/2026 comprovou os números acima, incluindo a migration `EnableAccentInsensitiveSearch` em banco vazio e a busca executada no PostgreSQL real. Audit npm e Trivy permanecem gates do runner Linux porque o host Windows local não dispõe de Trivy e sua conexão npm usa uma cadeia CA não reconhecida pelo Node.

O baseline anterior a este fechamento passou no [workflow CI 33049689836](https://github.com/felipemenezes25000-spec/site-empresa-deadopolis/actions/runs/33049689836) e no [schema preview 33049689905](https://github.com/felipemenezes25000-spec/site-empresa-deadopolis/actions/runs/33049689905). Cada commit posterior precisa repetir os mesmos gates; um relatório não substitui o status do SHA implantado.

## Dependências externas restantes

Storage/scanner, ICP-Brasil/timestamp, e-mail/DNS, integrações oficiais, conteúdo institucional, backup/monitoramento, TLS/reverse proxy, pentest, LGPD e aceite de acessibilidade humana. A lista e os critérios de aceite estão em `EXTERNAL_DEPENDENCIES.md`.

## Status factual

**READY FOR POC. READY FOR PRODUCTION PREPARATION WITH EXTERNAL DEPENDENCIES.**
