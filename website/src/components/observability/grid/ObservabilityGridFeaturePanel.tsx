"use client";

import React, { FC, ReactNode } from "react";
import styled from "styled-components";

import {
  PlanChipRow,
  PlanChipVariant,
} from "@/components/observability/PlanChip";

// One feature row inside the Grid variant. Renders as a single GridSection-
// width split: text column (eyebrow, h2, body, plan chips, optional bullets)
// on the left, visual on the right. Visuals share the section hairline borders
// with the row above and below so the page reads as one continuous frame.
//
// `reverse` swaps text/visual sides for the alternating asymmetric rhythm.

interface ObservabilityGridFeaturePanelProps {
  readonly eyebrow: string;
  readonly headline: ReactNode;
  readonly sub: string;
  readonly chips: readonly PlanChipVariant[];
  readonly bullets?: readonly string[];
  readonly children: ReactNode;
  readonly reverse?: boolean;
}

interface SplitProps {
  $reverse: boolean;
}

const Split = styled.div<SplitProps>`
  display: grid;
  grid-template-columns: minmax(0, 0.9fr) minmax(0, 1.2fr);
  gap: 0;
  align-items: stretch;
  width: 100%;
  border-top: 1px solid var(--cc-grid-hairline);
  border-bottom: 1px solid var(--cc-grid-hairline);

  ${({ $reverse }) =>
    $reverse
      ? `
        > .cc-grid-feat-copy {
          order: 2;
          border-left: 1px solid var(--cc-grid-hairline);
        }
        > .cc-grid-feat-viz {
          order: 1;
        }
      `
      : `
        > .cc-grid-feat-viz {
          border-left: 1px solid var(--cc-grid-hairline);
        }
      `}

  @media (max-width: 980px) {
    grid-template-columns: 1fr;

    > .cc-grid-feat-copy,
    > .cc-grid-feat-viz {
      order: initial;
      border-left: 0;
    }
    > .cc-grid-feat-viz {
      border-top: 1px solid var(--cc-grid-hairline);
    }
  }
`;

const Copy = styled.div`
  padding: clamp(32px, 4vw, 56px);
  display: flex;
  flex-direction: column;
  justify-content: center;
`;

const Viz = styled.div`
  padding: clamp(24px, 3vw, 40px);
  display: flex;
  align-items: center;
  justify-content: center;
  background: rgba(255, 255, 255, 0.012);
  min-height: 320px;

  > * {
    width: 100%;
  }
`;

const Bullets = styled.ul`
  list-style: none;
  margin: 18px 0 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 10px;

  li {
    position: relative;
    padding-left: 18px;
    font-size: 14px;
    line-height: 1.45;
    color: var(--cc-ink-dim);
  }
  li::before {
    content: "";
    position: absolute;
    left: 0;
    top: 9px;
    width: 8px;
    height: 1px;
    background: var(--cc-accent, var(--cc-ink));
  }
`;

export const ObservabilityGridFeaturePanel: FC<
  ObservabilityGridFeaturePanelProps
> = ({ eyebrow, headline, sub, chips, bullets, children, reverse = false }) => {
  return (
    <Split $reverse={reverse}>
      <Copy className="cc-grid-feat-copy">
        <span className="cc-grid-eyebrow">{eyebrow}</span>
        <h2 className="cc-grid-display cc-grid-h2">{headline}</h2>
        <PlanChipRow variants={chips} />
        <p className="cc-grid-body">{sub}</p>
        {bullets && (
          <Bullets>
            {bullets.map((b) => (
              <li key={b}>{b}</li>
            ))}
          </Bullets>
        )}
      </Copy>
      <Viz className="cc-grid-feat-viz">{children}</Viz>
    </Split>
  );
};
