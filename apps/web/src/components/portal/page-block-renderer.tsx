type PageBlock = {
  id?: string;
  type?: string;
  title?: string;
  content?: string;
  reference?: string;
  enabled?: boolean;
};

const linkOrGridTypes = new Set(["QuickAccess", "FeaturedNews", "NewsGrid", "ServiceGrid", "DepartmentGrid", "Events", "Documents", "Statistics", "Contact", "Video", "Gallery", "CustomLinks"]);

export function PageBlockRenderer({ payload }: { payload: unknown }) {
  const blocks = readBlocks(payload);
  if (blocks.length === 0) return null;
  return <section className="content-section" aria-label="Conteúdo complementar"><div className="page-shell grid gap-4">{blocks.map((block, index) => <PageBlockView key={block.id || `${block.type}-${index}`} block={block} />)}</div></section>;
}

function PageBlockView({ block }: { block: PageBlock }) {
  const type = block.type || "CustomLinks";
  const title = block.title?.trim() || defaultTitle(type);
  const content = block.content?.trim();
  const reference = block.reference?.trim();

  if (type === "ServiceSearch") {
    return <section className="prose-card" data-block-type={type}><h2>{title}</h2>{content && <p>{content}</p>}<form role="search" action="/buscar" method="get" className="flex flex-wrap gap-2"><label className="sr-only" htmlFor={`service-search-${block.id ?? "block"}`}>Buscar serviço</label><input id={`service-search-${block.id ?? "block"}`} name="q" type="search" placeholder="O que você precisa?" className="min-h-11 min-w-0 flex-1 rounded-lg border border-border bg-surface px-3 py-2" /><button className="min-h-11 rounded-lg bg-primary px-4 font-semibold text-white" type="submit">Buscar</button></form></section>;
  }

  if (type === "Hero") {
    return <section className="prose-card" data-block-type={type}><p className="eyebrow">Prefeitura de Deodápolis</p><h2>{title}</h2>{content && <p className="text-lg">{content}</p>}{reference && <BlockLink reference={reference}>Acessar destaque</BlockLink>}</section>;
  }

  if (type === "Alert") {
    return <section className="prose-card" role="status" data-block-type={type}><h2>{title}</h2>{content && <p>{content}</p>}{reference && <BlockLink reference={reference}>Saiba mais</BlockLink>}</section>;
  }

  if (type === "Banner") {
    return <section className="prose-card" data-block-type={type}><h2>{title}</h2>{content && <p>{content}</p>}{reference && <BlockLink reference={reference}>Ver conteúdo</BlockLink>}</section>;
  }

  if (linkOrGridTypes.has(type)) {
    return <section className="prose-card" data-block-type={type}><div className="flex flex-wrap items-start justify-between gap-3"><div><p className="eyebrow">{humanizeType(type)}</p><h2>{title}</h2></div>{reference && <BlockLink reference={reference}>Abrir</BlockLink>}</div>{content ? <p style={{ whiteSpace: "pre-wrap" }}>{content}</p> : <p className="text-muted">Conteúdo administrado pelo CMS municipal.</p>}</section>;
  }

  return <section className="prose-card" data-block-type={type}><h2>{title}</h2>{content && <p style={{ whiteSpace: "pre-wrap" }}>{content}</p>}{reference && <BlockLink reference={reference}>Abrir referência</BlockLink>}</section>;
}

function BlockLink({ reference, children }: { reference: string; children: string }) {
  const href = safeHref(reference);
  if (!href) return <small className="text-muted">Referência editorial: {reference}</small>;
  const external = href.startsWith("http://") || href.startsWith("https://");
  return <a href={href} className="action-button" target={external ? "_blank" : undefined} rel={external ? "noopener noreferrer" : undefined}>{children}</a>;
}

function readBlocks(payload: unknown): PageBlock[] {
  if (!payload || typeof payload !== "object" || Array.isArray(payload)) return [];
  const raw = (payload as Record<string, unknown>).blocks;
  if (!Array.isArray(raw)) return [];
  return raw.flatMap((entry) => {
    if (!entry || typeof entry !== "object" || Array.isArray(entry)) return [];
    const block = entry as PageBlock;
    if (!block.type || block.enabled === false) return [];
    return [block];
  });
}

function safeHref(value: string) {
  if (value.startsWith("/") && !value.startsWith("//")) return value;
  try {
    const url = new URL(value);
    return url.protocol === "https:" || url.protocol === "http:" ? url.toString() : null;
  } catch {
    return null;
  }
}

function defaultTitle(type: string) {
  const titles: Record<string, string> = {
    Hero: "Destaque",
    ServiceSearch: "Encontre um serviço",
    QuickAccess: "Acesso rápido",
    FeaturedNews: "Notícia em destaque",
    NewsGrid: "Notícias",
    ServiceGrid: "Serviços",
    DepartmentGrid: "Secretarias",
    Events: "Agenda",
    Banner: "Destaque",
    Alert: "Aviso importante",
    Documents: "Documentos",
    Statistics: "Indicadores",
    Contact: "Contato",
    Video: "Vídeo",
    Gallery: "Galeria",
    CustomLinks: "Links úteis",
  };
  return titles[type] ?? humanizeType(type);
}

function humanizeType(value: string) {
  return value.replace(/([a-z])([A-Z])/g, "$1 $2").replace(/^./, (character) => character.toUpperCase());
}
