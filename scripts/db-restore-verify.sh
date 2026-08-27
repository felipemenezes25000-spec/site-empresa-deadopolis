#!/usr/bin/env bash
set -euo pipefail

if [[ $# -ne 1 ]]; then
  echo "Uso: $0 /caminho/backup.dump" >&2
  exit 2
fi

backup_path="$1"
[[ -f "$backup_path" && -s "$backup_path" ]] || { echo "Backup inexistente ou vazio: $backup_path" >&2; exit 2; }
backup_dir="$(cd "$(dirname "$backup_path")" && pwd)"
backup_path="$backup_dir/$(basename "$backup_path")"
checksum_path="$backup_path.sha256"

command -v docker >/dev/null 2>&1 || { echo "Docker é obrigatório para o restore drill." >&2; exit 2; }

if [[ -f "$checksum_path" ]]; then
  (
    cd "$backup_dir"
    sha256sum -c "$(basename "$checksum_path")" >/dev/null
  )
else
  echo "Manifesto SHA-256 ausente: $checksum_path" >&2
  exit 3
fi

container_name="municipal-restore-verify-$$-$RANDOM"
restore_password="restore-$RANDOM-$(date +%s)"
cleanup() {
  docker rm -f "$container_name" >/dev/null 2>&1 || true
}
trap cleanup EXIT

docker run -d \
  --name "$container_name" \
  --security-opt no-new-privileges:true \
  -e POSTGRES_DB=municipal_restore \
  -e POSTGRES_USER=municipal \
  -e POSTGRES_PASSWORD="$restore_password" \
  postgres:17-alpine >/dev/null

ready=false
for _ in $(seq 1 30); do
  if docker exec -e PGPASSWORD="$restore_password" "$container_name" \
      pg_isready -h 127.0.0.1 -U municipal -d municipal_restore >/dev/null 2>&1; then
    ready=true
    break
  fi
  sleep 1
done
[[ "$ready" == true ]] || { echo "PostgreSQL temporário não ficou pronto." >&2; exit 4; }

docker cp "$backup_path" "$container_name:/tmp/municipal.dump" >/dev/null
docker exec -e PGPASSWORD="$restore_password" "$container_name" \
  pg_restore \
    --host 127.0.0.1 \
    --username municipal \
    --dbname municipal_restore \
    --no-owner \
    --no-privileges \
    /tmp/municipal.dump

table_count="$(docker exec -e PGPASSWORD="$restore_password" "$container_name" \
  psql -h 127.0.0.1 -U municipal -d municipal_restore -Atqc \
  "SELECT count(*) FROM information_schema.tables WHERE table_schema = 'public';")"
[[ "$table_count" =~ ^[0-9]+$ && "$table_count" -gt 0 ]] || { echo "Restore não produziu tabelas públicas." >&2; exit 5; }

migration_count="$(docker exec -e PGPASSWORD="$restore_password" "$container_name" \
  psql -h 127.0.0.1 -U municipal -d municipal_restore -Atqc \
  'SELECT count(*) FROM "__EFMigrationsHistory";' 2>/dev/null || true)"
[[ "$migration_count" =~ ^[0-9]+$ && "$migration_count" -gt 0 ]] || { echo "Histórico de migrations ausente após restore." >&2; exit 6; }

printf 'Restore isolado validado: %s tabelas públicas, %s migrations aplicadas.\n' "$table_count" "$migration_count"
