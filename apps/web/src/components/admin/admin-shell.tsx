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
// `capability` espelha exatamente a claim que a API exige no grupo de endpoints da área.
// Sem `capability` o item é sempre visível (a visão geral só pede sessão autenticada).
type AdminLink = { href: string; label: string; group: "Visão geral" | "Conteúdo" | "Atendimento" | "Plataforma"; icon: LucideIcon; capability?: string };

const links: AdminLink[] = [
  { href: "/admin", label: "Visão geral", group: "Visão geral", icon: LayoutDashboard },
  { href: "/admin/comunicacao", label: "Comunicação", group: "Conteúdo", icon: Megaphone, capability: "content.write" },
  { href: "/admin/conteudo", label: "Páginas e blocos", group: "Conteúdo", icon: Blocks, capability: "resources.manage" },
  { href: "/admin/governanca-conteudo", label: "Governança", group: "Conteúdo", icon: ShieldCheck, capability: "resources.manage" },
  { href: "/admin/servicos", label: "Serviços", group: "Conteúdo", icon: Wrench, capability: "services.manage" },
  { href: "/admin/midia", label: "Mídia", group: "Conteúdo", icon: ImageIcon, capability: "media.manage" },
  { href: "/admin/dados-abertos", label: "Dados Abertos", group: "Conteúdo", icon: Database, capability: "datasets.manage" },
  { href: "/admin/diario", label: "Diário Oficial", group: "Conteúdo", icon: FileBadge2, capability: "gazette.write" },
  { href: "/admin/tickets", label: "Tickets e SLA", group: "Atendimento", icon: MessageSquareText, capability: "support.write" },
  { href: "/admin/email", label: "E-mail institucional", group: "Atendimento", icon: Mail, capability: "mail.manage" },
  { href: "/admin/usuarios", label: "Usuários e RBAC", group: "Plataforma", icon: Users, capability: "users.manage" },
  { href: "/admin/integracoes", label: "Integrações", group: "Plataforma", icon: PlugZap, capability: "settings.manage" },
  { href: "/admin/migracao", label: "Migração", group: "Plataforma", icon: Waypoints, capability: "migration.manage" },
  { href: "/admin/operacoes", label: "Operações", group: "Plataforma", icon: Activity, capability: "operations.manage" },
  { href: "/admin/compliance", label: "Compliance", group: "Plataforma", icon: Gauge, capability: "settings.manage" },
];

const groups = ["Visão geral", "Conteúdo", "Atendimento", "Plataforma"] as const;

export function AdminShell({ children }: { children: ReactNode }) {
  const pathname = usePathname();
  const [user, setUser] = useState<User | null>(null);
  const [error, setError] = useState("");
  const [commandOpen, setCommandOpen] = useState(false);
  const [navOpen, setNavOpen] = useState(false);
  // O RBAC do backend já recusa o acesso; a navegação deixa de oferecer o caminho que terminaria em 403.
  const visibleLinks = useMemo(
    () => links.filter((item) => !item.capability || (user?.capabilities ?? []).includes(item.capability)),
    [user],
  );
  const commands = useMemo(
    () => visibleLinks.map(({ href, label }) => ({ id: href, label, description: `Abrir ${label}`, keywords: ["admin", "navegação"], run: () => window.location.assign(href) })),
    [visibleLinks],
  );

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
    <a className="skip-link" href="#admin-conteudo">Ir para o conteúdo do workspace</a>
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
          {groups.filter((group) => visibleLinks.some((item) => item.group === group)).map((group) => <div className="admin-nav-group" key={group}>
            {group !== "Visão geral" && <p>{group}</p>}
            {visibleLinks.filter((item) => item.group === group).map(({ href, label, icon: Icon }) => {
              const active = href === "/admin" ? pathname === href : pathname === href || pathname.startsWith(`${href}/`);
              return <Link key={href} href={href} onClick={() => setNavOpen(false)} className={active ? "is-active" : undefined} aria-current={active ? "page" : undefined}><Icon size={17} strokeWidth={1.9} aria-hidden="true" /><span>{label}</span></Link>;
            })}
          </div>)}
        </nav>
      </aside>
      <main className="admin-main" id="admin-conteudo" tabIndex={-1}>{children}</main>
    </div>
    {/* Montada só enquanto aberta: fechar pelo atalho Ctrl+K descarta a consulta anterior sem
        precisar zerar estado dentro de um efeito. */}
    {commandOpen && <CommandPalette open onClose={() => setCommandOpen(false)} items={commands} title="Ir para uma área" />}
  </div>;
}

function initials(displayName: string) {
  const parts = displayName.trim().split(/\s+/).filter(Boolean);
  return (parts.length > 1 ? `${parts[0][0]}${parts.at(-1)?.[0] ?? ""}` : parts[0]?.slice(0, 2) ?? "D").toUpperCase();
}
