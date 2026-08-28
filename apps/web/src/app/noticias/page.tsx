import type { Metadata } from "next";
import Link from "next/link";
import { EmptyPanel, PageIntro, PublicShell } from "@/components/portal/public-shell";
import { NEWS_CATEGORIES, NEWS_FILTER_OPTIONS, newsCategoryLabel } from "@/lib/news-categories";
import { getNews } from "@/lib/portal-api";

export const metadata: Metadata = { title: "Notícias" };

export default async function NewsPage({ searchParams }: { searchParams: Promise<{ category?: string }> }) {
  const { category } = await searchParams;
  const selectedCategory = NEWS_CATEGORIES.some(([value]) => value === category) ? category : "GERAL";
  const news = await getNews(selectedCategory);

  return <PublicShell>
    <PageIntro eyebrow="Comunicação" title="Notícias da Prefeitura" description="Informações publicadas pela comunicação municipal e suas secretarias." />
    <section className="content-section"><div className="page-shell">
      <form className="archive-filters archive-filters--dedicated" action="/noticias">
        <div><label htmlFor="news-category">Área da notícia</label><select id="news-category" name="category" defaultValue={selectedCategory}>{NEWS_FILTER_OPTIONS.map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select></div>
        <button type="submit">Filtrar notícias</button>
      </form>
      {news.length === 0
        ? <EmptyPanel title="Nenhuma notícia publicada" description={selectedCategory === "GERAL" ? "As publicações aprovadas aparecerão aqui." : "Não há publicações aprovadas para esta área."} />
        : <div className="news-list">{news.map(item => <Link className="news-row" href={`/noticias/${item.slug}`} key={item.slug}><time>{item.publishedAt ? new Intl.DateTimeFormat("pt-BR").format(new Date(item.publishedAt)) : "Publicação recente"}</time><div><span className="kicker">{newsCategoryLabel(item.category)}</span><h2>{item.title}</h2><p>{item.summary}</p></div><span aria-hidden="true">→</span></Link>)}</div>}
    </div></section>
  </PublicShell>;
}
