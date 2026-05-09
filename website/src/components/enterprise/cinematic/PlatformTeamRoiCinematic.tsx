"use client";

import React, { FC } from "react";
import styled from "styled-components";

import { Band } from "@/components/redesign-system/Band";
import { Anchor } from "@/components/redesign-system/cinematic";
import { StatRow, StatItem } from "@/components/redesign-system/StatRow";

// Cinematic variant of `PlatformTeamRoi`: identical content and rhythm,
// but drops a connector anchor inside the first stat ("47 → 1") so the
// page-level `<ConnectorLine>` can thread from the headline number through
// to the federation diagram center.

const ROI_ITEMS: StatItem[] = [
  {
    value: "47 → 1",
    label:
      "Hand-rolled BFFs consolidated into one Fusion mesh. One on-call rotation, one schema, one tracing story.",
    attribution: "TOP-5 EU RETAIL BANK",
  },
  {
    value: "9 wks",
    label:
      "End-to-end federation rollout for an 18-service FSI group, including audit sign-off and rollback drills.",
    attribution: "NORTH AMERICAN FSI GROUP",
  },
  {
    value: "480 → 90 ms",
    label:
      "P99 reduction after replacing a stack of hand-rolled BFFs with one Fusion gateway, measured over a single quarter.",
    attribution: "PLATFORM TEAM, RETAIL BANKING",
  },
];

// Position the anchor over the first StatRow cell. The connector primitive
// uses the center of the anchor's bounding box as the endpoint, so a 0x0
// marker placed in the first cell's gutter is enough.
const StatRowWrap = styled.div`
  position: relative;
`;

const FirstStatAnchor = styled.div`
  position: absolute;
  top: 50%;
  left: 14%;
  pointer-events: none;
`;

export const PlatformTeamRoiCinematic: FC = () => {
  return (
    <Band variant="tinted" ariaLabel="Customer outcomes">
      <div className="cc-ent-tint-scope">
        <div className="cc-section-label">
          <span className="num">02</span> Customer outcomes
        </div>
        <div className="cc-ent-roi-inner">
          <div className="cc-ent-heading">
            <div className="eyebrow">Customer outcomes</div>
            <h2 className="display">
              Real numbers from production platform teams.
            </h2>
            <p>
              We don't quote headline ROI percentages until we have a
              third-party study to back them up. Here's what we can publish now:
              the consolidation, the rollout time, and the latency move from
              teams running Fusion in production.
            </p>
          </div>
          <StatRowWrap>
            <FirstStatAnchor>
              <Anchor id="hero-stat-collapse" />
            </FirstStatAnchor>
            <StatRow items={ROI_ITEMS} align="left" />
          </StatRowWrap>
          <p className="cc-ent-roi-note">
            Each metric is approved for publication by the customer. Names and
            industry segments are anonymised; the numbers are not.
          </p>
        </div>
      </div>
    </Band>
  );
};
