import type { PortalHomeContent, PortalResource } from "@/lib/portal-api";
import { HomeComposition } from "./home-composition";
import { PortalChrome } from "./public-shell";

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
      <HomeComposition content={content} payload={homeLayout} />
    </PortalChrome>
  );
}
