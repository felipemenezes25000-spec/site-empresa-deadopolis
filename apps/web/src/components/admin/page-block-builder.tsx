"use client";

import { useEffect, useState } from "react";
import { pageBlockTypes, type PageBlock, type PageBlockItem, type PageBlockType } from "@/lib/page-blocks";
import { PageBlockRenderer } from "../portal/page-block-renderer";
import { PageBlockItemEditor, type ApprovedImage } from "./page-block-item-editor";

export { pageBlockTypes } from "@/lib/page-blocks";
export type { PageBlock } from "@/lib/page-blocks";

const itemBlockTypes = new Set<PageBlockType>(["QuickAccess", "FeaturedNews", "NewsGrid", "ServiceGrid", "DepartmentGrid", "Events", "Documents", "Statistics", "Gallery", "CustomLinks"]);

export function PageBlockBuilder({ initialBlocks, disabled = false }: { initialBlocks: PageBlock[]; disabled?: boolean }) {
  const [blocks, setBlocks] = useState<PageBlock[]>(initialBlocks);
  const [newType, setNewType] = useState<PageBlockType>("Hero");
  const [images, setImages] = useState<ApprovedImage[]>([]);
  const [mediaState, setMediaState] = useState<"LOADING" | "READY" | "ERROR">("LOADING");

  useEffect(() => {
    const controller = new AbortController();
    void fetch("/api/v1/admin/media?status=APPROVED&pageSize=100", { signal: controller.signal })
      .then(async (response) => {
        if (!response.ok) throw new Error("media");
        const assets = await response.json() as ApprovedImage[];
        if (!controller.signal.aborted) {
          setImages(assets.filter((asset) => asset.status === "APPROVED" && asset.mimeType.startsWith("image/")));
          setMediaState("READY");
        }
      })
      .catch(() => { if (!controller.signal.aborted) setMediaState("ERROR"); });
    return () => controller.abort();
  }, []);

  function addBlock() {
    if (blocks.length >= 30) return;
    setBlocks((current) => [...current, emptyBlock(newType, current.length)]);
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

  function addItem(block: PageBlock) {
    if (block.items.length >= 24) return;
    updateBlock(block.id, { items: [...block.items, emptyItem(block.items.length)] });
  }

  function updateItem(block: PageBlock, itemId: string, patch: Partial<PageBlockItem>) {
    updateBlock(block.id, { items: block.items.map((item) => item.id === itemId ? { ...item, ...patch } : item) });
  }

  function removeItem(block: PageBlock, itemId: string) {
    updateBlock(block.id, { items: block.items.filter((item) => item.id !== itemId) });
  }

  return <section className="grid gap-3" aria-labelledby="page-builder-title">
    <input type="hidden" name="payloadBlocksJson" value={JSON.stringify(blocks)} />
    <div><h3 id="page-builder-title">Page Builder</h3><small>Monte a página em blocos governados. A ordem abaixo é a ordem de renderização.</small></div>
    {!disabled && <div className="flex flex-wrap gap-2"><label className="field min-w-56">Tipo do novo bloco<select value={newType} onChange={(event) => setNewType(event.target.value as PageBlockType)}>{pageBlockTypes.map((type) => <option key={type} value={type}>{type}</option>)}</select></label><button type="button" className="action-button self-end" disabled={blocks.length >= 30} onClick={addBlock}>Adicionar bloco</button></div>}
    {blocks.length === 0 && <div className="empty-state"><h4>Nenhum bloco configurado</h4><p>O conteúdo textual tradicional continua válido; adicione blocos para uma composição visual mais rica.</p></div>}
    <div className="grid gap-3">{blocks.map((block, index) => <article key={block.id} className="rounded-xl border border-border bg-surface-soft p-4">
      <div className="mb-3 flex flex-wrap items-center justify-between gap-2"><div><strong>Bloco {index + 1}</strong><small className="ml-2 text-muted">{block.type}</small></div><div className="button-row"><button type="button" className="action-button secondary" disabled={disabled || index === 0} onClick={() => move(index, -1)} aria-label={`Mover bloco ${index + 1} para cima`}>↑</button><button type="button" className="action-button secondary" disabled={disabled || index === blocks.length - 1} onClick={() => move(index, 1)} aria-label={`Mover bloco ${index + 1} para baixo`}>↓</button><button type="button" className="action-button secondary" disabled={disabled} onClick={() => setBlocks((current) => current.filter((item) => item.id !== block.id))}>Remover</button></div></div>
      <div className="editor-fields">
        <label className="field">Tipo<select value={block.type} disabled={disabled} onChange={(event) => updateBlock(block.id, { type: event.target.value as PageBlockType })}>{pageBlockTypes.map((type) => <option key={type} value={type}>{type}</option>)}</select></label>
        <label className="field">Título<input value={block.title} maxLength={220} disabled={disabled} onChange={(event) => updateBlock(block.id, { title: event.target.value })} /></label>
        <label className="field">Conteúdo<textarea rows={4} value={block.content} maxLength={4000} disabled={disabled} onChange={(event) => updateBlock(block.id, { content: event.target.value })} /></label>
        {block.type === "Banner" && <BannerFields block={block} images={images} mediaState={mediaState} disabled={disabled} updateBlock={updateBlock} />}
        {!itemBlockTypes.has(block.type) && block.type !== "ServiceSearch" && block.type !== "Banner" && <ReferenceFields block={block} disabled={disabled} updateBlock={updateBlock} />}
        {itemBlockTypes.has(block.type) && <section className="grid gap-3" aria-label={`Itens do bloco ${index + 1}`}><div className="flex flex-wrap items-center justify-between gap-2"><strong>{itemSectionTitle(block.type)}</strong><button type="button" className="action-button secondary" disabled={disabled || block.items.length >= 24} onClick={() => addItem(block)}>{addItemLabel(block.type)}</button></div>{block.items.map((item, itemIndex) => <PageBlockItemEditor key={item.id} blockType={block.type} item={item} index={itemIndex} images={images} disabled={disabled} onChange={(patch) => updateItem(block, item.id, patch)} onRemove={() => removeItem(block, item.id)} />)}{block.items.length === 0 && <small className="text-muted">Adicione itens estruturados; não é necessário editar JSON.</small>}</section>}
        <label><input type="checkbox" checked={block.enabled} disabled={disabled} onChange={(event) => updateBlock(block.id, { enabled: event.target.checked })} /> Bloco habilitado</label>
      </div>
      <details className="mt-4 rounded-lg border border-border bg-surface"><summary className="cursor-pointer p-3 font-semibold">Pré-visualização do bloco</summary><PageBlockRenderer payload={{ blocks: [block] }} /></details>
    </article>)}</div>
  </section>;
}

function BannerFields({ block, images, mediaState, disabled, updateBlock }: { block: PageBlock; images: ApprovedImage[]; mediaState: "LOADING" | "READY" | "ERROR"; disabled: boolean; updateBlock: (id: string, patch: Partial<PageBlock>) => void }) {
  return <><label className="field">Selecionar imagem da biblioteca<select value={images.some((image) => `/api/v1/media/${image.id}` === block.imageUrl) ? block.imageUrl : ""} disabled={disabled || mediaState === "LOADING"} onChange={(event) => { const image = images.find((item) => `/api/v1/media/${item.id}` === event.target.value); if (image) updateBlock(block.id, { imageUrl: event.target.value, imageAlt: image.altText || image.originalFileName }); }}><option value="">Escolha uma imagem aprovada</option>{images.map((image) => <option key={image.id} value={`/api/v1/media/${image.id}`}>{image.originalFileName} — {image.altText || "sem ALT"}</option>)}</select>{mediaState === "ERROR" && <small>Biblioteca indisponível; informe uma rota interna validada.</small>}</label><label className="field">URL da imagem interna<input aria-label="URL da imagem interna" value={block.imageUrl} pattern="/api/v1/media/.*" maxLength={2048} disabled={disabled} onChange={(event) => updateBlock(block.id, { imageUrl: event.target.value })} /><small>Somente mídia aprovada em /api/v1/media/.</small></label><label className="field">Texto alternativo da imagem<input value={block.imageAlt} maxLength={500} disabled={disabled} onChange={(event) => updateBlock(block.id, { imageAlt: event.target.value })} /></label><ReferenceFields block={block} disabled={disabled} updateBlock={updateBlock} /></>;
}

function ReferenceFields({ block, disabled, updateBlock }: { block: PageBlock; disabled: boolean; updateBlock: (id: string, patch: Partial<PageBlock>) => void }) {
  return <><label className="field">{block.type === "Video" ? "URL do vídeo" : "Destino do botão"}<input value={block.reference} maxLength={2048} disabled={disabled} onChange={(event) => updateBlock(block.id, { reference: event.target.value })} /><small>Use rota interna ou URL HTTP(S); protocolos perigosos são rejeitados.</small></label><label className="field">Texto do botão<input value={block.linkLabel} maxLength={120} disabled={disabled} onChange={(event) => updateBlock(block.id, { linkLabel: event.target.value })} /></label></>;
}

function emptyBlock(type: PageBlockType, index: number): PageBlock {
  return { id: `block-${Date.now()}-${index}`, type, title: "", content: "", reference: "", imageUrl: "", imageAlt: "", linkLabel: "", items: [], enabled: true };
}

function emptyItem(index: number): PageBlockItem {
  return { id: `item-${Date.now()}-${index}`, label: "", description: "", value: "", url: "", date: "", mediaUrl: "", mediaAlt: "" };
}

function itemSectionTitle(type: PageBlockType) {
  return type === "Statistics" ? "Indicadores" : type === "Gallery" ? "Imagens" : type === "Events" ? "Eventos" : type === "Documents" ? "Documentos" : "Itens";
}

function addItemLabel(type: PageBlockType) {
  return type === "Statistics" ? "Adicionar indicador" : type === "Gallery" ? "Adicionar imagem" : type === "Events" ? "Adicionar evento" : type === "Documents" ? "Adicionar documento" : "Adicionar item";
}
