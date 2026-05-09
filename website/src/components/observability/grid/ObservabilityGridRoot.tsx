"use client";

import styled from "styled-components";

// Grid variant root for /products/nitro/observability. Owns the dark surface
// tier, the cream ink tokens, and every section-internal class the existing
// observability sub-components (TraceWaterfall, ErrorFeedMock, ReplayPanel,
// SchemaDiffMock, AgentTranscriptMock, OtelLogoStrip, TrustStrip) consume.
//
// The Grid variant collapses the layered Band system of the default variant
// into strict 1px hairline-bordered squares. Sections share borders, cards
// have zero corner radius, no chrome gradients. The dark navy palette stays,
// the structure mirrors `vercel-observability.jpeg` archetype-for-archetype.
export const ObservabilityGridRoot = styled.div`
  --cc-ink: #f5f1ea;
  --cc-ink-dim: rgba(245, 241, 234, 0.62);
  --cc-ink-faint: rgba(245, 241, 234, 0.16);
  --cc-line-w: 1.5px;
  --cc-col-cat: oklch(0.74 0.18 30);
  --cc-col-bil: oklch(0.82 0.16 90);
  --cc-col-ord: oklch(0.76 0.16 150);
  --cc-col-shi: oklch(0.74 0.14 220);
  --cc-col-usr: oklch(0.72 0.18 310);
  --cc-grid-hairline: #1f2a3d;
  --cc-grid-hairline-strong: #2c3a52;

  position: relative;
  width: 100%;
  color: var(--cc-ink);
  font-family: var(--cc-font-sans), system-ui, sans-serif;
  background: #0c1322;

  * {
    box-sizing: border-box;
  }

  /* ===== Section eyebrow / display type (used inside Grid cards) ===== */
  .cc-grid-eyebrow {
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    color: var(--cc-accent, var(--cc-ink-dim));
    display: inline-flex;
    align-items: center;
    gap: 8px;
    margin-bottom: 18px;
  }
  .cc-grid-display {
    font-family: var(--cc-font-sans), sans-serif;
    font-weight: 500;
    letter-spacing: -0.025em;
    line-height: 1.05;
    margin: 0;
    color: var(--cc-ink);
  }
  .cc-grid-h1 {
    font-size: clamp(40px, 5.5vw, 72px);
    line-height: 1.02;
    margin: 0 0 24px;
    color: var(--cc-ink);
  }
  .cc-grid-h2 {
    font-size: clamp(28px, 3vw, 40px);
    line-height: 1.1;
    margin: 0 0 16px;
    color: var(--cc-ink);
  }
  .cc-grid-h3 {
    font-size: 20px;
    font-weight: 500;
    letter-spacing: -0.01em;
    line-height: 1.3;
    margin: 0 0 10px;
    color: var(--cc-ink);
  }
  .cc-grid-body {
    font-size: 15px;
    line-height: 1.55;
    color: var(--cc-ink-dim);
    margin: 0;
    text-wrap: pretty;
    max-width: 60ch;
  }
  .cc-grid-body-sm {
    font-size: 14px;
    line-height: 1.5;
    color: var(--cc-ink-dim);
    margin: 0;
    text-wrap: pretty;
  }

  /* ===== Plan chips (mirror ObservabilityRoot tokens so PlanChip renders) === */
  .cc-plan-chip {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.14em;
    text-transform: uppercase;
    padding: 5px 9px;
    border-radius: 0;
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
  .cc-plan-chip.is-oss .cc-plan-chip-dot {
    background: var(--cc-col-ord);
  }
  .cc-plan-chip.is-fusion .cc-plan-chip-dot {
    background: var(--cc-col-shi);
  }
  .cc-plan-chip.is-enterprise .cc-plan-chip-dot {
    background: var(--cc-col-usr);
  }
  .cc-plan-chip-row {
    display: inline-flex;
    flex-wrap: wrap;
    gap: 8px;
    margin: 0 0 18px;
  }

  /* ===== Reused mock chrome from the default variant ===================== */
  /* Trace waterfall */
  .cc-trace-waterfall {
    width: 100%;
    display: block;
  }

  /* Error feed three-pane mock */
  .cc-error-mock {
    display: grid;
    grid-template-columns: minmax(0, 0.85fr) minmax(0, 1.1fr);
    gap: 0;
    border-top: 1px solid var(--cc-ink-faint);
  }
  @media (max-width: 880px) {
    .cc-error-mock {
      grid-template-columns: 1fr;
    }
  }
  .cc-error-feed,
  .cc-error-detail {
    background: rgba(8, 14, 26, 0.45);
    overflow: hidden;
    display: flex;
    flex-direction: column;
  }
  .cc-error-detail {
    border-left: 1px solid var(--cc-ink-faint);
  }
  @media (max-width: 880px) {
    .cc-error-detail {
      border-left: 0;
      border-top: 1px solid var(--cc-ink-faint);
    }
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
    border-radius: 0;
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

  /* Replay panel - flatten into Grid: square corners, no inner radii */
  .cc-replay-mock {
    display: flex;
    flex-direction: column;
    gap: 0;
  }
  .cc-replay-header {
    border-bottom: 1px solid var(--cc-ink-faint);
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
    gap: 0;
  }
  @media (max-width: 880px) {
    .cc-replay-grid {
      grid-template-columns: 1fr;
    }
  }
  .cc-replay-pane {
    position: relative;
    border-radius: 0;
    overflow: hidden;
  }
  .cc-replay-pane + .cc-replay-pane {
    border-left: 1px solid var(--cc-ink-faint);
  }
  @media (max-width: 880px) {
    .cc-replay-pane + .cc-replay-pane {
      border-left: 0;
      border-top: 1px solid var(--cc-ink-faint);
    }
  }
  .cc-replay-pane.is-fail {
    background: rgba(220, 80, 80, 0.06);
  }
  .cc-replay-pane.is-ok {
    background: rgba(80, 200, 120, 0.06);
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
    border-radius: 0;
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

  /* Schema diff stack - flatten radii */
  .cc-schema-mock {
    display: grid;
    grid-template-columns: minmax(0, 1.2fr) minmax(0, 0.85fr);
    gap: 0;
  }
  @media (max-width: 980px) {
    .cc-schema-mock {
      grid-template-columns: 1fr;
    }
  }
  .cc-schema-stack {
    display: flex;
    flex-direction: column;
    gap: 0;
    border-right: 1px solid var(--cc-ink-faint);
  }
  @media (max-width: 980px) {
    .cc-schema-stack {
      border-right: 0;
      border-bottom: 1px solid var(--cc-ink-faint);
    }
  }
  .cc-schema-pr {
    background: rgba(8, 14, 26, 0.65);
    padding: 14px 16px;
    display: flex;
    flex-direction: column;
    gap: 10px;
    border-bottom: 1px solid var(--cc-ink-faint);
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

  /* OTEL strip - same layout, square corners */
  .cc-otel-strip {
    margin-top: 24px;
    display: grid;
    grid-template-columns: repeat(6, 1fr);
    gap: 0;
    border-top: 1px solid var(--cc-ink-faint);
    border-left: 1px solid var(--cc-ink-faint);
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
    padding: 22px 12px 18px;
    border-right: 1px solid var(--cc-ink-faint);
    border-bottom: 1px solid var(--cc-ink-faint);
    border-radius: 0;
    background: rgba(255, 255, 255, 0.02);
    transition: background 0.15s ease;
  }
  .cc-otel-tile:hover {
    background: rgba(255, 255, 255, 0.04);
  }
  .cc-otel-mono {
    width: 44px;
    height: 44px;
    border-radius: 0;
    border: 1px solid var(--cc-ink-faint);
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

  /* Agent transcript inside the inverted band */
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
    border-radius: 0;
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
    border-radius: 0;
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
    background: var(--cc-accent-line, rgba(245, 241, 234, 0.32));
  }
  @media (max-width: 880px) {
    .cc-agent-rule {
      width: 100%;
      height: 1px;
    }
  }
  .cc-agent-trace {
    display: flex;
    flex-direction: column;
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
`;
