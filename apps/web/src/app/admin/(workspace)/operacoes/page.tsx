import { OperationsManager } from "@/components/admin/operations-manager";

export default function Page() {
  return <>
    <div className="admin-heading">
      <div>
        <h1>Operações</h1>
        <p>Monitore links, registre evidências de backup e mantenha sinais operacionais verificáveis do portal.</p>
      </div>
    </div>
    <OperationsManager />
  </>;
}
