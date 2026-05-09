"use client";

import React, { FC } from "react";

import { Band } from "@/components/redesign-system/Band";
import { CustomerOutcome as CustomerOutcomeData } from "@/data/enterprise/outcomes";

interface CustomerOutcomeProps {
  readonly outcome: CustomerOutcomeData;
  readonly sectionNumber: string;
  readonly sectionLabel?: string;
  readonly variant?: "default" | "tinted";
}

export const CustomerOutcome: FC<CustomerOutcomeProps> = ({
  outcome,
  sectionNumber,
  sectionLabel,
  variant = "default",
}) => {
  return (
    <Band variant={variant} ariaLabel={sectionLabel ?? "Customer outcome"}>
      <div className="cc-section-label">
        <span className="num">{sectionNumber}</span>{" "}
        {sectionLabel ?? "Customer outcome"}
      </div>
      <article className="cc-ent-outcome-card">
        <div className="cc-ent-outcome-persona">{outcome.persona}</div>
        <blockquote className="cc-ent-outcome-quote">
          “{outcome.quote}”
        </blockquote>
        <div className="cc-ent-outcome-meta">
          <span className="cc-ent-outcome-metric">{outcome.metric}</span>
          <span className="cc-ent-outcome-attribution">
            — {outcome.attribution}
          </span>
        </div>
      </article>
    </Band>
  );
};
