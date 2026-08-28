import { NextRequest } from "next/server";
import { afterEach, describe, expect, it, vi } from "vitest";
import { GET, POST } from "./route";

function params(path: string[]) {
  return { params: Promise.resolve({ path }) };
}

describe("API proxy route", () => {
  afterEach(() => vi.unstubAllGlobals());

  it("never re-emits the upstream connection framing to the browser", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response('{"ok":true}', {
      status: 200,
      headers: {
        "content-type": "application/json",
        "transfer-encoding": "chunked",
        "content-length": "11",
        "content-encoding": "gzip",
        connection: "keep-alive",
        "x-correlation-id": "abc",
      },
    })));

    const response = await GET(new NextRequest("http://portal.test/api/v1/tickets/DEO-1?code=secret"), params(["tickets", "DEO-1"]));

    expect(response.status).toBe(200);
    expect(response.headers.get("transfer-encoding")).toBeNull();
    expect(response.headers.get("content-encoding")).toBeNull();
    expect(response.headers.get("content-length")).toBeNull();
    expect(response.headers.get("connection")).toBeNull();
    expect(response.headers.get("x-correlation-id")).toBe("abc");
    expect(await response.json()).toEqual({ ok: true });
  });

  it("forwards the governed tenant header and the original query string", async () => {
    const fetchMock = vi.fn().mockResolvedValue(Response.json({ ok: true }));
    vi.stubGlobal("fetch", fetchMock);

    await GET(new NextRequest("http://portal.test/api/v1/tickets/DEO-1?code=secret"), params(["tickets", "DEO-1"]));

    const [destination, init] = fetchMock.mock.calls[0] as [URL, RequestInit];
    expect(destination.pathname).toBe("/api/v1/tickets/DEO-1");
    expect(destination.search).toBe("?code=secret");
    expect(new Headers(init.headers).get("X-Municipality")).toBe("deodapolis");
    expect(new Headers(init.headers).get("host")).toBeNull();
  });

  it("does not relay a stale request body length upstream", async () => {
    const fetchMock = vi.fn().mockResolvedValue(Response.json({ ok: true }, { status: 201 }));
    vi.stubGlobal("fetch", fetchMock);

    const request = new NextRequest("http://portal.test/api/v1/tickets", {
      method: "POST",
      body: JSON.stringify({ description: "manifestação" }),
      headers: { "content-type": "application/json", "content-length": "999", "transfer-encoding": "chunked" },
    });
    const response = await POST(request, params(["tickets"]));

    const [, init] = fetchMock.mock.calls[0] as [URL, RequestInit];
    const forwarded = new Headers(init.headers);
    expect(response.status).toBe(201);
    expect(forwarded.get("content-length")).toBeNull();
    expect(forwarded.get("transfer-encoding")).toBeNull();
    expect(forwarded.get("content-type")).toBe("application/json");
  });
});
