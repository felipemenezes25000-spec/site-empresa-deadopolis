import type { ReactNode } from "react";

export function RichText({ value }: { value: string }) {
  const lines = value.replace(/\r\n/g, "\n").split("\n");
  const blocks: ReactNode[] = [];
  let list: string[] = [];
  function flushList() {
    if (list.length === 0) return;
    blocks.push(<ul key={`list-${blocks.length}`}>{list.map((item, index) => <li key={`${item}-${index}`}>{inline(item)}</li>)}</ul>);
    list = [];
  }
  lines.forEach((line, index) => {
    if (line.startsWith("- ")) { list.push(line.slice(2)); return; }
    flushList();
    if (!line.trim()) { blocks.push(<br key={`break-${index}`} />); return; }
    if (line.startsWith("## ")) blocks.push(<h2 key={`heading-${index}`}>{inline(line.slice(3))}</h2>);
    else blocks.push(<p key={`paragraph-${index}`}>{inline(line)}</p>);
  });
  flushList();
  return <div className="rich-text">{blocks}</div>;
}

function inline(text: string): ReactNode[] {
  const pattern = /(\[[^\]]+\]\([^)]+\)|\*\*[^*]+\*\*|_[^_]+_)/g;
  const parts = text.split(pattern).filter(Boolean);
  return parts.map((part, index) => {
    if (part.startsWith("**") && part.endsWith("**")) return <strong key={index}>{part.slice(2, -2)}</strong>;
    if (part.startsWith("_") && part.endsWith("_")) return <em key={index}>{part.slice(1, -1)}</em>;
    const link = /^\[([^\]]+)\]\(([^)]+)\)$/.exec(part);
    if (link) {
      const href = safeHref(link[2]);
      return href ? <a key={index} href={href} target={href.startsWith("http") ? "_blank" : undefined} rel={href.startsWith("http") ? "noopener noreferrer" : undefined}>{link[1]}</a> : <span key={index}>{link[1]}</span>;
    }
    return <span key={index}>{part}</span>;
  });
}

function safeHref(value: string) {
  const normalized = value.trim();
  if (normalized.startsWith("/") && !normalized.startsWith("//")) return normalized;
  try { const url = new URL(normalized); return url.protocol === "https:" || url.protocol === "http:" ? url.toString() : null; } catch { return null; }
}
