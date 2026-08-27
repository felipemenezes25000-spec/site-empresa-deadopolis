# Runbook de backup e restore

O portal registra `BackupEvidence`, mas não executa backup automaticamente. A orquestração pertence à infraestrutura contratada; registrar evidência sem artefato real é proibido.

## Escopo mínimo

Um ponto de recuperação completo contém:

1. dump consistente do PostgreSQL;
2. objetos do storage e respectivos metadados/versões;
3. key ring de Data Protection;
4. configuração versionada e referência aos segredos no secret manager;
5. manifesto com horário, provider, tamanho, checksum e retenção.

RPO, RTO, retenção e residência dos dados precisam de aprovação institucional antes do go-live. A ausência dessa decisão bloqueia produção, não a POC.

## Backup controlado do PostgreSQL

Execute a partir de host confiável com `MUNICIPAL_DATABASE_URL` injetada pelo secret manager:

```bash
umask 077
backup_file="municipal-$(date -u +%Y%m%dT%H%M%SZ).dump"
pg_dump --format=custom --no-owner --no-acl --dbname="$MUNICIPAL_DATABASE_URL" --file="$backup_file"
sha256sum "$backup_file" > "$backup_file.sha256"
pg_restore --list "$backup_file" >/dev/null
```

Transfira dump e checksum para o cofre imutável do provider. Não deixe cópia permanente no host de aplicação. O backup de objetos deve usar snapshot/versionamento nativo do storage e registrar a mesma janela de consistência.

## Restore drill

O drill usa banco isolado e storage não público.

1. Abra change request com responsável, janela e artefatos escolhidos.
2. Valide checksums antes de abrir o dump.
3. Crie banco vazio com credencial temporária.
4. Restaure:

```bash
pg_restore --clean --if-exists --no-owner --no-acl --dbname="$MUNICIPAL_RESTORE_DATABASE_URL" "$backup_file"
```

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
