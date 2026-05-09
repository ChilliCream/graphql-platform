"use client";

import React, { FC } from "react";

import { AllStoriesGrid } from "@/components/customers/AllStoriesGrid";
import { ArchitectCallCta } from "@/components/customers/ArchitectCallCta";
import { ByTheNumbersBand } from "@/components/customers/ByTheNumbersBand";
import { CustomersHero } from "@/components/customers/CustomersHero";
import { FeaturedRail } from "@/components/customers/FeaturedRail";
import { IndustryTrustWall } from "@/components/customers/IndustryTrustWall";
import { RelatedLinks } from "@/components/customers/RelatedLinks";

import { CinematicCustomersRoot } from "./CinematicCustomersRoot";
import { MonogramWatermark } from "./MonogramWatermark";

// Cinematic variant of the /customers index. Renders the default
// component tree under a `<CinematicCustomersRoot>` shell that lays a
// single quiet flourish behind the bands: an oversized outlined "CC"
// monogram watermark anchored at the right edge of the page, paired
// with a thin letterhead rule across the very top. The bands
// themselves are unchanged.
export const CinematicCustomersPage: FC = () => {
  return (
    <CinematicCustomersRoot>
      <MonogramWatermark />
      <CustomersHero />
      <ByTheNumbersBand />
      <FeaturedRail />
      <IndustryTrustWall />
      <AllStoriesGrid />
      <ArchitectCallCta />
      <RelatedLinks />
    </CinematicCustomersRoot>
  );
};
