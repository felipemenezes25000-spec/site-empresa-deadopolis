import type { PageBlockItem, PageBlockType } from "@/lib/page-blocks";

export type ApprovedImage = { id: string; originalFileName: string; mimeType: string; altText: string; status: string };

export function PageBlockItemEditor({ blockType, item, index, images, disabled, onChange, onRemove }: { blockType: PageBlockType; item: PageBlockItem; index: number; images: ApprovedImage[]; disabled: boolean; onChange: (patch: Partial<PageBlockItem>) => void; onRemove: () => void }) {
  const number = index + 1;
  const gallery = blockType === "Gallery";
  const statistics = blockType === "Statistics";
  const events = blockType === "Events";

  return <fieldset className="grid gap-3 rounded-lg border border-border bg-surface p-3"><legend className="px-1 font-semibold">Item {number}</legend>
    {gallery && <label className="field">Imagem aprovada {number}<select value={images.some((image) => `/api/v1/media/${image.id}` === item.mediaUrl) ? item.mediaUrl : ""} disabled={disabled} onChange={(event) => { const image = images.find((entry) => `/api/v1/media/${entry.id}` === event.target.value); if (image) onChange({ mediaUrl: event.target.value, mediaAlt: image.altText || image.originalFileName, label: item.label || image.originalFileName }); }}><option value="">Escolha na biblioteca</option>{images.map((image) => <option key={image.id} value={`/api/v1/media/${image.id}`}>{image.originalFileName} — {image.altText || "sem ALT"}</option>)}</select></label>}
    <label className="field">{statistics ? `Nome do indicador ${number}` : gallery ? `Legenda da imagem ${number}` : `Título do item ${number}`}<input value={item.label} maxLength={220} disabled={disabled} onChange={(event) => onChange({ label: event.target.value })} /></label>
    {statistics && <label className="field">Valor do indicador {number}<input value={item.value} maxLength={120} disabled={disabled} onChange={(event) => onChange({ value: event.target.value })} /></label>}
    {events && <label className="field">Data do evento {number}<input type="date" value={item.date} disabled={disabled} onChange={(event) => onChange({ date: event.target.value })} /></label>}
    {gallery && <><label className="field">URL da mídia interna {number}<input value={item.mediaUrl} pattern="/api/v1/media/.*" maxLength={2048} disabled={disabled} onChange={(event) => onChange({ mediaUrl: event.target.value })} /></label><label className="field">Texto alternativo da imagem {number}<input value={item.mediaAlt} maxLength={500} disabled={disabled} onChange={(event) => onChange({ mediaAlt: event.target.value })} /></label></>}
    {!statistics && !gallery && <label className="field">Destino do item {number}<input value={item.url} maxLength={2048} disabled={disabled} onChange={(event) => onChange({ url: event.target.value })} /></label>}
    <label className="field">Descrição do item {number}<textarea rows={2} value={item.description} maxLength={1000} disabled={disabled} onChange={(event) => onChange({ description: event.target.value })} /></label>
    <button type="button" className="action-button secondary justify-self-start" disabled={disabled} aria-label={`Remover item ${number}`} onClick={onRemove}>Remover item</button>
  </fieldset>;
}
