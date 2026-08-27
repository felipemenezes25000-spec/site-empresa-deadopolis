import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { buildMediaVariantUrl, ResponsiveMediaImage } from "./responsive-media-image";

describe("ResponsiveMediaImage", () => {
  it("builds deterministic WebP variants and preserves the original fallback", () => {
    const src = "/api/v1/media/11111111-1111-1111-1111-111111111111";
    const { container } = render(
      <ResponsiveMediaImage
        src={src}
        width={1200}
        height={675}
        alt="Capa da notícia"
        variantWidths={[480, 768, 1200]}
      />,
    );

    const source = container.querySelector("source[type='image/webp']");
    expect(source).not.toBeNull();
    expect(source?.getAttribute("srcset")).toContain("width=480&height=270&format=webp 480w");
    expect(source?.getAttribute("srcset")).toContain("width=1200&height=675&format=webp 1200w");

    const fallback = screen.getByRole("img", { name: "Capa da notícia" });
    expect(fallback).toHaveAttribute("src", src);
    expect(fallback).toHaveAttribute("width", "1200");
    expect(fallback).toHaveAttribute("height", "675");
  });

  it("keeps existing query parameters after the variant parameters", () => {
    expect(buildMediaVariantUrl("/api/v1/media/example?download=false", 640, 360)).toBe(
      "/api/v1/media/example/variant?width=640&height=360&format=webp&download=false",
    );
  });
});
