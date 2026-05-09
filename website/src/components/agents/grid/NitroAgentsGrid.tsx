"use client";

import React, { FC } from "react";

import { AgentsGridDemos } from "./AgentsGridDemos";
import { AgentsGridDistribution } from "./AgentsGridDistribution";
import { AgentsGridFinalCta } from "./AgentsGridFinalCta";
import { AgentsGridGuardrails } from "./AgentsGridGuardrails";
import { AgentsGridHero } from "./AgentsGridHero";
import { AgentsGridLoop } from "./AgentsGridLoop";
import { AgentsGridPricing } from "./AgentsGridPricing";
import { AgentsGridProducts } from "./AgentsGridProducts";
import { AgentsGridReframe } from "./AgentsGridReframe";
import { AgentsGridRoot } from "./AgentsGridRoot";
import { AgentsGridSurfaces } from "./AgentsGridSurfaces";

// Grid variant of /products/nitro/agents. Renders the same content tree as
// the default variant (hero, reframe, loop, six surfaces, two demos,
// product surfaces, guardrails, distribution, pricing teaser, final CTA)
// recomposed onto the Vercel-derived Grid system: strict 1px hairlines,
// zero corner radius, no shadows, no chrome gradients, accent thread used
// only as a system signal in eyebrows / arrows / amber-tinted cells.
//
// VariantSwitcher is owned by the page-component dispatcher
// (`/page-components/nitro-agents.tsx`) so grid readers can hop between
// Default, Cinematic, and Grid from the same fixed-position pill.
export const NitroAgentsGrid: FC = () => {
  return (
    <AgentsGridRoot>
      <AgentsGridHero />
      <AgentsGridReframe />
      <AgentsGridLoop />
      <AgentsGridSurfaces />
      <AgentsGridDemos />
      <AgentsGridProducts />
      <AgentsGridGuardrails />
      <AgentsGridDistribution />
      <AgentsGridPricing />
      <AgentsGridFinalCta />
    </AgentsGridRoot>
  );
};
