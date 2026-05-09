"use client";

import styled from "styled-components";

// EnterpriseGridRoot owns the dark-navy / cream-ink design tokens for the
// Grid variant of /enterprise. Mirrors `EnterpriseRoot` but strips card
// rounding, drop shadows, and chrome gradients per the Grid spec
// (.work/reviews/grid-design-system.md). All section frames are square,
// hairline-bordered, and share their borders with adjacent cells.
//
// The page accent (`--cc-accent`, `--cc-accent-soft`, etc.) is supplied by
// `<AccentThread page="enterprise">` in the page component and shows up in
// exactly four places per the spec: eyebrow text, hero icon tint, active
// tab/cell underline, and the trailing arrow glyph in CTA links.
export const EnterpriseGridRoot = styled.div`
  --cc-ink: #f5f1ea;
  --cc-ink-dim: rgba(245, 241, 234, 0.62);
  --cc-ink-faint: rgba(245, 241, 234, 0.16);
  --cc-grid-hairline: rgba(245, 241, 234, 0.12);
  --cc-grid-hairline-strong: rgba(245, 241, 234, 0.22);
  --cc-grid-card-bg: #0f1828;
  --cc-grid-card-bg-inverted: #f5f1ea;
  --cc-pad-x: clamp(20px, 5vw, 64px);

  position: relative;
  width: 100%;
  color: var(--cc-ink);
  font-family: var(--cc-font-sans), system-ui, sans-serif;
  background: #0c1322;

  * {
    box-sizing: border-box;
  }

  /* ===== Mono eyebrow / kicker ===== */
  .cc-grid-eyebrow {
    display: inline-flex;
    align-items: center;
    gap: 10px;
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    color: var(--cc-accent, var(--cc-ink-dim));
    margin: 0 0 14px;
  }

  /* ===== Display type ===== */
  .cc-grid-h1 {
    font-family: var(--cc-font-sans), sans-serif;
    font-weight: 500;
    letter-spacing: -0.035em;
    line-height: 1.02;
    font-size: clamp(40px, 6vw, 88px);
    margin: 0 0 20px;
    color: var(--cc-ink);
    text-wrap: balance;
  }
  .cc-grid-h2 {
    font-family: var(--cc-font-sans), sans-serif;
    font-weight: 500;
    letter-spacing: -0.025em;
    line-height: 1.05;
    font-size: clamp(32px, 4.4vw, 56px);
    margin: 0 0 18px;
    color: var(--cc-ink);
    text-wrap: balance;
  }
  .cc-grid-h3 {
    font-family: var(--cc-font-sans), sans-serif;
    font-weight: 500;
    letter-spacing: -0.015em;
    line-height: 1.2;
    font-size: 20px;
    margin: 0 0 10px;
    color: var(--cc-ink);
  }
  .cc-grid-lede {
    font-size: clamp(15px, 1.2vw, 18px);
    line-height: 1.6;
    color: var(--cc-ink-dim);
    margin: 0;
    text-wrap: pretty;
  }
  .cc-grid-body {
    font-size: 14px;
    line-height: 1.55;
    color: var(--cc-ink-dim);
    margin: 0;
    text-wrap: pretty;
  }
  .cc-grid-arrow {
    color: var(--cc-accent, var(--cc-ink));
    font-family: var(--cc-font-mono), monospace;
  }

  /* ===== Section heading wrapper (centered intro above grids) ===== */
  .cc-grid-section-head {
    max-width: 760px;
    margin: 0 auto 0;
    text-align: center;
    padding-bottom: clamp(40px, 5vw, 64px);
  }
  .cc-grid-section-head .cc-grid-eyebrow {
    margin-left: auto;
    margin-right: auto;
  }
  .cc-grid-section-head p {
    font-size: clamp(15px, 1.1vw, 17px);
    color: var(--cc-ink-dim);
    margin: 0 auto;
    max-width: 56ch;
    line-height: 1.6;
    text-wrap: pretty;
  }
`;
