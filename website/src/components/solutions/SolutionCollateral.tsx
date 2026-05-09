"use client";

import React, { FC } from "react";

import { Band } from "@/components/redesign-system/Band";
import type { Collateral } from "@/data/solutions/types";

interface SolutionCollateralProps {
  readonly collateral: Collateral;
  readonly stepNumber: string;
}

const KIND_EYEBROWS: Record<Collateral["kind"], string> = {
  playbook: "Playbook · Free download",
  starter: "Starter · Clone & deploy",
  workshop: "Workshop · 90-minute session",
};

// Section 08: the one adjacent collateral CTA. A side-door for visitors
// not ready to talk to sales. We deliberately ship one offer per page,
// not three: the constraint is the conversion vehicle.
export const SolutionCollateral: FC<SolutionCollateralProps> = ({
  collateral,
  stepNumber,
}) => (
  <Band variant="accent" ariaLabel="Resource">
    <div className="cc-sl-section cc-sl-collateral">
      <div className="cc-section-label">
        <span className="num">{stepNumber}</span> Resource
      </div>
      <div className="cc-sl-collateral-inner">
        <div className="cc-sl-collateral-card">
          <div>
            <div className="cc-sl-collateral-eyebrow">
              {KIND_EYEBROWS[collateral.kind]}
            </div>
            <h2 className="cc-sl-collateral-title">{collateral.title}</h2>
          </div>
          <a href={collateral.href} className="cc-btn cc-btn-primary">
            Download →
          </a>
        </div>
      </div>
    </div>
  </Band>
);
