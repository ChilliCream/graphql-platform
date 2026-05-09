"use client";

import React, { FC } from "react";

import { AgentDemo } from "../AgentDemo";
import { AgentReframe } from "../AgentReframe";
import { AgentsFinalCta } from "../AgentsFinalCta";
import { AgentsHero } from "../AgentsHero";
import { AgentsLoopDiagram } from "../AgentsLoopDiagram";
import { AgentsPricingTeaser } from "../AgentsPricingTeaser";
import { GuardrailsSection } from "../GuardrailsSection";
import { ProductSurfaceTiles } from "../ProductSurfaceTiles";
import { WhatAgentSees } from "../WhatAgentSees";
import { WorksWhereYouWork } from "../WorksWhereYouWork";
import { Band } from "@/components/redesign-system/Band";
import { DEMOS } from "@/data/agents/demos";

import { AgentsCinematicRoot } from "./AgentsCinematicRoot";
import { PunchCardBackground } from "./PunchCardBackground";

// Cinematic variant of /products/nitro/agents. Renders the same component
// tree as the default variant (AgentsHero, AgentReframe, AgentsLoopDiagram,
// WhatAgentSees, two AgentDemos, ProductSurfaceTiles, GuardrailsSection,
// WorksWhereYouWork, AgentsPricingTeaser, AgentsFinalCta) with a single
// distinctive design idea layered behind everything: a faint mainframe
// punch-card geometric pattern (hollow Hollerith slots, a sparse pattern of
// amber-punched holes spelling "AGENT" / "MCP", column numbers along the
// top, row labels down the left, the 9-row dashed separator and a
// chamfered orientation notch in the top-left corner).
//
// VariantSwitcher is owned by the page-component dispatcher
// (`/page-components/nitro-agents.tsx`) so cinematic readers can hop back
// to the default variant from there.
export const NitroAgentsCinematic: FC = () => {
  return (
    <AgentsCinematicRoot>
      <PunchCardBackground />
      <AgentsHero />
      <AgentReframe />
      <AgentsLoopDiagram />
      <WhatAgentSees />

      {/* Section 05: two-demo proof block, inverted band ("lab" beat).
          Anchor `proof` is the target of the hero ghost CTA so the
          visitor lands directly on the load-bearing section. */}
      <Band variant="inverted" id="proof" ariaLabel="Proof">
        <div className="cc-ag-band-inner">
          <div className="cc-section-label">
            <span className="num">05</span> Proof
          </div>
          <div className="cc-ag-feature-header">
            <div className="eyebrow">Proof</div>
            <h2 className="display">
              Diagnose. Compose. Two loops, one agent.
            </h2>
            <p>
              Two prompts, two complete loops. Demo A descends into causes
              (Observe + Reason). Demo B fans out across the four surfaces the
              agent has to register against (Act + Compose + Ship). The
              transcripts are real-shape: the tool calls match what Nitro emits
              today.
            </p>
          </div>
          <div className="cc-ag-demos">
            {DEMOS.map((demo) => (
              <AgentDemo key={demo.key} demo={demo} />
            ))}
          </div>
        </div>
      </Band>

      <ProductSurfaceTiles />
      <GuardrailsSection />
      <WorksWhereYouWork />
      <AgentsPricingTeaser />
      <AgentsFinalCta />
    </AgentsCinematicRoot>
  );
};
