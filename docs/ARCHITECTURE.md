# Arquitetura da Plataforma Municipal Digital

**Status:** especificação aprovada pela missão executiva anexada  
**Primeira implantação:** Deodápolis/MS  
**Modelo de produto:** SaaS multi-tenant com monólito modular

## Alternativas consideradas

1. **Monólito modular Next.js + ASP.NET Core + PostgreSQL — escolhido.** Mantém implantação e transações simples, separa domínios por módulos e permite extrair serviços apenas quando houver evidência operacional.
2. **Next.js full-stack único.** Reduz componentes iniciais, mas conflita com o backend .NET exigido, concentra CMS/jobs/PDF e reduz clareza do domínio administrativo.
3. **Microserviços por módulo.** Aumenta custo, observabilidade distribuída e risco de consistência sem benefício comprovado para a primeira implantação.

## Visão de execução

```text
Navegador/PWA
  -> Next.js (portal público + admin, SSR/RSC)
    -> ASP.NET Core API (/api/v1)
      -> módulos de domínio + outbox + jobs
        -> PostgreSQL
        -> ObjectStorageProvider (filesystem dev / S3 produção)
        -> EmailProvider (mock dev / NOT_CONFIGURED produção)
        -> DigitalSignatureProvider (NOT_CONFIGURED sem credencial)
        -> LinkHealthProvider / SearchProvider
```

## Estrutura do monorepo

- `apps/web`: Next.js App Router, React, TypeScript strict e Tailwind.
- `apps/api`: ASP.NET Core API, autenticação, autorização, OpenAPI, health e workers.
- `tests/api`: testes de domínio e contratos HTTP da API.
- `apps/web/tests/e2e`: Playwright, axe, responsividade e crawl interno.
- `scripts`: verificações locais, backup/restore drill e automação segura da POC.
- `compose.yaml` e `apps/*/Dockerfile`: empacotamento da stack; ainda não há Terraform versionado, a infraestrutura permanece dependência externa.
- `docs`: arquitetura, segurança, operação, migração, POC e evidências.

## Limites modulares

| Módulo | Responsabilidade | Principais entidades |
|---|---|---|
| Platform | tenant, branding, domínio e feature flags | Municipality, MunicipalitySetting, FeatureFlag |
| Identity | usuários, papéis, capabilities, sessão e MFA-ready | User, Role, Capability, UserRole, RefreshSession |
| Content | páginas, notícias, banners, eventos, menus, versões | Page, NewsArticle, Banner, Event, MenuItem, ContentVersion |
| Services | Carta de Serviços, unidades, locais e contatos | Service, Department, Unit, Location, Contact |
| Media | upload, metadados, variantes, quarantine e checksum | MediaAsset, MediaVariant, MalwareScan |
| Search | índice de conteúdo e termos sem resultado | SearchDocument, SearchQueryEvent |
| Transparency | datasets, documentos e links externos | Dataset, DatasetVersion, TransparencyLink |
| Gazette | edições, seções, atos, PDF, hash e verificação | GazetteEdition, GazetteSection, GazetteAct, GazetteSignature |
| Support | tickets, comentários, anexos e SLA | Ticket, TicketComment, SlaPolicy, ChangeRequest |
| Mail | domínios, caixas, aliases e migrações | MailDomain, Mailbox, MailAlias, MailMigrationJob |
| Migration | descoberta, normalização, importação e redirects | LegacyUrl, ImportedContent, MigrationJob, MigrationEvidence, RedirectRule |
| Operations | auditoria, outbox, jobs, links, backup evidence | AuditEvent, OutboxMessage, LinkCheck, BackupEvidence, IntegrationStatus |

Cada tabela funcional carrega `municipality_id`; chaves únicas incluem o tenant e os serviços exigem um `MunicipalityContext`. Consultas administrativas sem tenant explícito falham fechadas. Eventos globais de plataforma ficam em schema/tabelas separadas.

## Identidade e autorização

- Login real com senha hasheada por ASP.NET Identity ou `PasswordHasher<T>`; credenciais de demonstração são aceitas apenas quando `DEMO_MODE=true`.
- Token de acesso curto em cookie `HttpOnly`, `Secure` em produção e `SameSite=Lax`; sessão renovável revogável persistida.
- Policies usam capabilities (`news.publish`, `gazette.sign`, `users.manage`) e sempre validam tenant no backend.
- Toda mutação institucional produz `AuditEvent` com ator, ação, recurso, diff semântico, correlação, IP e user agent minimizados.

## Fluxos editoriais

Estados editoriais: `DRAFT -> IN_REVIEW -> APPROVED -> SCHEDULED -> PUBLISHED -> ARCHIVED`. Transições são validadas por máquina de estados; Comunicação pode publicar diretamente quando a policy municipal permitir. Autosave usa versão de concorrência; conflitos retornam `409 ProblemDetails` com a versão atual.

Publicação agenda um `OutboxMessage` na mesma transação. O worker executa idempotentemente; reinício não perde publicação. Rollback cria nova versão, nunca apaga histórico.

## Diário Oficial

O compositor persiste seções e atos ordenados. A geração produz bytes determinísticos a partir de um snapshot imutável, calcula SHA-256, grava o objeto e cria código de verificação/QR. Após `PUBLISHED`, conteúdo e hash não mudam; correção gera `GazetteCorrection` vinculada. Assinatura ICP-Brasil é um provider real e permanece `NOT_CONFIGURED` sem PFX/serviço e secrets externos. Edições históricas nunca recebem assinatura retroativa.

## Mídia e uploads

O upload entra em `QUARANTINED`, recebe nome aleatório, limite por categoria, validação de MIME/magic bytes e SHA-256. O `MalwareScanner` pode usar um provider real; sem ele o item não é promovido em produção. Imagens aprovadas geram variantes responsivas e preservam original, ALT, crédito, legenda e ponto focal.

## Busca

`ISearchProvider` inicia com PostgreSQL full-text + `unaccent` e trigram para tolerância a acentos/pequenos erros. O índice combina serviços, páginas, notícias, secretarias, legislação, Diário e documentos. Termos sem resultado são armazenados anonimizados/agregados. OpenSearch pode substituir o provider sem alterar consumidores.

## Integrações

Toda integração expõe `status`, `lastCheckedAt`, `lastErrorCode` e health check. Estados permitidos: `CONFIGURED`, `DEGRADED`, `UNAVAILABLE`, `NOT_CONFIGURED`. Mock providers existem somente em ambiente `Development/Demo`; produção sem segredo responde `NOT_CONFIGURED`, nunca sucesso falso.

## API e erros

- REST versionado em `/api/v1`, DTOs explícitos e OpenAPI.
- Validação retorna `400` com `ProblemDetails`; conflito `409`; autenticação `401`; capability ausente `403`; ausente `404` sem vazar outro tenant.
- `X-Correlation-ID` é aceito/gerado e devolvido ao cliente; stack traces ficam fora de respostas.
- Mutação idempotente aceita `Idempotency-Key` nos fluxos de publicação, PDF, assinatura, importação e provisionamento.

## Interface

O portal é content-first, claro e leve, com verde institucional, azul petróleo e neutros quentes extraídos/ajustados a partir da marca oficial. A busca “Olá! O que você precisa?” é a ação principal; serviços são organizados por necessidade, não por organograma. O admin usa densidade média, navegação lateral agrupada, command palette e ações principais previsíveis. Não há gradientes decorativos, glassmorphism ou grids homogêneos sem hierarquia.

Componentes seguem tokens de cor/spacing/radius/foco, WCAG 2.2 AA, HTML semântico, skip link, toque mínimo de 44px, reduced motion e estados loading/empty/error. O painel continua utilizável em celular.

## Segurança, privacidade e operação

- CSP, HSTS, headers seguros, rate limiting, antiforgery/cookies, sanitização de HTML e limites de payload.
- Logs estruturados sem conteúdo sensível; retenção e exportação configuráveis por tenant.
- OpenTelemetry, health endpoints públicos mínimos e painel operacional autenticado.
- PostgreSQL privado, backup automatizado/PITR conforme infraestrutura, storage versionado, restore drill e evidência; nenhum SLA é declarado sem infraestrutura correspondente.
- Região de produção recomendada: Brasil, com Terraform parametrizado e secrets fora do repositório.

## Estratégia de testes

- Unit: máquinas de estado, capabilities, busca, SLA, hash, redirects e normalização.
- Integration: API e EF Core contra PostgreSQL real, tenant isolation, outbox e concorrência.
- E2E: autenticação; notícia; home; mídia; serviço; busca; Diário; ticket; provider; auditoria; redirect.
- A11y: axe nas rotas principais e navegação por teclado.
- Segurança: casos negativos 401/403/404, upload inválido, IDOR entre tenants e secret scan.

## Critérios verificáveis

Uma função só aparece como pronta quando UI/API/DB/permissão/validação/erros/estados/auditoria/teste/documentação aplicáveis existirem. `IMPLEMENTED`, `TESTED`, `LOCALLY_VALIDATED`, `EXTERNAL_DEPENDENCY`, `NOT_CONFIGURED` e `PRODUCTION_VALIDATED` são os únicos estados de evidência.
