"use client";

import React, { FC } from "react";

import { Band } from "@/components/redesign-system/Band";
import { StatRow, StatItem } from "@/components/redesign-system/StatRow";
import { AGGREGATE_STATS, TRUST_AGGREGATE } from "@/data/customers/aggregates";

// Section 02 (post re-sequence): proof strip. Each stat is bound to a
// single customer (named or anonymous-but-specific), rendered through
// the shared StatRow primitive on a tinted band. No gradient — the
// numbers are evidence, not decoration. The trust line above the strip
// is the highest-credibility sentence on the page.
export const ByTheNumbersBand: FC = () => {
  const items: StatItem[] = AGGREGATE_STATS.map((stat) => ({
    value: stat.value,
    label: stat.label,
    attribution: stat.attribution,
  }));

  return (
    <Band variant="tinted" ariaLabel="Proof strip">
      <div className="cc-section-label">
        <span className="num">02</span> Proof
      </div>
      <div className="cc-cu-proof-inner">
        <p className="cc-cu-proof-trustline" aria-label="Customer reach">
          {TRUST_AGGREGATE}
        </p>
        <StatRow items={items} align="left" />
      </div>
    </Band>
  );
};
