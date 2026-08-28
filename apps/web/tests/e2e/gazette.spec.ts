import { expect, test } from "@playwright/test";

test("Diário Oficial entrega PDF, hash, QR e verificação pública da edição publicada", async ({ page, request }) => {
  test.setTimeout(90_000);
  const password = process.env.DEMO_PASSWORD;
  if (!password) throw new Error("DEMO_PASSWORD é obrigatório para o teste governado do Diário.");
  const edition = Number(Date.now().toString().slice(-5));

  await page.goto("/admin/login");
  await page.getByLabel("Usuário").fill("admin.demo");
  await page.getByLabel("Senha").fill(password);
  await page.getByRole("button", { name: "Entrar" }).click();
  await page.waitForURL(/\/admin$/);

  await page.goto("/admin/diario");
  await page.getByLabel("Número").fill(String(edition));
  await page.getByRole("button", { name: "Criar edição" }).click();
  await expect(page.getByText(/Edição criada em DRAFT/)).toBeVisible();
  await page.getByRole("button", { name: "Salvar composição" }).click();
  await expect(page.getByText(/Composição persistida/)).toBeVisible();
  await page.getByRole("button", { name: "Revisão" }).click();
  await page.getByRole("button", { name: "Aprovar" }).click();
  await page.getByRole("button", { name: "Gerar PDF" }).click();
  await expect(page.getByText(/Ação generate concluída/)).toBeVisible();
  await page.getByRole("button", { name: "Assinar" }).click();
  await page.getByRole("button", { name: "Publicar" }).click();
  const verificationLink = page.getByRole("link", { name: /Abrir verificação pública/ });
  await expect(verificationLink).toBeVisible();
  // A edição corrente só aceita correção depois de publicada: é o sinal preciso desta edição.
  await expect(page.getByLabel("Número da nova edição")).toBeVisible();
  const verificationHref = await verificationLink.getAttribute("href");
  const verificationCode = verificationHref?.split("/").pop();
  expect(verificationCode, "código de verificação emitido na publicação").toBeTruthy();

  const editions = await page.request.get("/api/v1/admin/gazette");
  const published = (await editions.json() as Array<{ id: string; number: number; status: string; sha256: string | null; verificationCode: string | null }>)
    .find((item) => item.number === edition);
  expect(published?.status).toBe("PUBLISHED");
  expect(published?.sha256, "a edição publicada precisa registrar SHA-256").toMatch(/^[0-9a-f]{64}$/);
  expect(published?.verificationCode).toBe(verificationCode);

  // O artefato precisa ser realmente entregue: metadado sem PDF não é publicação verificável.
  const document = await request.get(`/api/v1/gazette/${published!.id}/document`);
  expect(document.status(), "o PDF da edição publicada precisa ser baixável").toBe(200);
  expect(document.headers()["content-type"]).toContain("application/pdf");
  const pdf = await document.body();
  expect(pdf.length).toBeGreaterThan(400);
  expect(pdf.subarray(0, 5).toString("latin1")).toBe("%PDF-");

  const integrity = await request.get(`/api/v1/gazette/${published!.id}/integrity`);
  expect(integrity.ok()).toBeTruthy();
  await expect(integrity.json()).resolves.toMatchObject({ edition: { sha256: published!.sha256, status: "PUBLISHED" } });

  const qr = await request.get(`/api/v1/gazette/verify/${verificationCode}/qr.svg`);
  expect(qr.status()).toBe(200);
  expect(qr.headers()["content-type"]).toContain("image/svg+xml");

  await page.goto(verificationHref!);
  await expect(page.getByText("Documento localizado")).toBeVisible();
  await expect(page.getByText(published!.sha256!)).toBeVisible();
  // A assinatura de demonstração nunca pode ser apresentada como ICP-Brasil.
  await expect(page.getByText(/NÃO ICP|Sem certificado registrado/)).toBeVisible();

  const unknown = await request.get("/api/v1/gazette/verify/codigo-inexistente/qr.svg");
  expect(unknown.status()).toBe(404);
});
