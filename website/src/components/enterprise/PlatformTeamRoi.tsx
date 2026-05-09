"use client";

import React, { FC } from "react";

import { Band } from "@/components/redesign-system/Band";
import { StatRow, StatItem } from "@/components/redesign-system/StatRow";

// ROI numbers as a StatRow on a tinted band — NOT a rack of cards. The
// previous 3-card layout looked identical to the SKUs and pillars below;
// rendering the numbers as a hairline-separated stat strip earns trust by
// typography rather than chrome.

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

export const PlatformTeamRoi: FC = () => {
  return (
    <Band variant="tinted" ariaLabel="Customer outcomes">
      <div className="cc-section-label">
        <span className="num">05</span> Customer outcomes
      </div>
      <div className="cc-ent-roi-inner">
        <div className="cc-ent-heading">
          <div className="eyebrow">Customer outcomes</div>
          <h2 className="display">
            Real numbers from production platform teams.
          </h2>
          <p>
            We don't quote headline ROI percentages until we have a third-party
            study to back them up. Here's what we can publish now: the
            consolidation, the rollout time, and the latency move from teams
            running Fusion in production.
          </p>
        </div>
        <StatRow items={ROI_ITEMS} align="left" />
        <p className="cc-ent-roi-note">
          Each metric is approved for publication by the customer. Names and
          industry segments are anonymised; the numbers are not.
        </p>
      </div>
    </Band>
  );
};
