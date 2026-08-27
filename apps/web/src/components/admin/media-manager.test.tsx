import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { MediaManager } from "./media-manager";

const asset = {
  id: "11111111-1111-1111-1111-111111111111",
  originalFileName: "vacinacao.jpg",
  mimeType: "image/jpeg",
  sizeBytes: 1024,
  sha256: "a".repeat(64),
  status: "APPROVED",
  altText: "Equipe de vacinação",
  caption: "",
  credit: "",
  tagsCsv: "saúde",
  uploadedAt: "2026-08-27T12:00:00Z",
};

describe("MediaManager", () => {
  afterEach(() => vi.unstubAllGlobals());

  it("loads paginated media and requests the next page", async () => {
    const response = () => new Response(JSON.stringify([asset]), { status: 200, headers: { "Content-Type": "application/json", "X-Total-Count": "25" } });
    const fetchMock = vi.fn().mockImplementation(() => Promise.resolve(response()));
    vi.stubGlobal("fetch", fetchMock);
    render(<MediaManager />);

    expect(await screen.findByText("vacinacao.jpg")).toBeInTheDocument();
    expect(screen.getByText("Página 1 de 2 · 25 itens")).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Próxima página" }));

    await waitFor(() => expect(fetchMock).toHaveBeenCalledWith(
      expect.stringContaining("page=2"),
      expect.objectContaining({ signal: expect.any(AbortSignal) }),
    ));
  });
});
