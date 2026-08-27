# Operações

## Sinais operacionais

A API expõe `/health/live` e `/health/ready`. Readiness verifica o banco e reporta, sem mascarar, o estado de storage, assinatura digital, timestamp, e-mail e malware scanner.

A aplicação possui correlation ID, métricas e tracing instrumentados. OpenTelemetry coleta ASP.NET Core, HttpClient e o meter próprio `MunicipalPlatform.Api`; exportação OTLP é opt-in por `Observability:OtlpEnabled=true` ou `OTEL_EXPORTER_OTLP_ENDPOINT`.

## Rotinas

- acompanhar health/readiness e erros HTTP;
- revisar links externos pelo worker SSRF-safe e pelo painel de Operações;
- registrar e revisar evidências de backup/restore;
- acompanhar calendário editorial e conteúdo vencido em Governança de conteúdo;
- revisar tickets/SLA e integrações degradadas;
- investigar eventos com correlation ID e trilha `AuditEvent`;
- aplicar change requests e changelog para alterações operacionais relevantes.

## Incidente

1. registrar horário, impacto, ambiente e correlation IDs;
2. verificar health, dependências externas e último deployment;
3. reduzir impacto sem apagar evidências;
4. decidir rollback quando a alteração recente for causa provável;
5. registrar ação corretiva e evidência de recuperação;
6. abrir RFC/changelog quando houver mudança permanente.

## Estados honestos

`NOT_CONFIGURED`, `DEMO_ONLY`, `DEVELOPMENT_ONLY` e `EXTERNAL_DEPENDENCY` não são sucesso de produção. O painel e o runbook devem preservar esses estados até que fornecedor, credencial e teste real existam.

Para backup e disaster recovery, consulte `BACKUP_RESTORE.md`. Para implantação, `DEPLOYMENT.md`.
