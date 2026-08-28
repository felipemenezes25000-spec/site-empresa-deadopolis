import Link from "next/link";
import { ArrowLeft, Search, Waypoints } from "lucide-react";
import { PageIntro, PublicShell } from "@/components/portal/public-shell";

export default function NotFound() {
  return <PublicShell>
    <div className="not-found-stage"><PageIntro eyebrow="Erro 404" title="Essa página tomou outro caminho." description="O endereço pode ter mudado. Quando existe um destino validado, a estratégia de migração preserva URLs antigas por redirecionamento." /></div>
    <section className="content-section"><div className="page-shell detail-grid">
      <article className="prose-card not-found-card">
        <p className="eyebrow dark">Encontre em segundos</p>
        <h2>O que você estava procurando?</h2>
        <p>Pesquise pelo assunto. Você não precisa saber qual secretaria é responsável para encontrar um serviço, notícia ou documento.</p>
        <form className="content-search" role="search" action="/buscar" method="get">
          <label className="sr-only" htmlFor="not-found-search">Buscar no portal</label>
          <Search size={19} aria-hidden="true" />
          <input id="not-found-search" name="q" type="search" placeholder="Ex.: IPTU, matrícula escolar, licitações" />
          <button className="action-button" type="submit">Buscar</button>
        </form>
        <Link className="action-button secondary" href="/"><ArrowLeft size={16} aria-hidden="true" /> Voltar ao início</Link>
      </article>
      <aside className="side-card">
        <Waypoints size={25} aria-hidden="true" />
        <h2>Atalhos úteis</h2>
        <div className="not-found-shortcuts">
          <Link href="/servicos">Carta de Serviços</Link>
          <Link href="/noticias">Notícias</Link>
          <Link href="/transparencia">Transparência</Link>
          <Link href="/diario-oficial">Diário Oficial</Link>
          <Link href="/ouvidoria">Ouvidoria</Link>
        </div>
      </aside>
    </div></section>
  </PublicShell>;
}
