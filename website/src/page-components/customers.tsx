"use client";

import React, { FC, useEffect } from "react";

import { AllStoriesGrid } from "@/components/customers/AllStoriesGrid";
import { ArchitectCallCta } from "@/components/customers/ArchitectCallCta";
import { ByTheNumbersBand } from "@/components/customers/ByTheNumbersBand";
import { CustomersHero } from "@/components/customers/CustomersHero";
import { CustomersRoot } from "@/components/customers/CustomersRoot";
import { FeaturedRail } from "@/components/customers/FeaturedRail";
import { IndustryTrustWall } from "@/components/customers/IndustryTrustWall";
import { RelatedLinks } from "@/components/customers/RelatedLinks";
import { LandingGlobalStyle } from "@/components/landing/LandingRoot";
import { SiteLayout } from "@/components/layout";
import { SEO } from "@/components/misc";
import { AccentThread } from "@/components/redesign-system/AccentThread";

// Band rhythm (per uplift-plan.md /customers section):
//   01 hero              default
//   02 proof strip       tinted     attributed StatRow, no gradient
//   03 featured rail     default    cards stay (case-study exhibits)
//   04 trust wall        inverted   page's one full-bleed dark moment
//   05 all stories       default    cards stay (case-study exhibits)
//   06 architect CTA     glow       single accent-glow band
//   07 related           tinted     no card chrome, content-on-band
//
// Wrapped in AccentThread page="customers" so the slate-warm tokens
// thread through hero, StatRow hover, glow band, and accent rules.
const CustomersPage: FC = () => {
  useEffect(() => {
    document.body.classList.add("cc-landing-body");
    return () => {
      document.body.classList.remove("cc-landing-body");
    };
  }, []);

  return (
    <SiteLayout disableStars>
      <SEO
        title="Customers"
        description="Real customer stories from the platform teams running Hot Chocolate, Fusion, and Nitro in production. Federations that ship. Agents that connect. Humans that sleep."
      />
      <LandingGlobalStyle />
      <AccentThread page="customers">
        <CustomersRoot>
          <CustomersHero />
          <ByTheNumbersBand />
          <FeaturedRail />
          <IndustryTrustWall />
          <AllStoriesGrid />
          <ArchitectCallCta />
          <RelatedLinks />
        </CustomersRoot>
      </AccentThread>
    </SiteLayout>
  );
};

export default CustomersPage;
