import type { Metadata } from "next";
import Link from "next/link";
import { ArrowLeft, BadgeCheck, Layers3, ShieldCheck, Sparkles } from "lucide-react";
import { LoginForm } from "@/components/admin/login-form";

export const metadata: Metadata = { title: "Área administrativa", robots: { index: false, follow: false } };

export default function Page() {
  return (
    <main className="login-shell login-experience">
      <section className="login-showcase" aria-label="Plataforma municipal">
        <div className="login-showcase-glow" aria-hidden="true" />
        <div className="login-showcase-grid" aria-hidden="true" />
        <div className="login-showcase-content">
          <Link className="login-back-link" href="/"><ArrowLeft size={16} aria-hidden="true" /> Portal público</Link>
          <div className="login-brand-lockup">
            <span className="login-brand-symbol" aria-hidden="true">D</span>
            <span><strong>Deodápolis Digital</strong><small>Plataforma Municipal</small></span>
          </div>
          <div className="login-showcase-copy">
            <div className="login-overline"><Sparkles size={15} aria-hidden="true" /> Gestão pública, em uma experiência única</div>
            <h1>Um portal inteiro.<br />Uma única operação.</h1>
            <p>Conteúdo, serviços ao cidadão, transparência, Diário Oficial, atendimento e integrações organizados em um workspace moderno e auditável.</p>
          </div>
          <div className="login-capabilities" aria-label="Recursos da plataforma">
            <div><Layers3 aria-hidden="true" /><span><strong>Operação integrada</strong><small>Todos os módulos no mesmo workspace</small></span></div>
            <div><ShieldCheck aria-hidden="true" /><span><strong>Governança por padrão</strong><small>RBAC, auditoria e fluxos de aprovação</small></span></div>
            <div><BadgeCheck aria-hidden="true" /><span><strong>Pronto para demonstração</strong><small>Experiência reproduzível e segura para POC</small></span></div>
          </div>
        </div>
      </section>

      <section className="login-access" aria-label="Acesso administrativo">
        <div className="login-access-inner">
          <div className="login-mobile-brand"><span className="login-brand-symbol" aria-hidden="true">D</span><strong>Deodápolis Digital</strong></div>
          <p className="eyebrow dark">Administração municipal</p>
          <h2>Bem-vindo de volta.</h2>
          <p className="login-access-lead">Entre para gerenciar o portal e acompanhar o que precisa de atenção.</p>
          <div className="demo-note"><strong>Ambiente de demonstração.</strong> Use a conta fornecida para a POC. Nenhuma senha é armazenada no repositório.</div>
          <LoginForm />
          <div className="login-trust-line"><ShieldCheck size={15} aria-hidden="true" /> Sessão protegida e ações administrativas auditadas.</div>
          <Link className="login-mobile-back" href="/"><ArrowLeft size={15} aria-hidden="true" /> Voltar ao portal</Link>
        </div>
      </section>
    </main>
  );
}
