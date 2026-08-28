"use client";

import { useEffect, useState, type FormEvent } from "react";
import { StatusBadge } from "@/components/ui";
import { ResourcePayloadFields, serializeResourcePayload } from "./resource-payload-fields";

type Resource = {
  id: string;
  kind: string;
  slug: string;
  title: string;
  summary: string;
  payloadJson: string;
  status: string;
  displayOrder: number;
  startsAt: string | null;
  endsAt: string | null;
  publishedAt: string | null;
  version: number;
  updatedAt: string;
  updatedBy: string;
};

type Revision = {
  id: string;
  resourceKind: string;
  version: number;
  snapshotJson: string;
  createdBy: string;
  createdAt: string;
};

const kinds = ["PAGE", "BANNER", "EVENT", "LEGISLATION", "DATASET", "LOCATION", "CONTACT", "ALERT", "MENU", "HOME_BLOCK", "PROCUREMENT_LINK", "ESIC_LINK", "OUVIDORIA_LINK"];

// Onde cada tipo realmente aparece no portal. `null` significa que nenhuma rota pública lê este
// tipo hoje: publicar continua gravando e versionando o registro, mas nada muda para o cidadão.
// Dizer isto antes é preferível a deixar o servidor descobrir depois que publicou no vazio.
const publicDestination: Record<string, string | null> = {
  PAGE: "Páginas institucionais, pelo slug",
  EVENT: "/agenda",
  LOCATION: "/locais",
  LEGISLATION: "/legislacao",
  CONTACT: "/contatos",
  PROCUREMENT_LINK: "/licitacoes",
  MENU: "Menu de navegação do portal",
  BANNER: null,
  DATASET: null,
  ALERT: null,
  HOME_BLOCK: null,
  ESIC_LINK: null,
  OUVIDORIA_LINK: null,
};

// Slugs de PAGE que possuem rota pública. Qualquer outro slug fica governado no CMS sem
// destino no portal até que uma rota passe a lê-lo.
const routedPageSlugs = [
  "home", "municipio", "gestao", "prefeito", "vice-prefeito", "conselhos", "obras",
  "acesso-a-informacao", "esic-estatisticas", "esic-perguntas-frequentes", "calendario-licitacoes",
];

export function ResourceManager() {
  const [items, setItems] = useState<Resource[]>([]);
  const [kind, setKind] = useState("PAGE");
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [revisions, setRevisions] = useState<Revision[]>([]);
  const [message, setMessage] = useState("");
  const [listState, setListState] = useState<"LOADING" | "READY" | "ERROR">("LOADING");
  const [reloadToken, setReloadToken] = useState(0);
  const selected = items.find((item) => item.id === selectedId) ?? null;
  const menuOptions = items.filter((item) => item.id !== selectedId).map((item) => ({ value: item.slug, label: item.title }));

  useEffect(() => {
    const controller = new AbortController();
    void fetch(`/api/v1/admin/resources?kind=${encodeURIComponent(kind)}`, { signal: controller.signal })
      .then(async (response) => {
        if (!response.ok) throw new Error("resources");
        const next = await response.json() as Resource[];
        if (controller.signal.aborted) return;
        setItems(next);
        setListState("READY");
      })
      .catch(() => { if (!controller.signal.aborted) setListState("ERROR"); });
    return () => controller.abort();
  }, [kind, reloadToken]);

  function changeKind(value: string) {
    setKind(value);
    setSelectedId(null);
    setRevisions([]);
    setMessage("");
  }

  async function load(preferredId?: string) {
    const response = await fetch(`/api/v1/admin/resources?kind=${encodeURIComponent(kind)}`);
    if (!response.ok) { setListState("ERROR"); return; }
    const nextItems = await response.json() as Resource[];
    setItems(nextItems);
    setListState("READY");
    if (preferredId) setSelectedId(nextItems.some((item) => item.id === preferredId) ? preferredId : null);
  }

  async function loadRevisions(id: string) {
    const response = await fetch(`/api/v1/admin/resources/${id}/revisions`);
    setRevisions(response.ok ? await response.json() as Revision[] : []);
  }

  async function selectResource(item: Resource) {
    setSelectedId(item.id);
    setMessage("");
    await loadRevisions(item.id);
  }

  async function create(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const formElement = event.currentTarget;
    const form = new FormData(formElement);
    let payloadJson: string;
    try {
      payloadJson = serializeResourcePayload(kind, form);
    } catch {
      setMessage("Os detalhes estruturados precisam ser válidos.");
      return;
    }
    const payload = {
      kind,
      slug: form.get("slug"),
      title: form.get("title"),
      summary: form.get("summary"),
      payloadJson,
      displayOrder: Number(form.get("displayOrder") || 0),
      startsAt: toIsoOrNull(form.get("startsAt")),
      endsAt: toIsoOrNull(form.get("endsAt")),
    };
    const response = await fetch("/api/v1/admin/resources", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload),
    });
    setMessage(response.ok ? "Conteúdo criado como rascunho." : await errorText(response));
    if (response.ok) {
      const created = await response.json() as Resource;
      formElement.reset();
      await load(created.id);
      await loadRevisions(created.id);
    }
  }

  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!selected) return;
    if (selected.status === "ARCHIVED") {
      setMessage("Restaure o conteúdo arquivado antes de editar.");
      return;
    }

    const form = new FormData(event.currentTarget);
    let payloadJson: string;
    try {
      payloadJson = serializeResourcePayload(selected.kind, form);
    } catch {
      setMessage("Os detalhes estruturados precisam ser válidos.");
      return;
    }
    const response = await fetch(`/api/v1/admin/resources/${selected.id}`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        title: form.get("title"),
        summary: form.get("summary"),
        payloadJson,
        displayOrder: Number(form.get("displayOrder") || 0),
        startsAt: toIsoOrNull(form.get("startsAt")),
        endsAt: toIsoOrNull(form.get("endsAt")),
        expectedVersion: selected.version,
      }),
    });

    if (response.status === 409) {
      const error = await errorText(response);
      await load(selected.id);
      await loadRevisions(selected.id);
      setMessage(`${error} O formulário foi recarregado com a versão mais recente.`);
      return;
    }

    if (!response.ok) {
      setMessage(await errorText(response));
      return;
    }

    const updated = await response.json() as Resource;
    setItems((current) => current.map((item) => item.id === updated.id ? updated : item));
    setSelectedId(updated.id);
    await loadRevisions(updated.id);
    setMessage("Alterações salvas com nova versão.");
  }

  async function transition(id: string, action: string) {
    const response = await fetch(`/api/v1/admin/resources/${id}/${action}`, { method: "POST" });
    if (!response.ok) {
      setMessage(await errorText(response));
      return;
    }
    const updated = await response.json() as Resource;
    setItems((current) => current.map((item) => item.id === updated.id ? updated : item));
    if (selectedId === id) {
      setSelectedId(updated.id);
      await loadRevisions(updated.id);
    }
    setMessage(`Ação ${action} concluída.`);
  }

  return <div className="editor-grid">
    <section className="admin-panel">
      <div className="resource-toolbar">
        <label>Tipo <select value={kind} onChange={(event) => changeKind(event.target.value)}>{kinds.map((value) => <option key={value}>{value}</option>)}</select></label>
        {selected && <button type="button" className="action-button secondary" onClick={() => { setSelectedId(null); setRevisions([]); setMessage(""); }}>Novo conteúdo</button>}
      </div>
      <PublicationDestination kind={kind} />
      {kind === "MENU" && items.length > 0 && <MenuStructureOverview items={items} />}
      {listState === "LOADING" && <p role="status" aria-live="polite">Carregando conteúdo governado…</p>}
      {listState === "ERROR" && <div className="form-message error" role="alert">Não foi possível carregar este tipo de conteúdo. <button type="button" className="action-button secondary" onClick={() => setReloadToken((current) => current + 1)}>Tentar novamente</button></div>}
      {listState === "READY" && (items.length === 0 ? <div className="empty-state"><h3>Nenhum conteúdo deste tipo</h3><p>Crie um item no formulário ao lado.</p></div> : <div className="compact-list">{items.map((item) => <div className="compact-item" key={item.id}>
        <div>
          <strong>{item.title}</strong>
          <small style={{ display: "block" }}>{item.slug} · v{item.version}{scheduleLabel(item)}</small>
        </div>
        <div className="button-row">
          <StatusBadge status={item.status} />
          <button type="button" className="action-button secondary" onClick={() => void selectResource(item)} aria-label={`Editar ${item.title}`}>Editar</button>
          {item.status !== "PUBLISHED" && item.status !== "ARCHIVED" && <button type="button" className="action-button" onClick={() => void transition(item.id, "publish")}>Publicar</button>}
          {item.status !== "ARCHIVED" && <button type="button" className="action-button secondary" onClick={() => void transition(item.id, "archive")}>Arquivar</button>}
          {item.status === "ARCHIVED" && <button type="button" className="action-button secondary" onClick={() => void transition(item.id, "restore")}>Restaurar</button>}
        </div>
      </div>)}</div>)}
      {message && <div className="form-message" role="status">{message}</div>}
    </section>

    {selected ? <section className="admin-panel editor-fields">
      <form key={`${selected.id}-${selected.version}`} onSubmit={save} className="editor-fields">
        <div>
          <h2>Editar conteúdo</h2>
          <small>{selected.kind} · {selected.slug} · versão atual {selected.version}</small>
        </div>
        {selected.status === "ARCHIVED" && <div className="form-message">Este conteúdo está arquivado. Restaure-o antes de salvar alterações.</div>}
        <label className="field">Título<input name="title" required maxLength={220} defaultValue={selected.title} disabled={selected.status === "ARCHIVED"} /></label>
        <label className="field">Slug<input value={selected.slug} readOnly aria-readonly="true" /></label>
        <label className="field">Resumo<textarea name="summary" rows={3} maxLength={500} defaultValue={selected.summary} disabled={selected.status === "ARCHIVED"} /></label>
        <label className="field">Ordem<input name="displayOrder" type="number" defaultValue={selected.displayOrder} disabled={selected.status === "ARCHIVED"} /></label>
        <label className="field">Início de exibição<input name="startsAt" type="datetime-local" defaultValue={toDateTimeLocal(selected.startsAt)} disabled={selected.status === "ARCHIVED"} /><small>Opcional. Antes desta data, mesmo publicado, o item não aparece no portal.</small></label>
        <label className="field">Fim de exibição<input name="endsAt" type="datetime-local" defaultValue={toDateTimeLocal(selected.endsAt)} disabled={selected.status === "ARCHIVED"} /><small>Opcional. O item deixa de aparecer automaticamente após esta data.</small></label>
        <ResourcePayloadFields kind={selected.kind} payloadJson={selected.payloadJson || "{}"} disabled={selected.status === "ARCHIVED"} menuOptions={menuOptions} />
        <button className="action-button" disabled={selected.status === "ARCHIVED"}>Salvar alterações</button>
      </form>
      <section aria-labelledby="revision-history-title">
        <h2 id="revision-history-title">Histórico de revisões</h2>
        {revisions.length === 0 ? <p>Nenhuma revisão anterior registrada para este item.</p> : <div className="compact-list">{revisions.map((revision) => <div className="compact-item" key={revision.id}>
          <div>
            <strong>Versão {revision.version}</strong>
            <small style={{ display: "block" }}>{formatDateTime(revision.createdAt)} · ator {revision.createdBy}</small>
          </div>
          <details>
            <summary>Ver snapshot</summary>
            <pre style={{ whiteSpace: "pre-wrap", wordBreak: "break-word", maxWidth: "42rem" }}>{prettySnapshot(revision.snapshotJson)}</pre>
          </details>
        </div>)}</div>}
      </section>
    </section> : <form key={`new-${kind}`} className="admin-panel editor-fields" onSubmit={create}>
      <h2>Novo conteúdo</h2>
      <label className="field">Título<input name="title" required maxLength={220} /></label>
      <label className="field">Slug<input name="slug" required pattern="[a-z0-9-]+" /></label>
      <label className="field">Resumo<textarea name="summary" rows={3} maxLength={500} /></label>
      <label className="field">Ordem<input name="displayOrder" type="number" defaultValue={0} /></label>
      <label className="field">Início de exibição<input name="startsAt" type="datetime-local" /><small>Opcional. Permite preparar hoje e exibir somente a partir da data escolhida.</small></label>
      <label className="field">Fim de exibição<input name="endsAt" type="datetime-local" /><small>Opcional. Remove o item da área pública após a data escolhida sem apagar o histórico.</small></label>
      <ResourcePayloadFields kind={kind} payloadJson="{}" menuOptions={menuOptions} />
      <button className="action-button">Salvar rascunho</button>
    </form>}
  </div>;
}

function MenuStructureOverview({ items }: { items: Resource[] }) {
  const nodes = items.map((item) => ({ item, payload: parsePayload(item.payloadJson) }));
  const roots = nodes.filter((node) => !node.payload.parent || !items.some((item) => item.slug === node.payload.parent));
  return <section className="mb-4 rounded-xl border border-border bg-surface-soft p-4" aria-labelledby="menu-preview-title"><h3 id="menu-preview-title">Estrutura do menu</h3><p className="text-sm text-muted">Prévia hierárquica baseada no item superior e na ordem editorial.</p><ul className="mt-2 grid gap-2">{roots.sort(byDisplayOrder).map((node) => <MenuNode key={node.item.id} node={node} nodes={nodes} visited={new Set()} />)}</ul></section>;
}

function MenuNode({ node, nodes, visited }: { node: { item: Resource; payload: Record<string, unknown> }; nodes: Array<{ item: Resource; payload: Record<string, unknown> }>; visited: Set<string> }) {
  if (visited.has(node.item.slug)) return <li><strong>{node.item.title}</strong> <small className="text-muted">ciclo detectado</small></li>;
  const nextVisited = new Set(visited).add(node.item.slug);
  const children = nodes.filter((candidate) => candidate.payload.parent === node.item.slug).sort(byDisplayOrder);
  return <li className="rounded-lg border border-border bg-surface p-2"><div><strong>{String(node.payload.label || node.item.title)}</strong> <small className="text-muted">{String(node.payload.url || "sem destino")} · {node.item.status}</small></div>{children.length > 0 && <ul className="ml-5 mt-2 grid gap-2 border-l border-border pl-3">{children.map((child) => <MenuNode key={child.item.id} node={child} nodes={nodes} visited={nextVisited} />)}</ul>}</li>;
}

function byDisplayOrder(a: { item: Resource }, b: { item: Resource }) { return a.item.displayOrder - b.item.displayOrder || a.item.title.localeCompare(b.item.title, "pt-BR"); }
function parsePayload(value: string) { try { const parsed = JSON.parse(value) as unknown; return parsed && typeof parsed === "object" && !Array.isArray(parsed) ? parsed as Record<string, unknown> : {}; } catch { return {}; } }

function toIsoOrNull(value: FormDataEntryValue | null) {
  const normalized = typeof value === "string" ? value.trim() : "";
  if (!normalized) return null;
  const date = new Date(normalized);
  return Number.isNaN(date.getTime()) ? normalized : date.toISOString();
}

function toDateTimeLocal(value: string | null) {
  if (!value) return "";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "";
  const local = new Date(date.getTime() - date.getTimezoneOffset() * 60_000);
  return local.toISOString().slice(0, 16);
}

/** Diz, antes da publicação, onde o conteúdo deste tipo aparece — ou que ainda não aparece. */
function PublicationDestination({ kind }: { kind: string }) {
  const destination = publicDestination[kind];
  if (destination === null) {
    return <div className="warning-box" role="note">
      <strong>Este tipo ainda não tem destino público.</strong>
      <p>Nenhuma rota do portal lê conteúdo <code>{kind}</code> hoje. O registro é criado, versionado e auditado normalmente, mas não passa a ser exibido ao cidadão enquanto uma área pública não consumir este tipo.</p>
    </div>;
  }
  if (kind === "PAGE") {
    return <div className="ok-box" role="note">
      <strong>Publica em: páginas institucionais.</strong>
      <p>Uma página só aparece no portal quando seu slug corresponde a uma rota existente: {routedPageSlugs.join(", ")}. Outros slugs ficam governados aqui até que uma rota passe a lê-los.</p>
    </div>;
  }
  return <div className="ok-box" role="note"><strong>Publica em: {destination}.</strong></div>;
}

function scheduleLabel(resource: Resource) {
  if (!resource.startsAt && !resource.endsAt) return "";
  const start = resource.startsAt ? formatDateTime(resource.startsAt) : "agora";
  const end = resource.endsAt ? formatDateTime(resource.endsAt) : "sem término";
  return ` · exibição ${start} → ${end}`;
}

function formatDateTime(value: string) {
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? value : new Intl.DateTimeFormat("pt-BR", { dateStyle: "short", timeStyle: "short" }).format(date);
}

function prettySnapshot(value: string) {
  try { return JSON.stringify(JSON.parse(value), null, 2); } catch { return value; }
}

async function errorText(response: Response) {
  const body = await response.json().catch(() => null) as { title?: string; detail?: string; errors?: Record<string, string[]> } | null;
  const validation = Object.values(body?.errors ?? {}).flat().join(" ");
  return body?.detail ?? body?.title ?? (validation || `Erro ${response.status}`);
}
