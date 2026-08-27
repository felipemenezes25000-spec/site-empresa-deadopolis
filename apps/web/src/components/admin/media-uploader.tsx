"use client";

import { useState, type DragEvent, type FormEvent } from "react";

const MAX_BATCH_FILES = 20;

export function MediaUploader({ onUploaded }: { onUploaded: (preferredId?: string) => void | Promise<void> }) {
  const [files, setFiles] = useState<File[]>([]);
  const [busy, setBusy] = useState(false);
  const [dragging, setDragging] = useState(false);
  const [message, setMessage] = useState("");

  function queue(next: File[]) {
    setFiles(next.slice(0, MAX_BATCH_FILES));
    setMessage(next.length > MAX_BATCH_FILES ? `O lote foi limitado aos primeiros ${MAX_BATCH_FILES} arquivos.` : "");
  }

  function drop(event: DragEvent<HTMLDivElement>) {
    event.preventDefault();
    setDragging(false);
    queue(Array.from(event.dataTransfer.files));
  }

  async function upload(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (files.length === 0) {
      setMessage("Selecione ao menos um arquivo.");
      return;
    }
    setBusy(true);
    const formElement = event.currentTarget;
    const form = new FormData(formElement);
    const metadata = ["altText", "caption", "credit"] as const;
    let uploaded = 0;
    let lastId: string | undefined;
    const errors: string[] = [];

    for (const file of files) {
      const data = new FormData();
      data.set("file", file);
      for (const field of metadata) data.set(field, String(form.get(field) ?? ""));
      try {
        const response = await fetch("/api/v1/admin/media/upload", { method: "POST", body: data });
        const body = await response.json().catch(() => null) as { id?: string; detail?: string; title?: string } | null;
        if (response.ok) {
          uploaded++;
          lastId = body?.id ?? lastId;
        } else {
          errors.push(`${file.name}: ${body?.detail ?? body?.title ?? `erro ${response.status}`}`);
        }
      } catch {
        errors.push(`${file.name}: falha de conexão`);
      }
    }

    if (uploaded > 0) {
      formElement.reset();
      setFiles([]);
      await onUploaded(lastId);
    }
    const success = uploaded === 1 ? "1 arquivo recebido" : `${uploaded} arquivos recebidos`;
    setMessage(errors.length > 0 ? `${success}. ${errors.join(" ")}` : `${success} para validação e scanner.`);
    setBusy(false);
  }

  return <form className="admin-panel editor-fields" onSubmit={upload}>
    <h2>Enviar mídia</h2>
    <div className={`media-drop-zone${dragging ? " is-dragging" : ""}`} onDragEnter={(event) => { event.preventDefault(); setDragging(true); }} onDragOver={(event) => event.preventDefault()} onDragLeave={() => setDragging(false)} onDrop={drop}>
      <label className="field">Arquivos<input name="files" type="file" accept="image/jpeg,image/png,image/webp,application/pdf" multiple onChange={(event) => queue(Array.from(event.target.files ?? []))} /></label>
      <small>Selecione ou arraste até {MAX_BATCH_FILES} arquivos. Cada arquivo mantém validação, hash, quarentena e auditoria independentes.</small>
    </div>
    {files.length > 0 && <ul className="media-upload-queue" aria-label="Arquivos selecionados">{files.map((file) => <li key={`${file.name}-${file.size}`}>{file.name}<small>{formatBytes(file.size)}</small></li>)}</ul>}
    <label className="field">Texto alternativo comum<input name="altText" maxLength={500} /></label>
    <label className="field">Legenda comum<input name="caption" maxLength={1000} /></label>
    <label className="field">Crédito comum<input name="credit" maxLength={500} /></label>
    <button className="action-button" disabled={busy || files.length === 0}>{busy ? "Processando lote…" : files.length === 1 ? "Enviar 1 arquivo" : `Enviar ${files.length} arquivos`}</button>
    {message && <div className="form-message" role="status">{message}</div>}
    <small>O backend valida bytes reais, tamanho, SHA-256 e mantém quarentena quando o scanner de produção não está configurado. A revisão nunca ignora o scanner.</small>
  </form>;
}

function formatBytes(value: number) {
  return value < 1024 ? `${value} B` : `${Math.ceil(value / 1024)} KB`;
}
