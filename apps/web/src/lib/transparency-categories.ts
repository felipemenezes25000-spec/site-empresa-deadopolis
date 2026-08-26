type TransparencyCategory = { title: string; description: string; archiveSubcategory?: string };

export const transparencyCategories: Record<string, TransparencyCategory> = {
  rreo: { title: "Relatório Resumido da Execução Orçamentária (RREO)", description: "Acervo e publicações do RREO municipal.", archiveSubcategory: "RREO" },
  rgf: { title: "Relatório de Gestão Fiscal (RGF)", description: "Acervo e publicações do Relatório de Gestão Fiscal.", archiveSubcategory: "RGF" },
  convenios: { title: "Convênios", description: "Instrumentos, contratos de convênio e documentos relacionados.", archiveSubcategory: "CONVENIOS" },
  "dados-gerais": { title: "Dados Gerais", description: "Documentos gerais de transparência e prestação de contas.", archiveSubcategory: "DADOS_GERAIS" },
  ppa: { title: "Plano Plurianual (PPA)", description: "Planos plurianuais e documentos relacionados.", archiveSubcategory: "PPA" },
  ldo: { title: "Lei de Diretrizes Orçamentárias (LDO)", description: "Leis, anexos e documentos das diretrizes orçamentárias.", archiveSubcategory: "LDO" },
  loa: { title: "Lei Orçamentária Anual (LOA)", description: "Leis, anexos e documentos do orçamento anual.", archiveSubcategory: "LOA" },
  balancetes: { title: "Balancetes", description: "Balancetes e demonstrativos periódicos do município.", archiveSubcategory: "BALANCETES" },
  "relatorios-gestao": { title: "Relatórios de Gestão", description: "Relatórios de gestão e documentos de acompanhamento.", archiveSubcategory: "RELATORIOS_GESTAO" },
  "recursos-federais": { title: "Recursos Federais", description: "Informações e documentos relativos a recursos federais.", archiveSubcategory: "RECURSOS_FEDERAIS" },
  "relatorios-gestao-sus": { title: "Relatórios de Gestão do SUS", description: "Relatórios e instrumentos de gestão do Sistema Único de Saúde.", archiveSubcategory: "RELATORIOS_GESTAO_SUS" },
  cosip: { title: "COSIP — Receitas e Despesas", description: "Demonstrativos de receitas e despesas da contribuição de iluminação pública.", archiveSubcategory: "COSIP" },
  balancos: { title: "Balanços", description: "Balanços e demonstrações contábeis disponibilizados pelo município.", archiveSubcategory: "BALANCOS" },
  ufid: { title: "UFID", description: "Publicações e referências municipais relacionadas à UFID.", archiveSubcategory: "UFID" },
  documentos: { title: "Documentos para download", description: "Acervo de documentos públicos preservados durante a migração do portal." },
};

export function isTransparencyCategory(slug: string) {
  return Object.hasOwn(transparencyCategories, slug);
}
