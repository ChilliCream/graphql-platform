"use client";

import styled from "styled-components";

// SolutionsRoot owns the dark navy / cream-ink design tokens for every
// /solutions/[slug] page. Same approach as CustomersRoot, EnterpriseRoot,
// TemplatesRoot: tokens, section shell, typography and button system are
// shared verbatim. Section CSS lives below, scoped by class prefix:
//   .cc-sl-*       solution page sections
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
  section.cc-sl-section {
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
    padding-top: 160px;
    padding-bottom: 88px;
    text-align: center;
  }
  .cc-sl-hero-inner {
    max-width: 920px;
    margin: 0 auto;
  }
  .cc-sl-hero h1 {
    font-size: clamp(42px, 6.4vw, 96px);
    margin: 18px 0 24px;
    line-height: 1.02;
    text-wrap: balance;
  }
  .cc-sl-hero h1 .accent {
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
  .cc-sl-hero p {
    font-size: clamp(15px, 1.2vw, 19px);
    line-height: 1.55;
    color: var(--cc-ink-dim);
    max-width: 60ch;
    margin: 0 auto 32px;
    text-wrap: pretty;
  }

  /* ===== 02 Proof strip ===== */
  .cc-sl-proof {
    padding-top: 0;
    padding-bottom: 120px;
  }
  .cc-sl-proof-inner {
    max-width: 1280px;
    margin: 0 auto;
    display: grid;
    grid-template-columns: repeat(4, 1fr);
    gap: 18px;
  }
  @media (max-width: 1080px) {
    .cc-sl-proof-inner {
      grid-template-columns: repeat(2, 1fr);
    }
  }
  @media (max-width: 540px) {
    .cc-sl-proof-inner {
      grid-template-columns: 1fr;
    }
  }
  .cc-sl-proof-card {
    display: flex;
    flex-direction: column;
    gap: 14px;
    padding: 30px 26px 26px;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 18px;
    background: rgba(255, 255, 255, 0.025);
    position: relative;
    overflow: hidden;
  }
  .cc-sl-proof-card::before {
    content: "";
    position: absolute;
    inset: 0;
    background: radial-gradient(
      120% 60% at 0% 0%,
      rgba(140, 180, 230, 0.14),
      transparent 60%
    );
    opacity: 0.6;
    pointer-events: none;
  }
  .cc-sl-proof-value {
    font-family: var(--cc-font-sans), sans-serif;
    font-size: clamp(36px, 4.6vw, 56px);
    font-weight: 500;
    letter-spacing: -0.035em;
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
    position: relative;
  }
  .cc-sl-proof-outcome {
    font-size: 15px;
    color: var(--cc-ink);
    line-height: 1.45;
    text-wrap: pretty;
    position: relative;
    flex: 1;
  }
  .cc-sl-proof-customer {
    font-family: var(--cc-font-mono), monospace;
    font-size: 10px;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
    padding-top: 14px;
    border-top: 1px solid var(--cc-ink-faint);
    position: relative;
  }

  /* ===== 03 Pillars ===== */
  .cc-sl-pillars {
    padding-top: 0;
    padding-bottom: 140px;
  }
  .cc-sl-pillars-inner {
    max-width: 1280px;
    margin: 0 auto;
  }
  .cc-sl-pillars-grid {
    display: grid;
    grid-template-columns: repeat(4, 1fr);
    gap: 24px;
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
    gap: 14px;
    padding: 28px 24px;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 18px;
    background: rgba(255, 255, 255, 0.02);
    transition: border-color 0.15s ease, background 0.15s ease,
      transform 0.15s ease;
  }
  .cc-sl-pillar:hover {
    border-color: rgba(245, 241, 234, 0.28);
    background: rgba(255, 255, 255, 0.04);
    transform: translateY(-2px);
  }
  .cc-sl-pillar-icon {
    width: 44px;
    height: 44px;
    border-radius: 12px;
    border: 1.5px solid var(--cc-ink-faint);
    display: flex;
    align-items: center;
    justify-content: center;
    color: var(--cc-ink);
    background: rgba(255, 255, 255, 0.03);
  }
  .cc-sl-pillar-title {
    font-size: 19px;
    font-weight: 500;
    letter-spacing: -0.015em;
    color: var(--cc-ink);
    line-height: 1.3;
    margin: 0;
  }
  .cc-sl-pillar-body {
    font-size: 14px;
    line-height: 1.55;
    color: var(--cc-ink-dim);
    text-wrap: pretty;
    margin: 0;
  }

  /* ===== 04 Concept diagram ===== */
  .cc-sl-diagram {
    padding-top: 0;
    padding-bottom: 140px;
  }
  .cc-sl-diagram-inner {
    max-width: 1180px;
    margin: 0 auto;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 22px;
    background: linear-gradient(
      180deg,
      rgba(14, 22, 38, 0.7),
      rgba(10, 17, 30, 0.7)
    );
    padding: 56px 48px 48px;
    overflow: hidden;
    position: relative;
  }
  @media (max-width: 720px) {
    .cc-sl-diagram-inner {
      padding: 36px 24px;
    }
  }
  .cc-sl-diagram-head {
    text-align: center;
    margin: 0 auto 36px;
    max-width: 720px;
  }
  .cc-sl-diagram-head h3 {
    font-size: clamp(22px, 2.4vw, 30px);
    font-weight: 500;
    letter-spacing: -0.02em;
    color: var(--cc-ink);
    margin: 8px 0 0;
    line-height: 1.2;
  }
  .cc-sl-diagram-canvas {
    aspect-ratio: 16 / 9;
    width: 100%;
    max-width: 920px;
    margin: 0 auto;
  }
  .cc-sl-diagram-canvas svg {
    width: 100%;
    height: 100%;
    display: block;
  }

  /* ===== 05 Code snippet ===== */
  .cc-sl-code {
    padding-top: 0;
    padding-bottom: 140px;
  }
  .cc-sl-code-inner {
    max-width: 980px;
    margin: 0 auto;
  }
  .cc-sl-code-frame {
    border: 1px solid var(--cc-ink-faint);
    border-radius: 16px;
    background: rgba(8, 14, 26, 0.92);
    overflow: hidden;
    box-shadow: 0 30px 80px -40px rgba(0, 0, 0, 0.7);
  }
  .cc-sl-code-head {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 12px;
    padding: 14px 20px;
    border-bottom: 1px solid var(--cc-ink-faint);
    background: rgba(255, 255, 255, 0.025);
  }
  .cc-sl-code-file {
    font-family: var(--cc-font-mono), monospace;
    font-size: 12px;
    color: var(--cc-ink);
    letter-spacing: 0.04em;
  }
  .cc-sl-code-lang {
    font-family: var(--cc-font-mono), monospace;
    font-size: 10px;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
    padding: 4px 8px;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 6px;
  }
  .cc-sl-code-body {
    margin: 0;
    padding: 22px 24px;
    overflow-x: auto;
    font-family: var(--cc-font-mono), monospace;
    font-size: 13.5px;
    line-height: 1.65;
    color: var(--cc-ink);
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

  /* ===== 06 Testimonials ===== */
  .cc-sl-testimonials {
    padding-top: 0;
    padding-bottom: 140px;
  }
  .cc-sl-testimonials-inner {
    max-width: 1080px;
    margin: 0 auto;
    display: grid;
    grid-template-columns: 1fr;
    gap: 24px;
  }
  .cc-sl-testimonials-inner.has-two {
    grid-template-columns: repeat(2, 1fr);
  }
  @media (max-width: 880px) {
    .cc-sl-testimonials-inner.has-two {
      grid-template-columns: 1fr;
    }
  }
  .cc-sl-testimonial {
    border: 1px solid var(--cc-ink-faint);
    border-left: 2px solid var(--cc-amber);
    border-radius: 0 16px 16px 0;
    background: rgba(255, 255, 255, 0.025);
    padding: 32px 36px;
    display: flex;
    flex-direction: column;
    gap: 22px;
  }
  .cc-sl-testimonial-quote {
    font-family: var(--cc-font-sans), sans-serif;
    font-size: clamp(18px, 1.8vw, 24px);
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
    padding-top: 18px;
    border-top: 1px solid var(--cc-ink-faint);
  }
  .cc-sl-testimonial-mono {
    width: 44px;
    height: 44px;
    border-radius: 12px;
    border: 1.5px solid var(--cc-ink-faint);
    display: flex;
    align-items: center;
    justify-content: center;
    font-family: var(--cc-font-sans), sans-serif;
    font-weight: 600;
    font-size: 14px;
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

  /* ===== 07 Feature cards ===== */
  .cc-sl-features {
    padding-top: 0;
    padding-bottom: 140px;
  }
  .cc-sl-features-inner {
    max-width: 1280px;
    margin: 0 auto;
  }
  .cc-sl-features-grid {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 18px;
  }
  @media (max-width: 980px) {
    .cc-sl-features-grid {
      grid-template-columns: repeat(2, 1fr);
    }
  }
  @media (max-width: 600px) {
    .cc-sl-features-grid {
      grid-template-columns: 1fr;
    }
  }
  .cc-sl-feature-card {
    display: flex;
    flex-direction: column;
    gap: 14px;
    padding: 26px 24px;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 16px;
    background: rgba(255, 255, 255, 0.02);
    transition: border-color 0.15s ease, background 0.15s ease;
  }
  .cc-sl-feature-card:hover {
    border-color: rgba(245, 241, 234, 0.28);
    background: rgba(255, 255, 255, 0.04);
  }
  .cc-sl-feature-icon {
    width: 40px;
    height: 40px;
    border-radius: 10px;
    border: 1.5px solid var(--cc-ink-faint);
    display: flex;
    align-items: center;
    justify-content: center;
    color: var(--cc-ink);
    background: rgba(255, 255, 255, 0.03);
  }
  .cc-sl-feature-title {
    font-size: 17px;
    font-weight: 500;
    letter-spacing: -0.015em;
    color: var(--cc-ink);
    line-height: 1.3;
    margin: 0;
  }
  .cc-sl-feature-body {
    font-size: 14px;
    line-height: 1.55;
    color: var(--cc-ink-dim);
    margin: 0;
    text-wrap: pretty;
  }

  /* ===== 08 Collateral ===== */
  .cc-sl-collateral {
    padding-top: 0;
    padding-bottom: 140px;
  }
  .cc-sl-collateral-inner {
    max-width: 980px;
    margin: 0 auto;
    border-radius: 22px;
    padding: 1px;
    background: linear-gradient(
      135deg,
      rgba(245, 241, 234, 0.28),
      rgba(245, 241, 234, 0.04) 35%,
      rgba(120, 140, 220, 0.22) 70%,
      rgba(245, 241, 234, 0.06)
    );
    box-shadow: 0 30px 80px -40px rgba(0, 0, 0, 0.7);
  }
  .cc-sl-collateral-card {
    border-radius: 21px;
    background: linear-gradient(
      180deg,
      rgba(14, 22, 38, 0.96),
      rgba(10, 17, 30, 0.96)
    );
    padding: 40px 44px;
    display: grid;
    grid-template-columns: 1fr auto;
    gap: 28px;
    align-items: center;
  }
  @media (max-width: 720px) {
    .cc-sl-collateral-card {
      grid-template-columns: 1fr;
      text-align: center;
      padding: 32px 24px;
    }
  }
  .cc-sl-collateral-eyebrow {
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
    margin-bottom: 10px;
  }
  .cc-sl-collateral-title {
    font-size: clamp(22px, 2.4vw, 30px);
    font-weight: 500;
    letter-spacing: -0.02em;
    color: var(--cc-ink);
    line-height: 1.2;
    margin: 0;
    text-wrap: pretty;
  }

  /* ===== 09 Logo wall ===== */
  .cc-sl-logos {
    padding-top: 0;
    padding-bottom: 140px;
  }
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
    margin: 0 0 28px;
  }
  .cc-sl-logos-grid {
    display: grid;
    grid-template-columns: repeat(5, 1fr);
    gap: 14px;
  }
  @media (max-width: 980px) {
    .cc-sl-logos-grid {
      grid-template-columns: repeat(3, 1fr);
    }
  }
  @media (max-width: 600px) {
    .cc-sl-logos-grid {
      grid-template-columns: repeat(2, 1fr);
    }
  }
  .cc-sl-logo-tile {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 10px;
    padding: 22px 12px 16px;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 14px;
    background: rgba(255, 255, 255, 0.02);
    transition: border-color 0.15s ease, background 0.15s ease;
  }
  .cc-sl-logo-tile:hover {
    border-color: rgba(245, 241, 234, 0.28);
    background: rgba(255, 255, 255, 0.04);
  }
  .cc-sl-logo-tile.is-named {
    border-color: rgba(245, 241, 234, 0.32);
    background: rgba(255, 255, 255, 0.045);
  }
  .cc-sl-logo-mono {
    width: 52px;
    height: 52px;
    border-radius: 12px;
    border: 1.5px solid var(--cc-ink-faint);
    display: flex;
    align-items: center;
    justify-content: center;
    color: var(--cc-ink);
    font-family: var(--cc-font-sans), sans-serif;
    font-weight: 600;
    font-size: 14px;
    letter-spacing: -0.02em;
    background: rgba(255, 255, 255, 0.025);
  }
  .cc-sl-logo-tile.is-named .cc-sl-logo-mono {
    border-color: rgba(245, 241, 234, 0.45);
    background: rgba(255, 255, 255, 0.08);
  }
  .cc-sl-logo-caption {
    font-family: var(--cc-font-mono), monospace;
    font-size: 10px;
    letter-spacing: 0.14em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
    text-align: center;
    line-height: 1.4;
  }
  .cc-sl-logo-tile.is-named .cc-sl-logo-caption {
    color: var(--cc-ink);
  }

  /* ===== 10 Final CTA ===== */
  .cc-sl-final {
    padding-top: 0;
    padding-bottom: 140px;
  }
  .cc-sl-final-inner {
    max-width: 880px;
    margin: 0 auto;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 22px;
    background: rgba(255, 255, 255, 0.025);
    padding: 56px 48px 52px;
    text-align: center;
  }
  @media (max-width: 720px) {
    .cc-sl-final-inner {
      padding: 36px 24px;
    }
  }
  .cc-sl-final-inner h2 {
    font-size: clamp(28px, 3.6vw, 44px);
    margin: 12px auto 14px;
    max-width: 24ch;
    line-height: 1.1;
  }
  .cc-sl-final-inner p {
    font-size: 16px;
    color: var(--cc-ink-dim);
    margin: 0 auto 28px;
    max-width: 56ch;
    line-height: 1.55;
    text-wrap: pretty;
  }
  .cc-sl-final-buttons {
    display: flex;
    gap: 14px;
    justify-content: center;
    flex-wrap: wrap;
  }

  /* ===== 11 Related ===== */
  .cc-sl-related {
    padding-top: 0;
    padding-bottom: 160px;
  }
  .cc-sl-related-inner {
    max-width: 1180px;
    margin: 0 auto;
  }
  .cc-sl-related-grid {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 18px;
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
    padding: 26px 24px;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 16px;
    background: rgba(255, 255, 255, 0.025);
    text-decoration: none;
    color: inherit;
    transition: border-color 0.15s ease, transform 0.15s ease,
      background 0.15s ease;
  }
  .cc-sl-related-card:hover {
    border-color: rgba(245, 241, 234, 0.32);
    transform: translateY(-2px);
    background: rgba(255, 255, 255, 0.04);
  }
  .cc-sl-related-eyebrow {
    font-family: var(--cc-font-mono), monospace;
    font-size: 10px;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
    margin-bottom: 12px;
  }
  .cc-sl-related-title {
    font-size: 19px;
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
