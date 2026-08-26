import type { NextRequest } from "next/server";

async function forward(request: NextRequest, context: { params: Promise<{ path: string[] }> }) {
  const { path } = await context.params;
  const apiUrl = process.env.API_URL ?? "http://localhost:5080";
  const destination = new URL(`/api/v1/${path.join("/")}`, apiUrl);
  destination.search = request.nextUrl.search;

  const headers = new Headers(request.headers);
  headers.set("X-Municipality", process.env.MUNICIPALITY_SLUG ?? "deodapolis");
  headers.delete("host");
  const response = await fetch(destination, {
    method: request.method,
    headers,
    body: request.method === "GET" || request.method === "HEAD" ? undefined : await request.arrayBuffer(),
    redirect: "manual",
  });

  return new Response(response.body, {
    status: response.status,
    headers: response.headers,
  });
}

export const GET = forward;
export const POST = forward;
export const PUT = forward;
export const PATCH = forward;
export const DELETE = forward;
