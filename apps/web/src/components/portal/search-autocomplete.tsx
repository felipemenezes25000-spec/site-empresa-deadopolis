"use client";

import { useEffect, useId, useState } from "react";

type Suggestion = { type: string; title: string; url: string; score: number };

export function SearchAutocomplete({ defaultValue = "" }: { defaultValue?: string }) {
  const listId = useId();
  const [value, setValue] = useState(defaultValue);
  const [suggestions, setSuggestions] = useState<Suggestion[]>([]);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    const query = value.trim();
    if (query.length < 2) {
      setSuggestions([]);
      setLoading(false);
      return;
    }

    const controller = new AbortController();
    const timer = window.setTimeout(() => {
      setLoading(true);
      void fetch(`/api/v1/search/suggest?q=${encodeURIComponent(query)}`, { signal: controller.signal })
        .then(async (response) => {
          if (!response.ok) throw new Error(`Busca de sugestões retornou ${response.status}`);
          const body = await response.json() as { suggestions?: Suggestion[] };
          if (!controller.signal.aborted) setSuggestions(body.suggestions ?? []);
        })
        .catch(() => {
          if (!controller.signal.aborted) setSuggestions([]);
        })
        .finally(() => {
          if (!controller.signal.aborted) setLoading(false);
        });
    }, 220);

    return () => {
      window.clearTimeout(timer);
      controller.abort();
    };
  }, [value]);

  return <div className="content-search">
    <label className="sr-only" htmlFor={`${listId}-input`}>Termo de busca</label>
    <input
      id={`${listId}-input`}
      aria-label="Termo de busca"
      aria-describedby={`${listId}-hint`}
      name="q"
      type="search"
      list={listId}
      autoComplete="off"
      value={value}
      onChange={(event) => setValue(event.target.value)}
      placeholder="Ex.: matrícula, IPTU, licitação"
    />
    <datalist id={listId}>{suggestions.map((item) => <option key={`${item.type}-${item.url}`} value={item.title}>{item.type}</option>)}</datalist>
    <button type="submit">Buscar</button>
    <span id={`${listId}-hint`} className="sr-only" aria-live="polite">{loading ? "Buscando sugestões" : `${suggestions.length} sugestões disponíveis`}</span>
  </div>;
}
