"use client";

import React, { FC } from "react";

import { LandingGlobalStyle } from "@/components/landing/LandingRoot";
import { SiteLayout } from "@/components/layout";
import { SEO } from "@/components/misc";
import { AccentThread } from "@/components/redesign-system/AccentThread";
import { VariantSwitcher } from "@/components/redesign-system/cinematic";
import { RecentBlogPost } from "@/components/widgets/most-recent-blog-posts-section";

import { PricingGridCompare } from "./PricingGridCompare";
import { PricingGridFaq } from "./PricingGridFaq";
import { PricingGridFooterCta } from "./PricingGridFooterCta";
import { PricingGridHero } from "./PricingGridHero";
import { PricingGridRoot } from "./PricingGridRoot";
import { PricingGridTiers } from "./PricingGridTiers";

interface PricingGridProps {
  recentPosts?: RecentBlogPost[];
}

// Grid variant of /pricing. Strict square cards, 1px hairline borders shared
// across adjacent cells, and a flat dark-navy canvas. Sections compose top
// to bottom following the canonical /pricing sequence in the Grid spec:
//
//   01 hero (archetype A)         text-only, two buttons
//   02 tier cards (archetype D)   3-up Nitro plans
//   03 comparison (archetype F)   feature x tier matrix as a real <table>
//   04 faq (archetype K)          asymmetric heading + accordion split
//   05 final cta (archetype L)    centered close
//
// The site Header, Footer, AccentThread, LandingGlobalStyle, and the
// VariantSwitcher position all stay identical to Default and Cinematic.
const VARIANT_OPTIONS = [
  { id: "default", label: "Default", href: "/pricing/" },
  { id: "cinematic", label: "Cinematic", href: "/pricing/?v=cinematic" },
  { id: "grid", label: "Grid", href: "/pricing/?v=grid" },
];

export const PricingGrid: FC<PricingGridProps> = () => {
  return (
    <SiteLayout disableStars>
      <SEO title="Pricing" />
      <LandingGlobalStyle />
      <AccentThread page="pricing">
        <PricingGridRoot>
          <PricingGridHero />
          <PricingGridTiers />
          <PricingGridCompare />
          <PricingGridFaq />
          <PricingGridFooterCta />
        </PricingGridRoot>
      </AccentThread>
      <VariantSwitcher options={VARIANT_OPTIONS} currentId="grid" />
    </SiteLayout>
  );
};
