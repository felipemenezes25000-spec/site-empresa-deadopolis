import type { Metadata } from "next";
import { DocumentArchive, type ArchiveSearch } from "@/components/portal/document-archive";
import { getResources } from "@/lib/portal-api";

export const metadata: Metadata = { title: "Legislação" };
export const dynamic = "force-dynamic";

export default async function LegislationPage({ searchParams }: { searchParams: Promise<ArchiveSearch> }) {
  const [resources, search] = await Promise.all([getResources("LEGISLATION"), searchParams]);

  return <DocumentArchive
    search={search}
    category="LEGISLACAO"
    action="/legislacao"
    intro={{
      eyebrow: "Normas municipais",
      title: "Legislação municipal",
      description: "Pesquise o acervo legislativo preservado por espécie normativa, ano, número ou assunto, com origem e integridade verificáveis.",
    }}
  >
    {resources.length > 0 && <section className="content-section" aria-labelledby="legislation-guidance">
      <div className="page-shell">
        <div className="section-heading"><p className="kicker">Fontes e orientações</p><h2 id="legislation-guidance">Acesso ao acervo legislativo</h2></div>
        <div className="card-grid">{resources.map(item => {
          const url = (item.payload as { url?: string })?.url;
          const content = <><span className="kicker">Conteúdo administrado</span><h3>{item.title}</h3><p>{item.summary}</p>{url && <small>{url.startsWith("http") ? "Abre a fonte oficial em uma nova janela." : "Acessar no portal."}</small>}</>;
          return url
            ? <a className="info-card" key={item.id} href={url} target={url.startsWith("http") ? "_blank" : undefined} rel={url.startsWith("http") ? "noreferrer" : undefined}>{content}</a>
            : <article className="info-card" key={item.id}>{content}</article>;
        })}</div>
      </div>
    </section>}
  </DocumentArchive>;
}
