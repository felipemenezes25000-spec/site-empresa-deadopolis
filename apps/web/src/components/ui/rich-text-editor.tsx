"use client";

import { useRef, type TextareaHTMLAttributes } from "react";

type RichTextEditorProps = Omit<TextareaHTMLAttributes<HTMLTextAreaElement>, "onChange"> & { value: string; onChange: (value: string) => void; label: string };

export function RichTextEditor({ value, onChange, label, id = "rich-text-editor", ...props }: RichTextEditorProps) {
  const ref = useRef<HTMLTextAreaElement>(null);
  function wrap(before: string, after = before) {
    const textarea = ref.current;
    if (!textarea) return;
    const start = textarea.selectionStart;
    const end = textarea.selectionEnd;
    const selected = value.slice(start, end);
    const next = `${value.slice(0, start)}${before}${selected}${after}${value.slice(end)}`;
    onChange(next);
    queueMicrotask(() => { textarea.focus(); textarea.setSelectionRange(start + before.length, end + before.length); });
  }
  return <div className="grid gap-2"><label htmlFor={id} className="font-semibold">{label}</label><div className="flex flex-wrap gap-2" role="toolbar" aria-label={`Formatação de ${label}`}><button type="button" className="rounded-md border border-border px-3 py-1.5 font-bold" onClick={() => wrap("**")}>Negrito</button><button type="button" className="rounded-md border border-border px-3 py-1.5 italic" onClick={() => wrap("_")}>Itálico</button><button type="button" className="rounded-md border border-border px-3 py-1.5" onClick={() => wrap("\n## ", "")}>Título</button><button type="button" className="rounded-md border border-border px-3 py-1.5" onClick={() => wrap("\n- ", "")}>Lista</button><button type="button" className="rounded-md border border-border px-3 py-1.5" onClick={() => wrap("[", "](https://)")}>Link</button></div><textarea ref={ref} id={id} value={value} onChange={(event) => onChange(event.target.value)} className="min-h-56 w-full rounded-lg border border-border bg-surface px-3 py-2 font-mono text-sm" {...props} /></div>;
}
