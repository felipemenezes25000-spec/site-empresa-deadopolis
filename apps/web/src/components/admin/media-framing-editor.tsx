"use client";

import { useState, type FormEvent, type MouseEvent } from "react";
import { ResponsiveMediaImage } from "@/components/portal/responsive-media-image";

export type FramingAsset = {
  id: string;
  altText: string;
  tagsCsv?: string;
  focalPointX?: number | null;
  focalPointY?: number | null;
  cropX?: number | null;
  cropY?: number | null;
  cropWidth?: number | null;
  cropHeight?: number | null;
};

export type FramingPayload = {
  tags: string;
  focalPointX: number;
  focalPointY: number;
  cropX: number | null;
  cropY: number | null;
  cropWidth: number | null;
  cropHeight: number | null;
};

const clamp = (value: number, min = 0, max = 100) => Math.min(max, Math.max(min, Math.round(value)));
const toPercent = (value: number | null | undefined, fallback: number) => typeof value === "number" ? clamp(value * 100) : fallback;

export function MediaFramingEditor({ asset, disabled, busy, onSave }: { asset: FramingAsset; disabled?: boolean; busy?: boolean; onSave: (payload: FramingPayload) => void | Promise<void> }) {
  const [tags, setTags] = useState(asset.tagsCsv ?? "");
  const [focalX, setFocalX] = useState(() => toPercent(asset.focalPointX, 50));
  const [focalY, setFocalY] = useState(() => toPercent(asset.focalPointY, 50));
  const [cropEnabled, setCropEnabled] = useState(() => [asset.cropX, asset.cropY, asset.cropWidth, asset.cropHeight].every((value) => typeof value === "number"));
  const [cropX, setCropX] = useState(() => toPercent(asset.cropX, 0));
  const [cropY, setCropY] = useState(() => toPercent(asset.cropY, 0));
  const [cropWidth, setCropWidth] = useState(() => toPercent(asset.cropWidth, 100));
  const [cropHeight, setCropHeight] = useState(() => toPercent(asset.cropHeight, 100));

  const boundedWidth = Math.min(cropWidth, 100 - cropX);
  const boundedHeight = Math.min(cropHeight, 100 - cropY);

  function pickFocalPoint(event: MouseEvent<HTMLButtonElement>) {
    const bounds = event.currentTarget.getBoundingClientRect();
    if (bounds.width === 0 || bounds.height === 0) return;
    setFocalX(clamp(((event.clientX - bounds.left) / bounds.width) * 100));
    setFocalY(clamp(((event.clientY - bounds.top) / bounds.height) * 100));
  }

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    void onSave({
      tags,
      focalPointX: focalX / 100,
      focalPointY: focalY / 100,
      cropX: cropEnabled ? cropX / 100 : null,
      cropY: cropEnabled ? cropY / 100 : null,
      cropWidth: cropEnabled ? boundedWidth / 100 : null,
      cropHeight: cropEnabled ? boundedHeight / 100 : null,
    });
  }

  return <form className="editor-fields" onSubmit={submit}>
    <h3>Enquadramento editorial</h3>
    <div className="media-framing">
      <button type="button" className="media-framing-stage" onClick={pickFocalPoint} disabled={disabled} aria-label="Definir ponto focal clicando na imagem">
        <ResponsiveMediaImage className="media-framing-image" src={`/api/v1/media/${asset.id}`} width={1200} height={800} sizes="(max-width: 900px) 100vw, 520px" alt={asset.altText || "Prévia da mídia aprovada"} />
        {cropEnabled && <span className="media-framing-crop" style={{ left: `${cropX}%`, top: `${cropY}%`, width: `${boundedWidth}%`, height: `${boundedHeight}%` }} aria-hidden="true" />}
        <span className="media-framing-focus" style={{ left: `${focalX}%`, top: `${focalY}%` }} aria-hidden="true" />
      </button>
      <p className="text-muted" role="status">Ponto focal em {focalX}% × {focalY}%{cropEnabled ? ` · recorte ${boundedWidth}% × ${boundedHeight}% a partir de ${cropX}% × ${cropY}%` : " · sem recorte editorial"}.</p>
    </div>

    <label className="field">Tags<input value={tags} onChange={(event) => setTags(event.target.value)} maxLength={2000} disabled={disabled} placeholder="saúde, obras, evento" /><small>Até 20 tags, separadas por vírgula.</small></label>
    <label className="field">Ponto focal horizontal · {focalX}%<input type="range" min="0" max="100" step="1" value={focalX} disabled={disabled} onChange={(event) => setFocalX(clamp(Number(event.target.value)))} /></label>
    <label className="field">Ponto focal vertical · {focalY}%<input type="range" min="0" max="100" step="1" value={focalY} disabled={disabled} onChange={(event) => setFocalY(clamp(Number(event.target.value)))} /></label>
    <label><input type="checkbox" checked={cropEnabled} disabled={disabled} onChange={(event) => setCropEnabled(event.target.checked)} /> Definir recorte editorial normalizado</label>
    {cropEnabled && <div className="editor-grid">
      <label className="field">X (%)<input type="number" min="0" max="99" step="1" value={cropX} disabled={disabled} onChange={(event) => setCropX(clamp(Number(event.target.value), 0, 99))} /></label>
      <label className="field">Y (%)<input type="number" min="0" max="99" step="1" value={cropY} disabled={disabled} onChange={(event) => setCropY(clamp(Number(event.target.value), 0, 99))} /></label>
      <label className="field">Largura (%)<input type="number" min="1" max="100" step="1" value={cropWidth} disabled={disabled} onChange={(event) => setCropWidth(clamp(Number(event.target.value), 1, 100))} /></label>
      <label className="field">Altura (%)<input type="number" min="1" max="100" step="1" value={cropHeight} disabled={disabled} onChange={(event) => setCropHeight(clamp(Number(event.target.value), 1, 100))} /></label>
    </div>}
    <small>O recorte é salvo como coordenadas 0..1 e nunca sobrescreve o arquivo original. Derivados WebP/AVIF só são declarados quando há encoder testado no runtime.</small>
    <button className="action-button" disabled={disabled || busy}>Salvar enquadramento</button>
  </form>;
}
