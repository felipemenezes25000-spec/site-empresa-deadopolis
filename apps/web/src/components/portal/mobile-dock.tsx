"use client";

import { Headphones, Home, LayoutGrid, Search } from "lucide-react";
import Link from "next/link";
import { usePathname } from "next/navigation";

/**
 * Barra fixa de atalhos no celular. Precisa ser componente de cliente para saber a rota atual:
 * uma navegação persistente que nunca indica onde a pessoa está deixa de ser navegação e vira
 * uma fileira de botões. O estado é marcado com aria-current, não só com cor.
 */
export function MobileDock() {
  // usePathname devolve null quando não há router montado; sem o padrão o dock quebraria
  // a renderização inteira do rodapé em vez de apenas não marcar item algum.
  const pathname = usePathname() ?? "";
  const isCurrent = (href: string) => (href === "/" ? pathname === "/" : pathname === href || pathname.startsWith(`${href}/`));
  const searchCurrent = pathname === "/buscar" || pathname.startsWith("/buscar/");

  return <nav className="public-mobile-dock" aria-label="Atalhos principais">
    <Link href="/" aria-current={isCurrent("/") ? "page" : undefined} className={isCurrent("/") ? "is-current" : undefined}>
      <Home size={19} aria-hidden="true" /><span>Início</span>
    </Link>
    <Link href="/servicos" aria-current={isCurrent("/servicos") ? "page" : undefined} className={isCurrent("/servicos") ? "is-current" : undefined}>
      <LayoutGrid size={19} aria-hidden="true" /><span>Serviços</span>
    </Link>
    <Link className={`public-mobile-dock-search${searchCurrent ? " is-current" : ""}`} href="/buscar" aria-current={searchCurrent ? "page" : undefined}>
      <span><Search size={21} aria-hidden="true" /></span><small>Buscar</small>
    </Link>
    <Link href="/ouvidoria" aria-current={isCurrent("/ouvidoria") ? "page" : undefined} className={isCurrent("/ouvidoria") ? "is-current" : undefined}>
      <Headphones size={19} aria-hidden="true" /><span>Ouvidoria</span>
    </Link>
  </nav>;
}
