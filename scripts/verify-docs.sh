#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repository_root="${1:-$(cd "$script_dir/.." && pwd)}"

if [[ ! -d "$repository_root" ]]; then
  echo "Contrato documental inválido: diretório não encontrado: $repository_root" >&2
  exit 2
fi

required_files=(
  "README.md"
  "docs/ARCHITECTURE.md"
  "docs/BACKUP_RESTORE_RUNBOOK.md"
  "docs/CURRENT_PORTAL_AUDIT.md"
  "docs/EXECUTIVE_DEMO.md"
  "docs/EXTERNAL_DEPENDENCIES.md"
  "docs/FINAL_REPORT.md"
  "docs/IMPLEMENTATION_PLAN.md"
  "docs/LEGACY_MIGRATION_REPORT.md"
  "docs/POC_RUNBOOK.md"
  "docs/PRODUCTION_RUNBOOK.md"
  "docs/REQUIREMENTS_MATRIX.md"
  "docs/SECURITY.md"
  "docs/URL_MIGRATION_MAP.md"
)

missing_files=()
for relative_path in "${required_files[@]}"; do
  if [[ ! -s "$repository_root/$relative_path" ]]; then
    missing_files+=("$relative_path")
  fi
done

if (( ${#missing_files[@]} > 0 )); then
  echo "Contrato documental incompleto. Arquivos obrigatórios ausentes ou vazios:" >&2
  printf ' - %s\n' "${missing_files[@]}" >&2
  exit 1
fi

echo "Contrato documental verificado: ${#required_files[@]} arquivos obrigatórios presentes."
