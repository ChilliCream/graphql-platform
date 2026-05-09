"use client";

import styled from "styled-components";

// TemplatesRoot owns the dark navy / cream-ink design tokens for the
// /templates index and /templates/[slug] detail pages. Same approach as
// CustomersRoot, PricingRoot, ObservabilityRoot: tokens, section shell,
// typography, button system are shared verbatim across the platform pages.
// The section-specific CSS lives below, scoped by class prefix:
//   .cc-tp-*       templates index sections
//   .cc-tpd-*      templates detail sections
export const TemplatesRoot = styled.div`
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
  section.cc-tp-section,
  section.cc-tpd-section {
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
  .cc-tp-hero {
    padding-top: 160px;
    padding-bottom: 56px;
    text-align: center;
  }
  .cc-tp-hero-inner {
    max-width: 920px;
    margin: 0 auto;
  }
  .cc-tp-hero .kicker {
    display: inline-block;
    padding: 6px 12px;
    border-radius: 999px;
    border: 1px solid var(--cc-ink-faint);
    background: rgba(255, 255, 255, 0.025);
    margin-bottom: 24px;
  }
  .cc-tp-hero h1 {
    font-size: clamp(40px, 6.2vw, 88px);
    margin: 0 0 24px;
    line-height: 1.02;
  }
  .cc-tp-hero h1 .accent {
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
  .cc-tp-hero p {
    font-size: clamp(15px, 1.2vw, 19px);
    line-height: 1.55;
    color: var(--cc-ink-dim);
    max-width: 56ch;
    margin: 0 auto;
    text-wrap: pretty;
  }

  /* ===== 02 Gallery (filter rail + grid) ===== */
  .cc-tp-gallery {
    padding-top: 32px;
    padding-bottom: 96px;
  }
  .cc-tp-gallery-inner {
    max-width: 1320px;
    margin: 0 auto;
    display: grid;
    grid-template-columns: 264px minmax(0, 1fr);
    gap: 48px;
    align-items: start;
  }
  @media (max-width: 980px) {
    .cc-tp-gallery-inner {
      grid-template-columns: 1fr;
      gap: 24px;
    }
  }

  /* ===== Filter rail ===== */
  .cc-tp-rail {
    position: sticky;
    top: 120px;
    display: flex;
    flex-direction: column;
    gap: 26px;
    padding: 24px 22px;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 18px;
    background: rgba(255, 255, 255, 0.025);
    max-height: calc(100vh - 140px);
    overflow-y: auto;
  }
  @media (max-width: 980px) {
    .cc-tp-rail {
      position: relative;
      top: 0;
      max-height: none;
    }
    .cc-tp-rail.is-collapsed {
      display: none;
    }
  }
  .cc-tp-rail-head {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding-bottom: 16px;
    border-bottom: 1px solid var(--cc-ink-faint);
  }
  .cc-tp-rail-title {
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    color: var(--cc-ink);
  }
  .cc-tp-rail-clearall {
    font-family: var(--cc-font-mono), monospace;
    font-size: 10px;
    letter-spacing: 0.16em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
    background: transparent;
    border: none;
    cursor: pointer;
    padding: 0;
    transition: color 0.15s ease;
  }
  .cc-tp-rail-clearall:hover {
    color: var(--cc-ink);
  }
  .cc-tp-axis {
    display: flex;
    flex-direction: column;
    gap: 10px;
  }
  .cc-tp-axis-head {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 8px;
  }
  .cc-tp-axis-title {
    font-family: var(--cc-font-mono), monospace;
    font-size: 10px;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    color: var(--cc-ink);
    display: inline-flex;
    align-items: center;
    gap: 8px;
  }
  .cc-tp-axis-count {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    min-width: 18px;
    padding: 2px 6px;
    border-radius: 999px;
    background: var(--cc-ink);
    color: #0c1322;
    font-family: var(--cc-font-mono), monospace;
    font-size: 10px;
    font-weight: 600;
    letter-spacing: 0.04em;
    line-height: 1;
  }
  .cc-tp-axis-clear {
    font-family: var(--cc-font-mono), monospace;
    font-size: 10px;
    letter-spacing: 0.14em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
    background: transparent;
    border: none;
    cursor: pointer;
    padding: 0;
    transition: color 0.15s ease;
  }
  .cc-tp-axis-clear:hover {
    color: var(--cc-ink);
  }
  .cc-tp-axis-options {
    display: flex;
    flex-wrap: wrap;
    gap: 6px;
  }
  .cc-tp-chip {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    padding: 7px 11px;
    border-radius: 999px;
    border: 1px solid var(--cc-ink-faint);
    background: rgba(255, 255, 255, 0.02);
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.06em;
    color: var(--cc-ink-dim);
    cursor: pointer;
    transition: border-color 0.15s ease, background 0.15s ease, color 0.15s ease;
    text-align: left;
  }
  .cc-tp-chip:hover {
    color: var(--cc-ink);
    border-color: rgba(245, 241, 234, 0.3);
  }
  .cc-tp-chip.is-active {
    color: #0c1322;
    background: var(--cc-ink);
    border-color: var(--cc-ink);
  }
  .cc-tp-toggle {
    display: inline-flex;
    align-items: center;
    gap: 10px;
    padding: 8px 12px;
    border-radius: 999px;
    border: 1px solid var(--cc-ink-faint);
    background: rgba(255, 255, 255, 0.02);
    color: var(--cc-ink);
    cursor: pointer;
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.08em;
    transition: border-color 0.15s ease, background 0.15s ease;
  }
  .cc-tp-toggle:hover {
    border-color: rgba(245, 241, 234, 0.3);
  }
  .cc-tp-toggle.is-active {
    color: #0c1322;
    background: var(--cc-ink);
    border-color: var(--cc-ink);
  }
  .cc-tp-toggle-dot {
    width: 8px;
    height: 8px;
    border-radius: 999px;
    background: var(--cc-col-ord);
  }
  .cc-tp-toggle.is-active .cc-tp-toggle-dot {
    background: #0c1322;
  }

  /* ===== Filter sheet button (mobile only) ===== */
  .cc-tp-rail-toggle {
    display: none;
    align-items: center;
    justify-content: space-between;
    width: 100%;
    padding: 14px 18px;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 14px;
    background: rgba(255, 255, 255, 0.025);
    color: var(--cc-ink);
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.16em;
    text-transform: uppercase;
    cursor: pointer;
  }
  @media (max-width: 980px) {
    .cc-tp-rail-toggle {
      display: inline-flex;
    }
  }
  .cc-tp-rail-toggle .badge {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    min-width: 22px;
    padding: 3px 8px;
    border-radius: 999px;
    background: var(--cc-ink);
    color: #0c1322;
    font-size: 10px;
    font-weight: 600;
    line-height: 1;
  }

  /* ===== Grid + count ===== */
  .cc-tp-grid-wrap {
    display: flex;
    flex-direction: column;
    gap: 18px;
  }
  .cc-tp-grid-bar {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 14px;
    padding-bottom: 14px;
    border-bottom: 1px solid var(--cc-ink-faint);
  }
  .cc-tp-grid-count {
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.16em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
  }
  .cc-tp-grid-count strong {
    color: var(--cc-ink);
    font-weight: 500;
  }
  .cc-tp-grid {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 22px;
  }
  @media (max-width: 1180px) {
    .cc-tp-grid {
      grid-template-columns: repeat(2, 1fr);
    }
  }
  @media (max-width: 720px) {
    .cc-tp-grid {
      grid-template-columns: 1fr;
    }
  }

  /* ===== Template card ===== */
  .cc-tp-card {
    position: relative;
    display: flex;
    flex-direction: column;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 18px;
    background: rgba(255, 255, 255, 0.025);
    text-decoration: none;
    color: inherit;
    overflow: hidden;
    transition: border-color 0.18s ease, transform 0.18s ease,
      background 0.18s ease, box-shadow 0.18s ease;
  }
  .cc-tp-card:hover {
    border-color: rgba(245, 241, 234, 0.35);
    transform: translateY(-2px);
    background: rgba(255, 255, 255, 0.04);
    box-shadow: 0 30px 60px -40px rgba(0, 0, 0, 0.7),
      0 0 0 1px rgba(245, 241, 234, 0.08);
  }
  .cc-tp-card-thumb {
    aspect-ratio: 16 / 9;
    width: 100%;
    border-bottom: 1px solid var(--cc-ink-faint);
    background: linear-gradient(
      180deg,
      rgba(14, 22, 38, 0.7),
      rgba(10, 17, 30, 0.7)
    );
    display: flex;
    align-items: center;
    justify-content: center;
    position: relative;
    overflow: hidden;
  }
  .cc-tp-card-thumb svg {
    width: 100%;
    height: 100%;
    display: block;
  }
  .cc-tp-card-thumb-tag {
    position: absolute;
    top: 12px;
    left: 12px;
    font-family: var(--cc-font-mono), monospace;
    font-size: 9px;
    letter-spacing: 0.16em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
    padding: 4px 8px;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 6px;
    background: rgba(8, 14, 26, 0.7);
  }
  .cc-tp-card-thumb-agent {
    position: absolute;
    top: 12px;
    right: 12px;
    font-family: var(--cc-font-mono), monospace;
    font-size: 9px;
    letter-spacing: 0.14em;
    text-transform: uppercase;
    color: #0c1322;
    padding: 4px 8px;
    border-radius: 6px;
    background: var(--cc-ink);
    font-weight: 600;
  }
  .cc-tp-card-body {
    display: flex;
    flex-direction: column;
    padding: 22px 22px 20px;
    gap: 12px;
    flex: 1;
  }
  .cc-tp-card-title {
    font-size: 19px;
    font-weight: 500;
    letter-spacing: -0.015em;
    color: var(--cc-ink);
    margin: 0;
    line-height: 1.25;
  }
  .cc-tp-card-tagline {
    font-size: 14px;
    line-height: 1.55;
    color: var(--cc-ink-dim);
    margin: 0;
    flex: 1;
    text-wrap: pretty;
  }
  .cc-tp-card-chips {
    display: flex;
    flex-wrap: wrap;
    gap: 6px;
    padding-top: 12px;
    border-top: 1px solid var(--cc-ink-faint);
  }
  .cc-tp-product-chip {
    font-family: var(--cc-font-mono), monospace;
    font-size: 10px;
    letter-spacing: 0.08em;
    text-transform: uppercase;
    color: var(--cc-ink);
    padding: 4px 8px;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 6px;
    background: rgba(255, 255, 255, 0.025);
    line-height: 1;
  }

  /* ===== Empty state ===== */
  .cc-tp-empty {
    grid-column: 1 / -1;
    border: 1px dashed var(--cc-ink-faint);
    border-radius: 18px;
    padding: 56px 28px;
    text-align: center;
    background: rgba(255, 255, 255, 0.02);
  }
  .cc-tp-empty h3 {
    font-size: 22px;
    font-weight: 500;
    letter-spacing: -0.02em;
    color: var(--cc-ink);
    margin: 0 0 10px;
  }
  .cc-tp-empty p {
    font-size: 14px;
    color: var(--cc-ink-dim);
    margin: 0 0 18px;
    line-height: 1.55;
  }
  .cc-tp-empty button {
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
  .cc-tp-empty button:hover {
    border-color: var(--cc-ink);
    background: rgba(255, 255, 255, 0.05);
  }

  /* ===== 03 CTA strip ===== */
  .cc-tp-ctastrip {
    padding-top: 0;
    padding-bottom: 140px;
  }
  .cc-tp-ctastrip-inner {
    max-width: 1180px;
    margin: 0 auto;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 22px;
    background: rgba(255, 255, 255, 0.025);
    padding: 44px 44px;
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 24px;
    flex-wrap: wrap;
  }
  .cc-tp-ctastrip-text {
    flex: 1;
    min-width: 0;
  }
  .cc-tp-ctastrip-text .eyebrow {
    margin-bottom: 8px;
    display: block;
  }
  .cc-tp-ctastrip h2 {
    font-size: clamp(22px, 2.6vw, 30px);
    font-weight: 500;
    letter-spacing: -0.02em;
    color: var(--cc-ink);
    margin: 0;
    line-height: 1.2;
    text-wrap: balance;
  }

  /* ============================================================
   *                     DETAIL PAGE
   * ============================================================ */

  /* ===== Detail header ===== */
  .cc-tpd-header {
    padding-top: 140px;
    padding-bottom: 32px;
  }
  .cc-tpd-header-inner {
    max-width: 1180px;
    margin: 0 auto;
  }
  .cc-tpd-breadcrumb {
    display: inline-flex;
    align-items: center;
    gap: 8px;
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.16em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
    margin-bottom: 24px;
  }
  .cc-tpd-breadcrumb a {
    color: var(--cc-ink-dim);
    text-decoration: none;
    transition: color 0.15s ease;
  }
  .cc-tpd-breadcrumb a:hover {
    color: var(--cc-ink);
  }
  .cc-tpd-breadcrumb .sep {
    color: var(--cc-ink-faint);
  }
  .cc-tpd-breadcrumb .crumb-current {
    color: var(--cc-ink);
  }
  .cc-tpd-header h1 {
    font-size: clamp(34px, 4.6vw, 60px);
    margin: 0 0 18px;
    max-width: 22ch;
    text-wrap: balance;
    line-height: 1.05;
  }
  .cc-tpd-header h1 .accent {
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
  .cc-tpd-tagline {
    font-size: clamp(15px, 1.2vw, 18px);
    line-height: 1.55;
    color: var(--cc-ink-dim);
    margin: 0;
    max-width: 60ch;
    text-wrap: pretty;
  }

  /* ===== Detail body + sticky sidebar ===== */
  .cc-tpd-body-section {
    padding-top: 32px;
    padding-bottom: 96px;
  }
  .cc-tpd-body-inner {
    max-width: 1180px;
    margin: 0 auto;
    display: grid;
    grid-template-columns: minmax(0, 1fr) 340px;
    gap: 56px;
    align-items: start;
  }
  @media (max-width: 980px) {
    .cc-tpd-body-inner {
      grid-template-columns: 1fr;
      gap: 40px;
    }
  }
  .cc-tpd-body-main {
    min-width: 0;
    max-width: 760px;
    font-size: 17px;
    line-height: 1.7;
    color: var(--cc-ink);
  }
  .cc-tpd-body-main h2 {
    font-size: clamp(22px, 2.4vw, 30px);
    font-weight: 500;
    letter-spacing: -0.02em;
    color: var(--cc-ink);
    margin: 56px 0 18px;
    line-height: 1.2;
  }
  .cc-tpd-body-main h2:first-child {
    margin-top: 0;
  }
  .cc-tpd-body-main p {
    margin: 0 0 18px;
    color: var(--cc-ink);
    text-wrap: pretty;
  }

  /* ===== Code blocks ===== */
  .cc-tpd-code {
    position: relative;
    margin: 22px 0 24px;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 12px;
    background: rgba(8, 14, 26, 0.7);
    overflow: hidden;
  }
  .cc-tpd-code-head {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 10px 14px;
    border-bottom: 1px solid var(--cc-ink-faint);
    background: rgba(255, 255, 255, 0.02);
  }
  .cc-tpd-code-lang {
    font-family: var(--cc-font-mono), monospace;
    font-size: 10px;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
  }
  .cc-tpd-code-copy {
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
  .cc-tpd-code-copy:hover {
    border-color: rgba(245, 241, 234, 0.35);
    background: rgba(255, 255, 255, 0.04);
  }
  .cc-tpd-code-copy.is-copied {
    color: var(--cc-col-ord);
    border-color: rgba(118, 200, 150, 0.45);
    background: rgba(118, 200, 150, 0.06);
  }
  .cc-tpd-code pre {
    margin: 0;
    padding: 18px 16px;
    overflow-x: auto;
    font-family: var(--cc-font-mono), monospace;
    font-size: 13px;
    line-height: 1.6;
    color: var(--cc-ink);
    text-shadow: 0 0 30px rgba(245, 241, 234, 0.08);
  }
  .cc-tpd-code pre code {
    font-family: inherit;
    font-size: inherit;
    color: inherit;
    background: transparent;
    padding: 0;
  }

  /* ===== CLI tabs ===== */
  .cc-tpd-clitabs {
    border: 1px solid var(--cc-ink-faint);
    border-radius: 12px;
    overflow: hidden;
    background: rgba(8, 14, 26, 0.7);
  }
  .cc-tpd-clitabs-head {
    display: flex;
    border-bottom: 1px solid var(--cc-ink-faint);
    background: rgba(255, 255, 255, 0.02);
  }
  .cc-tpd-clitabs-tab {
    flex: 0 0 auto;
    padding: 10px 14px;
    border: none;
    background: transparent;
    color: var(--cc-ink-dim);
    font-family: var(--cc-font-mono), monospace;
    font-size: 10px;
    letter-spacing: 0.16em;
    text-transform: uppercase;
    cursor: pointer;
    border-bottom: 2px solid transparent;
    margin-bottom: -1px;
    transition: color 0.15s ease, border-color 0.15s ease;
  }
  .cc-tpd-clitabs-tab:hover {
    color: var(--cc-ink);
  }
  .cc-tpd-clitabs-tab.is-active {
    color: var(--cc-ink);
    border-bottom-color: var(--cc-ink);
  }
  .cc-tpd-clitabs-body {
    position: relative;
  }
  .cc-tpd-clitabs-body pre {
    margin: 0;
    padding: 18px 16px 18px 16px;
    overflow-x: auto;
    font-family: var(--cc-font-mono), monospace;
    font-size: 13px;
    line-height: 1.6;
    color: var(--cc-ink);
  }
  .cc-tpd-clitabs-copy {
    position: absolute;
    top: 8px;
    right: 8px;
    display: inline-flex;
    align-items: center;
    gap: 6px;
    font-family: var(--cc-font-mono), monospace;
    font-size: 10px;
    letter-spacing: 0.16em;
    text-transform: uppercase;
    color: var(--cc-ink);
    background: rgba(8, 14, 26, 0.85);
    border: 1px solid var(--cc-ink-faint);
    border-radius: 6px;
    padding: 5px 10px;
    cursor: pointer;
    transition: border-color 0.15s ease, background 0.15s ease;
  }
  .cc-tpd-clitabs-copy:hover {
    border-color: rgba(245, 241, 234, 0.35);
  }
  .cc-tpd-clitabs-copy.is-copied {
    color: var(--cc-col-ord);
    border-color: rgba(118, 200, 150, 0.45);
  }

  /* ===== Sticky deploy sidebar ===== */
  .cc-tpd-sidebar {
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
    gap: 18px;
  }
  @media (max-width: 980px) {
    .cc-tpd-sidebar {
      position: relative;
      top: 0;
    }
  }
  .cc-tpd-sidebar-section-title {
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    color: var(--cc-ink);
    margin: 0;
  }
  .cc-tpd-sidebar-cta {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    gap: 10px;
    width: 100%;
    padding: 13px 18px;
    border-radius: 12px;
    font-family: var(--cc-font-sans), sans-serif;
    font-size: 14px;
    font-weight: 500;
    text-decoration: none;
    cursor: pointer;
    border: 1px solid var(--cc-ink-faint);
    background: rgba(255, 255, 255, 0.025);
    color: var(--cc-ink);
    transition: border-color 0.15s ease, background 0.15s ease,
      transform 0.12s ease;
  }
  .cc-tpd-sidebar-cta:hover {
    border-color: rgba(245, 241, 234, 0.35);
    background: rgba(255, 255, 255, 0.05);
  }
  .cc-tpd-sidebar-cta.is-primary {
    background: var(--cc-ink);
    color: #0c1322;
    border-color: var(--cc-ink);
  }
  .cc-tpd-sidebar-cta.is-primary:hover {
    transform: translateY(-1px);
    background: #ffffff;
  }
  .cc-tpd-sidebar-divider {
    height: 1px;
    background: var(--cc-ink-faint);
    margin: 4px -2px;
  }
  .cc-tpd-sidebar-row {
    display: flex;
    flex-direction: column;
    gap: 8px;
  }
  .cc-tpd-sidebar-row .label {
    font-family: var(--cc-font-mono), monospace;
    font-size: 10px;
    letter-spacing: 0.16em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
  }
  .cc-tpd-sidebar-row .value {
    font-size: 14px;
    color: var(--cc-ink);
    line-height: 1.45;
  }
  .cc-tpd-sidebar-row .value a {
    color: var(--cc-ink);
    text-decoration: underline;
    text-underline-offset: 3px;
    text-decoration-color: var(--cc-ink-faint);
    transition: text-decoration-color 0.15s ease;
  }
  .cc-tpd-sidebar-row .value a:hover {
    text-decoration-color: var(--cc-ink);
  }
  .cc-tpd-sidebar-tagchips {
    display: flex;
    flex-wrap: wrap;
    gap: 6px;
  }
  .cc-tpd-sidebar-tagchip {
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
  .cc-tpd-sidebar-tagchip:hover {
    border-color: rgba(245, 241, 234, 0.35);
    background: rgba(255, 255, 255, 0.05);
  }

  /* ===== Related templates rail ===== */
  .cc-tpd-related {
    padding-top: 0;
    padding-bottom: 140px;
  }
  .cc-tpd-related-inner {
    max-width: 1280px;
    margin: 0 auto;
  }
  .cc-tpd-related-heading {
    text-align: center;
    margin: 0 auto 36px;
  }
  .cc-tpd-related-heading h2 {
    font-size: clamp(26px, 3.2vw, 38px);
    margin: 8px auto 0;
    max-width: 22ch;
    line-height: 1.1;
  }
`;
