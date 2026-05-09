"use client";

import React, { FC } from "react";
import styled from "styled-components";

import {
  GridCard,
  GridRow,
  GridSection,
} from "@/components/redesign-system/grid";
import { AGENT_SEES_TILES, AgentSeesKind } from "@/data/agents/agent-sees";

// Section 04 (Grid): six surfaces, one MCP endpoint. Rendered as a 3-up
// GridRow that wraps to 2 rows of 3 (= 6 cells), every cell sharing
// hairlines with its neighbors per the Vercel grid discipline. Each cell
// is a GridCard with eyebrow + headline + body + tiny inline visual.
//
// The visual register is "schematic line art on white" per spec section 5.
// We render a small stroke icon per surface key so the cells read as an
// inventory of queryable signals, not yet another dark card rack.

const STROKE = {
  fill: "none" as const,
  stroke: "currentColor",
  strokeWidth: 1.4,
  strokeLinecap: "round" as const,
  strokeLinejoin: "round" as const,
};

const ICONS: Record<AgentSeesKind, () => React.ReactElement> = {
  // Distributed traces: stacked waterfall bars descending in length.
  traces: () => (
    <svg viewBox="0 0 64 40" width="100%" height="40" aria-hidden>
      <g {...STROKE}>
        <line x1="2" y1="6" x2="62" y2="6" strokeOpacity="0.2" />
        <rect x="2" y="10" width="56" height="4" />
        <rect x="6" y="18" width="40" height="4" stroke="var(--cc-amber)" />
        <rect x="14" y="26" width="22" height="4" />
        <rect x="6" y="34" width="14" height="4" />
      </g>
    </svg>
  ),
  // Metrics: sparkline with a tail dot.
  metrics: () => (
    <svg viewBox="0 0 64 40" width="100%" height="40" aria-hidden>
      <g {...STROKE}>
        <path d="M 2 30 L 10 22 L 18 26 L 26 14 L 34 18 L 42 8 L 50 12 L 58 4" />
        <circle
          cx="58"
          cy="4"
          r="2.4"
          fill="var(--cc-amber)"
          stroke="var(--cc-amber)"
        />
        <line
          x1="2"
          y1="36"
          x2="62"
          y2="36"
          strokeOpacity="0.18"
          strokeDasharray="2 3"
        />
      </g>
    </svg>
  ),
  // Logs: rows of dashed text with a level pip.
  logs: () => (
    <svg viewBox="0 0 64 40" width="100%" height="40" aria-hidden>
      <g {...STROKE}>
        <circle cx="6" cy="8" r="1.6" fill="currentColor" />
        <line x1="12" y1="8" x2="50" y2="8" strokeDasharray="3 3" />
        <circle
          cx="6"
          cy="18"
          r="1.6"
          fill="var(--cc-amber)"
          stroke="var(--cc-amber)"
        />
        <line x1="12" y1="18" x2="58" y2="18" strokeDasharray="3 3" />
        <circle cx="6" cy="28" r="1.6" fill="currentColor" />
        <line x1="12" y1="28" x2="46" y2="28" strokeDasharray="3 3" />
        <circle cx="6" cy="38" r="1.6" fill="currentColor" />
        <line x1="12" y1="38" x2="40" y2="38" strokeDasharray="3 3" />
      </g>
    </svg>
  ),
  // Messaging: pub-sub pipeline.
  messaging: () => (
    <svg viewBox="0 0 64 40" width="100%" height="40" aria-hidden>
      <g {...STROKE}>
        <circle cx="6" cy="20" r="3" />
        <line x1="9" y1="20" x2="26" y2="20" />
        <rect x="26" y="14" width="14" height="12" />
        <line x1="40" y1="20" x2="55" y2="20" />
        <circle cx="58" cy="14" r="2.5" />
        <circle cx="58" cy="26" r="2.5" />
      </g>
    </svg>
  ),
  // API graph: type → field → resolver flow.
  graph: () => (
    <svg viewBox="0 0 64 40" width="100%" height="40" aria-hidden>
      <g {...STROKE}>
        <rect x="2" y="14" width="14" height="12" />
        <line x1="16" y1="20" x2="26" y2="20" />
        <rect x="26" y="14" width="14" height="12" stroke="var(--cc-amber)" />
        <line x1="40" y1="20" x2="50" y2="20" />
        <rect x="50" y="14" width="12" height="12" />
      </g>
    </svg>
  ),
  // Code: stylized braces with a check.
  code: () => (
    <svg viewBox="0 0 64 40" width="100%" height="40" aria-hidden>
      <g {...STROKE}>
        <path d="M 14 8 Q 6 8 6 16 L 6 18 Q 6 20 4 20 Q 6 20 6 22 L 6 24 Q 6 32 14 32" />
        <path d="M 50 8 Q 58 8 58 16 L 58 18 Q 58 20 60 20 Q 58 20 58 22 L 58 24 Q 58 32 50 32" />
        <path
          d="M 22 20 L 28 26 L 40 14"
          stroke="var(--cc-amber)"
          strokeWidth="1.8"
        />
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
    max-width: 22ch;
  }
  p {
    font-size: 15px;
    line-height: 1.55;
    color: var(--cc-ink-dim);
    margin: 0;
    max-width: 60ch;
  }
`;

const TileBody = styled.div`
  display: flex;
  flex-direction: column;
  gap: 14px;
  min-height: 220px;

  .eyebrow {
    color: var(--cc-ink-dim);
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.16em;
    text-transform: uppercase;
  }
  h3 {
    font-family: var(--cc-font-sans), sans-serif;
    font-size: 20px;
    font-weight: 600;
    letter-spacing: -0.01em;
    line-height: 1.3;
    margin: 0;
    color: var(--cc-ink);
  }
  p {
    font-size: 14px;
    line-height: 1.55;
    color: var(--cc-ink-dim);
    margin: 0;
  }
`;

const VizFrame = styled.div`
  margin-top: auto;
  padding: 16px;
  border: 1px solid var(--cc-grid-hairline);
  color: var(--cc-ink);
  display: flex;
  align-items: center;
  justify-content: center;
`;

export const AgentsGridSurfaces: FC = () => {
  return (
    <GridSection variant="default">
      <Header>
        <div className="eyebrow">One schema-typed surface</div>
        <h2>Six surfaces. One MCP endpoint.</h2>
        <p>
          Every signal a senior engineer would chase, distributed traces,
          metrics, logs, messaging topology, the API graph, and the source code
          itself, queryable from one schema-typed place.
        </p>
      </Header>

      <GridRow cols={3}>
        {AGENT_SEES_TILES.map((tile) => {
          const Icon = ICONS[tile.key];
          return (
            <GridCard key={tile.key}>
              <TileBody>
                <div className="eyebrow">{tile.eyebrow}</div>
                <h3>{tile.title}</h3>
                <p>{tile.body}</p>
                <VizFrame>
                  <Icon />
                </VizFrame>
              </TileBody>
            </GridCard>
          );
        })}
      </GridRow>
    </GridSection>
  );
};
