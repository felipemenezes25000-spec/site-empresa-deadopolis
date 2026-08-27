import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { getPublicDocuments } from "@/lib/portal-api";
import { DocumentArchive } from "./document-archive";

vi.mock("@/lib/portal-api", () => ({
  getPublicDocuments: vi.fn().mockResolvedValue({
    page: 1,
    pageSize: 25,
    total: 1,
    totalPages: 1,
    items: [{
      id: "11111111-1111-1111-1111-111111111111",
      category: "PRESTACAO_CONTAS",
      subcategory: "RREO",
      title: "Relatório oficial de 2025",
      description: "Documento preservado do portal anterior.",
      documentNumber: "12/2025",
      processNumber: "",
      referencePeriod: "2025",
      publicationDate: "2025-12-31",
      responsibleDepartment: "Secretaria de Finanças",
      documentType: "REPORT",
      sourceUrl: "https://legacy.example.test/report.pdf",
      originalFileName: "report.pdf",
      mimeType: "application/pdf",
      sizeBytes: 2048,
      sha256: "abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789",
      sourceSystem: "LEGACY_PORTAL",
      publishedAt: "2026-08-26T12:00:00Z",
      downloadUrl: "/api/v1/public/documents/11111111-1111-1111-1111-111111111111/download",
    }],
  }),
}));

describe("DocumentArchive", () => {
  it("exposes accessible filters, provenance, hash and governed download", async () => {
    render(await DocumentArchive({ search: {} }));

    expect(screen.getByRole("heading", { level: 1, name: /acervo público de documentos/i })).toBeInTheDocument();
    expect(screen.getByRole("searchbox", { name: /buscar no acervo/i })).toBeInTheDocument();
    expect(screen.getByRole("heading", { level: 2, name: /relatório oficial de 2025/i })).toBeInTheDocument();
    expect(screen.getByText(/sha-256 abcdef0123456789/i)).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /baixar documento/i })).toHaveAttribute("href", "/api/v1/public/documents/11111111-1111-1111-1111-111111111111/download");
    expect(screen.getByRole("link", { name: /ver origem/i })).toHaveAttribute("href", "https://legacy.example.test/report.pdf");
  });

  it("locks a dedicated archive to its governed category", async () => {
    const view = render(await DocumentArchive({
      search: { q: "contrato", subcategory: "CONTRATOS" },
      category: "LICITACOES",
      action: "/licitacoes",
      intro: {
        eyebrow: "Compras públicas",
        title: "Licitações e contratos",
        description: "Acervo histórico e acesso ao sistema oficial.",
      },
    }));

    expect(getPublicDocuments).toHaveBeenLastCalledWith({ q: "contrato", subcategory: "CONTRATOS", category: "LICITACOES" });
    expect(screen.queryByRole("combobox", { name: /categoria/i })).not.toBeInTheDocument();
    expect(screen.getByRole("combobox", { name: /etapa/i })).toHaveValue("CONTRATOS");
    expect(view.container.querySelector("form")).toHaveAttribute("action", "/licitacoes");
    expect(view.container.querySelector('input[name="category"]')).toHaveValue("LICITACOES");
  });

  it("forces a verified transparency family instead of trusting query parameters", async () => {
    const view = render(await DocumentArchive({
      search: { subcategory: "OUTRA" },
      category: "PRESTACAO_CONTAS",
      subcategory: "RREO",
      action: "/transparencia/rreo",
    }));

    expect(getPublicDocuments).toHaveBeenLastCalledWith({ subcategory: "RREO", category: "PRESTACAO_CONTAS" });
    expect(view.container.querySelector('input[name="subcategory"]')).toHaveValue("RREO");
  });

  it("offers normative classification for the legislation archive", async () => {
    render(await DocumentArchive({
      search: {},
      category: "LEGISLACAO",
      action: "/legislacao",
    }));

    expect(screen.getByRole("combobox", { name: "Espécie normativa" })).toBeInTheDocument();
    expect(screen.getByRole("option", { name: "Decretos" })).toBeInTheDocument();
    expect(screen.getByRole("option", { name: "Leis ordinárias" })).toBeInTheDocument();
  });
});
