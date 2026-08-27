# LGPD — controles técnicos e responsabilidades

Este documento descreve controles técnicos da plataforma; não substitui parecer jurídico, inventário institucional de tratamento, definição de bases legais ou atuação do encarregado.

## Controles existentes

- autenticação administrativa, MFA e capabilities/RBAC;
- isolamento por município e filtros multi-tenant;
- cookies HttpOnly/SameSite e política Secure fora de desenvolvimento/teste;
- trilha de auditoria e correlation ID;
- limite de upload, inspeção de arquivo, quarentena e scanner provider;
- headers de segurança, rate limiting e proteção SSRF;
- segregação explícita entre providers de demo e produção;
- ausência de credenciais reais no repositório, com secret scan na CI.

## Dados pessoais

Formulários públicos e administrativos devem coletar somente campos necessários à finalidade institucional. Dados de Ouvidoria/e-SIC e logs administrativos exigem política de retenção, controle de acesso e canal formal para atendimento dos direitos do titular quando aplicável.

## Antes do go-live

A Prefeitura deve aprovar: controlador/operador e responsabilidades; encarregado/canal; bases legais; inventário de tratamentos; retenção e descarte; operadores/suboperadores; transferência internacional quando houver; procedimento de incidente; atendimento de direitos; textos de privacidade/cookies/formulários.

## Logs e auditoria

Logs não devem registrar senha, token, chave privada ou conteúdo sensível desnecessário. Identificadores de auditoria devem ter retenção institucional definida. Exportações para observabilidade/SIEM devem respeitar minimização e controles do destino.

## Estado

A plataforma fornece controles técnicos relevantes, mas **conformidade LGPD de produção depende de governança e aceite institucional/jurídico**. Esse aceite permanece dependência externa e não é inferido pela existência deste arquivo.
