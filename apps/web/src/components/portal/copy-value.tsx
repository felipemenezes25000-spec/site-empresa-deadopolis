"use client";

import { Check, Copy } from "lucide-react";
import { useEffect, useRef, useState } from "react";

/**
 * Copia um valor técnico (hash, protocolo, código de acompanhamento) e confirma a ação.
 * A confirmação é anunciada por aria-live porque quem usa leitor de tela não vê o ícone mudar,
 * e o rótulo diz o que está sendo copiado para nunca virar um "Copiar" ambíguo na lista de links.
 */
export function CopyValue({ value, label }: { value: string; label: string }) {
  const [copied, setCopied] = useState(false);
  const [failed, setFailed] = useState(false);
  const timer = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => () => { if (timer.current) clearTimeout(timer.current); }, []);

  async function copy() {
    try {
      await navigator.clipboard.writeText(value);
      setCopied(true);
      setFailed(false);
    } catch {
      // Sem permissão de área de transferência o valor continua selecionável na página.
      setFailed(true);
      setCopied(false);
    }
    if (timer.current) clearTimeout(timer.current);
    timer.current = setTimeout(() => { setCopied(false); setFailed(false); }, 2400);
  }

  return <span className="copy-value">
    <button type="button" className="copy-value-button" onClick={copy}>
      {copied ? <Check size={15} aria-hidden="true" /> : <Copy size={15} aria-hidden="true" />}
      <span>Copiar {label}</span>
    </button>
    <span role="status" aria-live="polite" className="copy-value-feedback">
      {copied ? `${label} copiado.` : failed ? `Não foi possível copiar automaticamente. Selecione o ${label.toLowerCase()} para copiar.` : ""}
    </span>
  </span>;
}
