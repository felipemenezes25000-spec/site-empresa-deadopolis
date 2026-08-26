import type { NextRequest } from "next/server";
import { NextResponse } from "next/server";
import { isTransparencyCategory } from "@/lib/transparency-categories";

export function proxy(request: NextRequest) {
  const slug = request.nextUrl.pathname.slice("/transparencia/".length);
  if (isTransparencyCategory(slug)) return NextResponse.next();

  return NextResponse.rewrite(new URL("/_not-found", request.url), { status: 404 });
}

export const config = {
  matcher: "/transparencia/:slug",
};
