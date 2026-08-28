import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { ResourceManager } from "./resource-manager";

describe("ResourceManager", () => {
  beforeEach(() => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response(JSON.stringify([]), {
      status: 200,
      headers: { "Content-Type": "application/json" },
    })));
  });

  afterEach(() => vi.unstubAllGlobals());

  it("reports a failed listing instead of showing an empty catalogue", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response(null, { status: 500 })));
    render(<ResourceManager />);

    expect(await screen.findByRole("alert")).toHaveTextContent("Não foi possível carregar este tipo de conteúdo.");
    expect(screen.queryByText("Nenhum conteúdo deste tipo")).not.toBeInTheDocument();
  });

  it("offers a structured page form without exposing raw JSON", async () => {
    render(<ResourceManager />);

    expect(await screen.findByRole("heading", { name: "Novo conteúdo" })).toBeInTheDocument();
    expect(screen.getByRole("textbox", { name: "Conteúdo da página" })).toBeInTheDocument();
    expect(screen.getByRole("textbox", { name: "Seções da página" })).toBeInTheDocument();
    expect(screen.queryByRole("textbox", { name: /detalhes estruturados.*json/i })).not.toBeInTheDocument();
  });

  it("changes the payload fields when the editorial type changes", async () => {
    render(<ResourceManager />);
    const kind = await screen.findByRole("combobox", { name: "Tipo" });

    fireEvent.change(kind, { target: { value: "BANNER" } });

    expect(screen.getByRole("textbox", { name: "URL da imagem" })).toBeInTheDocument();
    expect(screen.getByRole("textbox", { name: "Texto alternativo da imagem" })).toBeInTheDocument();
    expect(screen.getByRole("textbox", { name: "Texto do botão" })).toBeInTheDocument();
    expect(screen.getByRole("textbox", { name: "Destino do botão" })).toBeInTheDocument();
  });

  it("reloads and selects a resource after creating its governed draft", async () => {
    const created = {
      id: "page-home",
      kind: "PAGE",
      slug: "home",
      title: "Página inicial governada",
      summary: "Composição do portal",
      payloadJson: "{\"blocks\":[]}",
      status: "DRAFT",
      displayOrder: 0,
      startsAt: null,
      endsAt: null,
      publishedAt: null,
      version: 1,
      updatedAt: "2026-08-27T12:00:00Z",
      updatedBy: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    };
    let resourceReads = 0;
    const fetchMock = vi.fn().mockImplementation((url: string, options?: RequestInit) => {
      if (url.includes("/admin/media")) return Promise.resolve(Response.json([]));
      if (url.includes("/revisions")) return Promise.resolve(Response.json([]));
      if (options?.method === "POST") return Promise.resolve(Response.json(created, { status: 201 }));
      if (url.includes("/admin/resources")) {
        resourceReads++;
        return Promise.resolve(Response.json(resourceReads > 1 ? [created] : []));
      }
      return Promise.resolve(new Response(null, { status: 404 }));
    });
    vi.stubGlobal("fetch", fetchMock);
    render(<ResourceManager />);

    fireEvent.change(screen.getByLabelText("Título"), { target: { value: created.title } });
    fireEvent.change(screen.getByLabelText("Slug"), { target: { value: created.slug } });
    fireEvent.click(screen.getByRole("button", { name: "Salvar rascunho" }));

    expect(await screen.findByText("Conteúdo criado como rascunho.")).toBeInTheDocument();
    await waitFor(() => expect(fetchMock).toHaveBeenCalledWith("/api/v1/admin/resources?kind=PAGE"));
    expect(await screen.findByRole("heading", { name: "Editar conteúdo" })).toBeInTheDocument();
    expect(screen.getByDisplayValue(created.title)).toBeInTheDocument();
  });
});
