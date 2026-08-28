"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { StatusBadge } from "@/components/ui";

type Article = { id: string; title: string; slug: string; status: string; version: number; updatedAt: string; scheduledFor: string | null };

export function NewsList() {
  const [items, setItems] = useState<Article[]>([]);
  const [listState, setListState] = useState<"LOADING" | "READY" | "ERROR">("LOADING");
  const [error, setError] = useState("");
  const [reloadToken, setReloadToken] = useState(0);

  useEffect(() => {
    const controller = new AbortController();
    void fetch("/api/v1/admin/news", { signal: controller.signal })
      .then(async (response) => {
        if (!response.ok) throw new Error(`Erro ${response.status}`);
        const next = await response.json() as Article[];
        if (controller.signal.aborted) return;
        setItems(next);
        setListState("READY");
      })
      .catch((reason) => {
        if (controller.signal.aborted) return;
        setError(reason instanceof Error ? reason.message : "Falha");
        setListState("ERROR");
      });
    return () => controller.abort();
  }, [reloadToken]);

  return <section className="admin-panel">
    <div className="admin-heading"><div><h2>Publicações editoriais</h2><p>Rascunhos, revisão, agenda e publicações recentes.</p></div><Link className="action-button" href="/admin/noticias/nova">+ Nova notícia</Link></div>
    {listState === "LOADING" && <p role="status" aria-live="polite">Carregando publicações editoriais…</p>}
    {listState === "ERROR" && <div className="form-message error" role="alert">Não foi possível carregar as publicações ({error}). <button type="button" className="action-button secondary" onClick={() => setReloadToken((current) => current + 1)}>Tentar novamente</button></div>}
    {listState === "READY" && items.length === 0 && <div className="empty-state"><h3>Nenhuma notícia</h3><p>Crie a primeira publicação pelo editor.</p></div>}
    {listState === "READY" && items.length > 0 && <div className="table-scroll"><table className="admin-table">
      <thead><tr><th>Título</th><th>Status</th><th>Versão</th><th>Atualização</th><th>Ações</th></tr></thead>
      <tbody>{items.map((item) => <tr key={item.id}>
        <td><strong>{item.title}</strong><br /><small>/{item.slug}</small></td>
        <td><StatusBadge status={item.status} />{item.scheduledFor && <><br /><small>Agendada para {formatDate(item.scheduledFor)}</small></>}</td>
        <td>{item.version}</td>
        <td>{formatDate(item.updatedAt)}</td>
        <td><Link className="action-button secondary" href={`/admin/noticias/${item.id}`}>Editar</Link></td>
      </tr>)}</tbody>
    </table></div>}
  </section>;
}

function formatDate(value: string) {
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? value : new Intl.DateTimeFormat("pt-BR", { dateStyle: "short", timeStyle: "short" }).format(date);
}
