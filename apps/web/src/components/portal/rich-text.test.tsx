import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { RichText } from "./rich-text";

describe("RichText", () => {
  it("renders governed formatting without raw HTML", () => {
    render(<RichText value={"## Aviso\nTexto com **destaque**.\n- Primeiro item\n- Segundo item"} />);
    expect(screen.getByRole("heading", { name: "Aviso" })).toBeInTheDocument();
    expect(screen.getByText("destaque")).toHaveProperty("tagName", "STRONG");
    expect(screen.getAllByRole("listitem")).toHaveLength(2);
  });

  it("does not activate unsafe link schemes", () => {
    render(<RichText value={"[não abrir](javascript:alert(1))"} />);
    expect(screen.getByText("não abrir").closest("a")).toBeNull();
  });
});
