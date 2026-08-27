import type { Metadata } from "next";
import { ManagedResourcePage } from "@/components/portal/managed-resource-page";
import { getResource } from "@/lib/portal-api";

export const metadata: Metadata = { title: "Conselhos municipais" };

export default async function Page() {
  return <ManagedResourcePage resource={await getResource("PAGE", "conselhos")} eyebrow="Participação social" fallbackTitle="Conselhos municipais" fallbackDescription="Composição, atribuições, contatos e documentos dos conselhos municipais." />;
}
