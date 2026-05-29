import type { Metadata } from "next";

import { ComparisonTable } from "@/src/components/pricing/ComparisonTable";
import { EnterpriseBanner } from "@/src/components/pricing/EnterpriseBanner";
import { NitroTierCards } from "@/src/components/pricing/NitroTierCards";
import { OssStrip } from "@/src/components/pricing/OssStrip";
import { PricingFaq } from "@/src/components/pricing/PricingFaq";
import { PricingFooterCta } from "@/src/components/pricing/PricingFooterCta";
import { PricingHero } from "@/src/components/pricing/PricingHero";

export const metadata: Metadata = {
  title: "Pricing",
  description:
    "Open source, all the way up. Pay only for the parts you don't want to run. Compare the Nitro Free, Hosted, Self-Hosted, and Enterprise plans.",
};

// Pricing reads as a stack of sections with rhythm (hero + calculator, the
// open-source belt, Nitro tier cards, the full comparison matrix, the
// enterprise band, an objection-led FAQ, and a final self-serve CTA).
export default function PricingPage() {
  return (
    <>
      <PricingHero />
      <OssStrip />
      <NitroTierCards />
      <ComparisonTable />
      <EnterpriseBanner />
      <PricingFaq />
      <PricingFooterCta />
    </>
  );
}
