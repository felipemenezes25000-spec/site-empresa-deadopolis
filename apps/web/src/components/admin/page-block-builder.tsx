"use client";

import { useState } from "react";

export const pageBlockTypes = [
  "Hero",
  "ServiceSearch",
  "QuickAccess",
  "FeaturedNews",
  "NewsGrid",
  "ServiceGrid",
  "DepartmentGrid",
  "Events",
  "Banner",
  "Alert",
  "Documents",
  "Statistics",
  "Contact",
  "Video",
  "Gallery",
  "CustomLinks",
] as const;

export type PageBlock = {
  id: string;
  type: string;
  title: string;
  content: string;
  reference: string;
  enabled: boolean;
};

export function PageBlockBuilder({ initialBlocks, disabled = false }: { initialBlocks: PageBlock[]; disabled?: boolean }) {
  const [blocks, setBlocks] = useState<PageBlock[]>(initialBlocks);
  const [newType, setNewType] = useState<(typeof pageBlockTypes)[number]>("Hero");

  function addBlock() {
    setBlocks((current) => [...current, {
      id: `block-${Date.now()}-${current.length}`,
      type: newType,
      title: "",
      content: "",
      reference: "",
      enabled: true,
    }]);
  }

  function updateBlock(id: string, patch: Partial<PageBlock>) {
    setBlocks((current) => current.map((block) => block.id === id ? { ...block, ...patch } : block));
  }

  function move(index: number, direction: -1 | 1) {
    setBlocks((current) => {
      const target = index + direction;
      if (target < 0 || target >= current.length) return current;
      const copy = [...current];
      [copy[index], copy[target]] = [copy[target], copy[index]];
      return copy;
    });
  }

  function remove(id: string) {
    setBlocks((current) => current.filter((block) => block.id !== id));
  }

  return <section className="grid gap-3" aria-labelledby="page-builder-title">
    <input type="hidden" name="payloadBlocksJson" value={JSON.stringify(blocks)} />
    <div>
      <h3 id="page-builder-title">Page Builder</h3>
      <small>Monte a página em blocos governados. A ordem abaixo é a ordem de renderização.</small>
    </div>
    {!disabled && <div className="flex flex-wrap gap-2">
      <label className="field min-w-56">Tipo do novo bloco<select value={newType} onChange={(event) => setNewType(event.target.value as (typeof pageBlockTypes)[number])}>{pageBlockTypes.map((type) => <option key={type} value={type}>{type}</option>)}</select></label>
      <button type="button" className="action-button self-end" onClick={addBlock}>Adicionar bloco</button>
    </div>}
    {blocks.length === 0 && <div className="empty-state"><h4>Nenhum bloco configurado</h4><p>O conteúdo textual tradicional continua válido; adicione blocos para uma composição visual mais rica.</p></div>}
    <div className="grid gap-3">{blocks.map((block, index) => <article key={block.id} className="rounded-xl border border-border bg-surface-soft p-4">
      <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
        <div><strong>Bloco {index + 1}</strong><small className="ml-2 text-muted">{block.type}</small></div>
        <div className="button-row">
          <button type="button" className="action-button secondary" disabled={disabled || index === 0} onClick={() => move(index, -1)} aria-label={`Mover bloco ${index + 1} para cima`}>↑</button>
          <button type="button" className="action-button secondary" disabled={disabled || index === blocks.length - 1} onClick={() => move(index, 1)} aria-label={`Mover bloco ${index + 1} para baixo`}>↓</button>
          <button type="button" className="action-button secondary" disabled={disabled} onClick={() => remove(block.id)}>Remover</button>
        </div>
      </div>
      <div className="editor-fields">
        <label className="field">Tipo<select value={block.type} disabled={disabled} onChange={(event) => updateBlock(block.id, { type: event.target.value })}>{pageBlockTypes.map((type) => <option key={type} value={type}>{type}</option>)}</select></label>
        <label className="field">Título<input value={block.title} disabled={disabled} onChange={(event) => updateBlock(block.id, { title: event.target.value })} /></label>
        <label className="field">Conteúdo<textarea rows={4} value={block.content} disabled={disabled} onChange={(event) => updateBlock(block.id, { content: event.target.value })} /></label>
        <label className="field">Referência / origem<input value={block.reference} disabled={disabled} onChange={(event) => updateBlock(block.id, { reference: event.target.value })} /><small>Opcional: slug, categoria, URL de vídeo ou identificador usado pelo bloco.</small></label>
        <label><input type="checkbox" checked={block.enabled} disabled={disabled} onChange={(event) => updateBlock(block.id, { enabled: event.target.checked })} /> Bloco habilitado</label>
      </div>
    </article>)}</div>
  </section>;
}
