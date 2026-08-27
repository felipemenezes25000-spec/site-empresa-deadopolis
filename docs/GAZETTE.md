# Diário Oficial eletrônico

O módulo de Diário Oficial trata edição, seções, atos, anexos, geração determinística, hash, código/QR de verificação, assinatura, publicação e correções vinculadas.

## Fluxo

1. criar edição em rascunho;
2. compor seções e atos normalizados;
3. enviar para revisão;
4. aprovar;
5. gerar documento e SHA-256;
6. assinar digitalmente pelo provider configurado;
7. aplicar timestamp quando disponível/obrigatório;
8. publicar e registrar a publicação;
9. expor PDF e verificação pública.

Edição publicada é imutável. Alteração posterior deve ser uma correção/retificação vinculada, preservando o original.

## Verificação

A página pública utiliza código de verificação, hash e metadados de assinatura. O hash deve representar exatamente o artefato disponibilizado. QR/código não substituem assinatura criptográfica; são mecanismos de localização e conferência.

## Dependência externa

O software suporta o ciclo e providers abstratos, mas ICP-Brasil e carimbo do tempo reais dependem de contratação/configuração. Consulte `ICP_BRASIL.md`. O sistema não deve marcar edição nova como validamente assinada em produção por meio do provider de demonstração.
