"use client";

import { useEffect, useState, type FormEvent } from "react";

type LinkCheck = {
  id: string;
  url: string;
  statusCode: number | null;
  state: string;
  checkedAt: string | null;
  latencyMilliseconds: number | null;
  failureReason: string | null;
  consecutiveFailures: number;
  createdAt: string;
};

type BackupEvidence = {
  id: string;
  provider: string;
  backupType: string;
  startedAt: string;
  completedAt: string | null;
  status: string;
  reference: string | null;
  sizeBytes: number | null;
  restoreTestedAt: string | null;
  error: string | null;
};

export function OperationsManager() {
  const [links, setLinks] = useState<LinkCheck[]>([]);
  const [backups, setBackups] = useState<BackupEvidence[]>([]);
  const [message, setMessage] = useState("");
  const [loading, setLoading] = useState(true);
  // Uma carga que falhou não é um inventário vazio: sem esta distinção, um erro de rede
  // aparecia como "nenhuma URL cadastrada" e "nenhuma evidência de backup", que é a leitura
  // oposta da verdade num painel de operações.
  const [loadFailed, setLoadFailed] = useState(false);
  const [reloadToken, setReloadToken] = useState(0);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    const controller = new AbortController();
    void Promise.all([
      fetch("/api/v1/admin/operations/links", { signal: controller.signal }),
      fetch("/api/v1/admin/operations/backups", { signal: controller.signal }),
    ])
      .then(async ([linksResponse, backupsResponse]) => {
        if (!linksResponse.ok) throw new Error(await errorText(linksResponse));
        if (!backupsResponse.ok) throw new Error(await errorText(backupsResponse));
        const linkData = await linksResponse.json() as LinkCheck[];
        const backupData = await backupsResponse.json() as BackupEvidence[];
        if (controller.signal.aborted) return;
        setLinks(linkData);
        setBackups(backupData);
      })
      .catch((error) => {
        if (controller.signal.aborted) return;
        setLoadFailed(true);
        setMessage(error instanceof Error ? error.message : "Falha ao carregar operações.");
      })
      .finally(() => {
        if (!controller.signal.aborted) setLoading(false);
      });
    return () => controller.abort();
  }, [reloadToken]);

  async function refreshLinks() {
    const response = await fetch("/api/v1/admin/operations/links");
    if (!response.ok) throw new Error(await errorText(response));
    setLinks(await response.json() as LinkCheck[]);
  }

  async function refreshBackups() {
    const response = await fetch("/api/v1/admin/operations/backups");
    if (!response.ok) throw new Error(await errorText(response));
    setBackups(await response.json() as BackupEvidence[]);
  }

  async function addLink(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const formElement = event.currentTarget;
    const form = new FormData(formElement);
    await execute(async () => {
      const response = await fetch("/api/v1/admin/operations/links", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ url: String(form.get("url") ?? "").trim() }),
      });
      if (!response.ok) throw new Error(await errorText(response));
      formElement.reset();
      await refreshLinks();
      setMessage("URL adicionada ao monitoramento periódico e auditada.");
    });
  }

  async function checkLink(id: string) {
    await execute(async () => {
      const response = await fetch(`/api/v1/admin/operations/links/${id}/check`, { method: "POST" });
      if (!response.ok) throw new Error(await errorText(response));
      const result = await response.json() as LinkCheck;
      await refreshLinks();
      setMessage(`Verificação concluída: ${result.state}.`);
    });
  }

  async function removeLink(id: string) {
    await execute(async () => {
      const response = await fetch(`/api/v1/admin/operations/links/${id}`, { method: "DELETE" });
      if (!response.ok) throw new Error(await errorText(response));
      await refreshLinks();
      setMessage("URL removida do monitoramento; a ação ficou registrada na auditoria.");
    });
  }

  async function addBackupEvidence(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const formElement = event.currentTarget;
    const form = new FormData(formElement);
    await execute(async () => {
      const status = String(form.get("status") ?? "STARTED");
      const response = await fetch("/api/v1/admin/operations/backups/evidence", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          provider: String(form.get("provider") ?? "").trim(),
          backupType: String(form.get("backupType") ?? "").trim(),
          startedAt: toIso(String(form.get("startedAt") ?? "")),
          status,
          completedAt: toIso(String(form.get("completedAt") ?? ""), true),
          reference: nullableText(form.get("reference")),
          sizeBytes: nullableNumber(form.get("sizeBytes")),
          restoreTestedAt: toIso(String(form.get("restoreTestedAt") ?? ""), true),
          error: nullableText(form.get("error")),
        }),
      });
      if (!response.ok) throw new Error(await errorText(response));
      formElement.reset();
      await refreshBackups();
      setMessage("Evidência de backup registrada. Este painel não executa backup: ele preserva a prova produzida pelo provider/rotina externa.");
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

  if (loading) return <div className="admin-panel empty-state" aria-busy="true"><h2>Carregando operações…</h2></div>;

  return <>
    <section className="admin-panel">
      <div className="admin-heading">
        <div>
          <span className="kicker">Monitoramento ativo</span>
          <h2>Saúde de links</h2>
          <p>O worker verifica cada URL periodicamente. DNS privado, loopback, credenciais na URL, portas não permitidas e redirects automáticos são bloqueados pela política SSRF.</p>
        </div>
      </div>
      <form className="editor-fields" onSubmit={addLink}>
        <label className="field">URL monitorada<input name="url" type="url" required placeholder="https://servico.deodapolis.ms.gov.br/" /></label>
        <button className="action-button" disabled={busy}>Adicionar monitoramento</button>
      </form>
      <div className="compact-list" style={{ marginTop: 20 }}>
        {loadFailed
          ? <div className="warning-box" role="alert"><p>Não foi possível carregar o monitoramento de links.</p><p>Este painel não está afirmando que nenhuma URL está quebrada — a consulta não respondeu.</p><button type="button" className="action-button secondary" onClick={() => { setLoadFailed(false); setMessage(""); setReloadToken((current) => current + 1); }}>Tentar novamente</button></div>
          : links.length === 0 && <div className="empty-state"><p>Nenhuma URL cadastrada para monitoramento.</p></div>}
        {links.map((link) => <div className="compact-item" key={link.id}>
          <div style={{ minWidth: 0 }}>
            <strong style={{ overflowWrap: "anywhere" }}>{link.url}</strong>
            <small style={{ display: "block" }}>
              {link.statusCode ? `HTTP ${link.statusCode}` : "Sem status HTTP"} · {link.latencyMilliseconds !== null ? `${link.latencyMilliseconds} ms` : "latência indisponível"} · {link.checkedAt ? `checado ${formatDate(link.checkedAt)}` : "ainda não verificado"}
            </small>
            <small style={{ display: "block" }}>{link.consecutiveFailures} falha(s) consecutiva(s){link.failureReason ? ` · ${link.failureReason}` : ""}</small>
          </div>
          <div className="button-row">
            <span className="status-pill">{link.state}</span>
            <button type="button" className="action-button secondary" disabled={busy} aria-label={`Verificar ${link.url}`} onClick={() => void checkLink(link.id)}>Verificar agora</button>
            <button type="button" className="action-button secondary" disabled={busy} aria-label={`Remover ${link.url}`} onClick={() => void removeLink(link.id)}>Remover</button>
          </div>
        </div>)}
      </div>
    </section>

    <section className="admin-panel" style={{ marginTop: 20 }}>
      <div className="admin-heading">
        <div>
          <span className="kicker">Continuidade</span>
          <h2>Backup e teste de restauração</h2>
          <p>Registre evidências do backup executado pelo provider ou rotina externa. Uma evidência COMPLETED sem teste de restauração continua visivelmente sem confirmação de restore.</p>
        </div>
      </div>
      <form className="editor-fields" onSubmit={addBackupEvidence}>
        <label className="field">Provider do backup<input name="provider" required placeholder="PostgreSQL / S3 / provedor contratado" /></label>
        <label className="field">Tipo de backup<input name="backupType" required placeholder="DATABASE_FULL" /></label>
        <label className="field">Início<input name="startedAt" type="datetime-local" required /></label>
        <label className="field">Status da evidência<select name="status" defaultValue="COMPLETED"><option value="STARTED">STARTED</option><option value="COMPLETED">COMPLETED</option><option value="FAILED">FAILED</option></select></label>
        <label className="field">Conclusão<input name="completedAt" type="datetime-local" /></label>
        <label className="field">Referência do artefato<input name="reference" placeholder="backup://provider/artefato" /></label>
        <label className="field">Tamanho (bytes)<input name="sizeBytes" type="number" min="0" /></label>
        <label className="field">Restore testado em<input name="restoreTestedAt" type="datetime-local" /></label>
        <label className="field">Erro<textarea name="error" rows={2} placeholder="Preencha quando o status for FAILED" /></label>
        <button className="action-button secondary" disabled={busy}>Registrar evidência</button>
      </form>
      <div className="compact-list" style={{ marginTop: 20 }}>
        {loadFailed
          ? <div className="warning-box" role="alert"><p>Não foi possível carregar as evidências de backup.</p><p>A ausência de itens abaixo é falta de resposta, não ausência de backup.</p></div>
          : backups.length === 0 && <div className="empty-state"><p>Nenhuma evidência de backup registrada.</p></div>}
        {backups.map((backup) => <div className="compact-item" key={backup.id}>
          <div>
            <strong>{backup.provider} · {backup.backupType}</strong>
            <small style={{ display: "block" }}>Início {formatDate(backup.startedAt)}{backup.completedAt ? ` · conclusão ${formatDate(backup.completedAt)}` : ""}</small>
            <small style={{ display: "block" }}>{backup.reference ?? "Sem referência"}{backup.sizeBytes !== null ? ` · ${formatBytes(backup.sizeBytes)}` : ""}</small>
            <small style={{ display: "block" }}>{backup.restoreTestedAt ? `Restore testado ${formatDate(backup.restoreTestedAt)}` : "Restore ainda não evidenciado"}{backup.error ? ` · ${backup.error}` : ""}</small>
          </div>
          <span className="status-pill">{backup.status}</span>
        </div>)}
      </div>
    </section>

    {message && <div className="form-message" role="status" style={{ marginTop: 16 }}>{message}</div>}
  </>;
}

function toIso(value: string, nullable = false) {
  if (!value) {
    if (nullable) return null;
    throw new Error("Data de início é obrigatória.");
  }
  const parsed = new Date(value);
  if (Number.isNaN(parsed.getTime())) throw new Error("Data inválida.");
  return parsed.toISOString();
}

function nullableText(value: FormDataEntryValue | null) {
  const text = String(value ?? "").trim();
  return text || null;
}

function nullableNumber(value: FormDataEntryValue | null) {
  const text = String(value ?? "").trim();
  if (!text) return null;
  const number = Number(text);
  return Number.isFinite(number) ? number : null;
}

function formatDate(value: string | null) {
  if (!value) return "—";
  return new Intl.DateTimeFormat("pt-BR", { dateStyle: "medium", timeStyle: "short" }).format(new Date(value));
}

function formatBytes(value: number) {
  if (value < 1024) return `${value} B`;
  if (value < 1024 * 1024) return `${(value / 1024).toFixed(1)} KB`;
  if (value < 1024 * 1024 * 1024) return `${(value / (1024 * 1024)).toFixed(1)} MB`;
  return `${(value / (1024 * 1024 * 1024)).toFixed(1)} GB`;
}

async function errorText(response: Response) {
  const body = await response.json().catch(() => null) as { title?: string; detail?: string; errors?: Record<string, string[]> } | null;
  const validation = Object.values(body?.errors ?? {}).flat().join(" ");
  return validation || body?.detail || body?.title || `Erro ${response.status}`;
}
