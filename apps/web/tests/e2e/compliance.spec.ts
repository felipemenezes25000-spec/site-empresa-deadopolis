import { expect, test, type Page } from "@playwright/test";

async function signIn(page: Page) {
  const password = process.env.DEMO_PASSWORD;
  if (!password) throw new Error("DEMO_PASSWORD é obrigatório para auditar a tela de compliance.");
  await page.goto("/admin/login");
  await page.getByLabel("Usuário").fill("admin.demo");
  await page.getByLabel("Senha").fill(password);
  await page.getByRole("button", { name: "Entrar" }).click();
  await page.waitForURL(/\/admin$/);
}

async function badges(page: Page) {
  return page.locator("[data-severity]").evaluateAll((elements) =>
    elements.map((element) => ({ label: (element.textContent ?? "").trim(), severity: element.getAttribute("data-severity") })));
}

test("compliance nunca pinta uma capacidade parcial como confirmada", async ({ page }) => {
  await signIn(page);
  await page.goto("/admin/compliance");
  await expect(page.getByRole("heading", { name: "Capacidades do runtime" })).toBeVisible();

  const rendered = await badges(page);
  expect(rendered.length, "a tela precisa publicar estados legíveis").toBeGreaterThan(0);

  for (const badge of rendered) {
    expect(badge.label, "nenhum estado pode ser um número cru").not.toMatch(/^\d+$/);
    if (["DEMO_ONLY", "DEVELOPMENT_ONLY", "NOT_CONFIGURED", "DEGRADED", "NOT_READY"].includes(badge.label)) {
      expect(badge.severity, `${badge.label} não pode aparecer como confirmado`).toBe("attention");
    }
    if (badge.label === "UNAVAILABLE") expect(badge.severity).toBe("blocked");
  }

  const runtime = page.getByRole("heading", { name: "Capacidades do runtime" }).locator("xpath=ancestor::section");
  await expect(runtime.getByText("WebP", { exact: true })).toBeVisible();
  await expect(runtime.getByText("AVIF", { exact: true })).toBeVisible();
  // AVIF must stay explicitly unavailable while the runtime has no encoder for it.
  await expect(runtime.getByText("UNAVAILABLE", { exact: true })).toBeVisible();
  await expect(page.getByRole("heading", { name: "Dependências externas para produção" })).toBeVisible();
});

test("integrações e painel inicial publicam o mesmo vocabulário de estado", async ({ page }) => {
  await signIn(page);

  await page.goto("/admin/integracoes");
  await expect(page.getByRole("heading", { name: "Integrações cadastradas" })).toBeVisible();
  const integrationBadges = await badges(page);
  expect(integrationBadges.length).toBeGreaterThan(0);
  for (const badge of integrationBadges) {
    expect(badge.label).toMatch(/^[A-Z_]+$/);
  }
  await expect(page.getByText("NOT_CONFIGURED").first()).toBeVisible();

  await page.goto("/admin");
  await expect(page.getByRole("heading", { name: "Integrações" })).toBeVisible();
  const dashboardBadges = await badges(page);
  expect(dashboardBadges.length).toBeGreaterThan(0);
  for (const badge of dashboardBadges) {
    expect(badge.label, "o painel não pode exibir o ordinal do enum").not.toMatch(/^\d+$/);
  }
});
