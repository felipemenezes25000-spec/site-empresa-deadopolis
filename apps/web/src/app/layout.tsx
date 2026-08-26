import type { Metadata, Viewport } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: {
    default: "Prefeitura de Deodápolis",
    template: "%s | Prefeitura de Deodápolis",
  },
  description: "Serviços, notícias, transparência e atendimento da Prefeitura Municipal de Deodápolis, Mato Grosso do Sul.",
};

export const viewport: Viewport = {
  colorScheme: "light",
  themeColor: "#155f45",
};

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="pt-BR">
      <body>{children}</body>
    </html>
  );
}
