"use client";

import styled from "styled-components";

// PricingRoot holds the dark navy / cream-ink design tokens for the /pricing
// page. It mirrors DesktopLandingRoot's tokens, section shell, typography, and
// button system but stays self-contained: the pricing page has its own
// sections (hero, OSS strip, tier cards, comparison table, enterprise banner,
// FAQ, footer CTA) and doesn't share component CSS with the landing acts.
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

  /* ===== Section shell ===== */
  section.cc-pricing-section {
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

  /* ===== 01 Hero ===== */
  .cc-pricing-hero {
    padding-top: 160px;
    padding-bottom: 80px;
    text-align: center;
  }
  .cc-pricing-hero-inner {
    max-width: 880px;
    margin: 0 auto;
  }
  .cc-pricing-hero h1 {
    font-size: clamp(40px, 6vw, 88px);
    margin: 18px 0 24px;
    line-height: 1.02;
  }
  .cc-pricing-hero h1 .accent {
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
  .cc-pricing-hero p {
    font-size: clamp(15px, 1.2vw, 19px);
    line-height: 1.5;
    color: var(--cc-ink-dim);
    max-width: 60ch;
    margin: 0 auto;
    text-wrap: pretty;
  }

  /* ===== 02 OSS strip ===== */
  .cc-oss-strip {
    padding-top: 32px;
    padding-bottom: 88px;
  }
  .cc-oss-inner {
    max-width: 1080px;
    margin: 0 auto;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 18px;
    background: rgba(255, 255, 255, 0.025);
    padding: 32px 36px;
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
    border-color: rgba(245, 241, 234, 0.32);
    background: rgba(245, 241, 234, 0.06);
  }
  .cc-oss-line {
    font-size: 16px;
    color: var(--cc-ink-dim);
    line-height: 1.55;
    margin: 0;
  }
  .cc-oss-line strong {
    color: var(--cc-ink);
    font-weight: 500;
  }
  .cc-oss-terminal {
    border: 1px solid var(--cc-ink-faint);
    border-radius: 12px;
    background: rgba(8, 14, 26, 0.85);
    padding: 14px 18px;
    font-family: var(--cc-font-mono), monospace;
    font-size: 13px;
    color: var(--cc-ink);
    display: inline-flex;
    align-items: center;
    gap: 14px;
    min-width: 320px;
    box-shadow: inset 0 0 0 1px rgba(245, 241, 234, 0.04);
  }
  .cc-oss-terminal .prompt {
    color: var(--cc-col-shi);
  }
  .cc-oss-terminal .cmd {
    color: var(--cc-ink);
  }
  .cc-oss-terminal .pkg {
    color: var(--cc-col-bil);
  }

  /* ===== 03 Tier cards ===== */
  .cc-tiers {
    padding-top: 24px;
    padding-bottom: 96px;
  }
  .cc-tiers-inner {
    max-width: 1280px;
    margin: 0 auto;
  }
  .cc-tiers-heading {
    text-align: center;
    margin: 0 auto 56px;
    max-width: 720px;
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
    border: 1px solid var(--cc-ink-faint);
    border-radius: 18px;
    background: rgba(255, 255, 255, 0.025);
    transition: border-color 0.18s ease, transform 0.18s ease;
  }
  .cc-tier-card:hover {
    border-color: rgba(245, 241, 234, 0.28);
    transform: translateY(-2px);
  }
  .cc-tier-card.is-featured {
    border-color: rgba(245, 241, 234, 0.45);
    background: rgba(255, 255, 255, 0.06);
    box-shadow: 0 0 0 1px rgba(245, 241, 234, 0.1) inset,
      0 30px 80px -40px rgba(0, 0, 0, 0.6),
      0 0 60px -20px rgba(120, 140, 220, 0.18);
  }
  .cc-tier-badge {
    position: absolute;
    top: -12px;
    left: 50%;
    transform: translateX(-50%);
    padding: 5px 12px;
    border-radius: 999px;
    background: var(--cc-ink);
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
    color: var(--cc-col-shi);
    flex-shrink: 0;
    margin-top: 4px;
  }
  .cc-tier-card .cc-btn {
    width: 100%;
    padding: 14px 22px;
    font-size: 14px;
  }

  /* ===== 04 Spend controls ===== */
  .cc-spend-controls {
    padding-top: 0;
    padding-bottom: 96px;
  }
  .cc-spend-inner {
    max-width: 1080px;
    margin: 0 auto;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 18px;
    background: rgba(255, 255, 255, 0.02);
    padding: 36px;
  }
  .cc-spend-heading {
    text-align: center;
    margin: 0 0 28px;
  }
  .cc-spend-heading p {
    font-size: clamp(17px, 1.4vw, 22px);
    color: var(--cc-ink);
    margin: 8px 0 0;
    font-weight: 500;
    letter-spacing: -0.01em;
  }
  .cc-spend-grid {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 18px;
  }
  @media (max-width: 880px) {
    .cc-spend-grid {
      grid-template-columns: 1fr;
    }
  }
  .cc-spend-tile {
    display: flex;
    flex-direction: column;
    gap: 12px;
    padding: 22px 22px;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 14px;
    background: rgba(255, 255, 255, 0.025);
  }
  .cc-spend-tile-icon {
    width: 36px;
    height: 36px;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 10px;
    display: flex;
    align-items: center;
    justify-content: center;
    color: var(--cc-col-shi);
  }
  .cc-spend-tile-title {
    font-size: 15px;
    font-weight: 500;
    color: var(--cc-ink);
    margin: 0;
  }
  .cc-spend-tile-body {
    font-size: 14px;
    line-height: 1.55;
    color: var(--cc-ink-dim);
    margin: 0;
  }

  /* ===== 05 Comparison table ===== */
  .cc-compare {
    padding-top: 56px;
    padding-bottom: 120px;
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
    border: 1px solid var(--cc-ink-faint);
    border-left: 2px solid var(--cc-col-ord);
    border-radius: 10px;
    background: rgba(255, 255, 255, 0.02);
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
    border: 1px solid var(--cc-ink-faint);
    border-radius: 16px;
    background: rgba(255, 255, 255, 0.015);
  }
  .cc-compare-table {
    width: 100%;
    min-width: 1080px;
    border-collapse: collapse;
    font-family: var(--cc-font-sans), sans-serif;
  }
  .cc-compare-table thead th {
    position: sticky;
    top: 0;
    z-index: 3;
    background: linear-gradient(180deg, #0c1322 0%, #0c1322 80%, transparent);
    text-align: left;
    padding: 22px 18px 18px;
    border-bottom: 1px solid var(--cc-ink-faint);
    vertical-align: bottom;
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
      rgba(12, 19, 34, 0)
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
    padding: 28px 18px 8px;
    border-bottom: 1px solid var(--cc-ink-faint);
    background: rgba(255, 255, 255, 0.02);
  }
  .cc-compare-group-title {
    font-size: 13px;
    font-family: var(--cc-font-mono), monospace;
    font-weight: 500;
    letter-spacing: 0.16em;
    text-transform: uppercase;
    color: var(--cc-ink);
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
    background: rgba(255, 255, 255, 0.015);
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
    color: var(--cc-col-shi);
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

  /* ===== 06 Enterprise banner ===== */
  .cc-enterprise {
    padding-top: 0;
    padding-bottom: 120px;
  }
  .cc-enterprise-inner {
    max-width: 1180px;
    margin: 0 auto;
    position: relative;
    border-radius: 22px;
    padding: 1px;
    background: linear-gradient(
      135deg,
      rgba(245, 241, 234, 0.28),
      rgba(245, 241, 234, 0.04) 35%,
      rgba(120, 140, 220, 0.22) 70%,
      rgba(245, 241, 234, 0.06)
    );
    box-shadow: 0 30px 80px -40px rgba(0, 0, 0, 0.7),
      0 0 60px -10px rgba(120, 140, 220, 0.18);
  }
  .cc-enterprise-card {
    border-radius: 21px;
    background: linear-gradient(
      180deg,
      rgba(14, 22, 38, 0.96),
      rgba(10, 17, 30, 0.96)
    );
    padding: 56px 56px 52px;
    display: grid;
    grid-template-columns: minmax(0, 1.05fr) minmax(0, 1fr);
    gap: 56px;
    align-items: start;
  }
  @media (max-width: 880px) {
    .cc-enterprise-card {
      grid-template-columns: 1fr;
      padding: 36px 28px;
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
    margin: 0 0 28px;
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
    color: var(--cc-col-shi);
    flex-shrink: 0;
    margin-top: 3px;
  }

  /* ===== 07 FAQ ===== */
  .cc-faq {
    padding-top: 0;
    padding-bottom: 120px;
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
    color: var(--cc-col-shi);
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
    color: var(--cc-ink);
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

  /* ===== 08 Footer CTA ===== */
  .cc-footer-cta {
    padding-top: 60px;
    padding-bottom: 140px;
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
    margin: 0 auto 32px;
    max-width: 56ch;
    text-wrap: pretty;
  }
`;
