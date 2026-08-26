import { Download, ExternalLink, FileCheck2, ShieldCheck } from "lucide-react";
import Link from "next/link";
import { getPublicDocuments, type PublicDocument } from "@/lib/portal-api";
import { EmptyPanel, PageIntro, PublicShell } from "./public-shell";

type ArchiveSearch = { q?: string; category?: string; type?: string; year?: string; page?: string };

export async function DocumentArchive({ search }: { search: ArchiveSearch }) {
  const documents = await getPublicDocuments(search);
  return <PublicShell>
    <PageIntro eyebrow="Memória administrativa" title="Acervo público de documentos" description="Pesquise documentos preservados do portal anterior. Cada arquivo publicado conserva origem, contexto e impressão digital SHA-256." />
    <section className="content-section document-archive">
      <div className="page-shell">
        <form className="archive-filters" role="search" action="/transparencia/documentos">
          <div className="archive-query"><label htmlFor="document-q">Buscar no acervo</label><input id="document-q" name="q" type="search" defaultValue={search.q} placeholder="Título, número ou processo" /></div>
          <div><label htmlFor="document-category">Categoria</label><select id="document-category" name="category" defaultValue={search.category ?? ""}><option value="">Todas</option><option value="LICITACOES">Licitações</option><option value="PRESTACAO_CONTAS">Prestação de contas</option><option value="INFORMATIVOS">Informativos</option><option value="DOCUMENTOS">Documentos gerais</option></select></div>
          <div><label htmlFor="document-type">Tipo</label><select id="document-type" name="type" defaultValue={search.type ?? ""}><option value="">Todos</option><option value="PDF">PDF</option><option value="REPORT">Relatório</option><option value="EDITAL">Edital</option><option value="CONTRATO">Contrato</option><option value="OFFICE">Office</option></select></div>
          <div><label htmlFor="document-year">Ano</label><input id="document-year" name="year" inputMode="numeric" pattern="[0-9]{4}" defaultValue={search.year} placeholder="2025" /></div>
          <button type="submit">Pesquisar</button>
        </form>

        <div className="archive-summary" aria-live="polite"><strong>{documents.total.toLocaleString("pt-BR")}</strong><span>{documents.total === 1 ? "documento publicado" : "documentos publicados"}</span><small>Página {documents.page} de {Math.max(1, documents.totalPages)}</small></div>

        {documents.items.length === 0
          ? <EmptyPanel title="Nenhum documento encontrado" description="Revise os filtros ou pesquise por outro título, número ou processo." />
          : <div className="document-ledger">{documents.items.map(document => <DocumentRow key={document.id} document={document} />)}</div>}

        {documents.totalPages > 1 && <nav className="archive-pagination" aria-label="Paginação do acervo">
          {documents.page > 1 && <Link href={pageHref(search, documents.page - 1)}>Página anterior</Link>}
          <span>Página {documents.page} de {documents.totalPages}</span>
          {documents.page < documents.totalPages && <Link href={pageHref(search, documents.page + 1)}>Próxima página</Link>}
        </nav>}
      </div>
    </section>
  </PublicShell>;
}

function DocumentRow({ document }: { document: PublicDocument }) {
  return <article className="document-record">
    <div className="document-stamp" aria-hidden="true"><FileCheck2 /><strong>{document.documentType}</strong><span>{document.publicationDate?.slice(0, 4) ?? "s/d"}</span></div>
    <div className="document-copy">
      <p className="document-context">{label(document.category)}{document.subcategory ? ` · ${label(document.subcategory)}` : ""}</p>
      <h2>{document.title}</h2>
      {document.description && <p>{document.description}</p>}
      <dl className="document-metadata">
        {document.documentNumber && <><dt>Número</dt><dd>{document.documentNumber}</dd></>}
        {document.processNumber && <><dt>Processo</dt><dd>{document.processNumber}</dd></>}
        {document.referencePeriod && <><dt>Referência</dt><dd>{document.referencePeriod}</dd></>}
        {document.responsibleDepartment && <><dt>Órgão</dt><dd>{document.responsibleDepartment}</dd></>}
        <dt>Arquivo</dt><dd>{document.originalFileName} · {formatBytes(document.sizeBytes)}</dd>
      </dl>
      <div className="document-provenance"><ShieldCheck aria-hidden="true" /><span>Integridade verificada</span><code title={document.sha256}>SHA-256 {document.sha256.slice(0, 16)}…</code><a href={document.sourceUrl} target="_blank" rel="noreferrer">Ver origem <ExternalLink aria-hidden="true" /></a></div>
    </div>
    <a className="document-download" href={document.downloadUrl}><Download aria-hidden="true" />Baixar documento</a>
  </article>;
}

function pageHref(search: ArchiveSearch, page: number) {
  const params = new URLSearchParams();
  for (const [key, value] of Object.entries(search)) if (value && key !== "page") params.set(key, value);
  params.set("page", String(page));
  return `/transparencia/documentos?${params}`;
}

function label(value: string) { return value.toLocaleLowerCase("pt-BR").replaceAll("_", " ").replace(/^./, character => character.toLocaleUpperCase("pt-BR")); }
function formatBytes(bytes: number) { if (bytes < 1024) return `${bytes} B`; if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`; return `${(bytes / 1024 / 1024).toFixed(1)} MB`; }
