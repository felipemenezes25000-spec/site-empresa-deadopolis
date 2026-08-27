# E-mail institucional

A plataforma modela domínios, caixas, aliases e jobs de migração e expõe administração governada. Ela não embute credenciais de provedor.

## Provider

Em teste/Presentation Mode pode existir provider `DEMO_ONLY`. Fora desses ambientes, ausência de provedor real é apresentada como `NOT_CONFIGURED`; o sistema não finge entrega de e-mail.

## Go-live

São necessários: provedor contratado/aprovado, domínio e DNS, SPF, DKIM, DMARC, credenciais em secret manager, limites/cotas, remetentes autorizados, política de retenção e teste de entrega/recebimento.

## Migração

Jobs podem registrar origem IMAP/MBOX/EML e governança da migração. Credencial IMAP não deve ser persistida no job ou em payload de auditoria. Migração real deve reconciliar quantidade, falhas, anexos, datas e caixas de destino.

## Segurança operacional

Nunca registrar senha, OAuth refresh token, chave DKIM privada ou sessão de provedor. Mudança de DNS precisa de evidência e rollback. Eventos de entrega devem usar identificadores técnicos e evitar conteúdo sensível desnecessário.

## Estado

Governança de e-mail está implementada; provider, DNS e credenciais reais continuam `EXTERNAL_DEPENDENCY` até configuração e teste em domínio oficial.
