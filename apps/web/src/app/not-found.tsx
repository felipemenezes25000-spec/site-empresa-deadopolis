import Link from "next/link";
import { PageIntro, PublicShell } from "@/components/portal/public-shell";

export default function NotFound() {
  return <PublicShell>
    <PageIntro eyebrow="404" title="Esta página não foi encontrada." description="O endereço pode ter mudado. A estratégia de migração preserva URLs antigas por redirect sempre que existe um destino validado." />
    <section className="content-section"><div className="page-shell detail-grid">
      <article className="prose-card">
        <h2>Como continuar</h2>
        <p>Use a busca municipal para encontrar o serviço, a notícia ou o documento pelo assunto, sem precisar saber qual secretaria é responsável.</p>
        <form className="content-search" role="search" action="/buscar" method="get">
          <label className="sr-only" htmlFor="not-found-search">Buscar no portal</label>
          <input id="not-found-search" name="q" type="search" placeholder="Ex.: IPTU, matrícula escolar, licitações" />
          <button className="action-button" type="submit">Buscar</button>
        </form>
        <Link className="action-button secondary" href="/">Voltar ao portal</Link>
      </article>
      <aside className="side-card">
        <h2>Atalhos</h2>
        <p><Link href="/servicos">Carta de Serviços</Link></p>
        <p><Link href="/noticias">Notícias</Link></p>
        <p><Link href="/transparencia">Transparência</Link></p>
        <p><Link href="/diario-oficial">Diário Oficial</Link></p>
        <p><Link href="/ouvidoria">Ouvidoria</Link></p>
      </aside>
    </div></section>
  </PublicShell>;
}
