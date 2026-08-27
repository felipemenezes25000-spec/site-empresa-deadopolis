import type { Metadata } from "next";
import { ManagedResourcePage } from "@/components/portal/managed-resource-page";
import { getResource } from "@/lib/portal-api";

export const metadata: Metadata = { title: "Perguntas frequentes" };

export default async function Page() {
  return <ManagedResourcePage resource={await getResource("PAGE", "esic-perguntas-frequentes")} eyebrow="Acesso à informação" fallbackTitle="Perguntas frequentes" fallbackDescription="Respostas oficiais para dúvidas recorrentes sobre pedidos, prazos e recursos de acesso à informação." />;
}
