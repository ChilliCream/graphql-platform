"use client";

import styled from "styled-components";

import { IntegrationsRoot } from "../IntegrationsRoot";

// IntegrationsCinematicRoot extends IntegrationsRoot with the chrome that the
// cinematic variant adds: extra top gutter on every band so an `<ActLabel>` at
// top:36px has clearance above the section heading, an explicit
// `position: relative` on the dual-CTA band so a `<ScatterIllustration>`
// resolves against the band's own box, and dotted-grid clearance under the
// Native tile wall. Everything not explicitly re-tuned here inherits from
// IntegrationsRoot so the tonal palette, typography, and button system stay
// 1:1 with the default variant.
export const IntegrationsCinematicRoot = styled(IntegrationsRoot)`
  /* ===== Cinematic gutter: every band gets clearance above its heading
     so the ActLabel chapter marker at top:36px sits in the band's gutter
     without overlapping the section heading. The page is long and there
     are many chapter markers, so the floor is generous. */
  .cc-band {
    padding-top: clamp(112px, 11vw, 176px);
    position: relative;
    overflow: visible;
  }

  /* The legacy in-section .cc-section-label chip is hidden in the
     cinematic variant: the new ActLabel sits in the band gutter and
     replaces it. */
  .cc-section-label {
    display: none;
  }

  /* ===== Hero band: trim the in-section hero padding-top since the band
     supplies its own gutter for the ActLabel; the inner copy keeps its
     centered rhythm. */
  .cc-band.cc-band-hero .cc-in-hero {
    padding-top: 0;
  }

  /* ===== By-Category band: each category block carries its own ActLabel
     in cinematic mode, so the block needs position: relative and a top
     gutter for the chapter marker. Marquee blocks already have inset
     padding; raise their padding-top further so the label clears the
     accent rule and the eyebrow underneath. */
  .cc-band.cc-band-category .cc-in-cat-block {
    position: relative;
    padding-top: clamp(60px, 7vw, 92px);
  }
  .cc-band.cc-band-category .cc-in-cat-block.is-marquee {
    padding-top: clamp(70px, 8vw, 112px);
  }
  /* The category-block ActLabel is band-relative (not page-relative) so
     it lives at the top of its block, not at the band gutter. */
  .cc-band.cc-band-category .cc-in-cat-block > .cc-cinematic-cat-label {
    top: 18px;
    left: 0;
    position: absolute;
  }
  .cc-band.cc-band-category
    .cc-in-cat-block.is-marquee
    > .cc-cinematic-cat-label {
    top: 18px;
    left: 28px;
  }

  /* ===== Native band: a faint dotted-grid surface sits behind the tile
     wall, reframing the grid as a directory. The grid renders at z-index
     0 with the inner content lifted above on z-index 1 so cards stay
     interactive. */
  .cc-band.cc-band-native .cc-in-typesection-inner {
    position: relative;
    z-index: 1;
  }

  /* ===== Dual CTA band: a small orbit-mini scatter sits in the band's
     bottom-right negative space, echoing the hero's orbital diagram. The
     band needs position: relative for the scatter's percentage anchor;
     the inner CTA grid stays above on z-index 1 so the scatter never
     occludes the targets. */
  .cc-band.cc-band-dualcta .cc-in-dualcta-inner {
    position: relative;
    z-index: 1;
  }
`;
