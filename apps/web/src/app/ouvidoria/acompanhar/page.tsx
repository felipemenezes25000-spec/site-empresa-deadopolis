import type { Metadata } from "next";
import Link from "next/link";
import { PageIntro, PublicShell } from "@/components/portal/public-shell";
import { TicketTracker } from "@/components/portal/ticket-tracker";

export const metadata: Metadata = { title: "Acompanhar manifestação" };

export default function Page() {
  return <PublicShell>
    <PageIntro eyebrow="Ouvidoria" title="Acompanhar manifestação" description="Consulte a situação, os prazos e as respostas da sua manifestação usando o protocolo e o código de acompanhamento." />
    <section className="content-section"><div className="page-shell detail-grid">
      <article className="prose-card"><h2>Consultar situação</h2><TicketTracker /></article>
      <aside className="side-card">
        <h2>Não tem os códigos?</h2>
        <p>O protocolo e o código de acompanhamento são exibidos uma única vez, logo após o registro. Eles funcionam como senha: sem os dois, a Prefeitura não pode divulgar o conteúdo da manifestação por telefone nem por e-mail.</p>
        <p>Se você perdeu os códigos, registre uma nova manifestação informando a data aproximada do primeiro registro.</p>
        <Link className="action-button secondary" href="/ouvidoria">Registrar nova manifestação</Link>
      </aside>
    </div></section>
  </PublicShell>;
}
