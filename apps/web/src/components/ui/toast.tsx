export type ToastItem = { id: string; title: string; description?: string; tone?: "info" | "success" | "warning" | "error" };

export function ToastRegion({ items }: { items: ToastItem[] }) {
  return <div className="fixed bottom-4 right-4 z-[60] grid w-[min(92vw,24rem)] gap-2" aria-live="polite" aria-relevant="additions removals">
    {items.map((item) => <div key={item.id} role={item.tone === "error" ? "alert" : "status"} className="rounded-xl border border-border bg-surface p-4 shadow-lg">
      <strong className="block">{item.title}</strong>
      {item.description && <span className="mt-1 block text-sm text-muted">{item.description}</span>}
    </div>)}
  </div>;
}
