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

export function GazetteComposer() {
  const [items, setItems] = useState<Edition[]>([]);
  const [current, setCurrent] = useState<Edition | null>(null);
  const [message, setMessage] = useState("");
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

  async function create(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    const response = await fetch("/api/v1/admin/gazette", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ number: Number(form.get("number")), year: Number(form.get("year")), type: form.get("type"), publicationDate: form.get("date") }),
    });
    if (response.ok) {
      const edition = await response.json() as Edition;
      setCurrent(edition);
      setMessage("Edição criada em DRAFT.");
      await load();
    } else setMessage(await errorText(response));
  }

  async function compose() {
    if (!current) return;
    const response = await fetch(`/api/v1/admin/gazette/${current.id}/composition`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ sections: [{ title: section, acts: [{ title: actTitle, body: actBody, organization: "Administração", legalReference: "DEMONSTRAÇÃO" }] }] }),
    });
    if (response.ok) {
      setCurrent(await response.json() as Edition);
      setMessage("Composição persistida.");
    } else setMessage(await errorText(response));
  }

  async function action(name: string) {
    if (!current) return;
    const response = await fetch(`/api/v1/admin/gazette/${current.id}/${name}`, { method: "POST" });
    if (response.ok) {
      const body = await response.json() as Edition | { edition: Edition; warning?: string; provider?: string };
      const edition = "edition" in body ? body.edition : body;
      setCurrent(edition);
      setMessage(("warning" in body && body.warning) || `Ação ${name} concluída.`);
      await load();
    } else setMessage(await errorText(response));
  }

  return <div className="editor-grid">
    <section>
      <form className="admin-panel editor-fields" onSubmit={create}>
        <h2>Nova edição</h2>
        <label className="field">Número<input name="number" type="number" min="1" required /></label>
        <label className="field">Ano<input name="year" type="number" min="2000" defaultValue={new Date().getFullYear()} required /></label>
        <label className="field">Tipo<select name="type"><option>ORDINARY</option><option>EXTRAORDINARY</option><option>COMPLEMENTARY</option></select></label>
        <label className="field">Data<input name="date" type="date" defaultValue={new Date().toISOString().slice(0, 10)} required /></label>
        <button className="action-button" disabled={Boolean(current)}>Criar edição</button>
      </form>
      {current && <div className="admin-panel editor-fields">
        <h2>Composição · edição {current.number}/{current.year}</h2>
        <label className="field">Seção<input value={section} onChange={(event) => setSection(event.target.value)} /></label>
        <label className="field">Título do ato<input value={actTitle} onChange={(event) => setActTitle(event.target.value)} /></label>
        <label className="field">Conteúdo<textarea value={actBody} onChange={(event) => setActBody(event.target.value)} rows={8} /></label>
        <button type="button" className="action-button secondary" onClick={compose}>Salvar composição</button>
        <div className="button-row">
          <button type="button" className="action-button secondary" onClick={() => action("submit")} disabled={current.status !== "DRAFT"}>Revisão</button>
          <button type="button" className="action-button secondary" onClick={() => action("approve")} disabled={current.status !== "REVIEW"}>Aprovar</button>
          <button type="button" className="action-button secondary" onClick={() => action("generate")} disabled={current.status !== "APPROVED"}>Gerar PDF</button>
          <button type="button" className="action-button secondary" onClick={() => action("sign")} disabled={current.status !== "GENERATED"}>Assinar</button>
          <button type="button" className="action-button" onClick={() => action("publish")} disabled={current.status !== "SIGNED"}>Publicar</button>
        </div>
        <p><span className="status-pill">{current.status}</span>{current.sha256 && <> · SHA <code>{current.sha256.slice(0, 16)}…</code></>}</p>
        {current.verificationCode && <a href={`/verificar/${current.verificationCode}`} target="_blank" rel="noreferrer">Abrir verificação pública ↗</a>}
      </div>}
    </section>
    <aside className="admin-panel">
      <h2>Edições</h2>
      <div className="compact-list">{items.map((item) => <button type="button" key={item.id} className="compact-item" onClick={() => setCurrent(item)} style={{ width: "100%", cursor: "pointer" }}><span><strong>{item.number}/{item.year}</strong><small style={{ display: "block" }}>{item.type}</small></span><span className="status-pill">{item.status}</span></button>)}</div>
      {message && <div className="form-message" role="status">{message}</div>}
      <div className="warning-box" style={{ marginTop: 16 }}><strong>Assinatura:</strong> em POC pode existir provider <code>DEMO_ONLY</code>, explicitamente sem valor ICP-Brasil. Produção permanece <code>NOT_CONFIGURED</code> até certificado/serviço real.</div>
    </aside>
  </div>;
}

async function errorText(response: Response) {
  const body = await response.json().catch(() => null) as { title?: string; detail?: string } | null;
  return body?.detail ?? body?.title ?? `Erro ${response.status}`;
}
