import Link from "next/link";
import type { ReactNode } from "react";
import { EmptyPanel, PageIntro, PublicShell } from "./public-shell";
import { PageBlockRenderer } from "./page-block-renderer";
import type { PortalResource } from "@/lib/portal-api";

type ManagedResourcePageProps = {
  resource: PortalResource | null;
  eyebrow: string;
  fallbackTitle: string;
  fallbackDescription: string;
  breadcrumb?: { label: string; href?: string }[];
  children?: ReactNode;
};

export function ManagedResourcePage({ resource, eyebrow, fallbackTitle, fallbackDescription, breadcrumb, children }: ManagedResourcePageProps) {
  return <PublicShell>
    <PageIntro eyebrow={eyebrow} title={resource?.title ?? fallbackTitle} description={resource?.summary ?? fallbackDescription} breadcrumb={breadcrumb} />
    <section className="content-section">
      <div className={resource ? "page-shell detail-grid" : "page-shell"}>
        {resource
          ? <>
            <article className="prose-card"><h2>Informações</h2><ResourcePayload payload={resource.payload} /></article>
            <aside className="side-card">
              {/* Informação útil ao cidadão (quando esta página mudou), sem o discurso interno de
                  CMS, versionamento e trilha de auditoria que estava exposto ao público. */}
              <h2>Atualização desta página</h2>
              {resource.publishedAt
                ? <p>Publicada em {new Intl.DateTimeFormat("pt-BR", { dateStyle: "long" }).format(new Date(resource.publishedAt))}.</p>
                : <p>Esta página ainda não registra data de publicação.</p>}
              <p className="muted-note">Versão {resource.version}</p>
              <h2>Não encontrou o que procurava?</h2>
              <Link className="action-button secondary" href="/contatos">Falar com a Prefeitura</Link>
            </aside>
          </>
          // Antes: "Esta área é administrável pelo CMS municipal e ainda não possui publicação
          // ativa." — linguagem de bastidor, dirigida ao servidor, em dez rotas públicas, e sem
          // nenhum caminho de saída para quem chegou ali.
          : <EmptyPanel
            title="Esta página ainda não tem conteúdo publicado"
            description="A Prefeitura ainda não publicou as informações desta seção. Enquanto isso, você pode buscar o que precisa ou falar diretamente com a administração."
          >
            <div className="empty-state-actions">
              <Link className="action-button" href="/servicos">Ver a Carta de Serviços</Link>
              <Link className="action-button secondary" href="/buscar">Buscar no portal</Link>
              <Link className="action-button secondary" href="/contatos">Falar com a Prefeitura</Link>
            </div>
          </EmptyPanel>}
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
  // `externalSystemState` é estado de integração, não informação ao cidadão: exibi-lo cru numa
  // lista de definições transformava um detalhe interno em conteúdo institucional.
  const entries = Object.entries(data).filter(([key]) => !["classification", "conteudo", "body", "sections", "blocks", "externalSystemState"].includes(key));
  return <>
    {content && <p style={{ whiteSpace: "pre-wrap" }}>{content}</p>}
    {sections.length > 0 && <><h3>Nesta página</h3><ul>{sections.map((section) => <li key={section}>{section}</li>)}</ul></>}
    {entries.length > 0 && <dl className="definition-list">{entries.map(([key, value]) => <div style={{ display: "contents" }} key={key}><dt>{humanize(key)}</dt><dd>{Array.isArray(value) ? value.join(", ") : typeof value === "object" ? JSON.stringify(value) : String(value)}</dd></div>)}</dl>}
    {!content && sections.length === 0 && entries.length === 0 && <p>Esta página ainda não possui informações detalhadas.</p>}
  </>;
}

function humanize(value: string) { return value.replace(/([A-Z])/g, " $1").replace(/^./, (character) => character.toUpperCase()); }
