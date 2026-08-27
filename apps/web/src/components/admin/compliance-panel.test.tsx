import { render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { CompliancePanel } from "./compliance-panel";

describe("CompliancePanel", () => {
  afterEach(() => vi.unstubAllGlobals());

  it("renders factual runtime states and evidence counts", async () => {
    const fetchMock = vi.fn().mockImplementation((url: string) => Promise.resolve(Response.json(url.endsWith("/compliance") ? {
      generatedAt: "2026-08-27T18:00:00Z",
      readiness: { state: "READY", databaseReady: true },
      providers: {
        storage: { state: "DEVELOPMENT_ONLY", description: "Storage local de apresentação." },
        digitalSignature: { state: "DEMO_ONLY", description: "Sem valor ICP-Brasil." },
        timestamp: { state: "NOT_CONFIGURED", description: "Provider externo necessário." },
        institutionalEmail: { state: "DEMO_ONLY", description: "Nenhuma caixa externa criada." },
        malwareScanner: { state: "DEMO_ONLY", description: "Scanner de demonstração." },
        mediaVariants: { webp: { state: "AVAILABLE", detail: "Codec operacional." }, avif: { state: "UNAVAILABLE", detail: "Codec indisponível." } },
      },
      evidence: { links: { total: 3, degraded: 1 }, migration: { total: 4 }, backups: { total: 2, restoreTested: 1 }, gazette: { signatures: 1, publications: 1, corrections: 0 } },
      integrations: [],
      externalDependencies: [{ name: "ICP-Brasil", state: "DEMO_ONLY", requirement: "Certificado real" }],
    } : [])));
    vi.stubGlobal("fetch", fetchMock);

    render(<CompliancePanel />);

    expect(await screen.findByText("AVAILABLE", { exact: true })).toBeInTheDocument();
    expect(screen.getAllByText("DEMO_ONLY", { exact: true }).length).toBeGreaterThan(0);
    expect(screen.getByText("1 de 2", { exact: true })).toBeInTheDocument();
    expect(screen.getByText("Certificado real", { exact: true })).toBeInTheDocument();
  });
});
