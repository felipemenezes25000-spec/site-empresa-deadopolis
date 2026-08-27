"use client";

import { useMemo, useState } from "react";

export type CommandItem = { id: string; label: string; description?: string; keywords?: string[]; run: () => void };

export function CommandPalette({ open, onClose, items, title = "Comandos" }: { open: boolean; onClose: () => void; items: CommandItem[]; title?: string }) {
  const [query, setQuery] = useState("");
  const filtered = useMemo(() => {
    const normalized = query.trim().toLocaleLowerCase("pt-BR");
    if (!normalized) return items;
    return items.filter((item) => [item.label, item.description, ...(item.keywords ?? [])].filter(Boolean).join(" ").toLocaleLowerCase("pt-BR").includes(normalized));
  }, [items, query]);
  if (!open) return null;
  return <div className="fixed inset-0 z-[70] grid place-items-start bg-black/50 p-4 pt-[10vh]" role="presentation" onMouseDown={(event) => { if (event.currentTarget === event.target) onClose(); }}>
    <section role="dialog" aria-modal="true" aria-labelledby="command-title" className="mx-auto w-full max-w-2xl rounded-xl border border-border bg-surface p-4 shadow-2xl">
      <div className="mb-3 flex items-center justify-between gap-3"><h2 id="command-title" className="text-lg font-bold">{title}</h2><button type="button" onClick={onClose} className="rounded-lg border border-border px-3 py-2">Fechar</button></div>
      <input autoFocus value={query} onChange={(event) => setQuery(event.target.value)} onKeyDown={(event) => { if (event.key === "Escape") onClose(); }} placeholder="Buscar comando..." className="min-h-11 w-full rounded-lg border border-border bg-surface px-3 py-2" />
      <div className="mt-3 max-h-80 overflow-auto" role="listbox">{filtered.map((item) => <button key={item.id} type="button" role="option" aria-selected="false" className="block w-full rounded-lg px-3 py-3 text-left hover:bg-surface-soft" onClick={() => { item.run(); onClose(); }}><strong className="block">{item.label}</strong>{item.description && <small className="text-muted">{item.description}</small>}</button>)}{filtered.length === 0 && <p className="p-3 text-sm text-muted">Nenhum comando encontrado.</p>}</div>
    </section>
  </div>;
}
