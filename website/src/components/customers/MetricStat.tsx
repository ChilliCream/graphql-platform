"use client";

import React, { FC } from "react";

interface MetricStatProps {
  readonly eyebrow: string;
  readonly value: string;
  readonly label: string;
}

// Big-number tile used by the by-the-numbers band on the index. The value
// gets the gradient accent treatment shared across all section roots.
export const MetricStat: FC<MetricStatProps> = ({ eyebrow, value, label }) => (
  <div className="cc-cu-num-tile">
    <span className="cc-cu-num-eyebrow">{eyebrow}</span>
    <span className="cc-cu-num-value display">{value}</span>
    <span className="cc-cu-num-label">{label}</span>
  </div>
);
