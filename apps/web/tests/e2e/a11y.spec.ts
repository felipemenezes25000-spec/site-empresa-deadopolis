import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Page } from "@playwright/test";

const publicRoutes = ["/", "/servicos", "/servicos/emitir-guia-iptu", "/noticias", "/secretarias", "/transparencia", "/dados-abertos", "/diario-oficial", "/ouvidoria", "/ouvidoria/acompanhar", "/buscar?q=IPTU", "/acesso-a-informacao", "/contatos", "/admin/login"];
const administrativeRoutes = ["/admin", "/admin/conteudo", "/admin/comunicacao", "/admin/midia", "/admin/usuarios", "/admin/tickets", "/admin/compliance", "/admin/dados-abertos", "/admin/diario"];

async function severeViolations(page: Page) {
  const results = await new AxeBuilder({ page }).analyze();
  return results.violations
    .filter((item) => item.impact === "serious" || item.impact === "critical")
    .map((item) => ({ id: item.id, impact: item.impact, nodes: item.nodes.length, target: item.nodes[0]?.target?.join(" ") }));
}

for (const route of publicRoutes) {
  test(`axe sem violações serious/critical: ${route}`, async ({ page }) => {
    await page.goto(route);
    expect(await severeViolations(page)).toEqual([]);
  });
}

test("administração municipal não acumula violações serious/critical", async ({ page }) => {
  test.setTimeout(120_000);
  const password = process.env.DEMO_PASSWORD;
  if (!password) throw new Error("DEMO_PASSWORD é obrigatório para auditar as telas administrativas.");

  await page.goto("/admin/login");
  await page.getByLabel("Usuário").fill("admin.demo");
  await page.getByLabel("Senha").fill(password);
  await page.getByRole("button", { name: "Entrar" }).click();
  await page.waitForURL(/\/admin$/);

  for (const route of administrativeRoutes) {
    await page.goto(route);
    await expect(page.getByRole("heading", { level: 1 }).first(), route).toBeVisible();
    expect(await severeViolations(page), route).toEqual([]);
  }
});

test("navegação por teclado alcança o conteúdo principal e mantém foco visível", async ({ page }) => {
  await page.goto("/");

  await page.keyboard.press("Tab");
  const skipLink = page.getByRole("link", { name: "Ir para o conteúdo principal" });
  await expect(skipLink).toBeFocused();
  await expect(skipLink).toBeVisible();

  const outline = await skipLink.evaluate((element) => getComputedStyle(element).outlineStyle);
  expect(outline, "o foco precisa permanecer visível").not.toBe("none");

  await skipLink.press("Enter");
  await expect(page.locator("#conteudo-principal")).toBeVisible();
});

test("estrutura semântica pública expõe marco principal e hierarquia de títulos", async ({ page }) => {
  for (const route of ["/", "/servicos", "/ouvidoria", "/diario-oficial"]) {
    await page.goto(route);
    await expect(page.locator("main#conteudo-principal"), route).toHaveCount(1);
    await expect(page.getByRole("banner"), route).toHaveCount(1);
    await expect(page.getByRole("contentinfo"), route).toHaveCount(1);
    await expect(page.getByRole("heading", { level: 1 }), route).toHaveCount(1);
    const levels = await page.locator("h1, h2, h3, h4, h5, h6").evaluateAll((headings) => headings.map((heading) => Number(heading.tagName.slice(1))));
    const skipped = levels.filter((level, index) => index > 0 && level - levels[index - 1] > 1);
    expect(skipped, `níveis de título pulados em ${route}`).toEqual([]);
  }
});
