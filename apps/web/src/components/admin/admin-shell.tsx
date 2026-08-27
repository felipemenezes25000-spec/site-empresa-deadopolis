"use client";

import Link from "next/link";
import { useEffect, useMemo, useState, type ReactNode } from "react";
import { CommandPalette } from "@/components/ui";

type User = { displayName: string; role: string; capabilities: string[] };

const links = [
  ["/admin", "Visão geral"],
  ["/admin/usuarios", "Usuários e RBAC"],
  ["/admin/comunicacao", "Comunicação"],
  ["/admin/conteudo", "Páginas e blocos"],
  ["/admin/governanca-conteudo", "Governança de conteúdo"],
  ["/admin/servicos", "Serviços"],
  ["/admin/midia", "Mídia"],
  ["/admin/dados-abertos", "Dados Abertos"],
  ["/admin/diario", "Diário Oficial"],
  ["/admin/email", "E-mail institucional"],
  ["/admin/migracao", "Migração"],
  ["/admin/operacoes", "Operações"],
  ["/admin/tickets", "Tickets e SLA"],
  ["/admin/integracoes", "Integrações"],
  ["/admin/compliance", "Compliance"],
] as const;

export function AdminShell({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [error, setError] = useState("");
  const [commandOpen, setCommandOpen] = useState(false);
  const commands = useMemo(() => links.map(([href, label]) => ({ id: href, label, description: `Abrir ${label}`, keywords: ["admin", "navegação"], run: () => window.location.assign(href) })), []);

  useEffect(() => {
    fetch("/api/v1/auth/me", { credentials: "include" })
      .then(async (response) => {
        if (response.status === 401) {
          window.location.replace("/admin/login");
          return;
        }
        if (!response.ok) throw new Error("Falha ao validar sessão");
        setUser(await response.json() as User);
      })
      .catch((reason) => setError(reason instanceof Error ? reason.message : "Falha de sessão"));
  }, []);

  useEffect(() => {
    function handleKeyboard(event: KeyboardEvent) {
      if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === "k") {
        event.preventDefault();
        setCommandOpen((current) => !current);
      }
    }
    window.addEventListener("keydown", handleKeyboard);
    return () => window.removeEventListener("keydown", handleKeyboard);
  }, []);

  async function logout() {
    await fetch("/api/v1/auth/logout", { method: "POST", credentials: "include" });
    window.location.replace("/admin/login");
  }

  if (error) return <main className="login-shell"><div className="login-card"><h1>Não foi possível abrir o painel</h1><p>{error}</p><Link href="/admin/login">Voltar ao login</Link></div></main>;
  if (!user) return <main className="login-shell"><div className="login-card" aria-busy="true">Validando sessão…</div></main>;

  return <div className="admin-shell">
    <header className="admin-topbar"><strong>Deodápolis · Administração do Portal</strong><div className="flex flex-wrap items-center gap-2"><button type="button" className="action-button secondary" onClick={() => setCommandOpen(true)} aria-keyshortcuts="Control+K Meta+K">Buscar área <kbd>Ctrl K</kbd></button><span>{user.displayName} · {user.role}</span><button className="action-button secondary" onClick={logout}>Sair</button></div></header>
    <div className="admin-layout"><aside className="admin-sidebar"><nav aria-label="Administração">{links.map(([href, label]) => <Link key={href} href={href}>{label}</Link>)}</nav></aside><main className="admin-main">{children}</main></div>
    <CommandPalette open={commandOpen} onClose={() => setCommandOpen(false)} items={commands} title="Ir para uma área" />
  </div>;
}
