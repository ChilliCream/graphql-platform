"use client";

import { useSearchParams } from "next/navigation";
import React, { FC, Suspense, useEffect } from "react";

import { LandingGlobalStyle } from "@/components/landing/LandingRoot";
import { SiteLayout } from "@/components/layout";
import { SEO } from "@/components/misc";
import { PricingCinematic } from "@/components/pricing/cinematic/PricingCinematic";
import { ComparisonTable } from "@/components/pricing/ComparisonTable";
import { PricingGrid } from "@/components/pricing/grid/PricingGrid";
import { EnterpriseBanner } from "@/components/pricing/EnterpriseBanner";
import { NitroTierCards } from "@/components/pricing/NitroTierCards";
import { OssStrip } from "@/components/pricing/OssStrip";
import { PricingFaq } from "@/components/pricing/PricingFaq";
import { PricingFooterCta } from "@/components/pricing/PricingFooterCta";
import { PricingHero } from "@/components/pricing/PricingHero";
import { PricingRoot } from "@/components/pricing/PricingRoot";
import { AccentThread } from "@/components/redesign-system/AccentThread";
import { Band } from "@/components/redesign-system/Band";
import { VariantSwitcher } from "@/components/redesign-system/cinematic";
import { RecentBlogPost } from "@/components/widgets/most-recent-blog-posts-section";

interface PricingPageProps {
  recentPosts?: RecentBlogPost[];
}

const VARIANT_OPTIONS = [
  { id: "default", label: "Default", href: "/pricing/" },
  { id: "cinematic", label: "Cinematic", href: "/pricing/?v=cinematic" },
  { id: "grid", label: "Grid", href: "/pricing/?v=grid" },
];

// Pricing reads as a stack of bands with rhythm, not as 8 stacked cards.
// Mapping (left to right in scroll order):
//   01 hero            -> default     (page bg, calculator carries weight)
//   02 oss strip       -> inverted    (the "open source belt", once per page)
//   03 nitro plans     -> default     (Card variant=constraint inside)
//   04 compare plans   -> default     (table content-on-band, sticky thead)
//   05 enterprise      -> accent      (page accent washes the band)
//   06 faq             -> tinted      (subtle tonal lift on the dark page)
//   07 footer cta      -> glow        (radial accent corner, final ask)
//
// Spend-controls is no longer a standalone band: it inlines into the Hosted
// tier card (P1-pricing-5).
const PricingDefault: FC<PricingPageProps> = () => {
  return (
    <SiteLayout disableStars>
      <SEO title="Pricing" />
      <LandingGlobalStyle />
      <AccentThread page="pricing">
        <PricingRoot>
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
        </PricingRoot>
      </AccentThread>
      <VariantSwitcher options={VARIANT_OPTIONS} currentId="default" />
    </SiteLayout>
  );
};

// Variant dispatcher reads `?v=cinematic` and renders the cinematic tree;
// any other value (or none) falls through to the default variant. Wrapped
// in <Suspense> because useSearchParams suspends during static export.
const PricingPageInner: FC<PricingPageProps> = ({ recentPosts }) => {
  const searchParams = useSearchParams();
  const variant = searchParams?.get("v");

  if (variant === "cinematic") {
    return <PricingCinematic />;
  }
  if (variant === "grid") {
    return <PricingGrid recentPosts={recentPosts} />;
  }
  return <PricingDefault recentPosts={recentPosts} />;
};

const PricingPage: FC<PricingPageProps> = ({ recentPosts }) => {
  useEffect(() => {
    document.body.classList.add("cc-landing-body");
    return () => {
      document.body.classList.remove("cc-landing-body");
    };
  }, []);

  return (
    <Suspense fallback={null}>
      <PricingPageInner recentPosts={recentPosts} />
    </Suspense>
  );
};

export default PricingPage;
