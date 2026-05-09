"use client";

import { useSearchParams } from "next/navigation";
import React, { FC, useEffect } from "react";

import { AgentDemo } from "@/components/agents/AgentDemo";
import { AgentReframe } from "@/components/agents/AgentReframe";
import { AgentsFinalCta } from "@/components/agents/AgentsFinalCta";
import { AgentsHero } from "@/components/agents/AgentsHero";
import { AgentsLoopDiagram } from "@/components/agents/AgentsLoopDiagram";
import { AgentsPricingTeaser } from "@/components/agents/AgentsPricingTeaser";
import { AgentsRoot } from "@/components/agents/AgentsRoot";
import { NitroAgentsCinematic } from "@/components/agents/cinematic";
import { NitroAgentsGrid } from "@/components/agents/grid";
import { GuardrailsSection } from "@/components/agents/GuardrailsSection";
import { ProductSurfaceTiles } from "@/components/agents/ProductSurfaceTiles";
import { WhatAgentSees } from "@/components/agents/WhatAgentSees";
import { WorksWhereYouWork } from "@/components/agents/WorksWhereYouWork";
import { LandingGlobalStyle } from "@/components/landing/LandingRoot";
import { SiteLayout } from "@/components/layout";
import { SEO } from "@/components/misc";
import { AccentThread } from "@/components/redesign-system/AccentThread";
import { Band } from "@/components/redesign-system/Band";
import { VariantSwitcher } from "@/components/redesign-system/cinematic";
import { DEMOS } from "@/data/agents/demos";

// Page band rhythm:
//   01 Hero            default                hero terminal bleeds right
//   02 Reframe         tinted                 cream break, no card chrome
//   03 The Loop        accent (full-bleed)    amber wash, the page's USP
//   04 Six surfaces    default                inventory grid, no card chrome
//   05 Demos           inverted               "lab" beat, two shape-distinct demos
//   06 Product rows    tinted                 stacked rows, no card chrome
//   07 Guardrails      default                ONLY section that keeps card chrome
//   08 Distribution    tinted                 IDE chip strip, no cards
//   09 Pricing teaser  accent                 narrow band, single CTA
//   10 Final CTA       glow                   close, two buttons
//
// Wrapped in <AccentThread page="agents"> so amber resolves through the
// foundation. The legacy --cc-amber alias inside AgentsRoot remains so any
// existing CSS that references it directly keeps working.
//
// Variant dispatch: `?v=cinematic` renders the cinematic variant which
// threads homepage chrome (ActLabel, ConnectorLine, FrostedExplainer)
// through the same band rhythm. `?v=grid` renders the Grid variant, a
// strict 1px-hairline Vercel-derived recomposition of the same content
// tree. Default render is unchanged.

type AgentsVariantId = "default" | "cinematic" | "grid";

const VARIANT_OPTIONS = [
  { id: "default", label: "Default", href: "/products/nitro/agents" },
  {
    id: "cinematic",
    label: "Cinematic",
    href: "/products/nitro/agents?v=cinematic",
  },
  { id: "grid", label: "Grid", href: "/products/nitro/agents?v=grid" },
];

const resolveVariant = (raw: string | null | undefined): AgentsVariantId => {
  if (raw === "cinematic" || raw === "grid") {
    return raw;
  }
  return "default";
};

const NitroAgentsPage: FC = () => {
  const searchParams = useSearchParams();
  const variant = resolveVariant(searchParams?.get("v"));

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
      <AccentThread page="agents">
        {variant === "cinematic" ? (
          <NitroAgentsCinematic />
        ) : variant === "grid" ? (
          <NitroAgentsGrid />
        ) : (
          <AgentsRoot>
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
                    (Observe + Reason). Demo B fans out across the four surfaces
                    the agent has to register against (Act + Compose + Ship).
                    The transcripts are real-shape: the tool calls match what
                    Nitro emits today.
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
          </AgentsRoot>
        )}
      </AccentThread>
      <VariantSwitcher currentId={variant} options={VARIANT_OPTIONS} />
    </SiteLayout>
  );
};

export default NitroAgentsPage;
