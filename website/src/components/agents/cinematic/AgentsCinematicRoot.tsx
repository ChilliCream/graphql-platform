"use client";

import styled from "styled-components";

import { AgentsRoot } from "../AgentsRoot";

// AgentsCinematicRoot is the default AgentsRoot lifted onto a layered
// stacking context: the PunchCardBackground layer mounts at z-index 0 and
// every Band sits at z-index 1+ above it. Nothing else is re-tuned here so
// the dark navy / amber palette, typography, and button system stay 1:1
// with the default variant.
export const AgentsCinematicRoot = styled(AgentsRoot)`
  isolation: isolate;

  & > section {
    position: relative;
    z-index: 1;
  }
`;
