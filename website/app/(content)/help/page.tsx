import { HelpClosing } from "@/src/components/help/HelpClosing";
import { HelpFaq } from "@/src/components/help/HelpFaq";
import { HelpHero } from "@/src/components/help/HelpHero";
import { HelpTiers } from "@/src/components/help/HelpTiers";
import { SelfServeGrid } from "@/src/components/help/SelfServeGrid";
import { pageMetadata } from "@/src/helpers/pageMetadata";

export const metadata = pageMetadata({
  title: "Get GraphQL help fast",
  description:
    "Stuck on GraphQL? Get help from the ChilliCream community, book an expert consultancy session, or pick a support plan. Pick the path that fits the urgency.",
  keywords: [
    "GraphQL help",
    "ChilliCream support",
    "Hot Chocolate consultancy",
    "GraphQL Slack community",
    "GraphQL support plan",
  ],
  path: "/help",
});

export default function HelpPage() {
  return (
    <>
      <HelpHero />
      <HelpTiers />
      <SelfServeGrid />
      <HelpFaq />
      <HelpClosing />
    </>
  );
}
