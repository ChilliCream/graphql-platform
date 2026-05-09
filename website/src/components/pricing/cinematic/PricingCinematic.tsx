"use client";

import React, { FC } from "react";

import { LandingGlobalStyle } from "@/components/landing/LandingRoot";
import { SiteLayout } from "@/components/layout";
import { SEO } from "@/components/misc";
import { ComparisonTable } from "@/components/pricing/ComparisonTable";
import { EnterpriseBanner } from "@/components/pricing/EnterpriseBanner";
import { NitroTierCards } from "@/components/pricing/NitroTierCards";
import { OssStrip } from "@/components/pricing/OssStrip";
import { PricingFaq } from "@/components/pricing/PricingFaq";
import { PricingFooterCta } from "@/components/pricing/PricingFooterCta";
import { PricingHero } from "@/components/pricing/PricingHero";
import { AccentThread } from "@/components/redesign-system/AccentThread";
import { Band } from "@/components/redesign-system/Band";
import { VariantSwitcher } from "@/components/redesign-system/cinematic";

import { PriceTiers } from "./PriceTiers";
import { PricingCinematicRoot } from "./PricingCinematicRoot";

// Cinematic variant of /pricing. Renders the same component tree as the
// default variant (PricingHero, OssStrip, NitroTierCards, ComparisonTable,
// EnterpriseBanner, PricingFaq, PricingFooterCta) with a single distinctive
// design idea layered behind everything: a sparse price-tier ladder of
// hairlines paired with an oversized outlined "$" glyph bleeding off the
// right edge.
//
// VariantSwitcher is mounted at the bottom of the tree so cinematic readers
// can hop back to the default variant.
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
          <PriceTiers />
          <Band variant="default" className="cc-band cc-band-hero">
            <PricingHero />
          </Band>
          <Band variant="inverted" className="cc-band cc-band-oss">
            <OssStrip />
          </Band>
          <Band variant="default" className="cc-band cc-band-tiers">
            <NitroTierCards />
          </Band>
          <Band variant="default" className="cc-band cc-band-compare">
            <ComparisonTable />
          </Band>
          <Band variant="accent" className="cc-band cc-band-enterprise">
            <EnterpriseBanner />
          </Band>
          <Band variant="tinted" className="cc-band cc-band-faq">
            <PricingFaq />
          </Band>
          <Band
            variant="glow"
            glowFrom="bottom-right"
            className="cc-band cc-band-footer"
          >
            <PricingFooterCta />
          </Band>
        </PricingCinematicRoot>
      </AccentThread>
      <VariantSwitcher options={VARIANT_OPTIONS} currentId="cinematic" />
    </SiteLayout>
  );
};
