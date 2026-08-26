"use client";

import { useEffect } from "react";

export default function ErrorPage({ error, reset }: { error: Error & { digest?: string }; reset: () => void }) {
  useEffect(() => {
    console.error(error);
  }, [error]);

  return (
    <main className="error-page">
      <p className="eyebrow dark">Indisponibilidade temporária</p>
      <h1>Não foi possível carregar o portal</h1>
      <p>Os serviços estão demorando a responder. Aguarde alguns instantes e tente novamente.</p>
      <button type="button" onClick={reset}>Tentar novamente</button>
    </main>
  );
}
