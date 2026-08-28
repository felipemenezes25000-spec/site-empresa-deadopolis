import { sanitizeJsonLd } from "@/lib/utils";

/**
 * Dados estruturados do portal.
 *
 * Regra desta camada: nada aqui pode afirmar o que a plataforma não tem. Endereço, telefone,
 * coordenadas, CNPJ e horário de atendimento não estão modelados em lugar nenhum, então não
 * aparecem — um JSON-LD com dado institucional inventado é pior que a ausência dele, porque
 * mecanismos de busca o publicam como fato oficial do município.
 */
export function StructuredData({ data }: { data: Record<string, unknown> }) {
  return <script
    type="application/ld+json"
    // O payload é montado aqui a partir de dados já carregados, e serializado com escape do
    // delimitador de script antes de ir para o DOM.
    dangerouslySetInnerHTML={{ __html: sanitizeJsonLd(data) }}
  />;
}

export function governmentOrganization(baseUrl: string) {
  return {
    "@context": "https://schema.org",
    "@type": "GovernmentOrganization",
    name: "Prefeitura Municipal de Deodápolis",
    url: baseUrl,
    areaServed: { "@type": "AdministrativeArea", name: "Deodápolis", addressRegion: "MS", addressCountry: "BR" },
  };
}

export function newsArticle(article: { title: string; summary?: string | null; publishedAt?: string | null }, url: string) {
  return {
    "@context": "https://schema.org",
    "@type": "NewsArticle",
    headline: article.title,
    ...(article.summary ? { description: article.summary } : {}),
    ...(article.publishedAt ? { datePublished: article.publishedAt } : {}),
    url,
    publisher: { "@type": "GovernmentOrganization", name: "Prefeitura Municipal de Deodápolis" },
    inLanguage: "pt-BR",
  };
}

export function breadcrumbList(items: ReadonlyArray<{ label: string; href?: string }>, baseUrl: string) {
  return {
    "@context": "https://schema.org",
    "@type": "BreadcrumbList",
    itemListElement: items.map((item, index) => ({
      "@type": "ListItem",
      position: index + 1,
      name: item.label,
      ...(item.href ? { item: `${baseUrl}${item.href}` } : {}),
    })),
  };
}
