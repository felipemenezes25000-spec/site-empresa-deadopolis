export const pageBlockTypes = [
  "Hero", "ServiceSearch", "QuickAccess", "FeaturedNews", "NewsGrid", "ServiceGrid",
  "DepartmentGrid", "Events", "Banner", "Alert", "Documents", "Statistics", "Contact",
  "Video", "Gallery", "CustomLinks",
] as const;

export type PageBlockType = (typeof pageBlockTypes)[number];

export type PageBlockItem = {
  id: string;
  label: string;
  description: string;
  value: string;
  url: string;
  date: string;
  mediaUrl: string;
  mediaAlt: string;
};

export type PageBlock = {
  id: string;
  type: PageBlockType;
  title: string;
  content: string;
  reference: string;
  imageUrl: string;
  imageAlt: string;
  linkLabel: string;
  items: PageBlockItem[];
  enabled: boolean;
};

const allowedTypes = new Set<string>(pageBlockTypes);

export function readPageBlocks(payload: unknown) {
  if (!payload || typeof payload !== "object" || Array.isArray(payload)) return [];
  return normalizePageBlockList((payload as Record<string, unknown>).blocks);
}

export function normalizePageBlockList(value: unknown, options: { includeDisabled?: boolean } = {}): PageBlock[] {
  if (!Array.isArray(value)) return [];
  return value.flatMap((entry, index) => {
    if (!entry || typeof entry !== "object" || Array.isArray(entry)) return [];
    const candidate = entry as Record<string, unknown>;
    const type = boundedText(candidate.type, 32);
    if (!allowedTypes.has(type) || (candidate.enabled === false && !options.includeDisabled)) return [];
    return [{
      id: boundedText(candidate.id, 80) || `block-${index + 1}`,
      type: type as PageBlockType,
      title: boundedText(candidate.title, 220),
      content: boundedText(candidate.content, 4_000),
      reference: boundedText(candidate.reference, 2_048),
      imageUrl: boundedText(candidate.imageUrl, 2_048),
      imageAlt: boundedText(candidate.imageAlt, 500),
      linkLabel: boundedText(candidate.linkLabel, 120),
      items: normalizeItems(candidate.items),
      enabled: candidate.enabled !== false,
    }];
  }).slice(0, 30);
}

export function safeBlockHref(value: string) {
  const normalized = value.trim();
  if (normalized.startsWith("/") && !normalized.startsWith("//")) return normalized;
  try {
    const url = new URL(normalized);
    return url.protocol === "https:" || url.protocol === "http:" ? url.toString() : null;
  } catch {
    return null;
  }
}

export function internalMediaUrl(value: string) {
  const normalized = value.trim();
  return /^\/api\/v1\/media\/[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}(?:\?[^#]*)?$/i.test(normalized)
    ? normalized
    : null;
}

function normalizeItems(value: unknown): PageBlockItem[] {
  if (!Array.isArray(value)) return [];
  return value.flatMap((entry, index) => {
    if (!entry || typeof entry !== "object" || Array.isArray(entry)) return [];
    const candidate = entry as Record<string, unknown>;
    return [{
      id: boundedText(candidate.id, 80) || `item-${index + 1}`,
      label: boundedText(candidate.label, 220),
      description: boundedText(candidate.description, 1_000),
      value: boundedText(candidate.value, 120),
      url: boundedText(candidate.url, 2_048),
      date: boundedText(candidate.date, 40),
      mediaUrl: boundedText(candidate.mediaUrl, 2_048),
      mediaAlt: boundedText(candidate.mediaAlt, 500),
    }];
  }).slice(0, 24);
}

function boundedText(value: unknown, max: number) {
  return typeof value === "string" ? value.trim().slice(0, max) : "";
}
