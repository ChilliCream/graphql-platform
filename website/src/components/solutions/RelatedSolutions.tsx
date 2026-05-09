"use client";

import React, { FC } from "react";

import { Band } from "@/components/redesign-system/Band";
import type { SolutionRecord } from "@/data/solutions/types";

interface RelatedSolutionsProps {
  readonly solutions: readonly SolutionRecord[];
  readonly stepNumber: string;
}

// Section 11: three cross-link cards. Visitors who land on /federation
// should see /polyglot-federation, /event-driven, and /agents in the
// footer of the page. Aggressive cross-linking is how the suite reads
// as a coherent IA, not a pile of standalone pages.
export const RelatedSolutions: FC<RelatedSolutionsProps> = ({
  solutions,
  stepNumber,
}) => {
  if (solutions.length === 0) {
    return null;
  }

  return (
    <Band variant="tinted" ariaLabel="Related">
      <div className="cc-sl-tint-scope">
        <div className="cc-sl-section cc-sl-related">
          <div className="cc-section-label">
            <span className="num">{stepNumber}</span> Related
          </div>
          <div className="cc-sl-related-inner">
            <div className="cc-sl-heading">
              <div className="eyebrow">More solutions</div>
              <h2 className="display">Where this leads next.</h2>
            </div>
            <div className="cc-sl-related-grid">
              {solutions.map((s) => (
                <a
                  key={s.slug}
                  href={`/solutions/${s.slug}`}
                  className="cc-sl-related-card"
                >
                  <div className="cc-sl-related-eyebrow">
                    {s.category === "industry" ? "Industry" : "Use case"}
                  </div>
                  <h3 className="cc-sl-related-title">{s.title}</h3>
                  <p className="cc-sl-related-body">{s.hero.sub}</p>
                  <span className="cc-sl-related-link">Read more →</span>
                </a>
              ))}
            </div>
          </div>
        </div>
      </div>
    </Band>
  );
};
