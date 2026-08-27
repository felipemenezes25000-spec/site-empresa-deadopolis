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
  it("prioritizes service discovery with an accessible search form in fallback mode", () => {
    render(<PortalHome content={content} presentationMode />);

    expect(screen.getByRole("heading", { level: 1, name: /olá! o que você precisa/i })).toBeInTheDocument();
    expect(screen.getByRole("searchbox", { name: /buscar serviço/i })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /^buscar$/i })).toBeInTheDocument();
    expect(document.querySelector("main")).toHaveAttribute("data-home-composition", "fallback");
  });

  it("renders municipal navigation and real content links", () => {
    render(<PortalHome content={content} presentationMode />);

    expect(screen.getByRole("navigation", { name: /principal/i })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /emitir guia do iptu/i })).toHaveAttribute("href", "/servicos/emitir-guia-iptu");
    expect(screen.getByRole("link", { name: /portal da transparência/i })).toHaveAttribute("href", "https://example.test/transparencia");
    expect(screen.getByText("Ambiente de demonstração")).toBeInTheDocument();
  });

  it("renders an internal featured-news cover with responsive WebP variants", () => {
    const coverImageUrl = "/api/v1/media/11111111-1111-1111-1111-111111111111";
    const coverImageAlt = "Atendimento da feira municipal";
    const { container } = render(<PortalHome content={{
      ...content,
      latestNews: [{ ...content.latestNews[0], coverImageUrl, coverImageAlt }],
    }} />);

    const image = screen.getByRole("img", { name: coverImageAlt });
    expect(image).toHaveAttribute("src", coverImageUrl);
    expect(image).toHaveAttribute("width", "720");
    expect(image).toHaveAttribute("height", "720");

    const webpSource = container.querySelector("source[type='image/webp']");
    expect(webpSource).not.toBeNull();
    expect(webpSource).toHaveAttribute("sizes", "(max-width: 760px) 100vw, 420px");
    expect(webpSource?.getAttribute("srcset")).toContain(
      `${coverImageUrl}/variant?width=480&height=480&format=webp 480w`,
    );
    expect(webpSource?.getAttribute("srcset")).toContain(
      `${coverImageUrl}/variant?width=1200&height=1200&format=webp 1200w`,
    );
  });

  it("keeps the municipal placeholder for third-party cover URLs", () => {
    render(<PortalHome content={{
      ...content,
      latestNews: [{
        ...content.latestNews[0],
        coverImageUrl: "https://images.example.test/featured.jpg",
        coverImageAlt: "Imagem externa não autorizada",
      }],
    }} />);

    expect(screen.queryByRole("img", { name: "Imagem externa não autorizada" })).not.toBeInTheDocument();
    expect(screen.getByText("DEO")).toBeInTheDocument();
    expect(screen.getByText("Informação municipal")).toBeInTheDocument();
  });

  it("uses the published CMS block list to control home sections", () => {
    render(<PortalHome content={content} homeLayout={{ blocks: [
      { id: "news-first", type: "NewsGrid", title: "Atualizações do município", enabled: true },
      { id: "alert", type: "Alert", title: "Aviso da página inicial", content: "Conteúdo administrado pela Comunicação.", enabled: true },
      { id: "services", type: "ServiceGrid", title: "Serviços escolhidos pela Comunicação", enabled: true },
      { id: "hidden-search", type: "ServiceSearch", enabled: false },
    ] }} />);

    expect(document.querySelector("main")).toHaveAttribute("data-home-composition", "cms");
    expect(screen.getByRole("heading", { name: "Atualizações do município" })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Aviso da página inicial" })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Serviços escolhidos pela Comunicação" })).toBeInTheDocument();
    expect(screen.queryByRole("searchbox", { name: /buscar serviço/i })).not.toBeInTheDocument();

    const news = screen.getByRole("heading", { name: "Atualizações do município" });
    const services = screen.getByRole("heading", { name: "Serviços escolhidos pela Comunicação" });
    expect(news.compareDocumentPosition(services) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
  });

  it("rejects unknown CMS block types instead of rendering arbitrary structures", () => {
    render(<PortalHome content={content} homeLayout={{ blocks: [
      { id: "unsafe", type: "ArbitraryHtml", title: "Não deve aparecer", enabled: true },
    ] }} />);

    expect(document.querySelector("main")).toHaveAttribute("data-home-composition", "fallback");
    expect(screen.queryByText("Não deve aparecer")).not.toBeInTheDocument();
    expect(screen.getByRole("searchbox", { name: /buscar serviço/i })).toBeInTheDocument();
  });
});
