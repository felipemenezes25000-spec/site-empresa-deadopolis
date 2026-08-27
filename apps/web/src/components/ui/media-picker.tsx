"use client";

import { useMemo, useState } from "react";

export type MediaPickerItem = { id: string; name: string; mimeType: string; altText?: string; status?: string };

export function MediaPicker({ items, selectedId, onSelect, label = "Selecionar mídia" }: { items: MediaPickerItem[]; selectedId?: string; onSelect: (item: MediaPickerItem) => void; label?: string }) {
  const [query, setQuery] = useState("");
  const filtered = useMemo(() => items.filter((item) => `${item.name} ${item.altText ?? ""} ${item.mimeType}`.toLocaleLowerCase("pt-BR").includes(query.toLocaleLowerCase("pt-BR"))), [items, query]);
  return <fieldset className="grid gap-3"><legend className="font-semibold">{label}</legend><input type="search" value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Filtrar biblioteca..." className="min-h-11 rounded-lg border border-border bg-surface px-3 py-2" /><div className="grid max-h-72 gap-2 overflow-auto">{filtered.map((item) => <button key={item.id} type="button" aria-pressed={selectedId === item.id} onClick={() => onSelect(item)} className="rounded-lg border border-border p-3 text-left aria-pressed:ring-2 aria-pressed:ring-primary"><strong className="block break-all">{item.name}</strong><small className="text-muted">{item.mimeType}{item.status ? ` · ${item.status}` : ""}</small></button>)}</div></fieldset>;
}
