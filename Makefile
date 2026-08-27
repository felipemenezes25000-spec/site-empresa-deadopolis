.PHONY: demo-reset demo-up demo-down docs-verify test

demo-reset:
	bash scripts/demo-reset.sh

demo-up:
	docker compose up -d --build

demo-down:
	docker compose down --remove-orphans

docs-verify:
	bash scripts/tests/verify-docs.test.sh
	bash scripts/verify-docs.sh

test:
	bash scripts/tests/demo-reset.test.sh
	bash scripts/tests/verify-docs.test.sh
	dotnet test MunicipalPlatform.sln
	npm --prefix apps/web run lint
	npm --prefix apps/web run typecheck
	npm --prefix apps/web run test
