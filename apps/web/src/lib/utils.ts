import { type ClassValue, clsx } from "clsx";
import { twMerge } from "tailwind-merge";

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

/**
 * Serializa um payload JSON-LD para dentro de <script>. O escape de "<" é obrigatório: uma
 * sequência "</script>" vinda de um título de notícia fecharia o bloco e o restante do texto
 * passaria a ser interpretado como marcação.
 */
export function sanitizeJsonLd(data: unknown) {
  return JSON.stringify(data).replace(/</g, "\u003c");
}
