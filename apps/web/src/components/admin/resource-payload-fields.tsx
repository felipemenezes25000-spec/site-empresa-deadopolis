import { PageBlockBuilder, type PageBlock } from "./page-block-builder";

type ResourcePayloadFieldsProps = {
  kind: string;
  payloadJson: string;
  disabled?: boolean;
  menuOptions?: Array<{ value: string; label: string }>;
};

type Payload = Record<string, unknown>;

const specializedKinds = new Set([
  "PAGE",
  "BANNER",
  "MENU",
  "HOME_BLOCK",
  "ALERT",
  "CONTACT",
  "PROCUREMENT_LINK",
]);

export function ResourcePayloadFields({ kind, payloadJson, disabled = false, menuOptions = [] }: ResourcePayloadFieldsProps) {
  const payload = parsePayload(payloadJson);
  const normalizedKind = kind.toUpperCase();

  if (!specializedKinds.has(normalizedKind)) {
    return <label className="field">
      Detalhes estruturados (JSON)
      <textarea name="payloadJson" rows={8} defaultValue={formatPayload(payload)} disabled={disabled} />
      <small>Campo avançado para este tipo de conteúdo. O JSON é validado antes de ser salvo.</small>
    </label>;
  }

  const currentParent = text(payload.parent);
  return <>
    <input type="hidden" name="payloadBaseJson" value={formatPayload(payload)} />
    {normalizedKind === "PAGE" && <>
      <label className="field">
        Conteúdo da página
        <textarea aria-label="Conteúdo da página" name="payloadBody" rows={10} defaultValue={text(payload.conteudo) || text(payload.body)} disabled={disabled} />
        <small>Texto institucional principal exibido na página.</small>
      </label>
      <label className="field">
        Seções da página
        <textarea aria-label="Seções da página" name="payloadSections" rows={5} defaultValue={lines(payload.sections)} disabled={disabled} />
        <small>Informe uma seção por linha para organizar a navegação do conteúdo.</small>
      </label>
      <PageBlockBuilder initialBlocks={pageBlocks(payload.blocks)} disabled={disabled} />
    </>}
    {normalizedKind === "BANNER" && <>
      <label className="field">URL da imagem<input name="payloadImageUrl" type="text" defaultValue={text(payload.imageUrl)} disabled={disabled} /></label>
      <label className="field">Texto alternativo da imagem<input name="payloadImageAlt" defaultValue={text(payload.imageAlt)} disabled={disabled} /></label>
      <label className="field">Texto do botão<input name="payloadCtaLabel" defaultValue={text(payload.ctaLabel)} disabled={disabled} /></label>
      <label className="field">Destino do botão<input aria-label="Destino do botão" name="payloadUrl" type="text" defaultValue={text(payload.url)} disabled={disabled} /><small>Aceita uma rota interna ou uma URL externa completa.</small></label>
    </>}
    {normalizedKind === "MENU" && <>
      <label className="field">Rótulo do item<input name="payloadLabel" required defaultValue={text(payload.label)} disabled={disabled} /></label>
      <label className="field">Destino do item<input name="payloadUrl" required type="text" defaultValue={text(payload.url)} disabled={disabled} /></label>
      <label className="field">Item superior<select name="payloadParent" defaultValue={currentParent} disabled={disabled}><option value="">Raiz do menu</option>{currentParent && !menuOptions.some((option) => option.value === currentParent) && <option value={currentParent}>{currentParent} (legado)</option>}{menuOptions.map((option) => <option key={option.value} value={option.value}>{option.label} ({option.value})</option>)}</select><small>Escolha o item pai para montar a hierarquia sem digitar identificadores manualmente.</small></label>
      <label><input name="payloadExternal" type="checkbox" defaultChecked={boolean(payload.external)} disabled={disabled} /> Abrir como link externo</label>
      <label><input name="payloadEnabled" type="checkbox" defaultChecked={payload.enabled === undefined ? true : boolean(payload.enabled)} disabled={disabled} /> Item habilitado</label>
    </>}
    {normalizedKind === "HOME_BLOCK" && <>
      <label className="field">Conteúdo do bloco<textarea name="payloadContent" rows={7} defaultValue={text(payload.content)} disabled={disabled} /></label>
      <label className="field">Apresentação<select name="payloadVariant" defaultValue={text(payload.variant) || "FEATURE"} disabled={disabled}>
        <option value="FEATURE">Destaque</option>
        <option value="LIST">Lista</option>
        <option value="SHORTCUT">Atalho</option>
      </select></label>
    </>}
    {normalizedKind === "ALERT" && <>
      <label className="field">Mensagem do alerta<textarea name="payloadContent" rows={5} required defaultValue={text(payload.content)} disabled={disabled} /></label>
      <label className="field">Severidade<select name="payloadSeverity" defaultValue={text(payload.severity) || "INFO"} disabled={disabled}>
        <option value="INFO">Informativo</option>
        <option value="WARNING">Atenção</option>
        <option value="CRITICAL">Crítico</option>
      </select></label>
      <label className="field">Link complementar<input name="payloadUrl" type="text" defaultValue={text(payload.url)} disabled={disabled} /></label>
    </>}
    {normalizedKind === "CONTACT" && <>
      <label className="field">Telefone<input name="payloadPhone" type="tel" defaultValue={text(payload.phone)} disabled={disabled} /></label>
      <label className="field">E-mail<input name="payloadEmail" type="email" defaultValue={text(payload.email)} disabled={disabled} /></label>
      <label className="field">Endereço<textarea name="payloadAddress" rows={3} defaultValue={text(payload.address)} disabled={disabled} /></label>
      <label className="field">Horário de atendimento<textarea name="payloadOpeningHours" rows={3} defaultValue={text(payload.openingHours)} disabled={disabled} /></label>
    </>}
    {normalizedKind === "PROCUREMENT_LINK" && <>
      <label className="field">Destino oficial<input name="payloadUrl" required type="text" defaultValue={text(payload.url)} disabled={disabled} /><small>Informe somente a fonte oficial validada para licitações e contratos.</small></label>
      <label className="field">Situação da integração<select name="payloadExternalSystemState" defaultValue={text(payload.externalSystemState) || "NOT_CONFIGURED"} disabled={disabled}>
        <option value="NOT_CONFIGURED">Não configurada</option>
        <option value="CONFIGURED">Configurada</option>
        <option value="DEGRADED">Degradada</option>
      </select></label>
      <label className="field">Nome da fonte<input name="payloadSourceLabel" defaultValue={text(payload.sourceLabel)} disabled={disabled} /></label>
    </>}
  </>;
}

export function serializeResourcePayload(kind: string, form: FormData) {
  const normalizedKind = kind.toUpperCase();
  if (!specializedKinds.has(normalizedKind)) {
    return formatPayload(parsePayload(value(form, "payloadJson") || "{}", true));
  }

  const base = parsePayload(value(form, "payloadBaseJson") || "{}");
  const payload = normalizedKind === "PAGE" ? {
    ...without(without(base, "body"), "blocks"),
    conteudo: value(form, "payloadBody"),
    sections: value(form, "payloadSections").split(/\r?\n/).map(item => item.trim()).filter(Boolean),
    blocks: parsePageBlocks(value(form, "payloadBlocksJson")),
  } : normalizedKind === "BANNER" ? {
    ...base,
    imageUrl: value(form, "payloadImageUrl"),
    imageAlt: value(form, "payloadImageAlt"),
    ctaLabel: value(form, "payloadCtaLabel"),
    url: value(form, "payloadUrl"),
  } : normalizedKind === "MENU" ? {
    ...base,
    label: value(form, "payloadLabel"),
    url: value(form, "payloadUrl"),
    parent: value(form, "payloadParent"),
    external: form.has("payloadExternal"),
    enabled: form.has("payloadEnabled"),
  } : normalizedKind === "HOME_BLOCK" ? {
    ...base,
    content: value(form, "payloadContent"),
    variant: value(form, "payloadVariant"),
  } : normalizedKind === "ALERT" ? {
    ...base,
    content: value(form, "payloadContent"),
    severity: value(form, "payloadSeverity"),
    url: value(form, "payloadUrl"),
  } : normalizedKind === "CONTACT" ? {
    ...base,
    phone: value(form, "payloadPhone"),
    email: value(form, "payloadEmail"),
    address: value(form, "payloadAddress"),
    openingHours: value(form, "payloadOpeningHours"),
  } : {
    ...base,
    url: value(form, "payloadUrl"),
    externalSystemState: value(form, "payloadExternalSystemState"),
    sourceLabel: value(form, "payloadSourceLabel"),
  };

  return JSON.stringify(payload);
}

function parsePayload(payloadJson: string, strict = false): Payload {
  try {
    const parsed = JSON.parse(payloadJson) as unknown;
    if (parsed && typeof parsed === "object" && !Array.isArray(parsed)) return parsed as Payload;
  } catch (error) {
    if (strict) throw error;
  }
  if (strict) throw new Error("O payload precisa ser um objeto JSON.");
  return {};
}

function parsePageBlocks(raw: string): PageBlock[] {
  if (!raw) return [];
  const parsed = JSON.parse(raw) as unknown;
  if (!Array.isArray(parsed)) throw new Error("A composição da página precisa ser uma lista de blocos.");
  return pageBlocks(parsed);
}

function pageBlocks(value: unknown): PageBlock[] {
  if (!Array.isArray(value)) return [];
  return value.flatMap((entry, index) => {
    if (!entry || typeof entry !== "object" || Array.isArray(entry)) return [];
    const candidate = entry as Record<string, unknown>;
    const type = text(candidate.type);
    if (!type) return [];
    return [{
      id: text(candidate.id) || `block-${index + 1}`,
      type,
      title: text(candidate.title),
      content: text(candidate.content),
      reference: text(candidate.reference),
      enabled: candidate.enabled === undefined ? true : boolean(candidate.enabled),
    }];
  });
}

function formatPayload(payload: Payload) {
  return JSON.stringify(payload, null, 2);
}

function value(form: FormData, name: string) {
  const entry = form.get(name);
  return typeof entry === "string" ? entry.trim() : "";
}

function text(value: unknown) {
  return typeof value === "string" ? value : "";
}

function boolean(value: unknown) {
  return value === true;
}

function lines(value: unknown) {
  return Array.isArray(value) ? value.filter(item => typeof item === "string").join("\n") : "";
}

function without(payload: Payload, key: string) {
  const copy = { ...payload };
  delete copy[key];
  return copy;
}
