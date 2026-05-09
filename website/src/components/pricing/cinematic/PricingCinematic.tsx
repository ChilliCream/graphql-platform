"use client";

import React, { FC } from "react";

import { LandingGlobalStyle } from "@/components/landing/LandingRoot";
import { SiteLayout } from "@/components/layout";
import { SEO } from "@/components/misc";
import { EnterpriseBanner } from "@/components/pricing/EnterpriseBanner";
import { NitroTierCards } from "@/components/pricing/NitroTierCards";
import { PricingFaq } from "@/components/pricing/PricingFaq";
import { PricingFooterCta } from "@/components/pricing/PricingFooterCta";
import { AccentThread } from "@/components/redesign-system/AccentThread";
import { Band } from "@/components/redesign-system/Band";
import {
  ActLabel,
  VariantSwitcher,
} from "@/components/redesign-system/cinematic";

import { PricingCinematicComparisonTable } from "./PricingCinematicComparisonTable";
import { PricingCinematicHero } from "./PricingCinematicHero";
import { PricingCinematicOssStrip } from "./PricingCinematicOssStrip";
import { PricingCinematicRoot } from "./PricingCinematicRoot";

// Cinematic variant of /pricing. Same band rhythm as the default variant,
// same data files, same accent thread. The chrome lifted from the homepage:
//   * <ActLabel n="0X" name="..." /> in the gutter of every band, replacing
//     the undersized inline `.cc-section-label` (hidden by cinematic root);
//   * <TerminalChipRow accent="prism" /> on the OSS-strip and the comparison
//     table header so prism-bordered adapter chrome reads across both
//     "what's free" and "what's metered" beats;
//   * <ScatterIllustration variant="brewer-mini" /> anchored in the OSS-strip
//     band's bottom-right, echoing the brewing metaphor without re-using the
//     full Act 1 cup field.
//
// Anti-patterns this variant deliberately AVOIDS (per uplift-plan section 4):
//   * No connector lines through the comparison table.
//   * No vibrant blog tiles on a pricing page.
//   * No FusionLensEffect.
//
// VariantSwitcher is mounted once at the bottom of the tree so cinematic
// readers can hop back to the default variant.
const VARIANT_OPTIONS = [
  { id: "default", label: "Default", href: "/pricing/" },
  { id: "cinematic", label: "Cinematic", href: "/pricing/?v=cinematic" },
];

export const PricingCinematic: FC = () => {
  return (
    <SiteLayout disableStars>
      <SEO title="Pricing" />
      <LandingGlobalStyle />
      <AccentThread page="pricing">
        <PricingCinematicRoot>
          <Band variant="default" className="cc-band cc-band-hero">
            <ActLabel n="01" name="Pricing" />
            <PricingCinematicHero />
          </Band>
          <Band variant="inverted" className="cc-band cc-band-oss">
            <ActLabel n="02" name="Open Source Forever" />
            <PricingCinematicOssStrip />
          </Band>
          <Band variant="default" className="cc-band cc-band-tiers">
            <ActLabel n="03" name="Brew It Your Way" />
            <NitroTierCards />
          </Band>
          <Band variant="default" className="cc-band cc-band-compare">
            <ActLabel n="04" name="Every Meter, Every Cell" />
            <PricingCinematicComparisonTable />
          </Band>
          <Band variant="accent" className="cc-band cc-band-enterprise">
            <ActLabel n="05" name="Running Fusion in Production" />
            <EnterpriseBanner />
          </Band>
          <Band variant="tinted" className="cc-band cc-band-faq">
            <ActLabel n="06" name="Honest Answers" />
            <PricingFaq />
          </Band>
          <Band
            variant="glow"
            glowFrom="bottom-right"
            className="cc-band cc-band-footer"
          >
            <ActLabel n="07" name="Start Free" />
            <PricingFooterCta />
          </Band>
        </PricingCinematicRoot>
      </AccentThread>
      <VariantSwitcher options={VARIANT_OPTIONS} currentId="cinematic" />
    </SiteLayout>
  );
};
