"use client";

import React, { FC } from "react";
import styled from "styled-components";

import { GridSection, GridSplit } from "@/components/redesign-system/grid";

// Section 02 (Grid): reframe. 50-50 GridSplit. Left cell reads as "what
// every other AI dev tool does": muted ink, neutral chip styling. Right
// cell reads as "what ChilliCream does": full ink, amber-tinted column
// heading + chip border. The split component supplies the 1px center
// hairline; cells inherit it from GridSplit's collapsed-border rules.

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

const Cell = styled.div<{ $bright?: boolean }>`
  padding: clamp(32px, 4vw, 56px);
  display: flex;
  flex-direction: column;
  gap: 14px;
  background: ${({ $bright }) =>
    $bright ? "var(--cc-amber-soft)" : "transparent"};
  opacity: ${({ $bright }) => ($bright ? 1 : 0.62)};

  .eyebrow {
    color: ${({ $bright }) =>
      $bright ? "var(--cc-amber)" : "var(--cc-ink-dim)"};
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

const Bullets = styled.ul<{ $bright?: boolean }>`
  list-style: none;
  margin: 6px 0 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 6px;

  li {
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.14em;
    text-transform: uppercase;
    padding: 5px 9px;
    border-radius: 0;
    border: 1px solid
      ${({ $bright }) =>
        $bright ? "var(--cc-amber-line)" : "var(--cc-ink-faint)"};
    background: ${({ $bright }) =>
      $bright ? "var(--cc-amber-soft)" : "transparent"};
    color: ${({ $bright }) => ($bright ? "var(--cc-amber)" : "var(--cc-ink)")};
    width: fit-content;
  }
`;

export const AgentsGridReframe: FC = () => {
  return (
    <GridSection variant="default">
      <Header>
        <div className="eyebrow">Reframe</div>
        <h2>Code generation is downstream of system understanding.</h2>
        <p>
          Most AI for developers helps an agent write the next line. Nitro lets
          an agent operate the whole platform that line runs in.
        </p>
      </Header>

      <GridSplit ratio="50-50">
        <Cell>
          <div className="eyebrow">AI in your IDE</div>
          <h3>Auto-completes the next line.</h3>
          <p>
            Knows your repo's surface. Suggests imports. Refactors the file
            you're staring at. Stops at the file boundary.
          </p>
          <Bullets>
            <li>File-scoped</li>
            <li>Snippet-shaped</li>
            <li>Static repo view</li>
          </Bullets>
        </Cell>
        <Cell $bright>
          <div className="eyebrow">Agents on your platform</div>
          <h3>Operates the system.</h3>
          <p>
            Reads every trace. Walks every resolver. Diffs every schema. Knows
            which subgraph owns each field, and changes it safely across the
            federation.
          </p>
          <Bullets $bright>
            <li>Federation-wide</li>
            <li>Schema-typed</li>
            <li>Live runtime view</li>
          </Bullets>
        </Cell>
      </GridSplit>
    </GridSection>
  );
};
