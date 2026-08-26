# Matriz inicial de requisitos

| ID | Origem | Requisito | Módulo | Status | Teste | Evidência | Dependência externa | Observação |
|---|---|---|---|---|---|---|---|---|
| AUD-001 | Missão §1 | Auditar portal além da home | Migration | LOCALLY_VALIDATED | Crawl controlado | `docs/CURRENT_PORTAL_AUDIT.md` | Não | 180 páginas/1.238 URLs descobertas |
| MIG-001 | Missão §3 | Preservar URLs e 301 | Migration | IMPLEMENTING | Redirect integration | `docs/URL_MIGRATION_MAP.md` | Não | Mapa inicial criado; middleware pendente |
| CORE-001 | Missão §9 | Multi-tenant sem mistura | Platform | NOT_STARTED | Tenant isolation integration | — | Não | Obrigatório antes dos módulos |
| IAM-001 | Missão §37 | RBAC + capabilities backend | Identity | NOT_STARTED | Authorization negative | — | Não | UI nunca é fronteira de segurança |
| AUD-002 | Missão §38 | Trilha de auditoria | Audit | NOT_STARTED | Audit integration | — | Não | Diff semântico e correlation ID |
| CMS-001 | Missão §12 | Workflow editorial completo | Content | NOT_STARTED | News workflow unit/E2E | — | Não | Estados e transições explícitos |
| MED-001 | Missão §14/53 | Biblioteca e upload seguro | Media | NOT_STARTED | Magic byte/upload integration | — | Antivírus/S3 em produção | Provider sem credencial fica NOT_CONFIGURED |
| PUB-001 | Missão §5 | Home portal de serviços | Portal | NOT_STARTED | Portal E2E/axe | — | Não | Busca é ação principal |
| SRV-001 | Missão §19 | Carta estruturada | Services | NOT_STARTED | Service CRUD/search | — | Não | Não será lista de PDFs |
| SRC-001 | Missão §17 | Busca universal | Search | NOT_STARTED | Ranking/acento/typo | — | Não | PostgreSQL inicialmente |
| TRA-001 | Missão §22/23 | Dados Abertos e Transparência | Transparency | NOT_STARTED | Dataset/link integration | — | Sistemas financeiros | Linkar sem substituir indevidamente |
| GAZ-001 | Missão §27-32 | Diário, PDF, hash, QR e acervo | Gazette | NOT_STARTED | Gazette unit/integration/E2E | — | ICP-Brasil | Assinatura ficará NOT_CONFIGURED sem credencial |
| SUP-001 | Missão §35/36 | Tickets e SLA | Support | NOT_STARTED | Ticket/SLA E2E | — | Não | Comentário interno separado |
| MAIL-001 | Missão §33/34 | Gestão e migração de email | Mail | NOT_STARTED | Provider contract | — | SMTP/IMAP/provider | Produção NOT_CONFIGURED |
| OPS-001 | Missão §43/55-57 | Link health, health e backups | Operations | NOT_STARTED | Worker/health integration | — | Infraestrutura | Evidência, não certificação inventada |
| SEC-001 | Missão §52 | Hardening OWASP | Cross-cutting | NOT_STARTED | Abuse cases/scans | — | WAF em produção | Headers, rate limit e validação |
| A11Y-001 | Missão §46 | WCAG 2.2 AA | Web | NOT_STARTED | axe/keyboard | — | VLibras opcional | Widget não substitui acessibilidade |
| POC-001 | Missão §69-72 | Demo reproduzível | Demo | NOT_STARTED | `poc.spec.ts` | — | Não | Dados sintéticos marcados |
