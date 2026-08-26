import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { ManagedResourcePage } from "@/components/portal/managed-resource-page";
import { getResource } from "@/lib/portal-api";
import { transparencyCategories } from "@/lib/transparency-categories";

export const dynamic = "force-dynamic";

export async function generateMetadata({ params }: { params: Promise<{ slug: string }> }): Promise<Metadata> {
  const { slug } = await params;
  const category = transparencyCategories[slug];
  return { title: category?.title ?? "Transparência" };
}

export default async function TransparencyCategoryPage({ params }: { params: Promise<{ slug: string }> }) {
  const { slug } = await params;
  const category = transparencyCategories[slug];
  if (!category) notFound();

  return <ManagedResourcePage
    resource={await getResource("PAGE", `transparencia-${slug}`)}
    eyebrow="Prestação de contas"
    fallbackTitle={category.title}
    fallbackDescription={`${category.description} O acervo legado está sendo inventariado e somente será publicado após reconciliação e revisão administrativa.`}
  />;
}
