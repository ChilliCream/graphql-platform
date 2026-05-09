"use client";

import React, { FC } from "react";

// Plan chip pattern lifted from Vercel's observability page: a small monospace
// pill that sits inline with the section H2 to communicate which plan ships
// the feature, without breaking the panel's scroll rhythm. We never use this
// chip to gate visually, only to inform.

export type PlanChipVariant = "all" | "nitro" | "oss" | "fusion" | "enterprise";

const LABELS: Record<PlanChipVariant, string> = {
  all: "All plans",
  nitro: "Nitro",
  oss: "OSS-compatible",
  fusion: "Fusion",
  enterprise: "Enterprise",
};

interface PlanChipProps {
  readonly variant: PlanChipVariant;
}

export const PlanChip: FC<PlanChipProps> = ({ variant }) => (
  <span className={`cc-plan-chip is-${variant}`}>
    <span className="cc-plan-chip-dot" aria-hidden />
    {LABELS[variant]}
  </span>
);

interface PlanChipRowProps {
  readonly variants: readonly PlanChipVariant[];
}

export const PlanChipRow: FC<PlanChipRowProps> = ({ variants }) => (
  <div className="cc-plan-chip-row">
    {variants.map((v) => (
      <PlanChip key={v} variant={v} />
    ))}
  </div>
);
