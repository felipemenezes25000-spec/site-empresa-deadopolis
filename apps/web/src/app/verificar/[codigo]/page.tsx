import { BadgeCheck, FileText } from "lucide-react";
import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { CopyValue } from "@/components/portal/copy-value";
import { PageIntro, PublicShell } from "@/components/portal/public-shell";
import { verifyGazette } from "@/lib/portal-api";

export const metadata: Metadata = {
  title: "Verificar documento",
  description: "Confirme a autenticidade de uma edição do Diário Oficial de Deodápolis/MS pelo código de verificação.",
  robots: { index: false, follow: true },
};

const statusLabels: Record<string, string> = { PUBLISHED: "Publicada" };

/**
 * Reconhece uma assinatura ICP-Brasil apenas quando o certificado a afirma explicitamente.
 * A checagem falha fechada de propósito: diante de um certificado que não se identifica, o portal
 * mantém o aviso de demonstração em vez de deixar o cidadão supor validade jurídica.
 */
function isIcpBrasil(subject: string | null, issuer: string | null) {
  const combined = `${subject ?? ""} ${issuer ?? ""}`.toUpperCase();
  if (/N[ÃA]O\s*ICP/.test(combined)) return false;
  return /ICP-?BRASIL/.test(combined);
}

export default async function VerifyPage({ params }: { params: Promise<{ codigo: string }> }) {
  const { codigo } = await params;
  const item = await verifyGazette(codigo);
  if (!item) notFound();

  const officiallySigned = isIcpBrasil(item.certificateSubject, item.certificateIssuer);
  const publicationDate = new Date(`${item.publicationDate}T12:00:00Z`);

  return <PublicShell>
    <PageIntro eyebrow="Verificação pública" title={`Edição ${item.number}/${item.year}`} description="Este código corresponde a uma edição publicada do Diário Oficial. Confira abaixo os dados registrados no momento da publicação." />
    <section className="content-section"><div className="page-shell verification-layout">
      <div className="verification-box verification-found">
        <p className="verification-status"><BadgeCheck size={20} aria-hidden="true" /><strong>Documento localizado</strong></p>
        <p className="verification-explainer">
          A Prefeitura registrou o resumo criptográfico (SHA-256) do arquivo no momento da publicação.
          Se o hash do PDF que você tem em mãos for idêntico ao exibido aqui, o arquivo não foi alterado.
        </p>

        <dl className="definition-list">
          <dt>Situação</dt><dd>{statusLabels[item.status] ?? item.status}</dd>
          <dt>Data de publicação</dt><dd>{Number.isNaN(publicationDate.getTime()) ? item.publicationDate : new Intl.DateTimeFormat("pt-BR", { dateStyle: "long" }).format(publicationDate)}</dd>
          <dt>Código de verificação</dt><dd><code>{item.verificationCode}</code><CopyValue value={item.verificationCode} label="código de verificação" /></dd>
          <dt>SHA-256 do documento</dt><dd className="verification-hash"><code>{item.sha256}</code><CopyValue value={item.sha256} label="hash SHA-256" /></dd>
          <dt>Assinatura</dt><dd>{item.certificateSubject ?? "Sem certificado registrado"}</dd>
          <dt>Emissor</dt><dd>{item.certificateIssuer ?? "—"}</dd>
        </dl>

        {item.id && <a className="action-button" href={`/api/v1/gazette/${item.id}/document`}>
          <FileText size={16} aria-hidden="true" />Baixar o PDF desta edição
        </a>}
      </div>

      <aside className="side-card verification-aside">
        <h2>QR desta verificação</h2>
        <p>Aponte a câmera para abrir esta mesma página de conferência.</p>
        {/* SVG gerado pela própria API a partir do código; next/image não se aplica a este endpoint. */}
        {/* eslint-disable-next-line @next/next/no-img-element */}
        <img className="verification-qr" src={`/api/v1/gazette/verify/${encodeURIComponent(item.verificationCode)}/qr.svg`} alt={`QR Code de verificação da edição ${item.number}/${item.year}`} width={168} height={168} />

        <h2>Alcance desta verificação</h2>
        {officiallySigned
          ? <p>A assinatura registrada declara certificado ICP-Brasil.</p>
          : <p className="warning-box"><strong>Ambiente de demonstração.</strong> A assinatura registrada não é ICP-Brasil e não deve ser interpretada como assinatura oficial. A verificação de integridade acima continua válida: ela confirma que o arquivo não mudou.</p>}
        <p className="muted-note">O carimbo do tempo de autoridade externa ainda não está contratado, portanto a data exibida é a data de publicação registrada pela plataforma, não um carimbo de tempo certificado.</p>
      </aside>
    </div></section>
  </PublicShell>;
}
