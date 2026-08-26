import { expect, test } from "@playwright/test";

test("diretório público abre detalhe da secretaria por slug", async ({ page }) => {
  await page.goto("/secretarias");
  const health = page.getByRole("link", { name: /Secretaria Municipal de Saúde/i });
  await expect(health).toHaveAttribute("href", "/secretarias/saude");
  await health.click();
  await expect(page.getByRole("heading", { name: "Secretaria Municipal de Saúde" })).toBeVisible();
  await expect(page.getByRole("link", { name: /Voltar para Secretarias e órgãos/i })).toHaveAttribute("href", "/secretarias");
});
