"use client";

import styled from "styled-components";

// EnterpriseRoot owns the dark navy / cream-ink design tokens for the
// /enterprise page. It mirrors PricingRoot's approach: same tokens, same
// section shell, same typography and button system, but with section CSS
// scoped to the 13 enterprise sections (hero, pillars, outcomes, ROI,
// federation deep-dive, SKU cards, self-hosted, compliance grid, team,
// migration, inline form, etc.).
export const EnterpriseRoot = styled.div`
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
  section.cc-ent-section {
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

  /* ===== Section heading wrapper (used by most sections) ===== */
  .cc-ent-heading {
    text-align: center;
    margin: 0 auto 48px;
    max-width: 760px;
  }
  .cc-ent-heading h2 {
    font-size: clamp(34px, 4.4vw, 56px);
    margin: 8px auto 14px;
    max-width: 22ch;
  }
  .cc-ent-heading p {
    font-size: clamp(15px, 1.1vw, 17px);
    color: var(--cc-ink-dim);
    max-width: 56ch;
    margin: 0 auto;
    text-wrap: pretty;
    line-height: 1.55;
  }

  /* ===== 02 Hero ===== */
  .cc-ent-hero {
    padding-top: 160px;
    padding-bottom: 72px;
    text-align: center;
  }
  .cc-ent-hero-inner {
    max-width: 960px;
    margin: 0 auto;
  }
  .cc-ent-hero h1 {
    font-size: clamp(40px, 6vw, 88px);
    margin: 18px 0 24px;
    line-height: 1.02;
  }
  .cc-ent-hero h1 .accent {
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
  .cc-ent-hero p {
    font-size: clamp(15px, 1.2vw, 19px);
    line-height: 1.55;
    color: var(--cc-ink-dim);
    max-width: 64ch;
    margin: 0 auto 32px;
    text-wrap: pretty;
  }

  /* Hero monogram trust strip */
  .cc-ent-trust-strip {
    margin: 64px auto 0;
    max-width: 1080px;
    display: grid;
    grid-template-columns: repeat(5, 1fr);
    gap: 18px;
  }
  @media (max-width: 880px) {
    .cc-ent-trust-strip {
      grid-template-columns: repeat(2, 1fr);
      max-width: 480px;
      gap: 14px;
    }
  }
  .cc-ent-trust-tile {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 12px;
    padding: 22px 14px 18px;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 14px;
    background: rgba(255, 255, 255, 0.02);
    transition: border-color 0.15s ease, background 0.15s ease;
  }
  .cc-ent-trust-tile:hover {
    border-color: rgba(245, 241, 234, 0.28);
    background: rgba(255, 255, 255, 0.04);
  }
  .cc-ent-trust-mono {
    width: 56px;
    height: 56px;
    border-radius: 12px;
    border: 1.5px solid var(--cc-ink-faint);
    display: flex;
    align-items: center;
    justify-content: center;
    color: var(--cc-ink);
    background: rgba(255, 255, 255, 0.02);
  }
  .cc-ent-trust-caption {
    font-family: var(--cc-font-mono), monospace;
    font-size: 10px;
    letter-spacing: 0.16em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
    text-align: center;
    line-height: 1.35;
  }

  /* ===== 03 Pillars ===== */
  .cc-ent-pillars {
    padding-top: 96px;
    padding-bottom: 96px;
  }
  .cc-ent-pillars-inner {
    max-width: 1180px;
    margin: 0 auto;
  }
  .cc-ent-pillars-grid {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 24px;
  }
  @media (max-width: 980px) {
    .cc-ent-pillars-grid {
      grid-template-columns: 1fr;
      max-width: 540px;
      margin: 0 auto;
    }
  }
  .cc-ent-pillar {
    padding: 32px 28px;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 18px;
    background: rgba(255, 255, 255, 0.02);
    transition: border-color 0.15s ease, transform 0.15s ease;
  }
  .cc-ent-pillar:hover {
    border-color: rgba(245, 241, 234, 0.28);
    transform: translateY(-2px);
  }
  .cc-ent-pillar-icon {
    width: 44px;
    height: 44px;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 12px;
    display: flex;
    align-items: center;
    justify-content: center;
    color: var(--cc-col-shi);
    margin-bottom: 18px;
  }
  .cc-ent-pillar-title {
    font-size: 22px;
    font-weight: 500;
    letter-spacing: -0.02em;
    margin: 0 0 10px;
    color: var(--cc-ink);
  }
  .cc-ent-pillar-tagline {
    font-size: 15px;
    line-height: 1.55;
    color: var(--cc-ink-dim);
    margin: 0;
    text-wrap: pretty;
  }

  /* ===== Customer outcome card ===== */
  .cc-ent-outcome {
    padding-top: 0;
    padding-bottom: 96px;
  }
  .cc-ent-outcome-card {
    max-width: 980px;
    margin: 0 auto;
    padding: 48px 56px 44px;
    border: 1px solid var(--cc-ink-faint);
    border-left: 2px solid var(--cc-col-shi);
    border-radius: 20px;
    background: rgba(255, 255, 255, 0.025);
    display: flex;
    flex-direction: column;
    gap: 20px;
  }
  @media (max-width: 720px) {
    .cc-ent-outcome-card {
      padding: 32px 24px;
    }
  }
  .cc-ent-outcome-persona {
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.16em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
  }
  .cc-ent-outcome-quote {
    font-size: clamp(20px, 2.2vw, 28px);
    line-height: 1.35;
    letter-spacing: -0.015em;
    color: var(--cc-ink);
    font-weight: 500;
    margin: 0;
    text-wrap: pretty;
  }
  .cc-ent-outcome-meta {
    display: flex;
    flex-wrap: wrap;
    gap: 20px 28px;
    align-items: baseline;
    padding-top: 14px;
    border-top: 1px solid var(--cc-ink-faint);
  }
  .cc-ent-outcome-metric {
    font-family: var(--cc-font-mono), monospace;
    font-size: 12px;
    letter-spacing: 0.12em;
    text-transform: uppercase;
    color: var(--cc-col-shi);
  }
  .cc-ent-outcome-attribution {
    font-size: 13px;
    color: var(--cc-ink-dim);
  }

  /* ===== 05 ROI numbers ===== */
  .cc-ent-roi {
    padding-top: 0;
    padding-bottom: 120px;
  }
  .cc-ent-roi-inner {
    max-width: 1180px;
    margin: 0 auto;
  }
  .cc-ent-roi-grid {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 24px;
  }
  @media (max-width: 880px) {
    .cc-ent-roi-grid {
      grid-template-columns: 1fr;
    }
  }
  .cc-ent-roi-tile {
    padding: 40px 32px;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 18px;
    background: rgba(255, 255, 255, 0.025);
    text-align: center;
  }
  .cc-ent-roi-num {
    font-size: clamp(40px, 4.4vw, 60px);
    font-weight: 500;
    letter-spacing: -0.025em;
    color: var(--cc-ink);
    line-height: 1;
    margin: 0 0 10px;
  }
  .cc-ent-roi-num .accent {
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
  .cc-ent-roi-caption {
    font-size: 14px;
    color: var(--cc-ink-dim);
    line-height: 1.5;
    margin: 0;
    text-wrap: pretty;
  }
  .cc-ent-roi-note {
    text-align: center;
    margin: 32px auto 0;
    max-width: 60ch;
    font-size: 13px;
    color: var(--cc-ink-dim);
    line-height: 1.6;
  }

  /* ===== 06 Federation deep-dive ===== */
  .cc-ent-federation {
    padding-top: 0;
    padding-bottom: 120px;
  }
  .cc-ent-federation-inner {
    max-width: 1180px;
    margin: 0 auto;
  }
  .cc-ent-federation-grid {
    display: grid;
    grid-template-columns: minmax(0, 1fr) minmax(0, 1.1fr);
    gap: 56px;
    align-items: center;
  }
  @media (max-width: 980px) {
    .cc-ent-federation-grid {
      grid-template-columns: 1fr;
      gap: 32px;
    }
  }
  .cc-ent-federation-copy h2 {
    font-size: clamp(32px, 4vw, 48px);
    margin: 8px 0 18px;
    line-height: 1.05;
  }
  .cc-ent-federation-copy p {
    font-size: 16px;
    line-height: 1.6;
    color: var(--cc-ink-dim);
    margin: 0 0 24px;
    text-wrap: pretty;
  }
  .cc-ent-federation-bullets {
    list-style: none;
    padding: 0;
    margin: 0 0 24px;
    display: flex;
    flex-direction: column;
    gap: 10px;
  }
  .cc-ent-federation-bullets li {
    display: flex;
    align-items: flex-start;
    gap: 10px;
    font-size: 14px;
    color: var(--cc-ink);
    line-height: 1.5;
  }
  .cc-ent-federation-bullets li svg {
    color: var(--cc-col-shi);
    flex-shrink: 0;
    margin-top: 4px;
  }
  .cc-ent-federation-diagram {
    width: 100%;
    aspect-ratio: 5 / 4;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 18px;
    background: linear-gradient(
      180deg,
      rgba(14, 22, 38, 0.7),
      rgba(10, 17, 30, 0.7)
    );
    padding: 28px;
  }
  .cc-ent-federation-diagram svg {
    width: 100%;
    height: 100%;
  }

  /* ===== 08 SKU cards ===== */
  .cc-ent-skus {
    padding-top: 0;
    padding-bottom: 120px;
  }
  .cc-ent-skus-inner {
    max-width: 1280px;
    margin: 0 auto;
  }
  .cc-ent-skus-grid {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 24px;
  }
  @media (max-width: 980px) {
    .cc-ent-skus-grid {
      grid-template-columns: 1fr;
      max-width: 540px;
      margin: 0 auto;
    }
  }
  .cc-ent-sku-card {
    display: flex;
    flex-direction: column;
    padding: 36px 32px 32px;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 18px;
    background: rgba(255, 255, 255, 0.025);
    transition: border-color 0.15s ease, transform 0.15s ease;
  }
  .cc-ent-sku-card:hover {
    border-color: rgba(245, 241, 234, 0.32);
    transform: translateY(-2px);
  }
  .cc-ent-sku-icon {
    width: 88px;
    height: 96px;
    margin: 0 auto 22px;
  }
  .cc-ent-sku-name {
    text-align: center;
    font-size: 22px;
    font-weight: 500;
    letter-spacing: -0.02em;
    color: var(--cc-ink);
    margin: 0 0 8px;
  }
  .cc-ent-sku-tagline {
    text-align: center;
    font-size: 14px;
    line-height: 1.5;
    color: var(--cc-ink-dim);
    margin: 0 auto 22px;
    max-width: 32ch;
    text-wrap: pretty;
  }
  .cc-ent-sku-bullets {
    list-style: none;
    padding: 0;
    margin: 0 0 22px;
    display: flex;
    flex-direction: column;
    gap: 10px;
    flex: 1;
  }
  .cc-ent-sku-bullets li {
    display: flex;
    align-items: flex-start;
    gap: 10px;
    font-size: 14px;
    color: var(--cc-ink);
    line-height: 1.5;
  }
  .cc-ent-sku-bullets li svg {
    color: var(--cc-col-shi);
    flex-shrink: 0;
    margin-top: 4px;
  }
  .cc-ent-sku-link {
    text-align: center;
    font-family: var(--cc-font-mono), monospace;
    font-size: 12px;
    letter-spacing: 0.16em;
    text-transform: uppercase;
    color: var(--cc-ink);
    text-decoration: none;
    padding-top: 18px;
    border-top: 1px solid var(--cc-ink-faint);
    transition: color 0.15s ease;
  }
  .cc-ent-sku-link:hover {
    color: var(--cc-col-shi);
  }

  /* ===== 09 Self-hosted / air-gapped ===== */
  .cc-ent-airgap {
    padding-top: 0;
    padding-bottom: 120px;
  }
  .cc-ent-airgap-inner {
    max-width: 1180px;
    margin: 0 auto;
  }
  .cc-ent-airgap-grid {
    display: grid;
    grid-template-columns: minmax(0, 1.1fr) minmax(0, 1fr);
    gap: 56px;
    align-items: center;
  }
  @media (max-width: 980px) {
    .cc-ent-airgap-grid {
      grid-template-columns: 1fr;
      gap: 32px;
    }
  }
  .cc-ent-airgap-copy h2 {
    font-size: clamp(32px, 4vw, 48px);
    margin: 8px 0 18px;
    line-height: 1.05;
  }
  .cc-ent-airgap-copy p {
    font-size: 16px;
    line-height: 1.6;
    color: var(--cc-ink-dim);
    margin: 0 0 22px;
    text-wrap: pretty;
  }
  .cc-ent-airgap-chips {
    display: flex;
    flex-wrap: wrap;
    gap: 8px;
    margin-bottom: 24px;
  }
  .cc-ent-airgap-chip {
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
  .cc-ent-airgap-diagram {
    width: 100%;
    aspect-ratio: 5 / 4;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 18px;
    background: linear-gradient(
      180deg,
      rgba(14, 22, 38, 0.7),
      rgba(10, 17, 30, 0.7)
    );
    padding: 28px;
  }
  .cc-ent-airgap-diagram svg {
    width: 100%;
    height: 100%;
  }

  /* ===== 10 Compliance grid ===== */
  .cc-ent-compliance {
    padding-top: 0;
    padding-bottom: 120px;
  }
  .cc-ent-compliance-inner {
    max-width: 1280px;
    margin: 0 auto;
  }
  .cc-ent-compliance-grid {
    display: grid;
    grid-template-columns: repeat(4, 1fr);
    gap: 18px;
  }
  @media (max-width: 1080px) {
    .cc-ent-compliance-grid {
      grid-template-columns: repeat(2, 1fr);
    }
  }
  @media (max-width: 600px) {
    .cc-ent-compliance-grid {
      grid-template-columns: 1fr;
    }
  }
  .cc-ent-compliance-tile {
    display: flex;
    flex-direction: column;
    padding: 26px 22px;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 14px;
    background: rgba(255, 255, 255, 0.025);
    transition: border-color 0.15s ease, background 0.15s ease;
    min-height: 220px;
    text-decoration: none;
  }
  .cc-ent-compliance-tile:hover {
    border-color: rgba(245, 241, 234, 0.32);
    background: rgba(255, 255, 255, 0.04);
  }
  .cc-ent-compliance-icon {
    width: 36px;
    height: 36px;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 10px;
    display: flex;
    align-items: center;
    justify-content: center;
    color: var(--cc-col-shi);
    margin-bottom: 16px;
  }
  .cc-ent-compliance-title {
    font-size: 16px;
    font-weight: 500;
    letter-spacing: -0.01em;
    color: var(--cc-ink);
    margin: 0 0 10px;
    display: flex;
    align-items: center;
    gap: 8px;
    flex-wrap: wrap;
  }
  .cc-ent-compliance-status {
    font-family: var(--cc-font-mono), monospace;
    font-size: 9px;
    letter-spacing: 0.16em;
    text-transform: uppercase;
    color: var(--cc-col-bil);
    padding: 3px 6px;
    border: 1px solid rgba(245, 241, 234, 0.18);
    border-radius: 6px;
  }
  .cc-ent-compliance-body {
    font-size: 13px;
    line-height: 1.55;
    color: var(--cc-ink-dim);
    margin: 0 0 18px;
    flex: 1;
    text-wrap: pretty;
  }
  .cc-ent-compliance-link {
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.16em;
    text-transform: uppercase;
    color: var(--cc-ink);
    text-decoration: none;
    margin-top: auto;
  }
  .cc-ent-compliance-trust {
    margin: 36px auto 0;
    max-width: 720px;
    text-align: center;
    font-family: var(--cc-font-mono), monospace;
    font-size: 12px;
    letter-spacing: 0.12em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
    padding: 16px 18px;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 12px;
    background: rgba(255, 255, 255, 0.02);
  }
  .cc-ent-compliance-trust strong {
    color: var(--cc-col-ord);
    font-weight: 500;
  }

  /* ===== 11 Built by the team ===== */
  .cc-ent-team {
    padding-top: 0;
    padding-bottom: 120px;
  }
  .cc-ent-team-inner {
    max-width: 1180px;
    margin: 0 auto;
  }
  .cc-ent-team-grid {
    display: grid;
    grid-template-columns: repeat(4, 1fr);
    gap: 18px;
  }
  @media (max-width: 980px) {
    .cc-ent-team-grid {
      grid-template-columns: repeat(2, 1fr);
    }
  }
  @media (max-width: 560px) {
    .cc-ent-team-grid {
      grid-template-columns: 1fr;
    }
  }
  .cc-ent-team-tile {
    display: flex;
    flex-direction: column;
    align-items: flex-start;
    padding: 24px 22px;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 16px;
    background: rgba(255, 255, 255, 0.025);
  }
  .cc-ent-team-mono {
    width: 56px;
    height: 56px;
    border-radius: 14px;
    border: 1.5px solid var(--cc-ink-faint);
    display: flex;
    align-items: center;
    justify-content: center;
    font-family: var(--cc-font-sans), sans-serif;
    font-size: 22px;
    font-weight: 500;
    letter-spacing: -0.02em;
    color: var(--cc-ink);
    background: rgba(255, 255, 255, 0.025);
    margin-bottom: 16px;
  }
  .cc-ent-team-name {
    font-size: 16px;
    font-weight: 500;
    color: var(--cc-ink);
    margin: 0 0 4px;
  }
  .cc-ent-team-handle {
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.08em;
    color: var(--cc-col-shi);
    margin: 0 0 12px;
  }
  .cc-ent-team-bio {
    font-size: 13px;
    line-height: 1.5;
    color: var(--cc-ink-dim);
    margin: 0;
    text-wrap: pretty;
  }
  .cc-ent-team-footer {
    margin: 32px auto 0;
    max-width: 60ch;
    text-align: center;
    font-size: 14px;
    color: var(--cc-ink-dim);
    line-height: 1.6;
  }

  /* ===== 12 Migration ===== */
  .cc-ent-migration {
    padding-top: 0;
    padding-bottom: 120px;
  }
  .cc-ent-migration-inner {
    max-width: 1180px;
    margin: 0 auto;
  }
  .cc-ent-migration-grid {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 20px;
  }
  @media (max-width: 980px) {
    .cc-ent-migration-grid {
      grid-template-columns: 1fr;
      max-width: 560px;
      margin: 0 auto;
    }
  }
  .cc-ent-migration-card {
    display: flex;
    flex-direction: column;
    padding: 32px 28px;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 18px;
    background: rgba(255, 255, 255, 0.025);
  }
  .cc-ent-migration-source {
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.16em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
    margin-bottom: 14px;
  }
  .cc-ent-migration-headline {
    font-size: 19px;
    font-weight: 500;
    letter-spacing: -0.015em;
    color: var(--cc-ink);
    margin: 0 0 10px;
    line-height: 1.3;
  }
  .cc-ent-migration-body {
    font-size: 14px;
    line-height: 1.55;
    color: var(--cc-ink-dim);
    margin: 0 0 22px;
    flex: 1;
    text-wrap: pretty;
  }
  .cc-ent-migration-cta {
    font-family: var(--cc-font-mono), monospace;
    font-size: 12px;
    letter-spacing: 0.16em;
    text-transform: uppercase;
    color: var(--cc-ink);
    text-decoration: none;
    padding-top: 14px;
    border-top: 1px solid var(--cc-ink-faint);
    transition: color 0.15s ease;
  }
  .cc-ent-migration-card:hover .cc-ent-migration-cta {
    color: var(--cc-col-shi);
  }

  /* ===== 13 Inline form ===== */
  .cc-ent-form {
    padding-top: 0;
    padding-bottom: 140px;
  }
  .cc-ent-form-inner {
    max-width: 1080px;
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
    box-shadow: 0 30px 80px -40px rgba(0, 0, 0, 0.7),
      0 0 60px -10px rgba(120, 140, 220, 0.18);
  }
  .cc-ent-form-card {
    border-radius: 21px;
    background: linear-gradient(
      180deg,
      rgba(14, 22, 38, 0.96),
      rgba(10, 17, 30, 0.96)
    );
    padding: 56px 56px 52px;
    display: grid;
    grid-template-columns: minmax(0, 0.95fr) minmax(0, 1.05fr);
    gap: 48px;
    align-items: start;
  }
  @media (max-width: 980px) {
    .cc-ent-form-card {
      grid-template-columns: 1fr;
      padding: 36px 28px;
      gap: 32px;
    }
  }
  .cc-ent-form-copy h2 {
    font-size: clamp(28px, 3.4vw, 42px);
    margin: 12px 0 16px;
    line-height: 1.05;
  }
  .cc-ent-form-copy p {
    font-size: 16px;
    line-height: 1.6;
    color: var(--cc-ink-dim);
    margin: 0 0 22px;
    max-width: 50ch;
    text-wrap: pretty;
  }
  .cc-ent-form-bullets {
    list-style: none;
    padding: 0;
    margin: 0;
    display: flex;
    flex-direction: column;
    gap: 12px;
  }
  .cc-ent-form-bullets li {
    display: flex;
    align-items: flex-start;
    gap: 10px;
    font-size: 14px;
    color: var(--cc-ink);
    line-height: 1.45;
  }
  .cc-ent-form-bullets li svg {
    color: var(--cc-col-shi);
    flex-shrink: 0;
    margin-top: 3px;
  }
`;
