"use client";

import { useEffect, useState, type FormEvent } from "react";

type Edition = {
  id: string;
  number: number;
  year: number;
  type: string;
  publicationDate: string;
  status: string;
  sha256: string | null;
  verificationCode: string | null;
};

type IntegritySignature = {
  id: string;
  provider: string;
  certificateSerial: string;
  certificateSubject: string;
  certificateIssuer: string;
  certificateValidFrom: string;
  certificateValidTo: string;
  isIcpBrasil: boolean;
  signedAt: string;
  validationState: string;
};

type IntegrityCorrection = {
  id: string;
  originalEditionId: string;
  correctionEditionId: string;
  reason: string;
  createdAt: string;
};

type Integrity = {
  edition: Edition;
  signatures: IntegritySignature[];
  publication: null | {
    id: string;
    publishedAt: string;
    sha256: string;
    verificationCode: string;
    publicUrl: string;
  };
  corrections: IntegrityCorrection[];
  corrects: IntegrityCorrection[];
};

type ActionResponse = Edition | {
  edition: Edition;
  warning?: string | null;
  provider?: string;
};

type CorrectionResponse = {
  edition: Edition;
  correction: IntegrityCorrection;
};

export function GazetteComposer() {
  const [items, setItems] = useState<Edition[]>([]);
  const [current, setCurrent] = useState<Edition | null>(null);
  const [integrity, setIntegrity] = useState<Integrity | null>(null);
  const [message, setMessage] = useState("");
  const [busy, setBusy] = useState(false);
  const [section, setSection] = useState("Secretaria Municipal de Administração");
  const [actTitle, setActTitle] = useState("[DEMONSTRAÇÃO] Portaria para fluxo de apresentação");
  const [actBody, setActBody] = useState("Conteúdo sintético sem valor de ato oficial, usado exclusivamente para validar composição, PDF, hash, QR e workflow.");

  useEffect(() => {
    const controller = new AbortController();
    void fetch("/api/v1/admin/gazette", { signal: controller.signal })
      .then(async (response) => {
        if (response.ok && !controller.signal.aborted) setItems(await response.json() as Edition[]);
      })
      .catch(() => undefined);
    return () => controller.abort();
  }, []);

  async function load() {
    const response = await fetch("/api/v1/admin/gazette");
    if (response.ok) setItems(await response.json() as Edition[]);
  }

  async function loadIntegrity(id: string) {
    const response = await fetch(`/api/v1/admin/gazette/${id}/integrity`);
    if (response.ok) setIntegrity(await response.json() as Integrity);
    else setIntegrity(null);
  }

  async function selectEdition(edition: Edition) {
    setCurrent(edition);
    setMessage("");
    await loadIntegrity(edition.id);
  }

  async function create(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    setBusy(true);
    const response = await fetch("/api/v1/admin/gazette", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ number: Number(form.get("number")), year: Number(form.get("year")), type: form.get("type"), publicationDate: form.get("date") }),
    });
    if (response.ok) {
      const edition = await response.json() as Edition;
      setCurrent(edition);
      setIntegrity(null);
      setMessage("Edição criada em DRAFT.");
      await Promise.all([load(), loadIntegrity(edition.id)]);
    } else setMessage(await errorText(response));
    setBusy(false);
  }

  async function compose() {
    if (!current) return;
    setBusy(true);
    const response = await fetch(`/api/v1/admin/gazette/${current.id}/composition`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ sections: [{ title: section, acts: [{ title: actTitle, body: actBody, organization: "Administração", legalReference: "DEMONSTRAÇÃO" }] }] }),
    });
    if (response.ok) {
      setCurrent(await response.json() as Edition);
      setMessage("Composição persistida.");
      await loadIntegrity(current.id);
    } else setMessage(await errorText(response));
    setBusy(false);
  }

  async function action(name: string) {
    if (!current) return;
    setBusy(true);
    const response = await fetch(`/api/v1/admin/gazette/${current.id}/${name}`, { method: "POST" });
    if (response.ok) {
      const body = await response.json() as ActionResponse;
      const edition = "edition" in body ? body.edition : body;
      setCurrent(edition);
      setMessage(("warning" in body && body.warning) || `Ação ${name} concluída.`);
      await Promise.all([load(), loadIntegrity(edition.id)]);
    } else setMessage(await errorText(response));
    setBusy(false);
  }

  async function createCorrection(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!current) return;
    const form = new FormData(event.currentTarget);
    setBusy(true);
    const response = await fetch(`/api/v1/admin/gazette/${current.id}/corrections`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        number: Number(form.get("number")),
        year: Number(form.get("year")),
        publicationDate: form.get("date"),
        reason: form.get("reason"),
      }),
    });
    if (response.ok) {
      const result = await response.json() as CorrectionResponse;
      setMessage(`Correção vinculada criada como edição complementar ${result.edition.number}/${result.edition.year}. A edição original permaneceu imutável.`);
      setCurrent(result.edition);
      await Promise.all([load(), loadIntegrity(result.edition.id)]);
      event.currentTarget.reset();
    } else setMessage(await errorText(response));
    setBusy(false);
  }

  return <div className="editor-grid">
    <section>
      <form className="admin-panel editor-fields" onSubmit={create}>
        <h2>Nova edição</h2>
        <label className="field">Número<input name="number" type="number" min="1" required /></label>
        <label className="field">Ano<input name="year" type="number" min="2000" defaultValue={new Date().getFullYear()} required /></label>
        <label className="field">Tipo<select name="type"><option>ORDINARY</option><option>EXTRAORDINARY</option><option>COMPLEMENTARY</option></select></label>
        <label className="field">Data<input name="date" type="date" defaultValue={new Date().toISOString().slice(0, 10)} required /></label>
        <button className="action-button" disabled={busy || Boolean(current)}>Criar edição</button>
      </form>
      {current && <div className="admin-panel editor-fields">
        <div className="button-row" style={{ justifyContent: "space-between" }}>
          <div><h2>Composição · edição {current.number}/{current.year}</h2><small>{current.type} · {current.publicationDate}</small></div>
          <button type="button" className="action-button secondary" onClick={() => { setCurrent(null); setIntegrity(null); setMessage(""); }}>Nova edição</button>
        </div>
        {current.status === "PUBLISHED"
          ? <div className="warning-box"><strong>Edição publicada e imutável.</strong> Qualquer alteração exige uma correção vinculada, criada abaixo.</div>
          : <>
            <label className="field">Seção<input value={section} onChange={(event) => setSection(event.target.value)} disabled={busy || !["DRAFT", "REVIEW"].includes(current.status)} /></label>
            <label className="field">Título do ato<input value={actTitle} onChange={(event) => setActTitle(event.target.value)} disabled={busy || !["DRAFT", "REVIEW"].includes(current.status)} /></label>
            <label className="field">Conteúdo<textarea value={actBody} onChange={(event) => setActBody(event.target.value)} rows={8} disabled={busy || !["DRAFT", "REVIEW"].includes(current.status)} /></label>
            <button type="button" className="action-button secondary" onClick={compose} disabled={busy || !["DRAFT", "REVIEW"].includes(current.status)}>Salvar composição</button>
          </>}
        <div className="button-row">
          <button type="button" className="action-button secondary" onClick={() => action("submit")} disabled={busy || current.status !== "DRAFT"}>Revisão</button>
          <button type="button" className="action-button secondary" onClick={() => action("approve")} disabled={busy || current.status !== "REVIEW"}>Aprovar</button>
          <button type="button" className="action-button secondary" onClick={() => action("generate")} disabled={busy || current.status !== "APPROVED"}>Gerar PDF</button>
          <button type="button" className="action-button secondary" onClick={() => action("sign")} disabled={busy || current.status !== "GENERATED"}>Assinar</button>
          <button type="button" className="action-button" onClick={() => action("publish")} disabled={busy || current.status !== "SIGNED"}>Publicar</button>
        </div>
        <p><span className="status-pill">{current.status}</span>{current.sha256 && <> · SHA <code>{current.sha256.slice(0, 16)}…</code></>}</p>
        {current.verificationCode && <a href={`/verificar/${current.verificationCode}`} target="_blank" rel="noreferrer">Abrir verificação pública ↗</a>}
      </div>}

      {current?.status === "PUBLISHED" && <form className="admin-panel editor-fields" onSubmit={createCorrection}>
        <h2>Criar correção vinculada</h2>
        <p>A edição {current.number}/{current.year} não será alterada. O sistema criará uma nova edição <strong>COMPLEMENTARY</strong> ligada à original.</p>
        <label className="field">Número da nova edição<input name="number" type="number" min="1" required /></label>
        <label className="field">Ano<input name="year" type="number" min="2000" defaultValue={new Date().getFullYear()} required /></label>
        <label className="field">Data<input name="date" type="date" min={current.publicationDate} defaultValue={new Date().toISOString().slice(0, 10)} required /></label>
        <label className="field">Justificativa<textarea name="reason" rows={4} minLength={10} maxLength={2000} required /></label>
        <button className="action-button" disabled={busy}>Criar correção</button>
      </form>}

      {current && <IntegrityPanel integrity={integrity} />}
    </section>
    <aside className="admin-panel">
      <h2>Edições</h2>
      <div className="compact-list">{items.map((item) => <button type="button" key={item.id} className="compact-item" onClick={() => void selectEdition(item)} style={{ width: "100%", cursor: "pointer" }}><span><strong>{item.number}/{item.year}</strong><small style={{ display: "block" }}>{item.type}</small></span><span className="status-pill">{item.status}</span></button>)}</div>
      {message && <div className="form-message" role="status">{message}</div>}
      <div className="warning-box" style={{ marginTop: 16 }}><strong>Assinatura:</strong> em POC pode existir provider <code>DEMO_ONLY</code>, explicitamente sem valor ICP-Brasil. Produção permanece <code>NOT_CONFIGURED</code> até certificado/serviço real.</div>
    </aside>
  </div>;
}

function IntegrityPanel({ integrity }: { integrity: Integrity | null }) {
  if (!integrity) return <section className="admin-panel"><h2>Cadeia de integridade</h2><p>Nenhum registro normalizado disponível para esta edição ainda.</p></section>;
  return <section className="admin-panel editor-fields" aria-labelledby="gazette-integrity-title">
    <h2 id="gazette-integrity-title">Cadeia de integridade</h2>
    <div className="compact-list">
      {integrity.signatures.length === 0
        ? <div className="compact-item"><span><strong>Assinatura</strong><small style={{ display: "block" }}>Ainda não registrada.</small></span><span className="status-pill">PENDENTE</span></div>
        : integrity.signatures.map((signature) => <div className="compact-item" key={signature.id}>
          <div>
            <strong>{signature.provider} · {signature.isIcpBrasil ? "ICP-Brasil" : "não ICP"}</strong>
            <small style={{ display: "block" }}>{signature.certificateSubject}</small>
            <small style={{ display: "block" }}>Emissor: {signature.certificateIssuer}</small>
            <small style={{ display: "block" }}>Serial: {signature.certificateSerial} · assinado {formatDateTime(signature.signedAt)}</small>
            <small style={{ display: "block" }}>Validade: {formatDateTime(signature.certificateValidFrom)} → {formatDateTime(signature.certificateValidTo)}</small>
          </div>
          <span className="status-pill">{signature.validationState.startsWith("VALID:") ? "VALIDADA" : signature.validationState}</span>
        </div>)}
      {integrity.publication
        ? <div className="compact-item"><div><strong>Publicação registrada</strong><small style={{ display: "block" }}>{formatDateTime(integrity.publication.publishedAt)}</small><small style={{ display: "block" }}>Código: {integrity.publication.verificationCode}</small><a href={integrity.publication.publicUrl} target="_blank" rel="noreferrer">Abrir PDF público ↗</a></div><span className="status-pill">PUBLICADA</span></div>
        : <div className="compact-item"><span><strong>Publicação</strong><small style={{ display: "block" }}>Registro ainda não criado.</small></span><span className="status-pill">PENDENTE</span></div>}
    </div>
    {integrity.corrects.length > 0 && <div><h3>Corrige</h3>{integrity.corrects.map((link) => <p key={link.id}>Edição original <code>{link.originalEditionId}</code>: {link.reason}</p>)}</div>}
    {integrity.corrections.length > 0 && <div><h3>Correções vinculadas</h3>{integrity.corrections.map((link) => <p key={link.id}>Edição de correção <code>{link.correctionEditionId}</code>: {link.reason}</p>)}</div>}
  </section>;
}

function formatDateTime(value: string) {
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? value : new Intl.DateTimeFormat("pt-BR", { dateStyle: "short", timeStyle: "short" }).format(date);
}

async function errorText(response: Response) {
  const body = await response.json().catch(() => null) as { title?: string; detail?: string; errors?: Record<string, string[]> } | null;
  const validation = Object.values(body?.errors ?? {}).flat().join(" ");
  return body?.detail ?? body?.title ?? (validation || `Erro ${response.status}`);
}
