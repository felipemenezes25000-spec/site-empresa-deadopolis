import { FileText, ShieldCheck } from "lucide-react";
import type { Metadata } from "next";
import Link from "next/link";
import { EmptyPanel, PageIntro, PublicShell } from "@/components/portal/public-shell";
import { getGazette } from "@/lib/portal-api";

export const metadata: Metadata = {
  title: "Diário Oficial",
  description: "Edições publicadas do Diário Oficial Eletrônico de Deodápolis/MS, com hash SHA-256 e verificação pública de autenticidade.",
};

// O enum chega como ordinal na resposta pública; TypeName é o rótulo canônico e o mapa abaixo
// cobre respostas antigas sem ele. Um tipo desconhecido é omitido em vez de exibir "0".
const editionTypes: Record<string, string> = {
  Ordinary: "Edição ordinária",
  Extraordinary: "Edição extraordinária",
  Complementary: "Edição complementar",
  "0": "Edição ordinária",
  "1": "Edição extraordinária",
  "2": "Edição complementar",
};

function editionType(edition: { typeName?: string; type: string }) {
  return editionTypes[edition.typeName ?? ""] ?? editionTypes[String(edition.type)] ?? "";
}

function formatDate(value: string) {
  const date = new Date(`${value}T12:00:00Z`);
  return Number.isNaN(date.getTime()) ? value : new Intl.DateTimeFormat("pt-BR", { dateStyle: "long" }).format(date);
}

export default async function GazettePage() {
  const editions = await getGazette();

  return <PublicShell>
    <PageIntro
      eyebrow="Atos oficiais"
      title="Diário Oficial Eletrônico"
      description="Cada edição publicada registra um hash SHA-256 do arquivo entregue. Use a verificação pública para confirmar que um documento em suas mãos é exatamente o que a Prefeitura publicou."
    />
    <section className="content-section"><div className="page-shell">
      {editions.length === 0
        ? <EmptyPanel title="Nenhuma edição publicada" description="O acervo importado e as novas edições aparecerão aqui após validação." />
        : <div className="gazette-list">{editions.map((item) => <article className="gazette-row" key={item.verificationCode ?? `${item.year}-${item.number}`}>
          <div className="gazette-row-main">
            <p className="gazette-row-meta">{[editionType(item), formatDate(item.publicationDate)].filter(Boolean).join(" · ")}</p>
            <h2>Edição {item.number}/{item.year}</h2>
            {item.sha256
              ? <p className="gazette-row-hash"><ShieldCheck size={15} aria-hidden="true" /> SHA-256 <code>{item.sha256.slice(0, 24)}…</code></p>
              : <p className="gazette-row-hash">Hash não registrado para esta edição.</p>}
          </div>
          <div className="gazette-row-actions">
            {item.id && <a className="action-button secondary" href={`/api/v1/gazette/${item.id}/document`}>
              <FileText size={15} aria-hidden="true" />Baixar PDF da edição {item.number}
            </a>}
            {item.verificationCode
              ? <Link className="action-button" href={`/verificar/${item.verificationCode}`}>Verificar edição {item.number}</Link>
              : <span className="status-pill">Sem código de verificação</span>}
          </div>
        </article>)}</div>}
    </div></section>
  </PublicShell>;
}
