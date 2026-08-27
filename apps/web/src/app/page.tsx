import { PortalHome } from "@/components/portal/portal-home";
import { getPortalHome, getResource } from "@/lib/portal-api";

export const dynamic = "force-dynamic";

export default async function Home() {
  const [content, homePage] = await Promise.all([
    getPortalHome(),
    getResource("PAGE", "home"),
  ]);

  return (
    <PortalHome
      content={content}
      homeLayout={homePage?.payload}
      presentationMode={process.env.PRESENTATION_MODE === "true"}
    />
  );
}
