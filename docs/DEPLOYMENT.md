# Deployment

A plataforma possui dois contextos distintos: POC reproduzível e produção. Os procedimentos não devem ser misturados.

## POC

O POC usa `compose.yaml`, credenciais efêmeras e `PresentationMode=true`. O reset seguro é executado por `make demo-reset` / `scripts/demo-reset.sh` e recusa ambiente Production. Consulte `POC_RUNBOOK.md`.

## Produção

O repositório entrega imagens de API e Web e uma topologia de referência. O go-live exige infraestrutura externa escolhida pela Prefeitura ou contratada: PostgreSQL gerenciado ou operado com backup, storage S3 compatível, TLS/reverse proxy, DNS, secret manager, observabilidade/collector, malware scanner e providers institucionais.

Configurações obrigatórias devem ser fornecidas por segredo/variável de ambiente. Nunca versionar senha, certificado privado, `.pfx`, token, connection string real ou credencial de e-mail.

## Sequência de implantação

1. Fixar o SHA aprovado e confirmar CI completa verde nesse SHA.
2. Provisionar banco, storage, secrets, TLS e rede.
3. Executar migrations EF em banco vazio de homologação e depois no banco de destino com backup prévio.
4. Subir API e validar `/health/live` e `/health/ready`.
5. Subir Web apontando `API_URL` para a API homologada.
6. Configurar `OTEL_EXPORTER_OTLP_ENDPOINT` quando houver collector; sem endpoint, a aplicação continua funcional sem exporter externo.
7. Configurar providers externos e confirmar que nenhum componente crítico segue `NOT_CONFIGURED` antes de promovê-lo a requisito de produção.
8. Executar smoke, E2E, acessibilidade e rotas profundas no hostname final.
9. Fazer cutover de DNS/redirects somente após aprovação.

## Rollback

Rollback significa restaurar a versão de aplicação previamente aprovada e, quando houver alteração incompatível de dados, seguir o procedimento de restore aprovado em `BACKUP_RESTORE.md`. Não se deve executar migration destrutiva ou restore em produção sem janela, backup validado e autorização operacional.

Detalhes de produção e dependências externas estão em `PRODUCTION_RUNBOOK.md` e `EXTERNAL_DEPENDENCIES.md`.
