import { expect, test } from "@playwright/test";

const routes = ["/", "/servicos", "/noticias", "/secretarias", "/transparencia", "/diario-oficial", "/buscar?q=IPTU", "/buscar?q=sa%C3%BAde", "/acesso-a-informacao", "/ouvidoria", "/ouvidoria/acompanhar", "/dados-abertos", "/agenda", "/locais", "/contatos", "/licitacoes", "/legislacao", "/privacidade", "/acessibilidade", "/demo/modernization", "/admin/login"];

test("rotas públicas críticas não retornam 404/500", async ({ page, request }) => {
  test.setTimeout(90_000);
  for (const route of routes) {
    const response = await request.get(route);
    expect(response.status(), route).toBeLessThan(400);
    await page.goto(route);
    // A server error is rendered by the error boundary with HTTP 200, so the status alone proves nothing.
    await expect(page.locator("body"), route).not.toContainText("Application error");
    await expect(page.locator("body"), `boundary de erro em ${route}`).not.toContainText("Não foi possível carregar o portal");
    await expect(page.locator("h1").first(), `título principal ausente em ${route}`).toBeVisible();
    const brokenImages = await page.locator("img").evaluateAll((images) => images.filter((image) => !(image as HTMLImageElement).complete || (image as HTMLImageElement).naturalWidth === 0).map((image) => (image as HTMLImageElement).src));
    expect(brokenImages, `imagens quebradas em ${route}`).toEqual([]);
  }
});
