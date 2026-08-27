import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { NewsEditor } from "./news-editor";

const article = {
  id: "11111111-1111-1111-1111-111111111111",
  title: "Notícia original",
  slug: "noticia-original",
  summary: "Resumo original",
  body: "Conteúdo original",
  category: "GERAL",
  coverImageUrl: null,
  coverImageAlt: null,
  isFeatured: false,
  status: "DRAFT",
  version: 3,
};

describe("NewsEditor", () => {
  afterEach(() => vi.unstubAllGlobals());

  it("loads an existing draft and saves it with optimistic version control", async () => {
    const fetchMock = vi.fn().mockImplementation((url: string, options?: RequestInit) => {
      if (url.includes("/media?")) return Promise.resolve(Response.json([]));
      if (options?.method === "PUT") return Promise.resolve(Response.json({ ...article, title: "Notícia revisada", version: 4 }));
      return Promise.resolve(Response.json(article));
    });
    vi.stubGlobal("fetch", fetchMock);

    render(<NewsEditor articleId={article.id} />);
    const title = await screen.findByDisplayValue("Notícia original");
    fireEvent.change(title, { target: { value: "Notícia revisada" } });
    fireEvent.click(screen.getByRole("button", { name: "Salvar alterações" }));

    await waitFor(() => expect(fetchMock).toHaveBeenCalledWith(
      `/api/v1/admin/news/${article.id}`,
      expect.objectContaining({
        method: "PUT",
        body: expect.stringContaining('"expectedVersion":3'),
      }),
    ));
    expect(await screen.findByText("Alterações salvas como versão 4.")).toBeInTheDocument();
  });
});
