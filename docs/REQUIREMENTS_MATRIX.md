# Matriz de requisitos verificada

Atualizada em 26/08/2026. `VALIDATED` significa que há implementação e teste automatizado no repositório; não significa que um provedor externo esteja contratado ou que o cutover de produção tenha sido autorizado. `EXTERNAL_GATE` identifica exatamente essa fronteira.

| ID | Requisito | Módulo | Status | Verificação/evidência | Dependência ou ressalva real |
|---|---|---|---|---|---|
| AUD-001 | Inventariar o portal legado inteiro | Migration | VALIDATED | `docs/LEGACY_MIGRATION_REPORT.md`; `docs/evidence/legacy-inventory-final-2026-08-26.json`; dry-run schema 5; fila vazia | O portal legado é mutável; repetir antes do cutover |
| MIG-001 | Preservar URLs e 301 seguros | Migration | IMPLEMENTING | `LegacyRedirectMiddleware`, `RedirectRule`, testes de normalização/importação, `docs/URL_MIGRATION_MAP.md` | Redirect item a item só pode ser ativado depois da publicação do destino |
| MIG-002 | Crawler seguro, retomável e reconciliável | Migration | VALIDATED | `LegacyCrawlerServiceTests`, `ExternalUrlSafetyTests`, `LegacyTraversalPolicyTests` | 16 arquivos acima de 25 MB permanecem bloqueados com motivo |
| MIG-003 | Ingestão documental governada | Migration/Media | VALIDATED | `LegacyDocumentImportServiceTests`, `PublicDocumentContractTests`, migration `AddPublicDocumentArchive` | Publicação real exige storage e malware scanner configurados e aprovação administrativa |
| MIG-004 | Operação administrativa em escala | Migration/Web | VALIDATED | paginação/filtros, isolamento de falhas, CSV completo protegido contra fórmulas e lote sequencial limitado a 10 rascunhos | O lote para quando o navegador fecha; uma fila distribuída só é necessária após definir infraestrutura operacional |
| CORE-001 | Multi-tenant sem mistura | Platform | VALIDATED | `TenantPersistenceTests`, `TenantContextTests`, filtros globais EF | Cabeçalho/domínio de tenant deve ser configurado na infraestrutura de produção |
| IAM-001 | RBAC e capabilities no backend | Identity | VALIDATED | `UserAccountSecurityTests`, autenticação e policies dos endpoints admin | Segredos/MFA devem ser configurados no ambiente final |
| AUD-002 | Trilha de auditoria e correlation ID | Operations | VALIDATED | `AuditEvent`, middlewares de observabilidade e fluxos contratuais | Retenção/exportação dependem da política operacional final |
| CMS-001 | Workflow editorial governado | Content | VALIDATED | `EditorialWorkflowTests`, `PortalResourceTests`, revisões e publicação agendada | Conteúdo histórico ainda precisa de revisão antes de publicar |
| MED-001 | Upload, magic bytes, hash, quarentena e malware scan | Media | EXTERNAL_GATE | `DocumentFileInspector`, endpoints de mídia, testes de importação documental | Object storage e scanner reais precisam de credenciais; estado permanece explícito quando ausentes |
| PUB-001 | Portal público responsivo e acessível | Portal/Web | VALIDATED | build Next.js, E2E, axe, crawl interno e rotas públicas | Conteúdo atual depende do CMS e das integrações configuradas |
| SRV-001 | Carta de Serviços estruturada | Services | VALIDATED | `ServiceItem`, endpoints, `/servicos` e `/servicos/[slug]` | Catálogo legado ainda requer revisão editorial item a item |
| SRC-001 | Busca universal | Search | VALIDATED | `SearchNormalizerTests`, endpoint e página `/buscar` | Ranking de produção deve ser observado com dados reais |
| TRA-001 | Dados Abertos e Transparência | Transparency | VALIDATED | testes de datasets, `/dados-abertos`, categorias conhecidas com 404 rígido | Sistemas financeiros externos continuam como fontes declaradas |
| TRA-002 | Acervo de licitações e prestação de contas | Transparency/Migration | VALIDATED | `/licitacoes`, `/transparencia/[slug]`, filtros/paginação e testes do arquivo documental | Registros só aparecem depois de importar, aprovar o asset e publicar o documento |
| GAZ-001 | Diário Oficial, PDF, hash, QR e acervo | Gazette | EXTERNAL_GATE | `GazetteEditionTests`, `GazetteDocumentServiceTests`, E2E | Assinatura oficial ICP-Brasil não é simulada; provider real precisa ser contratado/configurado |
| SUP-001 | Tickets, comentários e SLA | Support | VALIDATED | `TicketFlowTests`, `SlaPolicyTests`, E2E | Notificações externas dependem do provedor de e-mail |
| MAIL-001 | Gestão e migração de e-mail | Mail | EXTERNAL_GATE | domínio, mailbox, alias e jobs auditáveis no POC/E2E | SMTP/IMAP/provider de produção não está configurado |
| OPS-001 | Health, link check e evidência de backup | Operations | EXTERNAL_GATE | `HealthContractTests`, `LinkCheckTests`, endpoints operacionais | Restore real, monitoramento e backup dependem da infraestrutura final |
| SEC-001 | Hardening OWASP | Cross-cutting | VALIDATED | SSRF/DNS/redirect tests, headers, rate limits e jobs de security scan na CI | WAF, rotação de segredos e pentest pertencem ao ambiente de produção |
| A11Y-001 | WCAG 2.2 AA | Web | VALIDATED | suíte Playwright/axe e navegação por teclado na CI | Auditoria humana com tecnologia assistiva continua recomendada |
| POC-001 | Demo reproduzível | Demo | VALIDATED | `apps/web/tests/e2e/poc.spec.ts` e job E2E da CI | Dados de demonstração permanecem marcados como `DEMO_ONLY` |
