import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { PageBlockRenderer } from "./page-block-renderer";

describe("PageBlockRenderer", () => {
  it("renders enabled visual blocks and ignores disabled ones", () => {
    render(<PageBlockRenderer payload={{ blocks: [
      { id: "hero", type: "Hero", title: "Deodápolis digital", content: "Serviços mais perto do cidadão.", enabled: true },
      { id: "hidden", type: "Alert", title: "Oculto", enabled: false },
    ] }} />);
    expect(screen.getByRole("heading", { name: "Deodápolis digital" })).toBeInTheDocument();
    expect(screen.queryByText("Oculto")).not.toBeInTheDocument();
  });

  it("renders service search as an accessible search form", () => {
    render(<PageBlockRenderer payload={{ blocks: [{ id: "services", type: "ServiceSearch", title: "Busque seu serviço", enabled: true }] }} />);
    expect(screen.getByRole("search")).toHaveAttribute("action", "/buscar");
    expect(screen.getByRole("searchbox", { name: "Buscar serviço" })).toBeInTheDocument();
  });

  it("renders a media-backed banner with a responsive internal image", () => {
    const imageUrl = "/api/v1/media/11111111-1111-1111-1111-111111111111";
    const { container } = render(<PageBlockRenderer payload={{ blocks: [{
      id: "banner",
      type: "Banner",
      title: "Campanha de vacinação",
      content: "Confira os locais e horários.",
      imageUrl,
      imageAlt: "Profissional preparando uma dose de vacina",
      reference: "/servicos/vacinacao",
      linkLabel: "Ver locais",
      enabled: true,
    }] }} />);

    expect(screen.getByRole("img", { name: "Profissional preparando uma dose de vacina" })).toHaveAttribute("src", imageUrl);
    expect(screen.getByRole("link", { name: "Ver locais" })).toHaveAttribute("href", "/servicos/vacinacao");
    expect(container.querySelector("source[type='image/webp']")?.getAttribute("srcset")).toContain(`${imageUrl}/variant`);
  });

  it("renders structured statistics, events and documents without unsafe links", () => {
    render(<PageBlockRenderer payload={{ blocks: [
      {
        id: "stats",
        type: "Statistics",
        title: "Indicadores do atendimento",
        items: [{ id: "resolved", label: "Solicitações resolvidas", value: "94%", description: "Últimos 30 dias" }],
        enabled: true,
      },
      {
        id: "events",
        type: "Events",
        title: "Próximos eventos",
        items: [{ id: "fair", label: "Feira municipal", date: "2026-09-12", url: "/agenda/feira-municipal", description: "Praça central" }],
        enabled: true,
      },
      {
        id: "documents",
        type: "Documents",
        title: "Documentos úteis",
        items: [
          { id: "safe", label: "Edital de convocação", url: "/api/v1/public-documents/22222222-2222-2222-2222-222222222222/download" },
          { id: "unsafe", label: "Link inseguro", url: "javascript:alert(1)" },
        ],
        enabled: true,
      },
    ] }} />);

    expect(screen.getByText("94%")).toBeInTheDocument();
    expect(screen.getByText("Solicitações resolvidas")).toBeInTheDocument();
    expect(screen.getByText("12/09/2026")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /feira municipal/i })).toHaveAttribute("href", "/agenda/feira-municipal");
    expect(screen.getByRole("link", { name: "Edital de convocação" })).toHaveAttribute("href", "/api/v1/public-documents/22222222-2222-2222-2222-222222222222/download");
    expect(screen.queryByRole("link", { name: "Link inseguro" })).not.toBeInTheDocument();
    expect(screen.getByText("Link inseguro")).toBeInTheDocument();
  });

  it("renders governed gallery media and ignores unknown block types", () => {
    render(<PageBlockRenderer payload={{ blocks: [
      {
        id: "gallery",
        type: "Gallery",
        title: "Galeria da obra",
        items: [
          { id: "photo", label: "Nova praça", mediaUrl: "/api/v1/media/33333333-3333-3333-3333-333333333333", mediaAlt: "Praça revitalizada" },
          { id: "external", label: "Imagem externa", mediaUrl: "https://images.example.test/photo.jpg", mediaAlt: "Não deve renderizar" },
        ],
        enabled: true,
      },
      { id: "unknown", type: "ArbitraryHtml", title: "Bloco desconhecido", enabled: true },
    ] }} />);

    expect(screen.getByRole("img", { name: "Praça revitalizada" })).toBeInTheDocument();
    expect(screen.queryByRole("img", { name: "Não deve renderizar" })).not.toBeInTheDocument();
    expect(screen.queryByText("Bloco desconhecido")).not.toBeInTheDocument();
  });
});
