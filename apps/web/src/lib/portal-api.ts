export type PortalHomeContent = {
  municipality: { name: string; slug: string; stateCode: string; primaryColor: string; logoObjectKey: string | null };
  featuredServices: Array<{ name: string; slug: string; description: string; area: string; isOnline: boolean; onlineUrl: string | null }>;
  latestNews: Array<{ title: string; slug: string; summary: string; coverImageUrl: string | null; coverImageAlt: string | null; isFeatured: boolean; publishedAt: string | null }>;
  transparencyLinks: Array<{ title: string; category: string; url: string; description: string }>;
  integrations: Array<{ provider: string; state: "CONFIGURED" | "DEGRADED" | "UNAVAILABLE" | "NOT_CONFIGURED"; message: string; lastCheckedAt: string }>;
};

export async function getPortalHome(): Promise<PortalHomeContent> {
  const apiUrl = process.env.API_URL ?? "http://localhost:5080";
  const response = await fetch(`${apiUrl}/api/v1/portal/home`, {
    cache: "no-store",
    headers: { "X-Municipality": process.env.MUNICIPALITY_SLUG ?? "deodapolis" },
  });
  if (!response.ok) throw new Error(`Portal API returned ${response.status}`);
  return (await response.json()) as PortalHomeContent;
}
