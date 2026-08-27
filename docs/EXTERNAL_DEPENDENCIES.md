# Dependências externas para produção

Somente itens que exigem autoridade, fornecedor, credencial ou dado institucional permanecem abertos.

| Dependência | Estado sem configuração | Condição de aceite |
|---|---|---|
| Conteúdo oficial e responsáveis | conteúdo demo/fallback | revisão e publicação pela Prefeitura, sem dados inventados |
| Object storage | `NOT_CONFIGURED` | bucket/endpoint, política de acesso, criptografia, retenção e credencial homologados |
| Malware scanner | `NOT_CONFIGURED` | provider real bloqueando arquivo malicioso antes da aprovação |
| ICP-Brasil | `NOT_CONFIGURED` | certificado/serviço, cadeia, senha protegida, política e validação homologados |
| Carimbo do tempo | `NOT_CONFIGURED` | ACT/provider contratado e health real |
| E-mail institucional | `NOT_CONFIGURED` | domínio, DNS, SMTP/API/IMAP, quotas e credenciais aprovados |
| e-SIC/GEO-OBRAS/compras | link/integração pendente | fonte oficial e estratégia de continuidade aprovadas |
| Backup/monitoramento/WAF | `NOT_CONFIGURED` ou evidência ausente | provider, retenção, alertas, RPO/RTO e restore drill reais |
| DNS/TLS/reverse proxy | ambiente local | hosts oficiais, certificado, headers e roteamento homologados |
| Redirecionamentos 301 | regras só podem ser ativadas após destino | conteúdo publicado, destino sem 404 e aceite do mapa final |
| LGPD e acessibilidade humana | automação não substitui aceite | política jurídica, encarregado e auditoria assistiva aprovados |

## Regra de integração

Cada provider real deve implementar a interface já existente, validar configuração no startup/health, retornar `NOT_CONFIGURED` quando faltar segredo e nunca cair silenciosamente para demo. Credenciais ficam fora do Git. A introdução de SDK só ocorre depois de escolher o fornecedor e passar por revisão de segurança/licença.

## Corte de produção

O go-live exige change request com responsáveis, inventário do conteúdo, último crawl do legado, mapa de redirects, evidência de backup/restore, resultado do pentest, run do CI do commit implantado e plano de rollback. Enquanto qualquer condição acima estiver ausente, o status correto é **READY FOR POC / produção bloqueada por dependência externa**.
