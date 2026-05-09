"use client";

import React, { FC } from "react";
import styled from "styled-components";

import {
  GridCard,
  GridRow,
  GridSection,
} from "@/components/redesign-system/grid";
import { IDE_CLIENTS } from "@/data/agents/ide-clients";

// Section 08 (Grid): distribution. 4-up GridRow of IDE-client tiles. Each
// tile is monogram + name + Add MCP arrow. The amber arrow is the system
// signal: "the agent installs here." Cards share hairlines per Vercel grid
// discipline; the card itself is a clickable anchor so the whole cell
// reads as one tile.

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
  align-items: flex-start;
  gap: 14px;
  min-height: 180px;

  .mono {
    width: 44px;
    height: 44px;
    border: 1px solid var(--cc-grid-hairline);
    color: var(--cc-ink);
    display: inline-flex;
    align-items: center;
    justify-content: center;
    font-family: var(--cc-font-sans), sans-serif;
    font-size: 18px;
    font-weight: 600;
  }
  .name {
    font-family: var(--cc-font-sans), sans-serif;
    font-size: 18px;
    font-weight: 600;
    letter-spacing: -0.01em;
    color: var(--cc-ink);
  }
  .cta {
    margin-top: auto;
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.16em;
    text-transform: uppercase;
    color: var(--cc-amber);
  }
`;

const TileLink = styled.a`
  display: block;
  height: 100%;
  text-decoration: none;
  color: inherit;
`;

export const AgentsGridDistribution: FC = () => {
  return (
    <GridSection variant="default">
      <Header>
        <div className="eyebrow">Distribution</div>
        <h2>It's not another chat window.</h2>
        <p>
          It's the same chat window, suddenly aware of your platform. The Nitro
          MCP server slots into the agent clients your team already uses, with
          one config line.
        </p>
      </Header>

      <GridRow cols={4}>
        {IDE_CLIENTS.map((c) => (
          <GridCard key={c.key}>
            <TileLink href={c.setup}>
              <Tile>
                <span className="mono" aria-hidden>
                  {c.letter}
                </span>
                <span className="name">{c.name}</span>
                <span className="cta">Add MCP →</span>
              </Tile>
            </TileLink>
          </GridCard>
        ))}
      </GridRow>
    </GridSection>
  );
};
