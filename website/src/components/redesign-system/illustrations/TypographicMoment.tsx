"use client";

import React from "react";
import styled, { css } from "styled-components";

// "Oversized typography as illustration" register: a single restrained
// typographic moment used in place of a graphic. Customer wordmarks set
// huge, hero numerals, code identifiers in headlines.

export type TypographicVariant = "gradient" | "outline" | "fill";
export type TypographicSize = "medium" | "huge";

export interface TypographicMomentProps {
  /** The display text, e.g. "47", "MICROSOFT", "p99". */
  text: string;
  /** Small label after big text, e.g. "%", "BFFs", "ms". */
  unit?: string;
  variant?: TypographicVariant;
  size?: TypographicSize;
  className?: string;
}

const sizeStyles = (size: TypographicSize) => {
  if (size === "huge") {
    return css`
      font-size: clamp(120px, 18vw, 280px);
      line-height: 0.9;
      font-feature-settings: "tnum" 1;
    `;
  }
  return css`
    font-size: clamp(64px, 9vw, 128px);
    line-height: 0.92;
    font-feature-settings: "tnum" 1;
  `;
};

const variantStyles = (variant: TypographicVariant) => {
  switch (variant) {
    case "gradient":
      return css`
        background: var(
          --cc-accent-gradient,
          linear-gradient(120deg, var(--cc-accent, currentColor), currentColor)
        );
        -webkit-background-clip: text;
        background-clip: text;
        -webkit-text-fill-color: transparent;
        color: transparent;
      `;
    case "outline":
      return css`
        color: transparent;
        -webkit-text-stroke: 1.5px currentColor;
        opacity: 0.62;
      `;
    case "fill":
    default:
      return css`
        color: var(--cc-ink);
      `;
  }
};

interface OuterProps {
  $size: TypographicSize;
}

const Outer = styled.div<OuterProps>`
  display: inline-flex;
  align-items: baseline;
  gap: clamp(6px, 1vw, 16px);
  max-width: 100%;
  overflow: hidden;
  ${({ $size }) =>
    $size === "huge"
      ? css`
          line-height: 0.9;
        `
      : css`
          line-height: 0.92;
        `}
`;

interface TextProps {
  $variant: TypographicVariant;
  $size: TypographicSize;
}

const Text = styled.span<TextProps>`
  font-family: var(--cc-font-sans), sans-serif;
  font-weight: 500;
  letter-spacing: -0.04em;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: clip;
  ${({ $size }) => sizeStyles($size)}
  ${({ $variant }) => variantStyles($variant)}
`;

interface UnitProps {
  $size: TypographicSize;
}

const Unit = styled.span<UnitProps>`
  font-family: var(--cc-font-sans), sans-serif;
  font-weight: 500;
  color: var(--cc-ink-dim);
  letter-spacing: -0.02em;
  ${({ $size }) =>
    $size === "huge"
      ? css`
          font-size: clamp(28px, 3.6vw, 56px);
        `
      : css`
          font-size: clamp(20px, 2.4vw, 32px);
        `}
`;

/**
 * Renders a single oversized typographic moment for use as illustration.
 * `huge` size is intended as the dominant element of a band; `medium` works
 * inline with body content.
 */
export const TypographicMoment: React.FC<TypographicMomentProps> = ({
  text,
  unit,
  variant = "fill",
  size = "medium",
  className,
}) => {
  return (
    <Outer $size={size} className={className}>
      <Text $variant={variant} $size={size}>
        {text}
      </Text>
      {unit ? <Unit $size={size}>{unit}</Unit> : null}
    </Outer>
  );
};
