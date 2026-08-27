.PHONY: demo-reset demo-up demo-down docs-verify backup restore-verify test

demo-reset:
	bash scripts/demo-reset.sh

demo-up:
	docker compose up -d --build

demo-down:
	docker compose down --remove-orphans

docs-verify:
	bash scripts/tests/verify-docs.test.sh
	bash scripts/verify-docs.sh

backup:
	bash scripts/db-backup.sh

restore-verify:
	@test -n "$(BACKUP_FILE)" || (echo "Use: make restore-verify BACKUP_FILE=/caminho/arquivo.dump" >&2; exit 2)
	bash scripts/db-restore-verify.sh "$(BACKUP_FILE)"

test:
	bash scripts/tests/demo-reset.test.sh
	bash scripts/tests/verify-docs.test.sh
	bash -n scripts/db-backup.sh scripts/db-restore-verify.sh
	dotnet test MunicipalPlatform.sln
	npm --prefix apps/web run lint
	npm --prefix apps/web run typecheck
	npm --prefix apps/web run test
