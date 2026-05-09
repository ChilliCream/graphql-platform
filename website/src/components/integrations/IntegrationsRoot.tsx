"use client";

import styled from "styled-components";

// IntegrationsRoot owns the dark navy / cream-ink design tokens for the
// /integrations index and /integrations/[slug] detail pages. Same shape as
// TemplatesRoot, EnterpriseRoot, ObservabilityRoot, PricingRoot. The
// section-specific CSS lives below, scoped by class prefix:
//   .cc-in-*       integrations index sections
//   .cc-ind-*      integrations detail sections
export const IntegrationsRoot = styled.div`
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

  /* The Band variant="tinted" primitive paints a cream surface and sets
   * its color to var(--cc-ink). On this dark page --cc-ink is also cream, so
   * we re-bind the ink tokens to dark values inside any band tagged with
   * .cc-in-tinted-band. The Spotlight accent band re-binds similarly so the
   * accent wash carries dark ink. The global h1-h6 rule binds to
   * --cc-heading-text-color (see global-style.tsx), so override that too. */
  .cc-in-tinted-band {
    --cc-ink: #0c1322;
    --cc-ink-dim: rgba(12, 19, 34, 0.7);
    --cc-ink-faint: rgba(12, 19, 34, 0.16);
    --cc-heading-text-color: #0c1322;
    color: var(--cc-ink);
  }
  .cc-in-tinted-band .display,
  .cc-in-tinted-band h1,
  .cc-in-tinted-band h2,
  .cc-in-tinted-band h3 {
    color: var(--cc-ink);
  }

  /* ===== Section shell ===== */
  section.cc-in-section,
  section.cc-ind-section {
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
  .cc-in-hero {
    padding-top: 160px;
    padding-bottom: 48px;
    text-align: center;
  }
  .cc-in-hero-inner {
    max-width: 980px;
    margin: 0 auto;
  }
  .cc-in-hero .kicker {
    display: inline-block;
    padding: 6px 12px;
    border-radius: 999px;
    border: 1px solid var(--cc-ink-faint);
    background: rgba(255, 255, 255, 0.025);
    margin-bottom: 24px;
  }
  .cc-in-hero h1 {
    font-size: clamp(38px, 5.6vw, 78px);
    margin: 0 0 20px;
    line-height: 1.04;
    text-wrap: balance;
  }
  .cc-in-hero h1 .accent {
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
  .cc-in-hero p {
    font-size: clamp(15px, 1.2vw, 19px);
    line-height: 1.55;
    color: var(--cc-ink-dim);
    max-width: 60ch;
    margin: 0 auto 28px;
    text-wrap: pretty;
  }
  .cc-in-hero-controls {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 16px;
    margin-top: 4px;
  }
  .cc-in-typepills {
    display: inline-flex;
    flex-wrap: wrap;
    gap: 8px;
    padding: 6px;
    border-radius: 999px;
    border: 1px solid var(--cc-ink-faint);
    background: rgba(255, 255, 255, 0.025);
  }
  .cc-in-typepill {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    padding: 8px 16px;
    border-radius: 999px;
    border: none;
    background: transparent;
    color: var(--cc-ink-dim);
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.14em;
    text-transform: uppercase;
    cursor: pointer;
    transition: color 0.15s ease, background 0.15s ease;
    text-decoration: none;
  }
  .cc-in-typepill:hover {
    color: var(--cc-ink);
  }
  .cc-in-typepill.is-active {
    background: var(--cc-ink);
    color: #0c1322;
  }
  .cc-in-typepill .count {
    font-size: 10px;
    opacity: 0.7;
  }
  .cc-in-search {
    display: inline-flex;
    align-items: center;
    gap: 10px;
    width: min(560px, 92%);
    padding: 12px 18px;
    border-radius: 999px;
    border: 1px solid var(--cc-ink-faint);
    background: rgba(255, 255, 255, 0.03);
    transition: border-color 0.15s ease, background 0.15s ease;
  }
  .cc-in-search:focus-within {
    border-color: rgba(245, 241, 234, 0.4);
    background: rgba(255, 255, 255, 0.06);
  }
  .cc-in-search svg {
    flex: 0 0 16px;
    color: var(--cc-ink-dim);
  }
  .cc-in-search input {
    flex: 1;
    border: none;
    outline: none;
    background: transparent;
    color: var(--cc-ink);
    font-family: var(--cc-font-sans), sans-serif;
    font-size: 15px;
    line-height: 1.4;
  }
  .cc-in-search input::placeholder {
    color: var(--cc-ink-dim);
  }
  .cc-in-search-clear {
    background: transparent;
    border: none;
    cursor: pointer;
    color: var(--cc-ink-dim);
    font-family: var(--cc-font-mono), monospace;
    font-size: 10px;
    letter-spacing: 0.14em;
    text-transform: uppercase;
    transition: color 0.15s ease;
  }
  .cc-in-search-clear:hover {
    color: var(--cc-ink);
  }

  /* ===== 02 Spotlight =====
   * Renders inside a full-bleed accent Band. Two-column layout: copy left,
   * orbital diagram right. Model providers orbit the central MCP endpoint;
   * the IDE-client strip below the headline is the per-client setup row. */
  .cc-in-spotlight-grid {
    display: grid;
    grid-template-columns: minmax(0, 1.05fr) minmax(0, 0.95fr);
    gap: clamp(28px, 4vw, 64px);
    align-items: center;
    max-width: 1280px;
    margin: 0 auto;
  }
  @media (max-width: 980px) {
    .cc-in-spotlight-grid {
      grid-template-columns: 1fr;
      gap: 32px;
    }
  }
  .cc-in-spotlight-copy {
    display: flex;
    flex-direction: column;
    align-items: flex-start;
    min-width: 0;
  }
  .cc-in-spotlight-orbital {
    position: relative;
    aspect-ratio: 4 / 3;
    width: 100%;
    color: var(--cc-ink);
  }
  .cc-in-spotlight-orbital-bg {
    position: absolute;
    inset: 0;
    color: var(--cc-ink-faint);
  }
  .cc-in-spotlight-orbital-bg svg {
    position: absolute;
    inset: 0;
    width: 100%;
    height: 100%;
  }
  .cc-in-spotlight-orbital-marks {
    position: absolute;
    inset: 0;
  }
  .cc-in-spotlight-orbital-mark {
    position: absolute;
    transform: translate(-50%, -50%);
    display: flex;
    align-items: center;
    gap: 8px;
    font-family: var(--cc-font-sans), sans-serif;
    font-size: 12px;
    color: var(--cc-ink);
    background: rgba(8, 14, 26, 0.7);
    border: 1px solid var(--cc-ink-faint);
    padding: 6px 10px 6px 6px;
    border-radius: 999px;
    backdrop-filter: blur(4px);
    white-space: nowrap;
  }
  .cc-in-spotlight-orbital-mono {
    width: 22px;
    height: 22px;
    border-radius: 6px;
    background: var(--cc-accent, var(--cc-ink));
    color: #0c1322;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    font-family: var(--cc-font-sans), sans-serif;
    font-weight: 700;
    font-size: 12px;
    line-height: 1;
  }
  .cc-in-spotlight-orbital-name {
    font-weight: 500;
    letter-spacing: -0.005em;
  }
  .cc-in-spotlight-orbital-core {
    position: absolute;
    left: 50%;
    top: 50%;
    transform: translate(-50%, -50%);
    width: 64px;
    height: 64px;
    border-radius: 50%;
    background: linear-gradient(
      140deg,
      var(--cc-accent, #f5f1ea),
      rgba(8, 14, 26, 0.9)
    );
    display: inline-flex;
    align-items: center;
    justify-content: center;
    font-family: var(--cc-font-mono), monospace;
    font-size: 13px;
    font-weight: 600;
    letter-spacing: 0.08em;
    color: #0c1322;
    box-shadow: 0 12px 32px -16px var(--cc-accent-glow, rgba(0, 0, 0, 0.6)),
      inset 0 1px 0 rgba(255, 255, 255, 0.18);
  }
  .cc-in-spotlight-copy h2 {
    font-size: clamp(28px, 3.6vw, 44px);
    margin: 14px 0;
    max-width: 22ch;
    line-height: 1.05;
    letter-spacing: -0.025em;
    text-wrap: balance;
  }
  .cc-in-spotlight-copy p {
    font-size: clamp(14px, 1.1vw, 17px);
    line-height: 1.55;
    color: var(--cc-ink-dim);
    margin: 0 0 24px;
    max-width: 56ch;
    text-wrap: pretty;
  }
  .cc-in-spotlight-copy .cc-in-spotlight-eyebrow {
    margin-bottom: 16px;
  }
  .cc-in-spotlight-copy .cc-in-spotlight-clients {
    margin-bottom: 22px;
  }

  /* ===== 02 Spotlight (legacy card layout) ===== */
  .cc-in-spotlight {
    padding-top: 24px;
    padding-bottom: 56px;
  }
  .cc-in-spotlight-inner {
    max-width: 1320px;
    margin: 0 auto;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 22px;
    background: linear-gradient(
        140deg,
        rgba(80, 60, 200, 0.18) 0%,
        rgba(40, 70, 180, 0.08) 45%,
        rgba(8, 14, 26, 0.6) 100%
      ),
      rgba(8, 14, 26, 0.85);
    padding: 48px 48px 44px;
    overflow: hidden;
    position: relative;
  }
  .cc-in-spotlight-inner::before {
    content: "";
    position: absolute;
    top: -40%;
    right: -10%;
    width: 60%;
    height: 160%;
    background: radial-gradient(
      circle,
      rgba(160, 120, 220, 0.18),
      transparent 60%
    );
    pointer-events: none;
  }
  @media (max-width: 880px) {
    .cc-in-spotlight-inner {
      padding: 36px 28px 32px;
    }
  }
  .cc-in-spotlight-eyebrow {
    display: inline-block;
    padding: 5px 10px;
    border-radius: 999px;
    border: 1px solid rgba(245, 241, 234, 0.22);
    background: rgba(245, 241, 234, 0.05);
    font-family: var(--cc-font-mono), monospace;
    font-size: 10px;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    color: var(--cc-ink);
    margin-bottom: 18px;
    position: relative;
  }
  .cc-in-spotlight h2 {
    font-size: clamp(28px, 3.6vw, 44px);
    margin: 0 0 14px;
    max-width: 24ch;
    line-height: 1.05;
    letter-spacing: -0.025em;
    text-wrap: balance;
    position: relative;
  }
  .cc-in-spotlight p {
    font-size: clamp(14px, 1.1vw, 17px);
    line-height: 1.55;
    color: var(--cc-ink-dim);
    margin: 0 0 28px;
    max-width: 56ch;
    text-wrap: pretty;
    position: relative;
  }
  .cc-in-spotlight-clients {
    display: flex;
    flex-wrap: wrap;
    gap: 10px;
    margin-bottom: 24px;
    position: relative;
  }
  .cc-in-spotlight-client {
    display: inline-flex;
    align-items: center;
    gap: 10px;
    padding: 10px 14px;
    border-radius: 12px;
    border: 1px solid rgba(245, 241, 234, 0.18);
    background: rgba(245, 241, 234, 0.03);
    text-decoration: none;
    color: var(--cc-ink);
    transition: border-color 0.15s ease, background 0.15s ease;
  }
  .cc-in-spotlight-client:hover {
    border-color: rgba(245, 241, 234, 0.35);
    background: rgba(245, 241, 234, 0.07);
  }
  .cc-in-spotlight-client-mono {
    width: 32px;
    height: 32px;
    border-radius: 8px;
    border: 1px solid rgba(245, 241, 234, 0.22);
    display: flex;
    align-items: center;
    justify-content: center;
    color: var(--cc-ink);
    background: rgba(245, 241, 234, 0.04);
  }
  .cc-in-spotlight-client-name {
    font-family: var(--cc-font-sans), sans-serif;
    font-size: 13px;
    font-weight: 500;
    letter-spacing: -0.005em;
  }
  .cc-in-spotlight-cta {
    display: inline-flex;
    align-items: center;
    gap: 10px;
    padding: 12px 22px;
    border-radius: 999px;
    background: var(--cc-ink);
    color: #0c1322;
    font-family: var(--cc-font-sans), sans-serif;
    font-size: 14px;
    font-weight: 500;
    text-decoration: none;
    transition: transform 0.12s ease;
    position: relative;
  }
  .cc-in-spotlight-cta:hover {
    transform: translateY(-1px);
  }

  /* ===== 03 Catalogue (rail + sections) ===== */
  .cc-in-catalogue {
    padding-top: 24px;
    padding-bottom: 80px;
  }
  .cc-in-catalogue-inner {
    max-width: 1320px;
    margin: 0 auto;
    display: grid;
    grid-template-columns: 240px minmax(0, 1fr);
    gap: 56px;
    align-items: start;
  }
  @media (max-width: 1080px) {
    .cc-in-catalogue-inner {
      grid-template-columns: 1fr;
      gap: 24px;
    }
  }
  .cc-in-rail {
    position: sticky;
    top: 120px;
    display: flex;
    flex-direction: column;
    gap: 4px;
    padding: 22px 18px;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 18px;
    background: rgba(255, 255, 255, 0.025);
  }
  @media (max-width: 1080px) {
    .cc-in-rail {
      position: relative;
      top: 0;
      flex-direction: row;
      flex-wrap: wrap;
      gap: 8px;
      padding: 16px;
    }
  }
  .cc-in-rail-title {
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
    padding-bottom: 12px;
    margin-bottom: 6px;
    border-bottom: 1px solid var(--cc-ink-faint);
    width: 100%;
  }
  @media (max-width: 1080px) {
    .cc-in-rail-title {
      flex: 1 1 100%;
      padding-bottom: 8px;
      margin-bottom: 4px;
    }
  }
  .cc-in-rail-link {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 8px;
    padding: 9px 12px;
    border-radius: 10px;
    font-family: var(--cc-font-sans), sans-serif;
    font-size: 13px;
    color: var(--cc-ink-dim);
    text-decoration: none;
    border: 1px solid transparent;
    background: transparent;
    transition: color 0.15s ease, background 0.15s ease, border-color 0.15s ease;
    cursor: pointer;
    width: 100%;
    text-align: left;
  }
  .cc-in-rail-link:hover {
    color: var(--cc-ink);
    background: rgba(255, 255, 255, 0.03);
  }
  .cc-in-rail-link .rail-count {
    font-family: var(--cc-font-mono), monospace;
    font-size: 10px;
    color: var(--cc-ink-dim);
    opacity: 0.8;
  }
  @media (max-width: 1080px) {
    .cc-in-rail-link {
      width: auto;
      padding: 8px 12px;
      border: 1px solid var(--cc-ink-faint);
      border-radius: 999px;
      background: rgba(255, 255, 255, 0.02);
    }
  }

  /* ===== Category section =====
   * The first visible category renders with .is-marquee: a darker inset
   * surface, accent top-rule, and a "Marquee category" eyebrow. This is the
   * page's one inverted-band moment without breaking the sticky-rail layout. */
  .cc-in-cat-stack {
    display: flex;
    flex-direction: column;
    gap: 56px;
  }
  .cc-in-cat-block {
    scroll-margin-top: 100px;
  }
  .cc-in-cat-block.is-marquee {
    position: relative;
    background: rgba(8, 12, 22, 0.7);
    border: 1px solid var(--cc-accent-line, var(--cc-ink-faint));
    border-radius: 18px;
    padding: 28px 28px 32px;
    box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.04);
  }
  .cc-in-cat-block.is-marquee::before {
    content: "";
    position: absolute;
    top: 0;
    left: 28px;
    right: 28px;
    height: 2px;
    background: var(--cc-accent-gradient, var(--cc-accent, var(--cc-ink)));
    border-radius: 2px;
  }
  .cc-in-cat-block.is-marquee .cc-in-cat-head {
    border-bottom-color: var(--cc-accent-line, var(--cc-ink-faint));
  }
  .cc-in-cat-block.is-marquee .cc-in-cat-head .eyebrow {
    color: var(--cc-accent, var(--cc-ink));
  }
  .cc-in-cat-head {
    display: flex;
    align-items: flex-end;
    justify-content: space-between;
    gap: 24px;
    margin-bottom: 22px;
    padding-bottom: 14px;
    border-bottom: 1px solid var(--cc-ink-faint);
  }
  .cc-in-cat-head h2 {
    font-size: clamp(22px, 2.4vw, 30px);
    font-weight: 500;
    letter-spacing: -0.02em;
    color: var(--cc-ink);
    margin: 0;
    line-height: 1.15;
  }
  .cc-in-cat-head p {
    font-size: 14px;
    color: var(--cc-ink-dim);
    margin: 4px 0 0;
    line-height: 1.55;
    max-width: 60ch;
  }
  .cc-in-cat-head .browse {
    flex: 0 0 auto;
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.16em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
    text-decoration: none;
    transition: color 0.15s ease;
    white-space: nowrap;
  }
  .cc-in-cat-head .browse:hover {
    color: var(--cc-ink);
  }

  /* ===== Featured section ===== */
  .cc-in-featured {
    padding-top: 0;
    padding-bottom: 72px;
  }
  .cc-in-featured-inner {
    max-width: 1320px;
    margin: 0 auto;
  }
  .cc-in-featured-head {
    display: flex;
    align-items: flex-end;
    justify-content: space-between;
    gap: 24px;
    margin-bottom: 22px;
  }
  .cc-in-featured-head h2 {
    font-size: clamp(24px, 2.6vw, 32px);
    font-weight: 500;
    letter-spacing: -0.025em;
    color: var(--cc-ink);
    margin: 4px 0 0;
    line-height: 1.1;
  }
  .cc-in-featured-head .eyebrow {
    display: block;
  }

  /* ===== Card grids ===== */
  .cc-in-grid {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 18px;
  }
  @media (max-width: 1100px) {
    .cc-in-grid {
      grid-template-columns: repeat(2, 1fr);
    }
  }
  @media (max-width: 640px) {
    .cc-in-grid {
      grid-template-columns: 1fr;
    }
  }
  .cc-in-grid.is-dense {
    grid-template-columns: repeat(4, 1fr);
    gap: 12px;
  }
  @media (max-width: 1240px) {
    .cc-in-grid.is-dense {
      grid-template-columns: repeat(3, 1fr);
    }
  }
  @media (max-width: 880px) {
    .cc-in-grid.is-dense {
      grid-template-columns: repeat(2, 1fr);
    }
  }
  @media (max-width: 520px) {
    .cc-in-grid.is-dense {
      grid-template-columns: 1fr;
    }
  }

  /* ===== Integration card =====
   * Two visual registers:
   *  .is-native    larger filled monogram with per-integration accent fill,
   *                left-edge accent stripe via --cc-card-edge, slightly
   *                stronger surface elevation. Reads as a real partner tile.
   *  .is-community stroked monogram on a faint background, denser when also
   *                .is-dense. No accent; the lack of color IS the trust
   *                tier signal. */
  .cc-in-card {
    position: relative;
    display: flex;
    flex-direction: column;
    gap: 14px;
    padding: 22px 22px 20px;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 16px;
    background: rgba(255, 255, 255, 0.025);
    text-decoration: none;
    color: inherit;
    transition: border-color 0.18s ease, transform 0.18s ease,
      background 0.18s ease, box-shadow 0.18s ease;
    overflow: hidden;
  }
  .cc-in-card:hover {
    border-color: rgba(245, 241, 234, 0.32);
    transform: translateY(-2px);
    background: rgba(255, 255, 255, 0.045);
    box-shadow: 0 30px 60px -40px rgba(0, 0, 0, 0.7),
      0 0 0 1px rgba(245, 241, 234, 0.07);
  }
  .cc-in-card.is-native {
    padding-left: 26px;
    background: linear-gradient(
      135deg,
      rgba(255, 255, 255, 0.045),
      rgba(255, 255, 255, 0.02) 60%
    );
  }
  .cc-in-card.is-native::before {
    content: "";
    position: absolute;
    top: 14px;
    bottom: 14px;
    left: 0;
    width: 3px;
    border-radius: 0 3px 3px 0;
    background: var(--cc-card-edge, var(--cc-accent, var(--cc-ink-faint)));
  }
  .cc-in-card.is-community {
    border-color: rgba(245, 241, 234, 0.1);
    background: rgba(255, 255, 255, 0.018);
  }
  .cc-in-card.is-community:hover {
    border-color: rgba(245, 241, 234, 0.22);
    background: rgba(255, 255, 255, 0.035);
  }
  .cc-in-card.is-dense {
    padding: 14px 14px 12px;
    gap: 10px;
    border-radius: 12px;
  }
  .cc-in-card-head {
    display: flex;
    align-items: flex-start;
    justify-content: space-between;
    gap: 12px;
  }
  .cc-in-card-mono {
    width: 48px;
    height: 48px;
    border-radius: 12px;
    border: 1.5px solid var(--cc-ink-faint);
    display: flex;
    align-items: center;
    justify-content: center;
    color: var(--cc-ink);
    background: rgba(245, 241, 234, 0.04);
    flex: 0 0 auto;
  }
  .cc-in-card-mono.is-filled {
    width: 56px;
    height: 56px;
    border-radius: 14px;
    border: none;
    background: var(--cc-card-mono-fill, var(--cc-ink));
    box-shadow: 0 8px 22px -12px var(--cc-card-edge, rgba(0, 0, 0, 0.6)),
      inset 0 1px 0 rgba(255, 255, 255, 0.12);
  }
  .cc-in-card.is-dense .cc-in-card-mono {
    width: 36px;
    height: 36px;
    border-radius: 9px;
  }
  .cc-in-card-eyebrow {
    font-family: var(--cc-font-mono), monospace;
    font-size: 10px;
    letter-spacing: 0.16em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
    text-align: right;
    line-height: 1.4;
    max-width: 120px;
  }
  .cc-in-card.is-dense .cc-in-card-eyebrow {
    font-size: 9px;
  }
  .cc-in-card-name {
    font-family: var(--cc-font-sans), sans-serif;
    font-size: 17px;
    font-weight: 500;
    letter-spacing: -0.015em;
    color: var(--cc-ink);
    margin: 0;
    line-height: 1.25;
  }
  .cc-in-card.is-dense .cc-in-card-name {
    font-size: 15px;
  }
  .cc-in-card-tagline {
    font-size: 13px;
    line-height: 1.5;
    color: var(--cc-ink-dim);
    margin: 0;
    text-wrap: pretty;
    flex: 1;
  }
  .cc-in-card.is-dense .cc-in-card-tagline {
    font-size: 12px;
    line-height: 1.45;
  }
  .cc-in-card-foot {
    display: flex;
    align-items: center;
    gap: 8px;
    padding-top: 10px;
    border-top: 1px solid var(--cc-ink-faint);
  }
  .cc-in-typebadge {
    display: inline-flex;
    align-items: center;
    gap: 5px;
    font-family: var(--cc-font-mono), monospace;
    font-size: 10px;
    letter-spacing: 0.14em;
    text-transform: uppercase;
    line-height: 1;
    padding: 4px 8px;
    border-radius: 6px;
  }
  .cc-in-typebadge.is-native {
    color: #0c1322;
    background: var(--cc-ink);
  }
  .cc-in-typebadge.is-community {
    color: var(--cc-ink-dim);
    border: 1px solid var(--cc-ink-faint);
  }
  .cc-in-card.is-community .cc-in-card-eyebrow {
    font-family: var(--cc-font-mono), monospace;
  }
  .cc-in-card-product {
    font-family: var(--cc-font-mono), monospace;
    font-size: 9px;
    letter-spacing: 0.14em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
  }
  .cc-in-card-readlink {
    margin-left: auto;
    font-family: var(--cc-font-mono), monospace;
    font-size: 10px;
    letter-spacing: 0.14em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
    transition: color 0.15s ease, transform 0.15s ease;
  }
  .cc-in-card:hover .cc-in-card-readlink {
    color: var(--cc-ink);
    transform: translateX(2px);
  }
  .cc-in-card.is-dense .cc-in-card-readlink {
    display: none;
  }

  /* ===== Native + Community sections ===== */
  .cc-in-typesection {
    padding-top: 0;
    padding-bottom: 80px;
  }
  .cc-in-typesection-inner {
    max-width: 1320px;
    margin: 0 auto;
  }
  .cc-in-typesection-head {
    display: flex;
    align-items: flex-end;
    justify-content: space-between;
    gap: 24px;
    margin-bottom: 24px;
    padding-bottom: 14px;
    border-bottom: 1px solid var(--cc-ink-faint);
  }
  .cc-in-typesection-head h2 {
    font-size: clamp(26px, 3vw, 38px);
    font-weight: 500;
    letter-spacing: -0.028em;
    color: var(--cc-ink);
    margin: 6px 0 0;
    line-height: 1.05;
  }
  .cc-in-typesection-head p {
    font-size: 14px;
    color: var(--cc-ink-dim);
    margin: 6px 0 0;
    line-height: 1.55;
    max-width: 60ch;
  }
  .cc-in-typesection-head .count-pill {
    flex: 0 0 auto;
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.16em;
    text-transform: uppercase;
    color: var(--cc-ink);
    padding: 6px 12px;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 999px;
    background: rgba(245, 241, 234, 0.05);
    white-space: nowrap;
  }

  /* ===== Empty state ===== */
  .cc-in-empty {
    grid-column: 1 / -1;
    border: 1px dashed var(--cc-ink-faint);
    border-radius: 18px;
    padding: 48px 28px;
    text-align: center;
    background: rgba(255, 255, 255, 0.02);
  }
  .cc-in-empty h3 {
    font-size: 20px;
    font-weight: 500;
    letter-spacing: -0.02em;
    color: var(--cc-ink);
    margin: 0 0 8px;
  }
  .cc-in-empty p {
    font-size: 14px;
    color: var(--cc-ink-dim);
    margin: 0 0 18px;
    line-height: 1.55;
  }
  .cc-in-empty button {
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.16em;
    text-transform: uppercase;
    color: var(--cc-ink);
    padding: 10px 18px;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 999px;
    background: rgba(255, 255, 255, 0.025);
    cursor: pointer;
    transition: border-color 0.15s ease, background 0.15s ease;
  }
  .cc-in-empty button:hover {
    border-color: var(--cc-ink);
    background: rgba(255, 255, 255, 0.05);
  }

  /* ===== Starter templates strip ===== */
  .cc-in-starters {
    padding-top: 0;
    padding-bottom: 72px;
  }
  .cc-in-starters-inner {
    max-width: 1320px;
    margin: 0 auto;
  }
  .cc-in-starters-head {
    margin-bottom: 22px;
  }
  .cc-in-starters-head h2 {
    font-size: clamp(24px, 2.6vw, 32px);
    font-weight: 500;
    letter-spacing: -0.025em;
    color: var(--cc-ink);
    margin: 4px 0 6px;
    line-height: 1.1;
  }
  .cc-in-starters-head p {
    font-size: 14px;
    color: var(--cc-ink-dim);
    margin: 0;
    line-height: 1.55;
    max-width: 60ch;
  }
  .cc-in-starters-grid {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 18px;
  }
  @media (max-width: 980px) {
    .cc-in-starters-grid {
      grid-template-columns: 1fr;
    }
  }
  .cc-in-starter {
    display: flex;
    flex-direction: column;
    gap: 12px;
    padding: 24px;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 16px;
    background: rgba(255, 255, 255, 0.025);
    text-decoration: none;
    color: inherit;
    transition: border-color 0.18s ease, background 0.18s ease,
      transform 0.18s ease;
  }
  .cc-in-starter:hover {
    border-color: rgba(245, 241, 234, 0.32);
    background: rgba(255, 255, 255, 0.045);
    transform: translateY(-2px);
  }
  .cc-in-starter .stack {
    font-family: var(--cc-font-mono), monospace;
    font-size: 10px;
    letter-spacing: 0.16em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
  }
  .cc-in-starter h3 {
    font-size: 18px;
    font-weight: 500;
    letter-spacing: -0.015em;
    color: var(--cc-ink);
    margin: 0;
    line-height: 1.25;
  }
  .cc-in-starter p {
    font-size: 13px;
    line-height: 1.55;
    color: var(--cc-ink-dim);
    margin: 0;
    flex: 1;
  }
  .cc-in-starter .cta {
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.16em;
    text-transform: uppercase;
    color: var(--cc-ink);
    margin-top: 4px;
  }

  /* ===== Dual CTA =====
   * Renders inside a tinted Band. With .is-bandlocked the wrapper gains a
   * hairline center rule and the cards drop their border (the band IS the
   * surface; no card-on-card chrome, per uplift-plan CC2). */
  .cc-in-dualcta {
    padding-top: 0;
    padding-bottom: 140px;
  }
  .cc-in-dualcta-inner {
    max-width: 1320px;
    margin: 0 auto;
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 18px;
  }
  @media (max-width: 880px) {
    .cc-in-dualcta-inner {
      grid-template-columns: 1fr;
    }
  }
  .cc-in-dualcta-inner.is-bandlocked {
    gap: 0;
    margin-top: 8px;
  }
  .cc-in-dualcta-inner.is-bandlocked .cc-in-dualcta-card + .cc-in-dualcta-card {
    border-left: 1px solid var(--cc-ink-faint);
  }
  @media (max-width: 880px) {
    .cc-in-dualcta-inner.is-bandlocked
      .cc-in-dualcta-card
      + .cc-in-dualcta-card {
      border-left: none;
      border-top: 1px solid var(--cc-ink-faint);
    }
  }
  .cc-in-dualcta-card {
    padding: 36px 32px;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 18px;
    background: rgba(255, 255, 255, 0.025);
    display: flex;
    flex-direction: column;
    gap: 12px;
    text-decoration: none;
    color: inherit;
    transition: border-color 0.18s ease, background 0.18s ease,
      transform 0.18s ease;
  }
  .cc-in-dualcta-card.is-ghost {
    border: none;
    background: transparent;
    border-radius: 0;
    padding: 28px 32px;
  }
  .cc-in-dualcta-card.is-ghost:hover {
    background: transparent;
    transform: translateY(-1px);
  }
  .cc-in-dualcta-card.is-ghost .cta {
    color: var(--cc-ink);
    transition: transform 0.15s ease;
  }
  .cc-in-dualcta-card.is-ghost:hover .cta {
    transform: translateX(2px);
  }
  .cc-in-dualcta-card:hover {
    border-color: rgba(245, 241, 234, 0.32);
    background: rgba(255, 255, 255, 0.045);
  }
  .cc-in-dualcta-card h3 {
    font-size: clamp(20px, 2.2vw, 26px);
    font-weight: 500;
    letter-spacing: -0.02em;
    color: var(--cc-ink);
    margin: 4px 0 0;
    line-height: 1.15;
  }
  .cc-in-dualcta-card p {
    font-size: 14px;
    color: var(--cc-ink-dim);
    line-height: 1.55;
    margin: 0;
    flex: 1;
  }
  .cc-in-dualcta-card .cta {
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.16em;
    text-transform: uppercase;
    color: var(--cc-ink);
  }

  /* ============================================================
   *                     DETAIL PAGE
   * ============================================================ */

  .cc-ind-header {
    padding-top: 140px;
    padding-bottom: 28px;
  }
  .cc-ind-header-inner {
    max-width: 1180px;
    margin: 0 auto;
  }
  .cc-ind-breadcrumb {
    display: inline-flex;
    align-items: center;
    gap: 8px;
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.16em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
    margin-bottom: 28px;
  }
  .cc-ind-breadcrumb a {
    color: var(--cc-ink-dim);
    text-decoration: none;
    transition: color 0.15s ease;
  }
  .cc-ind-breadcrumb a:hover {
    color: var(--cc-ink);
  }
  .cc-ind-breadcrumb .sep {
    color: var(--cc-ink-faint);
  }
  .cc-ind-breadcrumb .crumb-current {
    color: var(--cc-ink);
  }
  .cc-ind-header-row {
    display: grid;
    grid-template-columns: auto minmax(0, 1fr) auto;
    gap: 24px;
    align-items: center;
  }
  @media (max-width: 720px) {
    .cc-ind-header-row {
      grid-template-columns: auto minmax(0, 1fr);
      grid-template-rows: auto auto;
    }
    .cc-ind-header-row .cc-ind-header-actions {
      grid-column: 1 / -1;
    }
  }
  .cc-ind-header-mono {
    width: 72px;
    height: 72px;
    border-radius: 18px;
    border: 1.5px solid var(--cc-ink-faint);
    display: flex;
    align-items: center;
    justify-content: center;
    color: var(--cc-ink);
    background: rgba(245, 241, 234, 0.04);
  }
  .cc-ind-header-mono.is-filled {
    border: none;
    background: var(--cc-mono-fill, var(--cc-ink));
    box-shadow: 0 14px 28px -16px rgba(0, 0, 0, 0.7),
      inset 0 1px 0 rgba(255, 255, 255, 0.12);
  }
  .cc-ind-header-text h1 {
    font-size: clamp(30px, 4vw, 48px);
    font-weight: 500;
    letter-spacing: -0.03em;
    color: var(--cc-ink);
    margin: 0 0 6px;
    line-height: 1.05;
  }
  .cc-ind-header-text .eyebrow {
    display: block;
    margin-bottom: 6px;
  }
  .cc-ind-header-actions {
    display: inline-flex;
    align-items: center;
    gap: 10px;
    flex-wrap: wrap;
  }
  .cc-ind-header-cta {
    display: inline-flex;
    align-items: center;
    gap: 8px;
    padding: 12px 20px;
    border-radius: 999px;
    font-family: var(--cc-font-sans), sans-serif;
    font-size: 14px;
    font-weight: 500;
    text-decoration: none;
    cursor: pointer;
    transition: transform 0.12s ease, background 0.12s ease,
      border-color 0.12s ease;
  }
  .cc-ind-header-cta.is-primary {
    background: var(--cc-ink);
    color: #0c1322;
    border: 1px solid var(--cc-ink);
  }
  .cc-ind-header-cta.is-primary:hover {
    transform: translateY(-1px);
  }
  .cc-ind-header-cta.is-ghost {
    background: transparent;
    color: var(--cc-ink);
    border: 1px solid var(--cc-ink-faint);
  }
  .cc-ind-header-cta.is-ghost:hover {
    border-color: var(--cc-ink);
  }
  .cc-ind-stars {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    padding: 8px 12px;
    border-radius: 999px;
    border: 1px solid var(--cc-ink-faint);
    background: rgba(245, 241, 234, 0.04);
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.06em;
    color: var(--cc-ink);
    text-decoration: none;
    transition: border-color 0.15s ease, background 0.15s ease;
  }
  .cc-ind-stars:hover {
    border-color: rgba(245, 241, 234, 0.35);
    background: rgba(245, 241, 234, 0.07);
  }

  /* ===== Detail body + sticky sidebar ===== */
  .cc-ind-body-section {
    padding-top: 32px;
    padding-bottom: 96px;
  }
  .cc-ind-body-inner {
    max-width: 1180px;
    margin: 0 auto;
    display: grid;
    grid-template-columns: minmax(0, 1fr) 320px;
    gap: 56px;
    align-items: start;
  }
  @media (max-width: 980px) {
    .cc-ind-body-inner {
      grid-template-columns: 1fr;
      gap: 40px;
    }
  }
  .cc-ind-body-main {
    min-width: 0;
    max-width: 760px;
    font-size: 17px;
    line-height: 1.7;
    color: var(--cc-ink);
  }
  .cc-ind-body-main h2 {
    font-size: clamp(22px, 2.4vw, 30px);
    font-weight: 500;
    letter-spacing: -0.02em;
    color: var(--cc-ink);
    margin: 56px 0 18px;
    line-height: 1.2;
  }
  .cc-ind-body-main h2:first-child {
    margin-top: 0;
  }
  .cc-ind-body-main p {
    margin: 0 0 18px;
    color: var(--cc-ink);
    text-wrap: pretty;
  }

  /* ===== Detail code block ===== */
  .cc-ind-code {
    position: relative;
    margin: 22px 0 24px;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 12px;
    background: rgba(8, 14, 26, 0.7);
    overflow: hidden;
  }
  .cc-ind-code-head {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 10px 14px;
    border-bottom: 1px solid var(--cc-ink-faint);
    background: rgba(255, 255, 255, 0.02);
  }
  .cc-ind-code-lang {
    font-family: var(--cc-font-mono), monospace;
    font-size: 10px;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
  }
  .cc-ind-code-copy {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    font-family: var(--cc-font-mono), monospace;
    font-size: 10px;
    letter-spacing: 0.16em;
    text-transform: uppercase;
    color: var(--cc-ink);
    background: transparent;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 6px;
    padding: 5px 10px;
    cursor: pointer;
    transition: border-color 0.15s ease, background 0.15s ease;
  }
  .cc-ind-code-copy:hover {
    border-color: rgba(245, 241, 234, 0.35);
    background: rgba(255, 255, 255, 0.04);
  }
  .cc-ind-code-copy.is-copied {
    color: var(--cc-col-ord);
    border-color: rgba(118, 200, 150, 0.45);
    background: rgba(118, 200, 150, 0.06);
  }
  .cc-ind-code pre {
    margin: 0;
    padding: 18px 16px;
    overflow-x: auto;
    font-family: var(--cc-font-mono), monospace;
    font-size: 13px;
    line-height: 1.6;
    color: var(--cc-ink);
    text-shadow: 0 0 30px rgba(245, 241, 234, 0.08);
  }
  .cc-ind-code pre code {
    font-family: inherit;
    font-size: inherit;
    color: inherit;
    background: transparent;
    padding: 0;
  }
  .cc-ind-install {
    position: relative;
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 12px;
    padding: 14px 16px;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 10px;
    background: rgba(8, 14, 26, 0.7);
    font-family: var(--cc-font-mono), monospace;
    font-size: 13px;
    color: var(--cc-ink);
    margin: 18px 0 24px;
  }
  .cc-ind-install code {
    flex: 1;
    overflow-x: auto;
    white-space: nowrap;
  }
  .cc-ind-install button {
    flex: 0 0 auto;
    font-family: var(--cc-font-mono), monospace;
    font-size: 10px;
    letter-spacing: 0.16em;
    text-transform: uppercase;
    color: var(--cc-ink);
    padding: 5px 10px;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 6px;
    background: transparent;
    cursor: pointer;
    transition: border-color 0.15s ease, background 0.15s ease;
  }
  .cc-ind-install button:hover {
    border-color: rgba(245, 241, 234, 0.35);
    background: rgba(255, 255, 255, 0.04);
  }
  .cc-ind-install button.is-copied {
    color: var(--cc-col-ord);
    border-color: rgba(118, 200, 150, 0.45);
  }

  /* ===== Sidebar ===== */
  .cc-ind-sidebar {
    position: sticky;
    top: 120px;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 18px;
    background: linear-gradient(
      180deg,
      rgba(14, 22, 38, 0.7),
      rgba(10, 17, 30, 0.7)
    );
    padding: 24px;
    display: flex;
    flex-direction: column;
    gap: 16px;
  }
  @media (max-width: 980px) {
    .cc-ind-sidebar {
      position: relative;
      top: 0;
    }
  }
  .cc-ind-sidebar-row {
    display: flex;
    flex-direction: column;
    gap: 8px;
  }
  .cc-ind-sidebar-row .label {
    font-family: var(--cc-font-mono), monospace;
    font-size: 10px;
    letter-spacing: 0.16em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
  }
  .cc-ind-sidebar-row .value {
    font-size: 14px;
    color: var(--cc-ink);
    line-height: 1.45;
  }
  .cc-ind-sidebar-row .value a {
    color: var(--cc-ink);
    text-decoration: underline;
    text-underline-offset: 3px;
    text-decoration-color: var(--cc-ink-faint);
    transition: text-decoration-color 0.15s ease;
  }
  .cc-ind-sidebar-row .value a:hover {
    text-decoration-color: var(--cc-ink);
  }
  .cc-ind-sidebar-divider {
    height: 1px;
    background: var(--cc-ink-faint);
    margin: 4px -2px;
  }
  .cc-ind-sidebar-tagchips {
    display: flex;
    flex-wrap: wrap;
    gap: 6px;
  }
  .cc-ind-sidebar-tagchip {
    font-family: var(--cc-font-mono), monospace;
    font-size: 10px;
    letter-spacing: 0.08em;
    text-transform: uppercase;
    color: var(--cc-ink);
    padding: 4px 8px;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 6px;
    background: rgba(255, 255, 255, 0.025);
    text-decoration: none;
    transition: border-color 0.15s ease, background 0.15s ease;
  }
  .cc-ind-sidebar-tagchip:hover {
    border-color: rgba(245, 241, 234, 0.35);
    background: rgba(255, 255, 255, 0.05);
  }
  .cc-ind-sidebar-typebadge {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.14em;
    text-transform: uppercase;
    line-height: 1;
    padding: 6px 10px;
    border-radius: 999px;
    width: fit-content;
  }
  .cc-ind-sidebar-typebadge.is-native {
    color: #0c1322;
    background: var(--cc-ink);
  }
  .cc-ind-sidebar-typebadge.is-community {
    color: var(--cc-ink-dim);
    border: 1px solid var(--cc-ink-faint);
  }
  .cc-ind-sidebar-link {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 8px;
    padding: 10px 12px;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 10px;
    background: rgba(255, 255, 255, 0.025);
    text-decoration: none;
    color: var(--cc-ink);
    font-family: var(--cc-font-sans), sans-serif;
    font-size: 13px;
    transition: border-color 0.15s ease, background 0.15s ease;
  }
  .cc-ind-sidebar-link:hover {
    border-color: rgba(245, 241, 234, 0.35);
    background: rgba(255, 255, 255, 0.05);
  }
  .cc-ind-sidebar-link .arrow {
    color: var(--cc-ink-dim);
  }

  /* ===== Related rail ===== */
  .cc-ind-related {
    padding-top: 0;
    padding-bottom: 140px;
  }
  .cc-ind-related-inner {
    max-width: 1280px;
    margin: 0 auto;
  }
  .cc-ind-related-heading {
    text-align: center;
    margin: 0 auto 36px;
  }
  .cc-ind-related-heading h2 {
    font-size: clamp(26px, 3.2vw, 38px);
    margin: 8px auto 0;
    max-width: 22ch;
    line-height: 1.1;
  }
`;
