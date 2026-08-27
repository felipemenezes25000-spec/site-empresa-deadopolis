import { expect, test } from "@playwright/test";

const destinations = [
  ["/municipio", "O Município"],
  ["/municipio/gestao", "Gestão Municipal"],
  ["/governo/prefeito", "Prefeito"],
  ["/governo/vice-prefeito", "Vice-prefeito"],
  ["/conselhos", "Conselhos municipais"],
  ["/acesso-a-informacao/estatisticas", "Estatísticas do e-SIC"],
  ["/acesso-a-informacao/perguntas", "Perguntas frequentes"],
  ["/licitacoes/calendario", "Calendário de licitações"],
  ["/obras", "Obras municipais"],
] as const;

for (const [path, heading] of destinations) {
  test(`destino legado ${path} possui rota pública válida`, async ({ page }) => {
    const response = await page.goto(path);
    expect(response?.status()).toBe(200);
    await expect(page.getByRole("heading", { name: heading })).toBeVisible();
  });
}

test("notícias preservam a navegação por área editorial", async ({ page }) => {
  await page.goto("/noticias");
  const category = page.getByRole("combobox", { name: /área da notícia/i });

  await expect(category).toBeVisible();
  await category.selectOption("EDUCACAO");
  await page.getByRole("button", { name: /filtrar notícias/i }).click();

  await expect(page).toHaveURL(/category=EDUCACAO/);
});
