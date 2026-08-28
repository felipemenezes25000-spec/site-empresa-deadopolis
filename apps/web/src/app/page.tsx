import { PortalHome } from "@/components/portal/portal-home";
import { readMenuResources } from "@/components/portal/public-shell";
import { getPortalHome, getResource } from "@/lib/portal-api";

export const dynamic = "force-dynamic";

export default async function Home() {
  const [content, homePage, menuResources] = await Promise.all([
    getPortalHome(),
    getResource("PAGE", "home"),
    readMenuResources(),
  ]);

  return (
    <PortalHome
      content={content}
      homeLayout={homePage?.payload}
      menuResources={menuResources}
      presentationMode={process.env.PRESENTATION_MODE === "true"}
    />
  );
}
