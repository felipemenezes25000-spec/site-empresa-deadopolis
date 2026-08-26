import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { ManagedResourcePage } from "@/components/portal/managed-resource-page";
import { getResource } from "@/lib/portal-api";

const categories: Record<string, { title: string; description: string }> = {
  rreo: { title: "Relatório Resumido da Execução Orçamentária (RREO)", description: "Acervo e publicações do RREO municipal." },
  rgf: { title: "Relatório de Gestão Fiscal (RGF)", description: "Acervo e publicações do Relatório de Gestão Fiscal." },
  convenios: { title: "Convênios", description: "Instrumentos, contratos de convênio e documentos relacionados." },
  "dados-gerais": { title: "Dados Gerais", description: "Documentos gerais de transparência e prestação de contas." },
  ppa: { title: "Plano Plurianual (PPA)", description: "Planos plurianuais e documentos relacionados." },
  ldo: { title: "Lei de Diretrizes Orçamentárias (LDO)", description: "Leis, anexos e documentos das diretrizes orçamentárias." },
  loa: { title: "Lei Orçamentária Anual (LOA)", description: "Leis, anexos e documentos do orçamento anual." },
  balancetes: { title: "Balancetes", description: "Balancetes e demonstrativos periódicos do município." },
  "relatorios-gestao": { title: "Relatórios de Gestão", description: "Relatórios de gestão e documentos de acompanhamento." },
  "recursos-federais": { title: "Recursos Federais", description: "Informações e documentos relativos a recursos federais." },
  "relatorios-gestao-sus": { title: "Relatórios de Gestão do SUS", description: "Relatórios e instrumentos de gestão do Sistema Único de Saúde." },
  cosip: { title: "COSIP — Receitas e Despesas", description: "Demonstrativos de receitas e despesas da contribuição de iluminação pública." },
  balancos: { title: "Balanços", description: "Balanços e demonstrações contábeis disponibilizados pelo município." },
  ufid: { title: "UFID", description: "Publicações e referências municipais relacionadas à UFID." },
  documentos: { title: "Documentos para download", description: "Acervo de documentos públicos preservados durante a migração do portal." },
};

export const dynamic = "force-dynamic";

export async function generateMetadata({ params }: { params: Promise<{ slug: string }> }): Promise<Metadata> {
  const { slug } = await params;
  const category = categories[slug];
  return { title: category?.title ?? "Transparência" };
}

export default async function TransparencyCategoryPage({ params }: { params: Promise<{ slug: string }> }) {
  const { slug } = await params;
  const category = categories[slug];
  if (!category) notFound();

  return <ManagedResourcePage
    resource={await getResource("PAGE", `transparencia-${slug}`)}
    eyebrow="Prestação de contas"
    fallbackTitle={category.title}
    fallbackDescription={`${category.description} O acervo legado está sendo inventariado e somente será publicado após reconciliação e revisão administrativa.`}
  />;
}
