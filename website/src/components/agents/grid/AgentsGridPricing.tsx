"use client";

import React, { FC } from "react";
import styled from "styled-components";

import { GridSection } from "@/components/redesign-system/grid";

// Section 09 (Grid): pricing teaser. A narrow GridSection with one
// asymmetric row: copy left, a single ghost CTA right. No card chrome and
// no background tint, the section's hairlines on top and bottom carry the
// frame. The CTA picks up the amber accent on hover, the same "agent gets
// paid here" signal the default variant uses.

const Inner = styled.div`
  display: grid;
  grid-template-columns: minmax(0, 1.4fr) auto;
  gap: 40px;
  align-items: center;

  @media (max-width: 880px) {
    grid-template-columns: 1fr;
  }

  .eyebrow {
    color: var(--cc-accent, var(--cc-amber));
    margin-bottom: 12px;
  }
  h2 {
    font-family: var(--cc-font-sans), sans-serif;
    font-size: clamp(26px, 3vw, 36px);
    font-weight: 600;
    letter-spacing: -0.015em;
    line-height: 1.15;
    margin: 0 0 12px;
    max-width: 28ch;
  }
  p {
    font-size: 15px;
    line-height: 1.55;
    color: var(--cc-ink-dim);
    margin: 0;
    max-width: 56ch;
  }
`;

const Cta = styled.a`
  display: inline-flex;
  align-items: center;
  gap: 10px;
  padding: 14px 24px;
  border: 1px solid var(--cc-grid-hairline-strong);
  background: transparent;
  color: var(--cc-ink);
  font-family: var(--cc-font-sans), sans-serif;
  font-size: 14px;
  font-weight: 500;
  text-decoration: none;
  transition: border-color 0.12s ease, background 0.12s ease, color 0.12s ease;

  &:hover {
    border-color: var(--cc-amber-line);
    background: var(--cc-amber-soft);
    color: var(--cc-amber);
  }
`;

export const AgentsGridPricing: FC = () => {
  return (
    <GridSection variant="default" hairlineTop hairlineBottom>
      <Inner>
        <div>
          <div className="eyebrow">Billing</div>
          <h2>Nitro is the paid surface.</h2>
          <p>
            Hot Chocolate, Mocha, Strawberry Shake, and Fusion are open source
            and feed it. The agent loop runs on the same plan you already pay
            for observability and federation.
          </p>
        </div>
        <Cta href="/pricing">See pricing →</Cta>
      </Inner>
    </GridSection>
  );
};
