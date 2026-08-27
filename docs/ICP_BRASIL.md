# ICP-Brasil e carimbo do tempo

O Diário Oficial possui abstrações separadas para certificado/assinatura, validação e timestamp. O modo de demonstração não equivale a assinatura ICP-Brasil válida.

## Requisitos de produção

- certificado ICP-Brasil válido e cadeia verificável;
- chave privada protegida, preferencialmente em serviço/HSM ou mecanismo aprovado, nunca no Git;
- validação de período de validade, emissor, sujeito e cadeia;
- assinatura sobre o artefato/hash correto da edição imutável;
- carimbo do tempo por ACT quando contratado/configurado;
- registro auditável do recibo sem exposição da chave privada;
- procedimento de renovação/revogação e contingência.

## Estados

`DemoDigitalSigner`/equivalente é somente POC. Produção sem provider retorna `NOT_CONFIGURED` e bloqueia o fluxo que exige assinatura real. O timestamp também permanece `NOT_CONFIGURED` até endpoint/provider e credencial reais.

## Critério de aceite

Uma edição de homologação deve ser assinada pelo provider real, validada independentemente, publicada, baixada novamente e ter hash/código/QR reconciliados. A Prefeitura/jurídico deve confirmar o procedimento institucional. Até isso ocorrer, ICP-Brasil/timestamp permanecem `EXTERNAL_DEPENDENCY`.
