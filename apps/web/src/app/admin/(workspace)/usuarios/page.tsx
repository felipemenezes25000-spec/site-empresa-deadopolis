import { UserManager } from "@/components/admin/user-manager";

export default function Page() {
  return <><div className="admin-heading"><div><h1>Usuários e RBAC</h1><p>Gerencie contas, papéis, MFA e revogação de sessões com trilha de auditoria.</p></div></div><UserManager /></>;
}
