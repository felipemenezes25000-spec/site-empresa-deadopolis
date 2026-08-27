import { expect, test } from "@playwright/test";

test("POC principal: cidadão, CMS, Dados Abertos, Migração, E-mail, Operações, Diário e Ouvidoria", async ({ page }) => {
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

  const newsSlug = `poc-noticia-${suffix}`;
  await page.goto("/admin/noticias/nova");
  await page.getByLabel("Título").fill(`[DEMONSTRAÇÃO] Notícia POC ${suffix}`);
  await page.getByLabel("Slug").fill(newsSlug);
  await page.getByLabel("Linha fina").fill("Publicação sintética criada pelo teste E2E.");
  await page.getByLabel("Conteúdo").fill("Conteúdo de demonstração sem valor de comunicado oficial. Este texto comprova persistência e workflow editorial.");
  await page.getByRole("button", { name: "Criar rascunho" }).click();
  await expect(page.getByText(/Rascunho salvo no servidor/)).toBeVisible();
  await page.getByRole("button", { name: "Enviar para revisão" }).click();
  await expect(page.getByText(/Ação “submit” concluída/)).toBeVisible();
  await page.getByRole("button", { name: "Aprovar" }).click();
  await expect(page.getByText(/Ação “approve” concluída/)).toBeVisible();
  await page.getByRole("button", { name: "Publicar agora" }).click();
  await expect(page.getByText(/Ação “publish” concluída/)).toBeVisible();
  await page.goto(`/noticias/${newsSlug}`);
  await expect(page.getByRole("heading", { name: new RegExp(`Notícia POC ${suffix}`) })).toBeVisible();

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
