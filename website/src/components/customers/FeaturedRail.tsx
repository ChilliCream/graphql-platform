"use client";

import React, { FC } from "react";

import { Band } from "@/components/redesign-system/Band";
import { STORIES } from "@/data/customers/stories";

import { CaseStudyCard } from "./CaseStudyCard";

// Section 03: 6-card editorial rail. These ARE constraint signals — each
// card is a case-study exhibit with a detail-page target. Cards stay.
// Mix of 3 named + 3 anonymous tier stories so the page sets the
// expectation up front: anonymous is normal here and the metric is the
// hook regardless of whether we can name the brand.
export const FeaturedRail: FC = () => {
  const featured = STORIES.filter((s) => s.featured).slice(0, 6);

  return (
    <Band variant="default" ariaLabel="Featured stories">
      <div className="cc-section-label">
        <span className="num">03</span> Featured stories
      </div>
      <div className="cc-cu-featured-inner">
        <div className="cc-cu-heading">
          <div className="eyebrow">Featured stories</div>
          <h2 className="display">The win first. The brand second.</h2>
          <p>
            Six platform teams. Six different stacks. Six metrics that paid for
            the rollout in the first quarter. Some named, some not — every one
            real and verified.
          </p>
        </div>
        <div className="cc-cu-cards-grid">
          {featured.map((story) => (
            <CaseStudyCard key={story.slug} story={story} />
          ))}
        </div>
      </div>
    </Band>
  );
};
