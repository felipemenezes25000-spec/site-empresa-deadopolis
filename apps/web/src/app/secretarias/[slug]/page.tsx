import Link from "next/link";
import { notFound } from "next/navigation";
import { PageIntro, PublicShell } from "@/components/portal/public-shell";
import { getDepartment } from "@/lib/portal-api";

export const dynamic = "force-dynamic";

export default async function DepartmentPage({ params }: { params: Promise<{ slug: string }> }) {
  const { slug } = await params;
  const department = await getDepartment(slug);
  if (!department) notFound();

  const hasContact = Boolean(department.phone || department.email || department.address || department.openingHours);

  return <PublicShell>
    <PageIntro eyebrow={department.acronym || "Estrutura municipal"} title={department.name} description="Informações oficiais de contato e atendimento da unidade administrativa." />
    <section className="content-section">
      <div className="page-shell">
        <div className="card-grid">
          <article className="info-card">
            <span className="kicker">Responsável</span>
            <h2>{department.managerName || "Gestor em atualização"}</h2>
            <p>Consulte nesta página os canais publicados pela administração municipal.</p>
          </article>
          <article className="info-card">
            <span className="kicker">Contato</span>
            <h2>Canais de atendimento</h2>
            <p>{department.phone || "Telefone em atualização"}</p>
            <p>{department.email || "E-mail em atualização"}</p>
          </article>
          <article className="info-card">
            <span className="kicker">Atendimento</span>
            <h2>Local e horário</h2>
            <p>{department.address || "Endereço em atualização"}</p>
            <p>{department.openingHours || "Horário em atualização"}</p>
          </article>
        </div>
        {!hasContact && <p className="muted-note">Os dados detalhados desta unidade ainda estão em atualização pelo painel municipal.</p>}
        <p><Link className="section-link" href="/secretarias">← Voltar para Secretarias e órgãos</Link></p>
      </div>
    </section>
  </PublicShell>;
}
