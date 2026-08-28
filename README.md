# Portal Municipal Digital de Deodápolis

Plataforma municipal multi-tenant construída com ASP.NET Core, PostgreSQL, Next.js e Docker. O repositório contém portal público, área administrativa, CMS, transparência, acervo documental, migração do legado, Diário Oficial verificável, Ouvidoria, Dados Abertos e ferramentas operacionais.

O estado atual é **pronto para POC**. A entrada em produção depende de conteúdo oficial, infraestrutura e providers externos listados em [`docs/EXTERNAL_DEPENDENCIES.md`](docs/EXTERNAL_DEPENDENCIES.md). Estados `DEMO_ONLY` e `NOT_CONFIGURED` nunca representam serviço oficial ativo.

## Execução local

Pré-requisitos: Docker Desktop com Compose v2, Git e portas locais `3000`, `5080` e `54329` disponíveis.

1. Copie `.env.example` para `.env`.
2. Preencha `POSTGRES_PASSWORD` e `DEMO_PASSWORD` com valores longos e exclusivos. Não versione `.env`.
3. Inicie a stack:

```bash
docker compose up -d --build
```

4. Aguarde os health checks:

```bash
curl --fail http://127.0.0.1:5080/health/live
curl --fail http://127.0.0.1:5080/health/ready
```

Portal: `http://127.0.0.1:3000`

Admin: `http://127.0.0.1:3000/admin/login`

O usuário administrativo da demonstração é `admin.demo`; a senha é exatamente o valor local de `DEMO_PASSWORD`.

## Reset seguro da demonstração

O reset remove somente os volumes nomeados da stack Compose deste projeto. Ele exige autorização explícita e recusa `Production`:

```bash
DEMO_RESET_ALLOWED=true make demo-reset
```

Consulte [`docs/POC_RUNBOOK.md`](docs/POC_RUNBOOK.md) antes de uma apresentação.

## Verificação

```bash
dotnet restore MunicipalPlatform.sln
dotnet build MunicipalPlatform.sln -c Release --no-restore
dotnet test MunicipalPlatform.sln -c Release --no-build
npm --prefix apps/web ci
npm --prefix apps/web run lint
npm --prefix apps/web run typecheck
npm --prefix apps/web run test
npm --prefix apps/web run build
bash scripts/tests/demo-reset.test.sh
bash scripts/tests/verify-docs.test.sh
bash scripts/verify-docs.sh
```

Com a stack em execução, o drill de backup e restauração isolada também roda localmente:

```bash
bash scripts/db-restore-verify.sh "$(bash scripts/db-backup.sh)"
```

Os cenários E2E exigem a stack no ar e a mesma `DEMO_PASSWORD` usada pelo Compose:

```bash
npm --prefix apps/web run test:e2e
```

A CI também valida migrations em banco limpo, idempotência, imagens Docker, vulnerabilidades críticas corrigíveis, segredos e 48 cenários E2E (POC executiva, Ouvidoria, mídia, redirects, busca, compliance, acessibilidade, responsividade, 404 e crawl interno).

## Documentação

- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md): arquitetura e limites de domínio.
- [`docs/REQUIREMENTS_MATRIX.md`](docs/REQUIREMENTS_MATRIX.md): estado verificável dos requisitos.
- [`docs/URL_MIGRATION_MAP.md`](docs/URL_MIGRATION_MAP.md): continuidade das URLs históricas.
- [`docs/LEGACY_MIGRATION_REPORT.md`](docs/LEGACY_MIGRATION_REPORT.md): inventário completo do portal legado.
- [`docs/PRODUCTION_RUNBOOK.md`](docs/PRODUCTION_RUNBOOK.md): instalação, atualização e rollback.
- [`docs/BACKUP_RESTORE_RUNBOOK.md`](docs/BACKUP_RESTORE_RUNBOOK.md): backup, restore e evidência.
- [`docs/SECURITY.md`](docs/SECURITY.md): controles e responsabilidades de segurança.
- [`docs/FINAL_REPORT.md`](docs/FINAL_REPORT.md): evidências consolidadas e fronteira de produção.

## Estrutura

- `apps/api`: API ASP.NET Core e migrations EF Core.
- `apps/web`: portal e administração Next.js.
- `tests/api`: testes de domínio e contratos HTTP.
- `apps/web/tests/e2e`: Playwright, axe e crawl interno.
- `scripts`: verificações locais e automação segura da POC.
- `docs/evidence`: evidências versionadas da migração pública.
