"use client";

import styled from "styled-components";

import { AgentsRoot } from "../AgentsRoot";

// AgentsCinematicRoot extends AgentsRoot with the chrome the cinematic
// variant adds: extra top gutter on every Band so an `<ActLabel>` at
// top:36px has clearance above the section heading, an explicit
// `position: relative` so the absolute label resolves against the
// section's own box, and the legacy in-band `.cc-section-label` is
// hidden so the new ActLabel sits in the band gutter alone.
//
// Mirrors PricingCinematicRoot 1:1: same gutter rule, same legacy hide,
// same overflow:visible so the label lands in the gutter instead of being
// clipped. Everything not re-tuned here inherits from AgentsRoot so
// the dark navy / amber palette, typography, and button system stay
// 1:1 with the default variant.
export const AgentsCinematicRoot = styled(AgentsRoot)`
  /* ===== Cinematic gutter: every Band gets clearance above its heading
     so the ActLabel chapter marker at top:36px sits in the band gutter
     without overlapping the section heading. */
  & > section {
    padding-top: clamp(112px, 11vw, 176px);
    position: relative;
    overflow: visible;
  }

  /* The legacy in-section .cc-section-label is hidden in the cinematic
     variant: the new ActLabel sits in the band gutter and replaces it. */
  .cc-section-label {
    display: none;
  }

  /* ===== Loop diagram cinematic chrome =====
     The cinematic variant of the loop diagram renders explicit Bezier
     curves between each pair of stages (Observe -> Reason -> Act ->
     Compose -> Ship). The SVG container itself becomes the connector
     coordinate space, so it must be positioned, and the existing forward
     arcs are hidden so the connectors don't double up over them. */
  .cc-ag-loop.cc-ag-loop-cinematic {
    position: relative;
  }
  .cc-ag-loop.cc-ag-loop-cinematic .cc-ag-loop-forward-arc {
    display: none;
  }

  /* The HTML anchor row sits absolute on top of the SVG so each anchor
     resolves to the centre of the corresponding SVG node. The row is
     purely a layout vehicle for the zero-size <Anchor> spans; nothing
     visible renders here. */
  .cc-ag-loop-anchor-row {
    position: absolute;
    inset: 0;
    pointer-events: none;
  }
  .cc-ag-loop-anchor {
    position: absolute;
    top: 50%;
    transform: translate(-50%, -50%);
  }
`;
