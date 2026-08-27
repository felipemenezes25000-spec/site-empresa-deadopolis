# Matriz final de requisitos

Atualizada em 27/08/2026. Estados permitidos: `DONE`, `EXTERNAL_DEPENDENCY` e `NOT_APPLICABLE`. `DONE` significa código e evidência automatizada; não significa que conteúdo/provider externo esteja contratado.

| ID | Requisito | Estado | Evidência | Fronteira externa |
|---|---|---|---|---|
| AUD-001 | Inventário integral do legado | DONE | `LEGACY_MIGRATION_REPORT.md`, JSON de evidência, fila 0, sem truncamento | repetir imediatamente antes do cutover |
| MIG-001 | URLs e redirects 301 seguros | EXTERNAL_DEPENDENCY | middleware, regras, resolução, mapa e E2E | ativação depende de destino oficial publicado |
| MIG-002 | Crawler seguro e retomável | DONE | testes de SSRF, normalização, paginação, extração e importação | 16 arquivos acima do limite permanecem bloqueados com motivo |
| MIG-003 | Ingestão documental governada | EXTERNAL_DEPENDENCY | magic bytes, SHA-256, deduplicação, quarentena e acervo | storage/scanner reais e aprovação |
| MIG-004 | Operação administrativa em escala | DONE | paginação, filtros, CSV seguro, lote limitado e isolamento de falhas | fila distribuída depende da infraestrutura escolhida |
| CORE-001 | Multi-tenant sem mistura | DONE | filtros EF e testes de persistência/contexto | host/header oficial na infraestrutura |
| IAM-001 | Login, MFA, RBAC e capabilities | DONE | testes de identidade e contratos 401/403 | secret manager e política de usuários finais |
| AUD-002 | Auditoria e correlation ID | DONE | AuditEvent, middlewares e fluxos contratuais | retenção institucional |
| CMS-001 | Workflow editorial e CMS governado | DONE | revisões, versionamento, agenda, publicação e E2E | conteúdo oficial/revisores |
| MED-001 | Mídia segura | EXTERNAL_DEPENDENCY | validação, metadata, quarentena e providers explícitos | storage e antivírus reais |
| PUB-001 | Portal responsivo e acessível | DONE | build Next, axe, crawl e rotas críticas | auditoria humana assistiva recomendada |
| SRV-001 | Carta de Serviços | DONE | domínio, CRUD e páginas públicas | catálogo oficial item a item |
| SRC-001 | Busca global | DONE | consulta limitada no banco para serviços, notícias, secretarias, páginas, datasets e documentos | observar ranking com dados reais |
| TRA-001 | Dados Abertos/Transparência | DONE | datasets versionados e categorias públicas | sistemas financeiros permanecem fontes declaradas |
| TRA-002 | Acervo de licitações/contas/legislação | DONE | arquivo pesquisável, paginação, filtros, origem, hash e download | ingestão/publicação real depende de MIG-003 |
| GAZ-001 | Diário Oficial verificável | EXTERNAL_DEPENDENCY | composição, PDF, hash, QR, código, imutabilidade e E2E | ICP-Brasil e timestamp reais |
| SUP-001 | Ouvidoria, tickets e SLA | DONE | domínio, endpoints, UI e E2E | notificação externa por e-mail |
| MAIL-001 | Governança de e-mail | EXTERNAL_DEPENDENCY | domínio, mailbox, alias, migração e DEMO_ONLY explícito | provider, DNS e credenciais reais |
| OPS-001 | Health, link check e backup evidence | EXTERNAL_DEPENDENCY | health, worker SSRF-safe, tentativa manual e registro auditável | orquestração de backup/restore/monitoramento |
| SEC-001 | Hardening e supply chain | DONE | headers, cookies, rate limit, SSRF, scans e testes negativos | WAF, rotação, pentest e SIEM |
| A11Y-001 | WCAG 2.2 AA automatizada | DONE | axe serious/critical, teclado e E2E | aceite humano com tecnologia assistiva |
| POC-001 | Demo reproduzível e reset seguro | DONE | E2E, seed DEMONSTRAÇÃO e reset que recusa Production | nenhuma |
| DOC-001 | Runbooks e relatório operacional | DONE | verificador documental na CI e arquivos obrigatórios | políticas/valores institucionais finais |
