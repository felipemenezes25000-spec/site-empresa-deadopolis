import { DashboardClient } from "@/components/admin/dashboard-client";

export default function Page() {
  return <>
    <div className="admin-heading">
      <div>
        <p className="eyebrow dark">Workspace municipal</p>
        <h1>Visão geral da operação.</h1>
        <p>Conteúdo, atendimento e saúde da plataforma reunidos em um único painel.</p>
      </div>
    </div>
    <DashboardClient />
  </>;
}
