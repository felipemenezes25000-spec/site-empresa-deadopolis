import type { FormEvent } from "react";

export function SearchField({ value, onChange, onSubmit, label = "Buscar", placeholder = "Digite para buscar", name = "q" }: { value: string; onChange: (value: string) => void; onSubmit?: (value: string) => void; label?: string; placeholder?: string; name?: string }) {
  function submit(event: FormEvent<HTMLFormElement>) { event.preventDefault(); onSubmit?.(value); }
  return <form role="search" className="flex flex-wrap gap-2" onSubmit={submit}>
    <label className="sr-only" htmlFor={`${name}-search`}>{label}</label>
    <input id={`${name}-search`} name={name} type="search" value={value} onChange={(event) => onChange(event.target.value)} placeholder={placeholder} className="min-h-11 min-w-0 flex-1 rounded-lg border border-border bg-surface px-3 py-2 text-foreground" />
    {onSubmit && <button type="submit" className="min-h-11 rounded-lg border border-border px-4 font-semibold">{label}</button>}
  </form>;
}
