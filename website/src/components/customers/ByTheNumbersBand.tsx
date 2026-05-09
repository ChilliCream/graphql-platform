"use client";

import React, { FC } from "react";

import { AGGREGATE_STATS, TRUST_AGGREGATE } from "@/data/customers/aggregates";

import { MetricStat } from "./MetricStat";

// Section 03: aggregate stats band. Compensates for the missing logos by
// surfacing the cumulative scale. Updated quarterly. Stats are dominant —
// 48-88px display type with the gradient accent.
export const ByTheNumbersBand: FC = () => {
  return (
    <section className="cc-cu-section cc-cu-numbers">
      <div className="cc-section-label">
        <span className="num">03</span> By the numbers
      </div>
      <div className="cc-cu-numbers-inner">
        <div className="cc-cu-numbers-heading">
          <div className="eyebrow">By the numbers</div>
          <h2>The shape of the customer base, in four numbers.</h2>
        </div>
        <div className="cc-cu-numbers-grid">
          {AGGREGATE_STATS.map((stat) => (
            <MetricStat
              key={stat.key}
              eyebrow={stat.eyebrow}
              value={stat.value}
              label={stat.label}
            />
          ))}
        </div>
        <p className="cc-cu-numbers-foot">{TRUST_AGGREGATE}</p>
      </div>
    </section>
  );
};
