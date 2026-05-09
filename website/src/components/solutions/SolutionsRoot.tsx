"use client";

import styled from "styled-components";

// SolutionsRoot owns the dark navy / cream-ink design tokens for every
// /solutions/[slug] page. Same approach as CustomersRoot, EnterpriseRoot,
// TemplatesRoot: tokens, section shell, typography and button system are
// shared verbatim. Section CSS lives below, scoped by class prefix:
//   .cc-sl-*       solution page sections
//
// The redesign moves sections from card-stacks to band-stacks: the page
// is a vertical sequence of full-bleed bands (default / tinted / inverted
// / accent / glow) with the page accent threading through. Within each
// band, sections lay out edge-to-edge with whitespace and hairline rules
// instead of a per-section bordered rectangle.
export const SolutionsRoot = styled.div`
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
  --cc-amber: oklch(0.85 0.16 75);
  --cc-pad-x: clamp(28px, 5vw, 96px);
  --cc-font-mono: "JetBrains Mono", ui-monospace, SFMono-Regular, monospace;

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

  /* ===== Section shell: a band-aware wrapper. The Band primitive owns
     the full-bleed surface; this inner section adds the relative
     positioning, the section label slot, and the section-level horizontal
     pad that lifts the label off the band edge. ===== */
  .cc-sl-section {
    position: relative;
    width: 100%;
    padding-left: var(--cc-pad-x);
    padding-right: var(--cc-pad-x);
  }
  .cc-section-label {
    position: absolute;
    top: -8px;
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
  .cc-cta-row.cc-cta-row--left {
    justify-content: flex-start;
  }

  /* ===== Section heading wrapper ===== */
  .cc-sl-heading {
    text-align: center;
    margin: 0 auto 48px;
    max-width: 760px;
  }
  .cc-sl-heading h2 {
    font-size: clamp(32px, 4vw, 50px);
    margin: 8px auto 14px;
    max-width: 22ch;
    line-height: 1.05;
  }
  .cc-sl-heading h2 .accent {
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
  .cc-sl-heading p {
    font-size: clamp(15px, 1.1vw, 17px);
    color: var(--cc-ink-dim);
    max-width: 56ch;
    margin: 0 auto;
    text-wrap: pretty;
    line-height: 1.55;
  }

  /* ===== 01 Hero ===== */
  .cc-sl-hero {
    padding-top: 48px;
  }
  .cc-sl-hero-grid {
    display: grid;
    grid-template-columns: minmax(0, 1.4fr) minmax(0, 1fr);
    gap: clamp(32px, 5vw, 80px);
    align-items: center;
    min-height: clamp(380px, 56vh, 540px);
  }
  @media (max-width: 960px) {
    .cc-sl-hero-grid {
      grid-template-columns: 1fr;
    }
  }
  .cc-sl-hero-copy {
    display: flex;
    flex-direction: column;
    gap: 18px;
  }
  .cc-sl-hero-copy h1 {
    font-size: clamp(40px, 6vw, 84px);
    margin: 6px 0 8px;
    line-height: 1.02;
    text-wrap: balance;
  }
  .cc-sl-hero-copy h1 .accent {
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
  .cc-sl-hero-copy p {
    font-size: clamp(15px, 1.2vw, 19px);
    line-height: 1.55;
    color: var(--cc-ink-dim);
    max-width: 56ch;
    margin: 0 0 18px;
    text-wrap: pretty;
  }
  .cc-sl-hero-motif {
    color: var(--cc-ink);
    width: 100%;
    aspect-ratio: 4 / 3;
    max-height: 480px;
    display: flex;
    align-items: center;
    justify-content: center;
    opacity: 0.95;
  }
  .cc-sl-hero-motif > svg {
    width: 100%;
    height: 100%;
    display: block;
  }
  @media (max-width: 960px) {
    .cc-sl-hero-motif {
      max-height: 320px;
    }
  }

  /* ===== 02 Proof strip ===== */
  .cc-sl-proof {
    /* StatRow handles its own internal layout; the band gives it room */
  }

  /* ===== 03 Pillars (content-on-band, no card chrome) ===== */
  .cc-sl-pillars-grid {
    display: grid;
    grid-template-columns: repeat(4, 1fr);
    gap: clamp(24px, 3vw, 40px);
  }
  .cc-sl-pillars-grid.cc-sl-pillars-3 {
    grid-template-columns: repeat(3, 1fr);
    max-width: 1080px;
    margin: 0 auto;
  }
  @media (max-width: 1080px) {
    .cc-sl-pillars-grid,
    .cc-sl-pillars-grid.cc-sl-pillars-3 {
      grid-template-columns: repeat(2, 1fr);
    }
  }
  @media (max-width: 640px) {
    .cc-sl-pillars-grid,
    .cc-sl-pillars-grid.cc-sl-pillars-3 {
      grid-template-columns: 1fr;
    }
  }
  .cc-sl-pillar {
    display: flex;
    flex-direction: column;
    gap: 16px;
    padding: 0 0 0 20px;
    border-left: 1px solid var(--cc-ink-faint);
  }
  .cc-sl-pillar-icon {
    width: 36px;
    height: 36px;
    display: flex;
    align-items: center;
    justify-content: flex-start;
    color: var(--cc-accent, var(--cc-ink));
  }
  .cc-sl-pillar-title {
    font-size: 20px;
    font-weight: 500;
    letter-spacing: -0.015em;
    color: var(--cc-ink);
    line-height: 1.25;
    margin: 0;
  }
  .cc-sl-pillar-body {
    font-size: 14px;
    line-height: 1.6;
    color: var(--cc-ink-dim);
    text-wrap: pretty;
    margin: 0;
  }

  /* ===== 04 Concept diagram (inverted band, full-bleed canvas) ===== */
  .cc-sl-diagram-head {
    text-align: center;
    margin: 0 auto 40px;
    max-width: 720px;
  }
  .cc-sl-diagram-head h3 {
    font-size: clamp(24px, 2.6vw, 34px);
    font-weight: 500;
    letter-spacing: -0.02em;
    color: #ffffff;
    margin: 8px 0 0;
    line-height: 1.2;
  }
  .cc-sl-diagram-canvas {
    width: 100%;
    max-width: 1180px;
    height: clamp(420px, 56vw, 700px);
    margin: 0 auto;
    color: #f5f1ea;
  }
  .cc-sl-diagram-canvas svg {
    width: 100%;
    height: 100%;
    display: block;
  }

  /* ===== 05 Code snippet (tinted band) ===== */
  .cc-sl-code-inner {
    max-width: 980px;
    margin: 0 auto;
  }
  .cc-sl-code-frame {
    border: 1px solid rgba(10, 13, 24, 0.16);
    border-radius: 16px;
    background: rgba(8, 14, 26, 0.96);
    overflow: hidden;
    box-shadow: 0 30px 80px -40px rgba(0, 0, 0, 0.5);
  }
  .cc-sl-code-head {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 12px;
    padding: 14px 20px;
    border-bottom: 1px solid rgba(245, 241, 234, 0.12);
    background: rgba(255, 255, 255, 0.025);
  }
  .cc-sl-code-file {
    font-family: var(--cc-font-mono), monospace;
    font-size: 12px;
    color: #f5f1ea;
    letter-spacing: 0.04em;
  }
  .cc-sl-code-lang {
    font-family: var(--cc-font-mono), monospace;
    font-size: 10px;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    color: rgba(245, 241, 234, 0.62);
    padding: 4px 8px;
    border: 1px solid rgba(245, 241, 234, 0.16);
    border-radius: 6px;
  }
  .cc-sl-code-body {
    margin: 0;
    padding: 22px 24px;
    overflow-x: auto;
    font-family: var(--cc-font-mono), monospace;
    font-size: 13.5px;
    line-height: 1.65;
    color: #f5f1ea;
    white-space: pre;
  }
  .cc-sl-code-body .tok-comment {
    color: rgba(245, 241, 234, 0.42);
    font-style: italic;
  }
  .cc-sl-code-body .tok-keyword {
    color: var(--cc-col-usr);
  }
  .cc-sl-code-body .tok-string {
    color: var(--cc-col-bil);
  }
  .cc-sl-code-body .tok-number {
    color: var(--cc-col-ord);
  }
  .cc-sl-code-body .tok-key {
    color: var(--cc-col-shi);
  }
  .cc-sl-code-body .tok-type {
    color: var(--cc-col-cat);
  }
  .cc-sl-code-body .tok-attr {
    color: var(--cc-amber);
  }

  /* ===== 06 Testimonials (one treatment, full-width quotes) ===== */
  .cc-sl-testimonials-inner {
    max-width: 980px;
    margin: 0 auto;
    display: flex;
    flex-direction: column;
    gap: 56px;
  }
  .cc-sl-testimonial {
    border-left: 2px solid var(--cc-accent, var(--cc-amber));
    padding: 12px 0 12px 28px;
    display: flex;
    flex-direction: column;
    gap: 22px;
    margin: 0;
  }
  .cc-sl-testimonial-quote {
    font-family: var(--cc-font-sans), sans-serif;
    font-size: clamp(20px, 2vw, 28px);
    font-style: italic;
    font-weight: 500;
    color: var(--cc-ink);
    line-height: 1.4;
    letter-spacing: -0.01em;
    margin: 0;
    text-wrap: pretty;
  }
  .cc-sl-testimonial-attribution {
    display: flex;
    align-items: center;
    gap: 14px;
  }
  .cc-sl-testimonial-mono {
    width: 40px;
    height: 40px;
    border-radius: 10px;
    border: 1.5px solid var(--cc-ink-faint);
    display: flex;
    align-items: center;
    justify-content: center;
    font-family: var(--cc-font-sans), sans-serif;
    font-weight: 600;
    font-size: 13px;
    letter-spacing: -0.02em;
    color: var(--cc-ink);
    background: rgba(255, 255, 255, 0.025);
    flex-shrink: 0;
  }
  .cc-sl-testimonial-meta {
    display: flex;
    flex-direction: column;
    gap: 3px;
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.14em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
  }
  .cc-sl-testimonial-meta .name {
    color: var(--cc-ink);
  }

  /* ===== 07 Foundations (dense icon row, no card chrome) ===== */
  .cc-sl-features-inner {
    max-width: 1280px;
    margin: 0 auto;
  }
  .cc-sl-features-head {
    text-align: center;
    margin: 0 auto 36px;
    max-width: 720px;
  }
  .cc-sl-features-headline {
    font-family: var(--cc-font-sans), sans-serif;
    font-size: clamp(20px, 2.2vw, 26px);
    font-weight: 500;
    letter-spacing: -0.02em;
    color: #0c1322;
    line-height: 1.25;
    margin: 8px auto 0;
    text-wrap: balance;
  }
  .cc-sl-features-row {
    display: grid;
    grid-template-columns: repeat(6, minmax(0, 1fr));
    gap: clamp(16px, 2vw, 28px);
    align-items: start;
  }
  @media (max-width: 1080px) {
    .cc-sl-features-row {
      grid-template-columns: repeat(3, minmax(0, 1fr));
    }
  }
  @media (max-width: 600px) {
    .cc-sl-features-row {
      grid-template-columns: repeat(2, minmax(0, 1fr));
    }
  }
  .cc-sl-feature-chip {
    display: flex;
    flex-direction: column;
    gap: 10px;
    align-items: flex-start;
    padding: 0;
    color: #0c1322;
  }
  .cc-sl-feature-chip-icon {
    width: 32px;
    height: 32px;
    display: flex;
    align-items: center;
    justify-content: center;
    color: var(--cc-accent, #0c1322);
    opacity: 0.92;
  }
  .cc-sl-feature-chip-label {
    font-family: var(--cc-font-sans), sans-serif;
    font-size: 13.5px;
    font-weight: 500;
    line-height: 1.35;
    letter-spacing: -0.005em;
    color: #0c1322;
    text-wrap: balance;
  }

  /* ===== 08 Collateral ===== */
  .cc-sl-collateral-inner {
    max-width: 980px;
    margin: 0 auto;
  }
  .cc-sl-collateral-card {
    border: 1px solid var(--cc-accent-line, var(--cc-ink-faint));
    border-radius: 18px;
    background: rgba(255, 255, 255, 0.02);
    padding: 32px 36px;
    display: grid;
    grid-template-columns: 1fr auto;
    gap: 28px;
    align-items: center;
  }
  @media (max-width: 720px) {
    .cc-sl-collateral-card {
      grid-template-columns: 1fr;
      text-align: center;
      padding: 28px 22px;
    }
  }
  .cc-sl-collateral-eyebrow {
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
    margin-bottom: 8px;
  }
  .cc-sl-collateral-title {
    font-size: clamp(22px, 2.4vw, 28px);
    font-weight: 500;
    letter-spacing: -0.02em;
    color: var(--cc-ink);
    line-height: 1.2;
    margin: 0;
    text-wrap: pretty;
  }

  /* ===== 09 Logo wall (typographic descriptor lockups) ===== */
  .cc-sl-logos-inner {
    max-width: 1280px;
    margin: 0 auto;
  }
  .cc-sl-logos-caption {
    text-align: center;
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
    margin: 0 0 36px;
  }
  .cc-sl-logos-grid {
    display: grid;
    grid-template-columns: repeat(5, minmax(0, 1fr));
    gap: 0;
    border-top: 1px solid var(--cc-ink-faint);
    border-left: 1px solid var(--cc-ink-faint);
  }
  @media (max-width: 980px) {
    .cc-sl-logos-grid {
      grid-template-columns: repeat(2, minmax(0, 1fr));
    }
  }
  @media (max-width: 540px) {
    .cc-sl-logos-grid {
      grid-template-columns: 1fr;
    }
  }
  .cc-sl-logo-lockup {
    display: flex;
    align-items: center;
    justify-content: center;
    text-align: center;
    min-height: 88px;
    padding: 22px 18px;
    border-right: 1px solid var(--cc-ink-faint);
    border-bottom: 1px solid var(--cc-ink-faint);
  }
  .cc-sl-logo-wordmark {
    font-family: var(--cc-font-sans), sans-serif;
    font-size: 18px;
    font-weight: 600;
    letter-spacing: -0.02em;
    color: var(--cc-ink);
  }
  .cc-sl-logo-descriptor {
    font-family: var(--cc-font-mono), monospace;
    font-size: 10.5px;
    letter-spacing: 0.14em;
    color: var(--cc-ink-dim);
    line-height: 1.45;
    text-wrap: balance;
  }

  /* ===== 10 Final CTA (one primary + secondary link + tertiary) ===== */
  .cc-sl-final-inner {
    max-width: 720px;
    margin: 0 auto;
    text-align: center;
    display: flex;
    flex-direction: column;
    gap: 14px;
    align-items: center;
  }
  .cc-sl-final-inner h2 {
    font-size: clamp(36px, 5vw, 64px);
    margin: 8px auto 6px;
    max-width: 24ch;
    line-height: 1.05;
    letter-spacing: -0.035em;
  }
  .cc-sl-final-inner p {
    font-size: 17px;
    color: var(--cc-ink-dim);
    margin: 0 auto 22px;
    max-width: 56ch;
    line-height: 1.55;
    text-wrap: pretty;
  }
  .cc-sl-final-buttons {
    display: flex;
    gap: 22px;
    justify-content: center;
    align-items: center;
    flex-wrap: wrap;
  }
  .cc-sl-final-link {
    font-family: var(--cc-font-sans), sans-serif;
    font-size: 15px;
    font-weight: 500;
    color: var(--cc-ink);
    text-decoration: none;
    border-bottom: 1px solid var(--cc-ink-faint);
    padding-bottom: 2px;
    transition: border-color 0.12s ease;
  }
  .cc-sl-final-link:hover {
    border-color: var(--cc-ink);
  }
  .cc-sl-final-tertiary {
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.16em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
    text-decoration: none;
    margin-top: 14px;
    transition: color 0.12s ease;
  }
  .cc-sl-final-tertiary:hover {
    color: var(--cc-ink);
  }

  /* ===== 11 Related ===== */
  .cc-sl-related-inner {
    max-width: 1180px;
    margin: 0 auto;
  }
  .cc-sl-related-grid {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: clamp(20px, 2vw, 32px);
  }
  @media (max-width: 880px) {
    .cc-sl-related-grid {
      grid-template-columns: 1fr;
      max-width: 480px;
      margin: 0 auto;
    }
  }
  .cc-sl-related-card {
    display: flex;
    flex-direction: column;
    padding: 4px 0 0 18px;
    border-left: 1px solid var(--cc-ink-faint);
    text-decoration: none;
    color: inherit;
    transition: border-color 0.15s ease;
  }
  .cc-sl-related-card:hover {
    border-color: var(--cc-accent, var(--cc-ink));
  }
  .cc-sl-related-eyebrow {
    font-family: var(--cc-font-mono), monospace;
    font-size: 10px;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
    margin-bottom: 10px;
  }
  .cc-sl-related-title {
    font-size: 20px;
    font-weight: 500;
    letter-spacing: -0.015em;
    color: var(--cc-ink);
    margin: 0 0 10px;
    line-height: 1.3;
  }
  .cc-sl-related-body {
    font-size: 14px;
    line-height: 1.55;
    color: var(--cc-ink-dim);
    margin: 0 0 20px;
    flex: 1;
    text-wrap: pretty;
  }
  .cc-sl-related-link {
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.16em;
    text-transform: uppercase;
    color: var(--cc-ink);
    margin-top: auto;
    display: inline-flex;
    align-items: center;
    gap: 6px;
  }
`;
