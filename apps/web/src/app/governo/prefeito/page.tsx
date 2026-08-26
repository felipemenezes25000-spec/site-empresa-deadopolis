import type { Metadata } from "next";
import { ManagedResourcePage } from "@/components/portal/managed-resource-page";
import { getResource } from "@/lib/portal-api";

export const metadata: Metadata = { title: "Prefeito" };
export const dynamic = "force-dynamic";

export default async function MayorPage() {
  return <ManagedResourcePage
    resource={await getResource("PAGE", "prefeito")}
    eyebrow="Governo municipal"
    fallbackTitle="Prefeito"
    fallbackDescription="Informações institucionais do chefe do Poder Executivo municipal. Dados do portal legado serão publicados somente após revisão e validação administrativa."
  />;
}
