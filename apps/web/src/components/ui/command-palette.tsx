"use client";

import { CornerDownLeft, Search, X } from "lucide-react";
import { useMemo, useState } from "react";

export type CommandItem = { id: string; label: string; description?: string; keywords?: string[]; run: () => void };

export function CommandPalette({ open, onClose, items, title = "Comandos" }: { open: boolean; onClose: () => void; items: CommandItem[]; title?: string }) {
  const [query, setQuery] = useState("");
  const [activeIndex, setActiveIndex] = useState(0);
  const filtered = useMemo(() => {
    const normalized = query.trim().toLocaleLowerCase("pt-BR");
    if (!normalized) return items;
    return items.filter((item) => [item.label, item.description, ...(item.keywords ?? [])].filter(Boolean).join(" ").toLocaleLowerCase("pt-BR").includes(normalized));
  }, [items, query]);

  if (!open) return null;

  function close() {
    setQuery("");
    setActiveIndex(0);
    onClose();
  }

  function run(item: CommandItem | undefined) {
    if (!item) return;
    item.run();
    close();
  }

  return <div className="command-palette-overlay" role="presentation" onMouseDown={(event) => { if (event.currentTarget === event.target) close(); }}>
    <section role="dialog" aria-modal="true" aria-labelledby="command-title" className="command-palette-dialog">
      <header className="command-palette-header">
        <div><p>Atalho global</p><h2 id="command-title">{title}</h2></div>
        <button type="button" onClick={close} className="command-palette-close" aria-label="Fechar comandos"><X size={18} aria-hidden="true" /></button>
      </header>
      <div className="command-palette-search">
        <Search size={19} aria-hidden="true" />
        <input
          autoFocus
          value={query}
          onChange={(event) => { setQuery(event.target.value); setActiveIndex(0); }}
          onKeyDown={(event) => {
            if (event.key === "Escape") close();
            if (event.key === "ArrowDown") { event.preventDefault(); setActiveIndex((current) => filtered.length === 0 ? 0 : (current + 1) % filtered.length); }
            if (event.key === "ArrowUp") { event.preventDefault(); setActiveIndex((current) => filtered.length === 0 ? 0 : (current - 1 + filtered.length) % filtered.length); }
            if (event.key === "Enter") { event.preventDefault(); run(filtered[activeIndex]); }
          }}
          placeholder="Digite uma área, recurso ou ação…"
          aria-controls="command-results"
          aria-label="Buscar comando"
        />
        <kbd>ESC</kbd>
      </div>
      <div id="command-results" className="command-palette-results" role="listbox" aria-label="Comandos disponíveis">
        {filtered.map((item, index) => <button key={item.id} type="button" role="option" aria-selected={index === activeIndex} className={index === activeIndex ? "is-active" : undefined} onMouseEnter={() => setActiveIndex(index)} onClick={() => run(item)}><span><strong>{item.label}</strong>{item.description && <small>{item.description}</small>}</span>{index === activeIndex && <span className="command-enter-hint"><CornerDownLeft size={14} aria-hidden="true" /> Enter</span>}</button>)}
        {filtered.length === 0 && <div className="command-empty"><Search size={22} aria-hidden="true" /><strong>Nada por aqui.</strong><span>Tente outro termo para navegar pelo workspace.</span></div>}
      </div>
      <footer className="command-palette-footer"><span><kbd>↑</kbd><kbd>↓</kbd> navegar</span><span><kbd>Enter</kbd> abrir</span><span><kbd>Esc</kbd> fechar</span></footer>
    </section>
  </div>;
}
