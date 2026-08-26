import { PortalHome } from "@/components/portal/portal-home";
import { getPortalHome } from "@/lib/portal-api";

export const dynamic = "force-dynamic";

export default async function Home() {
  const content = await getPortalHome();

  return (
    <PortalHome
      content={content}
      presentationMode={process.env.PRESENTATION_MODE === "true"}
    />
  );
}
