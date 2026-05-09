"use client";

import React, { FC, useEffect } from "react";

import { AgentDemo } from "@/components/agents/AgentDemo";
import { AgentReframe } from "@/components/agents/AgentReframe";
import { AgentsFinalCta } from "@/components/agents/AgentsFinalCta";
import { AgentsHero } from "@/components/agents/AgentsHero";
import { AgentsLoopDiagram } from "@/components/agents/AgentsLoopDiagram";
import { AgentsPricingTeaser } from "@/components/agents/AgentsPricingTeaser";
import { AgentsRoot } from "@/components/agents/AgentsRoot";
import { GuardrailsSection } from "@/components/agents/GuardrailsSection";
import { ProductSurfaceTiles } from "@/components/agents/ProductSurfaceTiles";
import { WhatAgentSees } from "@/components/agents/WhatAgentSees";
import { WorksWhereYouWork } from "@/components/agents/WorksWhereYouWork";
import { LandingGlobalStyle } from "@/components/landing/LandingRoot";
import { SiteLayout } from "@/components/layout";
import { SEO } from "@/components/misc";
import { DEMOS } from "@/data/agents/demos";

const NitroAgentsPage: FC = () => {
  useEffect(() => {
    document.body.classList.add("cc-landing-body");
    return () => {
      document.body.classList.remove("cc-landing-body");
    };
  }, []);

  return (
    <SiteLayout disableStars>
      <SEO
        title="Agents"
        description="The agent that already knows your platform. Hot Chocolate, Mocha, Fusion, and Strawberry Shake feed Nitro a live map of your federation, then expose it over MCP. Your agent stops guessing."
      />
      <LandingGlobalStyle />
      <AgentsRoot>
        <AgentsHero />
        <AgentReframe />
        <AgentsLoopDiagram />
        <WhatAgentSees />

        {/* Section 05: two-demo proof block. Anchor `proof` is the target of
            the hero ghost CTA so the visitor lands directly on the load-bearing
            section. */}
        <section className="cc-ag-section cc-ag-feature" id="proof">
          <div className="cc-section-label">
            <span className="num">05</span> Proof
          </div>
          <div className="cc-ag-feature-inner cc-ag-feature-elevated">
            <div className="cc-ag-feature-header">
              <div className="eyebrow">Proof</div>
              <h2 className="display">Two prompts. Two complete loops.</h2>
              <p>
                Investigation and operation, both end-to-end. Same MCP surface,
                same guardrails. The transcripts are real-shape: the tool calls
                match what Nitro emits today.
              </p>
            </div>
            <div className="cc-ag-demos">
              {DEMOS.map((demo) => (
                <AgentDemo key={demo.key} demo={demo} />
              ))}
            </div>
          </div>
        </section>

        <ProductSurfaceTiles />
        <GuardrailsSection />
        <WorksWhereYouWork />
        <AgentsPricingTeaser />
        <AgentsFinalCta />
      </AgentsRoot>
    </SiteLayout>
  );
};

export default NitroAgentsPage;
