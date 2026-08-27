# Relatório do inventário legado reconciliado

**Fonte pública:** `https://www.deodapolis.ms.gov.br/`

**Data da coleta:** 26 de agosto de 2026

**Modo:** dry-run somente leitura, profundidade 6, teto de 20.000 URLs, schema de evidência 5

**Janela final:** 27/08/2026 02:08:59–02:48:32 UTC (26/08/2026 23:08:59–23:48:32 em America/Sao_Paulo)

## Resultado executivo

O inventário automatizado percorreu o host autorizado até esvaziar a fila. O resultado não foi truncado por limite. A execução persistiu uma decisão explícita para cada URL interna descoberta: migrar, redirecionar ou ignorar com motivo. A coleta é evidência de inventário, não autorização para publicar ou alterar o portal atual.

A evidência compacta e legível por máquina está em `docs/evidence/legacy-inventory-final-2026-08-26.json`.

| Métrica | Resultado |
|---|---:|
| URLs internas únicas | 14.373 |
| Respostas HTML classificadas | 2.556 |
| Documentos | 8.155 |
| PDFs | 7.356 |
| Arquivos Office | 799 |
| Imagens | 3.372 |
| Redirects HTTP | 4 |
| Referências externas observadas | 85.610 |
| URLs externas únicas | 381 |
| Falhas terminais com motivo | 16 |
| Duplicatas por SHA-256 | 445 |
| Itens restantes na fila | 0 |
| Truncado pelo limite | Não |

Os 16 itens terminais são arquivos que excedem o limite seguro de 25 MB do pipeline documental. Eles permanecem como `IGNORE_WITH_REASON`, com a URL e a causa registradas; não foram silenciosamente descartados. O limite só deve ser alterado por decisão operacional explícita, com storage, malware scanner e política de upload compatíveis.

## Decisões e respostas

O schema 5 transforma falhas terminais em decisão de governança explícita. A distribuição esperada e confirmada pelo relatório persistido é:

| Decisão | Quantidade | Interpretação |
|---|---:|---|
| `MIGRATE` | 14.264 | Conteúdo interno apto a seguir para classificação/importação controlada |
| `IGNORE_WITH_REASON` | 105 | 87 respostas 404, 2 formatos de pacote não aceitos e 16 falhas por limite seguro |
| `REDIRECT` | 4 | Respostas 301 registradas sem seguir destino automaticamente |
| `UNCLASSIFIED` | 0 | Nenhuma URL ficou sem decisão |

Distribuição HTTP principal: 14.266 respostas `200`, 87 respostas `404` e 4 respostas `301`. As 16 falhas de tamanho são registradas antes de uma resposta integral e, por isso, não entram na distribuição de status. Os tipos predominantes foram `text/html` (2.647 respostas, incluindo páginas 404/redirect), `application/pdf` (7.356), Office moderno (787), Word legado (12), JPEG (3.286), PNG (86), JavaScript (103) e CSS (78). Um RAR e um ZIP foram inventariados e recusados pela política documental atual.

## Reconciliação independente dos índices

Uma segunda coleta consultou diretamente 90 páginas potenciais de cada família de licitação e deduplicou apenas links de arquivos em `/uploads/`. Essa fonte independente evita concluir cobertura apenas porque o crawler terminou.

| Família | Referência histórica aproximada | Índice independente | Grafo no crawl final | Última página com documentos | Delta final |
|---|---:|---:|---:|---:|---:|
| Avisos | 1.117 | 1.116 | 1.117 | 56 | 0 |
| Editais | 1.752 | 1.750 | 1.752 | 71 | 0 |
| Resultados | 1.002 | 997 | 998 | 51 | -4 |
| Contratos | 1.470 | 1.469 | 1.469 | 74 | -1 |
| **Licitações — total** | **5.341** | **5.332** | **5.336** | — | **-5** |
| Prestação de contas | 615 | 610 | 610 | — | -5 |
| Informativos (`exibe23.php`) | 1.422 | 1.404 | 1.404 | — | -18 |

As referências históricas eram estimativas de um snapshot anterior; o portal continua mutável. Entre a reconciliação independente e o fim do crawl apareceram quatro referências de licitação (um aviso, dois editais e um resultado), enquanto o inventário global aumentou em três URLs/PDFs únicos. Esse delta observado durante a própria janela, a fila vazia e a ausência de truncamento indicam variação da origem, e não perda silenciosa do crawler.

Famílias de paginação capturadas incluem 57 páginas de avisos, 72 de editais, 52 de resultados, 75 de contratos, 49 de prestação de contas, 296 listagens de notícias, 1.404 páginas `exibe23.php` e 84 páginas de legislação. Entre as famílias de arquivos estão 1.394 itens em `uploads/noticias`, 610 em `uploads/prestacoes_contas`, 1.117 em `uploads/avisos`, 1.752 em `uploads/editais`, 998 em `uploads/extratos` e 1.469 em `uploads/contrato`.

## Fronteiras e bloqueios reais

- `robots.txt` e `sitemap.xml` retornaram 404; a descoberta partiu das páginas públicas e de suas famílias de paginação.
- O allowlist exige correspondência exata do host. A variante sem `www` e os demais provedores foram registrados como externos, não seguidos automaticamente.
- As 381 URLs externas incluem sistemas oficiais e terceiros. Elas devem ser tratadas como integrações monitoradas ou links de saída, não copiadas cegamente.
- As 87 respostas 404 representam dívida de continuidade no próprio legado e permanecem evidenciadas.
- RAR, ZIP e os 16 arquivos acima de 25 MB exigem política/manual de exceção antes de qualquer ingestão.

## Inventariado não significa publicado

Esta execução usou banco em memória e não importou conteúdo para produção. O repositório possui pipeline de `PublicDocument` que refaz o fetch com proteção SSRF, exige o mesmo SHA-256 do dry-run, valida magic bytes/MIME, passa por malware scanner, grava em object storage e cria apenas um rascunho. A publicação exige aprovação do asset e ação administrativa explícita.

Portanto, a etapa de inventário está concluída, mas o cutover ainda depende de:

1. configurar storage, malware scanner, banco e segredos do ambiente final;
2. importar e revisar editorialmente os itens autorizados;
3. decidir exceções de tamanho/formato;
4. publicar destinos válidos antes de ativar redirects item a item;
5. repetir o inventário imediatamente antes do corte e reconciliar o delta.

## Evidência reproduzível

O crawler, a política de travessia, a proteção de rede, a normalização e a importação documental têm testes automatizados no projeto backend. O endpoint administrativo de inventário oferece paginação, filtros e relatório CSV para que todas as 14 mil URLs sejam revisáveis/exportáveis sem corte silencioso; células são neutralizadas contra execução de fórmulas em planilhas. A CI executa build, testes, migrations, auditoria de dependências, containers e E2E.

O JSON integral da execução deve ser anexado ao `MigrationEvidence` do job de produção. Este relatório conserva as métricas reconciliadas e as decisões; a lista item a item pode ser obtida no CSV administrativo. A assinatura/arquivamento desse CSV no pacote final deve ocorrer durante o cutover, junto com a evidência persistida do job de produção.
