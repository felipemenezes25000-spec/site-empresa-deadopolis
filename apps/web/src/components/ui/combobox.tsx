"use client";

import { useId, useMemo, useState } from "react";

export type ComboboxOption = { value: string; label: string; description?: string };

type ComboboxProps = {
  label: string;
  options: ComboboxOption[];
  value?: string;
  onChange: (value: string) => void;
  placeholder?: string;
  disabled?: boolean;
  emptyMessage?: string;
};

export function Combobox({ label, options, value, onChange, placeholder = "Buscar...", disabled, emptyMessage = "Nenhuma opção encontrada." }: ComboboxProps) {
  const listId = useId();
  const [query, setQuery] = useState(() => options.find((option) => option.value === value)?.label ?? "");
  const [open, setOpen] = useState(false);
  const filtered = useMemo(() => {
    const normalized = query.trim().toLocaleLowerCase("pt-BR");
    if (!normalized) return options;
    return options.filter((option) => `${option.label} ${option.description ?? ""}`.toLocaleLowerCase("pt-BR").includes(normalized));
  }, [options, query]);

  return <div className="relative grid gap-1.5">
    <label className="font-semibold" htmlFor={`${listId}-input`}>{label}</label>
    <input
      id={`${listId}-input`}
      role="combobox"
      aria-autocomplete="list"
      aria-expanded={open}
      aria-controls={listId}
      aria-activedescendant={value ? `${listId}-${value}` : undefined}
      className="min-h-11 w-full rounded-lg border border-border bg-surface px-3 py-2 text-foreground"
      value={query}
      placeholder={placeholder}
      disabled={disabled}
      onFocus={() => setOpen(true)}
      onChange={(event) => { setQuery(event.target.value); setOpen(true); }}
      onKeyDown={(event) => { if (event.key === "Escape") setOpen(false); }}
    />
    {open && !disabled && <div id={listId} role="listbox" className="absolute left-0 right-0 top-full z-30 mt-1 max-h-64 overflow-auto rounded-lg border border-border bg-surface p-1 shadow-lg">
      {filtered.length === 0 && <div className="px-3 py-2 text-sm text-muted">{emptyMessage}</div>}
      {filtered.map((option) => <button
        id={`${listId}-${option.value}`}
        key={option.value}
        type="button"
        role="option"
        aria-selected={option.value === value}
        className="block w-full rounded-md px-3 py-2 text-left hover:bg-surface-soft focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
        onMouseDown={(event) => event.preventDefault()}
        onClick={() => { onChange(option.value); setQuery(option.label); setOpen(false); }}
      >
        <span className="font-semibold">{option.label}</span>
        {option.description && <small className="block text-muted">{option.description}</small>}
      </button>)}
    </div>}
  </div>;
}

export function Autocomplete(props: ComboboxProps) {
  return <Combobox {...props} />;
}
