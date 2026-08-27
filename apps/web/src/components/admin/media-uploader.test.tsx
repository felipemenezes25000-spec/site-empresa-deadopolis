import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { MediaUploader } from "./media-uploader";

describe("MediaUploader", () => {
  afterEach(() => vi.unstubAllGlobals());

  it("uploads every selected file through the governed endpoint", async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(new Response(JSON.stringify({ id: "one", status: "QUARANTINED", scan: { scannerState: "NOT_CONFIGURED" } }), { status: 201, headers: { "Content-Type": "application/json" } }))
      .mockResolvedValueOnce(new Response(JSON.stringify({ id: "two", status: "QUARANTINED", scan: { scannerState: "NOT_CONFIGURED" } }), { status: 201, headers: { "Content-Type": "application/json" } }));
    vi.stubGlobal("fetch", fetchMock);
    const uploaded = vi.fn();
    render(<MediaUploader onUploaded={uploaded} />);

    const files = [
      new File(["photo-one"], "foto-1.jpg", { type: "image/jpeg" }),
      new File(["photo-two"], "foto-2.jpg", { type: "image/jpeg" }),
    ];
    fireEvent.change(screen.getByLabelText("Arquivos"), { target: { files } });
    fireEvent.click(screen.getByRole("button", { name: "Enviar 2 arquivos" }));

    await screen.findByText(/2 arquivos recebidos/i);
    expect(fetchMock).toHaveBeenCalledTimes(2);
    expect(fetchMock).toHaveBeenNthCalledWith(1, "/api/v1/admin/media/upload", expect.objectContaining({ method: "POST" }));
    expect((fetchMock.mock.calls[0][1]?.body as FormData).get("file")).toEqual(files[0]);
    expect((fetchMock.mock.calls[1][1]?.body as FormData).get("file")).toEqual(files[1]);
    await waitFor(() => expect(uploaded).toHaveBeenCalledWith("two"));
  });
});
