import { SearchX } from "lucide-react";
import Link from "next/link";
import { PageIntro, PublicShell } from "@/components/portal/public-shell";

// Escopo do segmento: mantém o 404 real exigido pelos testes de contrato, mas explica o que
// aconteceu em vez de devolver a página genérica. Um código errado não é um erro do cidadão.
export default function VerificationNotFound() {
  return <PublicShell>
    <PageIntro eyebrow="Verificação pública" title="Não encontramos este código" description="Nenhuma edição publicada do Diário Oficial corresponde ao código informado." />
    <section className="content-section"><div className="page-shell detail-grid">
      <article className="prose-card">
        <p className="verification-status"><SearchX size={20} aria-hidden="true" /><strong>Código sem correspondência</strong></p>
        <p>Isso costuma ter uma explicação simples:</p>
        <ul>
          <li>algum caractere pode ter sido trocado na digitação — confira letra por letra;</li>
          <li>o código pode pertencer a uma edição que ainda não foi publicada;</li>
          <li>o documento pode ter sido emitido por outro órgão, fora do Diário de Deodápolis.</li>
        </ul>
        <p>O código de verificação fica impresso no rodapé do PDF e no QR Code da edição. Se preferir, localize a edição pela data na lista pública.</p>
        <Link className="action-button" href="/diario-oficial">Abrir o Diário Oficial</Link>
      </article>
      <aside className="side-card">
        <h2>Precisa de ajuda?</h2>
        <p>Se o código veio de um documento oficial e mesmo assim não for localizado, registre uma manifestação para que a Prefeitura verifique.</p>
        <Link className="action-button secondary" href="/ouvidoria">Falar com a Ouvidoria</Link>
      </aside>
    </div></section>
  </PublicShell>;
}
