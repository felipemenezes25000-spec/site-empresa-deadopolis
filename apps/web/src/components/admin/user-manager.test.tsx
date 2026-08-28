import { fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { UserManager } from "./user-manager";

describe("UserManager", () => {
  afterEach(() => vi.unstubAllGlobals());

  it("creates a user using a governed RBAC role", async () => {
    const user = { id: "one", username: "admin.demo", displayName: "Admin", role: "SUPER_ADMIN", isActive: true, mfaEnabled: false, createdAt: "2026-08-27T12:00:00Z", lastLoginAt: null, lockedUntil: null };
    const created = { ...user, id: "two", username: "comunicacao.nova", displayName: "Comunicação Nova", role: "COMMUNICATION" };
    let userReads = 0;
    const fetchMock = vi.fn().mockImplementation((url: string, options?: RequestInit) => {
      if (url.endsWith("/roles")) return Promise.resolve(Response.json([{ role: "SUPER_ADMIN", capabilities: ["settings.manage", "users.manage"] }, { role: "COMMUNICATION", capabilities: ["content.write"] }]));
      if (url.endsWith("/auth/me")) return Promise.resolve(Response.json({ id: "one", displayName: "Admin", role: "SUPER_ADMIN", capabilities: ["users.manage"] }));
      if (options?.method === "POST") return Promise.resolve(Response.json(created, { status: 201 }));
      userReads++;
      return Promise.resolve(Response.json(userReads > 1 ? [user, created] : [user]));
    });
    vi.stubGlobal("fetch", fetchMock);
    render(<UserManager />);

    expect(await screen.findByText(/admin\.demo/)).toBeInTheDocument();
    expect(screen.getByText("users.manage", { exact: true })).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText("Usuário"), { target: { value: "comunicacao.nova" } });
    fireEvent.change(screen.getByLabelText("Nome de exibição"), { target: { value: "Comunicação Nova" } });
    fireEvent.change(screen.getByLabelText("Papel RBAC"), { target: { value: "COMMUNICATION" } });
    fireEvent.change(screen.getByLabelText(/^Senha temporária/), { target: { value: "Temporary-Strong-2026!" } });
    fireEvent.click(screen.getByRole("button", { name: "Criar usuário" }));

    expect(await screen.findByText("Usuário criado. Entregue a senha temporária por canal seguro e oriente a ativação de MFA.")).toBeInTheDocument();
    await waitFor(() => expect(fetchMock).toHaveBeenCalledWith("/api/v1/admin/users", expect.objectContaining({ method: "POST" })));
    expect(screen.getByText(/comunicacao\.nova/)).toBeInTheDocument();
  });

  it("protects the current account while allowing the governed lifecycle for another user", async () => {
    const current = { id: "current", username: "admin.demo", displayName: "Admin", role: "SUPER_ADMIN", isActive: true, mfaEnabled: true, createdAt: "2026-08-27T12:00:00Z", lastLoginAt: "2026-08-27T12:00:00Z", lockedUntil: null };
    const managed = { ...current, id: "managed", username: "editor.demo", displayName: "Editor", role: "COMMUNICATION", mfaEnabled: false };
    const fetchMock = vi.fn().mockImplementation((url: string, options?: RequestInit) => {
      if (url.endsWith("/roles")) return Promise.resolve(Response.json([{ role: "SUPER_ADMIN", capabilities: ["users.manage"] }, { role: "COMMUNICATION", capabilities: ["content.write"] }]));
      if (url.endsWith("/auth/me")) return Promise.resolve(Response.json({ id: current.id, displayName: current.displayName, role: current.role, capabilities: ["users.manage"] }));
      if (options?.method === "PUT" || options?.method === "POST") return Promise.resolve(Response.json(managed));
      return Promise.resolve(Response.json([current, managed]));
    });
    vi.stubGlobal("fetch", fetchMock);
    render(<UserManager />);

    const currentCard = await screen.findByRole("article", { name: /Admin.*sessão atual/i });
    expect(within(currentCard).getByRole("button", { name: "Salvar papel" })).toBeDisabled();
    expect(within(currentCard).getByRole("button", { name: "Revogar sessões" })).toBeDisabled();
    expect(within(currentCard).getByRole("button", { name: "Desativar" })).toBeDisabled();

    const managedCard = screen.getByRole("article", { name: /Editor.*editor\.demo/i });
    fireEvent.change(within(managedCard).getByLabelText("Papel de editor.demo"), { target: { value: "SUPER_ADMIN" } });
    fireEvent.click(within(managedCard).getByRole("button", { name: "Salvar papel" }));
    await waitFor(() => expect(fetchMock).toHaveBeenCalledWith("/api/v1/admin/users/managed/role", expect.objectContaining({ method: "PUT" })));
  });
});
