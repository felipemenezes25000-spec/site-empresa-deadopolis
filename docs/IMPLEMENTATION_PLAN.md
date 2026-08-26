# Plataforma Municipal Digital Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Entregar uma POC municipal real, multi-tenant e demonstrável, preservando o patrimônio digital de Deodápolis e mantendo integrações externas honestamente configuráveis.

**Architecture:** Monólito modular com Next.js e ASP.NET Core sobre PostgreSQL. Domínios possuem limites claros, mutações auditadas e eventos confiáveis via outbox; providers externos ficam `NOT_CONFIGURED` sem credenciais.

**Tech Stack:** Next.js, React, TypeScript strict, Tailwind, shadcn/Radix, ASP.NET Core, EF Core, PostgreSQL, storage S3-compatible, Playwright, xUnit e Docker.

**Spec:** `docs/ARCHITECTURE.md`

## Global Constraints

- Tenant isolation obrigatório por `municipality_id`; falhar fechado quando tenant não estiver resolvido.
- Nenhuma integração externa pode responder como operacional sem credencial/health real.
- Produção não contém seed de senha, segredo, PFX, SMTP ou chave cloud.
- Conteúdo oficial publicado é versionado; Diário publicado é imutável.
- WCAG 2.2 AA é o alvo e o portal precisa funcionar a partir de 320px.
- Implementação comportamental segue RED -> GREEN -> REFACTOR.
- Cada wave termina com build e testes relevantes verdes antes da próxima.

---

### Task 1: Auditoria e decisões

**Files:** `docs/CURRENT_PORTAL_AUDIT.md`, `docs/ARCHITECTURE.md`, `docs/URL_MIGRATION_MAP.md`, `docs/REQUIREMENTS_MATRIX.md`

**Interfaces:** produz o inventário e os contratos arquiteturais consumidos por todas as waves.

- [x] Auditar o repositório, confirmar ausência de código e histórico.
- [x] Fazer crawl controlado do portal público e classificar recursos, documentos, 404s e hosts externos.
- [x] Escolher o monólito modular e registrar limites, dados e providers.
- [ ] Criar mapa inicial de redirecionamentos e matriz rastreável.
- [ ] Commit: `docs: audit legacy portal and define architecture`.

### Task 2: Fundação do monorepo e contratos de domínio

**Files:** `MunicipalPlatform.sln`, `apps/api/**`, `apps/web/**`, `Directory.Build.props`, `.editorconfig`, `.env.example`

**Interfaces:** produz `MunicipalityId`, `TenantContext`, `IAuditableEntity`, API `/api/v1` e cliente web tipado.

- [ ] Verificar versões locais de Node, npm, .NET e Docker.
- [ ] Consultar segurança/licenças das dependências diretas antes de adicionar manifests.
- [ ] Criar testes falhos para resolução obrigatória de tenant e health básico.
- [ ] Criar solução .NET e Next.js strict; configurar warnings como erros no código próprio.
- [ ] Implementar o mínimo para os testes passarem e adicionar Dockerfiles/Compose PostgreSQL.
- [ ] Executar restore, lint, typecheck, unit e build.
- [ ] Commit: `feat(core): bootstrap municipal platform`.

### Task 3: Identidade, RBAC e auditoria

**Files:** `apps/api/Modules/Identity/**`, `apps/api/Modules/Audit/**`, `apps/web/src/app/admin/**`

**Interfaces:** produz `POST /api/v1/auth/login`, `POST /auth/refresh`, `POST /auth/logout`, policies de capability e `AuditEvent`.

- [ ] Testar primeiro login válido/inválido, sessão revogada, isolamento de tenant e três acessos negativos 403.
- [ ] Implementar usuários, papéis, capabilities, cookies seguros, sessão e seeds somente Demo.
- [ ] Implementar middleware/policy que cruza tenant + capability.
- [ ] Persistir diff semântico e correlation ID para toda mutação.
- [ ] Criar tela real de login, loading/error e shell admin responsivo.
- [ ] Executar unit/integration e commit `feat(auth): implement tenant-scoped authorization`.

### Task 4: CMS, páginas, notícias, home, menus e mídia

**Files:** `apps/api/Modules/Content/**`, `apps/api/Modules/Media/**`, `apps/web/src/app/(portal)/**`, `apps/web/src/app/admin/comunicacao/**`

**Interfaces:** produz CRUD versionado, máquina editorial, uploads quarantine-aware e layout público publicado.

- [ ] Testar transições editoriais, autosave com concorrência, aprovação e scheduler/outbox.
- [ ] Implementar entidades/migrations/DTOs/endpoints com audit e soft-delete.
- [ ] Testar upload por magic bytes, tamanho, checksum e ALT obrigatório na publicação.
- [ ] Implementar provider filesystem dev/S3 contract, media library e variantes.
- [ ] Implementar editor de notícia, preview responsivo, inbox, calendário e quick actions.
- [ ] Implementar block builder controlado e editor de menus com preview/versionamento/rollback.
- [ ] Implementar home pública, páginas, notícias, eventos e alertas sem controles inertes.
- [ ] Rodar unit/integration/frontend e commit `feat(cms): add editorial workflow and portal publishing`.

### Task 5: Serviços, secretarias, busca e diretórios

**Files:** `apps/api/Modules/Services/**`, `apps/api/Modules/Search/**`, `apps/web/src/app/(portal)/servicos/**`

**Interfaces:** produz catálogo estruturado, diretórios e `GET /api/v1/search`.

- [ ] Testar filtros de serviço, páginas automáticas de secretaria e organograma.
- [ ] Testar normalização de acento/erro pequeno, ranking e registro anônimo de zero resultado.
- [ ] Implementar entidades, migrations, CRUD autorizado e páginas públicas.
- [ ] Implementar PostgreSQL FTS provider e contrato futuro OpenSearch.
- [ ] Criar autocomplete, filtros URL-shareable, empty/error/loading e contatos/locais.
- [ ] Rodar testes e commit `feat(portal): implement service finder and universal search`.

### Task 6: Transparência, Carta, Dados Abertos e hubs

**Files:** `apps/api/Modules/Transparency/**`, `apps/web/src/app/(portal)/(transparency)/**`

**Interfaces:** produz datasets versionados, links monitorados e hubs e-SIC/Ouvidoria/Licitações.

- [ ] Testar dataset/versionamento, exportação, links externos e aviso de saída.
- [ ] Implementar Carta de Serviços estruturada, catálogo e exportação acessível.
- [ ] Implementar Dados Abertos com versões/formatos/licença/metadados.
- [ ] Implementar hub de Transparência e entradas modernas de e-SIC, Ouvidoria e Licitações.
- [ ] Preservar fornecedores externos como links/configuração; não duplicar sem governança.
- [ ] Rodar testes e commit `feat(transparency): add structured public information hubs`.

### Task 7: Diário Oficial verificável

**Files:** `apps/api/Modules/Gazette/**`, `apps/web/src/app/admin/diario/**`, `apps/web/src/app/(portal)/diario-oficial/**`

**Interfaces:** produz compositor, PDF determinístico, SHA-256, QR, verificação pública e `IDigitalSigner`.

- [ ] Testar estados válidos/inválidos, imutabilidade, correção e idempotência.
- [ ] Testar byte snapshot/hash determinístico e verificação por código.
- [ ] Implementar edição/seção/ato/anexo e compositor ordenável.
- [ ] Implementar gerador PDF, storage, checksum, QR e página `/verificar/{codigo}`.
- [ ] Implementar contratos ICP/timestamp/validação, health e `NOT_CONFIGURED`.
- [ ] Implementar importação de edições legadas sem assinatura retroativa.
- [ ] Rodar testes e commit `feat(gazette): implement verifiable official gazette`.

### Task 8: Tickets, SLA, email e mudança contratual

**Files:** `apps/api/Modules/Support/**`, `apps/api/Modules/Mail/**`, `apps/web/src/app/admin/(support)/**`

**Interfaces:** produz protocolos, timeline, SLA, RFChange e `IInstitutionalEmailProvider`.

- [ ] Testar cálculo de SLA, transições, comentário interno e permissionamento.
- [ ] Implementar tickets, anexos, notificações/outbox, painéis e request for change.
- [ ] Implementar domínio/caixa/quota/alias/lista e provider mock somente Dev/Demo.
- [ ] Implementar wizard IMAP/MBOX/EML com dry-run, progresso e falhas; PST fica `EXTERNAL_DEPENDENCY` sem biblioteca/provider.
- [ ] Criar telas reais de operação e status.
- [ ] Rodar testes e commit `feat(support): add service desk, SLA and mail providers`.

### Task 9: Migração, redirects e link monitor

**Files:** `apps/api/Modules/Migration/**`, `apps/api/Modules/Operations/LinkHealth/**`, `tools/migration/**`

**Interfaces:** produz pipeline `DISCOVER/FETCH/PARSE/NORMALIZE/MAP/IMPORT/VERIFY`, evidências e middleware 301.

- [ ] Testar normalização UTF-8/mojibake, checksum/deduplicação e dry-run idempotente.
- [ ] Implementar entidades `LegacyUrl`, `RedirectRule`, `ImportedContent`, `MigrationJob`, `MigrationEvidence`.
- [ ] Implementar mídia/PDF com quarantine e origem preservada.
- [ ] Implementar redirects 301 e verificador de mapa.
- [ ] Implementar job de link health com timeout, redirect e broken sem derrubar o portal.
- [ ] Rodar crawl test local e commit `feat(migration): preserve legacy content and redirects`.

### Task 10: Observabilidade, backup, segurança e compliance

**Files:** `apps/api/Modules/Operations/**`, `apps/web/src/app/admin/compliance/**`, `infra/**`, `docs/SECURITY.md`, `docs/BACKUP_RESTORE.md`

**Interfaces:** produz health interno, integration status, backup evidence e cabeçalhos/limites.

- [ ] Testar headers, rate limit, upload malicioso, IDOR e health redigido.
- [ ] Implementar OpenTelemetry, logs estruturados e status operacional autenticado.
- [ ] Implementar contratos/jobs de backup e restore drill sem fabricar sucesso.
- [ ] Configurar CSP/HSTS/Permissions-Policy/Referrer-Policy e sanitização.
- [ ] Implementar Evidence Center a partir de evidência persistida, sem certificações inventadas.
- [ ] Rodar scans e commit `feat(observability): add health, evidence and platform hardening`.

### Task 11: Demo, POC, E2E e acessibilidade

**Files:** `apps/api/Infrastructure/Seed/**`, `tests/e2e/**`, `docs/POC_RUNBOOK.md`, `docs/EXECUTIVE_DEMO.md`

**Interfaces:** produz `DEMO_MODE`, reset reproduzível e roteiro automatizado.

- [ ] Criar seed sintético marcado DEMONSTRAÇÃO e senhas efêmeras/documentadas apenas localmente.
- [ ] Implementar `/demo/modernization`, banner Presentation Mode e comando `demo:reset`.
- [ ] Escrever E2E críticos e observar falha antes de completar cada fluxo faltante.
- [ ] Executar axe, teclado e responsividade 320/768/1024/1440.
- [ ] Corrigir controles inertes, 404/500, estados vazios e regressões visuais.
- [ ] Commit `test(e2e): cover municipal POC and critical flows`.

### Task 12: Infraestrutura e validação final

**Files:** `Dockerfile*`, `docker-compose.yml`, `.github/workflows/**`, `infra/terraform/**`, `docs/FINAL_REPORT.md`

**Interfaces:** produz build reproduzível, pipeline e relatório de evidência.

- [ ] Executar instalação limpa, lint, typecheck, unit e build frontend.
- [ ] Executar restore, build, unit/integration backend e migration em banco limpo.
- [ ] Executar E2E/a11y/crawl, Docker build, secret scan e dependency scan.
- [ ] Corrigir causas e repetir até verde ou dependência externa comprovada.
- [ ] Auditar todas as rotas/endpoints/permissões/providers e atualizar matriz.
- [ ] Criar documentação operacional, implantação, LGPD, acessibilidade, Diário, email, ICP e final.
- [ ] Fazer commits lógicos finais e registrar o estado exato do repositório.
