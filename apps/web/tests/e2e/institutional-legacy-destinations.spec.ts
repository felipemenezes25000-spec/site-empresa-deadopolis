import { expect, test } from "@playwright/test";

const destinations = [
  ["/municipio", "O Município"],
  ["/municipio/gestao", "Gestão Municipal"],
  ["/governo/prefeito", "Prefeito"],
  ["/governo/vice-prefeito", "Vice-prefeito"],
] as const;

for (const [path, heading] of destinations) {
  test(`destino legado ${path} possui rota pública válida`, async ({ page }) => {
    const response = await page.goto(path);
    expect(response?.status()).toBe(200);
    await expect(page.getByRole("heading", { name: heading })).toBeVisible();
  });
}
