import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { PortalHome } from "./portal-home";

const content = {
  municipality: {
    name: "Prefeitura Municipal de Deodápolis",
    slug: "deodapolis",
    stateCode: "MS",
    primaryColor: "#176B4D",
    logoObjectKey: null,
  },
  featuredServices: [
    {
      name: "Emitir guia do IPTU",
      slug: "emitir-guia-iptu",
      description: "Emita ou consulte a guia do IPTU.",
      area: "Tributos",
      isOnline: true,
      onlineUrl: "https://example.test/iptu",
    },
  ],
  latestNews: [
    {
      title: "Feira de serviços aproxima Prefeitura e moradores",
      slug: "feira-de-servicos",
      summary: "Atendimentos reunidos em um único local.",
      category: "PREFEITURA",
      coverImageUrl: null,
      coverImageAlt: null,
      isFeatured: true,
      publishedAt: "2026-08-25T12:00:00Z",
    },
  ],
  transparencyLinks: [
    {
      title: "Portal da Transparência",
      category: "Transparência",
      url: "https://example.test/transparencia",
      description: "Receitas e despesas públicas.",
    },
  ],
  integrations: [],
};

describe("PortalHome", () => {
  it("prioritizes service discovery with an accessible search form", () => {
    render(<PortalHome content={content} presentationMode />);

    expect(screen.getByRole("heading", { level: 1, name: /olá! o que você precisa/i })).toBeInTheDocument();
    expect(screen.getByRole("searchbox", { name: /buscar serviço/i })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /^buscar$/i })).toBeInTheDocument();
  });

  it("renders municipal navigation and real content links", () => {
    render(<PortalHome content={content} presentationMode />);

    expect(screen.getByRole("navigation", { name: /principal/i })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /emitir guia do iptu/i })).toHaveAttribute("href", "/servicos/emitir-guia-iptu");
    expect(screen.getByRole("link", { name: /portal da transparência/i })).toHaveAttribute("href", "https://example.test/transparencia");
    expect(screen.getByText("Ambiente de demonstração")).toBeInTheDocument();
  });
});
