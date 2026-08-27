#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repository_root="$(cd "$script_dir/../.." && pwd)"
verifier="$repository_root/scripts/verify-docs.sh"

if [[ ! -f "$verifier" ]]; then
  echo "FAIL: scripts/verify-docs.sh ainda não existe"
  exit 1
fi

fixture_root="$(mktemp -d)"
trap 'rm -rf -- "$fixture_root"' EXIT
mkdir -p "$fixture_root/docs"

set +e
missing_output="$(bash "$verifier" "$fixture_root" 2>&1)"
missing_status=$?
set -e

if [[ $missing_status -eq 0 ]]; then
  echo "FAIL: repositório sem documentação obrigatória foi aceito"
  exit 1
fi
if [[ "$missing_output" != *"README.md"* || "$missing_output" != *"docs/FINAL_REPORT.md"* || "$missing_output" != *"docs/LGPD.md"* ]]; then
  echo "FAIL: diagnóstico não identificou arquivos obrigatórios ausentes"
  printf '%s\n' "$missing_output"
  exit 1
fi

required_files=(
  "README.md"
  "docs/ARCHITECTURE.md"
  "docs/ACCESSIBILITY.md"
  "docs/BACKUP_RESTORE.md"
  "docs/BACKUP_RESTORE_RUNBOOK.md"
  "docs/CURRENT_PORTAL_AUDIT.md"
  "docs/DEPLOYMENT.md"
  "docs/EMAIL.md"
  "docs/EXECUTIVE_DEMO.md"
  "docs/EXTERNAL_DEPENDENCIES.md"
  "docs/FINAL_REPORT.md"
  "docs/GAZETTE.md"
  "docs/ICP_BRASIL.md"
  "docs/IMPLEMENTATION_PLAN.md"
  "docs/LEGACY_MIGRATION_REPORT.md"
  "docs/LGPD.md"
  "docs/MIGRATION_PLAN.md"
  "docs/OPERATIONS.md"
  "docs/POC_RUNBOOK.md"
  "docs/PRODUCTION_RUNBOOK.md"
  "docs/REQUIREMENTS_MATRIX.md"
  "docs/SECURITY.md"
  "docs/URL_MIGRATION_MAP.md"
)

for relative_path in "${required_files[@]}"; do
  mkdir -p "$(dirname "$fixture_root/$relative_path")"
  printf '# Evidência\n' > "$fixture_root/$relative_path"
done

bash "$verifier" "$fixture_root"
echo "PASS: contrato documental rejeita lacunas e aceita o conjunto completo"
