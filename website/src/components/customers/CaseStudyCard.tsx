"use client";

import Link from "next/link";
import React, { CSSProperties, FC } from "react";

import { findIndustry } from "@/data/customers/industries";
import { type Story } from "@/data/customers/stories";

import { WordmarkLockup } from "./WordmarkLockup";

interface CaseStudyCardProps {
  readonly story: Story;
}

// Metric-first card. The lockup zone holds a typographic identity per
// the disclosure hierarchy: a wordmark for named customers, a structured
// descriptor for anonymous tiers (no fallback monogram tile, that read as
// missing-asset placeholder). Industry colors the accent rule on the
// descriptor and tints the card's corner glow.
export const CaseStudyCard: FC<CaseStudyCardProps> = ({ story }) => {
  const industry = findIndustry(story.industry);
  const accent = industry.accentVar;
  const cardStyle: CSSProperties = {
    ["--cc-card-accent" as string]: accent,
  };
  const lockupVariant = story.named ? "wordmark" : "descriptor";
  const lockupText = story.named
    ? story.displayName
    : story.descriptor ?? story.displayName.toUpperCase();

  return (
    <Link
      href={`/customers/${story.slug}`}
      className="cc-cu-card"
      style={cardStyle}
    >
      <div className="cc-cu-card-eyebrow">
        <span className="dot" aria-hidden />
        <span>{industry.label}</span>
      </div>
      <p className="cc-cu-card-metric display">{story.cardMetric}</p>
      <p className="cc-cu-card-context">{story.cardContext}</p>
      <div className="cc-cu-card-foot">
        <WordmarkLockup
          variant={lockupVariant}
          text={lockupText}
          factLine={story.factLine}
          industry={industry}
          ghost={story.named ? undefined : industry.monogram}
        />
        <span className="cc-cu-card-link">Read story →</span>
      </div>
    </Link>
  );
};
