"use client";

import styled from "styled-components";

import { IntegrationsRoot } from "../IntegrationsRoot";

// Cinematic shell for the /integrations index. Extends the default
// IntegrationsRoot tokens, typography, and section CSS verbatim so the
// dark-navy / cream-ink palette stays in lockstep, then layers on the
// single cinematic-only flourish: a `<CircuitTraces>` background that
// renders behind every band as the first child of the root.
//
// The default `<section>` surfaces sit at `position: relative` already, so
// no per-band lift is required. The traces render at `z-index: 0` behind
// the bands and never receive pointer events.
export const IntegrationsCinematicRoot = styled(IntegrationsRoot)`
  /* Lift band content above the circuit-trace background. Sections are
     already position: relative; this just guarantees stacking. */
  & > section {
    position: relative;
    z-index: 1;
  }
`;
