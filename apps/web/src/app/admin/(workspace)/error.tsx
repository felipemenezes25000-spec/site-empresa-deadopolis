"use client";

import { AlertTriangle, RefreshCw } from "lucide-react";
import { useEffect } from "react";

export default function AdminError({ error, reset }: { error: Error & { digest?: string }; reset: () => void }) {
  useEffect(() => { console.error(error); }, [error]);

  return (
    <section className="admin-route-error" role="alert">
      <span className="admin-error-icon" aria-hidden="true"><AlertTriangle /></span>
      <p className="eyebrow dark">Falha temporária</p>
      <h1>Esta área não carregou como deveria.</h1>
      <p>O restante da plataforma continua disponível. Tente carregar esta área novamente; se o problema persistir, a ocorrência poderá ser investigada pelos registros operacionais.</p>
      <button type="button" className="action-button" onClick={reset}><RefreshCw size={17} aria-hidden="true" /> Tentar novamente</button>
    </section>
  );
}
