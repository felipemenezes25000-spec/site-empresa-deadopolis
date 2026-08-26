import type { InputHTMLAttributes, SelectHTMLAttributes, TextareaHTMLAttributes, ReactNode } from "react";

export function FormField({ label, htmlFor, hint, error, required, children }: { label: string; htmlFor: string; hint?: string; error?: string; required?: boolean; children: ReactNode }) {
  const hintId = hint ? `${htmlFor}-hint` : undefined;
  const errorId = error ? `${htmlFor}-error` : undefined;
  return <div className="grid gap-1.5"><label className="font-semibold" htmlFor={htmlFor}>{label}{required && <span aria-hidden="true"> *</span>}</label>{children}{hint && <small id={hintId} className="text-muted">{hint}</small>}{error && <small id={errorId} role="alert" className="font-semibold text-red-700">{error}</small>}</div>;
}

export function Input({ className = "", ...props }: InputHTMLAttributes<HTMLInputElement>) { return <input className={`min-h-11 w-full rounded-lg border border-border bg-surface px-3 py-2 text-foreground ${className}`} {...props} />; }
export function Textarea({ className = "", ...props }: TextareaHTMLAttributes<HTMLTextAreaElement>) { return <textarea className={`min-h-28 w-full rounded-lg border border-border bg-surface px-3 py-2 text-foreground ${className}`} {...props} />; }
export function Select({ className = "", children, ...props }: SelectHTMLAttributes<HTMLSelectElement>) { return <select className={`min-h-11 w-full rounded-lg border border-border bg-surface px-3 py-2 text-foreground ${className}`} {...props}>{children}</select>; }
export function Checkbox(props: InputHTMLAttributes<HTMLInputElement>) { return <input type="checkbox" className="h-5 w-5 accent-primary" {...props} />; }
export function Radio(props: InputHTMLAttributes<HTMLInputElement>) { return <input type="radio" className="h-5 w-5 accent-primary" {...props} />; }
export function Switch({ checked, ...props }: InputHTMLAttributes<HTMLInputElement>) { return <input type="checkbox" role="switch" checked={checked} className="h-5 w-10 accent-primary" {...props} />; }
