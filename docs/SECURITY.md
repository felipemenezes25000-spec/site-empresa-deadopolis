# Segurança

Este documento descreve os controles presentes no código e as condições obrigatórias antes da produção. Ele não substitui pentest, política institucional ou análise jurídica.

## Controles implementados

- Isolamento por município com resolução obrigatória de tenant e filtros globais do EF Core.
- Autenticação por cookie `HttpOnly`, `SameSite=Strict` e `Secure` fora de Development/Testing.
- Sessões revogáveis, lockout por falhas, MFA TOTP e capabilities verificadas no backend.
- Rate limit no login e confirmação MFA.
- Auditoria tenant-scoped, correlation ID e logs estruturados sem corpo, cookie ou credencial.
- Proteção SSRF com validação de esquema, DNS, IP privado/local/reservado, portas e redirects.
- Redirect legado restrito a destino interno: `//host`, `/\host`, esquema absoluto, caractere de controle, auto-referência e ciclo entre regras são recusados na criação, na importação, no middleware e no resolvedor público.
- Limite de requisição também nas rotas anônimas de abertura e acompanhamento de manifestação.
- Upload com limite, magic bytes, MIME, SHA-256, quarentena e liberação condicionada ao scanner.
- CSP, HSTS em produção, `nosniff`, `frame-ancestors`, Referrer Policy e Permissions Policy.
- Data Protection persistente e separada por aplicação.
- Secret scan, rejeição de certificados/chaves privadas, auditoria de dependências e Trivy na CI.

## Fronteira de confiança

O navegador chama `/api/v1` pelo proxy do Next.js. O proxy define `X-Municipality` a partir da configuração do servidor, não do valor enviado pelo cliente. A API resolve novamente o município e aplica autorização e filtros de tenant. Nenhuma decisão de permissão depende apenas da interface.

O proxy do portal remove cabeçalhos hop-by-hop (`connection`, `transfer-encoding`, `upgrade` e afins) e o comprimento/codificação do corpo da resposta original: o runtime já decodifica e reenquadra o conteúdo, e repassar o enquadramento anterior travava respostas no navegador.

Cookies `SameSite=Strict` reduzem CSRF; mutações administrativas também exigem sessão e capability. A implantação deve manter portal e API na mesma origem pública prevista pela arquitetura e não deve ampliar o domínio do cookie. Se a topologia futura aceitar origens adicionais, deve-se adicionar proteção antiforgery explícita antes da mudança.

## Segredos e dados sensíveis

Nunca versionar `.env`, dumps, tokens, cookies, PFX, P12, PEM, KEY ou senha. Em produção, segredos devem vir do secret manager da infraestrutura e ser rotacionáveis. O key ring de Data Protection precisa de volume persistente, criptografado e acessível somente à API.

O modo de apresentação deve permanecer desligado em produção:

```text
ASPNETCORE_ENVIRONMENT=Production
PresentationMode=false
PRESENTATION_MODE=false
```

Sem provider real, storage, scanner, assinatura, timestamp e e-mail permanecem `NOT_CONFIGURED`. `DEMO_ONLY` é aceitável somente em Testing/Presentation.

Todas as superfícies administrativas publicam o mesmo vocabulário (`CONFIGURED`, `DEGRADED`, `UNAVAILABLE`, `NOT_CONFIGURED`) e a interface classifica `DEMO_ONLY`, `DEVELOPMENT_ONLY`, `DEGRADED`, `NOT_CONFIGURED` e `QUARANTINED` como situação que exige atenção, nunca como confirmação.

## Checklist de produção

- HTTPS válido no proxy e redirecionamento HTTP→HTTPS.
- `AllowedHosts` restrito aos hosts oficiais.
- banco sem porta pública e credencial exclusiva de menor privilégio.
- Data Protection persistente, protegido e incluído no plano de recuperação.
- WAF/rate limit perimetral, centralização de logs e alertas definidos.
- storage e scanner homologados antes de liberar uploads/documentos.
- MFA obrigatório para perfis privilegiados e usuários demo inexistentes.
- política de retenção para auditoria, tickets e dados pessoais aprovada.
- teste de IDOR, autorização negativa, upload malicioso, CSRF e tenant leakage no pentest.
- backup e restore drill aprovados conforme o runbook.

## Reporte de vulnerabilidade

Não abra issue pública contendo exploração, credenciais ou dados pessoais. Use o canal privado de segurança definido pelo mantenedor do repositório ou pela Prefeitura. O relato deve conter versão/commit, impacto, passos mínimos de reprodução e evidência sanitizada.
