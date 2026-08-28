import type { Metadata } from "next";
import { ManagedResourcePage } from "@/components/portal/managed-resource-page";
import { getResource } from "@/lib/portal-api";

export const metadata: Metadata = { title: "Calendário de licitações" };

export default async function Page() {
  return <ManagedResourcePage resource={await getResource("PAGE", "calendario-licitacoes")} eyebrow="Compras públicas" fallbackTitle="Calendário de licitações" fallbackDescription="Agenda oficial de sessões e marcos dos processos de contratação pública." breadcrumb={[{ label: "Início", href: "/" }, { label: "Licitações", href: "/licitacoes" }, { label: "Calendário" }]} />;
}
