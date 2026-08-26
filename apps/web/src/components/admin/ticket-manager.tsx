"use client";

import { useEffect, useState } from "react";

type Ticket = { id: string; protocol: string; requesterName: string; category: string; priority: string; status: string; openedAt: string; firstResponseDueAt: string; resolutionDueAt: string };

export function TicketManager() {
  const [items, setItems] = useState<Ticket[]>([]);
  const [violations, setViolations] = useState<Ticket[]>([]);
  const [message, setMessage] = useState("");

  useEffect(() => {
    const controller = new AbortController();
    void Promise.all([
      fetch("/api/v1/admin/tickets", { signal: controller.signal }),
      fetch("/api/v1/admin/tickets/sla/violations", { signal: controller.signal }),
    ]).then(async ([ticketsResponse, violationsResponse]) => {
      if (controller.signal.aborted) return;
      if (ticketsResponse.ok) setItems(await ticketsResponse.json() as Ticket[]);
      if (violationsResponse.ok) setViolations(await violationsResponse.json() as Ticket[]);
    }).catch(() => undefined);
    return () => controller.abort();
  }, []);

  async function load() {
    const [ticketsResponse, violationsResponse] = await Promise.all([fetch("/api/v1/admin/tickets"), fetch("/api/v1/admin/tickets/sla/violations")]);
    if (ticketsResponse.ok) setItems(await ticketsResponse.json() as Ticket[]);
    if (violationsResponse.ok) setViolations(await violationsResponse.json() as Ticket[]);
  }

  async function comment(id: string) {
    const body = window.prompt("Resposta ao solicitante (ficará visível no acompanhamento):");
    if (!body) return;
    const response = await fetch(`/api/v1/admin/tickets/${id}/comments`, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ body, internal: false }) });
    setMessage(response.ok ? "Resposta registrada." : `Erro ${response.status}`);
    await load();
  }

  async function resolve(id: string) {
    const response = await fetch(`/api/v1/admin/tickets/${id}/resolve`, { method: "POST" });
    setMessage(response.ok ? "Ticket resolvido." : `Erro ${response.status}`);
    await load();
  }

  return <>
    <section className="admin-panel"><h2>Fila de atendimento</h2>{items.length === 0 ? <div className="empty-state"><h3>Nenhum ticket aberto</h3><p>Novas manifestações aparecerão aqui.</p></div> : <table className="admin-table"><thead><tr><th>Protocolo</th><th>Solicitante</th><th>Prioridade</th><th>Status</th><th>SLA resolução</th><th>Ações</th></tr></thead><tbody>{items.map((item) => <tr key={item.id}><td>{item.protocol}<br /><small>{item.category}</small></td><td>{item.requesterName}</td><td>{item.priority}</td><td><span className="status-pill">{item.status}</span></td><td>{new Intl.DateTimeFormat("pt-BR", { dateStyle: "short", timeStyle: "short" }).format(new Date(item.resolutionDueAt))}</td><td><div className="button-row"><button type="button" className="action-button secondary" onClick={() => comment(item.id)}>Responder</button><button type="button" className="action-button" onClick={() => resolve(item.id)} disabled={item.status === "RESOLVED"}>Resolver</button></div></td></tr>)}</tbody></table>}{message && <div className="form-message">{message}</div>}</section>
    <section className="admin-panel"><h2>Violações de SLA</h2>{violations.length === 0 ? <div className="ok-box">Nenhuma violação identificada agora.</div> : <div className="warning-box">{violations.length} ticket(s) exigem atenção imediata.</div>}</section>
  </>;
}
