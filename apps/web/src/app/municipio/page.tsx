import type { Metadata } from "next";
import { ManagedResourcePage } from "@/components/portal/managed-resource-page";
import { getResource } from "@/lib/portal-api";

export const metadata: Metadata = { title: "O Município" };
export const dynamic = "force-dynamic";

export default async function MunicipalityPage() {
  return <ManagedResourcePage
    resource={await getResource("PAGE", "municipio")}
    eyebrow="Deodápolis"
    fallbackTitle="O Município"
    fallbackDescription="História, características e informações institucionais de Deodápolis."
  />;
}
