import { ArrowRight, BookOpenText, Building2, CalendarDays, ChevronRight, FileCheck2, Headphones, Landmark, MapPin, Search, ShieldCheck } from "lucide-react";
import Link from "next/link";
import { Button } from "@/components/ui/button";
import type { PortalHomeContent } from "@/lib/portal-api";
import { PageBlockRenderer } from "./page-block-renderer";

type HomeBlock = {
  id?: string;
  type: string;
  title?: string;
  content?: string;
  reference?: string;
  enabled?: boolean;
};

const allowedTypes = new Set([
  "Hero",
  "ServiceSearch",
  "QuickAccess",
  "FeaturedNews",
  "NewsGrid",
  "ServiceGrid",
  "DepartmentGrid",
  "Events",
  "Banner",
  "Alert",
  "Documents",
  "Statistics",
  "Contact",
  "Video",
  "Gallery",
  "CustomLinks",
]);

const defaultBlocks: HomeBlock[] = [
  { id: "default-search", type: "ServiceSearch" },
  { id: "default-services", type: "ServiceGrid" },
  { id: "default-news", type: "NewsGrid" },
  { id: "default-participation", type: "CustomLinks" },
  { id: "default-quick-access", type: "QuickAccess" },
];

export function HomeComposition({ content, payload }: { content: PortalHomeContent; payload?: unknown }) {
  const configured = readHomeBlocks(payload);
  const blocks = configured.length > 0 ? configured : defaultBlocks;

  return <main id="conteudo-principal" data-home-composition={configured.length > 0 ? "cms" : "fallback"}>
    {blocks.map((block, index) => <HomeBlockView key={block.id || `${block.type}-${index}`} block={block} content={content} />)}
  </main>;
}

function HomeBlockView({ block, content }: { block: HomeBlock; content: PortalHomeContent }) {
  switch (block.type) {
    case "Hero":
    case "ServiceSearch":
      return <ServiceHero block={block} />;
    case "ServiceGrid":
      return <ServicesSection block={block} content={content} />;
    case "FeaturedNews":
    case "NewsGrid":
      return <NewsSection block={block} content={content} />;
    case "CustomLinks":
      return <ParticipationSection block={block} content={content} />;
    case "QuickAccess":
    case "Contact":
      return <UsefulSection block={block} />;
    default:
      return <PageBlockRenderer payload={{ blocks: [block] }} />;
  }
}

function ServiceHero({ block }: { block: HomeBlock }) {
  const title = clean(block.title) || "Olá! O que você precisa?";
  const lead = clean(block.content) || "Encontre serviços, documentos e canais de atendimento sem precisar saber qual secretaria procurar.";
  return <section className="service-hero" aria-labelledby={`hero-title-${safeId(block.id)}`} data-block-type={block.type}>
    <div className="hero-pattern" aria-hidden="true" />
    <div className="page-shell hero-content">
      <p className="eyebrow">Serviços municipais em um só lugar</p>
      <h1 id={`hero-title-${safeId(block.id)}`}>{title}</h1>
      <p className="hero-lead">{lead}</p>
      <form className="universal-search" role="search" action="/buscar" method="get">
        <label className="sr-only" htmlFor={`portal-search-${safeId(block.id)}`}>Buscar serviço</label>
        <Search aria-hidden="true" size={23} />
        <input id={`portal-search-${safeId(block.id)}`} name="q" type="search" placeholder="Ex.: segunda via do IPTU, vaga na escola, poda de árvore" autoComplete="off" />
        <Button type="submit" size="large">Buscar</Button>
      </form>
      <div className="quick-needs" aria-label="Buscas sugeridas"><span>Mais buscados:</span>{["IPTU", "Nota fiscal", "Matrícula", "Licitações", "Ouvidoria"].map((need) => <Link key={need} href={`/buscar?q=${encodeURIComponent(need)}`}>{need}</Link>)}</div>
      {safeInternalOrHttpUrl(block.reference) && <Link className="read-more" href={safeInternalOrHttpUrl(block.reference)!}>Abrir destaque <ArrowRight size={17} aria-hidden="true" /></Link>}
    </div>
  </section>;
}

function ServicesSection({ block, content }: { block: HomeBlock; content: PortalHomeContent }) {
  const title = clean(block.title) || "Serviços mais procurados";
  return <section className="page-shell services-section" aria-labelledby={`services-title-${safeId(block.id)}`} data-block-type={block.type}>
    <div className="section-heading"><div><p className="eyebrow dark">Resolva por aqui</p><h2 id={`services-title-${safeId(block.id)}`}>{title}</h2>{clean(block.content) && <p>{clean(block.content)}</p>}</div><Link className="section-link" href="/servicos">Ver todos os serviços <ArrowRight size={18} aria-hidden="true" /></Link></div>
    {content.featuredServices.length > 0 ? <div className="service-list">{content.featuredServices.map((service, index) => <Link className="service-row" href={`/servicos/${service.slug}`} key={service.slug}><span className="service-number" aria-hidden="true">{String(index + 1).padStart(2, "0")}</span><span className="service-copy"><span className="service-meta">{service.area} {service.isOnline && <em>Online</em>}</span><strong>{service.name}</strong><small>{service.description}</small></span><ChevronRight size={21} aria-hidden="true" /></Link>)}</div> : <div className="empty-state" role="status"><Building2 aria-hidden="true" /><h3>Nenhum serviço em destaque</h3><p>O catálogo continua disponível para consulta.</p><Link href="/servicos">Abrir catálogo de serviços</Link></div>}
  </section>;
}

function NewsSection({ block, content }: { block: HomeBlock; content: PortalHomeContent }) {
  const title = clean(block.title) || (block.type === "FeaturedNews" ? "Notícia em destaque" : "Notícias da Prefeitura");
  const featured = content.latestNews.find((item) => item.isFeatured) ?? content.latestNews[0];
  const recent = content.latestNews.filter((item) => item.slug !== featured?.slug).slice(0, 3);
  return <section className="news-section" aria-labelledby={`news-title-${safeId(block.id)}`} data-block-type={block.type}><div className="page-shell"><div className="section-heading"><div><p className="eyebrow dark">Acontece em Deodápolis</p><h2 id={`news-title-${safeId(block.id)}`}>{title}</h2>{clean(block.content) && <p>{clean(block.content)}</p>}</div><Link className="section-link" href="/noticias">Todas as notícias <ArrowRight size={18} aria-hidden="true" /></Link></div>{featured ? <div className="editorial-grid"><article className="lead-story"><div className="story-visual" aria-hidden="true"><span>DEO</span><small>Informação municipal</small></div><div className="story-copy"><p className="story-kicker">Destaque</p><h3><Link href={`/noticias/${featured.slug}`}>{featured.title}</Link></h3><p>{featured.summary}</p><Link className="read-more" href={`/noticias/${featured.slug}`}>Ler notícia <ArrowRight size={17} aria-hidden="true" /></Link></div></article><div className="recent-stories">{recent.map((article) => <article key={article.slug}><time dateTime={article.publishedAt ?? undefined}>{formatDate(article.publishedAt)}</time><h3><Link href={`/noticias/${article.slug}`}>{article.title}</Link></h3><p>{article.summary}</p></article>)}{recent.length === 0 && <p className="muted-note">Novas publicações aparecerão aqui.</p>}</div></div> : <div className="empty-state" role="status"><BookOpenText aria-hidden="true" /><h3>Nenhuma notícia publicada</h3><p>Assim que houver uma publicação aprovada, ela aparecerá nesta área.</p></div>}</div></section>;
}

function ParticipationSection({ block, content }: { block: HomeBlock; content: PortalHomeContent }) {
  const title = clean(block.title) || "Transparência e participação";
  const description = clean(block.content) || "Acompanhe recursos públicos, atos oficiais e fale com a Prefeitura pelos canais adequados.";
  return <section className="participation-section" aria-labelledby={`participation-title-${safeId(block.id)}`} data-block-type={block.type}><div className="page-shell participation-grid"><div className="participation-intro"><p className="eyebrow">Governo aberto</p><h2 id={`participation-title-${safeId(block.id)}`}>{title}</h2><p>{description}</p><div className="participation-seal"><ShieldCheck aria-hidden="true" /><span>Informação pública<br /><strong>organizada e acessível</strong></span></div></div><div className="public-links">{content.transparencyLinks.map((link) => <a key={link.title} href={link.url} target="_blank" rel="noreferrer"><Landmark aria-hidden="true" /><span><strong>{link.title}</strong><small>{link.description || link.category}</small></span><ArrowRight aria-hidden="true" /></a>)}<Link href="/diario-oficial"><FileCheck2 aria-hidden="true" /><span><strong>Diário Oficial</strong><small>Consulte edições e verifique documentos</small></span><ArrowRight aria-hidden="true" /></Link><Link href="/ouvidoria"><Headphones aria-hidden="true" /><span><strong>Ouvidoria</strong><small>Reclamação, solicitação, denúncia e elogio</small></span><ArrowRight aria-hidden="true" /></Link></div></div></section>;
}

function UsefulSection({ block }: { block: HomeBlock }) {
  const title = clean(block.title) || "Encontre a Prefeitura";
  return <section className="page-shell useful-section" aria-labelledby={`useful-title-${safeId(block.id)}`} data-block-type={block.type}><div className="section-heading"><div><p className="eyebrow dark">No dia a dia</p><h2 id={`useful-title-${safeId(block.id)}`}>{title}</h2>{clean(block.content) && <p>{clean(block.content)}</p>}</div></div><div className="useful-grid"><Link href="/locais"><MapPin aria-hidden="true" /><span><strong>Unidades e endereços</strong><small>UBS, escolas, CRAS e órgãos municipais</small></span></Link><Link href="/agenda"><CalendarDays aria-hidden="true" /><span><strong>Agenda municipal</strong><small>Eventos, campanhas e atividades públicas</small></span></Link><Link href="/secretarias"><Building2 aria-hidden="true" /><span><strong>Secretarias</strong><small>Gestores, contatos e serviços de cada área</small></span></Link></div></section>;
}

export function readHomeBlocks(payload: unknown): HomeBlock[] {
  if (!payload || typeof payload !== "object" || Array.isArray(payload)) return [];
  const raw = (payload as Record<string, unknown>).blocks;
  if (!Array.isArray(raw)) return [];
  return raw.flatMap((entry, index) => {
    if (!entry || typeof entry !== "object" || Array.isArray(entry)) return [];
    const candidate = entry as Record<string, unknown>;
    const type = clean(candidate.type);
    if (!type || !allowedTypes.has(type) || candidate.enabled === false) return [];
    return [{
      id: clean(candidate.id) || `cms-${index + 1}`,
      type,
      title: bounded(candidate.title, 220),
      content: bounded(candidate.content, 4_000),
      reference: bounded(candidate.reference, 2_048),
      enabled: true,
    }];
  }).slice(0, 30);
}

function bounded(value: unknown, max: number) {
  const normalized = clean(value);
  return normalized ? normalized.slice(0, max) : undefined;
}

function clean(value: unknown) {
  return typeof value === "string" ? value.trim() : "";
}

function safeId(value: string | undefined) {
  return (value || "block").replace(/[^a-zA-Z0-9_-]/g, "-").slice(0, 80) || "block";
}

function safeInternalOrHttpUrl(value: string | undefined) {
  const normalized = clean(value);
  if (!normalized) return null;
  if (normalized.startsWith("/") && !normalized.startsWith("//")) return normalized;
  try {
    const url = new URL(normalized);
    return url.protocol === "http:" || url.protocol === "https:" ? url.toString() : null;
  } catch {
    return null;
  }
}

function formatDate(value: string | null) {
  if (!value) return "Publicação recente";
  return new Intl.DateTimeFormat("pt-BR", { day: "2-digit", month: "short", year: "numeric", timeZone: "UTC" }).format(new Date(value));
}
