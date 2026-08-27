import type { ReactNode } from "react";

type OverlayProps = { open: boolean; title: string; children: ReactNode; onClose: () => void; side?: "left" | "right" };

function OverlayPanel({ open, title, children, onClose, side = "right", sheet = false }: OverlayProps & { sheet?: boolean }) {
  if (!open) return null;
  const sideClass = side === "left" ? "left-0" : "right-0";
  const widthClass = sheet ? "w-[min(92vw,56rem)]" : "w-[min(92vw,28rem)]";
  return <div className="fixed inset-0 z-50 bg-black/40" role="presentation" onMouseDown={(event) => { if (event.currentTarget === event.target) onClose(); }}>
    <section role="dialog" aria-modal="true" aria-labelledby="overlay-title" className={`absolute inset-y-0 ${sideClass} ${widthClass} overflow-auto border-border bg-surface p-5 shadow-2xl`}>
      <div className="mb-4 flex items-center justify-between gap-3">
        <h2 id="overlay-title" className="text-xl font-bold">{title}</h2>
        <button type="button" className="min-h-11 rounded-lg border border-border px-3" onClick={onClose} aria-label={`Fechar ${title}`}>Fechar</button>
      </div>
      {children}
    </section>
  </div>;
}

export function Drawer(props: OverlayProps) { return <OverlayPanel {...props} />; }
export function Sheet(props: OverlayProps) { return <OverlayPanel {...props} sheet />; }

export function Popover({ trigger, children }: { trigger: ReactNode; children: ReactNode }) {
  return <details className="relative inline-block"><summary className="cursor-pointer list-none">{trigger}</summary><div className="absolute right-0 z-40 mt-2 min-w-64 rounded-lg border border-border bg-surface p-3 shadow-lg">{children}</div></details>;
}

export function Dropdown({ label, items }: { label: string; items: Array<{ label: string; onSelect: () => void; disabled?: boolean }> }) {
  return <details className="relative inline-block"><summary className="cursor-pointer list-none rounded-lg border border-border px-3 py-2 font-semibold">{label}</summary><div role="menu" className="absolute right-0 z-40 mt-2 min-w-52 rounded-lg border border-border bg-surface p-1 shadow-lg">{items.map((item) => <button key={item.label} type="button" role="menuitem" disabled={item.disabled} className="block w-full rounded-md px-3 py-2 text-left hover:bg-surface-soft disabled:opacity-50" onClick={item.onSelect}>{item.label}</button>)}</div></details>;
}
