"use client";

import React, { FC } from "react";
import styled from "styled-components";

import {
  GridCard,
  GridRow,
  GridSection,
} from "@/components/redesign-system/grid";
import { PILLARS, PillarKey } from "@/data/enterprise/pillars";

// 3-up benefit row (archetype D). Pillar cards mirror the
// `vercel-ai-gateway` y≈340 pattern: eyebrow + h3 headline + 1-line body in a
// square hairline-bordered cell. The pillar tagline from the data file is
// already a single line per the design brief.

const PILLAR_EYEBROW: Record<PillarKey, string> = {
  federate: "Federate",
  operate: "Operate",
  agents: "Adopt agents",
};

const PILLAR_HEADLINE: Record<PillarKey, string> = {
  federate: "Compose every backend, in any language.",
  operate: "Run Nitro on infra you control.",
  agents: "Make your platform legible to your agents.",
};

const PillarCell = styled.div`
  display: flex;
  flex-direction: column;
  gap: 14px;
  min-height: 280px;
`;

const PillarBody = styled.p`
  font-size: 14px;
  line-height: 1.55;
  color: var(--cc-ink-dim);
  margin: 0;
  text-wrap: pretty;
  flex: 1;
`;

const PillarArrow = styled.span`
  font-family: var(--cc-font-mono), monospace;
  font-size: 16px;
  color: var(--cc-accent, var(--cc-ink));
  margin-top: auto;
`;

export const EnterpriseGridPillars: FC = () => {
  return (
    <GridSection>
      <div className="cc-grid-section-head">
        <span className="cc-grid-eyebrow">Platform pillars</span>
        <h2 className="cc-grid-h2">
          One stack. Three jobs. No piecemeal vendors.
        </h2>
        <p>
          We don't ship a stitched-together suite. Federate, Operate, and Adopt
          agents are the three things a platform team needs to run GraphQL at
          scale, and they share one engine, one schema model, and one control
          plane.
        </p>
      </div>
      <GridRow cols={3}>
        {PILLARS.map((pillar) => (
          <GridCard key={pillar.key}>
            <PillarCell>
              <span className="cc-grid-eyebrow">
                {PILLAR_EYEBROW[pillar.key]}
              </span>
              <h3 className="cc-grid-h3">{PILLAR_HEADLINE[pillar.key]}</h3>
              <PillarBody>{pillar.tagline}</PillarBody>
              <PillarArrow aria-hidden="true">→</PillarArrow>
            </PillarCell>
          </GridCard>
        ))}
      </GridRow>
    </GridSection>
  );
};
