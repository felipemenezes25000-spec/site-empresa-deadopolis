import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { MediaFramingEditor } from "./media-framing-editor";

const asset = {
  id: "11111111-1111-1111-1111-111111111111",
  altText: "Equipe de vacinação",
  tagsCsv: "saude",
  focalPointX: 0.5,
  focalPointY: 0.5,
  cropX: null,
  cropY: null,
  cropWidth: null,
  cropHeight: null,
};

describe("MediaFramingEditor", () => {
  it("previews the focal point while the editor moves it", () => {
    render(<MediaFramingEditor asset={asset} onSave={vi.fn()} />);

    expect(screen.getByRole("status")).toHaveTextContent("Ponto focal em 50% × 50% · sem recorte editorial.");
    fireEvent.change(screen.getByLabelText(/Ponto focal horizontal/), { target: { value: "72" } });
    fireEvent.change(screen.getByLabelText(/Ponto focal vertical/), { target: { value: "18" } });

    expect(screen.getByRole("status")).toHaveTextContent("Ponto focal em 72% × 18%");
    expect(screen.getByLabelText(/Ponto focal horizontal/)).toHaveValue("72");
  });

  it("saves normalized coordinates and never sends a crop beyond the original", async () => {
    const onSave = vi.fn();
    render(<MediaFramingEditor asset={asset} onSave={onSave} />);

    fireEvent.click(screen.getByLabelText("Definir recorte editorial normalizado"));
    fireEvent.change(screen.getByLabelText("X (%)"), { target: { value: "60" } });
    fireEvent.change(screen.getByLabelText("Largura (%)"), { target: { value: "90" } });
    fireEvent.change(screen.getByLabelText(/Ponto focal horizontal/), { target: { value: "80" } });
    fireEvent.click(screen.getByRole("button", { name: "Salvar enquadramento" }));

    expect(onSave).toHaveBeenCalledWith({
      tags: "saude",
      focalPointX: 0.8,
      focalPointY: 0.5,
      cropX: 0.6,
      cropY: 0,
      cropWidth: 0.4,
      cropHeight: 1,
    });
  });

  it("clears the crop when the editor turns it off", () => {
    const onSave = vi.fn();
    render(<MediaFramingEditor asset={{ ...asset, cropX: 0.1, cropY: 0.1, cropWidth: 0.5, cropHeight: 0.5 }} onSave={onSave} />);

    expect(screen.getByRole("status")).toHaveTextContent("recorte 50% × 50% a partir de 10% × 10%");
    fireEvent.click(screen.getByLabelText("Definir recorte editorial normalizado"));
    fireEvent.click(screen.getByRole("button", { name: "Salvar enquadramento" }));

    expect(onSave).toHaveBeenCalledWith(expect.objectContaining({ cropX: null, cropY: null, cropWidth: null, cropHeight: null }));
  });
});
