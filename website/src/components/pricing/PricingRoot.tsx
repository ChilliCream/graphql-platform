"use client";

import styled from "styled-components";

// PricingRoot holds the dark navy / cream-ink design tokens for the /pricing
// page. It mirrors DesktopLandingRoot's tokens, typography, and button
// system but stays self-contained: the pricing page composes its sections
// inside `<Band>` primitives, so this stylesheet defines (a) the page
// background, (b) the per-page accent overrides for `tinted`/`accent`/`glow`
// bands so they read as subtle tonal steps on a dark canvas, and (c) the
// component-internal styles for hero, OSS belt, tier cards, comparison
// table, enterprise band, FAQ, and footer.
export const PricingRoot = styled.div`
  --cc-ink: #f5f1ea;
  --cc-ink-dim: rgba(245, 241, 234, 0.62);
  --cc-ink-faint: rgba(245, 241, 234, 0.16);
  --cc-line-w: 1.5px;
  --cc-col-cat: oklch(0.74 0.18 30);
  --cc-col-bil: oklch(0.82 0.16 90);
  --cc-col-ord: oklch(0.76 0.16 150);
  --cc-col-shi: oklch(0.74 0.14 220);
  --cc-col-usr: oklch(0.72 0.18 310);
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

  /* ===== Band overrides for the dark canvas =====
     Band's tinted variant ships as cream which is meant for pages with a
     light surface; on the dark pricing canvas we re-interpret it as a
     subtle tonal step up. accent and glow get tuned to the page accent so
     the eye registers a band shift without losing the dark theme. */
  .cc-band {
    --cc-pad-x: clamp(28px, 5vw, 96px);
  }
  .cc-band > div {
    max-width: 1280px;
  }
  .cc-band.cc-band-faq {
    background: rgba(255, 255, 255, 0.025);
    color: var(--cc-ink);
  }
  .cc-band.cc-band-faq h1,
  .cc-band.cc-band-faq h2,
  .cc-band.cc-band-faq h3,
  .cc-band.cc-band-faq h4 {
    color: var(--cc-ink);
  }
  .cc-band.cc-band-enterprise {
    background: radial-gradient(
        60% 80% at 100% 50%,
        var(--cc-accent-glow, rgba(140, 160, 240, 0.18)),
        transparent 70%
      ),
      var(--cc-accent-soft, rgba(140, 160, 240, 0.08));
    color: var(--cc-ink);
  }
  .cc-band.cc-band-enterprise h1,
  .cc-band.cc-band-enterprise h2 {
    color: var(--cc-ink);
  }
  .cc-band.cc-band-footer {
    color: var(--cc-ink);
  }
  .cc-band.cc-band-oss {
    background: #07090f;
    color: var(--cc-ink);
  }
  .cc-band.cc-band-oss h1,
  .cc-band.cc-band-oss h2 {
    color: #ffffff;
  }

  /* ===== Section label (numbered eyebrow that floats over each band) ===== */
  .cc-section-label {
    position: absolute;
    top: -18px;
    left: 0;
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
    font-weight: 600;
    letter-spacing: -0.04em;
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
    align-items: center;
    flex-wrap: wrap;
  }

  /* ===== 01 Hero ===== */
  .cc-pricing-hero {
    position: relative;
    text-align: center;
  }
  .cc-pricing-hero-inner {
    max-width: 880px;
    margin: 0 auto;
  }
  .cc-pricing-hero h1 {
    font-size: clamp(44px, 6.5vw, 96px);
    margin: 18px 0 24px;
    line-height: 1.02;
    font-weight: 600;
    letter-spacing: -0.04em;
  }
  .cc-pricing-hero h1 .accent {
    background: var(
      --cc-accent-gradient,
      linear-gradient(120deg, var(--cc-col-shi), var(--cc-col-usr) 60%)
    );
    -webkit-background-clip: text;
    background-clip: text;
    -webkit-text-fill-color: transparent;
  }
  .cc-pricing-hero p {
    font-size: clamp(15px, 1.2vw, 19px);
    line-height: 1.5;
    color: var(--cc-ink-dim);
    max-width: 60ch;
    margin: 0 auto 36px;
    text-wrap: pretty;
  }

  /* ===== Inline pricing calculator (lives in the hero) ===== */
  .cc-calc {
    margin: 0 auto;
    max-width: 720px;
    padding: 24px 28px 22px;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 18px;
    background: rgba(255, 255, 255, 0.03);
    text-align: left;
    box-shadow: 0 0 80px var(--cc-accent-glow, rgba(140, 160, 240, 0.1));
  }
  .cc-calc-row {
    display: flex;
    align-items: flex-end;
    justify-content: space-between;
    gap: 24px;
    margin-bottom: 14px;
  }
  @media (max-width: 560px) {
    .cc-calc-row {
      flex-direction: column;
      align-items: stretch;
      gap: 14px;
    }
  }
  .cc-calc-eyebrow {
    display: block;
    font-family: var(--cc-font-mono), monospace;
    font-size: 10px;
    letter-spacing: 0.16em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
    margin-bottom: 4px;
  }
  .cc-calc-label {
    display: block;
    cursor: pointer;
  }
  .cc-calc-value {
    display: block;
    font-family: var(--cc-font-sans), sans-serif;
    font-size: clamp(22px, 2.4vw, 30px);
    font-weight: 500;
    letter-spacing: -0.02em;
    color: var(--cc-ink);
    font-feature-settings: "tnum" 1;
  }
  .cc-calc-output {
    text-align: right;
  }
  @media (max-width: 560px) {
    .cc-calc-output {
      text-align: left;
    }
  }
  .cc-calc-price {
    display: block;
    font-family: var(--cc-font-sans), sans-serif;
    font-size: clamp(28px, 3.2vw, 40px);
    font-weight: 600;
    letter-spacing: -0.03em;
    line-height: 1;
    background: var(
      --cc-accent-gradient,
      linear-gradient(120deg, var(--cc-col-shi), var(--cc-col-usr))
    );
    -webkit-background-clip: text;
    background-clip: text;
    -webkit-text-fill-color: transparent;
    font-feature-settings: "tnum" 1;
  }
  .cc-calc-price-unit {
    font-size: 0.5em;
    font-weight: 400;
    color: var(--cc-ink-dim);
    -webkit-text-fill-color: var(--cc-ink-dim);
    background: none;
  }
  .cc-calc-tier {
    display: block;
    margin-top: 6px;
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.12em;
    text-transform: uppercase;
    color: var(--cc-ink);
  }
  .cc-calc-slider {
    -webkit-appearance: none;
    appearance: none;
    width: 100%;
    height: 6px;
    border-radius: 999px;
    background: linear-gradient(
      90deg,
      var(--cc-accent-line, rgba(140, 160, 240, 0.32)),
      rgba(245, 241, 234, 0.12)
    );
    outline: none;
    margin: 8px 0 4px;
  }
  .cc-calc-slider::-webkit-slider-thumb {
    -webkit-appearance: none;
    appearance: none;
    width: 20px;
    height: 20px;
    border-radius: 50%;
    background: var(--cc-ink);
    border: 3px solid var(--cc-accent, var(--cc-col-shi));
    cursor: pointer;
    box-shadow: 0 0 0 6px var(--cc-accent-glow, rgba(140, 160, 240, 0.18));
    transition: transform 0.15s ease;
  }
  .cc-calc-slider::-webkit-slider-thumb:hover {
    transform: scale(1.08);
  }
  .cc-calc-slider::-moz-range-thumb {
    width: 20px;
    height: 20px;
    border-radius: 50%;
    background: var(--cc-ink);
    border: 3px solid var(--cc-accent, var(--cc-col-shi));
    cursor: pointer;
    box-shadow: 0 0 0 6px var(--cc-accent-glow, rgba(140, 160, 240, 0.18));
  }
  .cc-calc-slider:focus-visible {
    outline: 2px solid var(--cc-accent, var(--cc-col-shi));
    outline-offset: 6px;
    border-radius: 999px;
  }
  .cc-calc-scale {
    display: flex;
    justify-content: space-between;
    font-family: var(--cc-font-mono), monospace;
    font-size: 10px;
    letter-spacing: 0.08em;
    color: var(--cc-ink-dim);
    margin-top: 4px;
  }
  .cc-calc-note {
    margin: 12px 0 0;
    font-size: 13px;
    line-height: 1.5;
    color: var(--cc-ink-dim);
    text-align: left;
  }

  /* ===== 02 OSS strip (inverted band, no inner card chrome) ===== */
  .cc-oss-strip {
    position: relative;
  }
  .cc-oss-inner {
    max-width: 1080px;
    margin: 0 auto;
    display: grid;
    grid-template-columns: minmax(0, 1fr) auto;
    gap: 32px;
    align-items: center;
  }
  @media (max-width: 880px) {
    .cc-oss-inner {
      grid-template-columns: 1fr;
    }
  }
  .cc-oss-copy {
    display: flex;
    flex-direction: column;
    gap: 14px;
  }
  .cc-oss-chips {
    display: flex;
    flex-wrap: wrap;
    gap: 8px;
  }
  .cc-oss-chip {
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.08em;
    text-transform: uppercase;
    color: var(--cc-ink);
    padding: 6px 10px;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 8px;
    background: rgba(255, 255, 255, 0.025);
  }
  .cc-oss-chip.is-tag {
    border-color: var(--cc-accent-line, rgba(140, 160, 240, 0.32));
    background: var(--cc-accent-soft, rgba(140, 160, 240, 0.1));
    color: var(--cc-ink);
  }
  .cc-oss-line {
    font-size: 16px;
    color: var(--cc-ink-dim);
    line-height: 1.55;
    margin: 0;
    max-width: 60ch;
  }
  .cc-oss-line strong {
    color: var(--cc-ink);
    font-weight: 500;
  }
  .cc-oss-terminal {
    border: 1px solid var(--cc-ink-faint);
    border-radius: 12px;
    background: rgba(255, 255, 255, 0.03);
    padding: 14px 18px;
    font-family: var(--cc-font-mono), monospace;
    font-size: 13px;
    color: var(--cc-ink);
    display: inline-flex;
    align-items: center;
    gap: 14px;
    min-width: 320px;
  }
  .cc-oss-terminal .prompt {
    color: var(--cc-accent, var(--cc-col-shi));
  }
  .cc-oss-terminal .cmd {
    color: var(--cc-ink);
  }
  .cc-oss-terminal .pkg {
    color: var(--cc-col-bil);
  }

  /* ===== 03 Tier cards ===== */
  .cc-tiers {
    position: relative;
  }
  .cc-tiers-inner {
    max-width: 1280px;
    margin: 0 auto;
  }
  .cc-tiers-heading {
    text-align: center;
    margin: 0 auto 56px;
    max-width: 760px;
  }
  .cc-tiers-heading h2 {
    font-size: clamp(34px, 4.4vw, 56px);
    margin: 8px auto 14px;
    max-width: 18ch;
  }
  .cc-tiers-heading p {
    font-size: clamp(15px, 1.1vw, 17px);
    color: var(--cc-ink-dim);
    max-width: 56ch;
    margin: 0 auto;
    text-wrap: pretty;
  }
  .cc-tiers-subnote {
    margin-top: 18px !important;
    font-size: 13px !important;
    color: var(--cc-ink-dim);
  }
  .cc-tiers-subnote a {
    color: var(--cc-accent, var(--cc-col-shi));
    text-decoration: none;
    border-bottom: 1px solid var(--cc-accent-line, rgba(140, 160, 240, 0.32));
  }
  .cc-tiers-subnote a:hover {
    border-bottom-color: var(--cc-accent, var(--cc-col-shi));
  }
  .cc-tiers-grid {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 24px;
    align-items: stretch;
  }
  @media (max-width: 980px) {
    .cc-tiers-grid {
      grid-template-columns: 1fr;
      max-width: 480px;
      margin: 0 auto;
    }
  }
  .cc-tier-card {
    position: relative;
    display: flex;
    flex-direction: column;
    padding: 40px 32px 32px;
    background: rgba(255, 255, 255, 0.025);
    border-radius: 18px;
    transition: border-color 0.18s ease, transform 0.18s ease;
  }
  .cc-tier-card:hover {
    border-color: rgba(245, 241, 234, 0.28);
    transform: translateY(-2px);
  }
  .cc-tier-card.is-featured {
    background: rgba(255, 255, 255, 0.06);
    border-top: 2px solid var(--cc-accent, var(--cc-col-shi));
    box-shadow: 0 0 0 1px rgba(245, 241, 234, 0.08) inset,
      0 30px 80px -40px rgba(0, 0, 0, 0.6),
      0 0 80px -20px var(--cc-accent-glow, rgba(140, 160, 240, 0.2));
  }
  .cc-tier-badge {
    position: absolute;
    top: -12px;
    left: 50%;
    transform: translateX(-50%);
    padding: 5px 12px;
    border-radius: 999px;
    background: var(--cc-accent, var(--cc-ink));
    color: #0c1322;
    font-family: var(--cc-font-mono), monospace;
    font-size: 10px;
    font-weight: 600;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    white-space: nowrap;
  }
  .cc-tier-icon {
    width: 110px;
    height: 122px;
    margin: 0 auto 20px;
  }
  .cc-tier-brewer {
    text-align: center;
    font-family: var(--cc-font-mono), monospace;
    font-size: 10px;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
    margin-bottom: 6px;
  }
  .cc-tier-name {
    text-align: center;
    font-size: 26px;
    font-weight: 500;
    letter-spacing: -0.02em;
    margin: 0 0 6px;
    color: var(--cc-ink);
  }
  .cc-tier-tagline {
    text-align: center;
    font-size: 14px;
    color: var(--cc-ink-dim);
    margin: 0 auto 20px;
    max-width: 28ch;
    line-height: 1.5;
  }
  .cc-tier-price {
    text-align: center;
    margin-bottom: 24px;
    padding-bottom: 24px;
    border-bottom: 1px solid var(--cc-ink-faint);
  }
  .cc-tier-price-amount {
    display: block;
    font-size: 32px;
    font-weight: 500;
    letter-spacing: -0.02em;
    color: var(--cc-ink);
  }
  .cc-tier-price-note {
    display: block;
    margin-top: 4px;
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.08em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
  }
  .cc-tier-bullets {
    list-style: none;
    padding: 0;
    margin: 0 0 28px;
    display: flex;
    flex-direction: column;
    gap: 10px;
    flex: 1;
  }
  .cc-tier-bullets li {
    display: flex;
    align-items: flex-start;
    gap: 10px;
    font-size: 14px;
    color: var(--cc-ink);
    line-height: 1.5;
  }
  .cc-tier-bullets li svg {
    color: var(--cc-accent, var(--cc-col-shi));
    flex-shrink: 0;
    margin-top: 4px;
  }
  .cc-tier-card .cc-btn {
    width: 100%;
    padding: 14px 22px;
    font-size: 14px;
  }
  /* Inline spend-controls strip lives below the Hosted CTA so the anxiety
     and its resolution sit in the same card. */
  .cc-tier-spend-strip {
    list-style: none;
    margin: 14px 0 0;
    padding: 0;
    display: flex;
    flex-wrap: wrap;
    justify-content: center;
    gap: 4px 14px;
    font-family: var(--cc-font-mono), monospace;
    font-size: 10px;
    letter-spacing: 0.12em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
  }
  .cc-tier-spend-strip li {
    position: relative;
    padding-left: 14px;
  }
  .cc-tier-spend-strip li::before {
    content: "";
    position: absolute;
    left: 0;
    top: 50%;
    transform: translateY(-50%);
    width: 6px;
    height: 6px;
    border-radius: 50%;
    background: var(--cc-accent, var(--cc-col-shi));
    opacity: 0.7;
  }

  /* ===== 04 Comparison table (content-on-band, no outer card chrome) ===== */
  .cc-compare {
    position: relative;
  }
  .cc-compare-inner {
    max-width: 1280px;
    margin: 0 auto;
  }
  .cc-compare-heading {
    text-align: center;
    margin: 0 auto 28px;
    max-width: 720px;
  }
  .cc-compare-heading h2 {
    font-size: clamp(32px, 4vw, 50px);
    margin: 8px auto 14px;
  }
  .cc-compare-ribbon {
    display: block;
    max-width: 1080px;
    margin: 0 auto 36px;
    padding: 14px 20px;
    border-left: 2px solid var(--cc-accent, var(--cc-col-shi));
    font-size: 14px;
    line-height: 1.55;
    color: var(--cc-ink-dim);
  }
  .cc-compare-ribbon strong {
    color: var(--cc-ink);
    font-weight: 500;
  }
  .cc-compare-scroll {
    overflow-x: auto;
    overflow-y: visible;
    margin: 0 -8px;
    padding: 0 8px;
  }
  .cc-compare-table {
    width: 100%;
    min-width: 1080px;
    border-collapse: collapse;
    font-family: var(--cc-font-sans), sans-serif;
  }
  .cc-compare-table thead th {
    position: sticky;
    /* Sit below the 72px sticky site header so column titles stay visible
       as the table extends past the viewport. */
    top: 72px;
    z-index: 3;
    background: linear-gradient(
      180deg,
      #0c1322 0%,
      #0c1322 80%,
      rgba(12, 19, 34, 0.6)
    );
    text-align: left;
    padding: 22px 18px 18px;
    border-bottom: 1px solid var(--cc-accent-line, rgba(140, 160, 240, 0.32));
    vertical-align: bottom;
    backdrop-filter: blur(8px);
  }
  .cc-compare-table thead th.is-feature {
    width: 280px;
    min-width: 280px;
  }
  .cc-compare-table thead th.is-tier {
    width: 18%;
    min-width: 160px;
  }
  .cc-compare-table thead th.is-accent {
    background: linear-gradient(
      180deg,
      rgba(20, 36, 60, 0.95) 0%,
      rgba(14, 24, 42, 0.95) 80%,
      rgba(12, 19, 34, 0.6)
    );
  }
  .cc-compare-col-label {
    display: block;
    font-size: 14px;
    font-weight: 500;
    color: var(--cc-ink);
    margin-bottom: 4px;
  }
  .cc-compare-col-price {
    display: block;
    font-family: var(--cc-font-sans), sans-serif;
    font-size: 18px;
    font-weight: 500;
    color: var(--cc-ink);
    letter-spacing: -0.01em;
    margin-bottom: 2px;
  }
  .cc-compare-col-sub {
    display: block;
    font-family: var(--cc-font-mono), monospace;
    font-size: 10px;
    letter-spacing: 0.12em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
    line-height: 1.4;
  }
  .cc-compare-table tbody tr.cc-group-head th {
    text-align: left;
    padding: 36px 18px 12px;
    border-top: 1px solid var(--cc-accent-line, rgba(140, 160, 240, 0.32));
    border-bottom: 1px solid var(--cc-ink-faint);
  }
  .cc-compare-table tbody tr.cc-group-head.is-first th {
    border-top: none;
    padding-top: 28px;
  }
  .cc-compare-group-title {
    font-size: 13px;
    font-family: var(--cc-font-mono), monospace;
    font-weight: 500;
    letter-spacing: 0.16em;
    text-transform: uppercase;
    color: var(--cc-accent, var(--cc-col-shi));
    display: block;
    margin-bottom: 4px;
  }
  .cc-compare-group-summary {
    font-family: var(--cc-font-sans), sans-serif;
    font-size: 13px;
    font-weight: 400;
    letter-spacing: 0;
    text-transform: none;
    color: var(--cc-ink-dim);
    line-height: 1.5;
    display: block;
    max-width: 80ch;
  }
  .cc-compare-table tbody tr.cc-row {
    transition: background 0.12s ease;
  }
  .cc-compare-table tbody tr.cc-row:nth-child(odd) {
    background: rgba(255, 255, 255, 0.012);
  }
  .cc-compare-table tbody tr.cc-row:hover {
    background: rgba(255, 255, 255, 0.04);
  }
  .cc-compare-table tbody td,
  .cc-compare-table tbody th.cc-row-label {
    padding: 16px 18px;
    border-bottom: 1px solid rgba(245, 241, 234, 0.06);
    font-size: 14px;
    color: var(--cc-ink);
    vertical-align: top;
  }
  .cc-compare-table tbody th.cc-row-label {
    text-align: left;
    font-weight: 400;
  }
  .cc-row-label-hint {
    display: block;
    margin-top: 4px;
    font-size: 12px;
    color: var(--cc-ink-dim);
    line-height: 1.4;
  }
  .cc-compare-table tbody td.is-accent {
    background: rgba(20, 36, 60, 0.35);
  }
  .cc-cell-check svg {
    color: var(--cc-accent, var(--cc-col-shi));
    display: block;
  }
  .cc-cell-value {
    color: var(--cc-ink);
  }
  .cc-cell-meter {
    display: flex;
    flex-direction: column;
    gap: 4px;
  }
  .cc-cell-meter-included {
    color: var(--cc-ink);
    font-weight: 500;
  }
  .cc-cell-meter-overage {
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.04em;
    color: var(--cc-ink-dim);
  }
  .cc-cell-custom {
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
  }
  .cc-cell-none {
    color: var(--cc-ink-faint);
    font-family: var(--cc-font-mono), monospace;
    font-size: 14px;
  }
  .cc-compare-foot {
    margin: 28px auto 0;
    text-align: center;
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.14em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
  }

  /* ===== 05 Enterprise band (lives on accent Band) ===== */
  .cc-enterprise {
    position: relative;
  }
  .cc-enterprise-inner {
    max-width: 1180px;
    margin: 0 auto;
  }
  .cc-enterprise-grid {
    display: grid;
    grid-template-columns: minmax(0, 1.05fr) minmax(0, 1fr);
    gap: 56px;
    align-items: start;
  }
  @media (max-width: 880px) {
    .cc-enterprise-grid {
      grid-template-columns: 1fr;
      gap: 36px;
    }
  }
  .cc-enterprise-copy h2 {
    font-size: clamp(28px, 3.4vw, 44px);
    margin: 12px 0 18px;
    line-height: 1.05;
  }
  .cc-enterprise-copy p {
    font-size: 16px;
    line-height: 1.6;
    color: var(--cc-ink-dim);
    margin: 0 0 28px;
    max-width: 50ch;
    text-wrap: pretty;
  }
  .cc-enterprise-bullets {
    list-style: none;
    padding: 0;
    margin: 0;
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 16px 24px;
  }
  @media (max-width: 560px) {
    .cc-enterprise-bullets {
      grid-template-columns: 1fr;
    }
  }
  .cc-enterprise-bullets li {
    display: flex;
    align-items: flex-start;
    gap: 10px;
    font-size: 14px;
    color: var(--cc-ink);
    line-height: 1.45;
  }
  .cc-enterprise-bullets li svg {
    color: var(--cc-accent, var(--cc-col-shi));
    flex-shrink: 0;
    margin-top: 3px;
  }

  /* ===== 06 FAQ (tinted Band, grouped lists) ===== */
  .cc-faq {
    position: relative;
  }
  .cc-faq-inner {
    max-width: 880px;
    margin: 0 auto;
  }
  .cc-faq-heading {
    text-align: center;
    margin: 0 auto 48px;
  }
  .cc-faq-heading h2 {
    font-size: clamp(34px, 4.4vw, 56px);
    margin: 8px auto 0;
  }
  .cc-faq-groups {
    display: flex;
    flex-direction: column;
    gap: 36px;
  }
  .cc-faq-group-title {
    margin: 0 0 12px;
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    font-weight: 500;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    color: var(--cc-accent, var(--cc-col-shi));
  }
  .cc-faq-list {
    display: flex;
    flex-direction: column;
    border-top: 1px solid var(--cc-ink-faint);
  }
  .cc-faq-item {
    border-bottom: 1px solid var(--cc-ink-faint);
  }
  .cc-faq-item summary {
    list-style: none;
    cursor: pointer;
    display: flex;
    align-items: flex-start;
    justify-content: space-between;
    gap: 24px;
    padding: 24px 4px;
    font-size: clamp(16px, 1.3vw, 19px);
    font-weight: 500;
    color: var(--cc-ink);
    transition: color 0.15s ease;
  }
  .cc-faq-item summary::-webkit-details-marker {
    display: none;
  }
  .cc-faq-item summary:hover {
    color: var(--cc-accent, var(--cc-col-shi));
  }
  .cc-faq-num {
    flex-shrink: 0;
    width: 36px;
    font-family: var(--cc-font-mono), monospace;
    font-size: 12px;
    letter-spacing: 0.14em;
    color: var(--cc-ink-dim);
    padding-top: 4px;
  }
  .cc-faq-q {
    flex: 1;
    text-align: left;
  }
  .cc-faq-chevron {
    flex-shrink: 0;
    width: 22px;
    height: 22px;
    color: var(--cc-ink-dim);
    transition: transform 0.2s ease, color 0.15s ease;
    margin-top: 4px;
  }
  .cc-faq-item[open] .cc-faq-chevron {
    transform: rotate(180deg);
    color: var(--cc-accent, var(--cc-ink));
  }
  .cc-faq-item[open] summary {
    color: var(--cc-accent, var(--cc-ink));
  }
  .cc-faq-answer {
    padding: 0 4px 28px 40px;
    color: var(--cc-ink-dim);
    font-size: 16px;
    line-height: 1.65;
    text-wrap: pretty;
    max-width: 70ch;
    margin: 0;
  }

  /* ===== 07 Footer CTA (glow Band) ===== */
  .cc-footer-cta {
    text-align: center;
  }
  .cc-footer-cta-inner {
    max-width: 720px;
    margin: 0 auto;
    position: relative;
    z-index: 3;
  }
  .cc-footer-cta-inner h2 {
    font-size: clamp(38px, 5.4vw, 72px);
    margin: 14px 0 20px;
  }
  .cc-footer-cta-inner p {
    font-size: clamp(16px, 1.2vw, 19px);
    color: var(--cc-ink-dim);
    margin: 0 auto 28px;
    max-width: 56ch;
    text-wrap: pretty;
  }
  .cc-footer-install {
    display: inline-flex;
    align-items: center;
    gap: 12px;
    padding: 12px 18px;
    border: 1px solid var(--cc-accent-line, rgba(140, 160, 240, 0.32));
    border-radius: 10px;
    background: rgba(255, 255, 255, 0.02);
    font-family: var(--cc-font-mono), monospace;
    font-size: 13px;
    color: var(--cc-ink);
    margin: 0 auto 24px;
  }
  .cc-footer-install .prompt {
    color: var(--cc-accent, var(--cc-col-shi));
  }
  .cc-footer-install .pkg {
    color: var(--cc-col-bil);
  }
  .cc-footer-text-link {
    color: var(--cc-ink-dim);
    text-decoration: none;
    font-size: 14px;
    border-bottom: 1px solid var(--cc-ink-faint);
    padding-bottom: 2px;
    transition: color 0.15s ease, border-color 0.15s ease;
  }
  .cc-footer-text-link:hover {
    color: var(--cc-ink);
    border-bottom-color: var(--cc-ink);
  }
`;
