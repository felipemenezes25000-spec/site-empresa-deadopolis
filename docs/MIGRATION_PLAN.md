# Plano de migração do portal legado

Este documento define a passagem controlada do portal legado para a plataforma municipal. O inventário e as evidências já produzidas estão em `LEGACY_MIGRATION_REPORT.md`; este plano não transforma inventário em conteúdo automaticamente aprovado.

## Princípios

- preservar URLs públicas sempre que houver destino equivalente;
- usar `301` somente depois de o destino novo estar publicado e validado;
- manter SHA-256, URL de origem, classificação e evidência de cada item importado;
- bloquear SSRF, redes privadas, esquemas inseguros, arquivos acima dos limites e conteúdo não reconhecido;
- separar `dry-run`, importação, aprovação editorial e cutover;
- nunca ocultar falha de importação ou substituir um documento por conteúdo sintético.

## Fases

### 1. Inventário

Executar crawler somente leitura, normalizar URLs, classificar HTML/documentos/imagens/redirects e persistir evidências. O relatório final versionado deve ser comparado com uma nova execução imediatamente antes do cutover.

### 2. Dry-run e mapeamento

Gerar a relação `LegacyUrl -> classificação -> destino proposto`. Itens sem destino devem permanecer explicitamente pendentes. O mapa humano de URLs fica em `URL_MIGRATION_MAP.md`.

### 3. Importação

Importar páginas e documentos com deduplicação por hash. Documentos destinados ao acervo público continuam sujeitos a storage e malware scanner reais antes do go-live. Conteúdo editorial entra como rascunho quando exigir revisão humana.

### 4. Validação

Validar amostras e lotes com: conteúdo, título, data, anexos, checksum, links, deep link, resposta 404/301, mobile, acessibilidade e autorização administrativa. Nenhum erro é convertido em sucesso silencioso.

### 5. Cutover

Pré-condições: destino oficial publicado, DNS/TLS definidos, backup válido, rollback ensaiado, storage/scanner configurados, responsáveis designados e CI verde no SHA implantado. Só então ativar redirects 301 e atualizar DNS/reverse proxy.

### 6. Pós-cutover

Monitorar 404, redirects, links externos, tickets, logs, métricas e uso de rotas antigas. Manter rollback e snapshot pelo período definido pela Prefeitura.

## Critério de encerramento

A migração é encerrada somente quando a fila estiver reconciliada, exceções tiverem motivo e responsável, redirects críticos estiverem validados no domínio oficial e a Prefeitura tiver aceitado a amostra/relatório de migração. Dependências reais de storage, scanner e domínio permanecem `EXTERNAL_DEPENDENCY` até configuração e aceite.
