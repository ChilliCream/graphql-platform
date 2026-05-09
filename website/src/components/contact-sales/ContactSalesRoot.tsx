"use client";

import styled from "styled-components";

// ContactSalesRoot mirrors EnterpriseRoot's tokens, then layers on the
// single-column form layout with the right-rail social proof and the
// "what happens next" 3-step strip.
export const ContactSalesRoot = styled.div`
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

  /* ===== Buttons (shared with PricingRoot/EnterpriseRoot) ===== */
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

  /* ===== Section shell ===== */
  section.cc-cs-section {
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

  /* ===== Hero ===== */
  .cc-cs-hero {
    padding-top: 160px;
    padding-bottom: 56px;
    text-align: center;
  }
  .cc-cs-hero-inner {
    max-width: 760px;
    margin: 0 auto;
  }
  .cc-cs-hero h1 {
    font-size: clamp(36px, 5vw, 64px);
    margin: 16px 0 18px;
    line-height: 1.05;
  }
  .cc-cs-hero h1 .accent {
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
  .cc-cs-hero p {
    font-size: clamp(15px, 1.2vw, 18px);
    line-height: 1.55;
    color: var(--cc-ink-dim);
    max-width: 56ch;
    margin: 0 auto;
    text-wrap: pretty;
  }

  /* ===== Layout: form + right rail ===== */
  .cc-cs-layout {
    padding-top: 16px;
    padding-bottom: 96px;
  }
  .cc-cs-layout-inner {
    max-width: 1180px;
    margin: 0 auto;
    display: grid;
    grid-template-columns: minmax(0, 1.05fr) minmax(0, 0.95fr);
    gap: 56px;
    align-items: start;
  }
  @media (max-width: 980px) {
    .cc-cs-layout-inner {
      grid-template-columns: 1fr;
      gap: 36px;
    }
  }
  .cc-cs-form-card {
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
  .cc-cs-form-inner {
    border-radius: 21px;
    background: linear-gradient(
      180deg,
      rgba(14, 22, 38, 0.96),
      rgba(10, 17, 30, 0.96)
    );
    padding: 40px 40px 36px;
  }
  @media (max-width: 560px) {
    .cc-cs-form-inner {
      padding: 28px 22px;
    }
  }
  .cc-cs-form-heading {
    font-size: 22px;
    font-weight: 500;
    letter-spacing: -0.015em;
    color: var(--cc-ink);
    margin: 6px 0 18px;
  }

  /* ===== Right rail: social proof ===== */
  .cc-cs-rail {
    display: flex;
    flex-direction: column;
    gap: 24px;
    position: sticky;
    top: 32px;
  }
  @media (max-width: 980px) {
    .cc-cs-rail {
      position: static;
    }
  }
  .cc-cs-proof {
    border: 1px solid var(--cc-ink-faint);
    border-radius: 18px;
    background: rgba(255, 255, 255, 0.025);
    padding: 28px 26px;
  }
  .cc-cs-proof-title {
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
    margin: 0 0 18px;
  }
  .cc-cs-proof-list {
    list-style: none;
    padding: 0;
    margin: 0;
    display: flex;
    flex-direction: column;
    gap: 16px;
  }
  .cc-cs-proof-item {
    display: flex;
    flex-direction: column;
    gap: 6px;
    padding-bottom: 16px;
    border-bottom: 1px solid var(--cc-ink-faint);
  }
  .cc-cs-proof-item:last-child {
    border-bottom: 0;
    padding-bottom: 0;
  }
  .cc-cs-proof-persona {
    font-size: 13px;
    color: var(--cc-ink);
    font-weight: 500;
  }
  .cc-cs-proof-metric {
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.08em;
    color: var(--cc-col-shi);
  }

  .cc-cs-proof-alt {
    font-size: 13px;
    color: var(--cc-ink-dim);
    line-height: 1.5;
    margin: 0;
    text-wrap: pretty;
  }
  .cc-cs-proof-alt strong {
    color: var(--cc-ink);
    font-weight: 500;
  }

  /* ===== "What happens next" strip ===== */
  .cc-cs-next {
    padding-top: 0;
    padding-bottom: 140px;
  }
  .cc-cs-next-inner {
    max-width: 1180px;
    margin: 0 auto;
  }
  .cc-cs-next-heading {
    text-align: center;
    margin: 0 auto 40px;
    max-width: 640px;
  }
  .cc-cs-next-heading h2 {
    font-size: clamp(28px, 3.4vw, 40px);
    margin: 8px auto 14px;
  }
  .cc-cs-next-heading p {
    font-size: 15px;
    color: var(--cc-ink-dim);
    line-height: 1.55;
    margin: 0 auto;
    max-width: 50ch;
  }
  .cc-cs-next-grid {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 20px;
  }
  @media (max-width: 880px) {
    .cc-cs-next-grid {
      grid-template-columns: 1fr;
      max-width: 480px;
      margin: 0 auto;
    }
  }
  .cc-cs-step {
    padding: 28px 26px;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 18px;
    background: rgba(255, 255, 255, 0.025);
    display: flex;
    flex-direction: column;
    gap: 12px;
  }
  .cc-cs-step-num {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    width: 32px;
    height: 32px;
    border-radius: 999px;
    border: 1px solid var(--cc-ink-faint);
    font-family: var(--cc-font-mono), monospace;
    font-size: 12px;
    letter-spacing: 0.08em;
    color: var(--cc-ink);
    background: rgba(255, 255, 255, 0.04);
  }
  .cc-cs-step-title {
    font-size: 17px;
    font-weight: 500;
    letter-spacing: -0.01em;
    color: var(--cc-ink);
    margin: 0;
  }
  .cc-cs-step-body {
    font-size: 14px;
    line-height: 1.55;
    color: var(--cc-ink-dim);
    margin: 0;
    text-wrap: pretty;
  }
`;
