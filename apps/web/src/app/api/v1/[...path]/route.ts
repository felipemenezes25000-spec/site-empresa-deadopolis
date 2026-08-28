import type { NextRequest } from "next/server";

// Hop-by-hop headers are scoped to a single connection (RFC 9110 §7.6.1). Forwarding them
// re-frames a body that this runtime already framed and can stall the browser response.
const hopByHopHeaders = ["connection", "keep-alive", "proxy-authenticate", "proxy-authorization", "te", "trailer", "transfer-encoding", "upgrade"];
// The runtime decodes the upstream body transparently, so the upstream length and encoding no longer
// describe what we emit. The request body is re-sent from a buffer, so only its length is restated.
const rewrittenResponseHeaders = ["content-encoding", "content-length"];
const rewrittenRequestHeaders = ["content-length"];
// An upstream that never answers must fail visibly instead of holding the browser request open.
const upstreamTimeoutMs = 20_000;

async function forward(request: NextRequest, context: { params: Promise<{ path: string[] }> }) {
  const { path } = await context.params;
  const apiUrl = process.env.API_URL ?? "http://localhost:5080";
  const destination = new URL(`/api/v1/${path.join("/")}`, apiUrl);
  destination.search = request.nextUrl.search;

  const headers = new Headers(request.headers);
  headers.set("X-Municipality", process.env.MUNICIPALITY_SLUG ?? "deodapolis");
  headers.delete("host");
  for (const header of [...hopByHopHeaders, ...rewrittenRequestHeaders]) headers.delete(header);

  let response: Response;
  try {
    response = await fetch(destination, {
      method: request.method,
      headers,
      body: request.method === "GET" || request.method === "HEAD" ? undefined : await request.arrayBuffer(),
      redirect: "manual",
      signal: AbortSignal.timeout(upstreamTimeoutMs),
    });
  } catch {
    return Response.json(
      { title: "Serviço municipal indisponível", detail: "A API municipal não respondeu a tempo. Tente novamente em instantes.", status: 504 },
      { status: 504, headers: { "Content-Type": "application/problem+json" } },
    );
  }

  const responseHeaders = new Headers(response.headers);
  for (const header of [...hopByHopHeaders, ...rewrittenResponseHeaders]) responseHeaders.delete(header);

  return new Response(response.body, {
    status: response.status,
    headers: responseHeaders,
  });
}

export const GET = forward;
export const POST = forward;
export const PUT = forward;
export const PATCH = forward;
export const DELETE = forward;
