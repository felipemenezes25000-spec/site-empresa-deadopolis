import type { ReactNode } from "react";

export type StatusSeverity = "ok" | "attention" | "blocked" | "neutral";

const severityByStatus: Record<string, StatusSeverity> = {
  ACTIVE: "ok", ATIVO: "ok", APPROVED: "ok", APROVADA: "ok", AVAILABLE: "ok", CONFIGURED: "ok",
  HEALTHY: "ok", IMPLEMENTED: "ok", OK: "ok", PUBLISHED: "ok", PUBLICADO: "ok", READY: "ok",
  RESOLVED: "ok", SIGNED: "ok", RESOLVIDO: "ok", PUBLICADA: "ok", VALIDADA: "ok", ATIVA: "ok", CONCLUIDO: "ok",
  DISPONIVEL: "ok", OPERACIONAL: "ok",

  APPROVED_PENDING: "attention", DEGRADED: "attention", DEMO_ONLY: "attention",
  DEVELOPMENT_ONLY: "attention", DRAFT: "attention", EXTERNAL_DEPENDENCY: "attention",
  IN_PROGRESS: "attention", EM_ATENDIMENTO: "attention", ABERTO: "attention", IN_REVIEW: "attention", NOT_CONFIGURED: "attention",
  NOT_READY: "attention", PARTIAL: "attention", PENDING: "attention", QUARANTINED: "attention",
  REVIEW: "attention", SCHEDULED: "attention", TESTING_ONLY: "attention", PENDENTE: "attention",
  RASCUNHO: "attention", AGENDADA: "attention", AGENDADO: "attention", EM_REVISAO: "attention",
  QUARENTENA: "attention", DEMONSTRACAO: "attention",

  BLOCKED: "blocked", ERROR: "blocked", EXPIRED: "blocked", FAILED: "blocked", INACTIVE: "blocked",
  INATIVO: "blocked", REJECTED: "blocked", REJEITADA: "blocked", REVOKED: "blocked",
  UNAVAILABLE: "blocked", UNHEALTHY: "blocked", INATIVA: "blocked", FALHA: "blocked",
  ERRO: "blocked", CANCELADA: "blocked", REVOGADA: "blocked", INDISPONIVEL: "blocked",
  ARQUIVADO: "blocked", BROKEN: "blocked",
};

const severityStyles: Record<StatusSeverity, string> = {
  ok: "border-emerald-600 bg-emerald-50 text-emerald-900",
  attention: "border-amber-600 bg-amber-50 text-amber-900",
  blocked: "border-red-700 bg-red-50 text-red-900",
  neutral: "border-border bg-surface-muted text-foreground",
};

const severityLabels: Record<StatusSeverity, string> = {
  ok: "situação confirmada",
  attention: "situação exige atenção",
  blocked: "situação bloqueada ou indisponível",
  neutral: "situação informativa",
};

export function statusSeverity(status: string): StatusSeverity {
  const normalized = status.trim().toUpperCase().replace(/[\s-]+/g, "_");
  return severityByStatus[normalized] ?? "neutral";
}

export function Badge({ children }: { children: ReactNode }) { return <span className="inline-flex rounded-full border border-border bg-surface-muted px-2.5 py-1 text-xs font-bold">{children}</span>; }

export function StatusBadge({ status }: { status: string }) {
  const severity = statusSeverity(status);
  return <span data-severity={severity} title={severityLabels[severity]} className={`inline-flex rounded-full border px-2.5 py-1 text-xs font-extrabold ${severityStyles[severity]}`}>{status}</span>;
}

export function Alert({ title, children, role = "status" }: { title: string; children: ReactNode; role?: "status" | "alert" }) { return <div role={role} className="rounded-xl border border-border bg-surface p-4"><strong className="block">{title}</strong><div className="mt-1 text-sm text-muted">{children}</div></div>; }
export function EmptyState({ title, children }: { title: string; children?: ReactNode }) { return <div className="rounded-xl border border-dashed border-border bg-surface p-8 text-center"><strong>{title}</strong>{children && <div className="mt-2 text-sm text-muted">{children}</div>}</div>; }
export function Skeleton({ className = "" }: { className?: string }) { return <span aria-hidden="true" className={`block animate-pulse rounded bg-surface-muted ${className}`} />; }
