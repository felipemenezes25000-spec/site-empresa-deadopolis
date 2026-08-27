import { expect, test } from "@playwright/test";

test("transparência preserva categorias históricas sem aceitar slugs arbitrários", async ({ page }) => {
  await page.goto("/transparencia");
  const rreo = page.getByRole("link", { name: /RREO/ });
  await expect(rreo).toHaveAttribute("href", "/transparencia/rreo");
  await rreo.click();
  await expect(page.getByRole("heading", { name: /Relatório Resumido da Execução Orçamentária/ })).toBeVisible();

  const unknown = await page.goto("/transparencia/categoria-inexistente");
  expect(unknown?.status()).toBe(404);
});

test("acervo documental oferece busca pública e estado vazio orientativo", async ({ page }) => {
  await page.goto("/transparencia/documentos");

  await expect(page.getByRole("heading", { level: 1, name: /acervo público de documentos/i })).toBeVisible();
  await expect(page.getByRole("searchbox", { name: /buscar no acervo/i })).toBeVisible();
  await expect(page.getByRole("button", { name: /pesquisar/i })).toBeVisible();
  await expect(page.getByText(/documentos publicados/i)).toBeVisible();
});

test("licitações combina fontes oficiais e acervo histórico filtrável", async ({ page }) => {
  await page.goto("/licitacoes");

  await expect(page.getByRole("heading", { level: 1, name: /licitações e contratos/i })).toBeVisible();
  await expect(page.getByRole("searchbox", { name: /buscar no acervo/i })).toBeVisible();
  await expect(page.getByRole("combobox", { name: /etapa/i })).toBeVisible();
  await expect(page.getByText(/documentos publicados/i)).toBeVisible();
});
