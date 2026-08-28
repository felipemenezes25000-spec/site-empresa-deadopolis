"use client";

import { Monitor, Smartphone, Tablet } from "lucide-react";
import { useState } from "react";
import { ResponsiveMediaImage } from "@/components/portal/responsive-media-image";
import { RichText } from "@/components/portal/rich-text";
import { NEWS_CATEGORIES } from "@/lib/news-categories";

type PreviewDraft = {
  title: string;
  summary: string;
  body: string;
  category: string;
  coverImageUrl: string;
  coverImageAlt: string;
};

const viewports = [
  { id: "desktop", label: "Desktop", width: 1120, icon: Monitor },
  { id: "tablet", label: "Tablet", width: 768, icon: Tablet },
  { id: "mobile", label: "Celular", width: 390, icon: Smartphone },
] as const;

/**
 * Pré-visualização da notícia.
 *
 * Usa RichText e ResponsiveMediaImage — exatamente os componentes que /noticias/[slug] usa para
 * publicar. Reimplementar a renderização aqui produziria uma prévia que diverge silenciosamente
 * da página real assim que qualquer um dos dois mudasse, que é o pior tipo de prévia: a que
 * parece confiável e não é.
 *
 * O que ela não simula: cabeçalho, rodapé e navegação do portal. Por isso o quadro diz o que
 * está mostrando, em vez de se apresentar como a página inteira.
 */
export function NewsPreview({ draft }: { draft: PreviewDraft }) {
  const [viewport, setViewport] = useState<(typeof viewports)[number]["id"]>("desktop");
  const width = viewports.find((item) => item.id === viewport)?.width ?? 1120;
  const category = NEWS_CATEGORIES.find(([value]) => value === draft.category)?.[1] ?? draft.category;
  const internalCover = draft.coverImageUrl.startsWith("/api/v1/media/") ? draft.coverImageUrl : null;
  const empty = !draft.title.trim() && !draft.body.trim();

  return <section className="admin-panel news-preview" aria-label="Pré-visualização da notícia">
    <div className="news-preview-bar">
      <div>
        <p className="eyebrow dark">Pré-visualização</p>
        <h2>Como o cidadão vai ler</h2>
      </div>
      <div className="news-preview-viewports" role="group" aria-label="Largura da pré-visualização">
        {viewports.map(({ id, label, icon: Icon }) => <button
          key={id}
          type="button"
          className={id === viewport ? "is-active" : undefined}
          aria-pressed={id === viewport}
          onClick={() => setViewport(id)}
        >
          <Icon size={15} aria-hidden="true" />{label}
        </button>)}
      </div>
    </div>

    <p className="muted-note">Renderizado com os mesmos componentes da página pública. Cabeçalho, rodapé e navegação do portal não entram no quadro.</p>

    <div className="news-preview-stage">
      <div className="news-preview-frame" style={{ width: `min(100%, ${width}px)` }}>
        {empty
          ? <p className="news-preview-empty">Preencha título e conteúdo para ver a notícia tomar forma.</p>
          : <article className="news-preview-article">
            <p className="story-kicker">{category}</p>
            <h1>{draft.title || "Sem título"}</h1>
            {draft.summary && <p className="news-preview-summary">{draft.summary}</p>}
            {internalCover && <ResponsiveMediaImage
              src={internalCover}
              width={1200}
              height={675}
              alt={draft.coverImageAlt || ""}
              sizes="(max-width: 900px) 100vw, 900px"
              className="news-preview-cover"
            />}
            {draft.body ? <RichText value={draft.body} /> : <p className="news-preview-empty">O corpo da notícia aparece aqui.</p>}
          </article>}
      </div>
    </div>
  </section>;
}
