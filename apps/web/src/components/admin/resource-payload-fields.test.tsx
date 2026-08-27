import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import { ResourcePayloadFields, serializeResourcePayload } from "./resource-payload-fields";

describe("serializeResourcePayload", () => {
  it("converts the page form into a governed payload", () => {
    const form = new FormData();
    form.set("payloadBody", "Texto oficial da página.");
    form.set("payloadSections", "Atendimento\nPrazos\nRecursos");

    expect(JSON.parse(serializeResourcePayload("PAGE", form))).toEqual({
      conteudo: "Texto oficial da página.",
      sections: ["Atendimento", "Prazos", "Recursos"],
    });
  });

  it("preserves legacy metadata while editing governed fields", () => {
    const form = new FormData();
    form.set("payloadBaseJson", JSON.stringify({ sourceUrl: "https://legacy.example/page" }));
    form.set("payloadBody", "Conteúdo revisado.");

    expect(JSON.parse(serializeResourcePayload("PAGE", form))).toEqual({
      sourceUrl: "https://legacy.example/page",
      conteudo: "Conteúdo revisado.",
      sections: [],
    });
  });

  it("loads the established migrated page content into the guided field", () => {
    render(<ResourcePayloadFields kind="PAGE" payloadJson={JSON.stringify({ conteudo: "Texto importado." })} />);

    expect(screen.getByRole("textbox", { name: "Conteúdo da página" })).toHaveValue("Texto importado.");
  });

  it("preserves explicit booleans in menu configuration", () => {
    const form = new FormData();
    form.set("payloadLabel", "Transparência");
    form.set("payloadUrl", "/transparencia");
    form.set("payloadExternal", "on");

    expect(JSON.parse(serializeResourcePayload("MENU", form))).toEqual({
      label: "Transparência",
      url: "/transparencia",
      parent: "",
      external: true,
      enabled: false,
    });
  });
});
