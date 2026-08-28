import type { Metadata, Viewport } from "next";
import { Inter, Newsreader } from "next/font/google";
import "./globals.css";
import "./platform.css";
import "./document-archive.css";
import "./premium.css";
import "./public-experience.css";
import "./admin-premium.css";
import "./dashboard-premium.css";
import "./login-premium.css";
import "./experience-states.css";
import "./command-palette-premium.css";
import "./portal-search.css";
import "./typography.css";
// Carregada por último de propósito: é a camada que decide contraste e foco, e só consegue
// decidir se vier depois das folhas premium que reintroduzem cores decorativas mais claras.
import "./a11y-overrides.css";

// next/font baixa e auto-hospeda no build: nenhuma requisição a servidor de fontes em execução,
// que era a exigência de não depender de fonte remota frágil. Até aqui o CSS pedia "Inter" e
// "Iowan Old Style" sem carregar nenhuma das duas, então o portal caía em Segoe UI e Palatino.
const sans = Inter({ subsets: ["latin"], display: "swap", variable: "--font-sans-portal" });
const display = Newsreader({ subsets: ["latin"], display: "swap", weight: ["500", "600"], style: ["normal"], variable: "--font-display-portal" });

export const metadata: Metadata = {
  metadataBase: new URL(process.env.PUBLIC_PORTAL_URL ?? "http://localhost:3000"),
  title: { default: "Prefeitura de Deodápolis", template: "%s | Prefeitura de Deodápolis" },
  description: "Serviços, notícias, transparência e atendimento da Prefeitura Municipal de Deodápolis, Mato Grosso do Sul.",
  openGraph: { type: "website", locale: "pt_BR", siteName: "Prefeitura de Deodápolis" },
};

export const viewport: Viewport = { colorScheme: "light", themeColor: "#082c20" };

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return <html lang="pt-BR" className={`${sans.variable} ${display.variable}`}><body>{children}</body></html>;
}
