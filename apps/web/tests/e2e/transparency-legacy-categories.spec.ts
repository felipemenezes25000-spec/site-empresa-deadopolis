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
