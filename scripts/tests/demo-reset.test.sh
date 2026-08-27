#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repository_root="$(cd "$script_dir/../.." && pwd)"
reset_script="$repository_root/scripts/demo-reset.sh"

if [[ ! -f "$reset_script" ]]; then
  echo "FAIL: scripts/demo-reset.sh ainda não existe"
  exit 1
fi

fixture_root="$(mktemp -d)"
trap 'rm -rf -- "$fixture_root"' EXIT
fake_bin="$fixture_root/bin"
docker_log="$fixture_root/docker.log"
mkdir -p "$fake_bin"
cat > "$fake_bin/docker" <<'FAKE_DOCKER'
#!/usr/bin/env bash
printf '%s\n' "$*" >> "$DEMO_RESET_TEST_LOG"
FAKE_DOCKER
chmod +x "$fake_bin/docker"

set +e
production_output="$(PATH="$fake_bin:$PATH" DEMO_RESET_TEST_LOG="$docker_log" DEMO_RESET_ALLOWED=true ASPNETCORE_ENVIRONMENT=Production bash "$reset_script" 2>&1)"
production_status=$?
set -e
if [[ $production_status -eq 0 || "$production_output" != *"Production"* ]]; then
  echo "FAIL: reset não bloqueou explicitamente o ambiente Production"
  exit 1
fi
if [[ -e "$docker_log" ]]; then
  echo "FAIL: Docker foi chamado antes do bloqueio de produção"
  exit 1
fi

set +e
mixed_environment_output="$(PATH="$fake_bin:$PATH" DEMO_RESET_TEST_LOG="$docker_log" DEMO_RESET_ALLOWED=true ASPNETCORE_ENVIRONMENT=Development NODE_ENV=production bash "$reset_script" 2>&1)"
mixed_environment_status=$?
set -e
if [[ $mixed_environment_status -eq 0 || "$mixed_environment_output" != *"Production"* ]]; then
  echo "FAIL: reset aceitou Production quando outro marcador indicava Development"
  exit 1
fi
if [[ -e "$docker_log" ]]; then
  echo "FAIL: Docker foi chamado em ambiente com marcadores conflitantes"
  exit 1
fi

set +e
opt_in_output="$(PATH="$fake_bin:$PATH" DEMO_RESET_TEST_LOG="$docker_log" ASPNETCORE_ENVIRONMENT=Development bash "$reset_script" 2>&1)"
opt_in_status=$?
set -e
if [[ $opt_in_status -eq 0 || "$opt_in_output" != *"DEMO_RESET_ALLOWED=true"* ]]; then
  echo "FAIL: reset aceitou execução sem autorização explícita"
  exit 1
fi

PATH="$fake_bin:$PATH" DEMO_RESET_TEST_LOG="$docker_log" DEMO_RESET_ALLOWED=true ASPNETCORE_ENVIRONMENT=Development bash "$reset_script"
mapfile -t docker_calls < "$docker_log"
if [[ ${#docker_calls[@]} -ne 2 ]]; then
  echo "FAIL: reset deveria executar exatamente duas operações Docker"
  printf '%s\n' "${docker_calls[@]}"
  exit 1
fi
if [[ "${docker_calls[0]}" != "compose down -v --remove-orphans" || "${docker_calls[1]}" != "compose up -d --build" ]]; then
  echo "FAIL: sequência Docker do reset não preserva o contrato esperado"
  printf '%s\n' "${docker_calls[@]}"
  exit 1
fi

echo "PASS: reset da POC bloqueia produção e exige autorização explícita"
