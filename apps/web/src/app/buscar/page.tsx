import type { Metadata } from "next";
import Link from "next/link";
import { EmptyPanel, PageIntro, PublicShell } from "@/components/portal/public-shell";
import { SearchAutocomplete } from "@/components/portal/search-autocomplete";
import { searchPortal } from "@/lib/portal-api";

export const metadata: Metadata = { title: "Busca" };

export default async function SearchPage({ searchParams }: { searchParams: Promise<{ q?: string }> }) {
  const { q = "" } = await searchParams;
  const normalizedQuery = q.trim();
  const result = normalizedQuery.length >= 2 ? await searchPortal(normalizedQuery) : null;

  return <PublicShell>
    <PageIntro eyebrow="Busca universal" title="Encontre serviços e informações" description="Pesquise pelo que precisa, sem precisar saber qual secretaria é responsável." />
    <section className="content-section"><div className="page-shell">
      <form className="portal-search-form" role="search" action="/buscar"><SearchAutocomplete defaultValue={q} placeholder="Ex.: matrícula, IPTU, licitação" /></form>
      {!result
        ? <EmptyPanel title="Digite pelo menos dois caracteres" description="A busca consulta serviços, notícias, secretarias, páginas, dados abertos, documentos e Diário Oficial." />
        : result.results.length === 0
          ? <EmptyPanel title="Nenhum resultado" description={`Não encontramos “${q}”. Esse termo pode ser analisado pela equipe editorial para melhorar a descoberta de conteúdo.`} />
          : <>
            {result.usedFuzzy && <div className="form-message" role="status">Não houve correspondência literal forte. Mostramos resultados aproximados considerando possíveis erros de digitação.</div>}
            <div className="card-grid">{result.results.map((item, index) => <Link className="info-card" key={`${item.url}-${index}`} href={item.url}>
              <span className="kicker">{typeLabel(item.type)}</span>
              <h2>{item.title}</h2>
              <p>{item.description}</p>
            </Link>)}</div>
          </>}
    </div></section>
  </PublicShell>;
}

function typeLabel(type: string) {
  return ({ SERVICE: "Serviço", NEWS: "Notícia", DEPARTMENT: "Secretaria", PAGE: "Página", DATASET: "Dados abertos", DOCUMENT: "Documento", GAZETTE: "Diário Oficial" } as Record<string, string>)[type] ?? type;
}
