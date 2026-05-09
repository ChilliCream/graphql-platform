"use client";

import React, { FC } from "react";

import { ByTheNumbersBand } from "@/components/customers/ByTheNumbersBand";
import { RelatedLinks } from "@/components/customers/RelatedLinks";

import { CinematicAllStoriesGrid } from "./CinematicAllStoriesGrid";
import { CinematicArchitectCallCta } from "./CinematicArchitectCallCta";
import { CinematicCustomersHero } from "./CinematicCustomersHero";
import { CinematicCustomersRoot } from "./CinematicCustomersRoot";
import { CinematicFeaturedRail } from "./CinematicFeaturedRail";
import { CinematicIndustryTrustWall } from "./CinematicIndustryTrustWall";

// Cinematic variant of the /customers index. Mirrors the default band
// rhythm but threads homepage chrome through three precise insertions:
//
//   - `<ActLabel>` chapters: 01 CUSTOMERS · 02 FEATURED STORIES ·
//     03 TRUSTED BY · 04 ALL STORIES · 05 RESEARCH CALL.
//   - `<VibrantTile>` for the top three featured stories (orange,
//     yellow-rays, pink) — the marketing peak, not the spine.
//   - `<DottedGridBg>` behind the long trust wall — directory framing.
//
// ByTheNumbersBand and RelatedLinks reuse the default components verbatim.
// They sit between the chaptered bands and stay outside the act-label run.
export const CinematicCustomersPage: FC = () => {
  return (
    <CinematicCustomersRoot>
      <CinematicCustomersHero />
      <ByTheNumbersBand />
      <CinematicFeaturedRail />
      <CinematicIndustryTrustWall />
      <CinematicAllStoriesGrid />
      <CinematicArchitectCallCta />
      <RelatedLinks />
    </CinematicCustomersRoot>
  );
};
