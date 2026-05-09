"use client";

import React from "react";
import styled, { css } from "styled-components";

// Section-level full-bleed primitive. Use this for sections, NOT cards.
// Variants:
//   default  - current page background, no tint
//   tinted   - subtle cream tint (+2-3% lightness)
//   inverted - near-black surface (#0a0d18), bright type. Use 1-2 times per page max.
//   accent   - page accent color at 8-12% opacity, full-bleed colorwash
//   glow     - single radial gradient cast from one corner using accent
export type BandVariant = "default" | "tinted" | "inverted" | "accent" | "glow";

export type BandGlowOrigin =
  | "top-left"
  | "top-right"
  | "bottom-left"
  | "bottom-right";

export interface BandProps {
  variant?: BandVariant;
  glowFrom?: BandGlowOrigin;
  as?: keyof JSX.IntrinsicElements;
  children: React.ReactNode;
  className?: string;
  id?: string;
  ariaLabel?: string;
}

const GLOW_POSITIONS: Record<BandGlowOrigin, string> = {
  "top-left": "0% 0%",
  "top-right": "100% 0%",
  "bottom-left": "0% 100%",
  "bottom-right": "100% 100%",
};

const variantStyles = (variant: BandVariant, glowFrom: BandGlowOrigin) => {
  switch (variant) {
    case "tinted":
      return css`
        background: #f8f4ec;
        color: var(--cc-ink);
      `;
    case "inverted":
      return css`
        background: #0a0d18;
        color: var(--cc-ink);

        h1,
        h2,
        h3,
        h4,
        h5,
        h6 {
          color: #ffffff;
        }
      `;
    case "accent":
      return css`
        background: var(--cc-accent-soft, rgba(245, 241, 234, 0.08));
        color: var(--cc-ink);
      `;
    case "glow":
      return css`
        background-image: radial-gradient(
          60% 60% at ${GLOW_POSITIONS[glowFrom]},
          var(--cc-accent-glow, rgba(255, 255, 255, 0.08)),
          transparent 70%
        );
        color: var(--cc-ink);
      `;
    case "default":
    default:
      return css`
        color: var(--cc-ink);
      `;
  }
};

interface OuterProps {
  $variant: BandVariant;
  $glowFrom: BandGlowOrigin;
}

const Outer = styled.section<OuterProps>`
  position: relative;
  width: 100%;
  padding-top: clamp(64px, 8vw, 128px);
  padding-bottom: clamp(64px, 8vw, 128px);

  ${({ $variant, $glowFrom }) => variantStyles($variant, $glowFrom)}
`;

const Inner = styled.div`
  max-width: 1280px;
  margin: 0 auto;
  padding: 0 var(--cc-pad-x, 24px);
`;

export const Band: React.FC<BandProps> = ({
  variant = "default",
  glowFrom = "top-right",
  as,
  children,
  className,
  id,
  ariaLabel,
}) => {
  return (
    <Outer
      as={as as React.ElementType | undefined}
      $variant={variant}
      $glowFrom={glowFrom}
      className={className}
      id={id}
      aria-label={ariaLabel}
    >
      <Inner>{children}</Inner>
    </Outer>
  );
};

export const BAND_RULES =
  "Use Band as the section primitive on every redesigned page. Use Card only for constraint signals (tier choices, plan slots, security tiles, gated SKUs). Don't card content that's not a constraint.";
