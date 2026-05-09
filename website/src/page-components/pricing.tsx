"use client";

import React, { FC, useEffect } from "react";

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
import { PricingRoot } from "@/components/pricing/PricingRoot";
import { SpendControlsRow } from "@/components/pricing/SpendControlsRow";
import { RecentBlogPost } from "@/components/widgets/most-recent-blog-posts-section";

interface PricingPageProps {
  recentPosts?: RecentBlogPost[];
}

const PricingPage: FC<PricingPageProps> = () => {
  useEffect(() => {
    document.body.classList.add("cc-landing-body");
    return () => {
      document.body.classList.remove("cc-landing-body");
    };
  }, []);

  return (
    <SiteLayout disableStars>
      <SEO title="Pricing" />
      <LandingGlobalStyle />
      <PricingRoot>
        <PricingHero />
        <OssStrip />
        <NitroTierCards />
        <SpendControlsRow />
        <ComparisonTable />
        <EnterpriseBanner />
        <PricingFaq />
        <PricingFooterCta />
      </PricingRoot>
    </SiteLayout>
  );
};

export default PricingPage;
