import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import { PageIntro, PublicShell } from "@/components/portal/public-shell";
import { getOpenDataset } from "@/lib/portal-api";

// Todos os datasets compartilhavam um único título estático, embora o título real já seja
// carregado logo abaixo pela mesma função de leitura.
export async function generateMetadata({ params }: { params: Promise<{ slug: string }> }): Promise<Metadata> {
  const { slug } = await params;
  const detail = await getOpenDataset(slug);
  if (!detail) return { title: "Dados Abertos" };
  return {
    title: detail.dataset.title,
    description: detail.dataset.description || "Conjunto de dados abertos publicado pela Prefeitura de Deodápolis/MS.",
    alternates: { canonical: `/dados-abertos/${slug}` },
  };
}

export default async function Page({ params }: { params: Promise<{ slug: string }> }) {
  const { slug } = await params;
  const detail = await getOpenDataset(slug);
  if (!detail) notFound();

  const { dataset, versions } = detail;
  return <PublicShell>
    <PageIntro eyebrow={dataset.category || "Dados Abertos"} title={dataset.title} description={dataset.description} breadcrumb={[{ label: "Início", href: "/" }, { label: "Dados Abertos", href: "/dados-abertos" }, { label: dataset.title }]} />
    <section className="content-section">
      <div className="page-shell">
        <div className="editor-grid">
          <article className="info-card">
            <h2>Metadados</h2>
            <div className="compact-list">
              <div className="compact-item"><span>Órgão responsável</span><strong>{dataset.responsibleDepartment || "Não informado"}</strong></div>
              <div className="compact-item"><span>Periodicidade</span><strong>{dataset.updateFrequency || "Não informada"}</strong></div>
              <div className="compact-item"><span>Licença</span><strong>{dataset.license || "Não informada"}</strong></div>
              <div className="compact-item"><span>Período de referência</span><strong>{dataset.referencePeriod || "Não informado"}</strong></div>
              <div className="compact-item"><span>Última atualização</span><strong>{formatDate(dataset.lastUpdatedAt)}</strong></div>
              <div className="compact-item"><span>Próxima atualização prevista</span><strong>{formatDate(dataset.nextExpectedUpdateAt)}</strong></div>
              {dataset.source && <div className="compact-item"><span>Fonte</span><strong>{dataset.source}</strong></div>}
            </div>
          </article>

          <article className="info-card">
            <h2>Versões disponíveis</h2>
            {versions.length === 0 ? <p>Nenhuma versão disponível.</p> : <div className="compact-list">{versions.map((version) => <div className="compact-item" key={version.version}>
              <div><strong>Versão {version.version} · {version.fileName}</strong><small style={{ display: "block" }}>{version.format} · {formatBytes(version.sizeBytes)} · publicada em {formatDate(version.publishedAt)}</small><small style={{ display: "block", overflowWrap: "anywhere" }}>SHA-256 <code>{version.sha256}</code></small></div>
              <a className="action-button secondary" href={`/api/v1/public/datasets/${dataset.id}/versions/${version.version}/download`}>Baixar</a>
            </div>)}</div>}
          </article>
        </div>
        <div style={{ marginTop: 20 }}><Link href="/dados-abertos">← Voltar ao catálogo de dados</Link></div>
      </div>
    </section>
  </PublicShell>;
}

function formatDate(value: string | null | undefined) {
  if (!value) return "—";
  return new Intl.DateTimeFormat("pt-BR", { dateStyle: "medium" }).format(new Date(value));
}

function formatBytes(value: number) {
  if (value < 1024) return `${value} B`;
  if (value < 1024 * 1024) return `${(value / 1024).toFixed(1)} KB`;
  return `${(value / (1024 * 1024)).toFixed(1)} MB`;
}
