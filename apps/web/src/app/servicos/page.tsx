import type { Metadata } from "next";
import Link from "next/link";
import { EmptyPanel, PageIntro, PublicShell } from "@/components/portal/public-shell";
import { getServices } from "@/lib/portal-api";

export const metadata: Metadata = {
  title: "Carta de Serviços",
  description: "Serviços da Prefeitura de Deodápolis/MS organizados por necessidade, com requisitos, documentos, canais e prazos.",
};

function filterHref(params: { q?: string; area?: string }) {
  const search = new URLSearchParams();
  if (params.q) search.set("q", params.q);
  if (params.area) search.set("area", params.area);
  return search.size > 0 ? `/servicos?${search}` : "/servicos";
}

export default async function ServicesPage({ searchParams }: { searchParams: Promise<{ q?: string; area?: string }> }) {
  const { q, area } = await searchParams;
  // As áreas vêm do catálogo completo, não do resultado filtrado. Derivá-las da lista já filtrada
  // deixava apenas a área escolhida na tela, sem caminho de volta nem para outra área.
  const [services, catalogue] = await Promise.all([getServices(q, area), getServices()]);
  const areas = [...new Set(catalogue.map((item) => item.area))].sort((a, b) => a.localeCompare(b, "pt-BR"));
  const filtered = Boolean(q || area);

  return <PublicShell>
    <PageIntro eyebrow="Carta de Serviços" title="O que você precisa resolver?" description="Serviços organizados pela necessidade do cidadão, com requisitos, documentos, canais e prazos." />
    <section className="content-section"><div className="page-shell">

      <form className="content-toolbar" role="search">
        <div className="content-search">
          <label className="sr-only" htmlFor="service-q">Buscar serviço</label>
          <input id="service-q" name="q" defaultValue={q} placeholder="Ex.: IPTU, matrícula, poda de árvore" />
          <button type="submit">Buscar</button>
        </div>
        {/* Mantém a área ao pesquisar; sem isto a busca descartava o filtro em vigor. */}
        {area && <input type="hidden" name="area" value={area} />}
      </form>

      {areas.length > 0 && <nav className="filter-row" aria-label="Filtrar por área">
        <Link className="filter-pill" href={filterHref({ q })} aria-current={area ? undefined : "true"}>Todas as áreas</Link>
        {areas.map((item) => <Link
          key={item}
          className="filter-pill"
          href={filterHref({ q, area: item })}
          aria-current={area === item ? "true" : undefined}
        >{item}</Link>)}
      </nav>}

      <p className="result-count" role="status">
        {services.length === 0 ? "Nenhum serviço encontrado" : `${services.length} ${services.length === 1 ? "serviço encontrado" : "serviços encontrados"}`}
        {filtered && <> · <Link href="/servicos">limpar filtros</Link></>}
      </p>

      {services.length === 0
        ? <EmptyPanel title="Nenhum serviço encontrado" description="Nenhum serviço corresponde a esta combinação de termo e área. Remova os filtros para ver a Carta de Serviços completa." />
        : <div className="card-grid">{services.map((service) => <Link key={service.slug} className="info-card" href={`/servicos/${service.slug}`}>
          <span className="kicker">{service.area}{service.isOnline ? " · online" : ""}</span>
          <h2>{service.name}</h2>
          <p>{service.description}</p>
          <small>{service.expectedDuration || "Consulte os detalhes"} · {service.cost || "Consulte o custo"}</small>
        </Link>)}</div>}

    </div></section>
  </PublicShell>;
}
