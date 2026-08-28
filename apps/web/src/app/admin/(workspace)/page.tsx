import { DashboardClient } from "@/components/admin/dashboard-client";

// O cabeçalho dizia "Bom dia" a qualquer hora, inclusive às onze da noite. A saudação agora
// é lida do relógio de Mato Grosso do Sul, que é o fuso de quem opera este painel — e não do
// fuso do servidor, que em produção pode estar em outro lugar.
export const dynamic = "force-dynamic";

function greeting(now: Date) {
  const hour = Number(new Intl.DateTimeFormat("pt-BR", { hour: "numeric", hour12: false, timeZone: "America/Campo_Grande" }).format(now));
  if (hour < 12) return "Bom dia";
  if (hour < 18) return "Boa tarde";
  return "Boa noite";
}

export default function Page() {
  return <>
    <div className="admin-heading">
      <div>
        <p className="eyebrow dark">Workspace municipal</p>
        <h1>{greeting(new Date())}. Aqui está a operação.</h1>
        <p>Conteúdo, atendimento e saúde da plataforma reunidos em um único painel.</p>
      </div>
    </div>
    <DashboardClient />
  </>;
}
