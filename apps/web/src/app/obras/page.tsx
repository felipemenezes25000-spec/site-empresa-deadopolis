import type { Metadata } from "next";
import { ManagedResourcePage } from "@/components/portal/managed-resource-page";
import { getResource } from "@/lib/portal-api";

export const metadata: Metadata = { title: "Obras municipais" };

export default async function Page() {
  return <ManagedResourcePage resource={await getResource("PAGE", "obras")} eyebrow="Infraestrutura" fallbackTitle="Obras municipais" fallbackDescription="Informações e acompanhamento de obras públicas, publicados após validação da Prefeitura e das fontes responsáveis." />;
}
