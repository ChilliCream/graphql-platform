"use client";

import styled from "styled-components";

// AgentsRoot owns the dark navy / cream-ink design tokens for the
// /products/nitro/agents page. Mirrors PricingRoot, EnterpriseRoot and
// ObservabilityRoot 1:1: same tokens, same section shell, same typography
// and button system, with one extra accent — `--cc-amber` — used to signal
// agent activity (instrumentation in motion, not the typical AI purple/teal).
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

  /* ===== 01 Hero ===== */
  .cc-ag-hero {
    padding-top: 160px;
    padding-bottom: 80px;
  }
  .cc-ag-hero-inner {
    max-width: 1280px;
    margin: 0 auto;
    display: grid;
    grid-template-columns: minmax(0, 1fr) minmax(0, 1.05fr);
    gap: 64px;
    align-items: center;
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

  /* ===== Generic section frame (used by sections 02, 03, 04, 05, 06, 07,
     08, 09) ===== */
  .cc-ag-feature {
    padding-top: 32px;
    padding-bottom: 96px;
  }
  .cc-ag-feature-inner {
    max-width: 1280px;
    margin: 0 auto;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 22px;
    background: rgba(255, 255, 255, 0.02);
    padding: 56px;
  }
  @media (max-width: 880px) {
    .cc-ag-feature-inner {
      padding: 32px 24px;
    }
  }
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
  .cc-ag-feature-elevated {
    border-color: rgba(247, 186, 100, 0.32);
    background: linear-gradient(
      180deg,
      rgba(247, 186, 100, 0.05),
      rgba(247, 186, 100, 0.015)
    );
    box-shadow: 0 30px 80px -40px rgba(0, 0, 0, 0.7),
      0 0 60px -20px rgba(247, 186, 100, 0.22);
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
    background: #0c1322;
    padding: 6px 8px;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 999px;
    line-height: 1;
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

  /* ===== 03 Loop diagram ===== */
  .cc-ag-loop {
    margin-top: 12px;
    width: 100%;
    overflow-x: auto;
  }
  .cc-ag-loop-svg {
    display: block;
    width: 100%;
    min-width: 760px;
    max-width: 1180px;
    margin: 0 auto;
  }
  .cc-ag-loop-stages {
    display: grid;
    grid-template-columns: repeat(5, 1fr);
    gap: 12px;
    margin-top: 24px;
  }
  @media (max-width: 880px) {
    .cc-ag-loop-stages {
      grid-template-columns: repeat(2, 1fr);
    }
  }
  @media (max-width: 540px) {
    .cc-ag-loop-stages {
      grid-template-columns: 1fr;
    }
  }
  .cc-ag-loop-stage {
    border: 1px solid var(--cc-ink-faint);
    border-radius: 12px;
    background: rgba(255, 255, 255, 0.025);
    padding: 16px 18px;
    display: flex;
    flex-direction: column;
    gap: 6px;
  }
  .cc-ag-loop-stage .step {
    font-family: var(--cc-font-mono), monospace;
    font-size: 10px;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    color: var(--cc-amber);
  }
  .cc-ag-loop-stage h4 {
    font-family: var(--cc-font-sans), sans-serif;
    font-size: 18px;
    font-weight: 500;
    letter-spacing: -0.015em;
    margin: 0;
    color: var(--cc-ink);
  }
  .cc-ag-loop-stage .primitive {
    font-family: var(--cc-font-mono), monospace;
    font-size: 10px;
    letter-spacing: 0.14em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
  }
  .cc-ag-loop-stage p {
    font-size: 13px;
    line-height: 1.55;
    color: var(--cc-ink-dim);
    margin: 6px 0 0;
    text-wrap: pretty;
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
  .cc-ag-sees-tile {
    border: 1px solid var(--cc-ink-faint);
    border-radius: 14px;
    background: rgba(255, 255, 255, 0.02);
    padding: 20px;
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
    gap: 28px;
    margin-top: 16px;
  }
  .cc-ag-demo {
    border: 1px solid var(--cc-ink-faint);
    border-radius: 18px;
    background: linear-gradient(
      180deg,
      rgba(14, 22, 38, 0.78),
      rgba(10, 17, 30, 0.78)
    );
    padding: 28px;
  }
  @media (max-width: 880px) {
    .cc-ag-demo {
      padding: 22px;
    }
  }
  .cc-ag-demo-head {
    display: flex;
    align-items: baseline;
    flex-wrap: wrap;
    gap: 14px;
    margin-bottom: 22px;
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
  .cc-ag-demo-grid {
    display: grid;
    grid-template-columns: minmax(0, 1fr) minmax(0, 1.05fr);
    gap: 18px;
    align-items: stretch;
  }
  @media (max-width: 980px) {
    .cc-ag-demo-grid {
      grid-template-columns: 1fr;
    }
  }
  .cc-ag-demo-chat {
    border: 1px solid var(--cc-ink-faint);
    border-radius: 14px;
    background: rgba(8, 14, 26, 0.7);
    padding: 18px;
    display: flex;
    flex-direction: column;
    gap: 14px;
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

  /* ===== 06 Product surface tiles ===== */
  .cc-ag-products {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 18px;
    margin-top: 16px;
  }
  @media (max-width: 980px) {
    .cc-ag-products {
      grid-template-columns: repeat(2, 1fr);
    }
  }
  @media (max-width: 640px) {
    .cc-ag-products {
      grid-template-columns: 1fr;
    }
  }
  .cc-ag-product {
    border: 1px solid var(--cc-ink-faint);
    border-radius: 16px;
    background: rgba(255, 255, 255, 0.025);
    padding: 26px 24px;
    display: flex;
    flex-direction: column;
    gap: 16px;
    transition: border-color 0.18s ease, transform 0.18s ease,
      background 0.18s ease;
  }
  .cc-ag-product:hover {
    border-color: rgba(245, 241, 234, 0.32);
    transform: translateY(-2px);
    background: rgba(255, 255, 255, 0.04);
  }
  .cc-ag-product-icon {
    width: 56px;
    height: 56px;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 14px;
    display: flex;
    align-items: center;
    justify-content: center;
    color: var(--cc-ink);
    background: rgba(255, 255, 255, 0.025);
  }
  .cc-ag-product-tag {
    font-family: var(--cc-font-mono), monospace;
    font-size: 10px;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
  }
  .cc-ag-product h3 {
    font-family: var(--cc-font-sans), sans-serif;
    font-size: 20px;
    font-weight: 500;
    letter-spacing: -0.015em;
    color: var(--cc-ink);
    margin: 2px 0 0;
    line-height: 1.2;
  }
  .cc-ag-product p {
    font-size: 14px;
    line-height: 1.55;
    color: var(--cc-ink-dim);
    margin: 0;
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

  /* ===== 08 Works where you work ===== */
  .cc-ag-clients {
    display: grid;
    grid-template-columns: repeat(4, 1fr);
    gap: 14px;
    margin-top: 16px;
  }
  @media (max-width: 880px) {
    .cc-ag-clients {
      grid-template-columns: repeat(2, 1fr);
    }
  }
  @media (max-width: 480px) {
    .cc-ag-clients {
      grid-template-columns: 1fr;
    }
  }
  .cc-ag-client {
    border: 1px solid var(--cc-ink-faint);
    border-radius: 14px;
    background: rgba(255, 255, 255, 0.02);
    padding: 22px;
    display: flex;
    flex-direction: column;
    align-items: flex-start;
    gap: 14px;
    transition: border-color 0.15s ease, background 0.15s ease;
    text-decoration: none;
    color: inherit;
  }
  .cc-ag-client:hover {
    border-color: rgba(245, 241, 234, 0.32);
    background: rgba(255, 255, 255, 0.045);
  }
  .cc-ag-client-mono {
    width: 48px;
    height: 48px;
    border-radius: 12px;
    border: 1.5px solid var(--cc-ink-faint);
    display: flex;
    align-items: center;
    justify-content: center;
    color: var(--cc-ink);
    background: rgba(255, 255, 255, 0.025);
  }
  .cc-ag-client-name {
    font-family: var(--cc-font-sans), sans-serif;
    font-size: 16px;
    font-weight: 500;
    letter-spacing: -0.01em;
    color: var(--cc-ink);
  }
  .cc-ag-client-cta {
    margin-top: auto;
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.16em;
    text-transform: uppercase;
    color: var(--cc-amber);
  }

  /* ===== 09 Pricing teaser ===== */
  .cc-ag-pricing-inner {
    max-width: 1280px;
    margin: 0 auto;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 22px;
    background: rgba(255, 255, 255, 0.02);
    padding: 48px 56px;
    display: grid;
    grid-template-columns: minmax(0, 1.4fr) auto;
    gap: 40px;
    align-items: center;
  }
  @media (max-width: 880px) {
    .cc-ag-pricing-inner {
      grid-template-columns: 1fr;
      padding: 32px 24px;
    }
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

  /* ===== 10 Final CTA ===== */
  .cc-ag-final {
    padding-top: 60px;
    padding-bottom: 140px;
    text-align: center;
  }
  .cc-ag-final-inner {
    max-width: 720px;
    margin: 0 auto;
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
