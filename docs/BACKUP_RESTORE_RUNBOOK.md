# Runbook de backup e restore

O portal registra `BackupEvidence` e possui um drill executável para PostgreSQL em desenvolvimento/POC/CI. A orquestração, retenção e o cofre de produção pertencem à infraestrutura contratada; registrar evidência sem artefato real é proibido.

## Escopo mínimo

Um ponto de recuperação completo contém:

1. dump consistente do PostgreSQL;
2. objetos do storage e respectivos metadados/versões;
3. key ring de Data Protection;
4. configuração versionada e referência aos segredos no secret manager;
5. manifesto com horário, provider, tamanho, checksum e retenção.

RPO, RTO, retenção e residência dos dados precisam de aprovação institucional antes do go-live. A ausência dessa decisão bloqueia produção, não a POC.

## Drill local e CI

Com o compose já em execução:

```bash
backup_file="$(bash scripts/db-backup.sh /tmp/municipal-backups)"
bash scripts/db-restore-verify.sh "$backup_file"
```

O primeiro script usa `pg_dump --format=custom`, grava o dump com permissão restritiva e cria `<arquivo>.sha256`. O segundo valida o checksum e restaura em um container `postgres:17-alpine` temporário, distinto do banco de origem. O sucesso exige pelo menos uma tabela pública e histórico EF de migrations. O container temporário é removido mesmo em caso de erro.

Esse drill prova o mecanismo de dump/restore do PostgreSQL usado pelo projeto. Ele **não** prova retenção, object storage, key ring, RPO/RTO ou recuperação do provedor de produção.

## Backup controlado de produção

A infraestrutura aprovada deve executar o equivalente com credenciais vindas do secret manager, retenção, criptografia e destino imutável. O artefato não deve permanecer no host da aplicação. O backup de objetos deve usar snapshot/versionamento nativo do storage e registrar uma janela de consistência compatível com o dump do banco.

## Restore drill de produção

1. Abra change request com responsável, janela e artefatos escolhidos.
2. Valide checksums antes de abrir o dump.
3. Crie banco vazio com credencial temporária.
4. Restaure sem substituir o ambiente produtivo existente.
5. Restaure os objetos preservando chaves e hashes.
6. Restaure o key ring correspondente; sem ele, sessões e dados protegidos anteriores podem ficar inválidos.
7. Inicie API/Web isolados com `PresentationMode=false` e DNS não público.
8. Execute migrations somente se o objetivo for testar upgrade; registre o commit usado.
9. Valide `/health/ready`, login, leitura de conteúdo, download amostral e SHA-256, tenant isolation e auditoria.
10. Registre resultado real em `/admin/operacoes`, inclusive falha e mensagem quando aplicável.
11. Destrua o ambiente temporário pelo mecanismo aprovado do provider.

## Critérios de aceite

- checksum do artefato confere;
- banco abre sem erro e migrations correspondem ao commit;
- amostra de objetos confere com hashes persistidos;
- key ring funciona ou a perda planejada está formalmente aceita;
- nenhum endpoint de restore ficou público;
- duração real é compatível com o RTO aprovado;
- evidência aponta para artefato/provider real e possui `restoreTestedAt`.

Falha em qualquer critério gera estado de falha/degradação e novo drill após correção. Nunca altere a evidência para aparentar sucesso.
