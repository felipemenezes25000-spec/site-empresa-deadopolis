import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { TicketManager } from "./ticket-manager";

const summary = {
  id: "ticket-1",
  protocol: "DEO-20260827-A1B2C3D4",
  requesterName: "Pessoa Demonstração",
  category: "Reclamação",
  priority: "Normal",
  status: "OPEN",
  openedAt: "2026-08-27T12:00:00Z",
  firstResponseDueAt: "2026-08-27T20:00:00Z",
  resolutionDueAt: "2026-08-29T04:00:00Z",
  firstResponseAt: null,
  resolvedAt: null,
};

const detail = {
  ...summary,
  contact: "cidada@example.test",
  description: "Buraco na via principal do bairro há duas semanas.",
  comments: [{ id: "comment-1", body: "Encaminhado à Secretaria de Obras.", isInternal: true, createdAt: "2026-08-27T13:00:00Z", author: "Administração Demo" }],
};

function stubApi(overrides: (url: string, options?: RequestInit) => Response | null = () => null) {
  const fetchMock = vi.fn().mockImplementation((url: string, options?: RequestInit) => {
    const custom = overrides(url, options);
    if (custom) return Promise.resolve(custom);
    if (url.endsWith("/sla/violations")) return Promise.resolve(Response.json([{ id: summary.id, protocol: summary.protocol, firstResponseBreached: true, resolutionBreached: false }]));
    if (url.endsWith(`/admin/tickets/${summary.id}`)) return Promise.resolve(Response.json(detail));
    if (url.endsWith("/admin/tickets")) return Promise.resolve(Response.json([summary]));
    return Promise.resolve(Response.json({}, { status: 201 }));
  });
  vi.stubGlobal("fetch", fetchMock);
  return fetchMock;
}

describe("TicketManager", () => {
  afterEach(() => vi.unstubAllGlobals());

  it("opens the manifestation and separates internal notes from citizen answers", async () => {
    stubApi();
    render(<TicketManager />);

    expect(await screen.findByText(summary.protocol)).toBeInTheDocument();
    expect(screen.getByText("SLA estourado")).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Atender" }));

    expect(await screen.findByText(detail.description)).toBeInTheDocument();
    expect(screen.getByText("cidada@example.test")).toBeInTheDocument();
    expect(screen.getByText("Nota interna")).toBeInTheDocument();
    expect(screen.getByText("Encaminhado à Secretaria de Obras.")).toBeInTheDocument();
  });

  it("publishes an answer visible to the citizen and reports the governed outcome", async () => {
    const fetchMock = stubApi();
    render(<TicketManager />);

    fireEvent.click(await screen.findByRole("button", { name: "Atender" }));
    fireEvent.change(await screen.findByLabelText("Texto da resposta"), { target: { value: "Serviço programado para a próxima semana." } });
    fireEvent.click(screen.getByRole("button", { name: "Registrar" }));

    expect(await screen.findByText("Resposta publicada no acompanhamento do cidadão.")).toBeInTheDocument();
    await waitFor(() => expect(fetchMock).toHaveBeenCalledWith(
      `/api/v1/admin/tickets/${summary.id}/comments`,
      expect.objectContaining({ method: "POST", body: JSON.stringify({ body: "Serviço programado para a próxima semana.", internal: false }) })));
  });

  it("recalculates the SLA when the priority changes", async () => {
    const fetchMock = stubApi();
    render(<TicketManager />);

    fireEvent.click(await screen.findByRole("button", { name: "Atender" }));
    fireEvent.change(await screen.findByLabelText(`Prioridade de ${summary.protocol}`), { target: { value: "CRITICAL" } });

    expect(await screen.findByText("Prioridade alterada e prazos de SLA recalculados.")).toBeInTheDocument();
    await waitFor(() => expect(fetchMock).toHaveBeenCalledWith(
      `/api/v1/admin/tickets/${summary.id}/priority`,
      expect.objectContaining({ method: "POST", body: JSON.stringify({ priority: "CRITICAL" }) })));
  });

  it("surfaces a recoverable error instead of an empty queue", async () => {
    stubApi((url) => url.endsWith("/admin/tickets") ? new Response(null, { status: 500 }) : null);
    render(<TicketManager />);

    expect(await screen.findByRole("alert")).toHaveTextContent("Não foi possível carregar a fila.");
    expect(screen.queryByText("Nenhum ticket nesta visão")).not.toBeInTheDocument();
  });
});
