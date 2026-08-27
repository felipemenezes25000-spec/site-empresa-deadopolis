import { fireEvent, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { PageBlockBuilder } from "./page-block-builder";

describe("PageBlockBuilder", () => {
  beforeEach(() => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response(JSON.stringify([{
      id: "11111111-1111-1111-1111-111111111111",
      originalFileName: "vacinacao.jpg",
      mimeType: "image/jpeg",
      altText: "Equipe de vacinação",
      status: "APPROVED",
    }]), { status: 200, headers: { "Content-Type": "application/json" } })));
  });

  afterEach(() => vi.unstubAllGlobals());

  it("offers type-specific banner fields and approved media reuse", async () => {
    render(<PageBlockBuilder initialBlocks={[{
      id: "banner-1",
      type: "Banner",
      title: "Vacinação",
      content: "Confira o calendário.",
      reference: "/servicos/vacinacao",
      imageUrl: "",
      imageAlt: "",
      linkLabel: "Ver calendário",
      items: [],
      enabled: true,
    }]} />);

    expect(screen.getByRole("textbox", { name: "URL da imagem interna" })).toBeInTheDocument();
    expect(screen.getByRole("textbox", { name: "Texto alternativo da imagem" })).toBeInTheDocument();
    expect(screen.getByRole("textbox", { name: "Texto do botão" })).toHaveValue("Ver calendário");
    expect(await screen.findByRole("option", { name: "vacinacao.jpg — Equipe de vacinação" })).toBeInTheDocument();
  });

  it("adds structured statistic items without requiring JSON editing", () => {
    render(<PageBlockBuilder initialBlocks={[{
      id: "stats-1",
      type: "Statistics",
      title: "Indicadores",
      content: "",
      reference: "",
      imageUrl: "",
      imageAlt: "",
      linkLabel: "",
      items: [],
      enabled: true,
    }]} />);

    fireEvent.click(screen.getByRole("button", { name: "Adicionar indicador" }));

    expect(screen.getByRole("textbox", { name: "Nome do indicador 1" })).toBeInTheDocument();
    expect(screen.getByRole("textbox", { name: "Valor do indicador 1" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Remover item 1" })).toBeInTheDocument();
    expect(screen.getByText("Pré-visualização do bloco")).toBeInTheDocument();
  });
});
