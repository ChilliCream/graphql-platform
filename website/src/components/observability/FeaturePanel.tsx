"use client";

import React, { FC, ReactNode } from "react";

import { PlanChipRow, PlanChipVariant } from "./PlanChip";

// Reusable section frame for sections 02, 03, 04, 05, 07 and 08. Mirrors the
// visual grammar of Act3's `cc-tab-panel-d` (eyebrow, headline, sub, viz
// slot) but flow-laid-out instead of absolute-positioned, and with the plan
// chips inlined under the H2 the way Vercel does it.

interface FeaturePanelProps {
  readonly id?: string;
  readonly sectionNumber: string;
  readonly sectionLabel: string;
  readonly eyebrow: string;
  readonly headline: ReactNode;
  readonly sub: string;
  readonly chips: readonly PlanChipVariant[];
  readonly children: ReactNode;
  readonly elevated?: boolean;
}

export const FeaturePanel: FC<FeaturePanelProps> = ({
  id,
  sectionNumber,
  sectionLabel,
  eyebrow,
  headline,
  sub,
  chips,
  children,
  elevated = false,
}) => {
  return (
    <section id={id} className="cc-obs-section cc-obs-feature">
      <div className="cc-section-label">
        <span className="num">{sectionNumber}</span> {sectionLabel}
      </div>
      <div
        className={
          "cc-obs-feature-inner" + (elevated ? " cc-obs-feature-elevated" : "")
        }
      >
        <div className="cc-obs-feature-header">
          <div className="eyebrow">{eyebrow}</div>
          <h2 className="display">{headline}</h2>
          <PlanChipRow variants={chips} />
          <p>{sub}</p>
        </div>
        <div className="cc-obs-feature-viz">{children}</div>
      </div>
    </section>
  );
};
