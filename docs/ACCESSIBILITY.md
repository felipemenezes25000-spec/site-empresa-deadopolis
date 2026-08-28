# Acessibilidade

Meta de engenharia: WCAG 2.2 nível AA para fluxos públicos e administrativos relevantes.

## Gates automatizados

A CI executa Playwright/axe e bloqueia violações de severidade séria/crítica em 14 rotas públicas e em todo o workspace administrativo autenticado (`/admin`, conteúdo, comunicação, mídia, usuários, tickets, compliance, dados abertos e Diário). O frontend também passa por lint, typecheck, testes e build de produção.

Além do axe, os cenários automatizados exigem:

- skip link alcançável na primeira tabulação, com foco visível e destino real;
- exatamente um `main#conteudo-principal`, um `banner`, um `contentinfo` e um `h1` por página pública;
- hierarquia de títulos sem nível pulado;
- ausência de rolagem horizontal em 375, 768, 1024 e 1440 pixels, no portal e na administração;
- menu móvel que abre a navegação municipal e fecha por teclado;
- estado de compliance comunicado por texto literal, não apenas por cor.

## Critérios manuais

Antes do go-live devem ser validados por pessoas: navegação somente por teclado; ordem de foco; zoom e reflow; leitor de tela em fluxos prioritários; contraste em conteúdo editorial real; textos alternativos; mensagens de erro; formulários; PDFs/documentos publicados; linguagem e compreensão.

## Conteúdo editorial

Imagem informativa deve possuir texto alternativo. Arquivo/documento deve ter título e contexto. O CMS não deve permitir que uma aprovação humana de acessibilidade seja substituída apenas por teste automatizado.

## Evidência

Automação fornece evidência repetível sobre o código versionado. O aceite humano com tecnologia assistiva é uma etapa separada e permanece necessário para produção.
