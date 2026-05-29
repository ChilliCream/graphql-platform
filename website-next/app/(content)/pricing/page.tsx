import type { Metadata } from "next";

import { ComparisonTable } from "@/src/components/pricing/ComparisonTable";
import { EnterpriseBanner } from "@/src/components/pricing/EnterpriseBanner";
import { NitroTierCards } from "@/src/components/pricing/NitroTierCards";
import { OssStrip } from "@/src/components/pricing/OssStrip";
import { PricingFaq } from "@/src/components/pricing/PricingFaq";
import { PricingFooterCta } from "@/src/components/pricing/PricingFooterCta";
import { PricingHero } from "@/src/components/pricing/PricingHero";

import "@/src/components/pricing/pricing.css";

export const metadata: Metadata = {
  title: "Pricing",
  description:
    "Open source, all the way up. Pay only for the parts you don't want to run. Compare the Nitro Free, Hosted, Self-Hosted, and Enterprise plans.",
};

// Pricing reads as a stack of full-bleed bands with rhythm, not as stacked
// cards. Mapping (in scroll order):
//   01 hero        -> default     (page bg, calculator carries the weight)
//   02 oss strip   -> inverted    (near-black "open source belt")
//   03 nitro plans -> default     (tier cards)
//   04 compare     -> default     (table content-on-band, sticky thead)
//   05 enterprise  -> accent      (page accent washes the band)
//   06 faq         -> tinted      (subtle tonal lift)
//   07 footer cta  -> glow        (radial accent corner, final ask)
export default function PricingPage() {
  return (
    <div className="cc-pricing-root">
      <section className="cc-band cc-band-hero">
        <div className="cc-band-inner">
          <PricingHero />
        </div>
      </section>
      <section className="cc-band cc-band-oss">
        <div className="cc-band-inner">
          <OssStrip />
        </div>
      </section>
      <section className="cc-band cc-band-tiers">
        <div className="cc-band-inner">
          <NitroTierCards />
        </div>
      </section>
      <section className="cc-band cc-band-compare">
        <div className="cc-band-inner">
          <ComparisonTable />
        </div>
      </section>
      <section className="cc-band cc-band-enterprise">
        <div className="cc-band-inner">
          <EnterpriseBanner />
        </div>
      </section>
      <section className="cc-band cc-band-faq">
        <div className="cc-band-inner">
          <PricingFaq />
        </div>
      </section>
      <section className="cc-band cc-band-footer">
        <div className="cc-band-inner">
          <PricingFooterCta />
        </div>
      </section>
    </div>
  );
}
