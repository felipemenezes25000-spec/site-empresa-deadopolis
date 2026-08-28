import { expect, test } from "@playwright/test";

// A missing municipal resource must answer 404. A root loading.tsx would start streaming the shell
// before notFound() runs and silently downgrade every one of these to HTTP 200.
const missingResources = [
  "/servicos/servico-que-nao-existe",
  "/noticias/noticia-que-nao-existe",
  "/secretarias/secretaria-que-nao-existe",
  "/dados-abertos/dataset-que-nao-existe",
  "/transparencia/categoria-que-nao-existe",
  "/verificar/CODIGO-INEXISTENTE",
  "/rota-que-nao-existe",
];

test("recurso municipal inexistente responde 404 e não 200", async ({ request }) => {
  for (const route of missingResources) {
    const response = await request.get(route);
    expect(response.status(), route).toBe(404);
  }
});

test("página 404 mantém navegação governada e busca municipal", async ({ page }) => {
  await page.goto("/servicos/servico-que-nao-existe");

  await expect(page.getByRole("heading", { level: 1, name: "Esta página não foi encontrada." })).toBeVisible();
  await expect(page.getByRole("navigation", { name: /principal/i })).toBeVisible();
  await expect(page.getByRole("searchbox", { name: "Buscar no portal" })).toBeVisible();
  await expect(page.getByRole("complementary").getByRole("link", { name: "Carta de Serviços" })).toBeVisible();

  await page.getByRole("searchbox", { name: "Buscar no portal" }).fill("IPTU");
  await page.getByRole("button", { name: "Buscar" }).click();
  await page.waitForURL(/\/buscar\?q=IPTU/);
  await expect(page.getByRole("link", { name: /IPTU/i }).first()).toBeVisible();
});

test("páginas de detalhe expõem trilha de navegação municipal", async ({ page }) => {
  const trails: Array<[string, string[]]> = [
    ["/servicos/emitir-guia-iptu", ["Início", "Serviços"]],
    ["/secretarias/saude", ["Início", "Secretarias"]],
    ["/transparencia/documentos", ["Início", "Transparência"]],
  ];

  for (const [route, expected] of trails) {
    await page.goto(route);
    const breadcrumb = page.getByRole("navigation", { name: "Breadcrumb" });
    await expect(breadcrumb, route).toBeVisible();
    for (const label of expected) {
      await expect(breadcrumb.getByRole("link", { name: label }), `${route} → ${label}`).toBeVisible();
    }
    await expect(breadcrumb.locator("[aria-current='page']"), route).toHaveCount(1);
  }

  await page.goto("/servicos/emitir-guia-iptu");
  await page.getByRole("navigation", { name: "Breadcrumb" }).getByRole("link", { name: "Serviços" }).click();
  await page.waitForURL(/\/servicos$/);
  await expect(page.getByRole("heading", { level: 1, name: /O que você precisa resolver/i })).toBeVisible();
});
