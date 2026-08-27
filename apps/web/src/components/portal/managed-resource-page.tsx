import type { ReactNode } from "react";
import { EmptyPanel, PageIntro, PublicShell } from "./public-shell";
import { PageBlockRenderer } from "./page-block-renderer";
import type { PortalResource } from "@/lib/portal-api";
type ManagedResourcePageProps = {
  resource: PortalResource | null;
  eyebrow: string;
  fallbackTitle: string;
  fallbackDescription: string;
  children?: ReactNode;
};

export function ManagedResourcePage({ resource, eyebrow, fallbackTitle, fallbackDescription, children }: ManagedResourcePageProps) {
  return <PublicShell>
    <PageIntro eyebrow={eyebrow} title={resource?.title ?? fallbackTitle} description={resource?.summary ?? fallbackDescription} />
    <section className="content-section">
      <div className={resource ? "page-shell detail-grid" : "page-shell"}>
        {resource
          ? <><article className="prose-card"><h2>Informações</h2><ResourcePayload payload={resource.payload} /></article><aside className="side-card"><h2>Governança do conteúdo</h2><p>Esta página é publicada pelo CMS, possui versionamento e trilha de auditoria.</p><small>Versão {resource.version}{resource.publishedAt ? ` · publicada em ${new Intl.DateTimeFormat("pt-BR").format(new Date(resource.publishedAt))}` : ""}</small></aside></>
          : <EmptyPanel title="Conteúdo em atualização" description="Esta área é administrável pelo CMS municipal e ainda não possui publicação ativa." />}
      </div>
    </section>
    {resource && <PageBlockRenderer payload={resource.payload} />}
    {children}
  </PublicShell>;
}
function ResourcePayload({ payload }: { payload: unknown }) {
  if (!payload || typeof payload !== "object" || Array.isArray(payload)) return <p>Informação estruturada indisponível.</p>;
  const data = payload as Record<string, unknown>;
  const content = typeof data.conteudo === "string" ? data.conteudo : typeof data.body === "string" ? data.body : "";
  const sections = Array.isArray(data.sections) ? data.sections.filter((value): value is string => typeof value === "string") : [];
  const entries = Object.entries(data).filter(([key]) => !["classification", "conteudo", "body", "sections", "blocks"].includes(key));
  return <>{content && <p style={{ whiteSpace: "pre-wrap" }}>{content}</p>}{sections.length > 0 && <><h3>Nesta página</h3><ul>{sections.map((section) => <li key={section}>{section}</li>)}</ul></>}{entries.length > 0 && <dl className="definition-list">{entries.map(([key, value]) => <div style={{ display: "contents" }} key={key}><dt>{humanize(key)}</dt><dd>{Array.isArray(value) ? value.join(", ") : typeof value === "object" ? JSON.stringify(value) : String(value)}</dd></div>)}</dl>}{!content && sections.length === 0 && entries.length === 0 && <p>Conteúdo visual administrado pelo CMS.</p>}</>;
}
function humanize(value: string) { return value.replace(/([A-Z])/g, " $1").replace(/^./, (character) => character.toUpperCase()); }
