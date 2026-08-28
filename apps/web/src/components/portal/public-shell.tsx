import { CircleHelp, Headphones, Home, LayoutGrid, Menu, Search } from "lucide-react";
import Image from "next/image";
import Link from "next/link";
import type { ReactNode } from "react";
import { Breadcrumb } from "@/components/ui/navigation";
import { getResources, type PortalResource } from "@/lib/portal-api";

const fallbackNavigation = [
  ["/servicos", "Serviços"], ["/noticias", "Notícias"], ["/secretarias", "Secretarias"], ["/transparencia", "Transparência"], ["/diario-oficial", "Diário Oficial"]
] as const;
const fallbackFooter = [["/servicos", "Carta de Serviços"], ["/dados-abertos", "Dados Abertos"], ["/acesso-a-informacao", "e-SIC"], ["/privacidade", "Privacidade e LGPD"]] as const;

type MenuPayload = { label?: string; url?: string; parent?: string; external?: boolean; enabled?: boolean; placement?: string };
type MenuNode = { resource: PortalResource; payload: MenuPayload; children: MenuNode[] };

export async function PublicShell({ children }: { children: ReactNode }) {
  return <PortalChrome menuResources={await readMenuResources()} presentationMode={process.env.PRESENTATION_MODE === "true"}>
    <main id="conteudo-principal">{children}</main>
  </PortalChrome>;
}

export async function readMenuResources(): Promise<PortalResource[]> {
  try { return await getResources("MENU"); } catch { return []; }
}

export function PortalChrome({ menuResources, presentationMode = false, children }: { menuResources: PortalResource[]; presentationMode?: boolean; children: ReactNode }) {
  const headerMenu = buildMenuTree(menuResources, "HEADER");
  const institutionalMenu = buildMenuTree(menuResources, "INSTITUTIONAL");
  const servicesMenu = buildMenuTree(menuResources, "SERVICES");
  const quickMenu = buildMenuTree(menuResources, "QUICK_ACCESS");
  const footerMenu = buildMenuTree(menuResources, "FOOTER");
  const hasManagedHeader = headerMenu.length + institutionalMenu.length + servicesMenu.length > 0;

  return <div className="public-page">
    <a className="skip-link" href="#conteudo-principal">Ir para o conteúdo principal</a>
    {presentationMode && <div className="demo-bar" role="status"><span className="demo-dot" aria-hidden="true" />Ambiente de demonstração</div>}
    <header className="site-header">
      <div className="utility-bar"><div className="page-shell utility-inner"><p>Deodápolis · Mato Grosso do Sul</p><div className="utility-links">{quickMenu.flatMap(flattenMenu).slice(0, 5).map((node) => <MenuLink key={node.resource.id} node={node} />)}<Link href="/acessibilidade">Acessibilidade</Link><Link href="/contatos">Contatos</Link><Link href="/admin/login">Área administrativa</Link></div></div></div>
      <div className="page-shell brand-row"><Link className="municipal-brand" href="/" aria-label="Prefeitura de Deodápolis — início"><Image src="/brand/deodapolis-logo.png" width={278} height={74} priority alt="Prefeitura de Deodápolis — Juntos por um futuro ainda melhor" /></Link><div className="header-actions"><Link className="header-shortcut" href="/acesso-a-informacao"><CircleHelp size={18} aria-hidden="true" /> Acesso à informação</Link><Link className="header-shortcut" href="/ouvidoria"><Headphones size={18} aria-hidden="true" /> Ouvidoria</Link></div></div>
      <nav className="main-nav" aria-label="Navegação principal"><div className="page-shell nav-inner"><details className="mobile-navigation"><summary><Menu size={22} aria-hidden="true" /> Menu</summary><div>{hasManagedHeader ? <>{headerMenu.map((node) => <MobileMenuNode key={node.resource.id} node={node} />)}{institutionalMenu.length > 0 && <MobileMenuGroup label="Institucional" nodes={institutionalMenu} />}{servicesMenu.length > 0 && <MobileMenuGroup label="Serviços" nodes={servicesMenu} />}</> : fallbackNavigation.map(([href, label]) => <Link key={href} href={href}>{label}</Link>)}</div></details><div className="desktop-nav-links">{hasManagedHeader ? <>{headerMenu.map((node) => <DesktopMenuNode key={node.resource.id} node={node} />)}{institutionalMenu.length > 0 && <DesktopMenuGroup label="Institucional" nodes={institutionalMenu} />}{servicesMenu.length > 0 && <DesktopMenuGroup label="Serviços" nodes={servicesMenu} />}</> : fallbackNavigation.map(([href, label]) => <Link key={href} href={href}>{label}</Link>)}</div><Link className="emergency-link" href="/contatos#emergencia">Telefones úteis</Link></div></nav>
    </header>
    {children}
    <footer className="site-footer"><div className="page-shell footer-grid"><div className="footer-brand"><span className="brand-mark inverse" aria-hidden="true">D</span><div><strong>Prefeitura de Deodápolis</strong><p>Serviço público próximo, claro e acessível.</p></div></div><div><h2>Atendimento</h2><p>Consulte endereços e horários atualizados no diretório de contatos.</p><Link href="/contatos">Ver contatos</Link></div><div><h2>Acesso direto</h2>{footerMenu.length > 0 ? footerMenu.flatMap(flattenMenu).slice(0, 8).map((node) => <MenuLink key={node.resource.id} node={node} />) : fallbackFooter.map(([href, label]) => <Link key={href} href={href}>{label}</Link>)}</div></div><div className="page-shell footer-bottom"><span>© 2026 Prefeitura Municipal de Deodápolis</span><span>Portal preparado para dispositivos móveis.</span></div></footer>
    <nav className="public-mobile-dock" aria-label="Atalhos principais">
      <Link href="/"><Home size={19} aria-hidden="true" /><span>Início</span></Link>
      <Link href="/servicos"><LayoutGrid size={19} aria-hidden="true" /><span>Serviços</span></Link>
      <Link className="public-mobile-dock-search" href="/buscar"><span><Search size={21} aria-hidden="true" /></span><small>Buscar</small></Link>
      <Link href="/ouvidoria"><Headphones size={19} aria-hidden="true" /><span>Ouvidoria</span></Link>
    </nav>
  </div>;
}

function buildMenuTree(resources: PortalResource[], placement: string): MenuNode[] {
  const entries = resources.flatMap((resource) => {
    const payload = readPayload(resource.payload);
    const normalizedPlacement = (payload.placement || "HEADER").toUpperCase();
    if (payload.enabled === false || normalizedPlacement !== placement) return [];
    return [{ resource, payload, children: [] as MenuNode[] }];
  });
  const bySlug = new Map(entries.map((entry) => [entry.resource.slug, entry]));
  const roots: MenuNode[] = [];
  for (const entry of entries) {
    const parent = entry.payload.parent?.trim();
    const parentNode = parent ? bySlug.get(parent) : undefined;
    if (parentNode && parentNode !== entry && !wouldCreateCycle(entry, parentNode, bySlug)) parentNode.children.push(entry);
    else roots.push(entry);
  }
  const sort = (nodes: MenuNode[]) => nodes.sort((a, b) => a.resource.displayOrder - b.resource.displayOrder || menuLabel(a).localeCompare(menuLabel(b), "pt-BR"));
  for (const entry of entries) sort(entry.children);
  return sort(roots);
}

function wouldCreateCycle(node: MenuNode, parent: MenuNode, bySlug: Map<string, MenuNode>) {
  const seen = new Set([node.resource.slug]);
  let current: MenuNode | undefined = parent;
  while (current) {
    if (seen.has(current.resource.slug)) return true;
    seen.add(current.resource.slug);
    const parentSlug: string | undefined = current.payload.parent?.trim();
    current = parentSlug ? bySlug.get(parentSlug) : undefined;
  }
  return false;
}

function DesktopMenuNode({ node }: { node: MenuNode }) {
  if (node.children.length === 0) return <MenuLink node={node} />;
  return <details className="relative"><summary className="cursor-pointer list-none">{menuLabel(node)}</summary><div className="absolute left-0 z-40 mt-2 grid min-w-60 gap-1 rounded-lg border border-border bg-surface p-2 shadow-lg">{node.children.map((child) => <DesktopMenuNode key={child.resource.id} node={child} />)}</div></details>;
}
function DesktopMenuGroup({ label, nodes }: { label: string; nodes: MenuNode[] }) { return <details className="relative"><summary className="cursor-pointer list-none">{label}</summary><div className="absolute left-0 z-40 mt-2 grid min-w-60 gap-1 rounded-lg border border-border bg-surface p-2 shadow-lg">{nodes.map((node) => <DesktopMenuNode key={node.resource.id} node={node} />)}</div></details>; }
function MobileMenuNode({ node }: { node: MenuNode }) { return node.children.length === 0 ? <MenuLink node={node} /> : <details><summary>{menuLabel(node)}</summary><div className="ml-3 grid gap-1">{node.children.map((child) => <MobileMenuNode key={child.resource.id} node={child} />)}</div></details>; }
function MobileMenuGroup({ label, nodes }: { label: string; nodes: MenuNode[] }) { return <details><summary>{label}</summary><div className="ml-3 grid gap-1">{nodes.map((node) => <MobileMenuNode key={node.resource.id} node={node} />)}</div></details>; }
function MenuLink({ node }: { node: MenuNode }) {
  const label = menuLabel(node);
  const href = safeHref(node.payload.url);
  if (!href) return <span>{label}</span>;
  if (node.payload.external || href.startsWith("http://") || href.startsWith("https://")) return <a href={href} target="_blank" rel="noopener noreferrer">{label}</a>;
  return <Link href={href}>{label}</Link>;
}
function menuLabel(node: MenuNode) { return node.payload.label?.trim() || node.resource.title; }
function readPayload(payload: unknown): MenuPayload { return payload && typeof payload === "object" && !Array.isArray(payload) ? payload as MenuPayload : {}; }
function safeHref(value?: string) { if (!value) return null; const normalized = value.trim(); if (normalized.startsWith("/") && !normalized.startsWith("//")) return normalized; try { const url = new URL(normalized); return url.protocol === "https:" || url.protocol === "http:" ? url.toString() : null; } catch { return null; } }
function flattenMenu(node: MenuNode): MenuNode[] { return [node, ...node.children.flatMap(flattenMenu)]; }

export function PageIntro({ eyebrow, title, description, breadcrumb }: { eyebrow: string; title: string; description?: string; breadcrumb?: { label: string; href?: string }[] }) {
  return <section className="page-intro"><div className="page-shell">{breadcrumb && breadcrumb.length > 0 && <Breadcrumb items={breadcrumb} />}<p className="eyebrow">{eyebrow}</p><h1>{title}</h1>{description && <p>{description}</p>}</div></section>;
}

export function EmptyPanel({ title, description }: { title: string; description: string }) {
  return <div className="empty-state page-empty" role="status"><h2>{title}</h2><p>{description}</p></div>;
}
