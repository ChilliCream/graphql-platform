"use client";

import React, { FC } from "react";

import { AgentReframeCinematic } from "./AgentReframeCinematic";
import { AgentsCinematicRoot } from "./AgentsCinematicRoot";
import { AgentsFinalCtaCinematic } from "./AgentsFinalCtaCinematic";
import { AgentsHeroCinematic } from "./AgentsHeroCinematic";
import { AgentsLoopDiagramCinematic } from "./AgentsLoopDiagramCinematic";
import { AgentsPricingTeaserCinematic } from "./AgentsPricingTeaserCinematic";
import { AgentsProofCinematic } from "./AgentsProofCinematic";
import { GuardrailsSectionCinematic } from "./GuardrailsSectionCinematic";
import { ProductSurfaceTilesCinematic } from "./ProductSurfaceTilesCinematic";
import { WhatAgentSeesCinematic } from "./WhatAgentSeesCinematic";
import { WorksWhereYouWorkCinematic } from "./WorksWhereYouWorkCinematic";

// Cinematic variant of the agents page. Composes the cinematic-flavoured
// section components inside `AgentsCinematicRoot`, which extends the
// default `AgentsRoot` with extra band gutter clearance and hides the
// legacy inline `.cc-section-label` so the new `<ActLabel>` chapter
// markers carry the chapter chrome alone. The page expects to be rendered
// inside `<AccentThread page="agents">` (the page-component takes care of
// that so both variants share one thread instance).
//
// Page chapter rhythm (matches the homepage-uplift plan):
//   01 AGENTS              hero
//   02 REFRAME             cream tinted band, vs row
//   03 AGENT LOOP          accent band, ConnectorLine threading
//   04 SIX SURFACES        default band, six tiles
//   05 PROOF               inverted band, two demos
//   06 PRODUCT SURFACES    cream tinted band, six rows
//   07 GUARDRAILS          default band, four cards
//   08 DISTRIBUTION        cream tinted band, FrostedExplainer (cream)
//   09 BILLING             accent band, pricing teaser
//   10 READY?              glow band, final CTA

export const NitroAgentsCinematic: FC = () => {
  return (
    <AgentsCinematicRoot>
      <AgentsHeroCinematic />
      <AgentReframeCinematic />
      <AgentsLoopDiagramCinematic />
      <WhatAgentSeesCinematic />
      <AgentsProofCinematic />
      <ProductSurfaceTilesCinematic />
      <GuardrailsSectionCinematic />
      <WorksWhereYouWorkCinematic />
      <AgentsPricingTeaserCinematic />
      <AgentsFinalCtaCinematic />
    </AgentsCinematicRoot>
  );
};
