import { expect, test } from "@playwright/test";

test("redirect legado só aceita destino interno e nunca leva o cidadão para outro host", async ({ page, request }) => {
  test.setTimeout(60_000);
  const password = process.env.DEMO_PASSWORD;
  if (!password) throw new Error("DEMO_PASSWORD é obrigatório para o teste governado de redirects.");
  const suffix = Date.now().toString().slice(-8);

  await page.goto("/admin/login");
  await page.getByLabel("Usuário").fill("admin.demo");
  await page.getByLabel("Senha").fill(password);
  await page.getByRole("button", { name: "Entrar" }).click();
  await page.waitForURL(/\/admin$/);

  for (const destination of ["//example.test/evil", "https://example.test/evil", "/\\example.test/evil", "servicos"]) {
    const refused = await page.request.post("/api/v1/admin/redirects", {
      data: { legacyUrl: `/portal-antigo-hostil-${suffix}`, destinationPath: destination, permanent: true },
    });
    expect(refused.status(), `destino externo aceito: ${destination}`).toBe(400);
  }

  const legacyPath = `/portal-antigo-${suffix}`;
  const created = await page.request.post("/api/v1/admin/redirects", {
    data: { legacyUrl: legacyPath, destinationPath: "/servicos/emitir-guia-iptu", permanent: true },
  });
  expect(created.status()).toBe(201);
  const body = await created.json() as Record<string, unknown>;
  expect(body.municipalityId, "a resposta não pode expor o identificador do tenant").toBeUndefined();

  const resolved = await request.get(`/api/v1/legacy/resolve?url=${encodeURIComponent(legacyPath)}`);
  expect(resolved.ok()).toBeTruthy();
  await expect(resolved.json()).resolves.toMatchObject({ source: legacyPath, destination: "/servicos/emitir-guia-iptu", statusCode: 301 });

  const followed = await request.get(`/api/v1/legacy/resolve?url=${encodeURIComponent(`${legacyPath}?utm_source=facebook`)}`);
  expect(followed.ok(), "parâmetros de rastreamento não podem quebrar o redirect").toBeTruthy();

  const selfReference = await page.request.post("/api/v1/admin/redirects", {
    data: { legacyUrl: `/laco-${suffix}`, destinationPath: `/laco-${suffix}`, permanent: true },
  });
  expect(selfReference.status(), "um redirect não pode apontar para si mesmo").toBe(400);

  // A → B precisa impedir B → A: o navegador entraria em um laço de 301.
  const forward = await page.request.post("/api/v1/admin/redirects", {
    data: { legacyUrl: `/ciclo-a-${suffix}`, destinationPath: `/ciclo-b-${suffix}`, permanent: true },
  });
  expect(forward.status()).toBe(201);
  const backward = await page.request.post("/api/v1/admin/redirects", {
    data: { legacyUrl: `/ciclo-b-${suffix}`, destinationPath: `/ciclo-a-${suffix}`, permanent: true },
  });
  expect(backward.status(), "um ciclo de redirects não pode ser registrado").toBe(400);

  const duplicated = await page.request.post("/api/v1/admin/redirects", {
    data: { legacyUrl: legacyPath, destinationPath: "/noticias", permanent: true },
  });
  expect(duplicated.status(), "a mesma URL legada não pode ser mapeada duas vezes").toBe(409);
});
