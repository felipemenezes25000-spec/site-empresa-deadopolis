export const NEWS_CATEGORIES = [
  ["GERAL", "Todas as áreas"],
  ["PREFEITURA", "Prefeitura"],
  ["EDUCACAO", "Educação"],
  ["INFRAESTRUTURA", "Infraestrutura"],
  ["SAUDE", "Saúde"],
  ["ESPORTE", "Esporte"],
  ["MEIO_AMBIENTE", "Meio ambiente"],
  ["CULTURA", "Cultura"],
  ["ASSISTENCIA_SOCIAL", "Assistência social"],
  ["HABITACAO", "Habitação"],
  ["COVID_19", "Covid-19"],
] as const;

export function newsCategoryLabel(category: string) {
  return NEWS_CATEGORIES.find(([value]) => value === category)?.[1] ?? category.toLocaleLowerCase("pt-BR").replaceAll("_", " ").replace(/^./, character => character.toLocaleUpperCase("pt-BR"));
}
