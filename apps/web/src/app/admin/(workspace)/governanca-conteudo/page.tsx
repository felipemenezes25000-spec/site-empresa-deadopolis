import { ContentGovernanceManager } from "@/components/admin/content-governance-manager";

export default function Page() {
  return <><div className="admin-heading"><div><h1>Governança de conteúdo</h1><p>Calendário unificado de publicação, revisão periódica e identificação de conteúdo desatualizado.</p></div></div><ContentGovernanceManager /></>;
}
