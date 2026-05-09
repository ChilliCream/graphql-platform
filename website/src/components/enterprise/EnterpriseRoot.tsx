"use client";

import styled from "styled-components";

// EnterpriseRoot owns the dark navy / cream-ink design tokens for the
// /enterprise page. Layout responsibility is delegated to the redesign-system
// `<Band>` primitive: bands handle their own surface, padding, and rhythm.
// This file owns typography, buttons, section labels, and the per-section
// inner-content classes that bands wrap.
//
// The page accent (`--cc-accent`, `--cc-accent-soft`, etc.) is supplied by
// `<AccentThread page="enterprise">` in the page component. Hard-coded
// per-language gradients (--cc-col-*) are kept as illustration palette only.
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

  /* ===== Section label (kicker) ===== */
  .cc-section-label {
    display: inline-flex;
    align-items: center;
    gap: 10px;
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.18em;
    color: var(--cc-ink-dim);
    text-transform: uppercase;
    margin-bottom: 24px;
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
  .cc-ent-hero-inner {
    max-width: 960px;
    margin: 0 auto;
    text-align: center;
    padding-top: 64px;
  }
  .cc-ent-hero-inner .cc-section-label {
    margin: 0 auto 18px;
  }
  .cc-ent-hero-inner h1 {
    font-size: clamp(40px, 6vw, 88px);
    margin: 18px 0 24px;
    line-height: 1.02;
  }
  .cc-ent-hero-inner h1 .accent {
    background: var(
      --cc-accent-gradient,
      linear-gradient(120deg, var(--cc-accent), var(--cc-ink))
    );
    -webkit-background-clip: text;
    background-clip: text;
    -webkit-text-fill-color: transparent;
  }
  .cc-ent-hero-inner > p {
    font-size: clamp(15px, 1.2vw, 19px);
    line-height: 1.55;
    color: var(--cc-ink-dim);
    max-width: 64ch;
    margin: 0 auto 32px;
    text-wrap: pretty;
  }

  /* Hero typographic trust line */
  .cc-ent-hero-trustline {
    margin: 40px auto 0;
    max-width: 720px;
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.16em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
    line-height: 1.6;
    text-wrap: pretty;
  }
  .cc-ent-hero-trustline .seg {
    color: var(--cc-ink);
  }
  .cc-ent-hero-trustline .sep {
    color: var(--cc-ink-faint);
  }

  /* ===== 03 Pillars (band content, no card chrome) ===== */
  .cc-ent-pillars-inner {
    max-width: 1180px;
    margin: 0 auto;
  }
  .cc-ent-pillars-grid {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 0;
    border-top: 1px solid var(--cc-ink-faint);
  }
  @media (max-width: 980px) {
    .cc-ent-pillars-grid {
      grid-template-columns: 1fr;
      max-width: 540px;
      margin: 0 auto;
    }
  }
  .cc-ent-pillar {
    padding: 36px 28px 8px;
    border-right: 1px solid var(--cc-ink-faint);
  }
  .cc-ent-pillar:last-child {
    border-right: none;
  }
  @media (max-width: 980px) {
    .cc-ent-pillar {
      border-right: none;
      border-bottom: 1px solid var(--cc-ink-faint);
    }
    .cc-ent-pillar:last-child {
      border-bottom: none;
    }
  }
  .cc-ent-pillar-icon {
    width: 44px;
    height: 44px;
    display: flex;
    align-items: center;
    justify-content: flex-start;
    color: var(--cc-accent);
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
  .cc-ent-outcome-card {
    max-width: 980px;
    margin: 0 auto;
    padding: 48px 56px 44px;
    border: 1px solid var(--cc-ink-faint);
    border-left: 2px solid var(--cc-accent);
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
    color: var(--cc-accent);
  }
  .cc-ent-outcome-attribution {
    font-size: 13px;
    color: var(--cc-ink-dim);
  }

  /* Tinted-band ink scope.
     The Band variant="tinted" primitive paints a cream surface, but
     EnterpriseRoot ships --cc-ink as cream for the dark canvas. Apply this
     class on the band's content wrapper so labels, headings, hairlines and
     dim copy all read against the cream. The global h1-h6 rule binds to
     --cc-heading-text-color (see global-style.tsx), so the scope overrides
     that token too. */
  .cc-ent-tint-scope {
    --cc-ink: #1a1f2e;
    --cc-ink-dim: rgba(26, 31, 46, 0.66);
    --cc-ink-faint: rgba(26, 31, 46, 0.16);
    --cc-heading-text-color: #1a1f2e;
    color: var(--cc-ink);
  }
  .cc-ent-tint-scope .cc-section-label .num {
    border-color: var(--cc-ink-faint);
    color: var(--cc-ink);
  }

  /* ===== 05 ROI numbers (StatRow on tinted band) ===== */
  .cc-ent-roi-inner {
    max-width: 1180px;
    margin: 0 auto;
  }
  .cc-ent-roi-note {
    text-align: center;
    margin: 32px auto 0;
    max-width: 60ch;
    font-size: 13px;
    color: var(--cc-ink-dim);
    line-height: 1.6;
  }

  /* ===== 06 Federation deep-dive (full-bleed diagram) ===== */
  .cc-ent-federation-inner {
    max-width: 1280px;
    margin: 0 auto;
  }
  .cc-ent-federation-head {
    max-width: 760px;
    margin: 0 auto 40px;
    text-align: center;
  }
  .cc-ent-federation-head h2 {
    font-size: clamp(32px, 4.4vw, 56px);
    margin: 8px 0 18px;
    line-height: 1.05;
  }
  .cc-ent-federation-head p {
    font-size: clamp(15px, 1.1vw, 17px);
    line-height: 1.6;
    color: var(--cc-ink-dim);
    margin: 0;
    text-wrap: pretty;
  }
  /* The diagram is the page's signature visual — full column width, very
     tall (450-550px) so it dominates instead of competing with the SKU rack
     that follows. */
  .cc-ent-federation-diagram {
    width: 100%;
    min-height: 460px;
    height: clamp(460px, 56vw, 560px);
    border: 1px solid var(--cc-ink-faint);
    border-radius: 22px;
    background: radial-gradient(
        70% 70% at 50% 50%,
        var(--cc-accent-soft, rgba(120, 140, 220, 0.06)),
        transparent 70%
      ),
      linear-gradient(180deg, rgba(14, 22, 38, 0.7), rgba(10, 17, 30, 0.7));
    padding: clamp(20px, 3vw, 40px);
    margin: 0 auto;
  }
  .cc-ent-federation-diagram svg {
    width: 100%;
    height: 100%;
  }
  .cc-ent-federation-foot {
    max-width: 980px;
    margin: 40px auto 0;
    display: grid;
    grid-template-columns: minmax(0, 1fr) auto;
    gap: 32px;
    align-items: center;
  }
  @media (max-width: 880px) {
    .cc-ent-federation-foot {
      grid-template-columns: 1fr;
      text-align: left;
    }
  }
  .cc-ent-federation-bullets {
    list-style: none;
    padding: 0;
    margin: 0;
    display: grid;
    grid-template-columns: repeat(2, 1fr);
    gap: 12px 32px;
  }
  @media (max-width: 720px) {
    .cc-ent-federation-bullets {
      grid-template-columns: 1fr;
    }
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
    color: var(--cc-accent);
    flex-shrink: 0;
    margin-top: 4px;
  }

  /* ===== 08 SKU constraint cards (inverted band) ===== */
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
    color: var(--cc-accent);
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
    color: var(--cc-accent);
  }

  /* ===== 09 Self-hosted / air-gapped (default + Hemisphere) ===== */
  .cc-ent-airgap-hemi {
    position: absolute;
    top: 0;
    right: -120px;
    bottom: 0;
    width: 60%;
    max-width: 720px;
    pointer-events: none;
    color: var(--cc-accent);
    opacity: 0.7;
    z-index: 0;
  }
  .cc-ent-airgap-hemi svg {
    width: 100%;
    height: 100%;
  }
  .cc-ent-airgap-inner {
    position: relative;
    z-index: 1;
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

  /* ===== 10 Compliance two-tier ===== */
  .cc-ent-compliance-inner {
    max-width: 1280px;
    margin: 0 auto;
  }
  .cc-ent-attest-label,
  .cc-ent-cap-label {
    display: inline-flex;
    align-items: center;
    gap: 10px;
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.16em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
    margin: 0 0 18px;
  }
  .cc-ent-attest-label .num,
  .cc-ent-cap-label .num {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    width: 22px;
    height: 22px;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 4px;
    color: var(--cc-ink);
  }
  .cc-ent-cap-label {
    margin-top: 64px;
  }

  /* Tier 1: large attestation badge tiles */
  .cc-ent-attest-grid {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 20px;
  }
  @media (max-width: 980px) {
    .cc-ent-attest-grid {
      grid-template-columns: 1fr;
    }
  }
  .cc-ent-attest-tile {
    display: flex;
    align-items: stretch;
    gap: 20px;
    padding: 28px;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 16px;
    background: rgba(255, 255, 255, 0.025);
    text-decoration: none;
    color: var(--cc-ink);
    transition: border-color 0.15s ease, background 0.15s ease;
  }
  .cc-ent-attest-tile:hover {
    border-color: var(--cc-accent-line, rgba(245, 241, 234, 0.32));
    background: rgba(255, 255, 255, 0.04);
  }
  .cc-ent-attest-badge {
    width: 68px;
    height: 68px;
    flex-shrink: 0;
    border: 1.5px solid var(--cc-accent);
    border-radius: 16px;
    display: flex;
    align-items: center;
    justify-content: center;
    color: var(--cc-accent);
    padding: 14px;
    background: var(--cc-accent-soft, rgba(245, 241, 234, 0.04));
  }
  .cc-ent-attest-meta {
    display: flex;
    flex-direction: column;
    gap: 8px;
    flex: 1;
    min-width: 0;
  }
  .cc-ent-attest-title {
    font-size: 20px;
    font-weight: 500;
    letter-spacing: -0.015em;
    color: var(--cc-ink);
    margin: 0;
    display: flex;
    align-items: center;
    gap: 10px;
    flex-wrap: wrap;
  }
  .cc-ent-attest-body {
    font-size: 13px;
    line-height: 1.55;
    color: var(--cc-ink-dim);
    margin: 0;
    flex: 1;
    text-wrap: pretty;
  }
  .cc-ent-compliance-status {
    font-family: var(--cc-font-mono), monospace;
    font-size: 9px;
    letter-spacing: 0.16em;
    text-transform: uppercase;
    color: var(--cc-accent);
    padding: 3px 6px;
    border: 1px solid var(--cc-accent-line, rgba(245, 241, 234, 0.18));
    border-radius: 6px;
  }
  .cc-ent-compliance-link {
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.16em;
    text-transform: uppercase;
    color: var(--cc-ink);
    text-decoration: none;
    margin-top: 4px;
  }

  /* Tier 2: dense capability grid */
  .cc-ent-cap-grid {
    display: grid;
    grid-template-columns: repeat(4, 1fr);
    gap: 0;
    border-top: 1px solid var(--cc-ink-faint);
    border-bottom: 1px solid var(--cc-ink-faint);
  }
  @media (max-width: 1080px) {
    .cc-ent-cap-grid {
      grid-template-columns: repeat(2, 1fr);
    }
  }
  @media (max-width: 600px) {
    .cc-ent-cap-grid {
      grid-template-columns: 1fr;
    }
  }
  .cc-ent-cap-tile {
    display: flex;
    flex-direction: column;
    gap: 10px;
    padding: 24px 22px;
    text-decoration: none;
    color: var(--cc-ink);
    border-right: 1px solid var(--cc-ink-faint);
    border-bottom: 1px solid var(--cc-ink-faint);
    transition: background 0.15s ease;
  }
  .cc-ent-cap-tile:nth-child(4n) {
    border-right: none;
  }
  .cc-ent-cap-tile:nth-last-child(-n + 4) {
    border-bottom: none;
  }
  @media (max-width: 1080px) {
    .cc-ent-cap-tile:nth-child(4n) {
      border-right: 1px solid var(--cc-ink-faint);
    }
    .cc-ent-cap-tile:nth-child(2n) {
      border-right: none;
    }
  }
  @media (max-width: 600px) {
    .cc-ent-cap-tile {
      border-right: none;
    }
  }
  .cc-ent-cap-tile:hover {
    background: rgba(255, 255, 255, 0.03);
  }
  .cc-ent-cap-icon {
    width: 28px;
    height: 28px;
    color: var(--cc-accent);
  }
  .cc-ent-cap-title {
    font-size: 15px;
    font-weight: 500;
    letter-spacing: -0.01em;
    color: var(--cc-ink);
    margin: 0;
  }
  .cc-ent-cap-body {
    font-size: 13px;
    line-height: 1.5;
    color: var(--cc-ink-dim);
    margin: 0;
    text-wrap: pretty;
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
    color: var(--cc-accent);
    font-weight: 500;
  }

  /* ===== 11 Built by the team (typographic moment + roster) ===== */
  .cc-ent-team-inner {
    max-width: 1180px;
    margin: 0 auto;
    text-align: center;
  }
  .cc-ent-team-typo {
    margin: 0 auto 24px;
    display: flex;
    justify-content: center;
    width: 100%;
    color: var(--cc-ink);
  }
  .cc-ent-team-lede {
    max-width: 60ch;
    margin: 0 auto 48px;
    font-size: clamp(15px, 1.2vw, 18px);
    line-height: 1.55;
    color: var(--cc-ink-dim);
    text-wrap: pretty;
  }
  .cc-ent-team-stats {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 0;
    border-top: 1px solid var(--cc-ink-faint);
    border-bottom: 1px solid var(--cc-ink-faint);
    margin: 0 auto 56px;
  }
  @media (max-width: 720px) {
    .cc-ent-team-stats {
      grid-template-columns: 1fr;
    }
  }
  .cc-ent-team-stat {
    display: flex;
    flex-direction: column;
    gap: 6px;
    align-items: center;
    padding: 24px 20px;
    border-right: 1px solid var(--cc-ink-faint);
  }
  .cc-ent-team-stat:last-child {
    border-right: none;
  }
  @media (max-width: 720px) {
    .cc-ent-team-stat {
      border-right: none;
      border-bottom: 1px solid var(--cc-ink-faint);
    }
    .cc-ent-team-stat:last-child {
      border-bottom: none;
    }
  }
  .cc-ent-team-stat-num {
    font-size: clamp(24px, 3vw, 36px);
    font-weight: 500;
    letter-spacing: -0.025em;
    color: var(--cc-ink);
    line-height: 1;
  }
  .cc-ent-team-stat-label {
    font-family: var(--cc-font-mono), monospace;
    font-size: 10px;
    letter-spacing: 0.16em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
  }
  .cc-ent-team-roster {
    display: grid;
    grid-template-columns: 1fr;
    gap: 0;
    text-align: left;
    border-top: 1px solid var(--cc-ink-faint);
  }
  .cc-ent-team-row {
    display: grid;
    grid-template-columns: minmax(0, 0.8fr) minmax(0, 0.6fr) minmax(0, 1fr) minmax(
        0,
        1.6fr
      );
    gap: 24px;
    padding: 22px 0;
    border-bottom: 1px solid var(--cc-ink-faint);
    align-items: baseline;
  }
  @media (max-width: 880px) {
    .cc-ent-team-row {
      grid-template-columns: 1fr;
      gap: 6px;
    }
  }
  .cc-ent-team-name {
    font-size: 17px;
    font-weight: 500;
    color: var(--cc-ink);
    letter-spacing: -0.01em;
  }
  .cc-ent-team-handle {
    font-family: var(--cc-font-mono), monospace;
    font-size: 12px;
    letter-spacing: 0.08em;
    color: var(--cc-accent);
  }
  .cc-ent-team-role {
    font-size: 13px;
    color: var(--cc-ink);
  }
  .cc-ent-team-bio {
    font-size: 13px;
    line-height: 1.5;
    color: var(--cc-ink-dim);
    margin: 0;
    text-wrap: pretty;
  }

  /* ===== 12 Migration (ghost cards) ===== */
  .cc-ent-migration-inner {
    max-width: 1180px;
    margin: 0 auto;
  }
  .cc-ent-migration-grid {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 0;
    border-top: 1px solid var(--cc-ink-faint);
    border-bottom: 1px solid var(--cc-ink-faint);
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
    border-right: 1px solid var(--cc-ink-faint);
  }
  .cc-ent-migration-card:last-child {
    border-right: none;
  }
  @media (max-width: 980px) {
    .cc-ent-migration-card {
      border-right: none;
      border-bottom: 1px solid var(--cc-ink-faint);
    }
    .cc-ent-migration-card:last-child {
      border-bottom: none;
    }
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
    transition: color 0.15s ease;
  }
  .cc-ent-migration-card:hover .cc-ent-migration-cta {
    color: var(--cc-accent);
  }

  /* ===== 13 Inline form + What happens next strip ===== */
  .cc-ent-next-head {
    text-align: center;
    margin: 0 auto 40px;
    max-width: 760px;
  }
  .cc-ent-next-head h2 {
    font-size: clamp(28px, 3.4vw, 40px);
    margin: 8px auto 0;
    max-width: 22ch;
  }
  .cc-ent-next-strip {
    list-style: none;
    padding: 0;
    margin: 0 auto 64px;
    max-width: 1180px;
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 0;
    border-top: 1px solid var(--cc-ink-faint);
    border-bottom: 1px solid var(--cc-ink-faint);
  }
  @media (max-width: 880px) {
    .cc-ent-next-strip {
      grid-template-columns: 1fr;
    }
  }
  .cc-ent-next-step {
    padding: 28px 24px;
    border-right: 1px solid var(--cc-ink-faint);
    display: flex;
    flex-direction: column;
    gap: 10px;
  }
  .cc-ent-next-step:last-child {
    border-right: none;
  }
  @media (max-width: 880px) {
    .cc-ent-next-step {
      border-right: none;
      border-bottom: 1px solid var(--cc-ink-faint);
    }
    .cc-ent-next-step:last-child {
      border-bottom: none;
    }
  }
  .cc-ent-next-num {
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.18em;
    color: var(--cc-accent);
  }
  .cc-ent-next-title {
    font-size: 18px;
    font-weight: 500;
    letter-spacing: -0.015em;
    color: var(--cc-ink);
    margin: 0;
  }
  .cc-ent-next-body {
    font-size: 14px;
    line-height: 1.55;
    color: var(--cc-ink-dim);
    margin: 0;
    text-wrap: pretty;
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
      var(--cc-accent-line, rgba(120, 140, 220, 0.32)) 70%,
      rgba(245, 241, 234, 0.06)
    );
    box-shadow: 0 30px 80px -40px rgba(0, 0, 0, 0.7),
      0 0 60px -10px var(--cc-accent-glow, rgba(120, 140, 220, 0.18));
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
    color: var(--cc-accent);
    flex-shrink: 0;
    margin-top: 3px;
  }
`;
