import { MigrationImportManager } from "@/components/admin/migration-import-manager";
import { MigrationManager } from "@/components/admin/migration-manager";

export default function Page() {
  return <>
    <div className="admin-heading">
      <div>
        <h1>Migração do portal legado</h1>
        <p>Inventarie o portal anterior com crawler SSRF-safe, registre evidências, prepare rascunhos CMS e mantenha o mapa de redirects históricos.</p>
      </div>
    </div>
    <MigrationManager />
    <MigrationImportManager />
  </>;
}
