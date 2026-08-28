import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { PageIntro, PublicShell } from "@/components/portal/public-shell";

// Página de apresentação: existe apenas onde o modo demonstração foi ligado explicitamente.
// Sem o flag ela não é uma rota do portal, e por isso responde 404 como qualquer outra.
const presentationMode = () => process.env.PRESENTATION_MODE === "true";

export const metadata: Metadata = {
  title: "Modernização com continuidade",
  description: "Comparativo entre capacidades do portal legado e da nova plataforma municipal, usado em apresentação.",
  robots: { index: false, follow: false },
};

// Cada item descreve uma capacidade que existe hoje na plataforma e o cenário que ela substitui.
// O título é a capacidade nova; o cenário anterior entra como contexto, nunca como deboche.
const capabilities = [
  { capability: "Catálogo de serviços estruturado", detail: "Pesquisável, classificado por área e administrável pela própria Prefeitura.", before: "Carta de Serviços dispersa entre páginas" },
  { capability: "Busca universal por necessidade", detail: "O cidadão descreve o que precisa em vez de descobrir a secretaria responsável.", before: "Conteúdo espalhado por seções independentes" },
  { capability: "CMS com workflow e auditoria", detail: "Rascunho, revisão, agendamento e publicação registrados por autor e data.", before: "Alterações dependentes de equipe de desenvolvimento" },
  { capability: "Continuidade de endereços históricos", detail: "Mapa de redirecionamentos 301 e middleware para preservar links já divulgados.", before: "URLs históricas sem garantia de permanência" },
  { capability: "Integrações identificadas", detail: "Cada sistema externo aparece com estado próprio e é monitorável no painel.", before: "Links externos sem indicação de origem ou situação" },
  { capability: "Diário Oficial verificável", detail: "Composição, PDF, hash SHA-256, QR Code e verificação pública por código.", before: "Publicação sem verificação pública de autenticidade" },
];

export default function Page() {
  if (!presentationMode()) notFound();

  return <PublicShell>
    <PageIntro
      eyebrow="Ambiente de demonstração"
      title="Modernização com continuidade"
      description="Comparativo entre desafios identificados no portal legado e capacidades já implementadas nesta plataforma. Os itens abaixo descrevem funcionalidades existentes, sem projeção de economia, audiência ou prazo."
    />
    <section className="content-section"><div className="page-shell card-grid">
      {capabilities.map((item) => <article className="info-card" key={item.capability}>
        <span className="kicker">Nova plataforma</span>
        <h2>{item.capability}</h2>
        <p>{item.detail}</p>
        <small>Antes: {item.before}</small>
      </article>)}
    </div></section>
  </PublicShell>;
}
