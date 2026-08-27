#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repository_root="$(cd "$script_dir/.." && pwd)"
output_dir="${1:-$repository_root/.data/backups}"
compose_file="$repository_root/compose.yaml"

command -v docker >/dev/null 2>&1 || { echo "Docker é obrigatório para o backup local." >&2; exit 2; }
[[ -f "$compose_file" ]] || { echo "compose.yaml não encontrado." >&2; exit 2; }

if ! docker compose -f "$compose_file" ps --status running --services 2>/dev/null | grep -qx postgres; then
  echo "O serviço postgres do compose precisa estar em execução." >&2
  exit 3
fi

umask 077
mkdir -p "$output_dir"
output_dir="$(cd "$output_dir" && pwd)"
timestamp="$(date -u +%Y%m%dT%H%M%SZ)"
final_path="$output_dir/municipal-platform-$timestamp.dump"
temporary_path="$final_path.part"
checksum_path="$final_path.sha256"
trap 'rm -f -- "$temporary_path"' EXIT

# O dump é somente leitura e usa o PostgreSQL 17 do próprio compose, evitando
# incompatibilidade entre versões do cliente e do servidor.
docker compose -f "$compose_file" exec -T postgres \
  pg_dump \
    --username municipal \
    --dbname municipal_platform \
    --format=custom \
    --compress=6 \
    --no-owner \
    --no-privileges > "$temporary_path"

test -s "$temporary_path" || { echo "pg_dump produziu um arquivo vazio." >&2; exit 4; }
mv -- "$temporary_path" "$final_path"
(
  cd "$output_dir"
  sha256sum "$(basename "$final_path")" > "$(basename "$checksum_path")"
)

# stdout contém somente o caminho para permitir composição segura em scripts/CI.
printf '%s\n' "$final_path"
