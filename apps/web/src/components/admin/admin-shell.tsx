"use client";

import {
  Activity,
  Blocks,
  Database,
  FileBadge2,
  Gauge,
  ImageIcon,
  LayoutDashboard,
  LogOut,
  Mail,
  Megaphone,
  Menu,
  MessageSquareText,
  PlugZap,
  Search,
  ShieldCheck,
  Users,
  Waypoints,
  Wrench,
  X,
  type LucideIcon,
} from "lucide-react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { useEffect, useMemo, useState, type ReactNode } from "react";
import { CommandPalette } from "@/components/ui";

type User = { displayName: string; role: string; capabilities: string[] };
type AdminLink = { href: string; label: string; group: "Visão geral" | "Conteúdo" | "Atendimento" | "Plataforma"; icon: LucideIcon };

const links: AdminLink[] = [
  { href: "/admin", label: "Visão geral", group: "Visão geral", icon: LayoutDashboard },
  { href: "/admin/comunicacao", label: "Comunicação", group: "Conteúdo", icon: Megaphone },
  { href: "/admin/conteudo", label: "Páginas e blocos", group: "Conteúdo", icon: Blocks },
  { href: "/admin/governanca-conteudo", label: "Governança", group: "Conteúdo", icon: ShieldCheck },
  { href: "/admin/servicos", label: "Serviços", group: "Conteúdo", icon: Wrench },
  { href: "/admin/midia", label: "Mídia", group: "Conteúdo", icon: ImageIcon },
  { href: "/admin/dados-abertos", label: "Dados Abertos", group: "Conteúdo", icon: Database },
  { href: "/admin/diario", label: "Diário Oficial", group: "Conteúdo", icon: FileBadge2 },
  { href: "/admin/tickets", label: "Tickets e SLA", group: "Atendimento", icon: MessageSquareText },
  { href: "/admin/email", label: "E-mail institucional", group: "Atendimento", icon: Mail },
  { href: "/admin/usuarios", label: "Usuários e RBAC", group: "Plataforma", icon: Users },
  { href: "/admin/integracoes", label: "Integrações", group: "Plataforma", icon: PlugZap },
  { href: "/admin/migracao", label: "Migração", group: "Plataforma", icon: Waypoints },
  { href: "/admin/operacoes", label: "Operações", group: "Plataforma", icon: Activity },
  { href: "/admin/compliance", label: "Compliance", group: "Plataforma", icon: Gauge },
];

const groups = ["Visão geral", "Conteúdo", "Atendimento", "Plataforma"] as const;

export function AdminShell({ children }: { children: ReactNode }) {
  const pathname = usePathname();
  const [user, setUser] = useState<User | null>(null);
  const [error, setError] = useState("");
  const [commandOpen, setCommandOpen] = useState(false);
  const [navOpen, setNavOpen] = useState(false);
  const commands = useMemo(() => links.map(({ href, label }) => ({ id: href, label, description: `Abrir ${label}`, keywords: ["admin", "navegação"], run: () => window.location.assign(href) })), []);

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
    setNavOpen(false);
  }, [pathname]);

  useEffect(() => {
    function handleKeyboard(event: KeyboardEvent) {
      if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === "k") {
        event.preventDefault();
        setCommandOpen((current) => !current);
      }
      if (event.key === "Escape") setNavOpen(false);
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
    <header className="admin-topbar">
      <div className="admin-product-mark"><span className="admin-product-symbol" aria-hidden="true">D</span><span><strong>Deodápolis</strong><small>Portal Municipal · Workspace</small></span></div>
      <div className="admin-top-actions">
        <Link className="admin-portal-link" href="/">Ver portal</Link>
        <button type="button" className="admin-command-trigger" onClick={() => setCommandOpen(true)} aria-keyshortcuts="Control+K Meta+K"><Search size={16} aria-hidden="true" /><span>Buscar</span><kbd>Ctrl K</kbd></button>
        <div className="admin-user-chip"><span className="admin-avatar" aria-hidden="true">{initials(user.displayName)}</span><span><strong>{user.displayName}</strong><small>{user.role}</small></span></div>
        <button className="admin-icon-button" onClick={logout} aria-label="Sair"><LogOut size={18} aria-hidden="true" /></button>
        <button className="admin-icon-button admin-mobile-menu" type="button" onClick={() => setNavOpen((current) => !current)} aria-expanded={navOpen} aria-controls="admin-navigation" aria-label={navOpen ? "Fechar menu" : "Abrir menu"}>{navOpen ? <X size={19} aria-hidden="true" /> : <Menu size={19} aria-hidden="true" />}</button>
      </div>
    </header>
    <div className="admin-layout">
      {navOpen && <button className="admin-nav-backdrop" type="button" aria-label="Fechar menu" onClick={() => setNavOpen(false)} />}
      <aside id="admin-navigation" className={`admin-sidebar${navOpen ? " is-open" : ""}`}>
        <div className="admin-mobile-nav-heading"><span>Navegação</span><button type="button" onClick={() => setNavOpen(false)} aria-label="Fechar menu"><X size={18} aria-hidden="true" /></button></div>
        <nav aria-label="Administração">
          {groups.map((group) => <div className="admin-nav-group" key={group}>
            {group !== "Visão geral" && <p>{group}</p>}
            {links.filter((item) => item.group === group).map(({ href, label, icon: Icon }) => {
              const active = href === "/admin" ? pathname === href : pathname === href || pathname.startsWith(`${href}/`);
              return <Link key={href} href={href} className={active ? "is-active" : undefined} aria-current={active ? "page" : undefined}><Icon size={17} strokeWidth={1.9} aria-hidden="true" /><span>{label}</span></Link>;
            })}
          </div>)}
        </nav>
      </aside>
      <main className="admin-main">{children}</main>
    </div>
    <CommandPalette open={commandOpen} onClose={() => setCommandOpen(false)} items={commands} title="Ir para uma área" />
  </div>;
}

function initials(displayName: string) {
  const parts = displayName.trim().split(/\s+/).filter(Boolean);
  return (parts.length > 1 ? `${parts[0][0]}${parts.at(-1)?.[0] ?? ""}` : parts[0]?.slice(0, 2) ?? "D").toUpperCase();
}
