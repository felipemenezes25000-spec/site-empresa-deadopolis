#!/usr/bin/env bash
set -euo pipefail

for runtime_environment in "${ASPNETCORE_ENVIRONMENT:-}" "${DOTNET_ENVIRONMENT:-}" "${NODE_ENV:-}"; do
  if [[ "${runtime_environment,,}" == "production" ]]; then
    echo "Reset da POC recusado: o ambiente Production nunca pode ter dados ou volumes removidos." >&2
    exit 1
  fi
done

if [[ "${DEMO_RESET_ALLOWED:-false}" != "true" ]]; then
  echo "Reset da POC recusado: defina DEMO_RESET_ALLOWED=true somente no ambiente de demonstração." >&2
  exit 1
fi

if ! command -v docker >/dev/null 2>&1; then
  echo "Reset da POC recusado: Docker não está disponível." >&2
  exit 1
fi

echo "Recriando exclusivamente a stack municipal de demonstração e seu volume nomeado..."
docker compose down -v --remove-orphans
docker compose up -d --build
echo "Reset concluído. Aguarde /health/ready antes da apresentação."
