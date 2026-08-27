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
});
