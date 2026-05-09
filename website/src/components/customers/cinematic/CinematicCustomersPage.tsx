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
import { StampArchive } from "./StampArchive";

// Cinematic variant of the /customers index. Renders the default
// component tree under a `<CinematicCustomersRoot>` shell that lays a
// single archival flourish behind the bands: a hand-placed scatter of
// postage stamps with verified-reference postmarks, date strips, and
// wax-seal overstamps. The bands themselves are unchanged.
export const CinematicCustomersPage: FC = () => {
  return (
    <CinematicCustomersRoot>
      <StampArchive />
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
