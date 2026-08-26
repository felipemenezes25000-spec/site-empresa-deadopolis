#!/usr/bin/env sh
set -eu
patterns='(BEGIN (RSA |EC |OPENSSH )?PRIVATE KEY|AKIA[0-9A-Z]{16}|-----BEGIN CERTIFICATE-----|Demo__Password[[:space:]]*:[[:space:]]*[^$[:space:]]|POSTGRES_PASSWORD[[:space:]]*=[[:space:]]*[^$[:space:]])'
if git grep -nEI "$patterns" -- ':!scripts/secret-scan.sh' ':!docs/**' ':!*.md'; then
  echo "Potential secret material found in tracked source." >&2
  exit 1
fi
if git ls-files | grep -Ei '\.(pfx|p12|key|pem)$'; then
  echo "Sensitive certificate/key file extension found in repository." >&2
  exit 1
fi
echo "Secret scan: no known secret material patterns detected."
