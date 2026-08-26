export type PortalHomeContent = {
  municipality: { name: string; slug: string; stateCode: string; primaryColor: string; logoObjectKey: string | null };
  featuredServices: ServiceSummary[];
  latestNews: NewsSummary[];
  transparencyLinks: TransparencyLink[];
  integrations: Array<{ provider: string; state: "CONFIGURED" | "DEGRADED" | "UNAVAILABLE" | "NOT_CONFIGURED"; message: string; lastCheckedAt: string }>;
};
export type ServiceSummary = { name: string; slug: string; description: string; area: string; audience?: string; isOnline: boolean; onlineUrl: string | null; expectedDuration?: string; cost?: string };
export type ServiceDetail = ServiceSummary & { requirements: string; documents: string; steps: string; channels: string; phone: string; address: string; openingHours: string; legalBasis: string; lastReviewedAt: string };
export type NewsSummary = { title: string; slug: string; summary: string; coverImageUrl: string | null; coverImageAlt: string | null; isFeatured?: boolean; publishedAt: string | null };
export type NewsDetail = NewsSummary & { body: string; updatedAt: string };
export type Department = { name: string; slug: string; acronym: string; managerName: string; phone: string; email: string; address: string; openingHours: string };
export type TransparencyLink = { title: string; category: string; url: string; description: string; isExternal?: boolean };
export type SearchResult = { type: string; title: string; description: string; url: string };
export type GazetteEdition = { id?: string; number: number; year: number; type: string; publicationDate: string; verificationCode: string | null; sha256: string | null; documentObjectKey: string | null };
export type GazetteVerification = { number: number; year: number; publicationDate: string; sha256: string; verificationCode: string; certificateSubject: string | null; certificateIssuer: string | null; signedAt: string | null; status: string };
export type PortalResource = { id: string; kind: string; slug: string; title: string; summary: string; payload: unknown; displayOrder: number; startsAt: string | null; endsAt: string | null; publishedAt: string | null; version: number };
export type OpenDatasetSummary = {
  id: string;
  title: string;
  slug: string;
  description: string;
  category: string;
  responsibleDepartment: string;
  license: string;
  updateFrequency: string;
  referencePeriod: string | null;
  lastUpdatedAt: string | null;
  nextExpectedUpdateAt: string | null;
  source: string | null;
  latestVersion: number;
};
export type OpenDatasetVersion = {
  version: number;
  fileName: string;
  mimeType: string;
  sizeBytes: number;
  sha256: string;
  format: string;
  metadataJson: string;
  publishedAt: string;
};
export type OpenDatasetDetail = {
  dataset: OpenDatasetSummary & { status?: string | number; createdAt?: string; updatedAt?: string; publishedAt?: string | null };
  versions: OpenDatasetVersion[];
};

const API_URL = process.env.API_URL ?? "http://localhost:5080";
const MUNICIPALITY = process.env.MUNICIPALITY_SLUG ?? "deodapolis";

async function request<T>(path: string, optional = false): Promise<T | null> {
  const response = await fetch(`${API_URL}${path}`, { cache: "no-store", headers: { "X-Municipality": MUNICIPALITY } });
  if (optional && response.status === 404) return null;
  if (!response.ok) throw new Error(`Municipal API ${path} returned ${response.status}`);
  return (await response.json()) as T;
}

export async function getPortalHome() { return (await request<PortalHomeContent>("/api/v1/portal/home"))!; }
export async function getServices(query?: string, area?: string) { const search = new URLSearchParams(); if (query) search.set("query", query); if (area) search.set("area", area); return (await request<ServiceSummary[]>(`/api/v1/services${search.size ? `?${search}` : ""}`))!; }
export async function getService(slug: string) { return request<ServiceDetail>(`/api/v1/services/${encodeURIComponent(slug)}`, true); }
export async function getNews() { return (await request<NewsSummary[]>("/api/v1/news"))!; }
export async function getArticle(slug: string) { return request<NewsDetail>(`/api/v1/news/${encodeURIComponent(slug)}`, true); }
export async function getDepartments() { return (await request<Department[]>("/api/v1/departments"))!; }
export async function getDepartment(slug: string) { return request<Department>(`/api/v1/departments/${encodeURIComponent(slug)}`, true); }
export async function getTransparency() { return (await request<TransparencyLink[]>("/api/v1/transparency"))!; }
export async function searchPortal(q: string) { return (await request<{ query: string; results: SearchResult[] }>(`/api/v1/search?q=${encodeURIComponent(q)}`))!; }
export async function getGazette() { return (await request<GazetteEdition[]>("/api/v1/gazette"))!; }
export async function verifyGazette(code: string) { return request<GazetteVerification>(`/api/v1/gazette/verify/${encodeURIComponent(code)}`, true); }
export async function getResources(kind?: string) { const suffix = kind ? `?kind=${encodeURIComponent(kind)}` : ""; return (await request<PortalResource[]>(`/api/v1/resources${suffix}`))!; }
export async function getResource(kind: string, slug: string) { return request<PortalResource>(`/api/v1/resources/${encodeURIComponent(kind)}/${encodeURIComponent(slug)}`, true); }
export async function getOpenDatasets() { return (await request<OpenDatasetSummary[]>("/api/v1/public/datasets"))!; }
export async function getOpenDataset(slug: string) { return request<OpenDatasetDetail>(`/api/v1/public/datasets/${encodeURIComponent(slug)}`, true); }
