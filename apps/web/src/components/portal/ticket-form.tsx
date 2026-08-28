"use client";

import { useEffect, useRef, useState, type FormEvent } from "react";
import { CopyValue } from "./copy-value";

type Result = { protocol: string; trackingCode: string; status: string; firstResponseDueAt: string; resolutionDueAt: string };
type ProblemDetails = { detail?: string; title?: string; errors?: Record<string, string[]> };

/**
 * Extrai as mensagens de validação por campo que a API devolve. Sem isso o cidadão recebia apenas
 * o título genérico do ProblemDetails ("One or more validation errors occurred.") e não descobria
 * qual campo precisa corrigir.
 */
function readProblem(body: ProblemDetails | null) {
  const fieldMessages = Object.values(body?.errors ?? {}).flat().filter(Boolean);
  if (fieldMessages.length > 0) return fieldMessages.join(" ");
  return body?.detail ?? body?.title ?? "Não foi possível registrar a manifestação.";
}

export function TicketForm() {
  const [result, setResult] = useState<Result | null>(null);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);
  const confirmationRef = useRef<HTMLDivElement | null>(null);

  // Depois de registrar, o foco precisa ir para o comprovante: é onde estão o protocolo e o
  // código, exibidos uma única vez. Sem isso o foco fica no botão de um formulário que sumiu.
  useEffect(() => { if (result) confirmationRef.current?.focus(); }, [result]);

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    // React anula currentTarget ao fim do handler síncrono; guarde a referência antes do await.
    const form = event.currentTarget;
    setError("");
    setLoading(true);
    const data = new FormData(form);
    const payload = {
      requesterName: String(data.get("name") ?? ""),
      contact: String(data.get("contact") ?? ""),
      category: String(data.get("category") ?? ""),
      description: String(data.get("description") ?? ""),
      privacyConsent: data.get("privacyConsent") === "on",
    };

    try {
      const response = await fetch("/api/v1/tickets", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(payload) });
      if (response.ok) {
        setResult(await response.json() as Result);
        form.reset();
      } else {
        setError(readProblem(await response.json().catch(() => null) as ProblemDetails | null));
      }
    } catch {
      // Uma falha de rede não pode deixar o botão em "Enviando…" para sempre.
      setError("Não conseguimos falar com o sistema da Prefeitura agora. Verifique sua conexão e tente registrar novamente.");
    }
    setLoading(false);
  }

  if (result) {
    return <div className="form-message success ticket-receipt" role="status" aria-live="polite" tabIndex={-1} ref={confirmationRef}>
      <h2>Manifestação registrada</h2>
      <p>Guarde os dois dados abaixo. Eles são exibidos apenas agora e são exigidos para consultar a manifestação.</p>

      <div className="ticket-receipt-item">
        <p><strong>Protocolo:</strong> <code>{result.protocol}</code></p>
        <CopyValue value={result.protocol} label="protocolo" />
      </div>
      <div className="ticket-receipt-item">
        <p><strong>Código de acompanhamento:</strong> <code>{result.trackingCode}</code></p>
        <CopyValue value={result.trackingCode} label="código de acompanhamento" />
      </div>

      <p className="muted-note">A Prefeitura não reenvia o código de acompanhamento. Sem ele, esta manifestação não pode ser consultada.</p>
      <a className="action-button" href="/ouvidoria/acompanhar">Acompanhar manifestação</a>
    </div>;
  }

  return <form className="ticket-form" onSubmit={submit}>
    <div className="field"><label htmlFor="name">Nome</label><input id="name" name="name" required maxLength={160} /></div>
    <div className="field"><label htmlFor="contact">E-mail ou telefone</label><input id="contact" name="contact" required maxLength={200} /></div>
    <div className="field"><label htmlFor="category">Tipo</label><select id="category" name="category" required defaultValue="Solicitação"><option>Solicitação</option><option>Reclamação</option><option>Denúncia</option><option>Sugestão</option><option>Elogio</option></select></div>
    <div className="field">
      <label htmlFor="description">Descrição</label>
      <textarea id="description" name="description" required minLength={20} maxLength={4000} rows={7} aria-describedby="description-hint" />
      <small id="description-hint">Descreva o ocorrido com pelo menos 20 caracteres, incluindo local e data quando fizer sentido.</small>
    </div>
    <label><input type="checkbox" name="privacyConsent" required /> Concordo com o tratamento dos dados necessários para este atendimento.</label>
    {error && <div className="form-message error" role="alert">{error}</div>}
    <button className="action-button" disabled={loading}>{loading ? "Enviando…" : "Registrar manifestação"}</button>
  </form>;
}
