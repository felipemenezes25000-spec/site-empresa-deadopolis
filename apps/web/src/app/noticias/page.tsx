import type { Metadata } from "next";
import Link from "next/link";
import { EmptyPanel, PageIntro, PublicShell } from "@/components/portal/public-shell";
import { getNews } from "@/lib/portal-api";
export const metadata: Metadata={title:"Notícias"};
export default async function NewsPage(){const news=await getNews();return <PublicShell><PageIntro eyebrow="Comunicação" title="Notícias da Prefeitura" description="Informações publicadas pela comunicação municipal e suas secretarias."/><section className="content-section"><div className="page-shell">{news.length===0?<EmptyPanel title="Nenhuma notícia publicada" description="As publicações aprovadas aparecerão aqui."/>:<div className="news-list">{news.map(item=><Link className="news-row" href={`/noticias/${item.slug}`} key={item.slug}><time>{item.publishedAt?new Intl.DateTimeFormat("pt-BR").format(new Date(item.publishedAt)):"Publicação recente"}</time><div><h2>{item.title}</h2><p>{item.summary}</p></div><span aria-hidden="true">→</span></Link>)}</div>}</div></section></PublicShell>}
