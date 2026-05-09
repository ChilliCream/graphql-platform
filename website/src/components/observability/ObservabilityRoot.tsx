"use client";

import styled from "styled-components";

// ObservabilityRoot owns the dark navy / cream-ink design tokens and the
// section-local styles for /products/nitro/observability. Sections live inside
// <Band> primitives from the redesign system, so this stylesheet no longer
// owns the surface tier itself, only the typography, the trace/log/diff mocks,
// and the section-internal layout grammar.
export const ObservabilityRoot = styled.div`
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
  background: linear-gradient(
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

  /* ===== Section label (used by hero + each panel) ===== */
  .cc-section-label {
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.18em;
    color: var(--cc-ink-dim);
    text-transform: uppercase;
    display: inline-flex;
    align-items: center;
    gap: 10px;
    margin-bottom: 18px;
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

  /* ===== Plan chips ===== */
  .cc-plan-chip {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.14em;
    text-transform: uppercase;
    padding: 5px 9px;
    border-radius: 6px;
    border: 1px solid var(--cc-ink-faint);
    background: rgba(255, 255, 255, 0.025);
    color: var(--cc-ink);
    line-height: 1;
  }
  .cc-plan-chip-dot {
    width: 6px;
    height: 6px;
    border-radius: 999px;
    background: var(--cc-ink);
  }
  .cc-plan-chip.is-all {
    border-color: rgba(245, 241, 234, 0.32);
  }
  .cc-plan-chip.is-all .cc-plan-chip-dot {
    background: var(--cc-ink);
  }
  .cc-plan-chip.is-nitro {
    border-color: rgba(245, 241, 234, 0.32);
  }
  .cc-plan-chip.is-nitro .cc-plan-chip-dot {
    background: var(--cc-ink);
  }
  .cc-plan-chip.is-oss {
    border-color: rgba(118, 200, 150, 0.42);
    background: rgba(118, 200, 150, 0.08);
  }
  .cc-plan-chip.is-oss .cc-plan-chip-dot {
    background: var(--cc-col-ord);
  }
  .cc-plan-chip.is-fusion {
    border-color: rgba(140, 180, 230, 0.42);
    background: rgba(140, 180, 230, 0.08);
  }
  .cc-plan-chip.is-fusion .cc-plan-chip-dot {
    background: var(--cc-col-shi);
  }
  .cc-plan-chip.is-enterprise {
    border-color: rgba(200, 160, 230, 0.42);
    background: rgba(200, 160, 230, 0.08);
  }
  .cc-plan-chip.is-enterprise .cc-plan-chip-dot {
    background: var(--cc-col-usr);
  }
  .cc-plan-chip-row {
    display: inline-flex;
    flex-wrap: wrap;
    gap: 8px;
    margin-bottom: 16px;
  }

  /* ===== 01 Hero ===== */
  .cc-obs-hero-shell {
    position: relative;
    width: 100%;
    overflow: hidden;
  }
  .cc-obs-hero-ambient {
    position: absolute;
    inset: 0;
    pointer-events: none;
    color: var(--cc-accent, var(--cc-col-shi));
  }
  .cc-obs-hero-ambient svg {
    position: absolute;
    inset: 0;
    width: 100%;
    height: 100%;
    opacity: 0.85;
  }
  .cc-obs-hero-grid {
    position: relative;
    display: grid;
    grid-template-columns: minmax(0, 1fr) minmax(0, 1.15fr);
    gap: 40px;
    align-items: center;
    min-height: clamp(520px, 64vh, 720px);
  }
  @media (max-width: 980px) {
    .cc-obs-hero-grid {
      grid-template-columns: 1fr;
      gap: 32px;
      min-height: 0;
    }
  }
  .cc-obs-hero-copy h1 {
    font-size: clamp(40px, 6vw, 88px);
    margin: 18px 0 24px;
    line-height: 1.02;
  }
  .cc-obs-hero-copy h1 .accent {
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
  .cc-obs-hero-copy p {
    font-size: clamp(15px, 1.2vw, 19px);
    line-height: 1.55;
    color: var(--cc-ink-dim);
    max-width: 56ch;
    margin: 0 0 32px;
    text-wrap: pretty;
  }
  .cc-obs-hero-cta {
    display: flex;
    gap: 14px;
    flex-wrap: wrap;
  }
  .cc-obs-hero-dashboard {
    position: relative;
    width: calc(100% + clamp(24px, 6vw, 96px));
    margin-right: calc(-1 * clamp(24px, 6vw, 96px));
    color: var(--cc-accent, var(--cc-col-shi));
  }
  .cc-obs-hero-dashboard svg {
    width: 100%;
    height: auto;
    display: block;
    filter: drop-shadow(0 30px 80px rgba(0, 0, 0, 0.45));
  }
  @media (max-width: 980px) {
    .cc-obs-hero-dashboard {
      width: 100%;
      margin-right: 0;
    }
  }

  /* ===== Feature panel (sections 02, 03, 04, 05, 07) ===== */
  .cc-obs-panel {
    width: 100%;
  }
  .cc-obs-panel-header {
    max-width: 720px;
    margin: 0 0 36px;
  }
  .cc-obs-panel-header h2 {
    font-size: clamp(30px, 3.6vw, 46px);
    margin: 6px 0 12px;
    line-height: 1.05;
  }
  .cc-obs-panel-header p {
    font-size: clamp(15px, 1.1vw, 18px);
    color: var(--cc-ink-dim);
    margin: 0;
    line-height: 1.55;
    max-width: 60ch;
    text-wrap: pretty;
  }
  .cc-obs-panel-bullets {
    list-style: none;
    margin: 18px 0 0;
    padding: 0;
    display: flex;
    flex-direction: column;
    gap: 10px;
  }
  .cc-obs-panel-bullets li {
    position: relative;
    padding-left: 18px;
    font-size: 14px;
    line-height: 1.45;
    color: var(--cc-ink-dim);
  }
  .cc-obs-panel-bullets li::before {
    content: "";
    position: absolute;
    left: 0;
    top: 9px;
    width: 8px;
    height: 1px;
    background: var(--cc-accent, var(--cc-ink));
  }
  .cc-obs-panel-viz {
    width: 100%;
  }
  .cc-obs-panel-viz.is-bleed-right {
    width: calc(100% + 16px);
    margin-right: -16px;
  }
  .cc-obs-panel.is-sidebar {
    display: grid;
    grid-template-columns: minmax(260px, 0.9fr) minmax(0, 1.6fr);
    gap: 48px;
    align-items: start;
  }
  .cc-obs-panel.is-sidebar .cc-obs-panel-header {
    margin: 0;
  }
  .cc-obs-panel.is-diptych {
    display: grid;
    grid-template-columns: minmax(0, 1.4fr) minmax(260px, 0.9fr);
    gap: 48px;
    align-items: start;
  }
  .cc-obs-panel.is-diptych .cc-obs-panel-header {
    order: 2;
    margin: 0;
  }
  .cc-obs-panel.is-diptych .cc-obs-panel-viz {
    order: 1;
  }
  @media (max-width: 980px) {
    .cc-obs-panel.is-sidebar,
    .cc-obs-panel.is-diptych {
      grid-template-columns: 1fr;
      gap: 32px;
    }
    .cc-obs-panel.is-diptych .cc-obs-panel-header {
      order: 0;
    }
    .cc-obs-panel.is-diptych .cc-obs-panel-viz {
      order: 1;
    }
  }

  /* ===== Trace waterfall (SVG) ===== */
  .cc-trace-waterfall {
    width: 100%;
    display: block;
  }
  .cc-trace-row-name {
    font-family: var(--cc-font-mono), monospace;
    font-size: 12px;
    letter-spacing: 0.04em;
  }
  .cc-trace-row-meta {
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.1em;
    text-transform: uppercase;
  }

  /* ===== Error feed (Section 03) ===== */
  .cc-error-mock {
    display: grid;
    grid-template-columns: minmax(0, 0.85fr) minmax(0, 1.1fr);
    gap: 16px;
  }
  @media (max-width: 880px) {
    .cc-error-mock {
      grid-template-columns: 1fr;
    }
  }
  .cc-error-feed {
    border-top: 1px solid var(--cc-ink-faint);
    border-bottom: 1px solid var(--cc-ink-faint);
    background: rgba(8, 14, 26, 0.45);
    overflow: hidden;
  }
  .cc-error-feed-header,
  .cc-error-detail-header {
    padding: 12px 16px;
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
  .cc-error-feed ul {
    list-style: none;
    margin: 0;
    padding: 0;
  }
  .cc-error-feed li {
    display: grid;
    grid-template-columns: 12px minmax(0, 1fr) auto;
    gap: 10px;
    padding: 12px 16px;
    align-items: center;
    border-bottom: 1px solid rgba(245, 241, 234, 0.06);
    font-size: 13px;
    line-height: 1.4;
  }
  .cc-error-feed li:last-child {
    border-bottom: none;
  }
  .cc-error-feed li.is-active {
    background: rgba(120, 140, 220, 0.08);
  }
  .cc-error-dot {
    width: 8px;
    height: 8px;
    border-radius: 999px;
    background: var(--cc-col-cat);
  }
  .cc-error-msg {
    color: var(--cc-ink);
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }
  .cc-error-time {
    font-family: var(--cc-font-mono), monospace;
    font-size: 10px;
    letter-spacing: 0.08em;
    color: var(--cc-ink-dim);
    text-transform: uppercase;
  }
  .cc-error-detail {
    border-top: 1px solid var(--cc-ink-faint);
    border-bottom: 1px solid var(--cc-ink-faint);
    background: rgba(8, 14, 26, 0.45);
    overflow: hidden;
    display: flex;
    flex-direction: column;
  }
  .cc-error-breadcrumb {
    padding: 14px 16px;
    border-bottom: 1px solid var(--cc-ink-faint);
    font-family: var(--cc-font-mono), monospace;
    font-size: 12px;
    color: var(--cc-ink);
    line-height: 1.5;
    overflow-x: auto;
    white-space: nowrap;
  }
  .cc-error-breadcrumb .seg {
    color: var(--cc-ink);
  }
  .cc-error-breadcrumb .sep {
    color: var(--cc-ink-faint);
    margin: 0 4px;
  }
  .cc-error-breadcrumb .hot {
    color: var(--cc-col-cat);
    background: rgba(220, 110, 80, 0.12);
    padding: 2px 5px;
    border-radius: 4px;
  }
  .cc-error-source {
    flex: 1;
    padding: 14px 16px 16px;
    font-family: var(--cc-font-mono), monospace;
    font-size: 12px;
    line-height: 1.65;
    color: var(--cc-ink);
    overflow-x: auto;
  }
  .cc-error-source .gutter {
    display: inline-block;
    width: 28px;
    color: var(--cc-ink-faint);
    user-select: none;
  }
  .cc-error-source .kw {
    color: var(--cc-col-shi);
  }
  .cc-error-source .ty {
    color: var(--cc-col-usr);
  }
  .cc-error-source .str {
    color: var(--cc-col-bil);
  }
  .cc-error-source .com {
    color: var(--cc-ink-dim);
  }
  .cc-error-source .err {
    background: rgba(220, 110, 80, 0.14);
    border-left: 2px solid var(--cc-col-cat);
    margin-left: -16px;
    padding-left: 14px;
    display: block;
  }

  /* ===== Replay panel (Section 04) ===== */
  .cc-replay-mock {
    display: flex;
    flex-direction: column;
    gap: 14px;
  }
  .cc-replay-header {
    border: 1px solid var(--cc-ink-faint);
    border-radius: 12px;
    background: rgba(8, 14, 26, 0.65);
    padding: 14px 18px;
    display: flex;
    flex-wrap: wrap;
    gap: 18px;
    align-items: center;
    font-family: var(--cc-font-mono), monospace;
    font-size: 12px;
    color: var(--cc-ink-dim);
  }
  .cc-replay-header .method {
    color: var(--cc-col-shi);
    font-weight: 500;
  }
  .cc-replay-header .path {
    color: var(--cc-ink);
  }
  .cc-replay-grid {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 14px;
  }
  @media (max-width: 880px) {
    .cc-replay-grid {
      grid-template-columns: 1fr;
    }
  }
  .cc-replay-pane {
    position: relative;
    border-radius: 12px;
    border: 1px solid var(--cc-ink-faint);
    overflow: hidden;
  }
  .cc-replay-pane.is-fail {
    background: rgba(220, 80, 80, 0.06);
    border-color: rgba(220, 110, 80, 0.4);
  }
  .cc-replay-pane.is-ok {
    background: rgba(80, 200, 120, 0.06);
    border-color: rgba(110, 200, 140, 0.4);
  }
  .cc-replay-pane-header {
    padding: 12px 16px;
    border-bottom: 1px solid var(--cc-ink-faint);
    display: flex;
    align-items: center;
    justify-content: space-between;
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.14em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
  }
  .cc-replay-pane-status {
    color: var(--cc-ink);
    padding: 3px 7px;
    border-radius: 6px;
    border: 1px solid var(--cc-ink-faint);
    background: rgba(255, 255, 255, 0.025);
    font-size: 10px;
    letter-spacing: 0.14em;
  }
  .cc-replay-pane.is-fail .cc-replay-pane-status {
    color: var(--cc-col-cat);
    border-color: rgba(220, 110, 80, 0.4);
    background: rgba(220, 110, 80, 0.1);
  }
  .cc-replay-pane.is-ok .cc-replay-pane-status {
    color: var(--cc-col-ord);
    border-color: rgba(110, 200, 140, 0.4);
    background: rgba(110, 200, 140, 0.1);
  }
  .cc-replay-pane-body {
    padding: 16px;
  }

  /* ===== Schema diff (Section 05) ===== */
  .cc-schema-mock {
    display: grid;
    grid-template-columns: minmax(0, 1.2fr) minmax(0, 0.85fr);
    gap: 16px;
  }
  @media (max-width: 980px) {
    .cc-schema-mock {
      grid-template-columns: 1fr;
    }
  }
  .cc-schema-stack {
    display: flex;
    flex-direction: column;
    gap: 14px;
  }
  .cc-schema-pr {
    border: 1px solid var(--cc-ink-faint);
    border-radius: 12px;
    background: rgba(8, 14, 26, 0.65);
    padding: 14px 16px;
    display: flex;
    flex-direction: column;
    gap: 10px;
  }
  .cc-schema-pr-row {
    display: flex;
    align-items: center;
    gap: 10px;
    font-size: 13px;
    color: var(--cc-ink);
  }
  .cc-schema-pr-row .check {
    width: 18px;
    height: 18px;
    border-radius: 999px;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    flex-shrink: 0;
  }
  .cc-schema-pr-row .check.is-ok {
    color: var(--cc-col-ord);
    border: 1px solid rgba(110, 200, 140, 0.4);
    background: rgba(110, 200, 140, 0.12);
  }
  .cc-schema-pr-row .check.is-fail {
    color: var(--cc-col-cat);
    border: 1px solid rgba(220, 110, 80, 0.4);
    background: rgba(220, 110, 80, 0.12);
  }
  .cc-schema-pr-row .name {
    flex: 1;
    font-family: var(--cc-font-mono), monospace;
    font-size: 12px;
    color: var(--cc-ink);
  }
  .cc-schema-pr-row .kind {
    font-family: var(--cc-font-mono), monospace;
    font-size: 10px;
    letter-spacing: 0.14em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
  }
  .cc-schema-diff {
    border: 1px solid var(--cc-ink-faint);
    border-radius: 12px;
    background: rgba(8, 14, 26, 0.85);
    overflow: hidden;
  }
  .cc-schema-diff-header {
    padding: 10px 16px;
    border-bottom: 1px solid var(--cc-ink-faint);
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.14em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
    display: flex;
    justify-content: space-between;
    gap: 12px;
  }
  .cc-schema-diff pre {
    margin: 0;
    padding: 12px 0;
    font-family: var(--cc-font-mono), monospace;
    font-size: 12.5px;
    line-height: 1.6;
    color: var(--cc-ink);
    overflow-x: auto;
  }
  .cc-schema-diff .ln {
    display: grid;
    grid-template-columns: 36px 16px minmax(0, 1fr);
    gap: 0;
    padding: 0 16px;
  }
  .cc-schema-diff .ln .gutter {
    color: var(--cc-ink-faint);
    text-align: right;
    padding-right: 12px;
    user-select: none;
  }
  .cc-schema-diff .ln .sign {
    text-align: center;
    color: var(--cc-ink-faint);
  }
  .cc-schema-diff .ln.is-add {
    background: rgba(110, 200, 140, 0.08);
  }
  .cc-schema-diff .ln.is-add .sign {
    color: var(--cc-col-ord);
  }
  .cc-schema-diff .ln.is-del {
    background: rgba(220, 110, 80, 0.08);
  }
  .cc-schema-diff .ln.is-del .sign {
    color: var(--cc-col-cat);
  }
  .cc-schema-diff .kw {
    color: var(--cc-col-shi);
  }
  .cc-schema-diff .ty {
    color: var(--cc-col-usr);
  }
  .cc-schema-audit {
    border: 1px solid var(--cc-ink-faint);
    border-radius: 12px;
    background: rgba(8, 14, 26, 0.65);
    overflow: hidden;
    display: flex;
    flex-direction: column;
  }
  .cc-schema-audit-header {
    padding: 10px 16px;
    border-bottom: 1px solid var(--cc-ink-faint);
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.14em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
  }
  .cc-schema-audit ul {
    list-style: none;
    margin: 0;
    padding: 0;
    flex: 1;
  }
  .cc-schema-audit li {
    display: grid;
    grid-template-columns: minmax(0, 1fr) auto;
    gap: 10px;
    padding: 12px 16px;
    border-bottom: 1px solid rgba(245, 241, 234, 0.06);
    font-size: 13px;
    line-height: 1.4;
    color: var(--cc-ink);
  }
  .cc-schema-audit li:last-child {
    border-bottom: none;
  }
  .cc-schema-audit .who {
    font-family: var(--cc-font-mono), monospace;
    font-size: 10px;
    letter-spacing: 0.06em;
    color: var(--cc-ink-dim);
    margin-top: 3px;
  }
  .cc-schema-audit .when {
    font-family: var(--cc-font-mono), monospace;
    font-size: 10px;
    letter-spacing: 0.08em;
    color: var(--cc-ink-dim);
    text-transform: uppercase;
    text-align: right;
    align-self: start;
  }

  /* Tinted-band ink scope.
     The Band variant="tinted" primitive paints a cream surface, but
     ObservabilityRoot ships --cc-ink as cream for the dark canvas. Apply
     this class on the band's content wrapper so labels, headings, hairlines
     and dim copy all read against the cream. The global h1-h6 rule binds to
     --cc-heading-text-color (see global-style.tsx), so the scope overrides
     that token too. */
  .cc-obs-tint-scope {
    --cc-ink: #1a1f2e;
    --cc-ink-dim: rgba(26, 31, 46, 0.66);
    --cc-ink-faint: rgba(26, 31, 46, 0.16);
    --cc-heading-text-color: #1a1f2e;
    color: var(--cc-ink);
  }
  .cc-obs-tint-scope .cc-section-label .num {
    border-color: var(--cc-ink-faint);
    color: var(--cc-ink);
  }

  /* ===== Trust strip (Section 06) - banded, no card chrome ===== */
  .cc-obs-trust-row {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 0;
  }
  @media (max-width: 880px) {
    .cc-obs-trust-row {
      grid-template-columns: 1fr;
    }
  }
  .cc-obs-trust-cell {
    padding: 8px clamp(20px, 2.5vw, 36px);
  }
  .cc-obs-trust-cell + .cc-obs-trust-cell {
    border-left: 1px solid var(--cc-ink-faint);
  }
  @media (max-width: 880px) {
    .cc-obs-trust-cell + .cc-obs-trust-cell {
      border-left: none;
      border-top: 1px solid var(--cc-ink-faint);
      padding-top: 28px;
      margin-top: 28px;
    }
    .cc-obs-trust-cell:first-child {
      padding-left: 0;
    }
  }
  .cc-obs-trust-cell:first-child {
    padding-left: 0;
  }
  .cc-obs-trust-cell:last-child {
    padding-right: 0;
  }
  .cc-obs-trust-icon {
    width: 28px;
    height: 28px;
    display: flex;
    align-items: center;
    justify-content: center;
    color: var(--cc-accent, var(--cc-col-shi));
    margin-bottom: 16px;
  }
  .cc-obs-trust-title {
    font-size: 18px;
    font-weight: 500;
    letter-spacing: -0.015em;
    margin: 0 0 8px;
    color: var(--cc-ink);
  }
  .cc-obs-trust-body {
    font-size: 14px;
    line-height: 1.55;
    color: var(--cc-ink-dim);
    margin: 0;
    text-wrap: pretty;
  }

  /* ===== OTEL strip ===== */
  .cc-otel-strip {
    margin-top: 24px;
    display: grid;
    grid-template-columns: repeat(6, 1fr);
    gap: 14px;
  }
  @media (max-width: 880px) {
    .cc-otel-strip {
      grid-template-columns: repeat(3, 1fr);
    }
  }
  @media (max-width: 480px) {
    .cc-otel-strip {
      grid-template-columns: repeat(2, 1fr);
    }
  }
  .cc-otel-tile {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 10px;
    padding: 18px 12px 14px;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 12px;
    background: rgba(255, 255, 255, 0.02);
    transition: border-color 0.15s ease, background 0.15s ease;
  }
  .cc-otel-tile:hover {
    border-color: rgba(245, 241, 234, 0.3);
    background: rgba(255, 255, 255, 0.04);
  }
  .cc-otel-mono {
    width: 44px;
    height: 44px;
    border-radius: 10px;
    border: 1.5px solid var(--cc-ink-faint);
    display: flex;
    align-items: center;
    justify-content: center;
    color: var(--cc-ink);
    background: rgba(255, 255, 255, 0.025);
  }
  .cc-otel-name {
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.1em;
    color: var(--cc-ink-dim);
    text-transform: uppercase;
  }
  .cc-otel-timeline {
    width: 100%;
    margin-bottom: 18px;
  }

  /* ===== Agent transcript (Section 08) - banded, no card chrome ===== */
  .cc-agent-mock {
    display: grid;
    grid-template-columns: minmax(0, 1fr) 1px minmax(0, 1.2fr);
    gap: 32px;
    align-items: stretch;
  }
  @media (max-width: 880px) {
    .cc-agent-mock {
      grid-template-columns: 1fr;
      gap: 24px;
    }
  }
  .cc-agent-chat {
    display: flex;
    flex-direction: column;
    gap: 14px;
  }
  .cc-agent-chat-header {
    display: flex;
    align-items: center;
    gap: 8px;
    padding-bottom: 12px;
    border-bottom: 1px solid var(--cc-ink-faint);
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.14em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
  }
  .cc-agent-pill {
    font-family: var(--cc-font-mono), monospace;
    font-size: 10px;
    letter-spacing: 0.16em;
    text-transform: uppercase;
    padding: 3px 7px;
    border-radius: 6px;
    border: 1px solid var(--cc-accent-line, rgba(245, 241, 234, 0.32));
    background: var(--cc-accent-soft, rgba(245, 241, 234, 0.08));
    color: var(--cc-ink);
  }
  .cc-agent-msg {
    display: flex;
    flex-direction: column;
    gap: 6px;
  }
  .cc-agent-msg-role {
    font-family: var(--cc-font-mono), monospace;
    font-size: 10px;
    letter-spacing: 0.16em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
  }
  .cc-agent-msg.is-user .cc-agent-msg-body {
    font-family: var(--cc-font-mono), monospace;
    font-size: 13px;
    color: var(--cc-ink-dim);
    line-height: 1.55;
  }
  .cc-agent-msg.is-agent .cc-agent-msg-body {
    font-size: 14px;
    color: var(--cc-ink);
    line-height: 1.55;
  }
  .cc-agent-msg-body code {
    font-family: var(--cc-font-mono), monospace;
    color: var(--cc-accent, var(--cc-col-shi));
    background: var(--cc-accent-soft, rgba(120, 140, 220, 0.08));
    padding: 1px 5px;
    border-radius: 4px;
  }
  .cc-agent-step {
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    color: var(--cc-ink-dim);
    padding: 8px 10px;
    border-left: 2px solid var(--cc-accent-line, var(--cc-ink-faint));
    background: rgba(255, 255, 255, 0.02);
    margin-top: 4px;
    line-height: 1.55;
  }
  .cc-agent-rule {
    width: 1px;
    background: linear-gradient(
      180deg,
      transparent,
      var(--cc-accent-line, rgba(245, 241, 234, 0.32)) 20%,
      var(--cc-accent-line, rgba(245, 241, 234, 0.32)) 80%,
      transparent
    );
  }
  @media (max-width: 880px) {
    .cc-agent-rule {
      width: 100%;
      height: 1px;
      background: var(--cc-accent-line, rgba(245, 241, 234, 0.32));
    }
  }
  .cc-agent-trace {
    display: flex;
    flex-direction: column;
    width: calc(100% + 16px);
    margin-right: -16px;
  }
  @media (max-width: 880px) {
    .cc-agent-trace {
      width: 100%;
      margin-right: 0;
    }
  }
  .cc-agent-trace-header {
    padding-bottom: 12px;
    border-bottom: 1px solid var(--cc-ink-faint);
    margin-bottom: 14px;
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.14em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
  }

  /* ===== Final CTA ===== */
  .cc-obs-final-inner {
    max-width: 720px;
    margin: 0 auto;
    text-align: center;
  }
  .cc-obs-final-label {
    justify-content: center;
  }
  .cc-obs-final-inner h2 {
    font-size: clamp(36px, 5vw, 64px);
    margin: 14px 0 32px;
  }
  .cc-obs-final-inner h2 .accent {
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
  .cc-obs-final-link {
    display: inline-block;
    margin-top: 26px;
    font-family: var(--cc-font-mono), monospace;
    font-size: 12px;
    letter-spacing: 0.14em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
    text-decoration: none;
    border-bottom: 1px solid var(--cc-ink-faint);
    padding-bottom: 2px;
  }
  .cc-obs-final-link:hover {
    color: var(--cc-ink);
    border-bottom-color: var(--cc-ink);
  }
`;
