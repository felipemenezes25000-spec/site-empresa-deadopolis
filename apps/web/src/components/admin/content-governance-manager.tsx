"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { StatusBadge } from "@/components/ui";

type CalendarItem = { id: string; kind: string; title: string; status: string; actionAt: string; publishedAt: string | null; endsAt: string | null; publicUrl: string | null; adminUrl: string };
type CalendarResponse = { from: string; to: string; items: CalendarItem[] };
type StaleItem = { id: string; kind: string; title: string; slug: string; status: string; lastReviewedAt: string; ownerName: string | null; daysSinceReview: number };
type StaleResponse = { thresholdDays: number; cutoff: string; count: number; unassigned: number; items: StaleItem[] };

export function ContentGovernanceManager() {
  const [calendar, setCalendar] = useState<CalendarResponse | null>(null);
  const [stale, setStale] = useState<StaleResponse | null>(null);
  const [days, setDays] = useState(180);
  const [message, setMessage] = useState("");

  useEffect(() => {
    const controller = new AbortController();
    void Promise.all([
      fetch("/api/v1/admin/content-governance/calendar", { signal: controller.signal }),
      fetch(`/api/v1/admin/content-governance/stale?days=${days}`, { signal: controller.signal }),
    ]).then(async ([calendarResponse, staleResponse]) => {
      if (!controller.signal.aborted && calendarResponse.ok) setCalendar(await calendarResponse.json() as CalendarResponse);
      if (!controller.signal.aborted && staleResponse.ok) setStale(await staleResponse.json() as StaleResponse);
    }).catch(() => { if (!controller.signal.aborted) setMessage("Não foi possível carregar a governança de conteúdo."); });
    return () => controller.abort();
  }, [days]);

  return <div className="grid gap-6">
    {message && <div className="form-message" role="status">{message}</div>}
    <section className="admin-panel">
      <div className="flex flex-wrap items-end justify-between gap-3"><div><h2>Calendário unificado de publicação</h2><p>Notícias, páginas/blocos e Diário Oficial na mesma linha do tempo.</p></div>{calendar && <small>{formatDate(calendar.from)} → {formatDate(calendar.to)}</small>}</div>
      {!calendar ? <p aria-busy="true">Carregando calendário…</p> : calendar.items.length === 0 ? <div className="empty-state"><h3>Nenhuma publicação no período</h3><p>O calendário será preenchido por publicações e agendamentos reais.</p></div> : <div className="compact-list">{calendar.items.map((item) => <div className="compact-item" key={`${item.kind}-${item.id}-${item.actionAt}`}><div><strong>{item.title}</strong><small style={{ display: "block" }}>{item.kind} · {formatDateTime(item.actionAt)}{item.endsAt ? ` · encerra ${formatDateTime(item.endsAt)}` : ""}</small></div><div className="button-row"><StatusBadge status={item.status} />{item.publicUrl && <Link className="action-button secondary" href={item.publicUrl}>Ver público</Link>}<Link className="action-button secondary" href={item.adminUrl}>Administrar</Link></div></div>)}</div>}
    </section>

    <section className="admin-panel">
      <div className="flex flex-wrap items-end justify-between gap-3"><div><h2>Conteúdo desatualizado</h2><p>Itens que ultrapassaram o prazo de revisão editorial.</p></div><label className="field">Revisão vencida após<input type="number" min={30} max={730} value={days} onChange={(event) => setDays(Math.min(730, Math.max(30, Number(event.target.value) || 180)))} /><small>dias</small></label></div>
      {stale && <div className="mb-3 flex flex-wrap gap-3"><span className="status-pill">{stale.count} pendentes</span><span className="status-pill">{stale.unassigned} sem responsável identificável</span></div>}
      {!stale ? <p aria-busy="true">Carregando revisão…</p> : stale.items.length === 0 ? <div className="empty-state"><h3>Revisão em dia</h3><p>Nenhum conteúdo excedeu o limite configurado.</p></div> : <div className="compact-list">{stale.items.map((item) => <div className="compact-item" key={`${item.kind}-${item.id}`}><div><strong>{item.title}</strong><small style={{ display: "block" }}>{item.kind} · {item.daysSinceReview} dias sem revisão · responsável: {item.ownerName ?? "não atribuído"}</small></div><StatusBadge status={item.status} /></div>)}</div>}
    </section>
  </div>;
}

function formatDate(value: string) { const date = new Date(value); return Number.isNaN(date.getTime()) ? value : new Intl.DateTimeFormat("pt-BR", { dateStyle: "short" }).format(date); }
function formatDateTime(value: string) { const date = new Date(value); return Number.isNaN(date.getTime()) ? value : new Intl.DateTimeFormat("pt-BR", { dateStyle: "short", timeStyle: "short" }).format(date); }
