"use client";

import React, { CSSProperties, FC } from "react";

import type { Industry } from "@/data/customers/industries";

// Typographic identity for a customer card. Replaces the monogram tile.
//
// Two layouts share a single primitive so named and anonymous customers
// stack in the same lockup zone:
//   - "wordmark"    — the customer name set as bold display type
//                     (Microsoft, Adidas, SBB, etc.). Used for named.
//   - "descriptor"  — a structured anonymous lockup
//                     ("TIER-1 EUROPEAN BANK"). Carries an optional
//                     ghost background letter for visual anchor.
//
// Either way, a one-line factLine sits below in dim mono. The lockup
// matches the disclosure hierarchy: a typographic wordmark for those
// we can name; a typographic descriptor for those we can't.

export type WordmarkLockupVariant = "wordmark" | "descriptor";

export interface WordmarkLockupProps {
  readonly variant: WordmarkLockupVariant;
  /**
   * Top line. For wordmarks the actual brand text (e.g. "Microsoft");
   * for descriptors the all-caps anonymous tier (e.g. "TIER-1 EUROPEAN
   * BANK").
   */
  readonly text: string;
  /** One-line fact (dim mono, all-caps), e.g. "18M ACCOUNTS · DACH". */
  readonly factLine?: string;
  /** Industry color used on the descriptor accent rule and ghost letter. */
  readonly industry: Industry;
  /**
   * Single character rendered as a faint background "ghost". Anonymous
   * descriptors use the industry monogram; named lockups omit this.
   */
  readonly ghost?: string;
  readonly className?: string;
}

const wrapperStyle: CSSProperties = {
  position: "relative",
  display: "flex",
  flexDirection: "column",
  gap: 6,
  minWidth: 0,
  overflow: "hidden",
};

const ghostStyle: CSSProperties = {
  position: "absolute",
  top: "50%",
  right: "-8%",
  transform: "translateY(-52%)",
  fontFamily: "var(--cc-font-sans), sans-serif",
  fontWeight: 600,
  fontSize: "clamp(72px, 9vw, 128px)",
  lineHeight: 0.8,
  letterSpacing: "-0.06em",
  color: "var(--cc-cu-lockup-accent, currentColor)",
  opacity: 0.06,
  pointerEvents: "none",
  userSelect: "none",
};

const wordmarkStyle: CSSProperties = {
  fontFamily: "var(--cc-font-sans), sans-serif",
  fontWeight: 600,
  fontSize: "clamp(28px, 2.6vw, 36px)",
  letterSpacing: "-0.035em",
  lineHeight: 1,
  color: "var(--cc-ink)",
  position: "relative",
  zIndex: 1,
};

const descriptorStyle: CSSProperties = {
  fontFamily: "var(--cc-font-mono), monospace",
  fontWeight: 500,
  fontSize: "clamp(13px, 1.05vw, 15px)",
  letterSpacing: "0.14em",
  textTransform: "uppercase",
  color: "var(--cc-ink)",
  lineHeight: 1.2,
  position: "relative",
  zIndex: 1,
};

const accentRuleStyle = (industryAccent: string): CSSProperties => ({
  width: 28,
  height: 2,
  background: industryAccent,
  marginBottom: 4,
  position: "relative",
  zIndex: 1,
});

const factStyle: CSSProperties = {
  fontFamily: "var(--cc-font-mono), monospace",
  fontSize: 10,
  letterSpacing: "0.18em",
  textTransform: "uppercase",
  color: "var(--cc-ink-dim)",
  lineHeight: 1.4,
  position: "relative",
  zIndex: 1,
};

export const WordmarkLockup: FC<WordmarkLockupProps> = ({
  variant,
  text,
  factLine,
  industry,
  ghost,
  className,
}) => {
  const accentVarStyle = {
    "--cc-cu-lockup-accent": industry.accentVar,
  } as CSSProperties;

  return (
    <div style={{ ...wrapperStyle, ...accentVarStyle }} className={className}>
      {ghost ? (
        <span style={ghostStyle} aria-hidden>
          {ghost}
        </span>
      ) : null}
      {variant === "descriptor" ? (
        <span style={accentRuleStyle(industry.accentVar)} aria-hidden />
      ) : null}
      {variant === "wordmark" ? (
        <span style={wordmarkStyle}>{text}</span>
      ) : (
        <span style={descriptorStyle}>{text}</span>
      )}
      {factLine ? <span style={factStyle}>{factLine}</span> : null}
    </div>
  );
};
