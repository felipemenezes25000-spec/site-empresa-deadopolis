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

## Drill executável do PostgreSQL

O repositório possui dois scripts reais para desenvolvimento, POC e CI:

- `scripts/db-backup.sh [diretorio]`: executa `pg_dump` em formato custom usando o PostgreSQL 17 do próprio compose, grava de forma atômica e produz manifesto `.sha256`;
- `scripts/db-restore-verify.sh <arquivo.dump>`: valida o checksum, cria um PostgreSQL 17 temporário isolado, envia o arquivo por stdin (sem depender de caminhos dentro do contêiner, o que mantém o drill reprodutível em qualquer estação) e exige tabelas públicas e `__EFMigrationsHistory` antes de declarar sucesso.

O workflow E2E executa esse drill contra o ambiente efêmero da CI. Ele não altera o banco original e destrói o container de restore ao final.

## Restore

Restauração de produção deve ocorrer em ambiente isolado primeiro, validar migrations, integridade lógica, amostra de documentos e health da aplicação. Restore em produção exige autorização operacional, janela, registro da causa e evidência pós-restore.

## Estado atual

O PostgreSQL possui **backup + restore drill local/CI executável e verificável**. A execução de 28/08/2026 restaurou 40 tabelas públicas e 8 migrations em contêiner isolado. Isso não equivale a backup de produção: retenção, cofre imutável, cópia do object storage, key ring, criptografia gerenciada, RPO/RTO e restore drill do provedor continuam `EXTERNAL_DEPENDENCY` até a infraestrutura contratada existir e gerar evidência real. O sistema não transforma o sucesso do drill local em alegação de proteção de produção.
