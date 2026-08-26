import type { ButtonHTMLAttributes, ReactNode } from "react";

export function IconButton({ label, children, className = "", ...props }: ButtonHTMLAttributes<HTMLButtonElement> & { label: string; children: ReactNode }) {
  return <button aria-label={label} title={label} className={`inline-flex min-h-11 min-w-11 items-center justify-center rounded-lg border border-border bg-surface p-2 font-semibold focus-visible:outline focus-visible:outline-2 ${className}`} {...props}>{children}</button>;
}
