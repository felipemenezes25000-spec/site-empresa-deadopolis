# Runbook da POC

Este roteiro permite preparar e demonstrar a plataforma sem depender do desenvolvedor.

## Pré-requisitos

- Docker Desktop/Engine com Compose v2;
- Git;
- 8 GB de memória disponíveis para a stack;
- portas locais `3000`, `5080` e `54329` livres.

## Preparação

1. Copie `.env.example` para `.env`.
2. Defina `POSTGRES_PASSWORD` e `DEMO_PASSWORD` com pelo menos 14 caracteres, diferentes entre si e não reutilizados.
3. Confirme que o arquivo não está versionado: `git status --short` não deve mostrar `.env`.
4. Inicie:

```bash
docker compose up -d --build
```

5. Aguarde:

```bash
curl --fail http://127.0.0.1:5080/health/live
curl --fail http://127.0.0.1:5080/health/ready
curl --fail http://127.0.0.1:3000/
```

URLs: portal `http://127.0.0.1:3000`, admin `http://127.0.0.1:3000/admin/login`, API `http://127.0.0.1:5080`.

## Credenciais e verdade da demonstração

Usuário: `admin.demo`

Senha: valor local de `DEMO_PASSWORD`

Todos os usuários `.demo` usam a senha fornecida no ambiente. Dados sintéticos aparecem como `[DEMONSTRAÇÃO]`; e-mail, scanner e assinatura podem aparecer como `DEMO_ONLY`. Nada disso possui valor oficial.

## Roteiro recomendado

1. Home, busca, serviço do IPTU e diretório de secretarias.
2. Município, governo, acesso à informação e conselhos.
3. Notícias, transparência, legislação e acervo de licitações.
4. Dados Abertos, Diário Oficial, Ouvidoria e protocolo.
5. Login administrativo e dashboard de integrações.
6. CMS: editar página e mostrar histórico/versionamento.
7. Notícia: rascunho → revisão → aprovação → publicação.
8. Dataset: criar, adicionar arquivo, publicar e abrir no portal.
9. Migração: dry-run seguro, inventário, SSRF bloqueado e redirect resolvido.
10. Operações: link check, evidência de backup, auditoria e compliance.
11. E-mail/Diário: destacar claramente `DEMO_ONLY` e a fronteira externa.
12. Ouvidoria ponta a ponta: responder a manifestação, registrar uma nota interna e consultar `/ouvidoria/acompanhar` com protocolo e código para mostrar que a nota interna não é publicada.
13. Mídia: aprovar um arquivo e ajustar ponto focal e recorte com a prévia visual, sem sobrescrever o original.

O roteiro automatizado completo está em `apps/web/tests/e2e/poc.spec.ts`; os fluxos específicos de Ouvidoria, mídia, redirects, busca, compliance, acessibilidade, responsividade e 404 estão nos demais arquivos de `apps/web/tests/e2e/`.

## Reset

O reset apaga somente o banco/volumes nomeados da stack de demonstração e recria o seed. Feche qualquer trabalho que precise ser preservado.

```bash
DEMO_RESET_ALLOWED=true ASPNETCORE_ENVIRONMENT=Development bash scripts/demo-reset.sh
```

O script recusa `Production` e execução sem `DEMO_RESET_ALLOWED=true`.

## Diagnóstico

```bash
docker compose ps
docker compose logs --tail=200 api web postgres
docker compose config
```

- Falha de startup por senha: confira os dois campos de `.env` e o mínimo de 14 caracteres.
- `/health/live` funciona e `/health/ready` falha: verifique PostgreSQL e `ConnectionStrings__Database` nos logs.
- login falha após reset: use novamente o valor atual de `DEMO_PASSWORD`.
- provider `NOT_CONFIGURED`: esperado fora de Presentation/Testing; não altere para aparentar sucesso.
- porta ocupada: encerre o processo conflitante; não exponha os serviços em `0.0.0.0` para a POC local.

## Encerramento

```bash
docker compose down --remove-orphans
```

Para remover também os dados exclusivamente demonstrativos, use o reset autorizado ou `docker compose down -v --remove-orphans` somente após confirmar o nome do projeto `municipal-platform`.
