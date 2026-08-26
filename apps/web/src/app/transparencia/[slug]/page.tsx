import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { DocumentArchive } from "@/components/portal/document-archive";
import { transparencyCategories } from "@/lib/transparency-categories";

export const dynamic = "force-dynamic";

export async function generateMetadata({ params }: { params: Promise<{ slug: string }> }): Promise<Metadata> {
  const { slug } = await params;
  const category = transparencyCategories[slug];
  return { title: category?.title ?? "Transparência" };
}

export default async function TransparencyCategoryPage({ params, searchParams }: { params: Promise<{ slug: string }>; searchParams: Promise<{ q?: string; category?: string; subcategory?: string; type?: string; year?: string; page?: string }> }) {
  const { slug } = await params;
  const category = transparencyCategories[slug];
  if (!category) notFound();
  if (slug === "documentos") return <DocumentArchive search={await searchParams} />;
  return <DocumentArchive
    search={await searchParams}
    category="PRESTACAO_CONTAS"
    subcategory={category.archiveSubcategory}
    action={`/transparencia/${slug}`}
    intro={{
      eyebrow: "Prestação de contas",
      title: category.title,
      description: `${category.description} Somente documentos aprovados e publicados são exibidos.`,
    }}
  />;
}
