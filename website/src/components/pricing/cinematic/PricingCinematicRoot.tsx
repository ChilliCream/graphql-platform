"use client";

import styled from "styled-components";

import { PricingRoot } from "../PricingRoot";

// PricingCinematicRoot is the default PricingRoot lifted onto a layered
// stacking context: the PriceTiers background layer mounts at z-index 0,
// every band sits at z-index 1+ above it. Nothing else is re-tuned here so
// the tonal palette, typography, and button system stay 1:1 with the
// default variant.
export const PricingCinematicRoot = styled(PricingRoot)`
  isolation: isolate;

  & > .cc-band {
    position: relative;
    z-index: 1;
  }
`;
