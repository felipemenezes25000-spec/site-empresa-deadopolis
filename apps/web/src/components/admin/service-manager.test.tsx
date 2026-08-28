import { render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { ServiceManager } from "./service-manager";

describe("ServiceManager", () => {
  afterEach(() => vi.unstubAllGlobals());

  it("lists the governed catalogue when the API answers", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(Response.json([
      { id: "one", name: "Emitir guia do IPTU", slug: "emitir-guia-iptu", area: "Tributos", status: "PUBLISHED", isFeatured: true },
    ])));
    render(<ServiceManager />);

    expect(await screen.findByText("Emitir guia do IPTU")).toBeInTheDocument();
    expect(screen.getByText("Tributos · /emitir-guia-iptu")).toBeInTheDocument();
    expect(screen.queryByRole("alert")).not.toBeInTheDocument();
  });

  it("never presents a failed listing as an empty catalogue", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response(null, { status: 503 })));
    render(<ServiceManager />);

    expect(await screen.findByRole("alert")).toHaveTextContent("Não foi possível carregar o catálogo.");
    expect(screen.queryByText("Nenhum serviço cadastrado")).not.toBeInTheDocument();
  });

  it("states the empty catalogue explicitly when the API really returns nothing", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(Response.json([])));
    render(<ServiceManager />);

    expect(await screen.findByText("Nenhum serviço cadastrado")).toBeInTheDocument();
    expect(screen.queryByRole("alert")).not.toBeInTheDocument();
  });
});
