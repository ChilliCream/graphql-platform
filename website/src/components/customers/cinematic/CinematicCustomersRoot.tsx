"use client";

import styled from "styled-components";

import { CustomersRoot } from "../CustomersRoot";

// Cinematic shell for the /customers index. Extends the default
// CustomersRoot tokens, typography, and section CSS verbatim so the
// dark-navy / cream-ink palette stays in lockstep, then layers on the
// single cinematic-only flourish: a `<StampArchive>` background that
// renders behind every band as the first child of the root.
//
// The default `<Band>` surfaces sit at `position: relative` already, so
// no per-band lift is required. The archive renders at `z-index: 0`
// behind the bands and never receives pointer events.
export const CinematicCustomersRoot = styled(CustomersRoot)`
  /* Lift band content above the archival background. Bands are already
     position: relative; this just guarantees stacking. */
  & > section {
    position: relative;
    z-index: 1;
  }
`;
