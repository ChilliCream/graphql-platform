"use client";

import React, { FC } from "react";

import { Band } from "@/components/redesign-system/Band";
import { ActLabel } from "@/components/redesign-system/cinematic";

import { AgentTerminal } from "../AgentTerminal";

// Cinematic Section 01: hero. Identical to the default `AgentsHero` in
// content and layout; the only chrome difference is the `<ActLabel>`
// chapter marker that sits in the band gutter at top:36px instead of the
// inline `.cc-section-label` (which is hidden by `AgentsCinematicRoot`).

export const AgentsHeroCinematic: FC = () => {
  return (
    <Band variant="default" ariaLabel="Hero">
      <ActLabel n="01" name="Agents" />
      <div className="cc-ag-hero-inner">
        <div className="cc-ag-hero-copy">
          <div className="eyebrow">Nitro · Agents</div>
          <h1 className="display">
            The agent that already knows your{" "}
            <span className="accent">platform.</span>
          </h1>
          <p>
            Hot Chocolate, Mocha, Fusion, and Strawberry Shake feed Nitro a live
            map of your federation: schema, traces, code, topology. Then we
            expose it over MCP. Your agent stops guessing.
          </p>
          <div className="cc-ag-hero-cta">
            <a href="/pricing" className="cc-btn cc-btn-primary">
              Try the MCP server &rarr;
            </a>
            <a href="#proof" className="cc-btn cc-btn-ghost">
              See it in a real incident
            </a>
          </div>
        </div>

        <AgentTerminal session="nitro mcp · session 7c3a · cart-ops" />
      </div>
    </Band>
  );
};
