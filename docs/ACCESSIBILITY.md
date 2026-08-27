# Acessibilidade

Meta de engenharia: WCAG 2.2 nível AA para fluxos públicos e administrativos relevantes.

## Gates automatizados

A CI executa Playwright/axe nos cenários definidos e bloqueia violações configuradas de severidade séria/crítica. O frontend também passa por lint, typecheck, testes e build de produção. Há skip link, landmarks, labels, estados de foco e componentes com semântica ARIA onde aplicável.

## Critérios manuais

Antes do go-live devem ser validados por pessoas: navegação somente por teclado; ordem de foco; zoom e reflow; leitor de tela em fluxos prioritários; contraste em conteúdo editorial real; textos alternativos; mensagens de erro; formulários; PDFs/documentos publicados; linguagem e compreensão.

## Conteúdo editorial

Imagem informativa deve possuir texto alternativo. Arquivo/documento deve ter título e contexto. O CMS não deve permitir que uma aprovação humana de acessibilidade seja substituída apenas por teste automatizado.

## Evidência

Automação fornece evidência repetível sobre o código versionado. O aceite humano com tecnologia assistiva é uma etapa separada e permanece necessário para produção.
