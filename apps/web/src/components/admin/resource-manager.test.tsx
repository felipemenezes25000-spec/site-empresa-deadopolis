import { fireEvent, render, screen } from "@testing-library/react";
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
});
