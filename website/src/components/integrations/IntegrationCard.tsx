"use client";

import Link from "next/link";
import React, { FC } from "react";

import { categoryLabel } from "@/data/integrations/categories";
import type { Integration } from "@/data/integrations/integrations";
import { productLabel } from "@/data/templates/filters";

// Stroked single-letter monogram. Same vocabulary as
// agents/WorksWhereYouWork's IDE-client tile and customers/AnonymousMonogram,
// so the integrations grid reads as part of the same visual system.
const Monogram: FC<{ letter: string }> = ({ letter }) => (
  <svg viewBox="0 0 48 48" width="100%" height="100%" aria-hidden>
    <text
      x="24"
      y="31"
      textAnchor="middle"
      fontFamily="var(--cc-font-sans), sans-serif"
      fontWeight={500}
      fontSize="22"
      fill="currentColor"
    >
      {letter}
    </text>
  </svg>
);

interface IntegrationCardProps {
  readonly integration: Integration;
  // Dense variant is used for the Community grid, where smaller cards keep
  // the page from running too long without losing the brand-letter primary
  // signal.
  readonly dense?: boolean;
}

// Square monogram tile (top-left), category eyebrow (top-right), name + 1-line
// pitch in the body, type badge + primary product in the footer. No install
// counts, no ratings, deliberately. The card is an entry point, not a decision
// tool, mirroring Vercel's Marketplace card model.
export const IntegrationCard: FC<IntegrationCardProps> = ({
  integration,
  dense = false,
}) => {
  const className = dense ? "cc-in-card is-dense" : "cc-in-card";
  const monoSize = dense ? 36 : 48;
  return (
    <Link href={`/integrations/${integration.slug}`} className={className}>
      <div className="cc-in-card-head">
        <span
          className="cc-in-card-mono"
          style={{ width: monoSize, height: monoSize }}
        >
          <Monogram letter={integration.letter} />
        </span>
        <span className="cc-in-card-eyebrow">
          {categoryLabel(integration.category)}
        </span>
      </div>
      <h3 className="cc-in-card-name">{integration.name}</h3>
      <p className="cc-in-card-tagline">{integration.tagline}</p>
      <div className="cc-in-card-foot">
        <span className={`cc-in-typebadge is-${integration.type}`}>
          {integration.type === "native" ? "Native" : "Community"}
        </span>
        {integration.products.length > 0 && (
          <span className="cc-in-card-product">
            {productLabel(integration.products[0])}
          </span>
        )}
      </div>
    </Link>
  );
};
