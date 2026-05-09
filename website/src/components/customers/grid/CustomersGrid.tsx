"use client";

import React, { FC } from "react";
import styled from "styled-components";

import { GRID_TOKENS } from "@/components/redesign-system/grid";

import { CustomersGridAllStories } from "./CustomersGridAllStories";
import { CustomersGridArchitectCta } from "./CustomersGridArchitectCta";
import { CustomersGridFeaturedRail } from "./CustomersGridFeaturedRail";
import { CustomersGridHero } from "./CustomersGridHero";
import { CustomersGridRelatedLinks } from "./CustomersGridRelatedLinks";
import { CustomersGridStatStrip } from "./CustomersGridStatStrip";
import { CustomersGridTrustWall } from "./CustomersGridTrustWall";

// Grid variant of /customers, modeled on `vercel-sol-marketing.jpeg` and
// `vercel-sol-saas.jpeg` per the grid-design-system spec, section 9.5.
//
// Section sequence:
//   01 Hero                  text + corner-cross ornaments
//   02 By the numbers        4-up stat strip with attributed metrics
//   03 Featured rail         3-cols x 2-rows hairline-bordered case grid
//   04 Trust wall            6-up typographic descriptor lockups (no logos)
//   05 All stories           3-up smaller case-study grid
//   06 Architect CTA         centered headline + strict 2-button row
//   07 Related links         3-up navigation footer
//
// All bands flow inside `CustomersGridRoot`, which owns the dark-navy
// surface, the per-page accent variable, and the typography reset. No
// vibrant tiles. No drop shadows. Border-radius is strictly zero.
export const CustomersGrid: FC = () => {
  return (
    <CustomersGridRoot>
      <CustomersGridHero />
      <CustomersGridStatStrip />
      <CustomersGridFeaturedRail />
      <CustomersGridTrustWall />
      <CustomersGridAllStories />
      <CustomersGridArchitectCta />
      <CustomersGridRelatedLinks />
    </CustomersGridRoot>
  );
};

// Page surface for the Grid variant. Holds the dark-navy base background,
// the local hairline tokens, and the box-sizing reset. Sections inside
// share a single hairline border so the page reads as one continuous
// drafting frame from header to footer.
const CustomersGridRoot = styled.div`
  --cc-grid-hairline: ${GRID_TOKENS.hairline};
  --cc-grid-hairline-strong: ${GRID_TOKENS.hairlineStrong};
  --cc-grid-card-bg: ${GRID_TOKENS.bgCard};
  --cc-grid-card-bg-inverted: ${GRID_TOKENS.bgInverted};
  --cc-grid-card-hover: ${GRID_TOKENS.bgHover};

  position: relative;
  width: 100%;
  background: ${GRID_TOKENS.bgBase};
  color: ${GRID_TOKENS.inkPrimary};
  font-family: var(--cc-font-sans), system-ui, sans-serif;

  * {
    box-sizing: border-box;
  }

  /* Section padding override: Grid bands run lighter than Default so that
     adjacent hairlines read as a continuous frame rather than as separate
     boxed sections. */
  > section {
    padding-top: clamp(64px, 9vw, 112px);
    padding-bottom: clamp(64px, 9vw, 112px);
  }

  /* The first section sits flush against the global header; the last sits
     flush against the global footer. Hairlines on those edges anchor the
     surface visually under the persistent chrome. */
  > section:first-child {
    border-top: 1px solid ${GRID_TOKENS.hairline};
  }
`;
