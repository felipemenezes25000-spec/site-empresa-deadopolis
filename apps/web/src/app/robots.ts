import type { MetadataRoute } from "next";

// Mesmo motivo do sitemap: precisa ser resolvido em execução para apontar ao domínio real.
export const dynamic = "force-dynamic";

export default function robots(): MetadataRoute.Robots {
  const base = (process.env.PUBLIC_PORTAL_URL ?? "http://localhost:3000").replace(/\/+$/, "");
  return {
    rules: {
      userAgent: "*",
      allow: "/",
      // /demo só existe em apresentação e /verificar responde a um código específico:
      // nenhum dos dois é conteúdo institucional que deva ser indexado.
      disallow: ["/admin/", "/api/", "/demo/", "/verificar/"],
    },
    sitemap: `${base}/sitemap.xml`,
  };
}
