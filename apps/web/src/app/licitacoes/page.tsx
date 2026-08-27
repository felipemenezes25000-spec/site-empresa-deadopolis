import type { Metadata } from "next";
import Link from "next/link";
import { DocumentArchive, type ArchiveSearch } from "@/components/portal/document-archive";
import { getResources, getTransparency } from "@/lib/portal-api";

export const metadata: Metadata = { title: "Licitações" };
export const dynamic = "force-dynamic";

export default async function ProcurementPage({ searchParams }: { searchParams: Promise<ArchiveSearch> }) {
  const [resources, links, search] = await Promise.all([
    getResources("PROCUREMENT_LINK"),
    getTransparency(),
    searchParams,
  ]);
  const procurementLinks = links
    .filter(item => /licita|compra|contrat/i.test(`${item.title} ${item.category}`))
    .map(item => ({ id: item.url, title: item.title, summary: item.description, url: item.url }));
  const managedLinks = resources
    .map(item => ({ id: item.id, title: item.title, summary: item.summary, url: (item.payload as { url?: string })?.url ?? "" }))
    .filter(item => item.url.length > 0);
  const officialSources = [...new Map([...procurementLinks, ...managedLinks].map(item => [item.url, item])).values()];

  return <DocumentArchive
    search={search}
    category="LICITACOES"
    action="/licitacoes"
    intro={{
      eyebrow: "Compras públicas",
      title: "Licitações e contratos",
      description: "Consulte o acervo histórico preservado e acesse, separadamente, os sistemas oficiais que permanecem como fonte operacional dos processos atuais.",
    }}
  >
    <section className="content-section" aria-labelledby="procurement-calendar">
      <div className="page-shell">
        <Link className="info-card" href="/licitacoes/calendario">
          <span className="kicker">Agenda pública</span>
          <h2 id="procurement-calendar">Calendário de licitações</h2>
          <p>Consulte sessões e marcos dos processos de contratação publicados pela Prefeitura.</p>
          <span className="section-link">Consultar calendário →</span>
        </Link>
      </div>
    </section>
    {officialSources.length > 0 && <section className="content-section" aria-labelledby="official-procurement-sources">
      <div className="page-shell">
        <div className="section-heading">
          <p className="kicker">Fonte de verdade atual</p>
          <h2 id="official-procurement-sources">Sistemas oficiais de compras públicas</h2>
          <p>Os links abaixo levam aos sistemas operacionais. O acervo pesquisável desta página preserva documentos históricos aprovados.</p>
        </div>
        <div className="card-grid">
          {officialSources.map(item => <a className="info-card" key={item.id} href={item.url} target={item.url.startsWith("http") ? "_blank" : undefined} rel={item.url.startsWith("http") ? "noreferrer" : undefined}>
            <span className="kicker">{item.url.startsWith("http") ? "Sistema externo" : "Portal"}</span>
            <h3>{item.title}</h3>
            <p>{item.summary}</p>
            {item.url.startsWith("http") && <small>Abre a fonte oficial em uma nova janela.</small>}
          </a>)}
        </div>
      </div>
    </section>}
  </DocumentArchive>;
}
