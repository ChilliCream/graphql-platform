"use client";

import React, { FC } from "react";

import type { ProofMetric } from "@/data/solutions/types";

interface ProofStripProps {
  readonly metrics: readonly ProofMetric[];
}

// Section 02: the four-card metric strip. Vercel's most replicable pattern
// and the only credibility band that lands above the fold-and-a-half.
// Every metric is attributed; no anonymous numbers ever. The renderer
// expects exactly four entries (the constraint is the feature) but won't
// blow up if the count drifts; the grid simply reflows.
export const ProofStrip: FC<ProofStripProps> = ({ metrics }) => (
  <section className="cc-sl-section cc-sl-proof">
    <div className="cc-section-label">
      <span className="num">02</span> Proof
    </div>
    <div className="cc-sl-proof-inner">
      {metrics.map((m, i) => (
        <div key={`${m.value}-${i}`} className="cc-sl-proof-card">
          <div className="cc-sl-proof-value display">{m.value}</div>
          <div className="cc-sl-proof-outcome">{m.outcome}</div>
          <div className="cc-sl-proof-customer">{m.customer}</div>
        </div>
      ))}
    </div>
  </section>
);
