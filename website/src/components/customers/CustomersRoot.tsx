"use client";

import styled from "styled-components";

// CustomersRoot owns the dark navy / cream-ink design tokens for the
// /customers index and /customers/[slug] detail pages. Same approach as
// PricingRoot, EnterpriseRoot, ObservabilityRoot, AgentsRoot: tokens,
// section shell, typography, and button system are shared verbatim. The
// section-specific CSS lives below and is scoped by class prefix:
//   .cc-cu-*       customers index sections
//   .cc-csd-*      customer story detail sections
export const CustomersRoot = styled.div`
  --cc-ink: #f5f1ea;
  --cc-ink-dim: rgba(245, 241, 234, 0.62);
  --cc-ink-faint: rgba(245, 241, 234, 0.16);
  --cc-line-w: 1.5px;
  --cc-col-cat: oklch(0.74 0.18 30);
  --cc-col-bil: oklch(0.82 0.16 90);
  --cc-col-ord: oklch(0.76 0.16 150);
  --cc-col-shi: oklch(0.74 0.14 220);
  --cc-col-usr: oklch(0.72 0.18 310);
  --cc-col-tel: oklch(0.74 0.14 200);
  --cc-pad-x: clamp(28px, 5vw, 96px);

  position: relative;
  width: 100%;
  color: var(--cc-ink);
  font-family: var(--cc-font-sans), system-ui, sans-serif;
  background: radial-gradient(
      80% 50% at 70% 0%,
      rgba(80, 60, 200, 0.1),
      transparent 60%
    ),
    linear-gradient(
      180deg,
      #0c1322 0%,
      #0c1322 22%,
      #0b1220 38%,
      #0a111e 58%,
      #09101c 78%,
      #08101a 100%
    );

  * {
    box-sizing: border-box;
  }

  /* ===== Section shell ===== */
  section.cc-cu-section,
  section.cc-csd-section {
    position: relative;
    width: 100%;
    padding-left: var(--cc-pad-x);
    padding-right: var(--cc-pad-x);
  }
  .cc-section-label {
    position: absolute;
    top: 36px;
    left: var(--cc-pad-x);
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.18em;
    color: var(--cc-ink-dim);
    text-transform: uppercase;
    z-index: 4;
    display: inline-flex;
    align-items: center;
    gap: 10px;
  }
  .cc-section-label .num {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    padding: 3px 7px;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 4px;
    color: var(--cc-ink);
    line-height: 1;
  }

  /* ===== Typography ===== */
  .display {
    font-family: var(--cc-font-sans), sans-serif;
    font-weight: 500;
    letter-spacing: -0.035em;
    line-height: 0.98;
  }
  .eyebrow {
    font-family: var(--cc-font-mono), monospace;
    font-size: 12px;
    letter-spacing: 0.16em;
    color: var(--cc-ink-dim);
    text-transform: uppercase;
  }

  /* ===== Buttons ===== */
  .cc-btn {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    gap: 12px;
    padding: 16px 26px;
    border-radius: 999px;
    font-family: var(--cc-font-sans), sans-serif;
    font-size: 15px;
    font-weight: 500;
    cursor: pointer;
    border: none;
    text-decoration: none;
    transition: transform 0.12s ease, background 0.12s ease,
      border-color 0.12s ease, color 0.12s ease;
  }
  .cc-btn-primary {
    background: var(--cc-ink);
    color: #0c1322;
  }
  .cc-btn-primary:hover {
    transform: translateY(-1px);
  }
  .cc-btn-ghost {
    background: transparent;
    color: var(--cc-ink);
    border: 1px solid var(--cc-ink-faint);
  }
  .cc-btn-ghost:hover {
    border-color: var(--cc-ink);
  }
  .cc-cta-row {
    display: flex;
    gap: 14px;
    justify-content: center;
    flex-wrap: wrap;
  }

  /* ============================================================
   *                     INDEX PAGE
   * ============================================================ */

  /* ===== 01 Hero ===== */
  .cc-cu-hero {
    position: relative;
    padding-top: 96px;
    padding-bottom: 32px;
    text-align: center;
  }
  .cc-cu-hero-inner {
    max-width: 920px;
    margin: 0 auto;
  }
  .cc-cu-hero h1 {
    font-size: clamp(40px, 6.2vw, 92px);
    margin: 18px 0 24px;
    line-height: 1.02;
  }
  .cc-cu-hero h1 .accent {
    background: var(
      --cc-accent-gradient,
      linear-gradient(
        120deg,
        var(--cc-col-shi),
        var(--cc-col-usr) 60%,
        var(--cc-col-cat)
      )
    );
    -webkit-background-clip: text;
    background-clip: text;
    -webkit-text-fill-color: transparent;
  }
  .cc-cu-hero p {
    font-size: clamp(15px, 1.2vw, 19px);
    line-height: 1.55;
    color: var(--cc-ink-dim);
    max-width: 60ch;
    margin: 0 auto 32px;
    text-wrap: pretty;
  }

  /* ===== Generic section heading ===== */
  .cc-cu-heading {
    text-align: center;
    margin: 0 auto 48px;
    max-width: 760px;
  }
  .cc-cu-heading h2 {
    font-size: clamp(32px, 4vw, 50px);
    margin: 8px auto 14px;
    max-width: 22ch;
    line-height: 1.05;
  }
  .cc-cu-heading p {
    font-size: clamp(15px, 1.1vw, 17px);
    color: var(--cc-ink-dim);
    max-width: 56ch;
    margin: 0 auto;
    text-wrap: pretty;
    line-height: 1.55;
  }
  /* Flat heading variant for secondary sections (all-stories, related). */
  .cc-cu-heading.is-flat {
    margin-bottom: 24px;
    text-align: left;
    max-width: none;
  }
  .cc-cu-grid-section-title,
  .cc-cu-related-section-title {
    font-family: var(--cc-font-sans), sans-serif;
    font-size: clamp(22px, 2.1vw, 28px);
    font-weight: 500;
    letter-spacing: -0.02em;
    color: var(--cc-ink);
    margin: 4px 0 8px;
    line-height: 1.2;
  }
  .cc-cu-heading.is-flat p {
    font-size: 14px;
    color: var(--cc-ink-dim);
    margin: 0;
    max-width: 56ch;
  }

  /* ===== 02 Proof strip (tinted band) ===== */
  /* Tinted band uses cream background; flip the ink tokens locally so
     hairlines and dim type stay legible on cream. The global h1-h6 rule
     binds to --cc-heading-text-color, so override that token too. */
  .cc-cu-proof-inner {
    --cc-ink: #18121d;
    --cc-ink-dim: rgba(24, 18, 29, 0.62);
    --cc-ink-faint: rgba(24, 18, 29, 0.16);
    --cc-heading-text-color: #18121d;
    color: var(--cc-ink);
    max-width: 1180px;
    margin: 0 auto;
    display: flex;
    flex-direction: column;
    gap: 36px;
  }
  .cc-cu-proof-trustline {
    font-family: var(--cc-font-mono), monospace;
    font-size: clamp(13px, 1.05vw, 15px);
    letter-spacing: 0.06em;
    color: var(--cc-ink);
    margin: 0;
    max-width: 70ch;
    line-height: 1.55;
  }

  /* ===== 02 Featured rail ===== */
  .cc-cu-featured {
    padding-top: 32px;
    padding-bottom: 96px;
  }
  .cc-cu-featured-inner {
    max-width: 1280px;
    margin: 0 auto;
  }
  .cc-cu-cards-grid {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 22px;
  }
  @media (max-width: 1080px) {
    .cc-cu-cards-grid {
      grid-template-columns: repeat(2, 1fr);
    }
  }
  @media (max-width: 720px) {
    .cc-cu-cards-grid {
      grid-template-columns: 1fr;
    }
  }

  /* ===== Case study card ===== */
  .cc-cu-card {
    position: relative;
    display: flex;
    flex-direction: column;
    padding: 30px 28px 26px;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 18px;
    background: rgba(255, 255, 255, 0.025);
    text-decoration: none;
    color: inherit;
    transition: border-color 0.18s ease, transform 0.18s ease,
      background 0.18s ease;
    overflow: hidden;
  }
  .cc-cu-card::before {
    content: "";
    position: absolute;
    inset: 0;
    background: radial-gradient(
      120% 60% at 0% 0%,
      var(--cc-card-accent, transparent) 0%,
      transparent 60%
    );
    opacity: 0.12;
    pointer-events: none;
    transition: opacity 0.18s ease;
  }
  .cc-cu-card:hover {
    border-color: rgba(245, 241, 234, 0.3);
    transform: translateY(-2px);
    background: rgba(255, 255, 255, 0.04);
  }
  .cc-cu-card:hover::before {
    opacity: 0.22;
  }
  .cc-cu-card-eyebrow {
    font-family: var(--cc-font-mono), monospace;
    font-size: 10px;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
    margin-bottom: 16px;
    display: flex;
    align-items: center;
    gap: 8px;
    flex-wrap: wrap;
  }
  .cc-cu-card-eyebrow .dot {
    display: inline-block;
    width: 5px;
    height: 5px;
    border-radius: 999px;
    background: var(--cc-card-accent, var(--cc-ink-dim));
  }
  .cc-cu-card-metric {
    font-family: var(--cc-font-sans), sans-serif;
    font-size: clamp(28px, 2.6vw, 38px);
    font-weight: 500;
    letter-spacing: -0.025em;
    line-height: 1.05;
    color: var(--cc-ink);
    margin: 0 0 14px;
    text-wrap: balance;
  }
  .cc-cu-card-context {
    font-size: 14px;
    line-height: 1.55;
    color: var(--cc-ink-dim);
    margin: 0 0 26px;
    flex: 1;
    text-wrap: pretty;
  }
  .cc-cu-card-foot {
    display: flex;
    align-items: flex-end;
    justify-content: space-between;
    gap: 16px;
    padding-top: 18px;
    border-top: 1px solid var(--cc-ink-faint);
  }
  .cc-cu-card-link {
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.16em;
    text-transform: uppercase;
    color: var(--cc-ink);
    flex-shrink: 0;
    display: inline-flex;
    align-items: center;
    gap: 6px;
    padding-bottom: 2px;
  }
  .cc-cu-card:hover .cc-cu-card-link {
    color: var(--cc-card-accent, var(--cc-col-shi));
  }

  /* ===== 04 Industry trust wall ===== */
  /* Inverted band: dark surface, no card edges. Tiles are content
     cells separated by hairline foots, varied span by scale, with the
     industry chip carrying the color and the fact carrying the proof. */
  .cc-cu-trust-inner {
    max-width: 1280px;
    margin: 0 auto;
  }
  .cc-cu-trust-grid {
    display: grid;
    grid-template-columns: repeat(12, 1fr);
    gap: 18px 22px;
  }
  @media (max-width: 1080px) {
    .cc-cu-trust-grid {
      grid-template-columns: repeat(6, 1fr);
      gap: 16px;
    }
  }
  @media (max-width: 640px) {
    .cc-cu-trust-grid {
      grid-template-columns: repeat(2, 1fr);
      gap: 14px;
    }
  }
  .cc-cu-trust-tile {
    grid-column: span 2;
    display: flex;
    flex-direction: column;
    align-items: flex-start;
    gap: 8px;
    padding: 18px 16px 18px 14px;
    border-left: 2px solid var(--cc-trust-accent, var(--cc-ink-faint));
    background: transparent;
    transition: border-color 0.15s ease;
  }
  .cc-cu-trust-tile.is-md {
    grid-column: span 3;
  }
  .cc-cu-trust-tile.is-lg {
    grid-column: span 4;
    padding-bottom: 22px;
  }
  @media (max-width: 1080px) {
    .cc-cu-trust-tile,
    .cc-cu-trust-tile.is-md,
    .cc-cu-trust-tile.is-lg {
      grid-column: span 2;
    }
  }
  @media (max-width: 640px) {
    .cc-cu-trust-tile,
    .cc-cu-trust-tile.is-md,
    .cc-cu-trust-tile.is-lg {
      grid-column: span 1;
    }
  }
  .cc-cu-trust-chip {
    font-family: var(--cc-font-mono), monospace;
    font-size: 10px;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    color: var(--cc-trust-accent, var(--cc-ink-dim));
  }
  .cc-cu-trust-name {
    font-family: var(--cc-font-sans), sans-serif;
    font-weight: 500;
    font-size: clamp(14px, 1.05vw, 17px);
    letter-spacing: -0.01em;
    color: var(--cc-ink);
    line-height: 1.25;
  }
  .cc-cu-trust-tile.is-named .cc-cu-trust-name {
    font-weight: 600;
    font-size: clamp(15px, 1.15vw, 19px);
  }
  .cc-cu-trust-tile.is-lg .cc-cu-trust-name {
    font-size: clamp(17px, 1.4vw, 22px);
  }
  .cc-cu-trust-fact {
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.12em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
    line-height: 1.4;
  }

  /* ===== 05 All stories grid ===== */
  .cc-cu-grid-section {
    padding-top: 0;
    padding-bottom: 120px;
  }
  .cc-cu-grid-inner {
    max-width: 1280px;
    margin: 0 auto;
  }
  .cc-cu-filters {
    display: flex;
    flex-direction: column;
    gap: 16px;
    margin-bottom: 28px;
  }
  .cc-cu-filter-row {
    display: flex;
    flex-wrap: wrap;
    gap: 8px;
    align-items: center;
  }
  .cc-cu-filter-label {
    font-family: var(--cc-font-mono), monospace;
    font-size: 10px;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
    min-width: 78px;
  }
  .cc-cu-filter-chip {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    padding: 7px 12px;
    border-radius: 999px;
    border: 1px solid var(--cc-ink-faint);
    background: rgba(255, 255, 255, 0.02);
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.1em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
    cursor: pointer;
    transition: border-color 0.15s ease, background 0.15s ease, color 0.15s ease;
  }
  .cc-cu-filter-chip:hover {
    color: var(--cc-ink);
    border-color: rgba(245, 241, 234, 0.3);
  }
  .cc-cu-filter-chip.is-active {
    color: #0c1322;
    background: var(--cc-ink);
    border-color: var(--cc-ink);
  }
  .cc-cu-filter-count {
    margin-left: auto;
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.14em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
  }

  /* ===== 06 Architect call CTA (glow band) ===== */
  .cc-cu-architect-inner {
    max-width: 880px;
    margin: 0 auto;
    text-align: center;
  }
  .cc-cu-architect-inner h2 {
    font-size: clamp(30px, 3.8vw, 50px);
    margin: 12px auto 14px;
    line-height: 1.05;
    max-width: 22ch;
  }
  .cc-cu-architect-inner p {
    font-size: clamp(15px, 1.1vw, 17px);
    line-height: 1.6;
    color: var(--cc-ink-dim);
    margin: 0 auto 28px;
    max-width: 56ch;
    text-wrap: pretty;
  }
  .cc-cu-architect-inner .cc-cta-row {
    justify-content: center;
  }

  /* ===== 07 Related links (tinted band, no card chrome) ===== */
  .cc-cu-related-inner {
    --cc-ink: #18121d;
    --cc-ink-dim: rgba(24, 18, 29, 0.62);
    --cc-ink-faint: rgba(24, 18, 29, 0.14);
    --cc-heading-text-color: #18121d;
    color: var(--cc-ink);
    max-width: 1180px;
    margin: 0 auto;
  }
  .cc-cu-related-grid {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 0;
    border-top: 1px solid var(--cc-ink-faint);
  }
  @media (max-width: 880px) {
    .cc-cu-related-grid {
      grid-template-columns: 1fr;
    }
  }
  .cc-cu-related-row {
    display: flex;
    flex-direction: column;
    gap: 8px;
    padding: 24px 24px 24px 0;
    border-bottom: 1px solid var(--cc-ink-faint);
    text-decoration: none;
    color: inherit;
    transition: padding-left 0.15s ease;
  }
  .cc-cu-related-row + .cc-cu-related-row {
    padding-left: 24px;
    border-left: 1px solid var(--cc-ink-faint);
  }
  @media (max-width: 880px) {
    .cc-cu-related-row + .cc-cu-related-row {
      padding-left: 0;
      border-left: none;
    }
  }
  .cc-cu-related-row:hover {
    background: rgba(24, 18, 29, 0.04);
  }
  .cc-cu-related-eyebrow {
    font-family: var(--cc-font-mono), monospace;
    font-size: 10px;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
  }
  .cc-cu-related-title {
    font-family: var(--cc-font-sans), sans-serif;
    font-size: 18px;
    font-weight: 500;
    letter-spacing: -0.015em;
    color: var(--cc-ink);
    margin: 0;
    line-height: 1.3;
  }
  .cc-cu-related-body {
    font-size: 14px;
    line-height: 1.55;
    color: var(--cc-ink-dim);
    margin: 0;
    flex: 1;
    text-wrap: pretty;
  }
  .cc-cu-related-link {
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.16em;
    text-transform: uppercase;
    color: var(--cc-ink);
    margin-top: auto;
  }

  /* ============================================================
   *                     DETAIL PAGE
   * ============================================================ */

  /* ===== Story header ===== */
  .cc-csd-header {
    padding-top: 140px;
    padding-bottom: 56px;
  }
  .cc-csd-header-inner {
    max-width: 1180px;
    margin: 0 auto;
    text-align: center;
  }
  .cc-csd-back {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
    text-decoration: none;
    margin-bottom: 20px;
    transition: color 0.15s ease;
  }
  .cc-csd-back:hover {
    color: var(--cc-ink);
  }
  .cc-csd-header h1 {
    font-size: clamp(34px, 4.6vw, 60px);
    margin: 14px auto 22px;
    max-width: 24ch;
    text-wrap: balance;
    line-height: 1.05;
  }
  .cc-csd-header h1 .accent {
    background: linear-gradient(
      120deg,
      var(--cc-col-shi),
      var(--cc-col-usr) 60%,
      var(--cc-col-cat)
    );
    -webkit-background-clip: text;
    background-clip: text;
    -webkit-text-fill-color: transparent;
  }
  .cc-csd-header-sub {
    font-size: clamp(15px, 1.2vw, 18px);
    line-height: 1.55;
    color: var(--cc-ink-dim);
    margin: 0 auto 36px;
    max-width: 60ch;
    text-wrap: pretty;
  }
  .cc-csd-hero-diagram {
    max-width: 880px;
    margin: 0 auto;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 20px;
    background: linear-gradient(
      180deg,
      rgba(14, 22, 38, 0.7),
      rgba(10, 17, 30, 0.7)
    );
    padding: 32px;
    aspect-ratio: 16 / 7;
  }
  .cc-csd-hero-diagram svg {
    width: 100%;
    height: 100%;
  }

  /* ===== Story body + sidebar layout ===== */
  .cc-csd-body-section {
    padding-top: 32px;
    padding-bottom: 96px;
  }
  .cc-csd-body-inner {
    max-width: 1180px;
    margin: 0 auto;
    display: grid;
    grid-template-columns: minmax(0, 1fr) 320px;
    gap: 56px;
    align-items: start;
  }
  @media (max-width: 980px) {
    .cc-csd-body-inner {
      grid-template-columns: 1fr;
      gap: 40px;
    }
  }
  .cc-csd-body-main {
    min-width: 0;
    max-width: 720px;
    font-size: 17px;
    line-height: 1.7;
    color: var(--cc-ink);
  }
  .cc-csd-body-main h2 {
    font-size: clamp(22px, 2.4vw, 30px);
    font-weight: 500;
    letter-spacing: -0.02em;
    color: var(--cc-ink);
    margin: 56px 0 18px;
    line-height: 1.2;
  }
  .cc-csd-body-main h2:first-child {
    margin-top: 0;
  }
  .cc-csd-body-main p {
    margin: 0 0 18px;
    color: var(--cc-ink);
    text-wrap: pretty;
  }
  .cc-csd-body-main p strong {
    color: var(--cc-ink);
    font-weight: 600;
    background: linear-gradient(
      120deg,
      rgba(140, 180, 230, 0.12),
      rgba(200, 160, 230, 0.12)
    );
    padding: 1px 5px;
    border-radius: 4px;
  }

  /* ===== Pull quote ===== */
  .cc-csd-pullquote {
    margin: 32px 0;
    padding: 20px 28px;
    border-left: 2px solid var(--cc-col-bil);
    background: rgba(255, 255, 255, 0.025);
    border-radius: 0 12px 12px 0;
  }
  .cc-csd-pullquote-text {
    font-family: var(--cc-font-sans), sans-serif;
    font-size: clamp(18px, 1.7vw, 22px);
    font-style: italic;
    font-weight: 500;
    color: var(--cc-ink);
    line-height: 1.4;
    letter-spacing: -0.01em;
    margin: 0 0 14px;
    text-wrap: pretty;
  }
  .cc-csd-pullquote-attribution {
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.14em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
  }

  /* ===== AtAGlance sidebar ===== */
  .cc-csd-sidebar {
    position: sticky;
    top: 120px;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 18px;
    background: linear-gradient(
      180deg,
      rgba(14, 22, 38, 0.7),
      rgba(10, 17, 30, 0.7)
    );
    padding: 28px;
    display: flex;
    flex-direction: column;
    gap: 18px;
  }
  @media (max-width: 980px) {
    .cc-csd-sidebar {
      position: relative;
      top: 0;
    }
  }
  .cc-csd-sidebar-title {
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    color: var(--cc-ink);
    padding-bottom: 14px;
    border-bottom: 1px solid var(--cc-ink-faint);
  }
  .cc-csd-sidebar-row {
    display: flex;
    flex-direction: column;
    gap: 5px;
  }
  .cc-csd-sidebar-row .label {
    font-family: var(--cc-font-mono), monospace;
    font-size: 10px;
    letter-spacing: 0.16em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
  }
  .cc-csd-sidebar-row .value {
    font-size: 14px;
    color: var(--cc-ink);
    line-height: 1.45;
  }
  .cc-csd-sidebar-chips {
    display: flex;
    flex-wrap: wrap;
    gap: 6px;
  }
  .cc-csd-sidebar-chip {
    font-family: var(--cc-font-mono), monospace;
    font-size: 10px;
    letter-spacing: 0.08em;
    text-transform: uppercase;
    color: var(--cc-ink);
    padding: 4px 8px;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 6px;
    background: rgba(255, 255, 255, 0.025);
  }
  .cc-csd-sidebar-divider {
    margin: 6px -2px 0;
    height: 1px;
    background: var(--cc-ink-faint);
  }
  .cc-csd-sidebar-metrics {
    display: flex;
    flex-direction: column;
    gap: 14px;
  }
  .cc-csd-sidebar-metric .value {
    font-family: var(--cc-font-sans), sans-serif;
    font-size: 24px;
    font-weight: 500;
    letter-spacing: -0.02em;
    line-height: 1;
    background: linear-gradient(
      120deg,
      var(--cc-col-shi),
      var(--cc-col-usr) 60%,
      var(--cc-col-cat)
    );
    -webkit-background-clip: text;
    background-clip: text;
    -webkit-text-fill-color: transparent;
    margin-bottom: 4px;
  }
  .cc-csd-sidebar-metric .label {
    font-family: var(--cc-font-mono), monospace;
    font-size: 10px;
    letter-spacing: 0.14em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
    line-height: 1.4;
  }
  .cc-csd-sidebar-cta {
    margin-top: 6px;
    padding: 12px 16px;
    border-radius: 10px;
    background: rgba(255, 255, 255, 0.025);
    border: 1px solid var(--cc-ink-faint);
    text-align: center;
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.16em;
    text-transform: uppercase;
    color: var(--cc-ink);
    text-decoration: none;
    transition: border-color 0.15s ease, background 0.15s ease;
  }
  .cc-csd-sidebar-cta:hover {
    border-color: rgba(245, 241, 234, 0.32);
    background: rgba(255, 255, 255, 0.05);
  }

  /* ===== Detail-page bottom CTA ===== */
  .cc-csd-cta {
    padding-top: 0;
    padding-bottom: 96px;
  }
  .cc-csd-cta-inner {
    max-width: 880px;
    margin: 0 auto;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 22px;
    background: rgba(255, 255, 255, 0.025);
    padding: 48px 44px;
    text-align: center;
  }
  .cc-csd-cta-inner h2 {
    font-size: clamp(28px, 3.4vw, 42px);
    margin: 8px auto 14px;
    max-width: 22ch;
    line-height: 1.1;
  }
  .cc-csd-cta-inner p {
    font-size: 16px;
    color: var(--cc-ink-dim);
    margin: 0 auto 28px;
    max-width: 50ch;
    line-height: 1.55;
    text-wrap: pretty;
  }

  /* ===== Related stories rail ===== */
  .cc-csd-related {
    padding-top: 0;
    padding-bottom: 140px;
  }
  .cc-csd-related-inner {
    max-width: 1280px;
    margin: 0 auto;
  }
`;
