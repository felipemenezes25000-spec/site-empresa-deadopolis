import { CircleHelp, Headphones, Menu } from "lucide-react";
import Image from "next/image";
import Link from "next/link";
import type { ReactNode } from "react";

const navigation = [
  ["/servicos", "Serviços"], ["/noticias", "Notícias"], ["/secretarias", "Secretarias"], ["/transparencia", "Transparência"], ["/diario-oficial", "Diário Oficial"]
] as const;

export function PublicShell({ children }: { children: ReactNode }) {
  return <div className="public-page">
    <a className="skip-link" href="#conteudo-principal">Ir para o conteúdo principal</a>
    <header className="site-header">
      <div className="utility-bar"><div className="page-shell utility-inner"><p>Deodápolis · Mato Grosso do Sul</p><div className="utility-links"><Link href="/acessibilidade">Acessibilidade</Link><Link href="/contatos">Contatos</Link><Link href="/admin/login">Área administrativa</Link></div></div></div>
      <div className="page-shell brand-row"><Link className="municipal-brand" href="/" aria-label="Prefeitura de Deodápolis — início"><Image src="/brand/deodapolis-logo.png" width={278} height={74} priority alt="Prefeitura de Deodápolis — Juntos por um futuro ainda melhor" /></Link><div className="header-actions"><Link className="header-shortcut" href="/acesso-a-informacao"><CircleHelp size={18} aria-hidden="true" /> Acesso à informação</Link><Link className="header-shortcut" href="/ouvidoria"><Headphones size={18} aria-hidden="true" /> Ouvidoria</Link></div></div>
      <nav className="main-nav" aria-label="Navegação principal"><div className="page-shell nav-inner"><details className="mobile-navigation"><summary><Menu size={22} aria-hidden="true" /> Menu</summary><div>{navigation.map(([href,label]) => <Link key={href} href={href}>{label}</Link>)}</div></details><div className="desktop-nav-links">{navigation.map(([href,label]) => <Link key={href} href={href}>{label}</Link>)}</div><Link className="emergency-link" href="/contatos#emergencia">Telefones úteis</Link></div></nav>
    </header>
    <main id="conteudo-principal">{children}</main>
    <footer className="site-footer"><div className="page-shell footer-grid"><div className="footer-brand"><span className="brand-mark inverse" aria-hidden="true">D</span><div><strong>Prefeitura de Deodápolis</strong><p>Serviço público próximo, claro e acessível.</p></div></div><div><h2>Atendimento</h2><p>Consulte endereços e horários atualizados no diretório de contatos.</p><Link href="/contatos">Ver contatos</Link></div><div><h2>Acesso direto</h2><Link href="/servicos">Carta de Serviços</Link><Link href="/dados-abertos">Dados Abertos</Link><Link href="/acesso-a-informacao">e-SIC</Link><Link href="/privacidade">Privacidade e LGPD</Link></div></div><div className="page-shell footer-bottom"><span>© 2026 Prefeitura Municipal de Deodápolis</span><span>Portal preparado para dispositivos móveis.</span></div></footer>
  </div>;
}

export function PageIntro({ eyebrow, title, description }: { eyebrow: string; title: string; description?: string }) {
  return <section className="page-intro"><div className="page-shell"><p className="eyebrow">{eyebrow}</p><h1>{title}</h1>{description && <p>{description}</p>}</div></section>;
}

export function EmptyPanel({ title, description }: { title: string; description: string }) {
  return <div className="empty-state page-empty" role="status"><h2>{title}</h2><p>{description}</p></div>;
}
