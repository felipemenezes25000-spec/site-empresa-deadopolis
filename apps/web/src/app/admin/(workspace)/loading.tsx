export default function Loading() {
  return (
    <div className="admin-route-loading" role="status" aria-label="Carregando área administrativa">
      <div className="admin-loading-heading">
        <span className="skeleton-line skeleton-kicker" />
        <span className="skeleton-line skeleton-title" />
        <span className="skeleton-line skeleton-copy" />
      </div>
      <div className="admin-loading-grid" aria-hidden="true">
        {Array.from({ length: 4 }, (_, index) => <span className="skeleton-card" key={index} />)}
      </div>
      <div className="skeleton-panel" aria-hidden="true"><span className="skeleton-line skeleton-panel-title" /><span className="skeleton-line" /><span className="skeleton-line" /><span className="skeleton-line skeleton-short" /></div>
      <span className="sr-only">Carregando…</span>
    </div>
  );
}
