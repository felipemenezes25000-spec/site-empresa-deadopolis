import { CircleHelp, Headphones, Menu } from "lucide-react";
import Image from "next/image";
import Link from "next/link";
import type { PortalHomeContent } from "@/lib/portal-api";
import { HomeComposition } from "./home-composition";

const primaryNavigation = [
  { href: "/servicos", label: "Serviços" },
  { href: "/noticias", label: "Notícias" },
  { href: "/secretarias", label: "Secretarias" },
  { href: "/transparencia", label: "Transparência" },
  { href: "/diario-oficial", label: "Diário Oficial" },
];

export function PortalHome({
  content,
  presentationMode = false,
  homeLayout,
}: {
  content: PortalHomeContent;
  presentationMode?: boolean;
  homeLayout?: unknown;
}) {
  return (
    <div className="min-h-screen bg-background text-foreground">
      <a className="skip-link" href="#conteudo-principal">Ir para o conteúdo principal</a>
      {presentationMode && <div className="demo-bar" role="status"><span className="demo-dot" aria-hidden="true" />Ambiente de demonstração</div>}

      <header className="site-header">
        <div className="utility-bar"><div className="page-shell utility-inner"><p>Deodápolis · Mato Grosso do Sul</p><div className="utility-links"><Link href="/acessibilidade">Acessibilidade</Link><Link href="/contatos">Contatos</Link><Link href="/admin/login">Área administrativa</Link></div></div></div>
        <div className="page-shell brand-row">
          <Link className="municipal-brand" href="/" aria-label="Prefeitura de Deodápolis — início"><Image src="/brand/deodapolis-logo.png" width={278} height={74} priority alt="Prefeitura de Deodápolis — Juntos por um futuro ainda melhor" /></Link>
          <div className="header-actions"><Link className="header-shortcut" href="/acesso-a-informacao"><CircleHelp size={18} aria-hidden="true" /> Acesso à informação</Link><Link className="header-shortcut" href="/ouvidoria"><Headphones size={18} aria-hidden="true" /> Ouvidoria</Link></div>
        </div>
        <nav className="main-nav" aria-label="Navegação principal"><div className="page-shell nav-inner"><details className="mobile-navigation"><summary><Menu size={22} aria-hidden="true" /> Menu</summary><div>{primaryNavigation.map((item) => <Link key={item.href} href={item.href}>{item.label}</Link>)}</div></details><div className="desktop-nav-links">{primaryNavigation.map((item) => <Link key={item.href} href={item.href}>{item.label}</Link>)}</div><Link className="emergency-link" href="/contatos#emergencia">Telefones úteis</Link></div></nav>
      </header>

      <HomeComposition content={content} payload={homeLayout} />

      <footer className="site-footer"><div className="page-shell footer-grid"><div className="footer-brand"><span className="brand-mark inverse" aria-hidden="true">D</span><div><strong>Prefeitura de Deodápolis</strong><p>Serviço público próximo, claro e acessível.</p></div></div><div><h2>Atendimento</h2><p>Av. Francisco Alves da Silva, 443<br />Centro · Deodápolis/MS</p><p>Segunda a sexta<br />7h–11h e 13h–17h</p></div><div><h2>Acesso direto</h2><Link href="/servicos">Carta de Serviços</Link><Link href="/dados-abertos">Dados Abertos</Link><Link href="/acesso-a-informacao">e-SIC</Link><Link href="/privacidade">Privacidade e LGPD</Link></div></div><div className="page-shell footer-bottom"><span>© 2026 Prefeitura Municipal de Deodápolis</span><span>Portal preparado para conexão lenta e dispositivos móveis.</span></div></footer>
    </div>
  );
}
