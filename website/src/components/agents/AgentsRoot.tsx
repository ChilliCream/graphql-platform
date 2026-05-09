"use client";

import styled from "styled-components";

// AgentsRoot owns the dark navy / cream-ink design tokens for the
// /products/nitro/agents page. Mirrors PricingRoot, EnterpriseRoot and
// ObservabilityRoot 1:1: same tokens, same section shell, same typography
// and button system, with one extra accent — `--cc-amber` — used to signal
// agent activity (instrumentation in motion, not the typical AI purple/teal).
//
// Amber is a SYSTEM SIGNAL on this page, not paint. Reserved for:
//   - AGENT pill in transcripts
//   - The Loop pulse + stage indicators
//   - The "agents on your platform" column in the reframe
//   - The amber chips on FEDERATION-WIDE / SCHEMA-TYPED / LIVE RUNTIME
//   - "Add MCP →" links on IDE clients
//   - The pricing-teaser CTA hover state
//   - The guardrail card edges (where edge IS the constraint signal)
// Anywhere else, dial back to ink-dim.
//
// Sections are wrapped in <Band> from the foundation. The legacy
// `--cc-amber` alias is preserved for any CSS that references it directly.
// The page is also wrapped in <AccentThread page="agents"> at page level
// so `--cc-accent` resolves to the same amber token through the foundation.
export const AgentsRoot = styled.div`
  --cc-ink: #f5f1ea;
  --cc-ink-dim: rgba(245, 241, 234, 0.62);
  --cc-ink-faint: rgba(245, 241, 234, 0.16);
  --cc-line-w: 1.5px;
  --cc-col-cat: oklch(0.74 0.18 30);
  --cc-col-bil: oklch(0.82 0.16 90);
  --cc-col-ord: oklch(0.76 0.16 150);
  --cc-col-shi: oklch(0.74 0.14 220);
  --cc-col-usr: oklch(0.72 0.18 310);
  /* Warm yellow-amber for agent activity. Sits between --cc-col-bil (cream
     yellow) and --cc-col-cat (terracotta) so it visually rhymes with the
     existing palette while reading clearly as "instrumentation in motion"
     rather than chatbot chrome. */
  --cc-amber: oklch(0.78 0.16 70);
  --cc-amber-soft: rgba(247, 186, 100, 0.14);
  --cc-amber-line: rgba(247, 186, 100, 0.42);
  --cc-pad-x: clamp(28px, 5vw, 96px);

  position: relative;
  width: 100%;
  color: var(--cc-ink);
  font-family: var(--cc-font-sans), system-ui, sans-serif;
  background: radial-gradient(
      80% 50% at 70% 0%,
      rgba(247, 186, 100, 0.08),
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
  section.cc-ag-section {
    position: relative;
    width: 100%;
    padding-left: var(--cc-pad-x);
    padding-right: var(--cc-pad-x);
  }
  .cc-section-label {
    position: relative;
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.18em;
    color: var(--cc-ink-dim);
    text-transform: uppercase;
    z-index: 4;
    display: inline-flex;
    align-items: center;
    gap: 10px;
    margin-bottom: 28px;
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
  /* Legacy section labels that still rely on absolute positioning inside a
     bordered .cc-ag-feature-inner. New components route through .cc-ag-band-inner
     which uses inline labels. */
  section.cc-ag-section .cc-section-label {
    position: absolute;
    top: 36px;
    left: var(--cc-pad-x);
    margin-bottom: 0;
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
  .accent-amber {
    color: var(--cc-amber);
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

  /* ===== Band-inner shell (replaces cc-ag-feature-inner for non-card sections) =====
     Provides max-width + a content column without card chrome. Used by every
     section that isn't the hero or guardrails (the only card-chrome section
     is GuardrailsSection). */
  .cc-ag-band-inner {
    max-width: 1280px;
    margin: 0 auto;
    position: relative;
  }

  /* When a Band uses variant="tinted" the foundation paints the section
     background cream. The page tokens need to flip in scope so cream-on-cream
     text doesn't disappear. .cc-ag-tint-scope is the explicit signal. The
     global h1-h6 rule binds to --cc-heading-text-color (see global-style.tsx),
     so the scope overrides that token too. */
  .cc-ag-tint-scope {
    --cc-ink: #1a1f2e;
    --cc-ink-dim: rgba(26, 31, 46, 0.66);
    --cc-ink-faint: rgba(26, 31, 46, 0.16);
    --cc-heading-text-color: #1a1f2e;
    color: var(--cc-ink);
  }
  .cc-ag-tint-scope .cc-section-label .num {
    border-color: var(--cc-ink-faint);
    color: var(--cc-ink);
  }

  /* ===== 01 Hero ===== */
  .cc-ag-hero-label {
    margin-bottom: 36px;
  }
  .cc-ag-hero-inner {
    max-width: 1280px;
    margin: 0 auto;
    display: grid;
    grid-template-columns: minmax(0, 1fr) minmax(0, 1.05fr);
    gap: 64px;
    align-items: center;
    padding-top: 60px;
  }
  .cc-ag-hero-inner > .cc-ag-hero-label {
    grid-column: 1 / -1;
  }
  @media (max-width: 980px) {
    .cc-ag-hero-inner {
      grid-template-columns: 1fr;
      gap: 48px;
    }
  }
  .cc-ag-hero-copy h1 {
    font-size: clamp(40px, 6vw, 88px);
    margin: 18px 0 24px;
    line-height: 1.02;
  }
  .cc-ag-hero-copy h1 .accent {
    background: linear-gradient(
      120deg,
      var(--cc-amber),
      var(--cc-col-bil) 60%,
      var(--cc-col-shi)
    );
    -webkit-background-clip: text;
    background-clip: text;
    -webkit-text-fill-color: transparent;
  }
  .cc-ag-hero-copy p {
    font-size: clamp(15px, 1.2vw, 19px);
    line-height: 1.55;
    color: var(--cc-ink-dim);
    max-width: 56ch;
    margin: 0 0 32px;
    text-wrap: pretty;
  }
  .cc-ag-hero-cta {
    display: flex;
    gap: 14px;
    flex-wrap: wrap;
  }

  /* ===== Hero terminal mock ===== */
  .cc-term {
    position: relative;
    border-radius: 22px;
    padding: 1px;
    background: linear-gradient(
      135deg,
      rgba(247, 186, 100, 0.32),
      rgba(245, 241, 234, 0.04) 35%,
      rgba(247, 186, 100, 0.18) 70%,
      rgba(245, 241, 234, 0.06)
    );
    box-shadow: 0 30px 80px -40px rgba(0, 0, 0, 0.7),
      0 0 60px -10px rgba(247, 186, 100, 0.18);
  }
  .cc-term-inner {
    border-radius: 21px;
    background: linear-gradient(
      180deg,
      rgba(14, 22, 38, 0.96),
      rgba(10, 17, 30, 0.96)
    );
    padding: 0;
    overflow: hidden;
    display: flex;
    flex-direction: column;
  }
  .cc-term-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 14px 20px;
    border-bottom: 1px solid var(--cc-ink-faint);
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.14em;
    color: var(--cc-ink-dim);
    text-transform: uppercase;
  }
  .cc-term-header .dots {
    display: inline-flex;
    gap: 6px;
  }
  .cc-term-header .dots span {
    width: 8px;
    height: 8px;
    border-radius: 999px;
    background: var(--cc-ink-faint);
  }
  .cc-term-body {
    padding: 20px 22px 22px;
    font-family: var(--cc-font-mono), monospace;
    font-size: 12.5px;
    line-height: 1.7;
    color: var(--cc-ink);
    min-height: 360px;
    display: flex;
    flex-direction: column;
    gap: 6px;
  }
  .cc-term-line {
    display: flex;
    gap: 10px;
    align-items: flex-start;
    opacity: 0;
    animation: cc-term-fade-in 0.35s ease forwards;
  }
  .cc-term-line.is-out {
    animation: cc-term-fade-out 0.35s ease forwards;
  }
  @keyframes cc-term-fade-in {
    from {
      opacity: 0;
      transform: translateY(4px);
    }
    to {
      opacity: 1;
      transform: translateY(0);
    }
  }
  @keyframes cc-term-fade-out {
    from {
      opacity: 1;
      transform: translateY(0);
    }
    to {
      opacity: 0.18;
      transform: translateY(-2px);
    }
  }
  .cc-term-pill {
    display: inline-flex;
    align-items: center;
    flex-shrink: 0;
    padding: 1px 7px;
    border-radius: 5px;
    font-size: 10px;
    letter-spacing: 0.14em;
    line-height: 1.4;
    text-transform: uppercase;
  }
  .cc-term-pill.is-user {
    background: rgba(255, 255, 255, 0.04);
    color: var(--cc-ink-dim);
    border: 1px solid var(--cc-ink-faint);
  }
  .cc-term-pill.is-agent {
    background: var(--cc-amber-soft);
    color: var(--cc-amber);
    border: 1px solid var(--cc-amber-line);
  }
  .cc-term-pill.is-mcp {
    background: rgba(120, 140, 220, 0.1);
    color: var(--cc-col-shi);
    border: 1px solid rgba(120, 140, 220, 0.32);
  }
  .cc-term-line .body {
    color: var(--cc-ink);
    overflow-wrap: anywhere;
    flex: 1;
  }
  .cc-term-line .body .arg {
    color: var(--cc-col-bil);
  }
  .cc-term-line .body .key {
    color: var(--cc-ink-dim);
  }
  .cc-term-line .body .ok {
    color: var(--cc-col-ord);
  }
  .cc-term-cursor {
    display: inline-block;
    width: 8px;
    height: 14px;
    background: var(--cc-amber);
    margin-left: 6px;
    vertical-align: -2px;
    animation: cc-term-blink 1s steps(2, end) infinite;
  }
  @keyframes cc-term-blink {
    50% {
      opacity: 0;
    }
  }
  .cc-term-prompt {
    color: var(--cc-amber);
    margin-right: 4px;
  }

  /* ===== Section header (H2 + sub-paragraph) =====
     The card chrome that used to live around every section was removed in
     favour of the Band primitive — see CC2 in the uplift plan. The header
     class survives because it's a pure typographic block. */
  .cc-ag-feature-header {
    max-width: 740px;
    margin: 0 0 36px;
  }
  .cc-ag-feature-header h2 {
    font-size: clamp(30px, 3.6vw, 46px);
    margin: 6px 0 12px;
    line-height: 1.05;
  }
  .cc-ag-feature-header p {
    font-size: clamp(15px, 1.1vw, 18px);
    color: var(--cc-ink-dim);
    margin: 0;
    line-height: 1.55;
    max-width: 60ch;
    text-wrap: pretty;
  }
  /* ===== 02 Reframe ===== */
  .cc-ag-reframe-grid {
    display: grid;
    grid-template-columns: 1fr 1px 1fr;
    gap: 32px;
    align-items: stretch;
    margin-top: 16px;
  }
  @media (max-width: 880px) {
    .cc-ag-reframe-grid {
      grid-template-columns: 1fr;
      gap: 16px;
    }
  }
  .cc-ag-reframe-col {
    padding: 24px 4px;
    display: flex;
    flex-direction: column;
    gap: 14px;
  }
  .cc-ag-reframe-col.is-muted {
    opacity: 0.55;
  }
  .cc-ag-reframe-col.is-bright .cc-ag-reframe-h {
    color: var(--cc-amber);
  }
  .cc-ag-reframe-h {
    font-family: var(--cc-font-sans), sans-serif;
    font-size: clamp(22px, 2.1vw, 28px);
    font-weight: 500;
    letter-spacing: -0.02em;
    color: var(--cc-ink);
    margin: 0;
    line-height: 1.15;
  }
  .cc-ag-reframe-body {
    font-size: 15px;
    line-height: 1.6;
    color: var(--cc-ink-dim);
    margin: 0;
    text-wrap: pretty;
  }
  .cc-ag-reframe-bullets {
    list-style: none;
    margin: 8px 0 0;
    padding: 0;
    display: flex;
    flex-direction: column;
    gap: 8px;
  }
  .cc-ag-reframe-bullets li {
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.14em;
    text-transform: uppercase;
    color: var(--cc-ink);
    padding: 6px 10px;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 8px;
    background: rgba(255, 255, 255, 0.02);
    display: inline-block;
    width: fit-content;
  }
  .cc-ag-reframe-col.is-bright .cc-ag-reframe-bullets li {
    border-color: var(--cc-amber-line);
    background: var(--cc-amber-soft);
    color: var(--cc-amber);
  }
  .cc-ag-reframe-divider {
    position: relative;
    background: var(--cc-ink-faint);
    width: 1px;
  }
  .cc-ag-reframe-divider::after {
    content: "vs";
    position: absolute;
    top: 50%;
    left: 50%;
    transform: translate(-50%, -50%);
    font-family: var(--cc-font-mono), monospace;
    font-size: 10px;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
    /* Inherits the band background through the band's wrapper colour rather
       than baking in the dark token, so this works on tinted (cream) bands
       too. */
    background: var(--cc-band-bg, #0c1322);
    padding: 6px 8px;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 999px;
    line-height: 1;
  }
  /* Inside a tinted-scope band the "vs" pill should sit on cream. */
  .cc-ag-tint-scope .cc-ag-reframe-divider::after {
    background: #f8f4ec;
  }
  @media (max-width: 880px) {
    .cc-ag-reframe-divider {
      width: auto;
      height: 1px;
    }
  }
  .cc-ag-reframe-punch {
    margin-top: 32px;
    padding-top: 28px;
    border-top: 1px dashed var(--cc-ink-faint);
    text-align: center;
    font-family: var(--cc-font-sans), sans-serif;
    font-size: clamp(18px, 2vw, 22px);
    font-weight: 500;
    color: var(--cc-ink);
    letter-spacing: -0.015em;
  }
  .cc-ag-reframe-punch .accent {
    color: var(--cc-amber);
  }

  /* ===== 03 Loop diagram (full-bleed accent band) =====
     The Loop is the page's intellectual property. Five interlocking arcs
     ride a wide SVG; an amber pulse travels along the path once the band
     enters the viewport (gated by data-active on .cc-ag-loop). Stage strip
     above the diagram serves as both legend and TOC. */
  .cc-ag-loop-band {
    max-width: 1280px;
    margin: 0 auto;
    position: relative;
  }
  .cc-ag-loop-header {
    max-width: 880px;
    margin: 0 0 32px;
  }
  .cc-ag-loop-header h2 {
    font-size: clamp(34px, 4.6vw, 60px);
    margin: 6px 0 14px;
    line-height: 1.02;
  }
  .cc-ag-loop-header h2 .sep {
    color: var(--cc-ink-faint);
    margin: 0 0.05em;
  }
  .cc-ag-loop-header p {
    font-size: clamp(15px, 1.15vw, 18px);
    color: var(--cc-ink-dim);
    margin: 0;
    line-height: 1.55;
    max-width: 64ch;
    text-wrap: pretty;
  }

  .cc-ag-loop-stage-strip {
    display: grid;
    grid-template-columns: repeat(5, 1fr);
    gap: 18px;
    margin: 0 0 36px;
    padding: 24px 0;
    border-top: 1px solid var(--cc-amber-line);
    border-bottom: 1px solid var(--cc-amber-line);
  }
  @media (max-width: 980px) {
    .cc-ag-loop-stage-strip {
      grid-template-columns: repeat(2, 1fr);
    }
  }
  @media (max-width: 560px) {
    .cc-ag-loop-stage-strip {
      grid-template-columns: 1fr;
    }
  }
  .cc-ag-loop-strip-cell {
    display: grid;
    grid-template-columns: auto 1fr;
    gap: 12px;
    align-items: flex-start;
  }
  .cc-ag-loop-strip-cell .step {
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    color: var(--cc-amber);
    padding: 4px 6px;
    border: 1px solid var(--cc-amber-line);
    border-radius: 4px;
    line-height: 1;
  }
  .cc-ag-loop-strip-cell .meta {
    display: flex;
    flex-direction: column;
    gap: 4px;
  }
  .cc-ag-loop-strip-cell h4 {
    font-family: var(--cc-font-sans), sans-serif;
    font-size: 16px;
    font-weight: 500;
    letter-spacing: -0.015em;
    margin: 0;
    color: var(--cc-ink);
    line-height: 1.2;
  }
  .cc-ag-loop-strip-cell p {
    font-size: 12.5px;
    line-height: 1.5;
    color: var(--cc-ink-dim);
    margin: 0;
    text-wrap: pretty;
  }
  .cc-ag-loop-strip-cell .primitive {
    font-family: var(--cc-font-mono), monospace;
    font-size: 9.5px;
    letter-spacing: 0.14em;
    text-transform: uppercase;
    color: var(--cc-amber);
    opacity: 0.78;
  }

  .cc-ag-loop {
    width: 100%;
    overflow-x: auto;
  }
  .cc-ag-loop-svg {
    display: block;
    width: 100%;
    min-width: 880px;
    max-width: 1280px;
    margin: 0 auto;
  }
  .cc-ag-loop-pulse {
    opacity: 0;
    transition: opacity 0.4s ease;
  }
  .cc-ag-loop[data-active="true"] .cc-ag-loop-pulse {
    opacity: 1;
  }

  .cc-ag-loop-legend {
    margin-top: 28px;
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    justify-content: center;
    gap: 14px;
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.14em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
  }
  .cc-ag-loop-legend .legend-pair {
    display: inline-flex;
    align-items: center;
    gap: 8px;
  }
  .cc-ag-loop-legend .legend-dot {
    width: 6px;
    height: 6px;
    border-radius: 999px;
    background: var(--cc-amber);
    box-shadow: 0 0 0 3px rgba(247, 186, 100, 0.18);
  }
  .cc-ag-loop-legend .legend-stages {
    color: var(--cc-amber);
  }
  .cc-ag-loop-legend .legend-sep {
    color: var(--cc-ink-faint);
  }

  /* ===== 04 What the agent sees (tile grid) ===== */
  .cc-ag-sees-grid {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 18px;
    margin-top: 16px;
  }
  @media (max-width: 980px) {
    .cc-ag-sees-grid {
      grid-template-columns: repeat(2, 1fr);
    }
  }
  @media (max-width: 640px) {
    .cc-ag-sees-grid {
      grid-template-columns: 1fr;
    }
  }
  /* Borderless tiles. Card chrome is reserved for guardrails — see CC2.
     Top hairline + tonal-step background does the lift instead. */
  .cc-ag-sees-tile {
    padding: 24px 4px 4px;
    border-top: 1px solid var(--cc-ink-faint);
    display: flex;
    flex-direction: column;
    gap: 12px;
    min-height: 260px;
  }
  .cc-ag-sees-tile .eyebrow {
    margin-bottom: 0;
  }
  .cc-ag-sees-tile h3 {
    font-family: var(--cc-font-sans), sans-serif;
    font-size: 18px;
    font-weight: 500;
    letter-spacing: -0.015em;
    margin: 0;
    color: var(--cc-ink);
    line-height: 1.25;
  }
  .cc-ag-sees-tile p {
    font-size: 13.5px;
    line-height: 1.55;
    color: var(--cc-ink-dim);
    margin: 0;
    text-wrap: pretty;
  }
  .cc-ag-sees-viz {
    border: 1px solid var(--cc-ink-faint);
    border-radius: 10px;
    background: rgba(8, 14, 26, 0.6);
    padding: 12px;
    flex: 1;
    overflow: hidden;
    display: flex;
    align-items: center;
    justify-content: center;
  }
  .cc-ag-sees-viz svg {
    display: block;
    width: 100%;
    height: auto;
    max-height: 120px;
  }
  .cc-ag-sees-mini-trace {
    display: flex;
    flex-direction: column;
    gap: 5px;
    width: 100%;
  }
  .cc-ag-sees-mini-trace-row {
    display: grid;
    grid-template-columns: 76px minmax(0, 1fr) auto;
    gap: 8px;
    align-items: center;
    font-family: var(--cc-font-mono), monospace;
    font-size: 9px;
    letter-spacing: 0.06em;
    color: var(--cc-ink);
  }
  .cc-ag-sees-mini-trace-row .name {
    color: var(--cc-ink-dim);
  }
  .cc-ag-sees-mini-trace-row .bar-track {
    height: 7px;
    background: rgba(255, 255, 255, 0.04);
    border-radius: 2px;
    position: relative;
    overflow: hidden;
  }
  .cc-ag-sees-mini-trace-row .bar {
    position: absolute;
    height: 100%;
    border-radius: 2px;
    top: 0;
  }
  .cc-ag-sees-mini-trace-row .ms {
    color: var(--cc-ink-dim);
    text-align: right;
    font-size: 9px;
  }
  .cc-ag-sees-logs {
    width: 100%;
    font-family: var(--cc-font-mono), monospace;
    font-size: 10.5px;
    line-height: 1.7;
    color: var(--cc-ink);
  }
  .cc-ag-sees-logs .field {
    color: var(--cc-amber);
  }
  .cc-ag-sees-logs .lvl {
    color: var(--cc-ink-dim);
  }
  .cc-ag-sees-logs .err {
    color: var(--cc-col-cat);
  }
  .cc-ag-sees-code {
    width: 100%;
    font-family: var(--cc-font-mono), monospace;
    font-size: 10.5px;
    line-height: 1.65;
    color: var(--cc-ink);
  }
  .cc-ag-sees-code .kw {
    color: var(--cc-col-shi);
  }
  .cc-ag-sees-code .ty {
    color: var(--cc-col-usr);
  }
  .cc-ag-sees-code .com {
    color: var(--cc-ink-dim);
  }
  .cc-ag-sees-code .gutter {
    display: inline-block;
    width: 22px;
    color: var(--cc-ink-faint);
    user-select: none;
  }

  /* ===== 05 Demos ===== */
  .cc-ag-demos {
    display: flex;
    flex-direction: column;
    gap: 64px;
    margin-top: 16px;
  }
  .cc-ag-demo {
    /* No outer card chrome on the inverted band; structure is the visual
       anchor, not a frame. The two demos are SHAPE-distinct: investigate
       reads top-down (waterfall), operate reads horizontally (fan-out). */
    background: transparent;
    padding: 0;
  }
  .cc-ag-demo-head {
    display: flex;
    align-items: baseline;
    flex-wrap: wrap;
    gap: 14px;
    margin-bottom: 22px;
  }
  .cc-ag-demo-stages {
    font-family: var(--cc-font-mono), monospace;
    font-size: 10px;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    padding: 4px 9px;
    border-radius: 6px;
    border: 1px solid var(--cc-amber-line);
    background: var(--cc-amber-soft);
    color: var(--cc-amber);
    display: inline-flex;
    align-items: center;
    gap: 8px;
  }
  .cc-ag-demo-stages .stage-label {
    color: var(--cc-ink-dim);
    letter-spacing: 0.22em;
    font-size: 9px;
  }
  .cc-ag-demo-head .badge {
    font-family: var(--cc-font-mono), monospace;
    font-size: 10px;
    letter-spacing: 0.16em;
    text-transform: uppercase;
    padding: 4px 9px;
    border-radius: 6px;
    border: 1px solid var(--cc-amber-line);
    background: var(--cc-amber-soft);
    color: var(--cc-amber);
  }
  .cc-ag-demo-head h3 {
    font-family: var(--cc-font-sans), sans-serif;
    font-size: clamp(20px, 2.4vw, 28px);
    font-weight: 500;
    letter-spacing: -0.02em;
    color: var(--cc-ink);
    margin: 0;
  }
  .cc-ag-demo-head p {
    font-size: 14px;
    color: var(--cc-ink-dim);
    margin: 0;
    flex-basis: 100%;
    line-height: 1.55;
    text-wrap: pretty;
  }
  /* ===== Demo A — Investigative waterfall (vertical, top-down) =====
     Chat narrow on the left, descending cause-chain ledger on the right,
     full-width trace waterfall below. Reads as one question descending into
     causes. */
  .cc-ag-demo-investigate-grid {
    display: grid;
    grid-template-columns: minmax(0, 0.95fr) minmax(0, 0.65fr);
    gap: 18px;
    align-items: stretch;
    margin-bottom: 18px;
  }
  @media (max-width: 980px) {
    .cc-ag-demo-investigate-grid {
      grid-template-columns: 1fr;
    }
  }
  .cc-ag-demo-investigate-side {
    border: 1px solid var(--cc-ink-faint);
    border-radius: 14px;
    background: rgba(8, 14, 26, 0.7);
    padding: 18px;
    display: flex;
    flex-direction: column;
    gap: 14px;
  }
  .cc-ag-demo-side-tag {
    font-family: var(--cc-font-mono), monospace;
    font-size: 10px;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    color: var(--cc-amber);
    padding-bottom: 12px;
    border-bottom: 1px solid var(--cc-ink-faint);
  }
  .cc-ag-demo-cause-chain {
    list-style: none;
    margin: 0;
    padding: 0;
    display: flex;
    flex-direction: column;
    gap: 12px;
  }
  .cc-ag-demo-cause-chain li {
    position: relative;
    display: grid;
    grid-template-columns: auto 1fr;
    grid-template-rows: auto auto;
    column-gap: 14px;
    padding-left: 4px;
  }
  .cc-ag-demo-cause-chain li::before {
    content: "";
    position: absolute;
    left: 21px;
    top: 28px;
    bottom: -10px;
    width: 1px;
    background: var(--cc-amber-line);
    opacity: 0.5;
  }
  .cc-ag-demo-cause-chain li:last-child::before {
    display: none;
  }
  .cc-ag-demo-cause-chain .cause-step {
    grid-row: 1 / span 2;
    width: 28px;
    height: 28px;
    border-radius: 999px;
    border: 1px solid var(--cc-amber-line);
    background: var(--cc-amber-soft);
    color: var(--cc-amber);
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.06em;
    display: flex;
    align-items: center;
    justify-content: center;
  }
  .cc-ag-demo-cause-chain .cause-name {
    font-family: var(--cc-font-mono), monospace;
    font-size: 13px;
    color: var(--cc-ink);
    line-height: 1.2;
  }
  .cc-ag-demo-cause-chain .cause-meta {
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.04em;
    color: var(--cc-ink-dim);
    line-height: 1.4;
  }

  .cc-ag-demo-trace-full {
    border: 1px solid var(--cc-ink-faint);
    border-radius: 14px;
    background: rgba(8, 14, 26, 0.7);
    padding: 18px;
    margin-bottom: 18px;
    display: flex;
    flex-direction: column;
    gap: 12px;
  }

  /* ===== Demo B — Horizontal fan-out (lateral, parallel) =====
     Chat in the middle column, four surface tiles arrayed laterally on
     either side, each lighting up amber as the agent registers it. Reads
     as one command fanning out across surfaces. */
  .cc-ag-demo-fanout {
    display: grid;
    grid-template-columns: minmax(0, 0.9fr) minmax(0, 1.1fr) minmax(0, 0.9fr);
    gap: 18px;
    align-items: stretch;
    margin-bottom: 18px;
  }
  @media (max-width: 980px) {
    .cc-ag-demo-fanout {
      grid-template-columns: 1fr;
    }
  }
  .cc-ag-fanout-col {
    display: flex;
    flex-direction: column;
    gap: 14px;
    justify-content: center;
  }
  .cc-ag-fanout-tile {
    position: relative;
    border: 1px solid var(--cc-amber-line);
    border-radius: 12px;
    background: var(--cc-amber-soft);
    padding: 14px 16px;
    display: grid;
    grid-template-columns: 22px 1fr;
    grid-template-rows: auto auto auto;
    column-gap: 10px;
    row-gap: 4px;
    align-items: start;
  }
  .cc-ag-fanout-tile .check {
    grid-row: 1 / span 3;
    align-self: center;
    width: 22px;
    height: 22px;
    border-radius: 999px;
    border: 1px solid var(--cc-amber-line);
    background: rgba(247, 186, 100, 0.18);
    color: var(--cc-amber);
    display: inline-flex;
    align-items: center;
    justify-content: center;
  }
  .cc-ag-fanout-tile .kind {
    font-family: var(--cc-font-mono), monospace;
    font-size: 9.5px;
    letter-spacing: 0.16em;
    text-transform: uppercase;
    color: var(--cc-amber);
  }
  .cc-ag-fanout-tile .name {
    font-family: var(--cc-font-mono), monospace;
    font-size: 12.5px;
    color: var(--cc-ink);
    line-height: 1.2;
  }
  .cc-ag-fanout-tile .payload {
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    color: var(--cc-ink-dim);
    line-height: 1.3;
  }
  .cc-ag-fanout-spoke {
    position: absolute;
    top: 50%;
    width: 60px;
    height: 12px;
    transform: translateY(-50%);
    pointer-events: none;
  }
  .cc-ag-fanout-spoke.is-right {
    right: -56px;
  }
  .cc-ag-fanout-spoke.is-left {
    left: -56px;
  }
  @media (max-width: 980px) {
    .cc-ag-fanout-spoke {
      display: none;
    }
  }
  .cc-ag-fanout-center {
    display: flex;
    flex-direction: column;
  }

  .cc-ag-demo-chat {
    border: 1px solid var(--cc-ink-faint);
    border-radius: 14px;
    background: rgba(8, 14, 26, 0.7);
    padding: 18px;
    display: flex;
    flex-direction: column;
    gap: 14px;
    flex: 1;
  }
  .cc-ag-demo-chat-head {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding-bottom: 12px;
    border-bottom: 1px solid var(--cc-ink-faint);
    font-family: var(--cc-font-mono), monospace;
    font-size: 10px;
    letter-spacing: 0.16em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
    gap: 8px;
  }
  .cc-ag-demo-chat-pill {
    font-family: var(--cc-font-mono), monospace;
    font-size: 10px;
    letter-spacing: 0.16em;
    text-transform: uppercase;
    padding: 3px 7px;
    border-radius: 6px;
    border: 1px solid var(--cc-amber-line);
    background: var(--cc-amber-soft);
    color: var(--cc-amber);
  }
  .cc-ag-demo-msg {
    display: flex;
    flex-direction: column;
    gap: 6px;
  }
  .cc-ag-demo-msg .role {
    font-family: var(--cc-font-mono), monospace;
    font-size: 10px;
    letter-spacing: 0.16em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
    display: inline-flex;
    align-items: center;
    gap: 8px;
  }
  .cc-ag-demo-msg.is-agent .role {
    color: var(--cc-amber);
  }
  .cc-ag-demo-msg .role .agent-pill {
    width: 6px;
    height: 6px;
    border-radius: 999px;
    background: var(--cc-amber);
    box-shadow: 0 0 0 3px rgba(247, 186, 100, 0.18);
  }
  .cc-ag-demo-msg.is-user .body {
    font-family: var(--cc-font-mono), monospace;
    font-size: 13px;
    color: var(--cc-ink-dim);
    line-height: 1.55;
  }
  .cc-ag-demo-msg.is-agent .body {
    font-size: 14px;
    color: var(--cc-ink);
    line-height: 1.55;
  }
  .cc-ag-demo-mcp {
    display: grid;
    grid-template-columns: auto minmax(0, 1fr);
    gap: 10px;
    align-items: center;
    padding: 10px 12px;
    border-radius: 8px;
    border: 1px solid rgba(120, 140, 220, 0.32);
    background: rgba(120, 140, 220, 0.06);
    margin-top: 4px;
  }
  .cc-ag-demo-mcp .pill {
    font-family: var(--cc-font-mono), monospace;
    font-size: 9px;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    color: var(--cc-col-shi);
    padding: 2px 6px;
    border-radius: 4px;
    border: 1px solid rgba(120, 140, 220, 0.4);
    background: rgba(120, 140, 220, 0.1);
  }
  .cc-ag-demo-mcp code {
    font-family: var(--cc-font-mono), monospace;
    font-size: 11.5px;
    color: var(--cc-ink);
    overflow-wrap: anywhere;
    line-height: 1.45;
  }
  .cc-ag-demo-out {
    border: 1px solid var(--cc-ink-faint);
    border-radius: 14px;
    background: rgba(8, 14, 26, 0.7);
    padding: 18px;
    display: flex;
    flex-direction: column;
    gap: 14px;
  }
  .cc-ag-demo-out-head {
    padding-bottom: 12px;
    border-bottom: 1px solid var(--cc-ink-faint);
    font-family: var(--cc-font-mono), monospace;
    font-size: 10px;
    letter-spacing: 0.16em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
    display: flex;
    align-items: center;
    justify-content: space-between;
  }
  .cc-ag-demo-out-snippet {
    border: 1px solid var(--cc-ink-faint);
    border-radius: 10px;
    background: rgba(8, 14, 26, 0.85);
    padding: 14px;
    font-family: var(--cc-font-mono), monospace;
    font-size: 11.5px;
    line-height: 1.6;
    color: var(--cc-ink);
  }
  .cc-ag-demo-out-snippet .com {
    color: var(--cc-ink-dim);
  }
  .cc-ag-demo-out-snippet .add {
    color: var(--cc-col-ord);
    background: rgba(110, 200, 140, 0.08);
    padding: 1px 4px;
    border-radius: 3px;
  }
  .cc-ag-demo-out-snippet .kw {
    color: var(--cc-col-shi);
  }
  .cc-ag-demo-out-snippet .ty {
    color: var(--cc-col-usr);
  }

  /* Ledger panel for Demo B */
  .cc-ag-ledger {
    display: flex;
    flex-direction: column;
    gap: 10px;
  }
  .cc-ag-ledger-row {
    display: grid;
    grid-template-columns: 28px minmax(0, 1fr) auto;
    gap: 12px;
    align-items: center;
    padding: 12px 14px;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 10px;
    background: rgba(255, 255, 255, 0.025);
  }
  .cc-ag-ledger-row.is-on {
    border-color: var(--cc-amber-line);
    background: var(--cc-amber-soft);
  }
  .cc-ag-ledger-row .check {
    width: 22px;
    height: 22px;
    border-radius: 999px;
    border: 1px solid var(--cc-ink-faint);
    display: inline-flex;
    align-items: center;
    justify-content: center;
    color: var(--cc-ink-dim);
    background: rgba(255, 255, 255, 0.02);
    font-size: 12px;
  }
  .cc-ag-ledger-row.is-on .check {
    border-color: var(--cc-amber-line);
    background: var(--cc-amber-soft);
    color: var(--cc-amber);
  }
  .cc-ag-ledger-row .name {
    font-family: var(--cc-font-mono), monospace;
    font-size: 12.5px;
    color: var(--cc-ink);
  }
  .cc-ag-ledger-row .kind {
    font-family: var(--cc-font-mono), monospace;
    font-size: 10px;
    letter-spacing: 0.14em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
  }
  .cc-ag-ledger-row.is-on .kind {
    color: var(--cc-amber);
  }

  /* ===== 06 Product surfaces (stacked rows) =====
     Vertical list of borderless rows on a tinted band. Each row is a
     product surface name + tag + capability line + small inline
     mini-illustration. Hairline rules between rows handle separation. */
  .cc-ag-product-rows {
    list-style: none;
    margin: 0;
    padding: 0;
    border-top: 1px solid var(--cc-ink-faint);
  }
  .cc-ag-product-row {
    display: grid;
    grid-template-columns: 56px 130px minmax(0, 1.05fr) minmax(0, 1.6fr);
    column-gap: 28px;
    align-items: center;
    padding: 22px 4px;
    border-bottom: 1px solid var(--cc-ink-faint);
    transition: background 0.18s ease;
  }
  .cc-ag-product-row:hover {
    background: rgba(0, 0, 0, 0.02);
  }
  @media (max-width: 880px) {
    .cc-ag-product-row {
      grid-template-columns: 56px minmax(0, 1fr);
      grid-template-rows: auto auto auto auto;
      row-gap: 6px;
    }
    .cc-ag-product-row > .cc-ag-product-row-tag,
    .cc-ag-product-row > .cc-ag-product-row-name,
    .cc-ag-product-row > .cc-ag-product-row-body {
      grid-column: 2;
    }
  }
  .cc-ag-product-row-icon {
    width: 44px;
    height: 44px;
    display: flex;
    align-items: center;
    justify-content: center;
    color: var(--cc-ink);
  }
  .cc-ag-product-row-tag {
    font-family: var(--cc-font-mono), monospace;
    font-size: 10px;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
  }
  .cc-ag-product-row-name {
    font-family: var(--cc-font-sans), sans-serif;
    font-size: 20px;
    font-weight: 500;
    letter-spacing: -0.015em;
    color: var(--cc-ink);
    line-height: 1.2;
  }
  .cc-ag-product-row-body {
    font-size: 14.5px;
    line-height: 1.5;
    color: var(--cc-ink-dim);
    text-wrap: pretty;
  }

  /* ===== 07 Guardrails ===== */
  .cc-ag-guardrails {
    display: grid;
    grid-template-columns: repeat(2, 1fr);
    gap: 16px;
    margin-top: 16px;
  }
  @media (max-width: 880px) {
    .cc-ag-guardrails {
      grid-template-columns: 1fr;
    }
  }
  .cc-ag-guardrail {
    border: 1px solid var(--cc-ink-faint);
    border-radius: 14px;
    background: rgba(255, 255, 255, 0.025);
    padding: 22px 24px;
    display: grid;
    grid-template-columns: 44px minmax(0, 1fr);
    gap: 16px;
    align-items: flex-start;
  }
  .cc-ag-guardrail-icon {
    width: 44px;
    height: 44px;
    border: 1px solid var(--cc-amber-line);
    border-radius: 12px;
    background: var(--cc-amber-soft);
    color: var(--cc-amber);
    display: flex;
    align-items: center;
    justify-content: center;
  }
  .cc-ag-guardrail h4 {
    font-family: var(--cc-font-sans), sans-serif;
    font-size: 17px;
    font-weight: 500;
    letter-spacing: -0.015em;
    color: var(--cc-ink);
    margin: 0 0 6px;
    line-height: 1.25;
  }
  .cc-ag-guardrail p {
    font-size: 14px;
    line-height: 1.55;
    color: var(--cc-ink-dim);
    margin: 0;
    text-wrap: pretty;
  }

  /* ===== 08 Distribution (chip strip) =====
     Single horizontal row of logo + name + amber Add-MCP link. No card
     chrome — the chips read as a setup index, not a product wall. */
  .cc-ag-client-strip {
    list-style: none;
    margin: 16px 0 0;
    padding: 0;
    display: flex;
    flex-wrap: wrap;
    gap: 16px 28px;
    align-items: center;
  }
  .cc-ag-client-chip {
    display: inline-flex;
  }
  .cc-ag-client-chip a {
    display: inline-flex;
    align-items: center;
    gap: 12px;
    padding: 10px 16px;
    border-radius: 999px;
    border: 1px solid var(--cc-ink-faint);
    background: transparent;
    text-decoration: none;
    color: inherit;
    transition: border-color 0.18s ease, background 0.18s ease,
      transform 0.18s ease;
  }
  .cc-ag-client-chip a:hover {
    border-color: var(--cc-amber-line);
    background: var(--cc-amber-soft);
    transform: translateY(-1px);
  }
  .cc-ag-client-mono {
    width: 36px;
    height: 36px;
    display: flex;
    align-items: center;
    justify-content: center;
    color: var(--cc-ink);
    flex-shrink: 0;
  }
  .cc-ag-client-name {
    font-family: var(--cc-font-sans), sans-serif;
    font-size: 15px;
    font-weight: 500;
    letter-spacing: -0.01em;
    color: var(--cc-ink);
  }
  .cc-ag-client-cta {
    font-family: var(--cc-font-mono), monospace;
    font-size: 10.5px;
    letter-spacing: 0.16em;
    text-transform: uppercase;
    color: var(--cc-amber);
    padding-left: 6px;
  }

  /* ===== 09 Pricing teaser =====
     Lives directly inside an accent (amber) Band. No card chrome — the
     amber wash IS the visual frame. */
  .cc-ag-pricing-inner {
    max-width: 1280px;
    margin: 0 auto;
    display: grid;
    grid-template-columns: minmax(0, 1.4fr) auto;
    gap: 40px;
    align-items: center;
  }
  @media (max-width: 880px) {
    .cc-ag-pricing-inner {
      grid-template-columns: 1fr;
    }
  }
  .cc-ag-pricing-label {
    margin-bottom: 16px;
  }
  .cc-ag-pricing-inner h2 {
    font-size: clamp(26px, 3vw, 36px);
    margin: 6px 0 8px;
    line-height: 1.1;
  }
  .cc-ag-pricing-inner p {
    color: var(--cc-ink-dim);
    margin: 0;
    line-height: 1.55;
    max-width: 56ch;
    text-wrap: pretty;
    font-size: 15px;
  }
  /* Amber as system signal: the pricing CTA hover state. */
  .cc-ag-pricing-cta:hover {
    border-color: var(--cc-amber-line);
    background: var(--cc-amber-soft);
    color: var(--cc-amber);
  }

  /* ===== 10 Final CTA (glow band) ===== */
  .cc-ag-final-label {
    margin-bottom: 14px;
    justify-content: center;
  }
  .cc-ag-final-inner {
    max-width: 720px;
    margin: 0 auto;
    text-align: center;
    padding: 40px 0 80px;
  }
  .cc-ag-final-inner h2 {
    font-size: clamp(36px, 5vw, 64px);
    margin: 14px 0 32px;
  }
  .cc-ag-final-inner h2 .accent {
    background: linear-gradient(
      120deg,
      var(--cc-amber),
      var(--cc-col-bil) 60%,
      var(--cc-col-shi)
    );
    -webkit-background-clip: text;
    background-clip: text;
    -webkit-text-fill-color: transparent;
  }
`;
