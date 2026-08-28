import { expect, test } from "@playwright/test";

test("Ouvidoria: cidadão registra, servidor responde e o acompanhamento público mostra apenas o que é público", async ({ page }) => {
  test.setTimeout(60_000);
  const password = process.env.DEMO_PASSWORD;
  if (!password) throw new Error("DEMO_PASSWORD é obrigatório para o teste governado da Ouvidoria.");
  const suffix = Date.now().toString().slice(-8);

  await page.goto("/ouvidoria");
  await page.getByLabel("Nome").fill(`Cidadã Ouvidoria ${suffix}`);
  await page.getByLabel("E-mail ou telefone").fill(`ouvidoria-${suffix}@example.test`);
  await page.getByLabel("Tipo").selectOption("Reclamação");
  await page.getByLabel("Descrição").fill(`Manifestação sintética ${suffix} para validar protocolo, SLA e acompanhamento público da Ouvidoria.`);
  await page.getByRole("checkbox").check();
  await page.getByRole("button", { name: "Registrar manifestação" }).click();

  const confirmation = page.locator(".form-message.success");
  await expect(confirmation).toContainText("Manifestação registrada");
  const confirmationText = await confirmation.innerText();
  const protocol = /DEO-\d{8}-[0-9A-F]{8}/.exec(confirmationText)?.[0];
  const trackingCode = /Código de acompanhamento:\s*([0-9a-f]{32})/.exec(confirmationText)?.[1];
  expect(protocol, "protocolo emitido no registro").toBeTruthy();
  expect(trackingCode, "código de acompanhamento emitido no registro").toBeTruthy();

  await confirmation.getByRole("link", { name: "Acompanhar manifestação" }).click();
  await expect(page.getByRole("heading", { name: "Acompanhar manifestação" })).toBeVisible();
  await page.getByLabel("Protocolo").fill(protocol!);
  await page.getByLabel("Código de acompanhamento").fill("00000000000000000000000000000000");
  await page.getByRole("button", { name: "Consultar manifestação" }).click();
  // A consulta cruza portal e API; o cenário verifica o comportamento, não a latência de uma execução.
  await expect(page.locator(".form-message.error")).toContainText("Nenhuma manifestação corresponde", { timeout: 25_000 });

  await page.getByLabel("Código de acompanhamento").fill(trackingCode!);
  await page.getByRole("button", { name: "Consultar manifestação" }).click();
  await expect(page.getByRole("heading", { name: `Manifestação ${protocol}` })).toBeVisible({ timeout: 25_000 });
  await expect(page.getByText("Aberta — aguardando primeira resposta")).toBeVisible();
  await expect(page.getByText("Ainda não há resposta pública registrada para esta manifestação.")).toBeVisible();

  await page.goto("/admin/login");
  await page.getByLabel("Usuário").fill("admin.demo");
  await page.getByLabel("Senha").fill(password);
  await page.getByRole("button", { name: "Entrar" }).click();
  await page.waitForURL(/\/admin$/);

  await page.goto("/admin/tickets");
  const row = page.getByRole("row").filter({ hasText: protocol! });
  await row.getByRole("button", { name: "Atender" }).click();
  const workspace = page.getByRole("region", { name: "Atendimento do ticket selecionado" });
  await expect(workspace.getByText(`Manifestação sintética ${suffix}`)).toBeVisible();
  await expect(workspace.getByText(`ouvidoria-${suffix}@example.test`)).toBeVisible();

  const internalNote = `Triagem interna ${suffix} sem divulgação ao cidadão.`;
  await workspace.getByLabel("Texto da resposta").fill(internalNote);
  await workspace.getByLabel(/Nota interna/).check();
  await workspace.getByRole("button", { name: "Registrar" }).click();
  await expect(page.getByText("Nota interna registrada; ela não aparece no acompanhamento do cidadão.")).toBeVisible();

  const publicAnswer = `Resposta oficial ${suffix} publicada no acompanhamento.`;
  await workspace.getByLabel("Texto da resposta").fill(publicAnswer);
  await workspace.getByRole("button", { name: "Registrar" }).click();
  await expect(page.getByText("Resposta publicada no acompanhamento do cidadão.")).toBeVisible();
  await expect(workspace.getByText("Nota interna", { exact: true })).toBeVisible();
  await expect(workspace.getByText("Resposta ao cidadão", { exact: true })).toBeVisible();

  await workspace.getByLabel(`Prioridade de ${protocol}`).selectOption("CRITICAL");
  await expect(page.getByText("Prioridade alterada e prazos de SLA recalculados.")).toBeVisible();
  await workspace.getByRole("button", { name: "Resolver" }).click();
  await expect(page.getByText("Ticket resolvido e prazo de conclusão cumprido.")).toBeVisible();

  await page.goto("/ouvidoria/acompanhar");
  await page.getByLabel("Protocolo").fill(protocol!);
  await page.getByLabel("Código de acompanhamento").fill(trackingCode!);
  await page.getByRole("button", { name: "Consultar manifestação" }).click();
  await expect(page.getByText("Respondida e encerrada")).toBeVisible({ timeout: 25_000 });
  await expect(page.getByText(publicAnswer)).toBeVisible();
  await expect(page.getByText(internalNote)).toHaveCount(0);
});
