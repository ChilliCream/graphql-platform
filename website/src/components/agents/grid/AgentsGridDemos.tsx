"use client";

import React, { FC } from "react";
import styled from "styled-components";

import { GridCard, GridSection } from "@/components/redesign-system/grid";
import { DEMOS } from "@/data/agents/demos";

import { AgentDemo } from "../AgentDemo";

// Section 05 (Grid): proof. Two demos. We render them as STACKED GridCards
// (each demo is dense and shape-distinct, side-by-side wins nothing here).
// The two demos sit on an inverted band so the lab beat reads against the
// flat dark surface above. Card chrome stays square, no internal padding
// override — AgentDemo brings its own panel structure.

const Header = styled.div`
  margin-bottom: 32px;

  .eyebrow {
    color: var(--cc-accent, var(--cc-amber));
    margin-bottom: 12px;
  }
  h2 {
    font-family: var(--cc-font-sans), sans-serif;
    font-size: clamp(28px, 3vw, 40px);
    font-weight: 600;
    letter-spacing: -0.015em;
    line-height: 1.15;
    margin: 0 0 14px;
    max-width: 28ch;
    color: var(--cc-ink);
  }
  p {
    font-size: 15px;
    line-height: 1.55;
    color: var(--cc-ink-dim);
    margin: 0;
    max-width: 60ch;
  }
`;

const Stack = styled.div`
  display: flex;
  flex-direction: column;
  gap: 0;
  border-top: 1px solid var(--cc-grid-hairline);
  border-left: 1px solid var(--cc-grid-hairline);

  > * {
    border: 0;
    border-right: 1px solid var(--cc-grid-hairline);
    border-bottom: 1px solid var(--cc-grid-hairline);
  }
`;

export const AgentsGridDemos: FC = () => {
  return (
    <GridSection variant="default" id="proof">
      <Header>
        <div className="eyebrow">Proof</div>
        <h2>Diagnose. Compose. Two loops, one agent.</h2>
        <p>
          Two prompts, two complete loops. Demo A descends into causes (Observe
          + Reason). Demo B fans out across the four surfaces the agent has to
          register against (Act + Compose + Ship). The transcripts are
          real-shape: the tool calls match what Nitro emits today.
        </p>
      </Header>

      <Stack>
        {DEMOS.map((demo) => (
          <GridCard key={demo.key}>
            <AgentDemo demo={demo} />
          </GridCard>
        ))}
      </Stack>
    </GridSection>
  );
};
