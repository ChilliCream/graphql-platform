"use client";

import Link from "next/link";
import React, { CSSProperties, FC } from "react";

import { categoryLabel } from "@/data/integrations/categories";
import type { Integration } from "@/data/integrations/integrations";
import { partnerAccent } from "@/data/integrations/partner-accents";
import { productLabel } from "@/data/templates/filters";

// Native cards use a filled, display-face monogram on a per-integration
// accent fill. Community cards stay with the stroked monogram and a denser
// container so the visual asymmetry, not the badge, communicates the trust
// tier. Same vocabulary as agents/WorksWhereYouWork's IDE-client tile and
// customers/AnonymousMonogram so the system reads as one family.
const FilledMonogram: FC<{ letter: string; ink: string; size: number }> = ({
  letter,
  ink,
  size,
}) => (
  <svg viewBox="0 0 48 48" width={size} height={size} aria-hidden>
    <text
      x="24"
      y="33"
      textAnchor="middle"
      fontFamily="var(--cc-font-sans), sans-serif"
      fontWeight={700}
      fontSize="26"
      letterSpacing="-0.02em"
      fill={ink}
    >
      {letter}
    </text>
  </svg>
);

const StrokedMonogram: FC<{ letter: string; size: number }> = ({
  letter,
  size,
}) => (
  <svg viewBox="0 0 48 48" width={size} height={size} aria-hidden>
    <text
      x="24"
      y="31"
      textAnchor="middle"
      fontFamily="var(--cc-font-mono), monospace"
      fontWeight={500}
      fontSize="20"
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

interface CardCSSProperties extends CSSProperties {
  "--cc-card-edge"?: string;
  "--cc-card-mono-fill"?: string;
}

// Filled-tile monogram (Native, with brand-color accent), stroked monogram
// (Community, denser, no accent), category eyebrow, name + 1-line pitch in
// the body, type badge + product + "Read docs →" affordance in the footer.
// The card is a marketplace entry point, not a blog post, so the foot verb
// commits to the install/learn intent.
export const IntegrationCard: FC<IntegrationCardProps> = ({
  integration,
  dense = false,
}) => {
  const isNative = integration.type === "native";
  const accent = partnerAccent(integration);
  const monoSize = dense ? 36 : isNative ? 56 : 48;

  const className = [
    "cc-in-card",
    dense ? "is-dense" : "",
    isNative ? "is-native" : "is-community",
  ]
    .filter(Boolean)
    .join(" ");

  const style: CardCSSProperties = isNative
    ? {
        "--cc-card-edge": accent.edge,
        "--cc-card-mono-fill": accent.fill,
      }
    : {};

  return (
    <Link
      href={`/integrations/${integration.slug}`}
      className={className}
      style={style}
    >
      <div className="cc-in-card-head">
        <span
          className={`cc-in-card-mono${isNative ? " is-filled" : ""}`}
          style={{ width: monoSize, height: monoSize }}
        >
          {isNative ? (
            <FilledMonogram
              letter={integration.letter}
              ink={accent.ink}
              size={monoSize}
            />
          ) : (
            <StrokedMonogram letter={integration.letter} size={monoSize} />
          )}
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
        <span className="cc-in-card-readlink" aria-hidden>
          Read docs →
        </span>
      </div>
    </Link>
  );
};
