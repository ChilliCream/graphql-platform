"use client";

import React, { FC } from "react";
import styled from "styled-components";

import { DashboardComposite } from "@/components/redesign-system/illustrations";
import { GridButton } from "@/components/redesign-system/grid";

// Hero: text left, large dashboard composite right. No card frame around the
// composite, the dashboard bleeds off the right edge of the section so the
// Vercel-observability "the dashboard IS the page top" feeling reads. The
// section closes with a 1px hairline that the next section reuses as its top.

const Outer = styled.div`
  display: grid;
  grid-template-columns: minmax(0, 0.95fr) minmax(0, 1.25fr);
  gap: 48px;
  align-items: center;
  min-height: clamp(480px, 60vh, 640px);

  @media (max-width: 980px) {
    grid-template-columns: 1fr;
    gap: 40px;
    min-height: 0;
  }
`;

const Copy = styled.div`
  display: flex;
  flex-direction: column;
  align-items: flex-start;
`;

const CtaRow = styled.div`
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
  margin-top: 32px;
`;

const Composite = styled.div`
  position: relative;
  width: calc(100% + var(--cc-pad-x, clamp(24px, 5vw, 64px)));
  margin-right: calc(-1 * var(--cc-pad-x, clamp(24px, 5vw, 64px)));
  color: var(--cc-accent, var(--cc-col-shi));

  svg {
    width: 100%;
    height: auto;
    display: block;
  }

  @media (max-width: 980px) {
    width: 100%;
    margin-right: 0;
  }
`;

export const ObservabilityGridHero: FC = () => {
  return (
    <Outer aria-label="Observability hero">
      <Copy>
        <span className="cc-grid-eyebrow">Nitro · Observability</span>
        <h1 className="cc-grid-display cc-grid-h1">
          Understand production from the inside out.
        </h1>
        <p
          className="cc-grid-body"
          style={{ fontSize: "clamp(15px, 1.2vw, 18px)" }}
        >
          One trace spans the gateway and every owning service. Federation-aware
          traces, query replay, schema diffs, and an MCP surface for agents,
          built into Nitro.
        </p>
        <CtaRow>
          <GridButton variant="primary" href="/pricing">
            Start free
          </GridButton>
          <GridButton
            variant="secondary"
            href="mailto:contact@chillicream.com?subject=Nitro%20demo"
          >
            Get a demo
          </GridButton>
        </CtaRow>
      </Copy>
      <Composite aria-hidden>
        <DashboardComposite
          panels={["trace", "chart", "log-stream"]}
          bleedDirection="right"
        />
      </Composite>
    </Outer>
  );
};
