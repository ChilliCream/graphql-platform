"use client";

import React, { FC } from "react";

import { Band } from "@/components/redesign-system/Band";
import { StatRow, type StatItem } from "@/components/redesign-system/StatRow";
import type { ProofMetric } from "@/data/solutions/types";

interface ProofStripProps {
  readonly metrics: readonly ProofMetric[];
}

// Section 02: the proof strip. Uses the shared StatRow primitive so the
// numbers carry the visual weight (display type, oversized) and the
// attribution is the quiet eyebrow above. No card chrome; the band is the
// container, the stats are the content.
export const ProofStrip: FC<ProofStripProps> = ({ metrics }) => {
  const items: StatItem[] = metrics.map((m) => ({
    value: m.value,
    label: m.outcome,
    attribution: m.customer.toUpperCase(),
  }));

  return (
    <Band variant="default" ariaLabel="Proof">
      <div className="cc-sl-section cc-sl-proof">
        <div className="cc-section-label">
          <span className="num">02</span> Proof
        </div>
        <StatRow items={items} align="left" />
      </div>
    </Band>
  );
};
