"use client";

import React, { FC, ReactNode } from "react";

import { PlanChipRow, PlanChipVariant } from "./PlanChip";

// Section header used inside a Band. Replaces the original card-framed
// FeaturePanel: the band primitive provides the surface tier, this component
// just stacks the section number + eyebrow + headline + chips + sub copy and
// the viz slot. NO card outline, NO inner border, the band carries the rhythm.
//
// `layout` toggles the relationship between header and viz:
//   - "centered" stacks the header above a full-width viz row (default).
//   - "sidebar" puts the header on the left and the viz on the right with a
//     wider viz column (sidebar-copy + breakout pane primitive).
//   - "diptych" mirrors sidebar with copy on the right and viz on the left.

export type FeaturePanelLayout = "centered" | "sidebar" | "diptych";

interface FeaturePanelProps {
  readonly id?: string;
  readonly sectionNumber: string;
  readonly sectionLabel: string;
  readonly eyebrow: string;
  readonly headline: ReactNode;
  readonly sub: string;
  readonly chips: readonly PlanChipVariant[];
  readonly children: ReactNode;
  readonly layout?: FeaturePanelLayout;
  /** Render the viz with a 16px right overshoot so it bleeds the column. */
  readonly bleedRight?: boolean;
  /** Optional small bullet column on sidebar layouts. */
  readonly sidebarBullets?: readonly string[];
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
  layout = "centered",
  bleedRight = false,
  sidebarBullets,
}) => {
  const className =
    "cc-obs-panel" +
    (layout === "sidebar"
      ? " is-sidebar"
      : layout === "diptych"
      ? " is-diptych"
      : "");

  return (
    <section id={id} className={className}>
      <div className="cc-section-label">
        <span className="num">{sectionNumber}</span> {sectionLabel}
      </div>
      <div className="cc-obs-panel-header">
        <div className="eyebrow">{eyebrow}</div>
        <h2 className="display">{headline}</h2>
        <PlanChipRow variants={chips} />
        <p>{sub}</p>
        {sidebarBullets && (
          <ul className="cc-obs-panel-bullets">
            {sidebarBullets.map((b) => (
              <li key={b}>{b}</li>
            ))}
          </ul>
        )}
      </div>
      <div
        className={"cc-obs-panel-viz" + (bleedRight ? " is-bleed-right" : "")}
      >
        {children}
      </div>
    </section>
  );
};
