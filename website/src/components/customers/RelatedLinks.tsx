"use client";

import Link from "next/link";
import React, { FC } from "react";

import { Band } from "@/components/redesign-system/Band";

interface RelatedLink {
  readonly key: string;
  readonly eyebrow: string;
  readonly title: string;
  readonly body: string;
  readonly href: string;
  readonly cta: string;
}

const RELATED: readonly RelatedLink[] = [
  {
    key: "pricing",
    eyebrow: "Pricing",
    title: "See what each tier ships with",
    body: "Free OSS through enterprise. Hard limits, budget alerts, and the same engine underneath.",
    href: "/pricing",
    cta: "Pricing →",
  },
  {
    key: "enterprise",
    eyebrow: "Enterprise",
    title: "Nitro for enterprise platform teams",
    body: "Self-hosted, air-gapped, agent-ready, supported by the engineers who built it.",
    href: "/enterprise",
    cta: "Enterprise →",
  },
  {
    key: "support",
    eyebrow: "Support",
    title: "Support plans + dedicated SAs",
    body: "Custom SLAs, 24x7 oncall, federation governance, and procurement-ready compliance.",
    href: "/pricing#support",
    cta: "Support plans →",
  },
];

// Section 07: three related links on a tinted band. No card chrome —
// content sits on the band, separated by hairlines, so the section reads
// as a footer-style aside rather than a third row of stacked cards after
// the architect-call CTA.
export const RelatedLinks: FC = () => {
  return (
    <Band variant="tinted" ariaLabel="Related links">
      <div className="cc-section-label">
        <span className="num">07</span> Related
      </div>
      <div className="cc-cu-related-inner">
        <div className="cc-cu-heading is-flat">
          <div className="eyebrow">Related</div>
          <h3 className="cc-cu-related-section-title">
            Three more places to go.
          </h3>
        </div>
        <div className="cc-cu-related-grid">
          {RELATED.map((link) => (
            <Link key={link.key} href={link.href} className="cc-cu-related-row">
              <span className="cc-cu-related-eyebrow">{link.eyebrow}</span>
              <span className="cc-cu-related-title">{link.title}</span>
              <span className="cc-cu-related-body">{link.body}</span>
              <span className="cc-cu-related-link">{link.cta}</span>
            </Link>
          ))}
        </div>
      </div>
    </Band>
  );
};
