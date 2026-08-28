// GERAL cumpria dois papéis com um rótulo só: em /noticias ele é o filtro "mostrar tudo", mas no
// editor é a área da notícia, e o cartão público exibia "TODAS AS ÁREAS" como chapéu da matéria.
// Aqui os rótulos são os da área editorial; o filtro monta sua própria lista logo abaixo.
export const NEWS_CATEGORIES = [
  ["GERAL", "Geral"],
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

/** Opções do filtro público: a primeira entrada significa "sem restrição de área". */
export const NEWS_FILTER_OPTIONS: ReadonlyArray<readonly [string, string]> =
  NEWS_CATEGORIES.map(([value, label]): readonly [string, string] => (value === "GERAL" ? [value, "Todas as áreas"] : [value, label]));

export function newsCategoryLabel(category: string) {
  return NEWS_CATEGORIES.find(([value]) => value === category)?.[1] ?? category.toLocaleLowerCase("pt-BR").replaceAll("_", " ").replace(/^./, character => character.toLocaleUpperCase("pt-BR"));
}
