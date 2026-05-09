"use client";

import React, { FC } from "react";

import { Band } from "@/components/redesign-system/Band";

import { AgentTerminal } from "./AgentTerminal";

// Section 01: hero. Copy on the left, the auto-cycling AgentTerminal on the
// right. The H1 is "operator framing" (Headline Option A from the spec). The
// terminal is the highest-leverage visual on the page, so we let it dominate
// the right column with an amber-leaning gradient frame. Lives on the
// default band so the page background gradient bleeds through.

export const AgentsHero: FC = () => {
  return (
    <Band variant="default" ariaLabel="Hero">
      <div className="cc-ag-hero-inner">
        <div className="cc-section-label cc-ag-hero-label">
          <span className="num">01</span> Agents
        </div>
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
              Try the MCP server →
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
