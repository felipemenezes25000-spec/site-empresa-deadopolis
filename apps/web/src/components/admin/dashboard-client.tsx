"use client";

import { AlertTriangle, ArrowUpRight, FilePenLine, Files, Headphones, ImageIcon, Layers3, Plus, Send, Wrench, type LucideIcon } from "lucide-react";
import Link from "next/link";
import { useEffect, useState } from "react";
import { StatusBadge } from "@/components/ui";

type Data = {
  editorial: { drafts: number; review: number; scheduled: number };
  support: { open: number; breached: number };
  content: { resources: number; services: number; mediaQuarantined: number };
  integrations: Array<{ provider: string; state: string; message: string }>;
};

const quickActions = [
  { href: "/admin/noticias/nova", label: "Nova notícia", detail: "Criar publicação", icon: Plus },
  { href: "/admin/conteudo", label: "Páginas", detail: "Editar portal", icon: Files },
  { href: "/admin/midia", label: "Mídia", detail: "Arquivos e imagens", icon: ImageIcon },
  { href: "/admin/tickets", label: "Atendimento", detail: "Tickets e SLA", icon: Headphones },
] as const;

export function DashboardClient() {
  const [data, setData] = useState<Data | null>(null);
  const [error, setError] = useState("");

  useEffect(() => {
    fetch("/api/v1/admin/dashboard")
      .then(async (response) => {
        if (!response.ok) throw new Error(`Dashboard retornou ${response.status}`);
        setData(await response.json() as Data);
      })
      .catch((reason) => setError(reason instanceof Error ? reason.message : "Falha"));
  }, []);

  if (error) return <div className="form-message error">{error}</div>;
  if (!data) return <div className="admin-panel dashboard-loading" aria-busy="true"><span className="dashboard-loading-dot" />Carregando visão operacional…</div>;

  return <div className="dashboard-stack">
    <nav className="dashboard-launchpad" aria-label="Ações rápidas">
      {quickActions.map(({ href, label, detail, icon: Icon }) => <Link href={href} key={href}><span className="dashboard-launch-icon"><Icon size={18} aria-hidden="true" /></span><span><strong>{label}</strong><small>{detail}</small></span><ArrowUpRight className="dashboard-launch-arrow" size={16} aria-hidden="true" /></Link>)}
    </nav>

    <section className="dashboard-bento" aria-label="Indicadores operacionais">
      <div className="dashboard-zone dashboard-zone-editorial">
        <div className="dashboard-zone-heading"><span>Publicação</span><strong>Fluxo editorial</strong></div>
        <div className="dashboard-zone-metrics">
          <Metric label="Rascunhos" value={data.editorial.drafts} icon={FilePenLine} />
          <Metric label="Em revisão" value={data.editorial.review} icon={Layers3} />
          <Metric label="Agendadas" value={data.editorial.scheduled} icon={Send} />
        </div>
      </div>
      <Metric label="Tickets abertos" value={data.support.open} icon={Headphones} featured />
      <Metric label="SLA violado" value={data.support.breached} icon={AlertTriangle} alert={data.support.breached > 0} />
      <Metric label="Recursos CMS" value={data.content.resources} icon={Layers3} />
      <Metric label="Serviços" value={data.content.services} icon={Wrench} />
      <Metric label="Mídia em quarentena" value={data.content.mediaQuarantined} icon={ImageIcon} alert={data.content.mediaQuarantined > 0} />
    </section>

    <section className="admin-panel dashboard-integrations">
      <div className="dashboard-panel-heading"><div><p>Infraestrutura conectada</p><h2>Saúde das integrações</h2></div><span>{data.integrations.length} providers</span></div>
      <div className="compact-list">{data.integrations.map((item) => <div className="compact-item integration-row" key={item.provider}><div><strong>{item.provider}</strong><small>{item.message}</small></div><StatusBadge status={item.state} /></div>)}</div>
    </section>
  </div>;
}

function Metric({ label, value, icon: Icon, featured = false, alert = false }: { label: string; value: number; icon: LucideIcon; featured?: boolean; alert?: boolean }) {
  return <article className={`metric-card dashboard-metric${featured ? " is-featured" : ""}${alert ? " is-alert" : ""}`}>
    <div className="dashboard-metric-top"><span>{label}</span><i aria-hidden="true"><Icon size={17} strokeWidth={1.9} /></i></div>
    <strong>{value}</strong>
  </article>;
}
