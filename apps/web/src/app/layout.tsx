import type { Metadata, Viewport } from "next";
import "./globals.css";
import "./platform.css";
import "./a11y-overrides.css";
import "./document-archive.css";
import "./premium.css";

export const metadata: Metadata = {
  metadataBase: new URL(process.env.PUBLIC_PORTAL_URL ?? "http://localhost:3000"),
  title: { default: "Prefeitura de Deodápolis", template: "%s | Prefeitura de Deodápolis" },
  description: "Serviços, notícias, transparência e atendimento da Prefeitura Municipal de Deodápolis, Mato Grosso do Sul.",
  openGraph: { type: "website", locale: "pt_BR", siteName: "Prefeitura de Deodápolis" },
};

export const viewport: Viewport = { colorScheme: "light", themeColor: "#082c20" };

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return <html lang="pt-BR"><body>{children}</body></html>;
}
