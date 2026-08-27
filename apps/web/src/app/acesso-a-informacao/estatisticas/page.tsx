import type { Metadata } from "next";
import { ManagedResourcePage } from "@/components/portal/managed-resource-page";
import { getResource } from "@/lib/portal-api";

export const metadata: Metadata = { title: "Estatísticas do e-SIC" };

export default async function Page() {
  return <ManagedResourcePage resource={await getResource("PAGE", "esic-estatisticas")} eyebrow="Acesso à informação" fallbackTitle="Estatísticas do e-SIC" fallbackDescription="Indicadores oficiais sobre os pedidos de acesso à informação recebidos pelo Município." />;
}
