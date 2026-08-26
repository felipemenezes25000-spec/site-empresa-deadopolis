.PHONY: demo-reset demo-up demo-down test

demo-reset:
	docker compose down -v --remove-orphans
	docker compose up -d --build

demo-up:
	docker compose up -d --build

demo-down:
	docker compose down --remove-orphans

test:
	dotnet test MunicipalPlatform.sln
	npm --prefix apps/web run lint
	npm --prefix apps/web run typecheck
	npm --prefix apps/web run test
