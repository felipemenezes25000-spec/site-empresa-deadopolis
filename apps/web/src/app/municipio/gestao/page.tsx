import type { Metadata } from "next";
import { ManagedResourcePage } from "@/components/portal/managed-resource-page";
import { getResource } from "@/lib/portal-api";

export const metadata: Metadata = { title: "Gestão Municipal" };
export const dynamic = "force-dynamic";

export default async function MunicipalManagementPage() {
  return <ManagedResourcePage
    resource={await getResource("PAGE", "gestao")}
    eyebrow="Institucional"
    fallbackTitle="Gestão Municipal"
    fallbackDescription="Missão, diretrizes e informações institucionais da gestão municipal. O conteúdo legado precisa ser revisado no CMS antes do redirecionamento definitivo."
  />;
}
