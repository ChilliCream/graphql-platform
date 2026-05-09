"use client";

import styled from "styled-components";

import { PricingRoot } from "../PricingRoot";

// PricingCinematicRoot extends PricingRoot with the chrome that the cinematic
// variant adds: extra top gutter on every band so an ActLabel at top:36px
// has clearance above the section heading, an explicit position:relative on
// the OSS-strip band so a ScatterIllustration resolves against the band's
// own box, and a tighter relationship between the cinematic chrome and the
// page accent. Everything not explicitly re-tuned here inherits from
// PricingRoot so the tonal palette, typography, and button system stay
// 1:1 with the default variant.
export const PricingCinematicRoot = styled(PricingRoot)`
  /* Cinematic gutter: every band gets clearance above its heading so the
     ActLabel chapter marker at top:36px sits in the band's gutter without
     overlapping the section heading. */
  .cc-band {
    padding-top: clamp(112px, 11vw, 176px);
    position: relative;
    overflow: visible;
  }

  /* OSS strip needs position:relative and overflow:visible so the absolute
     ScatterIllustration anchors to the band's own bounds and isn't clipped
     by the surrounding page background. */
  .cc-band.cc-band-oss {
    position: relative;
    overflow: visible;
  }

  /* The legacy in-section .cc-section-label is hidden in the cinematic
     variant: the new ActLabel sits in the band gutter and replaces it. */
  .cc-section-label {
    display: none;
  }

  /* The Every meter, every cell comparison band gets a tighter gap between
     the cinematic header chip row and the heading since the chips now act
     as the chapter banner. */
  .cc-cinematic-compare-chips {
    margin: 0 auto 24px;
    max-width: 1080px;
  }
`;
