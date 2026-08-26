import type { Metadata } from "next";
import { PageIntro, PublicShell } from "@/components/portal/public-shell";
import { getTransparency } from "@/lib/portal-api";

export const metadata: Metadata = { title: "Transparência" };

const fixed = [
  { title: "Diário Oficial", description: "Atos e publicações", url: "/diario-oficial" },
  { title: "Dados Abertos", description: "Catálogo de datasets", url: "/dados-abertos" },
  { title: "Acesso à Informação", description: "e-SIC e orientações", url: "/acesso-a-informacao" },
  { title: "Ouvidoria", description: "Participação social", url: "/ouvidoria" },
  { title: "Licitações", description: "Compras públicas", url: "/licitacoes" },
];

const accountability = [
  { title: "RREO", description: "Relatório Resumido da Execução Orçamentária", url: "/transparencia/rreo" },
  { title: "RGF", description: "Relatório de Gestão Fiscal", url: "/transparencia/rgf" },
  { title: "PPA", description: "Plano Plurianual", url: "/transparencia/ppa" },
  { title: "LDO", description: "Lei de Diretrizes Orçamentárias", url: "/transparencia/ldo" },
  { title: "LOA", description: "Lei Orçamentária Anual", url: "/transparencia/loa" },
  { title: "Balancetes", description: "Demonstrativos periódicos", url: "/transparencia/balancetes" },
  { title: "Balanços", description: "Balanços e demonstrações contábeis", url: "/transparencia/balancos" },
  { title: "Convênios", description: "Instrumentos e documentos de convênios", url: "/transparencia/convenios" },
  { title: "Relatórios de Gestão", description: "Relatórios gerais e de acompanhamento", url: "/transparencia/relatorios-gestao" },
  { title: "Recursos Federais", description: "Documentos e informações sobre recursos federais", url: "/transparencia/recursos-federais" },
];

export default async function TransparencyPage() {
  const links = await getTransparency();
  return <PublicShell>
    <PageIntro eyebrow="Governo aberto" title="Transparência e participação" description="Um ponto de entrada claro para receitas, despesas, orçamento, licitações, contratos e controle social." />
    <section className="content-section">
      <div className="page-shell">
        <div className="card-grid">{[...links, ...fixed].map((item, index) => <a className="info-card" key={`${item.title}-${index}`} href={item.url} target={item.url.startsWith("http") ? "_blank" : undefined} rel={item.url.startsWith("http") ? "noreferrer" : undefined}>
          <span className="kicker">{item.url.startsWith("http") ? "Sistema externo" : "Portal municipal"}</span>
          <h2>{item.title}</h2>
          <p>{item.description}</p>
          {item.url.startsWith("http") && <small>Você será direcionado para um sistema externo.</small>}
        </a>)}</div>
      </div>
    </section>
    <section className="content-section" aria-labelledby="prestacao-contas-title">
      <div className="page-shell">
        <div className="section-heading"><div><p className="eyebrow dark">Acervo histórico</p><h2 id="prestacao-contas-title">Prestação de contas</h2></div></div>
        <p className="muted-note">As categorias abaixo preservam a taxonomia do portal legado. Documentos históricos aparecem somente depois de inventário e revisão, evitando perda ou duplicação de acervo.</p>
        <div className="card-grid">{accountability.map((item) => <a className="info-card" key={item.url} href={item.url}>
          <span className="kicker">Prestação de contas</span>
          <h3>{item.title}</h3>
          <p>{item.description}</p>
        </a>)}</div>
      </div>
    </section>
  </PublicShell>;
}
