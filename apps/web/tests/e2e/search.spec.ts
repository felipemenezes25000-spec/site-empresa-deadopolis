import { expect, test } from "@playwright/test";

// The runtime image starts .NET in globalization-invariant mode, where Unicode normalization is a
// silent no-op. These cases only prove anything when they run against the real container.
const equivalentQueries = [
  { accented: "saúde", plain: "saude" },
  { accented: "SAÚDE", plain: "saude" },
  { accented: "educação", plain: "educacao" },
  { accented: "licitações", plain: "licitacoes" },
];

test("busca trata consulta acentuada e sem acento como o mesmo termo", async ({ request }) => {
  for (const { accented, plain } of equivalentQueries) {
    const [accentedResponse, plainResponse] = await Promise.all([
      request.get(`/api/v1/search?q=${encodeURIComponent(accented)}`),
      request.get(`/api/v1/search?q=${encodeURIComponent(plain)}`),
    ]);
    expect(accentedResponse.ok(), accented).toBeTruthy();
    expect(plainResponse.ok(), plain).toBeTruthy();
    const accentedResults = (await accentedResponse.json() as { results: unknown[] }).results;
    const plainResults = (await plainResponse.json() as { results: unknown[] }).results;
    expect(plainResults.length, `${plain} deveria encontrar conteúdo municipal`).toBeGreaterThan(0);
    expect(accentedResults.length, `${accented} deveria encontrar o mesmo que ${plain}`).toBe(plainResults.length);
  }
});

test("busca pública encontra serviço por termo acentuado e por erro de digitação", async ({ page }) => {
  await page.goto(`/buscar?q=${encodeURIComponent("saúde")}`);
  await expect(page.getByRole("heading", { level: 1, name: /Encontre serviços e informações/i })).toBeVisible();
  await expect(page.getByRole("link", { name: /Secretaria Municipal de Saúde/i })).toBeVisible();
  await expect(page.getByText("Nenhum resultado")).toHaveCount(0);

  await page.goto("/buscar?q=matricla");
  await expect(page.getByRole("link", { name: /Matrícula/i }).first()).toBeVisible();

  await page.goto("/buscar?q=zzzzqqqqwwww");
  await expect(page.getByRole("heading", { name: "Nenhum resultado" })).toBeVisible();
});
