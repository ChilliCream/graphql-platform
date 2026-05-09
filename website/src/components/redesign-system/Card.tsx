/* Cards are for CONSTRAINT signals (plans, tiers, SKUs, security gates). For section content, prefer <Band>. */
"use client";

import React from "react";
import styled, { css } from "styled-components";

// Three card variants:
//   constraint  - strong border, dark inset background, slight inner padding.
//                 Use ONLY for plans/SKUs/tiers, things that represent a
//                 constraint the user picks between.
//   default     - softer border, used for content that needs some chrome but
//                 isn't a constraint signal. Avoid using this for content
//                 that could live on a band.
//   ghost       - borderless, just internal padding. Used for content panels
//                 that are visually elevated by accent or layout but don't
//                 need a frame.
//
// `featured` adds a subtle accent glow halo and a 1px accent-tinted border.

export type CardVariant = "constraint" | "default" | "ghost";

export interface CardProps {
  variant?: CardVariant;
  featured?: boolean;
  children: React.ReactNode;
  className?: string;
  as?: keyof JSX.IntrinsicElements;
}

const variantStyles = (variant: CardVariant) => {
  switch (variant) {
    case "constraint":
      return css`
        border: 1px solid var(--cc-ink-faint);
        background: rgba(10, 13, 24, 0.55);
        padding: clamp(20px, 2.4vw, 28px);
        border-radius: 14px;
      `;
    case "ghost":
      return css`
        border: none;
        background: transparent;
        padding: clamp(20px, 2.4vw, 32px);
        border-radius: 0;
      `;
    case "default":
    default:
      return css`
        border: 1px solid var(--cc-ink-faint);
        background: transparent;
        padding: clamp(20px, 2.4vw, 28px);
        border-radius: 14px;
      `;
  }
};

const featuredStyles = css`
  border-color: var(--cc-accent-line, var(--cc-ink-faint));
  box-shadow: 0 0 80px var(--cc-accent-glow, rgba(255, 255, 255, 0.06));
`;

interface OuterProps {
  $variant: CardVariant;
  $featured: boolean;
}

const Outer = styled.div<OuterProps>`
  position: relative;
  display: block;
  color: var(--cc-ink);

  ${({ $variant }) => variantStyles($variant)}
  ${({ $featured }) => ($featured ? featuredStyles : "")}
`;

/**
 * Bordered surface for constraint signals. Reserve `constraint` for plans,
 * tiers, SKUs, or guardrail tiles. Reserve `default` for content that needs
 * chrome but isn't a constraint. Reserve `ghost` for borderless content
 * panels elevated by layout or accent alone.
 */
export const Card: React.FC<CardProps> = ({
  variant = "default",
  featured = false,
  children,
  className,
  as,
}) => {
  return (
    <Outer
      as={as as React.ElementType | undefined}
      $variant={variant}
      $featured={featured}
      className={className}
    >
      {children}
    </Outer>
  );
};
