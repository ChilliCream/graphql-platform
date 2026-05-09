"use client";

import React, { FC } from "react";
import styled from "styled-components";

import { GridSection } from "@/components/redesign-system/grid";

import { AgentTerminal } from "../AgentTerminal";

// Section 01 (Grid): hero. Asymmetric 60-40 split inside a single GridSection
// band. Left column carries the eyebrow + h1 + sub + 2 CTAs; right column
// hosts the auto-cycling AgentTerminal. The terminal sits naked inside its
// cell, no card chrome, just the inherited 1px hairline of the split. The
// spec calls for a strict square frame on the terminal which AgentsGridRoot
// already enforces by overriding the .cc-term radius and shadow.

const Hero = styled.div`
  display: grid;
  grid-template-columns: 3fr 2fr;
  align-items: stretch;
  border: 1px solid var(--cc-grid-hairline);
  margin-top: 32px;

  > * {
    padding: clamp(40px, 5vw, 72px);
    display: flex;
    flex-direction: column;
    justify-content: center;
  }
  > * + * {
    border-left: 1px solid var(--cc-grid-hairline);
  }

  @media (max-width: 880px) {
    grid-template-columns: 1fr;
    > * + * {
      border-left: 0;
      border-top: 1px solid var(--cc-grid-hairline);
    }
  }
`;

const Copy = styled.div`
  .eyebrow {
    color: var(--cc-accent, var(--cc-amber));
    margin-bottom: 18px;
  }
  h1 {
    font-family: var(--cc-font-sans), sans-serif;
    font-size: clamp(40px, 5.5vw, 72px);
    font-weight: 600;
    letter-spacing: -0.02em;
    line-height: 1.05;
    margin: 0 0 24px;
  }
  h1 .accent {
    color: var(--cc-accent, var(--cc-amber));
    background: none;
    -webkit-text-fill-color: currentColor;
  }
  p {
    font-size: 16px;
    line-height: 1.55;
    color: var(--cc-ink-dim);
    margin: 0 0 32px;
    max-width: 52ch;
  }
`;

const TerminalCell = styled.div`
  /* Override the inherited padding so the terminal can fill the cell edge
     to edge (the spec wants the terminal naked, square, 1px border). The
     border around the terminal comes from its own .cc-term rule as
     overridden in AgentsGridRoot. */
  padding: 0 !important;

  .cc-term {
    border: 0;
    height: 100%;
  }
`;

const Cta = styled.div`
  display: flex;
  gap: 12px;
  flex-wrap: wrap;
`;

export const AgentsGridHero: FC = () => {
  return (
    <GridSection variant="default" hairlineBottom>
      <Hero>
        <Copy>
          <div className="eyebrow">Nitro / Agents</div>
          <h1>
            The agent that already knows your{" "}
            <span className="accent">platform.</span>
          </h1>
          <p>
            Hot Chocolate, Mocha, Fusion, and Strawberry Shake feed Nitro a live
            map of your federation: schema, traces, code, topology. Then we
            expose it over MCP. Your agent stops guessing.
          </p>
          <Cta>
            <a href="/pricing" className="cc-btn cc-btn-primary">
              Try the MCP server
            </a>
            <a href="#proof" className="cc-btn cc-btn-ghost">
              See it in a real incident
            </a>
          </Cta>
        </Copy>
        <TerminalCell>
          <AgentTerminal session="nitro mcp · session 7c3a · cart-ops" />
        </TerminalCell>
      </Hero>
    </GridSection>
  );
};
