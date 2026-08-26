"use client";

import { useEffect, useState, type FormEvent } from "react";

type Provider = { state: string; description: string };
type MailDomain = { id: string; domain: string; state: string; externalId: string | null; createdAt: string; updatedAt: string };
type Mailbox = { id: string; address: string; displayName: string; quotaMegabytes: number; status: string; externalId: string | null };
type MailAlias = { id: string; address: string; targetAddress: string; isActive: boolean; createdAt: string };
type MailMigrationJob = { id: string; sourceType: string; sourceReference: string; targetAddress: string; state: string; importedMessages: number; failedMessages: number; lastError: string | null; createdAt: string; updatedAt: string };
type MailboxList = { provider: Provider; mailboxes: Mailbox[] };
type DomainList = { provider: Provider; domains: MailDomain[] };

export function MailManager() {
  const [provider, setProvider] = useState<Provider | null>(null);
  const [domains, setDomains] = useState<MailDomain[]>([]);
  const [mailboxes, setMailboxes] = useState<Mailbox[]>([]);
  const [aliases, setAliases] = useState<MailAlias[]>([]);
  const [migrationJobs, setMigrationJobs] = useState<MailMigrationJob[]>([]);
  const [selectedMailbox, setSelectedMailbox] = useState<Mailbox | null>(null);
  const [message, setMessage] = useState("");
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    const controller = new AbortController();
    void loadAll(controller.signal)
      .catch((error) => {
        if (!controller.signal.aborted) setMessage(error instanceof Error ? error.message : "Falha ao carregar o e-mail institucional.");
      })
      .finally(() => {
        if (!controller.signal.aborted) setLoading(false);
      });
    return () => controller.abort();
  }, []);

  async function loadAll(signal?: AbortSignal) {
    const [mailboxesResponse, domainsResponse, aliasesResponse, migrationsResponse] = await Promise.all([
      fetch("/api/v1/admin/mailboxes", { signal }),
      fetch("/api/v1/admin/mail/domains", { signal }),
      fetch("/api/v1/admin/mail/aliases", { signal }),
      fetch("/api/v1/admin/mail/migration-jobs", { signal }),
    ]);
    for (const response of [mailboxesResponse, domainsResponse, aliasesResponse, migrationsResponse]) {
      if (!response.ok) throw new Error(await errorText(response));
    }
    const mailboxData = await mailboxesResponse.json() as MailboxList;
    const domainData = await domainsResponse.json() as DomainList;
    setProvider(mailboxData.provider ?? domainData.provider);
    setMailboxes(mailboxData.mailboxes);
    setDomains(domainData.domains);
    setAliases(await aliasesResponse.json() as MailAlias[]);
    setMigrationJobs(await migrationsResponse.json() as MailMigrationJob[]);
  }

  async function refreshMailboxes(preferredId?: string) {
    const response = await fetch("/api/v1/admin/mailboxes");
    if (!response.ok) throw new Error(await errorText(response));
    const data = await response.json() as MailboxList;
    setProvider(data.provider);
    setMailboxes(data.mailboxes);
    const id = preferredId ?? selectedMailbox?.id;
    setSelectedMailbox(id ? data.mailboxes.find((item) => item.id === id) ?? null : null);
  }

  async function refreshDomains() {
    const response = await fetch("/api/v1/admin/mail/domains");
    if (!response.ok) throw new Error(await errorText(response));
    const data = await response.json() as DomainList;
    setProvider(data.provider);
    setDomains(data.domains);
  }

  async function refreshAliases() {
    const response = await fetch("/api/v1/admin/mail/aliases");
    if (!response.ok) throw new Error(await errorText(response));
    setAliases(await response.json() as MailAlias[]);
  }

  async function refreshMigrations() {
    const response = await fetch("/api/v1/admin/mail/migration-jobs");
    if (!response.ok) throw new Error(await errorText(response));
    setMigrationJobs(await response.json() as MailMigrationJob[]);
  }

  async function createDomain(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const formElement = event.currentTarget;
    const form = new FormData(formElement);
    await execute(async () => {
      const response = await fetch("/api/v1/admin/mail/domains", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ domain: String(form.get("domain") ?? "").trim() }),
      });
      if (!response.ok) throw new Error(await errorText(response));
      formElement.reset();
      await refreshDomains();
      setMessage("Domínio institucional cadastrado. O estado exibido abaixo reflete o provider realmente configurado.");
    });
  }

  async function createMailbox(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const formElement = event.currentTarget;
    const form = new FormData(formElement);
    await execute(async () => {
      const response = await fetch("/api/v1/admin/mailboxes", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          address: String(form.get("address") ?? "").trim(),
          displayName: String(form.get("displayName") ?? "").trim(),
          quotaMegabytes: Number(form.get("quotaMegabytes") ?? 1024),
        }),
      });
      if (!response.ok) throw new Error(await errorText(response));
      const result = await response.json() as { id: string; provider: string; detail: string };
      formElement.reset();
      await refreshMailboxes(result.id);
      setMessage(`${result.detail} Estado do provider: ${result.provider}.`);
    });
  }

  async function updateMailbox(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!selectedMailbox) return;
    const form = new FormData(event.currentTarget);
    await execute(async () => {
      const response = await fetch(`/api/v1/admin/mailboxes/${selectedMailbox.id}`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          displayName: String(form.get("displayName") ?? "").trim(),
          quotaMegabytes: Number(form.get("quotaMegabytes") ?? selectedMailbox.quotaMegabytes),
        }),
      });
      if (!response.ok) throw new Error(await errorText(response));
      await refreshMailboxes(selectedMailbox.id);
      setMessage("Metadados da caixa atualizados e auditados.");
    });
  }

  async function createAlias(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const formElement = event.currentTarget;
    const form = new FormData(formElement);
    await execute(async () => {
      const response = await fetch("/api/v1/admin/mail/aliases", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          address: String(form.get("address") ?? "").trim(),
          targetAddress: String(form.get("targetAddress") ?? "").trim(),
        }),
      });
      if (!response.ok) throw new Error(await errorText(response));
      formElement.reset();
      await refreshAliases();
      setMessage("Alias institucional cadastrado e auditado.");
    });
  }

  async function deactivateAlias(id: string) {
    await execute(async () => {
      const response = await fetch(`/api/v1/admin/mail/aliases/${id}/deactivate`, { method: "POST" });
      if (!response.ok) throw new Error(await errorText(response));
      await refreshAliases();
      setMessage("Alias desativado; o registro histórico foi preservado.");
    });
  }

  async function createMigration(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const formElement = event.currentTarget;
    const form = new FormData(formElement);
    await execute(async () => {
      const response = await fetch("/api/v1/admin/mail/migration-jobs", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          sourceType: String(form.get("sourceType") ?? "EML"),
          sourceReference: String(form.get("sourceReference") ?? "").trim(),
          targetAddress: String(form.get("targetAddress") ?? "").trim(),
        }),
      });
      if (!response.ok) throw new Error(await errorText(response));
      const result = await response.json() as { externalDependency: string };
      formElement.reset();
      await refreshMigrations();
      setMessage(`Pedido de migração registrado. ${result.externalDependency}`);
    });
  }

  async function execute(action: () => Promise<void>) {
    setBusy(true);
    setMessage("");
    try {
      await action();
    } catch (error) {
      setMessage(error instanceof Error ? error.message : "A operação não pôde ser concluída.");
    } finally {
      setBusy(false);
    }
  }

  if (loading) return <div className="admin-panel empty-state" aria-busy="true"><h2>Carregando e-mail institucional…</h2></div>;

  return <>
    <div className="warning-box">
      <strong>Provider: {provider?.state ?? "DESCONHECIDO"}.</strong> {provider?.description ?? "Não foi possível obter a configuração do provider."}
      {(provider?.state === "DEMO_ONLY" || provider?.state === "NOT_CONFIGURED") && <p>Nenhuma caixa externa deve ser considerada operacional enquanto este estado permanecer ativo. O painel registra a configuração desejada sem transformar demonstração em serviço oficial.</p>}
    </div>

    <div className="editor-grid" style={{ marginTop: 20 }}>
      <section className="admin-panel">
        <h2>Domínios institucionais</h2>
        {domains.length === 0 ? <div className="empty-state"><p>Nenhum domínio cadastrado.</p></div> : <div className="compact-list">{domains.map((domain) => <div className="compact-item" key={domain.id}><div><strong>{domain.domain}</strong><small style={{ display: "block" }}>Atualizado em {formatDate(domain.updatedAt)}</small>{domain.externalId && <small style={{ display: "block" }}>ID externo: {domain.externalId}</small>}</div><span className="status-pill">{domain.state}</span></div>)}</div>}
        <form className="editor-fields" onSubmit={createDomain} style={{ marginTop: 20 }}>
          <label className="field">Domínio institucional<input name="domain" required placeholder="deodapolis.ms.gov.br" /></label>
          <button className="action-button secondary" disabled={busy}>Cadastrar domínio</button>
        </form>
      </section>

      <section className="admin-panel">
        <h2>Caixas postais</h2>
        {mailboxes.length === 0 ? <div className="empty-state"><p>Nenhuma caixa cadastrada.</p></div> : <div className="compact-list">{mailboxes.map((mailbox) => <button type="button" className="compact-item" key={mailbox.id} onClick={() => setSelectedMailbox(mailbox)} style={{ width: "100%", cursor: "pointer" }}><div><strong>{mailbox.address}</strong><small style={{ display: "block" }}>{mailbox.displayName} · {mailbox.quotaMegabytes} MB</small></div><span className="status-pill">{mailbox.status}</span></button>)}</div>}
        <form className="editor-fields" onSubmit={createMailbox} style={{ marginTop: 20 }}>
          <h3>Nova caixa</h3>
          <label className="field">Endereço da caixa<input name="address" type="email" required placeholder="contato@deodapolis.ms.gov.br" /></label>
          <label className="field">Nome de exibição<input name="displayName" required placeholder="Secretaria Municipal de Administração" /></label>
          <label className="field">Quota (MB)<input name="quotaMegabytes" type="number" min="128" max="102400" defaultValue="2048" required /></label>
          <button className="action-button" disabled={busy}>Solicitar caixa</button>
        </form>
      </section>
    </div>

    {selectedMailbox && <section className="admin-panel" style={{ marginTop: 20 }}>
      <div className="admin-heading"><div><span className="kicker">{selectedMailbox.status}</span><h2>{selectedMailbox.address}</h2><p>Edite somente metadados locais; o status continua refletindo o resultado real do provider.</p></div></div>
      <form className="editor-fields" key={`${selectedMailbox.id}-${selectedMailbox.displayName}-${selectedMailbox.quotaMegabytes}`} onSubmit={updateMailbox}>
        <label className="field">Nome de exibição da caixa<input name="displayName" defaultValue={selectedMailbox.displayName} required /></label>
        <label className="field">Quota da caixa (MB)<input name="quotaMegabytes" type="number" min="128" max="102400" defaultValue={selectedMailbox.quotaMegabytes} required /></label>
        <button className="action-button secondary" disabled={busy}>Salvar caixa</button>
      </form>
    </section>}

    <div className="editor-grid" style={{ marginTop: 20 }}>
      <section className="admin-panel">
        <h2>Aliases</h2>
        {aliases.length === 0 ? <div className="empty-state"><p>Nenhum alias cadastrado.</p></div> : <div className="compact-list">{aliases.map((alias) => <div className="compact-item" key={alias.id}><div><strong>{alias.address}</strong><small style={{ display: "block" }}>→ {alias.targetAddress}</small></div><div className="button-row"><span className="status-pill">{alias.isActive ? "ATIVO" : "INATIVO"}</span>{alias.isActive && <button type="button" className="action-button secondary" disabled={busy} onClick={() => void deactivateAlias(alias.id)}>Desativar</button>}</div></div>)}</div>}
        <form className="editor-fields" onSubmit={createAlias} style={{ marginTop: 20 }}>
          <label className="field">Endereço do alias<input name="address" type="email" required placeholder="ouvidoria@deodapolis.ms.gov.br" /></label>
          <label className="field">Destino do alias<input name="targetAddress" type="email" required placeholder="contato@deodapolis.ms.gov.br" /></label>
          <button className="action-button secondary" disabled={busy}>Cadastrar alias</button>
        </form>
      </section>

      <section className="admin-panel">
        <h2>Migração de mensagens</h2>
        <div className="warning-box"><strong>Sem segredos no portal.</strong> O job registra origem, referência e destino. Credenciais IMAP ou acesso ao provider devem permanecer em integração externa segura.</div>
        {migrationJobs.length === 0 ? <div className="empty-state"><p>Nenhum pedido de migração cadastrado.</p></div> : <div className="compact-list">{migrationJobs.map((job) => <div className="compact-item" key={job.id}><div><strong>{job.sourceType} → {job.targetAddress}</strong><small style={{ display: "block" }}>{job.sourceReference} · {formatDate(job.createdAt)}</small><small style={{ display: "block" }}>{job.importedMessages} importadas · {job.failedMessages} falhas</small>{job.lastError && <small style={{ display: "block" }}>{job.lastError}</small>}</div><span className="status-pill">{job.state}</span></div>)}</div>}
        <form className="editor-fields" onSubmit={createMigration} style={{ marginTop: 20 }}>
          <label className="field">Tipo de origem<select name="sourceType" defaultValue="EML"><option value="IMAP">IMAP</option><option value="MBOX">MBOX</option><option value="EML">EML</option></select></label>
          <label className="field">Referência da origem<input name="sourceReference" required placeholder="Lote, arquivo, mailbox ou identificador sem credencial" /></label>
          <label className="field">Caixa de destino<input name="targetAddress" type="email" required /></label>
          <button className="action-button secondary" disabled={busy}>Registrar migração</button>
        </form>
      </section>
    </div>

    {message && <div className="form-message" role="status" style={{ marginTop: 16 }}>{message}</div>}
  </>;
}

function formatDate(value: string | null) {
  if (!value) return "—";
  return new Intl.DateTimeFormat("pt-BR", { dateStyle: "medium", timeStyle: "short" }).format(new Date(value));
}

async function errorText(response: Response) {
  const body = await response.json().catch(() => null) as { title?: string; detail?: string; errors?: Record<string, string[]> } | null;
  const validation = Object.values(body?.errors ?? {}).flat().join(" ");
  return validation || body?.detail || body?.title || `Erro ${response.status}`;
}
