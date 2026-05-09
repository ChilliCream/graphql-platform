"use client";

import styled from "styled-components";

import { GRID_TOKENS } from "@/components/redesign-system/grid";

// SolutionsGridRoot owns the dark-navy / cream-ink design tokens for the
// Grid variant of /solutions/[slug]. Mirrors `EnterpriseGridRoot` and
// `PricingGridRoot`: same surface palette, same hairline tokens, same
// monochrome chrome. The per-page accent thread (supplied by AccentThread
// with `page="solutions"` and the per-slug override) shows up in exactly
// four places per the Grid spec: eyebrow text, hero icon tint, active-tab
// underline, and the trailing arrow glyph in CTA links.
//
// Square corners only: card border-radius is 0 throughout. No shadows, no
// chrome gradients. Adjacent cards share their 1px hairline borders, which
// is what produces the Vercel-style continuous grid frame.
export const SolutionsGridRoot = styled.div`
  --cc-grid-bg: ${GRID_TOKENS.bgBase};
  --cc-grid-card-bg: ${GRID_TOKENS.bgCard};
  --cc-grid-card-bg-inverted: ${GRID_TOKENS.bgInverted};
  --cc-grid-card-hover: ${GRID_TOKENS.bgHover};
  --cc-grid-hairline: ${GRID_TOKENS.hairline};
  --cc-grid-hairline-strong: ${GRID_TOKENS.hairlineStrong};
  --cc-grid-ink: ${GRID_TOKENS.inkPrimary};
  --cc-grid-ink-body: ${GRID_TOKENS.inkBody};
  --cc-grid-ink-muted: ${GRID_TOKENS.inkMuted};
  --cc-grid-ink-faint: ${GRID_TOKENS.inkFaint};

  --cc-ink: ${GRID_TOKENS.inkPrimary};
  --cc-ink-dim: ${GRID_TOKENS.inkBody};
  --cc-ink-faint: ${GRID_TOKENS.inkFaint};
  --cc-pad-x: ${GRID_TOKENS.pageGutter};

  /* ConceptDiagram body relies on these legacy color tokens (--cc-col-*)
     for the per-language tints in the polyglot diagram. Map them here so
     the diagram reads correctly on the dark Grid surface. */
  --cc-col-cat: oklch(0.74 0.18 30);
  --cc-col-bil: oklch(0.82 0.16 90);
  --cc-col-ord: oklch(0.76 0.16 150);
  --cc-col-shi: oklch(0.74 0.14 220);
  --cc-col-usr: oklch(0.72 0.18 310);
  --cc-col-tel: oklch(0.74 0.14 200);
  --cc-amber: oklch(0.85 0.16 75);

  position: relative;
  width: 100%;
  background: ${GRID_TOKENS.bgBase};
  color: ${GRID_TOKENS.inkPrimary};
  font-family: var(--cc-font-sans), system-ui, sans-serif;

  * {
    box-sizing: border-box;
  }

  /* ===== Mono eyebrow ===== */
  .cc-grid-eyebrow {
    display: inline-flex;
    align-items: center;
    gap: 10px;
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    color: var(--cc-accent, ${GRID_TOKENS.inkMuted});
    margin: 0 0 14px;
  }

  /* ===== Display type ===== */
  .cc-grid-h1 {
    font-family: var(--cc-font-sans), sans-serif;
    font-weight: 600;
    letter-spacing: -0.035em;
    line-height: 1.02;
    font-size: clamp(40px, 5.5vw, 72px);
    margin: 0 0 20px;
    color: ${GRID_TOKENS.inkPrimary};
    text-wrap: balance;
  }
  .cc-grid-h2 {
    font-family: var(--cc-font-sans), sans-serif;
    font-weight: 600;
    letter-spacing: -0.025em;
    line-height: 1.05;
    font-size: ${GRID_TOKENS.h2Size};
    margin: 0 0 18px;
    color: ${GRID_TOKENS.inkPrimary};
    text-wrap: balance;
  }
  .cc-grid-h3 {
    font-family: var(--cc-font-sans), sans-serif;
    font-weight: 600;
    letter-spacing: -0.015em;
    line-height: 1.25;
    font-size: 20px;
    margin: 0 0 10px;
    color: ${GRID_TOKENS.inkPrimary};
  }
  .cc-grid-lede {
    font-size: clamp(15px, 1.2vw, 18px);
    line-height: 1.6;
    color: ${GRID_TOKENS.inkBody};
    margin: 0;
    text-wrap: pretty;
  }
  .cc-grid-body {
    font-size: 14px;
    line-height: 1.55;
    color: ${GRID_TOKENS.inkBody};
    margin: 0;
    text-wrap: pretty;
  }

  /* ===== Inverted-band ink scope.
     GridSection variant="inverted" paints a near-black surface; the
     base ink colors stay cream so they read against it. Override only
     where headlines/eyebrows live inside the inverted band. */

  /* ===== Section heading wrapper ===== */
  .cc-grid-section-head {
    max-width: 760px;
    margin: 0 auto;
    text-align: center;
    padding-bottom: clamp(40px, 5vw, 64px);
  }
  .cc-grid-section-head .cc-grid-eyebrow {
    margin-left: auto;
    margin-right: auto;
  }
  .cc-grid-section-head p {
    font-size: clamp(15px, 1.1vw, 17px);
    color: ${GRID_TOKENS.inkBody};
    margin: 0 auto;
    max-width: 56ch;
    line-height: 1.6;
    text-wrap: pretty;
  }
`;
