import { expect, test, type Page } from "@playwright/test";

const viewports = [
  { name: "375px", width: 375, height: 812 },
  { name: "768px", width: 768, height: 1024 },
  { name: "1024px", width: 1024, height: 768 },
  { name: "1440px", width: 1440, height: 900 },
];

const publicRoutes = ["/", "/servicos", "/servicos/emitir-guia-iptu", "/noticias", "/secretarias", "/transparencia", "/transparencia/documentos", "/dados-abertos", "/diario-oficial", "/ouvidoria", "/ouvidoria/acompanhar", "/buscar?q=IPTU", "/licitacoes", "/contatos"];
const administrativeRoutes = ["/admin", "/admin/conteudo", "/admin/comunicacao", "/admin/midia", "/admin/usuarios", "/admin/tickets", "/admin/compliance", "/admin/dados-abertos", "/admin/operacoes", "/admin/diario"];

// A page that scrolls sideways on a phone hides municipal content behind an invisible gesture.
async function horizontalOverflow(page: Page) {
  return page.evaluate(() => {
    const root = document.documentElement;
    if (root.scrollWidth <= root.clientWidth + 1) return null;
    const widest = [...document.querySelectorAll<HTMLElement>("body *")]
      .filter((element) => element.getBoundingClientRect().right > root.clientWidth + 1)
      .slice(-3)
      .map((element) => `${element.tagName}.${element.className.toString().slice(0, 48)}`);
    return { scrollWidth: root.scrollWidth, clientWidth: root.clientWidth, widest };
  });
}

test("portal público não rola horizontalmente em nenhum ponto de quebra", async ({ page }) => {
  test.setTimeout(180_000);
  for (const viewport of viewports) {
    await page.setViewportSize({ width: viewport.width, height: viewport.height });
    for (const route of publicRoutes) {
      await page.goto(route);
      await expect(page.getByRole("heading", { level: 1 }).first(), `${route} @ ${viewport.name}`).toBeVisible();
      expect(await horizontalOverflow(page), `${route} @ ${viewport.name}`).toBeNull();
    }
  }
});

test("workspace administrativo não rola horizontalmente em nenhum ponto de quebra", async ({ page }) => {
  test.setTimeout(180_000);
  const password = process.env.DEMO_PASSWORD;
  if (!password) throw new Error("DEMO_PASSWORD é obrigatório para auditar o workspace administrativo.");

  await page.goto("/admin/login");
  await page.getByLabel("Usuário").fill("admin.demo");
  await page.getByLabel("Senha").fill(password);
  await page.getByRole("button", { name: "Entrar" }).click();
  await page.waitForURL(/\/admin$/);

  for (const viewport of viewports) {
    await page.setViewportSize({ width: viewport.width, height: viewport.height });
    for (const route of administrativeRoutes) {
      await page.goto(route);
      await expect(page.getByRole("heading", { level: 1 }).first(), `${route} @ ${viewport.name}`).toBeVisible();
      expect(await horizontalOverflow(page), `${route} @ ${viewport.name}`).toBeNull();
    }
  }
});

test("menu móvel abre a navegação municipal e fecha por teclado", async ({ page }) => {
  await page.setViewportSize({ width: 375, height: 812 });
  await page.goto("/");

  const mobileMenu = page.locator("details.mobile-navigation");
  await expect(mobileMenu).toBeVisible();
  await expect(mobileMenu.getByRole("link", { name: "Serviços" })).toBeHidden();

  await mobileMenu.getByText("Menu", { exact: true }).click();
  await expect(mobileMenu.getByRole("link", { name: "Serviços" })).toBeVisible();
  await expect(mobileMenu.getByRole("link", { name: "Diário Oficial" })).toBeVisible();

  await page.keyboard.press("Enter");
  await expect(mobileMenu.getByRole("link", { name: "Serviços" })).toBeHidden();
});
