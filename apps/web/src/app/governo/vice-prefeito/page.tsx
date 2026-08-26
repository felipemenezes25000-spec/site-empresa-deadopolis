import type { Metadata } from "next";
import { ManagedResourcePage } from "@/components/portal/managed-resource-page";
import { getResource } from "@/lib/portal-api";

export const metadata: Metadata = { title: "Vice-prefeito" };
export const dynamic = "force-dynamic";

export default async function DeputyMayorPage() {
  return <ManagedResourcePage
    resource={await getResource("PAGE", "vice-prefeito")}
    eyebrow="Governo municipal"
    fallbackTitle="Vice-prefeito"
    fallbackDescription="Informações institucionais da Vice-Prefeitura. Dados históricos serão publicados pelo CMS somente após reconciliação e revisão administrativa."
  />;
}
