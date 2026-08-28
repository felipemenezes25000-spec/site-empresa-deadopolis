"use client";

import { Eye, EyeOff, LockKeyhole, LogIn, UserRound } from "lucide-react";
import { useState, type FormEvent } from "react";

export function LoginForm() {
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);
  const [showPassword, setShowPassword] = useState(false);

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setLoading(true);
    setError("");
    const data = new FormData(event.currentTarget);
    try {
      const response = await fetch("/api/v1/auth/login", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
        body: JSON.stringify({ username: data.get("username"), password: data.get("password") }),
      });
      if (response.ok) {
        window.location.replace("/admin");
        return;
      }
      setError(response.status === 429 ? "Muitas tentativas. Aguarde um momento e tente novamente." : "Usuário ou senha inválidos.");
    } catch {
      setError("Não foi possível conectar ao serviço de autenticação.");
    } finally {
      setLoading(false);
    }
  }

  return (
    <form className="premium-login-form" onSubmit={submit}>
      <div className="field premium-login-field">
        <label htmlFor="username">Usuário</label>
        <div className="login-input-frame"><UserRound size={18} aria-hidden="true" /><input id="username" name="username" autoComplete="username" autoFocus required placeholder="Seu usuário" /></div>
      </div>
      <div className="field premium-login-field">
        <label htmlFor="password">Senha</label>
        <div className="login-input-frame"><LockKeyhole size={18} aria-hidden="true" /><input id="password" name="password" type={showPassword ? "text" : "password"} autoComplete="current-password" required placeholder="Sua senha" /><button type="button" className="login-password-toggle" onClick={() => setShowPassword((current) => !current)} aria-label={showPassword ? "Ocultar senha" : "Mostrar senha"}>{showPassword ? <EyeOff size={18} aria-hidden="true" /> : <Eye size={18} aria-hidden="true" />}</button></div>
      </div>
      {error && <div className="form-message error login-error" role="alert">{error}</div>}
      <button className="action-button premium-login-submit" disabled={loading} aria-busy={loading}>{loading ? <><span className="login-spinner" aria-hidden="true" /> Entrando…</> : <>Entrar <LogIn size={18} aria-hidden="true" /></>}</button>
    </form>
  );
}
