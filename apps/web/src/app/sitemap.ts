import type { MetadataRoute } from "next";

// Sem isto o Next pré-renderiza o sitemap durante o build da imagem, quando PUBLIC_PORTAL_URL
// ainda não existe, e congela o fallback localhost no artefato de produção.
export const dynamic = "force-dynamic";

const paths = [
  "/",
  "/servicos",
  "/noticias",
  "/secretarias",
  "/municipio",
  "/municipio/gestao",
  "/governo/prefeito",
  "/governo/vice-prefeito",
  "/conselhos",
  "/obras",
  "/transparencia",
  "/diario-oficial",
  "/dados-abertos",
  "/acesso-a-informacao",
  "/acesso-a-informacao/estatisticas",
  "/acesso-a-informacao/perguntas",
  "/ouvidoria",
  "/licitacoes",
  "/licitacoes/calendario",
  "/legislacao",
  "/agenda",
  "/locais",
  "/contatos",
  "/acessibilidade",
  "/privacidade",
];

export default function sitemap(): MetadataRoute.Sitemap {
  // lastModified é omitido de propósito: a plataforma não tem, para estas páginas institucionais,
  // uma data de alteração real a declarar, e a data do build não é essa informação.
  const base = (process.env.PUBLIC_PORTAL_URL ?? "http://localhost:3000").replace(/\/+$/, "");
  return paths.map((path) => ({
    url: `${base}${path}`,
    changeFrequency: path === "/" ? "daily" : "weekly",
    priority: path === "/" ? 1 : 0.7,
  }));
}
