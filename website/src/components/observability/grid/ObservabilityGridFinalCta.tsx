"use client";

import React, { FC } from "react";
import styled from "styled-components";

import { GridButton } from "@/components/redesign-system/grid";

// Final CTA band: centered eyebrow + headline + 3-button row. Mirrors the
// Vercel observability final CTA archetype with primary, secondary, and a
// ghost link as the third element.

const Outer = styled.div`
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
  gap: 32px;
  padding: 16px 0;
`;

const Heading = styled.h2`
  font-family: var(--cc-font-sans), sans-serif;
  font-weight: 500;
  letter-spacing: -0.025em;
  line-height: 1.05;
  font-size: clamp(36px, 5vw, 64px);
  margin: 0;
  max-width: 18ch;
  color: var(--cc-ink);
`;

const Sub = styled.p`
  font-size: clamp(15px, 1.1vw, 17px);
  line-height: 1.55;
  color: var(--cc-ink-dim);
  margin: 0;
  max-width: 56ch;
`;

const CtaRow = styled.div`
  display: flex;
  flex-wrap: wrap;
  justify-content: center;
  gap: 12px;
  margin-top: 8px;
`;

export const ObservabilityGridFinalCta: FC = () => {
  return (
    <Outer>
      <span className="cc-grid-eyebrow">Ready when you are</span>
      <Heading>See the federation. Quietly.</Heading>
      <Sub>
        Start free in minutes, or talk to an engineer about your federation.
      </Sub>
      <CtaRow>
        <GridButton variant="primary" href="/pricing">
          Start free
        </GridButton>
        <GridButton
          variant="secondary"
          href="mailto:contact@chillicream.com?subject=Nitro%20observability"
        >
          Talk to an engineer
        </GridButton>
        <GridButton variant="ghost" href="/enterprise">
          Nitro for enterprise
        </GridButton>
      </CtaRow>
    </Outer>
  );
};
