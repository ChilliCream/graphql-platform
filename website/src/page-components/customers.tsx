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
      <CustomersRoot>
        <CustomersHero />
        <FeaturedRail />
        <ByTheNumbersBand />
        <IndustryTrustWall />
        <AllStoriesGrid />
        <ArchitectCallCta />
        <RelatedLinks />
      </CustomersRoot>
    </SiteLayout>
  );
};

export default CustomersPage;
