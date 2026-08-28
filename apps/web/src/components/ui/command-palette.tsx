"use client";

import { CornerDownLeft, Search, X } from "lucide-react";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";

export type CommandItem = { id: string; label: string; description?: string; keywords?: string[]; run: () => void };

/**
 * Dobra acentos para que "servicos" encontre "Serviços". Em português, digitar sem acento é o
 * caso comum, não a exceção — sem isto a maior parte das buscas do painel não retorna nada.
 * Usa apenas String.normalize (motor JS), sem qualquer dependência de globalização adicional.
 */
function fold(value: string) {
  return value.normalize("NFD").replace(/\p{Diacritic}/gu, "").toLowerCase();
}

export function CommandPalette({ open, onClose, items, title = "Comandos" }: { open: boolean; onClose: () => void; items: CommandItem[]; title?: string }) {
  const [query, setQuery] = useState("");
  const [activeIndex, setActiveIndex] = useState(0);
  const dialogRef = useRef<HTMLElement | null>(null);
  const inputRef = useRef<HTMLInputElement | null>(null);
  const optionsRef = useRef<Array<HTMLButtonElement | null>>([]);
  const restoreFocusRef = useRef<HTMLElement | null>(null);

  const filtered = useMemo(() => {
    const normalized = fold(query.trim());
    if (!normalized) return items;
    return items.filter((item) => fold([item.label, item.description, ...(item.keywords ?? [])].filter(Boolean).join(" ")).includes(normalized));
  }, [items, query]);

  const close = useCallback(() => {
    setQuery("");
    setActiveIndex(0);
    onClose();
  }, [onClose]);

  // Lembra quem abriu a paleta para devolver o foco ao fechar, em vez de despejá-lo no <body>.
  useEffect(() => {
    if (!open) return;
    restoreFocusRef.current = document.activeElement as HTMLElement | null;
    inputRef.current?.focus();
    return () => restoreFocusRef.current?.focus?.();
  }, [open]);

  // Trava a rolagem do fundo enquanto o diálogo modal está aberto.
  useEffect(() => {
    if (!open) return;
    const previous = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    return () => { document.body.style.overflow = previous; };
  }, [open]);

  // A opção ativa precisa permanecer visível quando a navegação é por teclado.
  useEffect(() => {
    optionsRef.current[activeIndex]?.scrollIntoView({ block: "nearest" });
  }, [activeIndex]);

  const move = useCallback((delta: number) => {
    setActiveIndex((current) => (filtered.length === 0 ? 0 : (current + delta + filtered.length) % filtered.length));
  }, [filtered.length]);

  const run = useCallback((item: CommandItem | undefined) => {
    if (!item) return;
    item.run();
    close();
  }, [close]);

  // Escape e as setas precisam funcionar em qualquer ponto do diálogo, não só dentro do campo:
  // ao chegar a uma opção pelo mouse ou por Tab, o teclado deixava de responder.
  function onDialogKeyDown(event: React.KeyboardEvent<HTMLElement>) {
    if (event.key === "Escape") { event.preventDefault(); close(); return; }
    if (event.key === "ArrowDown") { event.preventDefault(); move(1); return; }
    if (event.key === "ArrowUp") { event.preventDefault(); move(-1); return; }
    if (event.key === "Home") { event.preventDefault(); setActiveIndex(0); return; }
    if (event.key === "End") { event.preventDefault(); setActiveIndex(Math.max(0, filtered.length - 1)); return; }
    if (event.key === "Enter") { event.preventDefault(); run(filtered[activeIndex]); return; }
    if (event.key !== "Tab") return;

    // Mantém o foco dentro do diálogo modal.
    const focusable = dialogRef.current?.querySelectorAll<HTMLElement>('button:not([disabled]), input, [href], [tabindex]:not([tabindex="-1"])');
    if (!focusable || focusable.length === 0) return;
    const first = focusable[0];
    const last = focusable[focusable.length - 1];
    if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus(); }
    else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus(); }
  }

  if (!open) return null;

  const activeId = filtered[activeIndex] ? `command-option-${filtered[activeIndex].id.replace(/[^\w-]/g, "-")}` : undefined;

  return <div className="command-palette-overlay" role="presentation" onMouseDown={(event) => { if (event.currentTarget === event.target) close(); }}>
    <section
      role="dialog"
      aria-modal="true"
      aria-labelledby="command-title"
      className="command-palette-dialog"
      ref={dialogRef}
      onKeyDown={onDialogKeyDown}
    >
      <header className="command-palette-header">
        <div><p>Atalho global</p><h2 id="command-title">{title}</h2></div>
        <button type="button" onClick={close} className="command-palette-close" aria-label="Fechar comandos"><X size={18} aria-hidden="true" /></button>
      </header>
      <div className="command-palette-search">
        <Search size={19} aria-hidden="true" />
        <input
          ref={inputRef}
          value={query}
          onChange={(event) => { setQuery(event.target.value); setActiveIndex(0); }}
          placeholder="Digite uma área, recurso ou ação…"
          role="combobox"
          aria-expanded="true"
          aria-controls="command-results"
          aria-activedescendant={activeId}
          aria-autocomplete="list"
          aria-label="Buscar comando"
        />
        <kbd>ESC</kbd>
      </div>

      {/* O listbox só existe quando há opções: um estado vazio dentro dele seria um filho inválido. */}
      {filtered.length > 0
        ? <div id="command-results" className="command-palette-results" role="listbox" aria-label="Comandos disponíveis">
          {filtered.map((item, index) => <button
            key={item.id}
            id={`command-option-${item.id.replace(/[^\w-]/g, "-")}`}
            ref={(element) => { optionsRef.current[index] = element; }}
            type="button"
            role="option"
            tabIndex={-1}
            aria-selected={index === activeIndex}
            className={index === activeIndex ? "is-active" : undefined}
            onMouseEnter={() => setActiveIndex(index)}
            onClick={() => run(item)}
          >
            <span><strong>{item.label}</strong>{item.description && <small>{item.description}</small>}</span>
            {index === activeIndex && <span className="command-enter-hint"><CornerDownLeft size={14} aria-hidden="true" /> Enter</span>}
          </button>)}
        </div>
        : <div id="command-results" className="command-palette-results">
          <div className="command-empty" role="status"><Search size={22} aria-hidden="true" /><strong>Nada por aqui.</strong><span>Tente outro termo para navegar pelo workspace.</span></div>
        </div>}

      <footer className="command-palette-footer"><span><kbd>↑</kbd><kbd>↓</kbd> navegar</span><span><kbd>Enter</kbd> abrir</span><span><kbd>Esc</kbd> fechar</span></footer>
    </section>
  </div>;
}
