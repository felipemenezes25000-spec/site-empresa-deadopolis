import type { Metadata } from "next";
import Link from "next/link";
import { EmptyPanel, PageIntro, PublicShell } from "@/components/portal/public-shell";
import { getOpenDatasets } from "@/lib/portal-api";

export const metadata: Metadata = { title: "Dados Abertos" };

export default async function Page() {
  const items = await getOpenDatasets();
  return <PublicShell>
    <PageIntro eyebrow="Dados públicos" title="Catálogo de Dados Abertos" description="Bases oficiais estruturadas com órgão responsável, periodicidade, licença, atualização e histórico de versões verificável." />
    <section className="content-section">
      <div className="page-shell">
        {items.length === 0 ? <EmptyPanel title="Nenhum dataset publicado" description="Os conjuntos de dados aparecerão aqui assim que forem versionados e publicados pelo painel municipal." /> : <div className="card-grid">{items.map((item) => <article className="info-card" key={item.id}>
          <span className="kicker">{item.category || "Dados Abertos"}</span>
          <h2><Link href={`/dados-abertos/${item.slug}`}>{item.title}</Link></h2>
          <p>{item.description}</p>
          <small>{item.responsibleDepartment} · {item.updateFrequency}{item.latestVersion ? ` · versão ${item.latestVersion}` : ""}</small>
          <div className="button-row" style={{ marginTop: 16 }}><Link className="action-button secondary" href={`/dados-abertos/${item.slug}`}>Ver dataset</Link></div>
        </article>)}</div>}
      </div>
    </section>
  </PublicShell>;
}
