"use client";

import { ArrowRight, Building2, FileText, Loader2, Newspaper, Search, Wrench } from "lucide-react";
import Link from "next/link";
import { useCallback, useEffect, useId, useRef, useState, type KeyboardEvent } from "react";

type Suggestion = { type: string; title: string; url: string; score: number };

const typeMeta: Record<string, { label: string; icon: typeof Wrench }> = {
  SERVICE: { label: "Serviço", icon: Wrench },
  NEWS: { label: "Notícia", icon: Newspaper },
  DEPARTMENT: { label: "Secretaria", icon: Building2 },
  DOCUMENT: { label: "Documento", icon: FileText },
  GAZETTE: { label: "Diário Oficial", icon: FileText },
};

function meta(type: string) {
  return typeMeta[type.toUpperCase()] ?? { label: type, icon: FileText };
}

/**
 * Busca com sugestões reais do portal. A versão anterior usava <datalist>: as sugestões chegavam
 * com a URL do destino e essa URL era descartada, então escolher "Emitir guia do IPTU" apenas
 * preenchia o texto e ainda exigia passar por uma página de resultados. Aqui a sugestão navega
 * direto para o recurso, que é o motivo de existir uma API de sugestão.
 */
export function SearchAutocomplete({
  defaultValue = "",
  placeholder = "Ex.: segunda via do IPTU, vaga na escola, poda de árvore",
  label = "Buscar no portal",
  variant = "content",
}: {
  defaultValue?: string;
  placeholder?: string;
  label?: string;
  variant?: "content" | "hero";
}) {
  const id = useId();
  const [value, setValue] = useState(defaultValue);
  const [suggestions, setSuggestions] = useState<Suggestion[]>([]);
  const [loading, setLoading] = useState(false);
  const [open, setOpen] = useState(false);
  const [active, setActive] = useState(-1);
  const rootRef = useRef<HTMLDivElement | null>(null);
  const optionRefs = useRef<Array<HTMLAnchorElement | null>>([]);

  const query = value.trim();
  const visible = open && query.length >= 2 ? suggestions : [];

  useEffect(() => {
    const normalized = value.trim();
    if (normalized.length < 2) return;
    const controller = new AbortController();
    const timer = window.setTimeout(() => {
      setLoading(true);
      void fetch(`/api/v1/search/suggest?q=${encodeURIComponent(normalized)}`, { signal: controller.signal })
        .then(async (response) => {
          if (!response.ok) throw new Error(String(response.status));
          const body = await response.json() as { suggestions?: Suggestion[] };
          if (!controller.signal.aborted) { setSuggestions(body.suggestions ?? []); setActive(-1); }
        })
        .catch(() => { if (!controller.signal.aborted) setSuggestions([]); })
        .finally(() => { if (!controller.signal.aborted) setLoading(false); });
    }, 180);
    return () => { window.clearTimeout(timer); controller.abort(); };
  }, [value]);

  // Fecha ao clicar fora, sem capturar o clique que escolhe uma sugestão.
  useEffect(() => {
    function onPointerDown(event: PointerEvent) {
      if (!rootRef.current?.contains(event.target as Node)) setOpen(false);
    }
    document.addEventListener("pointerdown", onPointerDown);
    return () => document.removeEventListener("pointerdown", onPointerDown);
  }, []);

  const change = useCallback((next: string) => {
    setValue(next);
    setOpen(true);
    if (next.trim().length < 2) { setSuggestions([]); setLoading(false); setActive(-1); }
  }, []);

  // Enter sobre a opção ativa dispara o próprio link, então a navegação é a mesma do clique
  // e continua sendo client-side, sem depender do contexto de router.
  function goToActive() {
    optionRefs.current[active]?.click();
  }

  function onKeyDown(event: KeyboardEvent<HTMLInputElement>) {
    // preventDefault é obrigatório: em input[type=search] o Escape nativo apaga o texto digitado,
    // então dispensar a lista de sugestões custaria a consulta inteira.
    if (event.key === "Escape") { event.preventDefault(); setOpen(false); setActive(-1); return; }
    if (visible.length === 0) return;
    if (event.key === "ArrowDown") { event.preventDefault(); setActive((c) => (c + 1) % visible.length); return; }
    if (event.key === "ArrowUp") { event.preventDefault(); setActive((c) => (c - 1 + visible.length) % visible.length); return; }
    // Enter sem seleção mantém o comportamento de sempre: envia o formulário para /buscar.
    if (event.key === "Enter" && active >= 0) { event.preventDefault(); goToActive(); }
  }

  const activeId = active >= 0 && visible[active] ? `${id}-opt-${active}` : undefined;

  return <div className={`portal-search portal-search--${variant}`} ref={rootRef}>
    <div className="portal-search-field">
      <Search size={variant === "hero" ? 21 : 18} aria-hidden="true" />
      <label className="sr-only" htmlFor={`${id}-input`}>{label}</label>
      <input
        id={`${id}-input`}
        name="q"
        type="search"
        autoComplete="off"
        value={value}
        placeholder={placeholder}
        onChange={(event) => change(event.target.value)}
        onFocus={() => setOpen(true)}
        onKeyDown={onKeyDown}
        role="combobox"
        aria-expanded={visible.length > 0}
        aria-controls={`${id}-list`}
        aria-activedescendant={activeId}
        aria-autocomplete="list"
        aria-label={label}
      />
      {loading && query.length >= 2 && <Loader2 className="portal-search-spinner" size={17} aria-hidden="true" />}
      <button type="submit">Buscar</button>
    </div>

    {visible.length > 0 && <div className="portal-search-suggestions" id={`${id}-list`} role="listbox" aria-label="Sugestões">
      {visible.map((item, index) => {
        const { label: typeLabel, icon: Icon } = meta(item.type);
        return <Link
          key={`${item.type}-${item.url}`}
          id={`${id}-opt-${index}`}
          href={item.url}
          ref={(element) => { optionRefs.current[index] = element; }}
          role="option"
          aria-selected={index === active}
          className={index === active ? "is-active" : undefined}
          tabIndex={-1}
          onMouseEnter={() => setActive(index)}
          onClick={() => setOpen(false)}
        >
          <Icon size={16} aria-hidden="true" />
          <span><strong>{item.title}</strong><small>{typeLabel}</small></span>
          <ArrowRight size={15} aria-hidden="true" />
        </Link>;
      })}
    </div>}

    <span className="sr-only" aria-live="polite">
      {loading && query.length >= 2 ? "Buscando sugestões" : visible.length > 0 ? `${visible.length} sugestões disponíveis. Use as setas para navegar.` : ""}
    </span>
  </div>;
}
