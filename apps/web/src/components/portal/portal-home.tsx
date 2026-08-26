import { ArrowRight, BookOpenText, Building2, CalendarDays, ChevronRight, CircleHelp, FileCheck2, Headphones, Landmark, MapPin, Menu, Search, ShieldCheck } from "lucide-react";
import Image from "next/image";
import Link from "next/link";
import { Button } from "@/components/ui/button";
import type { PortalHomeContent } from "@/lib/portal-api";

const primaryNavigation = [
  { href: "/servicos", label: "Serviços" },
  { href: "/noticias", label: "Notícias" },
  { href: "/secretarias", label: "Secretarias" },
  { href: "/transparencia", label: "Transparência" },
  { href: "/diario-oficial", label: "Diário Oficial" },
];

const quickNeeds = ["IPTU", "Nota fiscal", "Matrícula", "Licitações", "Ouvidoria"];

export function PortalHome({ content, presentationMode = false }: { content: PortalHomeContent; presentationMode?: boolean }) {
  const featured = content.latestNews.find((item) => item.isFeatured) ?? content.latestNews[0];
  const recent = content.latestNews.filter((item) => item.slug !== featured?.slug).slice(0, 3);

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

      <main id="conteudo-principal">
        <section className="service-hero" aria-labelledby="hero-title"><div className="hero-pattern" aria-hidden="true" /><div className="page-shell hero-content"><p className="eyebrow">Serviços municipais em um só lugar</p><h1 id="hero-title">Olá! O que você precisa?</h1><p className="hero-lead">Encontre serviços, documentos e canais de atendimento sem precisar saber qual secretaria procurar.</p><form className="universal-search" role="search" action="/buscar" method="get"><label className="sr-only" htmlFor="portal-search">Buscar serviço</label><Search aria-hidden="true" size={23} /><input id="portal-search" name="q" type="search" placeholder="Ex.: segunda via do IPTU, vaga na escola, poda de árvore" autoComplete="off" /><Button type="submit" size="large">Buscar</Button></form><div className="quick-needs" aria-label="Buscas sugeridas"><span>Mais buscados:</span>{quickNeeds.map((need) => <Link key={need} href={`/buscar?q=${encodeURIComponent(need)}`}>{need}</Link>)}</div></div></section>

        <section className="page-shell services-section" aria-labelledby="services-title">
          <div className="section-heading"><div><p className="eyebrow dark">Resolva por aqui</p><h2 id="services-title">Serviços mais procurados</h2></div><Link className="section-link" href="/servicos">Ver todos os serviços <ArrowRight size={18} aria-hidden="true" /></Link></div>
          {content.featuredServices.length > 0 ? <div className="service-list">{content.featuredServices.map((service, index) => <Link className="service-row" href={`/servicos/${service.slug}`} key={service.slug}><span className="service-number" aria-hidden="true">{String(index + 1).padStart(2, "0")}</span><span className="service-copy"><span className="service-meta">{service.area} {service.isOnline && <em>Online</em>}</span><strong>{service.name}</strong><small>{service.description}</small></span><ChevronRight size={21} aria-hidden="true" /></Link>)}</div> : <div className="empty-state" role="status"><Building2 aria-hidden="true" /><h3>Nenhum serviço em destaque</h3><p>O catálogo continua disponível para consulta.</p><Link href="/servicos">Abrir catálogo de serviços</Link></div>}
        </section>

        <section className="news-section" aria-labelledby="news-title"><div className="page-shell"><div className="section-heading"><div><p className="eyebrow dark">Acontece em Deodápolis</p><h2 id="news-title">Notícias da Prefeitura</h2></div><Link className="section-link" href="/noticias">Todas as notícias <ArrowRight size={18} aria-hidden="true" /></Link></div>{featured ? <div className="editorial-grid"><article className="lead-story"><div className="story-visual" aria-hidden="true"><span>DEO</span><small>Informação municipal</small></div><div className="story-copy"><p className="story-kicker">Destaque</p><h3><Link href={`/noticias/${featured.slug}`}>{featured.title}</Link></h3><p>{featured.summary}</p><Link className="read-more" href={`/noticias/${featured.slug}`}>Ler notícia <ArrowRight size={17} aria-hidden="true" /></Link></div></article><div className="recent-stories">{recent.map((article) => <article key={article.slug}><time dateTime={article.publishedAt ?? undefined}>{formatDate(article.publishedAt)}</time><h3><Link href={`/noticias/${article.slug}`}>{article.title}</Link></h3><p>{article.summary}</p></article>)}{recent.length === 0 && <p className="muted-note">Novas publicações aparecerão aqui.</p>}</div></div> : <div className="empty-state" role="status"><BookOpenText aria-hidden="true" /><h3>Nenhuma notícia publicada</h3><p>Assim que houver uma publicação aprovada, ela aparecerá nesta área.</p></div>}</div></section>

        <section className="participation-section" aria-labelledby="participation-title"><div className="page-shell participation-grid"><div className="participation-intro"><p className="eyebrow">Governo aberto</p><h2 id="participation-title">Transparência e participação</h2><p>Acompanhe recursos públicos, atos oficiais e fale com a Prefeitura pelos canais adequados.</p><div className="participation-seal"><ShieldCheck aria-hidden="true" /><span>Informação pública<br /><strong>organizada e acessível</strong></span></div></div><div className="public-links">{content.transparencyLinks.map((link) => <a key={link.title} href={link.url} target="_blank" rel="noreferrer"><Landmark aria-hidden="true" /><span><strong>{link.title}</strong><small>{link.description || link.category}</small></span><ArrowRight aria-hidden="true" /></a>)}<Link href="/diario-oficial"><FileCheck2 aria-hidden="true" /><span><strong>Diário Oficial</strong><small>Consulte edições e verifique documentos</small></span><ArrowRight aria-hidden="true" /></Link><Link href="/ouvidoria"><Headphones aria-hidden="true" /><span><strong>Ouvidoria</strong><small>Reclamação, solicitação, denúncia e elogio</small></span><ArrowRight aria-hidden="true" /></Link></div></div></section>

        <section className="page-shell useful-section" aria-labelledby="useful-title"><div className="section-heading"><div><p className="eyebrow dark">No dia a dia</p><h2 id="useful-title">Encontre a Prefeitura</h2></div></div><div className="useful-grid"><Link href="/locais"><MapPin aria-hidden="true" /><span><strong>Unidades e endereços</strong><small>UBS, escolas, CRAS e órgãos municipais</small></span></Link><Link href="/agenda"><CalendarDays aria-hidden="true" /><span><strong>Agenda municipal</strong><small>Eventos, campanhas e atividades públicas</small></span></Link><Link href="/secretarias"><Building2 aria-hidden="true" /><span><strong>Secretarias</strong><small>Gestores, contatos e serviços de cada área</small></span></Link></div></section>
      </main>

      <footer className="site-footer"><div className="page-shell footer-grid"><div className="footer-brand"><span className="brand-mark inverse" aria-hidden="true">D</span><div><strong>Prefeitura de Deodápolis</strong><p>Serviço público próximo, claro e acessível.</p></div></div><div><h2>Atendimento</h2><p>Av. Francisco Alves da Silva, 443<br />Centro · Deodápolis/MS</p><p>Segunda a sexta<br />7h–11h e 13h–17h</p></div><div><h2>Acesso direto</h2><Link href="/servicos">Carta de Serviços</Link><Link href="/dados-abertos">Dados Abertos</Link><Link href="/acesso-a-informacao">e-SIC</Link><Link href="/privacidade">Privacidade e LGPD</Link></div></div><div className="page-shell footer-bottom"><span>© 2026 Prefeitura Municipal de Deodápolis</span><span>Portal preparado para conexão lenta e dispositivos móveis.</span></div></footer>
    </div>
  );
}

function formatDate(value: string | null) {
  if (!value) return "Publicação recente";
  return new Intl.DateTimeFormat("pt-BR", { day: "2-digit", month: "short", year: "numeric", timeZone: "UTC" }).format(new Date(value));
}
