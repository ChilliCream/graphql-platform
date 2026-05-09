"use client";

import Link from "next/link";
import React, { FC } from "react";

import { categoryLabel } from "@/data/integrations/categories";
import type { Integration } from "@/data/integrations/integrations";

interface IntegrationDetailHeaderProps {
  readonly integration: Integration;
}

const Monogram: FC<{ letter: string }> = ({ letter }) => (
  <svg viewBox="0 0 72 72" width="100%" height="100%" aria-hidden>
    <text
      x="36"
      y="48"
      textAnchor="middle"
      fontFamily="var(--cc-font-sans), sans-serif"
      fontWeight={500}
      fontSize="34"
      fill="currentColor"
    >
      {letter}
    </text>
  </svg>
);

const StarIcon: FC = () => (
  <svg
    width="12"
    height="12"
    viewBox="0 0 12 12"
    fill="currentColor"
    aria-hidden
  >
    <path d="M6 0.6 L7.5 4.2 L11.4 4.5 L8.4 7 L9.3 11 L6 8.9 L2.7 11 L3.6 7 L0.6 4.5 L4.5 4.2 Z" />
  </svg>
);

const formatStars = (n: number): string => {
  if (n >= 1000) {
    const k = (n / 1000).toFixed(1);
    return `${k.endsWith(".0") ? k.slice(0, -2) : k}k`;
  }
  return String(n);
};

// Detail-page header: breadcrumb back to /integrations, breadcrumb to the
// category, then a row of [logo · name · primary CTA · GitHub stars]. The
// "Get Started" CTA points at docs by default, with sensible fallbacks when
// docs aren't authored yet. The stars badge is real (read from the seed,
// updated by hand for now); a future task can wire it to the GitHub API.
export const IntegrationDetailHeader: FC<IntegrationDetailHeaderProps> = ({
  integration,
}) => {
  const ctaHref = integration.links.docs ?? integration.links.website ?? "#";
  return (
    <section className="cc-ind-section cc-ind-header">
      <div className="cc-section-label">
        <span className="num">01</span> Integration
      </div>
      <div className="cc-ind-header-inner">
        <nav className="cc-ind-breadcrumb" aria-label="Breadcrumb">
          <Link href="/integrations">Integrations</Link>
          <span className="sep" aria-hidden>
            /
          </span>
          <Link href={`/integrations?category=${integration.category}`}>
            {categoryLabel(integration.category)}
          </Link>
          <span className="sep" aria-hidden>
            /
          </span>
          <span className="crumb-current">{integration.name}</span>
        </nav>
        <div className="cc-ind-header-row">
          <div className="cc-ind-header-mono">
            <Monogram letter={integration.letter} />
          </div>
          <div className="cc-ind-header-text">
            <span className="eyebrow">
              {categoryLabel(integration.category)}
            </span>
            <h1 className="display">{integration.name}</h1>
          </div>
          <div className="cc-ind-header-actions">
            {integration.githubStars && integration.links.github && (
              <a
                href={integration.links.github}
                className="cc-ind-stars"
                rel="noopener"
                target="_blank"
                aria-label={`${integration.githubStars} GitHub stars`}
              >
                <StarIcon />
                {formatStars(integration.githubStars)}
              </a>
            )}
            {integration.links.github && (
              <a
                href={integration.links.github}
                className="cc-ind-header-cta is-ghost"
                rel="noopener"
                target="_blank"
              >
                View on GitHub
              </a>
            )}
            <a
              href={ctaHref}
              className="cc-ind-header-cta is-primary"
              rel="noopener"
            >
              Get Started →
            </a>
          </div>
        </div>
      </div>
    </section>
  );
};
