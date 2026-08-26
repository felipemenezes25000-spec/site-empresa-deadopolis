import { MailManager } from "@/components/admin/mail-manager";

export default function Page() {
  return <>
    <div className="admin-heading">
      <div>
        <h1>E-mail institucional</h1>
        <p>Gerencie domínios, caixas, aliases e pedidos de migração sem ocultar dependências externas ou estados de demonstração.</p>
      </div>
    </div>
    <MailManager />
  </>;
}
