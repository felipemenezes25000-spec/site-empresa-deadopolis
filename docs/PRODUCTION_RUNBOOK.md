# Runbook de produção

Este runbook cobre instalação, upgrade e rollback. Ele não autoriza go-live sem as dependências externas e aprovações descritas em `EXTERNAL_DEPENDENCIES.md`.

## Configuração obrigatória

API:

- `ASPNETCORE_ENVIRONMENT=Production`
- `ConnectionStrings__Database`: PostgreSQL com TLS e usuário de menor privilégio.
- `DefaultMunicipalitySlug`: slug oficial quando o host não o determinar.
- `PresentationMode=false`
- `DataProtection__KeyRingPath`: volume persistente, privado e incluído no restore.
- `PublicPortalBaseUrl`: URL HTTPS pública.

Web:

- `NODE_ENV=production`
- `API_URL`: endereço interno da API.
- `MUNICIPALITY_SLUG`: tenant servido pelo frontend.
- `PRESENTATION_MODE=false`
- `PUBLIC_PORTAL_URL`: URL HTTPS pública.

`Demo__Password` não deve existir em produção. Providers externos continuam `NOT_CONFIGURED` até a configuração homologada; consulte o painel `/admin/integracoes`.

## Instalação nova

1. Registre change request, commit/tag, imagens por digest e plano de rollback.
2. Provisione PostgreSQL privado, storage, key ring, secret manager, logs e monitoramento.
3. Garanta que o usuário de migration tenha permissão `CREATE` no banco para instalar a extensão confiável `unaccent`, ou solicite ao DBA que execute previamente `CREATE EXTENSION IF NOT EXISTS unaccent`. O usuário cotidiano da aplicação pode permanecer com privilégio menor.
4. Faça backup inicial/configuração de retenção.
5. Valide as imagens da mesma revisão aprovada pela CI.
6. Execute migrations uma única vez pelo job de implantação:

```bash
dotnet ef database update --project apps/api --startup-project apps/api --configuration Release
```

7. Inicie API; aguarde `/health/live` e `/health/ready`.
8. Inicie Web atrás do proxy HTTPS.
9. Valide headers, login/MFA, tenant, busca com e sem acentos, conteúdo público, downloads, auditoria e estados de providers.
10. Só então altere DNS/tráfego.

Não use `compose.yaml` da POC como manifesto de produção: ele define `Development`, Presentation Mode e storage temporário.

## Upgrade

1. Confirme CI verde no SHA exato e leia migrations/changelog.
2. Gere backup e valide o artefato antes da janela.
3. Teste migration e aplicação em cópia sanitizada.
4. Publique imagens imutáveis, mantenha a versão anterior disponível.
5. Execute migrations antes de aumentar tráfego quando forem retrocompatíveis; mudanças destrutivas exigem implantação em fases.
6. Faça smoke tests: health, home, busca, admin, publicação, download e auditoria.
7. Observe erros, latência e conexões do banco durante a janela definida.

## Rollback

- Erro somente na aplicação e schema compatível: volte as imagens ao digest anterior.
- Migration incompatível: interrompa escrita, aplique o procedimento reverso previamente testado ou restaure o ponto aprovado. Não use `git reset` nem edite tabela manualmente em produção.
- Conteúdo/redirect incorreto: desative a regra ou restaure a revisão pelo fluxo administrativo e registre auditoria.
- Provider externo degradado: mantenha `DEGRADED/NOT_CONFIGURED`, preserve o portal de leitura e não substitua por provider demo.

Após rollback, valide health, integridade dos dados, fila/outbox e downloads; registre incidente e decisão.

## Operação contínua

- sondar `/health/live` para processo e `/health/ready` para dependências essenciais;
- centralizar logs por correlation ID sem cookies/tokens/corpos sensíveis;
- monitorar falhas de links e providers no painel administrativo;
- executar backup conforme política e restore drill periódico;
- rotacionar credenciais e revisar perfis/capabilities;
- repetir inventário do legado imediatamente antes do cutover;
- nunca ativar 301 antes de o destino oficial existir e responder sem 404.
