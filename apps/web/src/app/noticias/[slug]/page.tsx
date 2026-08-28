import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { PageIntro, PublicShell } from "@/components/portal/public-shell";
import { ResponsiveMediaImage } from "@/components/portal/responsive-media-image";
import { RichText } from "@/components/portal/rich-text";
import { StructuredData, breadcrumbList, newsArticle } from "@/components/portal/structured-data";
import { getArticle } from "@/lib/portal-api";

export async function generateMetadata({ params }: { params: Promise<{ slug: string }> }): Promise<Metadata> {
  const { slug } = await params;
  const article = await getArticle(slug);
  return { title: article?.title ?? "Notícia", description: article?.summary };
}

export default async function ArticlePage({ params }: { params: Promise<{ slug: string }> }) {
  const { slug } = await params;
  const article = await getArticle(slug);
  if (!article) notFound();

  const baseUrl = (process.env.PUBLIC_PORTAL_URL ?? "http://localhost:3000").replace(/\/+$/, "");
  const trail = [{ label: "Início", href: "/" }, { label: "Notícias", href: "/noticias" }, { label: article.title }];
  const internalCover = article.coverImageUrl?.startsWith("/api/v1/media/") ? article.coverImageUrl : null;

  return (
    <PublicShell>
      <StructuredData data={newsArticle(article, `${baseUrl}/noticias/${slug}`)} />
      <StructuredData data={breadcrumbList(trail, baseUrl)} />
      <PageIntro eyebrow="Notícia" title={article.title} description={article.summary} breadcrumb={trail} />
      <section className="content-section">
        <div className="page-shell detail-grid">
          <article className="prose-card">
            {internalCover && (
              <ResponsiveMediaImage
                src={internalCover}
                width={1200}
                height={675}
                alt={article.coverImageAlt || "Imagem de capa da notícia"}
                style={{ width: "100%", height: "auto", borderRadius: 12, marginBottom: 20 }}
              />
            )}
            <p>
              <strong>Publicado em:</strong>{" "}
              {article.publishedAt
                ? new Intl.DateTimeFormat("pt-BR", { dateStyle: "long" }).format(new Date(article.publishedAt))
                : "—"}
            </p>
            <RichText value={article.body} />
          </article>
          <aside className="side-card">
            <h2>Informação pública</h2>
            <p>Conteúdo sujeito ao fluxo editorial e trilha de auditoria do portal.</p>
            <small>Atualizado em {new Intl.DateTimeFormat("pt-BR").format(new Date(article.updatedAt))}</small>
          </aside>
        </div>
      </section>
    </PublicShell>
  );
}
