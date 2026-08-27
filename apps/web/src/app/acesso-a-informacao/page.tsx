import type { Metadata } from "next";
import Link from "next/link";
import { ManagedResourcePage } from "@/components/portal/managed-resource-page";
import { getResource } from "@/lib/portal-api";

export const metadata: Metadata = { title: "Acesso à Informação" };

export default async function Page() {
  return <ManagedResourcePage
    resource={await getResource("PAGE", "acesso-a-informacao")}
    eyebrow="e-SIC"
    fallbackTitle="Acesso à Informação"
    fallbackDescription="Orientações, prazos e acesso ao canal eletrônico de pedidos de informação."
  >
    <section className="content-section" aria-labelledby="esic-complementary-information">
      <div className="page-shell">
        <div className="section-heading">
          <p className="kicker">Transparência passiva</p>
          <h2 id="esic-complementary-information">Informações complementares do e-SIC</h2>
        </div>
        <div className="card-grid">
          <Link className="info-card" href="/acesso-a-informacao/estatisticas"><span className="kicker">Acompanhamento</span><h3>Estatísticas do e-SIC</h3><p>Consulte os dados oficiais publicados pela Prefeitura sobre pedidos de acesso à informação.</p><span className="section-link">Consultar estatísticas →</span></Link>
          <Link className="info-card" href="/acesso-a-informacao/perguntas"><span className="kicker">Orientação</span><h3>Perguntas frequentes</h3><p>Encontre respostas oficiais para dúvidas recorrentes sobre o acesso à informação.</p><span className="section-link">Consultar perguntas →</span></Link>
        </div>
      </div>
    </section>
  </ManagedResourcePage>;
}
