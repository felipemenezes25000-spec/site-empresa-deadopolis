import { expect, test } from "@playwright/test";

test("POC principal: cidadão, RBAC, CMS, Dados Abertos, Migração, E-mail, Operações, Diário e Ouvidoria", async ({ page }) => {
  test.setTimeout(120_000);
  const password = process.env.DEMO_PASSWORD;
  if (!password) throw new Error("DEMO_PASSWORD é obrigatório para a POC automatizada.");

  await page.goto("/");
  await expect(page.getByRole("heading", { name: /Olá! O que você precisa/i })).toBeVisible();
  await page.goto("/servicos");
  await page.getByRole("link", { name: /Emitir guia do IPTU/i }).click();
  await expect(page.getByRole("heading", { name: /Emitir guia do IPTU/i })).toBeVisible();

  await page.goto("/admin/login");
  await page.getByLabel("Usuário").fill("admin.demo");
  await page.getByLabel("Senha").fill(password);
  await page.getByRole("button", { name: "Entrar" }).click();
  await page.waitForURL(/\/admin$/);
  await expect(page.getByRole("heading", { name: /Bom dia/i })).toBeVisible();

  const suffix = Date.now().toString().slice(-8);

  const managedUsername = `comunicacao.${suffix}`;
  await page.goto("/admin/usuarios");
  await expect(page.getByRole("heading", { name: "Usuários e RBAC" })).toBeVisible();
  await expect(page.getByText("users.manage", { exact: true })).toBeVisible();
  await page.getByLabel("Usuário").fill(managedUsername);
  await page.getByLabel("Nome de exibição").fill(`Comunicação POC ${suffix}`);
  await page.getByLabel("Papel RBAC").selectOption("COMMUNICATION");
  await page.getByLabel(/^Senha temporária/).fill(`Temporary-${suffix}-Aa!`);
  await page.getByRole("button", { name: "Criar usuário" }).click();
  await expect(page.getByText(/Usuário criado/)).toBeVisible();
  await expect(page.getByText(new RegExp(managedUsername))).toBeVisible();
  const currentAccount = page.getByRole("article", { name: /admin\.demo.*sessão atual/i });
  await expect(currentAccount.getByRole("button", { name: "Salvar papel" })).toBeDisabled();
  await expect(currentAccount.getByRole("button", { name: "Revogar sessões" })).toBeDisabled();
  await expect(currentAccount.getByRole("button", { name: "Desativar" })).toBeDisabled();
  const managedAccount = page.getByRole("article", { name: new RegExp(managedUsername) });
  await managedAccount.getByLabel(`Papel de ${managedUsername}`).selectOption("SUPER_ADMIN");
  await managedAccount.getByRole("button", { name: "Salvar papel" }).click();
  await expect(page.getByText("Papel atualizado e sessões anteriores revogadas.")).toBeVisible();
  await managedAccount.getByRole("button", { name: "Revogar sessões" }).click();
  await expect(page.getByText(`Sessões de ${managedUsername} revogadas.`)).toBeVisible();
  await managedAccount.getByRole("button", { name: "Desativar" }).click();
  await expect(page.getByText("Conta desativada e sessões revogadas.")).toBeVisible();
  await expect(managedAccount.getByText("INATIVO", { exact: true })).toBeVisible();
  const auditResponse = await page.request.get("/api/v1/admin/audit");
  expect(auditResponse.ok()).toBeTruthy();
  const auditEvents = await auditResponse.json() as Array<{ action: string; resourceId: string }>;
  const managedAuditActions = auditEvents.filter((event) => event.resourceId === managedUsername || event.action.startsWith("identity.user.")).map((event) => event.action);
  expect(managedAuditActions).toEqual(expect.arrayContaining(["identity.user.created", "identity.user.role.assigned", "identity.user.sessions.revoked", "identity.user.state.changed"]));

  const coverFileName = `capa-poc-${suffix}.png`;
  const coverAlt = `Imagem sintética de capa da POC ${suffix}`;
  await page.goto("/admin/midia");
  await page.getByLabel("Arquivos").setInputFiles({
    name: coverFileName,
    mimeType: "image/png",
    buffer: Buffer.from("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=", "base64"),
  });
  await page.getByLabel("Texto alternativo comum").fill(coverAlt);
  await page.getByRole("button", { name: "Enviar 1 arquivo" }).click();
  await expect(page.getByText(/1 arquivo recebido/)).toBeVisible();
  await expect(page.getByText(coverFileName, { exact: true }).first()).toBeVisible();

  const cmsSummary = `[DEMONSTRAÇÃO] Conteúdo CMS atualizado pelo E2E ${suffix}.`;
  const cmsStartsAt = new Date(Date.now() - 24 * 60 * 60 * 1000).toISOString().slice(0, 16);
  const cmsEndsAt = new Date(Date.now() + 24 * 60 * 60 * 1000).toISOString().slice(0, 16);
  await page.goto("/admin/conteudo");
  await page.getByRole("button", { name: "Editar Acesso à Informação" }).click();
  await expect(page.getByRole("heading", { name: "Editar conteúdo" })).toBeVisible();
  await page.getByLabel("Resumo").fill(cmsSummary);
  await page.getByLabel("Início de exibição").fill(cmsStartsAt);
  await page.getByLabel("Fim de exibição").fill(cmsEndsAt);
  await page.getByRole("button", { name: "Salvar alterações" }).click();
  await expect(page.getByText("Alterações salvas com nova versão.")).toBeVisible();
  await expect(page.getByRole("heading", { name: "Histórico de revisões" })).toBeVisible();
  await expect(page.getByText(/Versão 1/).first()).toBeVisible();
  await page.goto("/acesso-a-informacao");
  await expect(page.getByText(cmsSummary, { exact: true })).toBeVisible();

  await page.goto("/admin/conteudo");
  await expect(page.getByRole("button", { name: "Editar Acesso à Informação" })).toBeVisible();
  const existingHome = page.locator(".compact-item").filter({ has: page.locator("small", { hasText: /^home · v\d+/ }) });
  const homeAlreadyGoverned = await existingHome.count() > 0;
  if (homeAlreadyGoverned) {
    await existingHome.first().getByRole("button", { name: /^Editar / }).click();
    await expect(page.getByRole("heading", { name: "Editar conteúdo" })).toBeVisible();
  }
  const homeForm = page.getByRole("heading", { name: homeAlreadyGoverned ? "Editar conteúdo" : "Novo conteúdo" }).locator("xpath=ancestor::form");
  await homeForm.getByRole("textbox", { name: "Título", exact: true }).fill(`Página inicial POC ${suffix}`);
  if (!homeAlreadyGoverned) await homeForm.getByRole("textbox", { name: "Slug", exact: true }).fill("home");
  await homeForm.getByRole("textbox", { name: "Resumo", exact: true }).fill("Composição governada da página inicial criada pelo teste executivo.");
  const homeBuilder = homeForm.getByRole("region", { name: "Page Builder" });
  for (let remaining = await homeBuilder.getByRole("article").count(); remaining > 0; remaining--) {
    await homeBuilder.getByRole("article").first().getByRole("button", { name: /^Remover bloco / }).click();
  }
  await expect(homeBuilder.getByRole("article")).toHaveCount(0);
  await homeBuilder.getByLabel("Tipo do novo bloco").selectOption("Hero");
  await homeBuilder.getByRole("button", { name: "Adicionar bloco" }).click();
  const heroBlock = homeBuilder.getByRole("article").nth(0);
  await heroBlock.getByLabel("Título do bloco 1").fill("Olá! O que você precisa?");
  await heroBlock.getByLabel("Conteúdo do bloco 1").fill("Composição administrada e publicada pelo CMS municipal.");
  await homeBuilder.getByLabel("Tipo do novo bloco").selectOption("Alert");
  await homeBuilder.getByRole("button", { name: "Adicionar bloco" }).click();
  const alertBlock = homeBuilder.getByRole("article").nth(1);
  await alertBlock.getByLabel("Título do bloco 2").fill(`Aviso governado POC ${suffix}`);
  await alertBlock.getByLabel("Conteúdo do bloco 2").fill("Este aviso comprova a renderização pública da composição estruturada.");
  await homeForm.getByRole("button", { name: homeAlreadyGoverned ? "Salvar alterações" : "Salvar rascunho" }).click();
  await expect(page.getByText(homeAlreadyGoverned ? "Alterações salvas com nova versão." : "Conteúdo criado como rascunho.")).toBeVisible();
  const homeResource = page.getByText(`Página inicial POC ${suffix}`, { exact: true }).locator("xpath=ancestor::div[contains(@class, 'compact-item')]");
  const publishHome = homeResource.getByRole("button", { name: "Publicar" });
  if (await publishHome.count() > 0) {
    await publishHome.click();
    await expect(page.getByText("Ação publish concluída.")).toBeVisible();
  }
  await expect(homeResource.getByText("PUBLISHED", { exact: true })).toBeVisible();
  await page.goto("/");
  await expect(page.getByRole("heading", { level: 1, name: "Olá! O que você precisa?" })).toBeVisible();
  await expect(page.getByRole("heading", { name: `Aviso governado POC ${suffix}` })).toBeVisible();

  const newsSlug = `poc-noticia-${suffix}`;
  await page.goto("/admin/noticias/nova");
  await page.getByLabel("Título").fill(`[DEMONSTRAÇÃO] Notícia POC ${suffix}`);
  await page.getByLabel("Slug").fill(newsSlug);
  await page.getByLabel("Linha fina").fill("Publicação sintética criada pelo teste E2E.");
  await page.getByLabel("Área editorial").selectOption("PREFEITURA");
  await page.getByLabel("Conteúdo").fill("Conteúdo de demonstração sem valor de comunicado oficial. Este texto comprova persistência e workflow editorial.");
  await page.getByText("Selecionar capa da biblioteca", { exact: true }).click();
  await page.getByRole("button", { name: new RegExp(coverFileName) }).click();
  await expect(page.getByText("Capa selecionada", { exact: true })).toBeVisible();
  await expect(page.getByLabel("Texto alternativo")).toHaveValue(coverAlt);
  await page.getByRole("button", { name: "Criar rascunho" }).click();
  await expect(page.getByText(/Rascunho salvo no servidor/)).toBeVisible();
  await page.goto("/admin/comunicacao");
  const newsRow = page.getByRole("row").filter({ hasText: `Notícia POC ${suffix}` });
  await newsRow.getByRole("link", { name: "Editar" }).click();
  await expect(page.getByRole("heading", { name: "Editar notícia" }).first()).toBeVisible();
  await page.getByLabel("Linha fina").fill("Publicação sintética revisada pela Comunicação no teste E2E.");
  await page.getByRole("button", { name: "Salvar alterações" }).click();
  await expect(page.getByText(/Alterações salvas como versão/)).toBeVisible();
  await page.getByRole("button", { name: "Enviar para revisão" }).click();
  await expect(page.getByText(/Ação “submit” concluída/)).toBeVisible();
  await page.getByRole("button", { name: "Aprovar" }).click();
  await expect(page.getByText(/Ação “approve” concluída/)).toBeVisible();
  await page.getByRole("button", { name: "Publicar agora" }).click();
  await expect(page.getByText(/Ação “publish” concluída/)).toBeVisible();
  await page.goto(`/noticias/${newsSlug}`);
  await expect(page.getByRole("heading", { name: new RegExp(`Notícia POC ${suffix}`) })).toBeVisible();
  await expect(page.getByRole("img", { name: coverAlt })).toBeVisible();

  const datasetSlug = `poc-dataset-${suffix}`;
  await page.goto("/admin/dados-abertos");
  await page.getByLabel("Título do dataset").fill(`[DEMONSTRAÇÃO] Dataset POC ${suffix}`);
  await page.getByLabel("Slug do dataset").fill(datasetSlug);
  await page.getByLabel("Descrição").first().fill("Base sintética para validar versionamento e publicação de Dados Abertos.");
  await page.getByLabel("Categoria").first().fill("Demonstração");
  await page.getByLabel("Órgão responsável").first().fill("Secretaria Municipal de Administração");
  await page.getByLabel("Periodicidade de atualização").first().fill("Mensal");
  await page.getByRole("button", { name: "Criar dataset" }).click();
  await expect(page.getByText("Dataset criado como rascunho.")).toBeVisible();
  await page.getByLabel("Arquivo da versão").setInputFiles({
    name: `dataset-${suffix}.json`,
    mimeType: "application/json",
    buffer: Buffer.from(JSON.stringify({ demonstration: true, suffix, municipality: "Deodápolis" })),
  });
  await page.getByRole("button", { name: "Adicionar versão" }).click();
  await expect(page.getByText(/Nova versão armazenada e registrada com hash SHA-256/)).toBeVisible();
  await page.getByRole("button", { name: "Publicar dataset" }).click();
  await expect(page.getByText("Dataset publicado no catálogo público.")).toBeVisible();
  await page.goto(`/dados-abertos/${datasetSlug}`);
  await expect(page.getByRole("heading", { name: new RegExp(`Dataset POC ${suffix}`) })).toBeVisible();
  await expect(page.getByText(/Versão 1/)).toBeVisible();

  await page.goto("/admin/migracao");
  await page.getByLabel("URL inicial").fill("http://127.0.0.1/");
  await page.getByLabel("Profundidade máxima").fill("0");
  await page.getByLabel("Máximo de páginas").fill("1");
  await page.getByRole("button", { name: "Criar job de dry-run" }).click();
  await expect(page.getByText(/Job criado para o host autorizado 127\.0\.0\.1/)).toBeVisible();
  await page.getByRole("button", { name: "Executar dry-run seguro" }).click();
  await expect(page.getByText(/Dry-run concluído: 1 URL\(s\), 0 documento\(s\), 1 falha\(s\) e 0 item\(ns\) pendente\(s\) na fila/)).toBeVisible();
  await expect(page.getByText(/Bloqueio\/falha: Host resolveu para endereço privado, local ou reservado/i)).toBeVisible();

  const legacyPath = `/portal-antigo-${suffix}`;
  const destinationPath = `/dados-abertos/${datasetSlug}`;
  await page.getByLabel("URL ou caminho legado").fill(legacyPath);
  await page.getByLabel("Destino interno").fill(destinationPath);
  await page.getByRole("button", { name: "Adicionar redirect" }).click();
  await expect(page.getByText("Redirect legado registrado e auditado.")).toBeVisible();
  const resolved = await page.request.get(`/api/v1/legacy/resolve?url=${encodeURIComponent(legacyPath)}`);
  expect(resolved.ok()).toBeTruthy();
  await expect(resolved.json()).resolves.toMatchObject({ source: legacyPath, destination: destinationPath, statusCode: 301 });

  const mailDomain = `poc-${suffix}.deodapolis.ms.gov.br`;
  const mailboxAddress = `contato-${suffix}@${mailDomain}`;
  const aliasAddress = `ouvidoria-${suffix}@${mailDomain}`;
  await page.goto("/admin/email");
  await expect(page.getByText(/Provider: DEMO_ONLY/)).toBeVisible();
  await page.getByLabel("Domínio institucional").fill(mailDomain);
  await page.getByRole("button", { name: "Cadastrar domínio" }).click();
  await expect(page.getByText(/Domínio institucional cadastrado/)).toBeVisible();
  await expect(page.getByText(mailDomain, { exact: true })).toBeVisible();
  await page.getByLabel("Endereço da caixa").fill(mailboxAddress);
  await page.getByLabel("Nome de exibição").fill(`Caixa POC ${suffix}`);
  await page.getByLabel("Quota (MB)").fill("2048");
  await page.getByRole("button", { name: "Solicitar caixa" }).click();
  await expect(page.getByText(/Estado do provider: DEMO_ONLY/)).toBeVisible();
  await expect(page.getByText(mailboxAddress, { exact: true }).first()).toBeVisible();
  await page.getByLabel("Endereço do alias").fill(aliasAddress);
  await page.getByLabel("Destino do alias").fill(mailboxAddress);
  await page.getByRole("button", { name: "Cadastrar alias" }).click();
  await expect(page.getByText("Alias institucional cadastrado e auditado.")).toBeVisible();
  await expect(page.getByText(aliasAddress, { exact: true })).toBeVisible();
  await page.getByLabel("Tipo de origem").selectOption("EML");
  await page.getByLabel("Referência da origem").fill(`lote-poc-${suffix}`);
  await page.getByLabel("Caixa de destino").fill(mailboxAddress);
  await page.getByRole("button", { name: "Registrar migração" }).click();
  await expect(page.getByText(/Pedido de migração registrado/)).toBeVisible();
  await expect(page.getByText(`EML → ${mailboxAddress}`, { exact: true })).toBeVisible();

  await page.goto("/admin/operacoes");
  await page.getByLabel("URL monitorada").fill("http://127.0.0.1/");
  await page.getByRole("button", { name: "Adicionar monitoramento" }).click();
  await expect(page.getByText(/IP privado, local ou reservado.*SSRF/i)).toBeVisible();
  const monitoredUrl = `https://unresolved-${suffix}.invalid/health`;
  await page.getByLabel("URL monitorada").fill(monitoredUrl);
  await page.getByRole("button", { name: "Adicionar monitoramento" }).click();
  await expect(page.getByText("URL adicionada ao monitoramento periódico e auditada.")).toBeVisible();
  await expect(page.getByText(monitoredUrl, { exact: true })).toBeVisible();
  await page.getByRole("button", { name: `Verificar ${monitoredUrl}` }).click();
  await expect(page.getByText("Verificação concluída: DEGRADED.")).toBeVisible();
  await expect(page.getByText("DEGRADED", { exact: true }).first()).toBeVisible();

  const startedAt = new Date(Date.now() - 60_000).toISOString().slice(0, 16);
  const completedAt = new Date().toISOString().slice(0, 16);
  const backupProvider = `POC Provider ${suffix}`;
  await page.getByLabel("Provider do backup").fill(backupProvider);
  await page.getByLabel("Tipo de backup").fill("DATABASE_FULL");
  await page.getByLabel("Início").fill(startedAt);
  await page.getByLabel("Conclusão").fill(completedAt);
  await page.getByLabel("Referência do artefato").fill(`backup://poc/${suffix}`);
  await page.getByLabel("Tamanho (bytes)").fill("4096");
  await page.getByLabel("Restore testado em").fill(completedAt);
  await page.getByRole("button", { name: "Registrar evidência" }).click();
  await expect(page.getByText(/Evidência de backup registrada/)).toBeVisible();
  await expect(page.getByText(new RegExp(`^${backupProvider} · DATABASE_FULL$`))).toBeVisible();

  await page.goto("/admin/compliance");
  const runtimeSection = page.getByRole("heading", { name: "Capacidades do runtime" }).locator("xpath=ancestor::section");
  await expect(runtimeSection.getByText("WebP", { exact: true })).toBeVisible();
  await expect(runtimeSection.getByText("AVAILABLE", { exact: true }).first()).toBeVisible();
  await expect(page.getByRole("heading", { name: "Dependências externas para produção" })).toBeVisible();

  await page.goto("/admin/diario");
  const edition = Number(suffix.slice(-5));
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
  await expect(page.getByText(/NÃO possui valor de assinatura ICP-Brasil|Ação sign concluída/)).toBeVisible();
  await page.getByRole("button", { name: "Publicar" }).click();
  const verificationLink = page.getByRole("link", { name: /Abrir verificação pública/ });
  await expect(verificationLink).toBeVisible();
  const href = await verificationLink.getAttribute("href");
  expect(href).toMatch(/^\/verificar\//);
  await page.getByLabel("Número da nova edição").fill(String(edition + 1));
  await page.getByLabel("Justificativa").fill("Correção sintética vinculada pelo E2E, sem sobrescrever a edição originalmente publicada.");
  await page.getByRole("button", { name: "Criar correção" }).click();
  await expect(page.getByText(/A edição original permaneceu imutável/)).toBeVisible();
  await page.goto(href!);
  await expect(page.getByText("Documento localizado")).toBeVisible();

  await page.goto("/ouvidoria");
  await page.getByLabel("Nome").fill("Pessoa Demonstração");
  await page.getByLabel("E-mail ou telefone").fill("poc@example.test");
  await page.getByLabel("Descrição").fill("Solicitação sintética criada pelo teste automatizado para validar protocolo e acompanhamento.");
  await page.getByRole("checkbox").check();
  await page.getByRole("button", { name: "Registrar manifestação" }).click();
  await expect(page.getByText("Manifestação registrada")).toBeVisible();
  await expect(page.getByText(/DEO-/)).toBeVisible();
});
