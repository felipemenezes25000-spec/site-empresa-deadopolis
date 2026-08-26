import { DatasetManager } from "@/components/admin/dataset-manager";

export default function Page() {
  return <>
    <div className="admin-heading">
      <div>
        <h1>Dados Abertos</h1>
        <p>Cadastre datasets oficiais, publique versões validadas e acompanhe metadados, hash e histórico de arquivos.</p>
      </div>
    </div>
    <DatasetManager />
  </>;
}
