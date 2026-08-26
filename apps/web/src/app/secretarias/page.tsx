import type { Metadata } from "next";
import Link from "next/link";
import { EmptyPanel, PageIntro, PublicShell } from "@/components/portal/public-shell";
import { getDepartments } from "@/lib/portal-api";

export const metadata: Metadata = { title: "Secretarias" };

export default async function DepartmentsPage() {
  const items = await getDepartments();

  return <PublicShell>
    <PageIntro eyebrow="Estrutura municipal" title="Secretarias e órgãos" description="Encontre responsáveis, contatos, endereços e horários de atendimento." />
    <section className="content-section">
      <div className="page-shell">
        {items.length === 0
          ? <EmptyPanel title="Diretório em atualização" description="As unidades administrativas serão publicadas pelo painel municipal." />
          : <div className="card-grid">{items.map((item) => <Link className="info-card" href={`/secretarias/${item.slug}`} key={item.slug}>
            <span className="kicker">{item.acronym}</span>
            <h2>{item.name}</h2>
            {item.managerName && <p><strong>Gestor:</strong> {item.managerName}</p>}
            <p>{item.phone || "Telefone em atualização"}<br />{item.email || "E-mail em atualização"}</p>
            <small>{item.openingHours || item.address || "Informações em atualização"}</small>
            <span className="section-link">Ver detalhes →</span>
          </Link>)}</div>}
      </div>
    </section>
  </PublicShell>;
}
