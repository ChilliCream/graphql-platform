"use client";

import Link from "next/link";
import React, { FC } from "react";

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

// Section 08: three related links — pricing, enterprise, support. The
// equivalent of Vercel's "explore Vercel Enterprise" tail card, but split
// into three because our buyer wants the comparison.
export const RelatedLinks: FC = () => {
  return (
    <section className="cc-cu-section cc-cu-related">
      <div className="cc-section-label">
        <span className="num">08</span> Related
      </div>
      <div className="cc-cu-related-inner">
        <div className="cc-cu-heading">
          <div className="eyebrow">Related</div>
          <h2 className="display">Three more places to go.</h2>
        </div>
        <div className="cc-cu-related-grid">
          {RELATED.map((link) => (
            <Link
              key={link.key}
              href={link.href}
              className="cc-cu-related-card"
            >
              <span className="cc-cu-related-eyebrow">{link.eyebrow}</span>
              <h3 className="cc-cu-related-title">{link.title}</h3>
              <p className="cc-cu-related-body">{link.body}</p>
              <span className="cc-cu-related-link">{link.cta}</span>
            </Link>
          ))}
        </div>
      </div>
    </section>
  );
};
