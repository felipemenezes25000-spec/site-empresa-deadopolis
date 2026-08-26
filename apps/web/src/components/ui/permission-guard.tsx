import type { ReactNode } from "react";
export function PermissionGuard({ allowed, children, fallback=null }: { allowed:boolean; children:ReactNode; fallback?:ReactNode }) { return allowed ? <>{children}</> : <>{fallback}</>; }
