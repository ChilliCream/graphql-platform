import type { Metadata } from "next";

import { ButtonRow } from "@/src/components/ButtonRow";
import { MarketingHero } from "@/src/components/MarketingHero";
import { ClosingCta } from "@/src/components/pricing/ClosingCta";
import { CompareTable } from "@/src/components/pricing/CompareTable";
import { PlanSelector } from "@/src/components/pricing/PlanSelector";
import { PricingFaq } from "@/src/components/pricing/PricingFaq";
import { RegulatedBand } from "@/src/components/pricing/RegulatedBand";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { pageMetadata } from "@/src/helpers/pageMetadata";

export const metadata: Metadata = {
  ...pageMetadata({
    title: "Pricing",
    description:
      "Nitro pricing: start free on the shared cloud, pay as you go at $20/mo, run a dedicated single-tenant instance from $400, or self-host. Compare every plan.",
    path: "/pricing",
  }),
  keywords: [
    "ChilliCream pricing",
    "Nitro pricing",
    "GraphQL platform pricing",
    "GraphQL plans",
    "dedicated instance",
    "self-hosted",
    "schema registry pricing",
    "GraphQL observability pricing",
  ],
};

export default function PricingPage() {
  return (
    <>
      <Hero />
      <PlanSelector />
      <CompareTable />
      <PricingFaq />
      <RegulatedBand />
      <ClosingCta />
    </>
  );
}

function Hero() {
  return (
    <MarketingHero
      eyebrow="Nitro pricing"
      title="Pricing that scales with your platform."
      lead="Start free on the shared cloud. Pay as you go as traffic grows, run a dedicated single-tenant instance when you need your own region and isolation, or self-host on your own infrastructure."
      actions={
        <ButtonRow align="center">
          <SolidButton href="https://nitro.chillicream.com">
            Start for Free
          </SolidButton>
          <OutlineButton href="/services/support/contact?subject=Sales">
            Talk to Sales
          </OutlineButton>
        </ButtonRow>
      }
    />
  );
}
