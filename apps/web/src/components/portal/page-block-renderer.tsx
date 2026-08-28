import { CalendarDays, Download, ExternalLink, FileText, PlayCircle } from "lucide-react";
import type { PageBlock, PageBlockItem } from "@/lib/page-blocks";
import { internalMediaUrl, readPageBlocks, safeBlockHref } from "@/lib/page-blocks";
import { ResponsiveMediaImage } from "./responsive-media-image";

const linkGridTypes = new Set(["QuickAccess", "FeaturedNews", "NewsGrid", "ServiceGrid", "DepartmentGrid", "CustomLinks"]);

export function PageBlockRenderer({ payload }: { payload: unknown }) {
  const blocks = readPageBlocks(payload);
  if (blocks.length === 0) return null;
  return <section className="content-section" aria-label="Conteúdo complementar"><div className="page-shell grid gap-4">{blocks.map((block) => <PageBlockView key={block.id} block={block} />)}</div></section>;
}

function PageBlockView({ block }: { block: PageBlock }) {
  if (block.type === "ServiceSearch") return <SearchBlock block={block} />;
  if (block.type === "Hero") return <HeroBlock block={block} />;
  if (block.type === "Alert") return <AlertBlock block={block} />;
  if (block.type === "Banner") return <BannerBlock block={block} />;
  if (block.type === "Gallery") return <GalleryBlock block={block} />;
  if (block.type === "Video") return <VideoBlock block={block} />;
  if (block.type === "Statistics") return <StatisticsBlock block={block} />;
  if (block.type === "Events") return <EventsBlock block={block} />;
  if (block.type === "Documents") return <DocumentsBlock block={block} />;
  if (block.type === "Contact") return <ContactBlock block={block} />;
  if (linkGridTypes.has(block.type)) return <LinkGridBlock block={block} />;
  return null;
}

function SearchBlock({ block }: { block: PageBlock }) {
  return <section className="prose-card" data-block-type={block.type}><h2>{titleFor(block)}</h2>{block.content && <p>{block.content}</p>}<form role="search" action="/buscar" method="get" className="flex flex-wrap gap-2"><label className="sr-only" htmlFor={`service-search-${block.id}`}>Buscar serviço</label><input id={`service-search-${block.id}`} name="q" type="search" placeholder="O que você precisa?" className="min-h-11 min-w-0 flex-1 rounded-lg border border-border bg-surface px-3 py-2" /><button className="min-h-11 rounded-lg bg-primary px-4 font-semibold text-white" type="submit">Buscar</button></form></section>;
}

function HeroBlock({ block }: { block: PageBlock }) {
  return <section className="prose-card cms-hero-block" data-block-type={block.type}><p className="eyebrow">Prefeitura de Deodápolis</p><h2>{titleFor(block)}</h2>{block.content && <p className="text-lg">{block.content}</p>}{block.reference && <BlockLink reference={block.reference}>{block.linkLabel || "Acessar destaque"}</BlockLink>}</section>;
}

function AlertBlock({ block }: { block: PageBlock }) {
  return <section className="prose-card cms-alert-block" role="status" data-block-type={block.type}><h2>{titleFor(block)}</h2>{block.content && <p>{block.content}</p>}{block.reference && <BlockLink reference={block.reference}>{block.linkLabel || "Saiba mais"}</BlockLink>}</section>;
}

function BannerBlock({ block }: { block: PageBlock }) {
  const imageUrl = internalMediaUrl(block.imageUrl);
  return <section className={`cms-banner${imageUrl ? " has-media" : ""}`} data-block-type={block.type}>
    {imageUrl && <ResponsiveMediaImage className="cms-banner-image" src={imageUrl} width={1200} height={480} sizes="(max-width: 760px) 100vw, 1200px" alt={block.imageAlt || titleFor(block)} />}
    <div className="cms-banner-copy"><p className="eyebrow">Destaque</p><h2>{titleFor(block)}</h2>{block.content && <p>{block.content}</p>}{block.reference && <BlockLink reference={block.reference}>{block.linkLabel || "Ver conteúdo"}</BlockLink>}</div>
  </section>;
}

function GalleryBlock({ block }: { block: PageBlock }) {
  const items = block.items.flatMap((item) => {
    const mediaUrl = internalMediaUrl(item.mediaUrl);
    return mediaUrl ? [{ item, mediaUrl }] : [];
  });
  return <section className="prose-card" data-block-type={block.type}><BlockIntro block={block} eyebrow="Galeria" />{items.length > 0 ? <div className="cms-gallery">{items.map(({ item, mediaUrl }) => <figure key={item.id}><ResponsiveMediaImage className="cms-gallery-image" src={mediaUrl} width={720} height={720} sizes="(max-width: 760px) 100vw, 360px" alt={item.mediaAlt || item.label || titleFor(block)} />{(item.label || item.description) && <figcaption><strong>{item.label}</strong>{item.description && <span>{item.description}</span>}</figcaption>}</figure>)}</div> : <EmptyBlockItems label="Nenhuma imagem válida configurada para esta galeria." />}</section>;
}

function VideoBlock({ block }: { block: PageBlock }) {
  const internalVideo = internalMediaUrl(block.reference);
  const externalVideo = safeBlockHref(block.reference);
  return <section className="prose-card" data-block-type={block.type}><BlockIntro block={block} eyebrow="Vídeo" />{internalVideo ? <video className="cms-video" controls preload="metadata" aria-label={titleFor(block)}><source src={internalVideo} /></video> : externalVideo ? <BlockLink reference={externalVideo} icon="video">{block.linkLabel || "Assistir ao vídeo"}</BlockLink> : <EmptyBlockItems label="Nenhum vídeo válido configurado." />}</section>;
}

function StatisticsBlock({ block }: { block: PageBlock }) {
  return <section className="prose-card" data-block-type={block.type}><BlockIntro block={block} eyebrow="Indicadores" />{block.items.length > 0 ? <dl className="cms-statistics">{block.items.map((item) => <div key={item.id}><dd>{item.value || "—"}</dd><dt>{item.label || "Indicador"}</dt>{item.description && <p>{item.description}</p>}</div>)}</dl> : <EmptyBlockItems label="Nenhum indicador publicado." />}</section>;
}

function EventsBlock({ block }: { block: PageBlock }) {
  return <section className="prose-card" data-block-type={block.type}><BlockIntro block={block} eyebrow="Agenda" />{block.items.length > 0 ? <div className="cms-event-list">{block.items.map((item) => <article key={item.id}><CalendarDays aria-hidden="true" /><div>{item.date && <time dateTime={item.date}>{formatBlockDate(item.date)}</time>}<h3><OptionalItemLink item={item} /></h3>{item.description && <p>{item.description}</p>}</div></article>)}</div> : <EmptyBlockItems label="Nenhum evento programado." />}</section>;
}

function DocumentsBlock({ block }: { block: PageBlock }) {
  return <section className="prose-card" data-block-type={block.type}><BlockIntro block={block} eyebrow="Documentos" />{block.items.length > 0 ? <div className="cms-document-list">{block.items.map((item) => <div key={item.id}><FileText aria-hidden="true" /><div><OptionalItemLink item={item} />{item.description && <p>{item.description}</p>}{item.date && <small><time dateTime={item.date}>{formatBlockDate(item.date)}</time></small>}</div>{safeBlockHref(item.url) && <span className="cms-document-format"><Download size={16} aria-hidden="true" />{documentFormat(item.url)}</span>}</div>)}</div> : <EmptyBlockItems label="Nenhum documento publicado." />}</section>;
}

function documentFormat(url: string) {
  const path = url.split(/[?#]/, 1)[0];
  const extension = /\.([a-z0-9]{2,5})$/i.exec(path)?.[1];
  return extension ? extension.toUpperCase() : "Arquivo";
}

function ContactBlock({ block }: { block: PageBlock }) {
  return <section className="prose-card" data-block-type={block.type}><BlockIntro block={block} eyebrow="Contato" />{block.reference && <BlockLink reference={block.reference}>{block.linkLabel || "Abrir canal de atendimento"}</BlockLink>}</section>;
}

function LinkGridBlock({ block }: { block: PageBlock }) {
  const featured = block.type === "FeaturedNews";
  return <section className="prose-card" data-block-type={block.type}><BlockIntro block={block} eyebrow={defaultTitle(block.type)} />{block.items.length > 0 ? <div className={`cms-link-grid${featured ? " is-featured" : ""}`}>{block.items.map((item) => <LinkGridCard key={item.id} item={item} />)}</div> : block.reference ? <BlockLink reference={block.reference}>{block.linkLabel || "Abrir"}</BlockLink> : <EmptyBlockItems label="Nenhum item publicado neste bloco." />}</section>;
}

function LinkGridCard({ item }: { item: PageBlockItem }) {
  const mediaUrl = internalMediaUrl(item.mediaUrl);
  return <article className={mediaUrl ? "has-media" : undefined}>
    {mediaUrl && <ResponsiveMediaImage className="cms-link-grid-image" src={mediaUrl} width={640} height={360} sizes="(max-width: 760px) 100vw, 380px" alt={item.mediaAlt || item.label} />}
    <div className="cms-link-grid-copy">
      {item.date && <time dateTime={item.date}>{formatBlockDate(item.date)}</time>}
      <h3><OptionalItemLink item={item} /></h3>
      {item.description && <p>{item.description}</p>}
    </div>
  </article>;
}

function BlockIntro({ block, eyebrow }: { block: PageBlock; eyebrow: string }) {
  return <div className="cms-block-heading"><p className="eyebrow">{eyebrow}</p><h2>{titleFor(block)}</h2>{block.content && <p>{block.content}</p>}</div>;
}

function OptionalItemLink({ item }: { item: PageBlockItem }) {
  const href = safeBlockHref(item.url);
  const label = item.label || "Item sem título";
  if (!href) return <span>{label}</span>;
  const external = isExternal(href);
  return <a href={href} target={external ? "_blank" : undefined} rel={external ? "noopener noreferrer" : undefined}>{label}{external && <ExternalLink size={15} aria-label="Abre em nova janela" />}</a>;
}

function BlockLink({ reference, children, icon }: { reference: string; children: string; icon?: "video" }) {
  const href = safeBlockHref(reference);
  if (!href) return <small className="text-muted">Referência editorial inválida.</small>;
  const external = isExternal(href);
  return <a href={href} className="action-button" target={external ? "_blank" : undefined} rel={external ? "noopener noreferrer" : undefined}>{icon === "video" && <PlayCircle aria-hidden="true" />}{children}{external && <ExternalLink size={15} aria-label="Abre em nova janela" />}</a>;
}

function EmptyBlockItems({ label }: { label: string }) {
  return <p className="text-muted" role="status">{label}</p>;
}

function titleFor(block: PageBlock) {
  return block.title || defaultTitle(block.type);
}

function defaultTitle(type: PageBlock["type"]) {
  const titles: Record<PageBlock["type"], string> = {
    Hero: "Destaque", ServiceSearch: "Encontre um serviço", QuickAccess: "Acesso rápido", FeaturedNews: "Notícia em destaque", NewsGrid: "Notícias", ServiceGrid: "Serviços", DepartmentGrid: "Secretarias", Events: "Agenda", Banner: "Destaque", Alert: "Aviso importante", Documents: "Documentos", Statistics: "Indicadores", Contact: "Contato", Video: "Vídeo", Gallery: "Galeria", CustomLinks: "Links úteis",
  };
  return titles[type];
}

function formatBlockDate(value: string) {
  const parsed = /^\d{4}-\d{2}-\d{2}$/.test(value) ? new Date(`${value}T12:00:00Z`) : new Date(value);
  return Number.isNaN(parsed.valueOf()) ? value : new Intl.DateTimeFormat("pt-BR", { timeZone: "UTC" }).format(parsed);
}

function isExternal(href: string) {
  return href.startsWith("http://") || href.startsWith("https://");
}
