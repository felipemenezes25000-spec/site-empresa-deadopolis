import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { TicketTracker } from "./ticket-tracker";

const tracked = {
  protocol: "DEO-20260827-A1B2C3D4",
  category: "Reclamação",
  status: "IN_PROGRESS",
  openedAt: "2026-08-27T12:00:00Z",
  firstResponseDueAt: "2026-08-27T20:00:00Z",
  resolutionDueAt: "2026-08-29T04:00:00Z",
  firstResponseAt: "2026-08-27T15:30:00Z",
  resolvedAt: null,
  comments: [{ body: "A equipe de obras avaliou o pedido no local.", createdAt: "2026-08-27T15:30:00Z" }],
};

describe("TicketTracker", () => {
  afterEach(() => vi.unstubAllGlobals());

  it("shows the governed status and published answers for a valid protocol and code", async () => {
    const fetchMock = vi.fn().mockResolvedValue(Response.json(tracked));
    vi.stubGlobal("fetch", fetchMock);
    render(<TicketTracker />);

    fireEvent.change(screen.getByLabelText("Protocolo"), { target: { value: tracked.protocol } });
    fireEvent.change(screen.getByLabelText("Código de acompanhamento"), { target: { value: "abc123" } });
    fireEvent.click(screen.getByRole("button", { name: "Consultar manifestação" }));

    expect(await screen.findByText(`Manifestação ${tracked.protocol}`)).toBeInTheDocument();
    expect(screen.getByText("Em atendimento")).toBeInTheDocument();
    expect(screen.getByText("A equipe de obras avaliou o pedido no local.")).toBeInTheDocument();
    await waitFor(() => expect(fetchMock).toHaveBeenCalledWith(`/api/v1/tickets/${encodeURIComponent(tracked.protocol)}?code=abc123`, expect.objectContaining({ signal: expect.any(AbortSignal) })));
  });

  it("never reveals a manifestation when the tracking code does not match", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response(null, { status: 404 })));
    render(<TicketTracker />);

    fireEvent.change(screen.getByLabelText("Protocolo"), { target: { value: tracked.protocol } });
    fireEvent.change(screen.getByLabelText("Código de acompanhamento"), { target: { value: "codigo-errado" } });
    fireEvent.click(screen.getByRole("button", { name: "Consultar manifestação" }));

    expect(await screen.findByRole("alert")).toHaveTextContent("Nenhuma manifestação corresponde ao protocolo e código informados.");
    expect(screen.queryByText(/^Manifestação DEO-/)).not.toBeInTheDocument();
  });
});
