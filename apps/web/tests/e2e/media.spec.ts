import { expect, test } from "@playwright/test";

const onePixelPng = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=";

test("biblioteca de mídia governa upload, enquadramento visual e reuso público", async ({ page }) => {
  test.setTimeout(90_000);
  const password = process.env.DEMO_PASSWORD;
  if (!password) throw new Error("DEMO_PASSWORD é obrigatório para o teste governado de mídia.");
  const suffix = Date.now().toString().slice(-8);
  const fileName = `enquadramento-${suffix}.png`;
  const altText = `Imagem sintética de enquadramento ${suffix}`;

  await page.goto("/admin/login");
  await page.getByLabel("Usuário").fill("admin.demo");
  await page.getByLabel("Senha").fill(password);
  await page.getByRole("button", { name: "Entrar" }).click();
  await page.waitForURL(/\/admin$/);

  await page.goto("/admin/midia");
  await page.getByLabel("Arquivos").setInputFiles({ name: fileName, mimeType: "image/png", buffer: Buffer.from(onePixelPng, "base64") });
  await page.getByLabel("Texto alternativo comum").fill(altText);
  await page.getByRole("button", { name: "Enviar 1 arquivo" }).click();
  await expect(page.getByText(/1 arquivo recebido/)).toBeVisible();

  const uploaded = page.locator(".compact-item").filter({ hasText: fileName }).first();
  await uploaded.getByRole("button", { name: "Revisar" }).click();
  await expect(page.getByRole("heading", { name: "Revisão da mídia" })).toBeVisible();

  const approve = page.getByRole("button", { name: "Revalidar com scanner e aprovar" });
  if (await approve.count() > 0) {
    await approve.click();
    await expect(page.getByText(/aprovado para uso público/)).toBeVisible();
  }

  await expect(page.getByRole("heading", { name: "Enquadramento editorial" })).toBeVisible();
  const framing = page.getByRole("status").filter({ hasText: "Ponto focal em" });
  await expect(framing).toContainText("sem recorte editorial");

  await page.getByLabel(/Ponto focal horizontal/).fill("70");
  await page.getByLabel(/Ponto focal vertical/).fill("30");
  await expect(framing).toContainText("Ponto focal em 70% × 30%");

  await page.getByLabel("Definir recorte editorial normalizado").check();
  await page.getByLabel("X (%)").fill("10");
  await page.getByLabel("Y (%)").fill("20");
  await page.getByLabel("Largura (%)").fill("60");
  await page.getByLabel("Altura (%)").fill("50");
  await expect(framing).toContainText("recorte 60% × 50% a partir de 10% × 20%");

  await page.getByRole("button", { name: "Salvar enquadramento" }).click();
  await expect(page.getByText("Tags, ponto focal e recorte governado atualizados.")).toBeVisible();

  // The governed framing must survive a reload, and the original file must stay readable.
  await page.reload();
  const reloaded = page.locator(".compact-item").filter({ hasText: fileName }).first();
  await reloaded.getByRole("button", { name: "Revisar" }).click();
  await expect(page.getByRole("status").filter({ hasText: "Ponto focal em" })).toContainText("Ponto focal em 70% × 30%");

  const originalResponse = await page.request.get(`/api/v1/media/${await currentAssetId(page)}`);
  expect(originalResponse.status()).toBe(200);
  expect(originalResponse.headers()["content-type"]).toContain("image/png");
});

async function currentAssetId(page: import("@playwright/test").Page) {
  const response = await page.request.get("/api/v1/admin/media?status=APPROVED&pageSize=1");
  const assets = await response.json() as Array<{ id: string }>;
  return assets[0].id;
}
