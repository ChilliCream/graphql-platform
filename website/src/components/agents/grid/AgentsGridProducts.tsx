"use client";

import React, { FC } from "react";
import styled from "styled-components";

import {
  GridCard,
  GridRow,
  GridSection,
} from "@/components/redesign-system/grid";
import {
  PRODUCT_SURFACES,
  ProductSurfaceIcon,
} from "@/data/agents/product-surfaces";

// Section 06 (Grid): the six pieces that feed the agent. Two rows of three
// GridCards, sharing hairlines. Each card is icon + tag + name + body. The
// icons are reproduced inline rather than imported from ProductSurfaceTiles
// because that file's icon palette ships next to a list-of-rows layout, not
// a card grid; importing only the icon record would fight the bundler over
// a default export shape that doesn't exist.

const STROKE = {
  fill: "none" as const,
  stroke: "currentColor",
  strokeWidth: 1.6,
  strokeLinecap: "round" as const,
  strokeLinejoin: "round" as const,
};

const ICONS: Record<ProductSurfaceIcon, () => React.ReactElement> = {
  mcp: () => (
    <svg viewBox="0 0 28 28" width="28" height="28" aria-hidden>
      <g {...STROKE}>
        <rect x="4" y="9" width="11" height="10" />
        <line x1="15" y1="14" x2="22" y2="14" />
        <line x1="22" y1="11" x2="22" y2="17" />
        <line x1="7" y1="9" x2="7" y2="6" />
        <line x1="12" y1="9" x2="12" y2="6" />
        <circle cx="22" cy="14" r="2" fill="currentColor" stroke="none" />
      </g>
    </svg>
  ),
  hotchocolate: () => (
    <svg viewBox="0 0 28 28" width="28" height="28" aria-hidden>
      <g {...STROKE}>
        <path d="M7 11 L7 20 Q7 23 10 23 L18 23 Q21 23 21 20 L21 11 Z" />
        <path d="M21 13 Q24 13 24 16 Q24 19 21 19" />
        <path d="M11 4 Q12 6 11 8" />
        <path d="M14 3 Q15 5 14 7" />
        <path d="M17 4 Q18 6 17 8" />
      </g>
    </svg>
  ),
  mocha: () => (
    <svg viewBox="0 0 28 28" width="28" height="28" aria-hidden>
      <g {...STROKE}>
        <ellipse cx="14" cy="14" rx="8" ry="10" transform="rotate(20 14 14)" />
        <path d="M11 6 Q14 14 11 22" transform="rotate(20 14 14)" />
      </g>
    </svg>
  ),
  fusion: () => (
    <svg viewBox="0 0 28 28" width="28" height="28" aria-hidden>
      <g {...STROKE}>
        <circle cx="6" cy="7" r="2" />
        <circle cx="22" cy="7" r="2" />
        <circle cx="6" cy="21" r="2" />
        <circle cx="22" cy="21" r="2" />
        <circle cx="14" cy="14" r="2" fill="currentColor" stroke="none" />
        <line x1="8" y1="8" x2="12" y2="13" />
        <line x1="20" y1="8" x2="16" y2="13" />
        <line x1="8" y1="20" x2="12" y2="15" />
        <line x1="20" y1="20" x2="16" y2="15" />
      </g>
    </svg>
  ),
  shake: () => (
    <svg viewBox="0 0 28 28" width="28" height="28" aria-hidden>
      <g {...STROKE}>
        <path d="M9 7 L19 7 L17 23 Q17 25 14 25 Q11 25 11 23 Z" />
        <line x1="9" y1="11" x2="19" y2="11" />
        <line x1="14" y1="3" x2="14" y2="9" />
        <path d="M14 3 L17 5" />
      </g>
    </svg>
  ),
  tracing: () => (
    <svg viewBox="0 0 28 28" width="28" height="28" aria-hidden>
      <g {...STROKE}>
        <rect x="4" y="6" width="20" height="3" />
        <rect x="6" y="11" width="14" height="3" />
        <rect x="9" y="16" width="14" height="3" />
        <rect x="6" y="21" width="9" height="3" />
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

const Tile = styled.div`
  display: flex;
  flex-direction: column;
  gap: 12px;
  min-height: 200px;

  .icon {
    color: var(--cc-amber);
    margin-bottom: 4px;
  }
  .tag {
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.16em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
  }
  .name {
    font-family: var(--cc-font-sans), sans-serif;
    font-size: 20px;
    font-weight: 600;
    letter-spacing: -0.01em;
    line-height: 1.3;
    color: var(--cc-ink);
  }
  .body {
    font-size: 14px;
    line-height: 1.55;
    color: var(--cc-ink-dim);
  }
`;

export const AgentsGridProducts: FC = () => {
  return (
    <GridSection variant="default">
      <Header>
        <div className="eyebrow">Product surfaces</div>
        <h2>The six pieces that feed the agent.</h2>
        <p>
          Each primitive earns its place in the loop. The MCP server is the
          endpoint; the rest are the ChilliCream products you already run,
          instrumented for an audience that isn't human.
        </p>
      </Header>

      <GridRow cols={3}>
        {PRODUCT_SURFACES.map((surface) => {
          const Icon = ICONS[surface.key];
          return (
            <GridCard key={surface.key}>
              <Tile>
                <span className="icon" aria-hidden>
                  <Icon />
                </span>
                <span className="tag">{surface.tag}</span>
                <span className="name">{surface.title}</span>
                <span className="body">{surface.body}</span>
              </Tile>
            </GridCard>
          );
        })}
      </GridRow>
    </GridSection>
  );
};
