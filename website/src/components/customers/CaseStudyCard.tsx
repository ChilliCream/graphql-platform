"use client";

import Link from "next/link";
import React, { CSSProperties, FC } from "react";

import { findIndustry } from "@/data/customers/industries";
import { productLabel, type Story } from "@/data/customers/stories";

import { AnonymousMonogram } from "./AnonymousMonogram";

interface CaseStudyCardProps {
  readonly story: Story;
}

// Metric-first card with two rendering variants: named (logo monogram in
// cream-on-glass) and anonymous (industry-accented stroke monogram). Per the
// brief: metric is the hook, brand is second, link is third.
export const CaseStudyCard: FC<CaseStudyCardProps> = ({ story }) => {
  const industry = findIndustry(story.industry);
  const accent = industry.accentVar;
  const cardStyle: CSSProperties = {
    ["--cc-card-accent" as string]: accent,
  };
  const products = story.products.slice(0, 2).map(productLabel).join(" · ");

  return (
    <Link
      href={`/customers/${story.slug}`}
      className="cc-cu-card"
      style={cardStyle}
    >
      <div className="cc-cu-card-eyebrow">
        <span className="dot" aria-hidden />
        <span>{industry.label}</span>
        {products ? <span aria-hidden>·</span> : null}
        {products ? <span>{products}</span> : null}
      </div>
      <p className="cc-cu-card-metric display">{story.cardMetric}</p>
      <p className="cc-cu-card-context">{story.cardContext}</p>
      <div className="cc-cu-card-foot">
        <div className="cc-cu-card-brand">
          {story.named ? (
            <span className="cc-cu-card-logo" aria-hidden>
              {story.logoMonogram}
            </span>
          ) : (
            <span className="cc-cu-card-logo is-anonymous" aria-hidden>
              <AnonymousMonogram industry={industry} size={36} />
            </span>
          )}
          <span className="cc-cu-card-brand-text">
            <span className="cc-cu-card-brand-name">{story.displayName}</span>
            <span className="cc-cu-card-brand-meta">
              {story.named ? "Named customer" : "Anonymous tier"}
            </span>
          </span>
        </div>
        <span className="cc-cu-card-link">Read story →</span>
      </div>
    </Link>
  );
};
