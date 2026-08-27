# Backup e restore

Este é o contrato de engenharia para backup e restauração. O passo a passo operacional detalhado está em `BACKUP_RESTORE_RUNBOOK.md`.

## Escopo mínimo de produção

O backup deve cobrir PostgreSQL e objetos persistidos no storage. Secrets e certificados devem seguir o mecanismo seguro do provedor e não ser copiados para o repositório.

## Requisitos

- backup automatizado com retenção definida pela Prefeitura;
- cópia fora do mesmo ponto único de falha do workload primário;
- criptografia em trânsito e repouso conforme o provedor adotado;
- evidência de execução, tamanho, checksum/identificador do snapshot e horário;
- teste periódico de restauração em ambiente isolado;
- RPO e RTO aprovados antes do go-live;
- acesso restrito e auditável aos artefatos de backup.

## Restore

Restauração deve ocorrer em ambiente isolado primeiro, validar migrations, integridade lógica, amostra de documentos e health da aplicação. Restore em produção exige autorização operacional, janela, registro da causa e evidência pós-restore.

## Estado atual

O código possui `BackupEvidence`, painel operacional e runbook, mas a orquestração real depende da infraestrutura escolhida. Portanto backup/restore de produção continua `EXTERNAL_DEPENDENCY` até existir job real, retenção, destino, restore testado e evidência aprovada. Não há simulação de sucesso de backup no modo de produção.
