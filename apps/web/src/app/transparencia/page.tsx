import { ExternalLink, FolderSearch } from "lucide-react";
import type { Metadata } from "next";
import { PageIntro, PublicShell } from "@/components/portal/public-shell";
import { transparencyCategories } from "@/lib/transparency-categories";
import { getTransparency, type TransparencyLink } from "@/lib/portal-api";

export const metadata: Metadata = {
  title: "Transparência",
  description: "Receitas, despesas, orçamento, licitações, contratos, prestação de contas e canais de controle social da Prefeitura de Deodápolis/MS.",
};

const fixed: TransparencyLink[] = [
  { title: "Diário Oficial", category: "Atos oficiais", description: "Edições publicadas, hash e verificação de autenticidade.", url: "/diario-oficial" },
  { title: "Dados Abertos", category: "Dados", description: "Catálogo de conjuntos de dados para download e reuso.", url: "/dados-abertos" },
  { title: "Acesso à Informação", category: "e-SIC", description: "Como pedir informações e acompanhar os prazos legais.", url: "/acesso-a-informacao" },
  { title: "Ouvidoria", category: "Participação", description: "Solicitação, reclamação, denúncia, sugestão e elogio.", url: "/ouvidoria" },
  { title: "Licitações", category: "Compras públicas", description: "Avisos, editais, resultados e contratos.", url: "/licitacoes" },
];

// O catálogo vindo da API e a lista fixa se sobrepunham: "Dados Abertos" e "Acesso à Informação"
// apareciam duas vezes, e o próprio /transparencia aparecia como cartão para a página atual.
// A deduplicação é por destino, mantendo a primeira ocorrência com descrição preenchida.
function entryPoints(links: readonly TransparencyLink[]) {
  const merged = new Map<string, TransparencyLink>();
  for (const item of [...fixed, ...links]) {
    const key = item.url.replace(/\/+$/, "").toLowerCase();
    if (key === "/transparencia") continue;
    const existing = merged.get(key);
    if (!existing) merged.set(key, item);
    else if (!existing.description && item.description) merged.set(key, item);
  }
  return [...merged.values()];
}

function isExternal(item: TransparencyLink) {
  // A API é a fonte autoritativa; o teste de prefixo continua como reserva para itens sem o campo.
  return item.isExternal ?? /^https?:\/\//i.test(item.url);
}

const accountability = Object.entries(transparencyCategories).map(([slug, category]) => ({
  slug,
  label: category.shortLabel ?? category.title,
  title: category.title,
  description: category.description,
  isArchive: slug === "documentos",
}));

export default async function TransparencyPage() {
  const links = await getTransparency();
  const archive = accountability.find((item) => item.isArchive);
  const categories = accountability.filter((item) => !item.isArchive);

  return <PublicShell>
    <PageIntro eyebrow="Governo aberto" title="Transparência e participação" description="Um ponto de entrada claro para receitas, despesas, orçamento, licitações, contratos e controle social." />

    <section className="content-section"><div className="page-shell">
      <div className="card-grid">{entryPoints(links).map((item) => <a
        className="info-card"
        key={item.url}
        href={item.url}
        target={isExternal(item) ? "_blank" : undefined}
        rel={isExternal(item) ? "noopener noreferrer" : undefined}
      >
        <span className="kicker">{isExternal(item) ? "Sistema externo oficial" : item.category || "Portal municipal"}</span>
        <h2>{item.title}</h2>
        <p>{item.description}</p>
        {isExternal(item) && <small className="external-note"><ExternalLink size={13} aria-hidden="true" />Abre em nova aba, fora do portal.</small>}
      </a>)}</div>
    </div></section>

    <section className="content-section" aria-labelledby="prestacao-contas-title"><div className="page-shell">
      <div className="section-heading"><div><p className="eyebrow dark">Acervo histórico</p><h2 id="prestacao-contas-title">Prestação de contas</h2></div></div>
      <p className="muted-note">As categorias abaixo preservam a taxonomia do portal legado. Documentos históricos aparecem somente depois de inventário e revisão, evitando perda ou duplicação de acervo.</p>

      {archive && <a className="info-card transparency-archive" href={`/transparencia/${archive.slug}`}>
        <span className="kicker"><FolderSearch size={13} aria-hidden="true" />Busca no acervo</span>
        <h3>{archive.title}</h3>
        <p>{archive.description}</p>
      </a>}

      <div className="card-grid transparency-categories">{categories.map((item) => <a className="info-card" key={item.slug} href={`/transparencia/${item.slug}`}>
        <span className="kicker">Prestação de contas</span>
        <h3>{item.label}</h3>
        <p>{item.description}</p>
      </a>)}</div>
    </div></section>
  </PublicShell>;
}
