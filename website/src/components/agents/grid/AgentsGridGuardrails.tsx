"use client";

import React, { FC } from "react";
import styled from "styled-components";

import {
  GridCard,
  GridRow,
  GridSection,
} from "@/components/redesign-system/grid";
import { GUARDRAILS, GuardrailIcon } from "@/data/agents/guardrails";

// Section 07 (Grid): four guardrails. 4-up GridRow. Cards keep card chrome
// because the edge IS the constraint signal — this is the one place on the
// page where "the card is the boundary" makes literal sense. Each card
// pairs a stroke icon (in amber) with a serious one-liner.

const STROKE = {
  fill: "none" as const,
  stroke: "currentColor",
  strokeWidth: 1.6,
  strokeLinecap: "round" as const,
  strokeLinejoin: "round" as const,
};

const ICONS: Record<GuardrailIcon, () => React.ReactElement> = {
  schema: () => (
    <svg viewBox="0 0 24 24" width="22" height="22" aria-hidden>
      <g {...STROKE}>
        <path d="M9 5 Q5 5 5 9 L5 11 Q5 12 4 12 Q5 12 5 13 L5 15 Q5 19 9 19" />
        <path d="M15 5 Q19 5 19 9 L19 11 Q19 12 20 12 Q19 12 19 13 L19 15 Q19 19 15 19" />
        <path d="M9.5 12 L11.5 14 L14.5 10" />
      </g>
    </svg>
  ),
  token: () => (
    <svg viewBox="0 0 24 24" width="22" height="22" aria-hidden>
      <g {...STROKE}>
        <circle cx="9" cy="12" r="4" />
        <line x1="13" y1="12" x2="21" y2="12" />
        <line x1="17" y1="12" x2="17" y2="15" />
        <line x1="20" y1="12" x2="20" y2="14" />
      </g>
    </svg>
  ),
  audit: () => (
    <svg viewBox="0 0 24 24" width="22" height="22" aria-hidden>
      <g {...STROKE}>
        <line x1="6" y1="6" x2="18" y2="6" />
        <line x1="6" y1="10" x2="14" y2="10" />
        <line x1="6" y1="14" x2="18" y2="14" />
        <line x1="6" y1="18" x2="12" y2="18" />
        <circle cx="20" cy="6" r="1.6" fill="currentColor" stroke="none" />
        <circle cx="16" cy="10" r="1.6" fill="currentColor" stroke="none" />
      </g>
    </svg>
  ),
  sandbox: () => (
    <svg viewBox="0 0 24 24" width="22" height="22" aria-hidden>
      <g {...STROKE}>
        <rect x="4" y="6" width="16" height="14" />
        <path d="M9 6 L9 4 Q9 3 10 3 L14 3 Q15 3 15 4 L15 6" />
        <rect x="10" y="11" width="4" height="5" />
        <line x1="11" y1="13" x2="13" y2="13" />
      </g>
    </svg>
  ),
};

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
  }
  p {
    font-size: 15px;
    line-height: 1.55;
    color: var(--cc-ink-dim);
    margin: 0;
    max-width: 60ch;
  }
`;

const Body = styled.div`
  display: flex;
  flex-direction: column;
  gap: 12px;
  min-height: 180px;

  .icon {
    width: 36px;
    height: 36px;
    border: 1px solid var(--cc-amber-line);
    background: var(--cc-amber-soft);
    color: var(--cc-amber);
    display: inline-flex;
    align-items: center;
    justify-content: center;
    margin-bottom: 4px;
  }
  h4 {
    font-family: var(--cc-font-sans), sans-serif;
    font-size: 16px;
    font-weight: 600;
    letter-spacing: -0.01em;
    line-height: 1.3;
    margin: 0;
    color: var(--cc-ink);
  }
  p {
    font-size: 13.5px;
    line-height: 1.55;
    color: var(--cc-ink-dim);
    margin: 0;
  }
`;

export const AgentsGridGuardrails: FC = () => {
  return (
    <GridSection variant="default">
      <Header>
        <div className="eyebrow">Autonomy with a leash</div>
        <h2>Bounded by the schema, audited by Mocha.</h2>
        <p>
          Agents move fastest when the rails are real. Every Nitro MCP
          interaction is typed against the live federated schema, scoped to an
          identity, and replayable from the audit log.
        </p>
      </Header>

      <GridRow cols={4}>
        {GUARDRAILS.map((g) => {
          const Icon = ICONS[g.key];
          return (
            <GridCard key={g.key}>
              <Body>
                <span className="icon" aria-hidden>
                  <Icon />
                </span>
                <h4>{g.title}</h4>
                <p>{g.body}</p>
              </Body>
            </GridCard>
          );
        })}
      </GridRow>
    </GridSection>
  );
};
