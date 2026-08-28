"use client";

import { useEffect, useState, type FormEvent } from "react";

type User = { id: string; username: string; displayName: string; role: string; isActive: boolean; mfaEnabled: boolean; createdAt: string; lastLoginAt: string | null; lockedUntil: string | null };
type Role = { role: string; capabilities: string[] };
type CurrentUser = { id: string };

export function UserManager() {
  const [users, setUsers] = useState<User[]>([]);
  const [roles, setRoles] = useState<Role[]>([]);
  const [currentUserId, setCurrentUserId] = useState("");
  const [message, setMessage] = useState("");
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    const controller = new AbortController();
    void Promise.all([
      fetch("/api/v1/admin/users", { signal: controller.signal }),
      fetch("/api/v1/admin/users/roles", { signal: controller.signal }),
      fetch("/api/v1/auth/me", { signal: controller.signal }),
    ]).then(async ([usersResponse, rolesResponse, currentUserResponse]) => {
      if (!usersResponse.ok) throw new Error(await errorText(usersResponse));
      if (!rolesResponse.ok) throw new Error(await errorText(rolesResponse));
      if (!currentUserResponse.ok) throw new Error(await errorText(currentUserResponse));
      const [userData, roleData, currentUser] = await Promise.all([usersResponse.json() as Promise<User[]>, rolesResponse.json() as Promise<Role[]>, currentUserResponse.json() as Promise<CurrentUser>]);
      if (!controller.signal.aborted) { setUsers(userData); setRoles(roleData); setCurrentUserId(currentUser.id); }
    }).catch((error) => {
      if (!controller.signal.aborted) setMessage(error instanceof Error ? error.message : "Não foi possível carregar os usuários.");
    }).finally(() => {
      if (!controller.signal.aborted) setLoading(false);
    });
    return () => controller.abort();
  }, []);

  async function refresh() {
    const response = await fetch("/api/v1/admin/users");
    if (!response.ok) throw new Error(await errorText(response));
    setUsers(await response.json() as User[]);
  }

  async function create(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const formElement = event.currentTarget;
    const form = new FormData(formElement);
    await execute(async () => {
      const response = await fetch("/api/v1/admin/users", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ username: form.get("username"), displayName: form.get("displayName"), role: form.get("role"), temporaryPassword: form.get("temporaryPassword") }),
      });
      if (!response.ok) throw new Error(await errorText(response));
      formElement.reset();
      await refresh();
      setMessage("Usuário criado. Entregue a senha temporária por canal seguro e oriente a ativação de MFA.");
    });
  }

  async function assignRole(id: string, event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const role = new FormData(event.currentTarget).get("role");
    await execute(async () => {
      const response = await fetch(`/api/v1/admin/users/${id}/role`, { method: "PUT", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ role }) });
      if (!response.ok) throw new Error(await errorText(response));
      await refresh();
      setMessage("Papel atualizado e sessões anteriores revogadas.");
    });
  }

  async function setActive(user: User) {
    await execute(async () => {
      const response = await fetch(`/api/v1/admin/users/${user.id}/state`, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ active: !user.isActive }) });
      if (!response.ok) throw new Error(await errorText(response));
      await refresh();
      setMessage(user.isActive ? "Conta desativada e sessões revogadas." : "Conta reativada.");
    });
  }

  async function revokeSessions(user: User) {
    await execute(async () => {
      const response = await fetch(`/api/v1/admin/users/${user.id}/sessions/revoke`, { method: "POST" });
      if (!response.ok) throw new Error(await errorText(response));
      await refresh();
      setMessage(`Sessões de ${user.username} revogadas.`);
    });
  }

  async function execute(action: () => Promise<void>) {
    setBusy(true); setMessage("");
    try { await action(); }
    catch (error) { setMessage(error instanceof Error ? error.message : "A operação não pôde ser concluída."); }
    finally { setBusy(false); }
  }

  if (loading) return <div className="admin-panel" aria-busy="true">Carregando usuários e papéis…</div>;

  return <div className="editor-grid">
    <form className="admin-panel editor-fields" onSubmit={create}>
      <h2>Novo usuário</h2>
      <label className="field">Usuário<input name="username" minLength={3} maxLength={100} pattern="[A-Za-z0-9._-]+" autoComplete="off" required /></label>
      <label className="field">Nome de exibição<input name="displayName" maxLength={160} required /></label>
      <label className="field">Papel RBAC<select name="role" required>{roles.map((role) => <option key={role.role} value={role.role}>{role.role}</option>)}</select></label>
      <label className="field">Senha temporária<input name="temporaryPassword" type="password" minLength={14} maxLength={128} autoComplete="new-password" required /><small>Use maiúscula, minúscula, número e símbolo. A senha não é retornada nem registrada em auditoria.</small></label>
      <button className="action-button" disabled={busy || roles.length === 0}>Criar usuário</button>
      <div className="warning-box"><strong>Operação institucional:</strong> entregue a senha fora de chamados e logs. O usuário deve cadastrar MFA na própria sessão.</div>
    </form>

    <section className="admin-panel">
      <h2>Papéis e capabilities</h2>
      <div className="compact-list">{roles.map((role) => <div className="compact-item" key={role.role}><div><strong>{role.role}</strong><ul aria-label={`Capabilities do papel ${role.role}`} style={{ display: "flex", flexWrap: "wrap", gap: 6, margin: "8px 0 0", padding: 0, listStyle: "none" }}>{role.capabilities.map((capability) => <li className="status-pill" key={capability}><code>{capability}</code></li>)}</ul></div><span className="status-pill" aria-label={`${role.capabilities.length} capabilities`}>{role.capabilities.length}</span></div>)}</div>
    </section>

    <section className="admin-panel" style={{ gridColumn: "1 / -1" }}>
      <h2>Contas do município</h2>
      {users.length === 0 ? <div className="empty-state"><p>Nenhuma conta cadastrada.</p></div> : <div className="compact-list">{users.map((user) => { const isCurrent = user.id === currentUserId; return <article className="compact-item" key={user.id} aria-label={`${user.displayName} · ${user.username}${isCurrent ? " · sessão atual" : ""}`}>
        <div><strong>{user.displayName}</strong>{isCurrent && <span className="status-pill" style={{ marginLeft: 8 }}>SESSÃO ATUAL</span>}<small style={{ display: "block" }}>{user.username} · {user.mfaEnabled ? "MFA ativo" : "MFA pendente"}</small><small style={{ display: "block" }}>{user.lastLoginAt ? `Último acesso ${formatDate(user.lastLoginAt)}` : "Ainda não acessou"}{user.lockedUntil ? ` · bloqueado até ${formatDate(user.lockedUntil)}` : ""}</small></div>
        <div>
          <form className="button-row" onSubmit={(event) => void assignRole(user.id, event)}><select key={`${user.id}-${user.role}`} name="role" defaultValue={user.role} aria-label={`Papel de ${user.username}`} disabled={isCurrent || busy}>{roles.map((role) => <option key={role.role} value={role.role}>{role.role}</option>)}</select><button className="action-button secondary" disabled={isCurrent || busy}>Salvar papel</button></form>
          <div className="button-row"><span className="status-pill">{user.isActive ? "ATIVO" : "INATIVO"}</span><button type="button" className="action-button secondary" disabled={isCurrent || busy} onClick={() => void revokeSessions(user)}>Revogar sessões</button><button type="button" className="action-button secondary" disabled={isCurrent || busy} onClick={() => void setActive(user)}>{user.isActive ? "Desativar" : "Reativar"}</button></div>
        </div>
      </article>; })}</div>}
      {message && <div className="form-message" role="status" aria-live="polite" style={{ marginTop: 16 }}>{message}</div>}
    </section>
  </div>;
}

function formatDate(value: string) { const date = new Date(value); return Number.isNaN(date.getTime()) ? value : new Intl.DateTimeFormat("pt-BR", { dateStyle: "short", timeStyle: "short" }).format(date); }
async function errorText(response: Response) { const body = await response.json().catch(() => null) as { title?: string; detail?: string; errors?: Record<string, string[]> } | null; const validation = Object.values(body?.errors ?? {}).flat().join(" "); return validation || body?.detail || body?.title || `Erro ${response.status}`; }
