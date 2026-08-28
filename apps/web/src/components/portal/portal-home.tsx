import type { PortalHomeContent, PortalResource } from "@/lib/portal-api";
import { HomeComposition } from "./home-composition";
import { PortalChrome } from "./public-shell";
import { StructuredData, governmentOrganization } from "./structured-data";

function portalBaseUrl() {
  return (process.env.PUBLIC_PORTAL_URL ?? "http://localhost:3000").replace(/\/+$/, "");
}

export function PortalHome({
  content,
  presentationMode = false,
  homeLayout,
  menuResources = [],
}: {
  content: PortalHomeContent;
  presentationMode?: boolean;
  homeLayout?: unknown;
  menuResources?: PortalResource[];
}) {
  return (
    <PortalChrome menuResources={menuResources} presentationMode={presentationMode}>
      <StructuredData data={governmentOrganization(portalBaseUrl())} />
      <HomeComposition content={content} payload={homeLayout} />
    </PortalChrome>
  );
}
