# Plano de implementação reconciliado

O plano inicial foi executado em ondas. Este arquivo registra o resultado real em vez de manter checkboxes históricos que já não representavam o código. O detalhe verificável vive em `REQUIREMENTS_MATRIX.md`.

| Onda | Resultado | Evidência principal |
|---|---|---|
| Auditoria e arquitetura | DONE | `CURRENT_PORTAL_AUDIT.md`, `ARCHITECTURE.md` |
| Fundação, tenancy e identidade | DONE | solução .NET/Next, Compose e testes de tenant/IAM |
| CMS, portal e mídia | DONE / EXTERNAL_DEPENDENCY | workflow e UI prontos; storage/scanner externos |
| Serviços, secretarias e busca | DONE | catálogo, diretórios e busca global database-side |
| Transparência e Dados Abertos | DONE | datasets, hubs e acervo documental |
| Diário Oficial | DONE / EXTERNAL_DEPENDENCY | fluxo verificável; ICP/timestamp externos |
| Ouvidoria e e-mail | DONE / EXTERNAL_DEPENDENCY | tickets/SLA prontos; provider de e-mail externo |
| Migração e continuidade | DONE / EXTERNAL_DEPENDENCY | crawler/inventário/importação; publicação e cutover institucionais |
| Operações, segurança e compliance | DONE / EXTERNAL_DEPENDENCY | health/scans/link check; infraestrutura operacional externa |
| POC, E2E e acessibilidade | DONE | reset protegido, 22 E2E, axe e crawl interno |
| Documentação e entrega | DONE | README, runbooks, matriz, relatório e contrato documental |

## Política de conclusão

Nenhum item controlável permanece como “parcial”. O que depende de certificado, credencial, provider, DNS, dado oficial, decisão institucional ou infraestrutura está classificado como `EXTERNAL_DEPENDENCY`, com contrato, estado seguro e critério de aceite documentados.

Cada alteração futura deve manter backend/frontend/E2E/migrations/containers verdes, atualizar a matriz quando mudar uma fronteira e nunca converter `NOT_CONFIGURED` ou `DEMO_ONLY` em sucesso de produção.
