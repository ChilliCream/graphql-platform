"use client";

import styled from "styled-components";

export const DesktopLandingRoot = styled.div`
  --cc-ink: #f5f1ea;
  --cc-ink-dim: rgba(245, 241, 234, 0.62);
  --cc-ink-faint: rgba(245, 241, 234, 0.16);
  /* Line stroke width — kept thin so the diagram reads cleanly at higher
     zoom levels and on large displays. */
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
  section.cc-act {
    position: relative;
    width: 100%;
    padding-left: var(--cc-pad-x);
    padding-right: var(--cc-pad-x);
    overflow: hidden;
    /* Sections sit above the ConnectorLayer (zIndex:0) so text, circles,
       and the FUSION COMPOSITION glow render in front of the lines. */
    z-index: 1;
  }
  section.cc-act.cc-act-spills {
    overflow: visible;
  }

  .cc-act-label {
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
  .cc-act-label .num {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    padding: 3px 7px;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 4px;
    color: var(--cc-ink);
    line-height: 1;
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

  .cc-btn {
    display: inline-flex;
    align-items: center;
    gap: 12px;
    padding: 16px 26px;
    border-radius: 999px;
    font-family: var(--cc-font-sans), sans-serif;
    font-size: 15px;
    font-weight: 500;
    cursor: pointer;
    border: none;
    transition: transform 0.12s ease, background 0.12s ease,
      border-color 0.12s ease;
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

  /* ===== ACT 1 — Hero ===== */
  .cc-act-hero {
    padding-top: 0;
    padding-bottom: 0;
    display: flex;
    flex-direction: column;
  }
  .cc-hero-canvas-wrap {
    position: relative;
    width: 100%;
    max-width: 1480px;
    aspect-ratio: 1000 / 760;
    margin: 0 auto;
  }
  .cc-hero-canvas {
    position: absolute;
    inset: 0;
    width: 100%;
    height: 100%;
    pointer-events: none;
    z-index: 1;
  }
  .cc-hero-copy {
    position: absolute;
    z-index: 5;
    left: 50%;
    top: 18%;
    transform: translateX(-50%);
    max-width: 70%;
    text-align: center;
  }
  .cc-hero-copy h1 {
    font-size: clamp(40px, 6vw, 88px);
    margin: 16px 0 22px;
    line-height: 1.05;
  }
  .cc-hero-copy h1 .accent {
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
  .cc-hero-copy p {
    font-size: clamp(15px, 1.2vw, 18px);
    line-height: 1.5;
    color: var(--cc-ink-dim);
    max-width: 560px;
    margin: 0 auto 28px;
    text-wrap: pretty;
  }
  .cc-cup-pos-scatter {
    position: absolute;
    transform: translate(-50%, -50%);
    z-index: 3;
    pointer-events: none;
  }
  .cup-wrap {
    position: relative;
  }

  /* ===== Section headline fade ===== */
  .cc-section-headline-fade {
    position: absolute;
  }
  .cc-section-headline-fade::before {
    content: "";
    position: absolute;
    left: 50%;
    top: 50%;
    transform: translate(-50%, -50%);
    width: 1100px;
    height: 150px;
    background: radial-gradient(
      ellipse 600px 110px at 50% 50%,
      rgba(12, 19, 34, 0.96) 0%,
      rgba(12, 19, 34, 0.78) 30%,
      rgba(12, 19, 34, 0.42) 60%,
      transparent 100%
    );
    pointer-events: none;
    z-index: -1;
  }

  /* ===== Section explainer paragraph =====
     Frosted-glass plate to keep the explanatory copy readable when the
     prism beams or service lines pass behind it. Subtle blur preserves the
     visual continuity of the lines while restoring legibility. */
  .cc-explainer {
    position: relative;
    display: inline-block;
    margin: 18px auto 0;
    max-width: 56ch;
    padding: 18px 26px;
    font-family: var(--cc-font-sans), sans-serif;
    font-size: clamp(15px, 1.1vw, 17px);
    line-height: 1.6;
    color: var(--cc-ink);
    text-wrap: pretty;
    border-radius: 14px;
    background: rgba(12, 19, 34, 0.55);
    backdrop-filter: blur(10px) saturate(110%);
    -webkit-backdrop-filter: blur(10px) saturate(110%);
    border: 1px solid rgba(245, 241, 234, 0.08);
    box-shadow: 0 14px 40px -22px rgba(0, 0, 0, 0.6);
  }
  /* Soft halo around the plate so its edge dissolves into the dark
     background instead of cutting hard against the bright lines. */
  .cc-explainer::before {
    content: "";
    position: absolute;
    inset: -28px -52px;
    background: radial-gradient(
      ellipse 70% 90% at 50% 50%,
      rgba(12, 19, 34, 0.55) 0%,
      rgba(12, 19, 34, 0.32) 45%,
      transparent 80%
    );
    border-radius: 28px;
    pointer-events: none;
    z-index: -1;
  }

  /* ===== Build (Act 2) ===== */
  .cc-act-build {
    padding-top: 0;
    padding-bottom: 0;
  }

  .cc-tabbar-h {
    display: flex;
    gap: 4px;
    padding: 4px;
    background: rgba(255, 255, 255, 0.04);
    border: 1px solid var(--cc-ink-faint);
    border-radius: 14px;
    margin-bottom: 18px;
    flex-wrap: wrap;
  }
  .cc-tabbar-h-tab {
    flex: 1;
    min-width: max-content;
    padding: 10px 18px;
    background: transparent;
    border: none;
    border-radius: 10px;
    color: var(--cc-ink-dim);
    font-family: var(--cc-font-sans), sans-serif;
    font-size: 14px;
    font-weight: 500;
    cursor: pointer;
    transition: background 0.15s ease, color 0.15s ease;
    white-space: nowrap;
  }
  .cc-tabbar-h-tab:hover {
    color: var(--cc-ink);
  }
  .cc-tabbar-h-tab.is-active {
    background: rgba(255, 255, 255, 0.1);
    color: var(--cc-ink);
    box-shadow: 0 1px 0 rgba(255, 255, 255, 0.06) inset;
  }

  .cc-tab-panel-d {
    background: rgba(255, 255, 255, 0.025);
    border: 1px solid var(--cc-ink-faint);
    border-radius: 14px;
    padding: 32px 36px;
    min-height: 540px;
    display: flex;
    flex-direction: column;
    animation: cc-d-fadein 0.25s ease;
  }
  @keyframes cc-d-fadein {
    from {
      opacity: 0;
      transform: translateY(4px);
    }
    to {
      opacity: 1;
      transform: translateY(0);
    }
  }
  .cc-tab-grid {
    display: grid;
    grid-template-columns: minmax(0, 1.15fr) minmax(0, 1fr);
    gap: 40px;
    flex: 1;
    align-items: stretch;
  }
  .cc-tab-text {
    min-width: 0;
  }
  .cc-tab-viz {
    border: 1px dashed var(--cc-ink-faint);
    border-radius: 10px;
    background: radial-gradient(
        80% 60% at 50% 0%,
        rgba(255, 255, 255, 0.03),
        transparent 60%
      ),
      rgba(255, 255, 255, 0.015);
    min-height: 240px;
    display: flex;
    align-items: center;
    justify-content: center;
  }
  .cc-tab-viz-label {
    font-family: var(--cc-font-mono), monospace;
    font-size: 10px;
    letter-spacing: 0.22em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
    opacity: 0.6;
  }
  .cc-tab-key {
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.16em;
    color: var(--cc-ink-dim);
    text-transform: uppercase;
    margin-bottom: 6px;
  }
  .cc-tab-title {
    font-size: 26px;
    font-weight: 500;
    letter-spacing: -0.02em;
    margin: 0 0 16px;
  }
  .cc-tab-body {
    font-size: 15px;
    line-height: 1.6;
    color: var(--cc-ink-dim);
    margin: 0 0 14px;
    text-wrap: pretty;
  }
  .cc-tab-body:last-child {
    margin-bottom: 0;
  }
  .cc-tab-footer {
    margin-top: 32px;
    padding-top: 24px;
    border-top: 1px solid var(--cc-ink-faint);
    display: flex;
    flex-direction: column;
    gap: 16px;
  }
  .cc-tab-bullets-d {
    list-style: none;
    padding: 0;
    margin: 0;
    display: flex;
    flex-wrap: wrap;
    gap: 8px;
  }
  .cc-tab-bullets-d li {
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.06em;
    color: var(--cc-ink);
    text-transform: uppercase;
    padding: 6px 10px;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 8px;
    background: rgba(255, 255, 255, 0.02);
  }
  .cc-tab-meta {
    display: flex;
    gap: 22px;
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.14em;
    text-transform: uppercase;
  }
  .cc-tab-meta a {
    color: var(--cc-ink);
    text-decoration: none;
  }
  .cc-tab-meta a:hover {
    color: var(--cc-col-shi);
  }

  @media (max-width: 880px) {
    .cc-tab-grid {
      grid-template-columns: 1fr;
    }
    .cc-tab-viz {
      min-height: 160px;
    }
  }

  .cc-canvas-stripe-label {
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.16em;
    text-transform: uppercase;
    cursor: pointer;
    transition: color 0.15s ease;
    white-space: nowrap;
  }
  .cc-canvas-stripe-label:hover {
    color: var(--cc-ink) !important;
  }

  .cc-pill {
    border-radius: 999px;
    display: flex;
    align-items: center;
    justify-content: center;
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    color: #0a0807;
    font-weight: 500;
  }

  /* ===== Fusion (Act 3) ===== */
  .cc-act-fusion {
    padding-top: 0;
    padding-bottom: 0;
  }

  /* ===== Adapters (Act 4) ===== */
  .cc-act-adapters {
    padding-top: 0;
    padding-bottom: 0;
  }
  .cc-adapter-pill-d {
    border-radius: 12px;
    display: flex;
    align-items: center;
    justify-content: center;
    font-family: var(--cc-font-mono), monospace;
    font-size: 13px;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    color: var(--cc-ink);
    font-weight: 600;
    border: 1.5px solid transparent;
    background-color: transparent;
    background-image: linear-gradient(#0c1322, #0c1322),
      linear-gradient(
        90deg,
        var(--cc-col-ord),
        var(--cc-col-shi),
        var(--cc-col-usr),
        var(--cc-col-cat),
        var(--cc-col-bil)
      );
    background-origin: padding-box, border-box;
    background-clip: padding-box, border-box;
    box-shadow: 0 12px 30px -16px rgba(0, 0, 0, 0.6);
  }

  /* ===== Clients (Act-Clients) ===== */
  .cc-act-clients {
    padding-top: 0;
    padding-bottom: 0;
    position: relative;
  }
  .cc-endpoint-d {
    text-align: center;
  }
  .cc-endpoint-frame-d {
    aspect-ratio: 1/1;
    margin: 0 auto;
    width: 100%;
    max-width: 160px;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 14px;
    display: flex;
    align-items: center;
    justify-content: center;
    background: rgba(255, 255, 255, 0.025);
  }
  .cc-endpoint-name-d {
    margin-top: 18px;
    font-size: 17px;
    font-weight: 500;
    letter-spacing: -0.01em;
  }
  .cc-endpoint-kind-d {
    margin-top: 4px;
    font-family: var(--cc-font-mono), monospace;
    font-size: 10px;
    letter-spacing: 0.14em;
    color: var(--cc-ink-dim);
    text-transform: uppercase;
  }
  .cc-endpoint-protocols-d {
    margin-top: 10px;
    display: flex;
    gap: 6px;
    justify-content: center;
    flex-wrap: wrap;
  }
  .cc-endpoint-protocol-d {
    font-family: var(--cc-font-mono), monospace;
    font-size: 9px;
    letter-spacing: 0.16em;
    text-transform: uppercase;
    padding: 3px 8px;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 999px;
    background: rgba(255, 255, 255, 0.04);
    color: var(--cc-ink-dim);
  }

  /* ===== Brew (Act 5) ===== */
  .cc-act-brew {
    padding-top: 100px;
    padding-bottom: 110px;
  }
  .cc-brew-inner-d {
    max-width: 1280px;
    margin: 0 auto;
  }
  .cc-brew-heading-d {
    text-align: center;
    margin: 0 auto 64px;
    max-width: 720px;
  }
  .cc-brew-heading-d h2 {
    font-size: clamp(36px, 4.4vw, 60px);
    margin: 8px auto 16px;
    max-width: 16ch;
  }
  .cc-brew-heading-d p {
    font-size: clamp(15px, 1.1vw, 17px);
    color: var(--cc-ink-dim);
    max-width: 56ch;
    margin: 0 auto;
    text-wrap: pretty;
  }
  .cc-brew-grid-d {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 24px;
    align-items: stretch;
  }
  @media (max-width: 980px) {
    .cc-brew-grid-d {
      grid-template-columns: 1fr;
      max-width: 480px;
      margin: 0 auto;
    }
  }
  .cc-brew-card-d {
    position: relative;
    display: flex;
    flex-direction: column;
    padding: 40px 32px 32px;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 18px;
    background: rgba(255, 255, 255, 0.025);
    transition: border-color 0.18s ease, transform 0.18s ease;
  }
  .cc-brew-card-d:hover {
    border-color: rgba(245, 241, 234, 0.28);
    transform: translateY(-2px);
  }
  .cc-brew-card-d.is-featured {
    border-color: rgba(245, 241, 234, 0.45);
    background: rgba(255, 255, 255, 0.06);
    box-shadow: 0 0 0 1px rgba(245, 241, 234, 0.1) inset,
      0 30px 80px -40px rgba(0, 0, 0, 0.6);
  }
  .cc-brew-badge-d {
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
  .cc-brew-icon-d {
    width: 140px;
    height: 154px;
    margin: 0 auto 24px;
  }
  .cc-brew-tag-d {
    text-align: center;
    font-family: var(--cc-font-mono), monospace;
    font-size: 10px;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
    margin-bottom: 6px;
  }
  .cc-brew-title-d {
    text-align: center;
    font-size: 28px;
    font-weight: 500;
    letter-spacing: -0.02em;
    margin: 0 0 18px;
    color: var(--cc-ink);
  }
  .cc-brew-price-d {
    text-align: center;
    margin-bottom: 24px;
    padding-bottom: 24px;
    border-bottom: 1px solid var(--cc-ink-faint);
  }
  .cc-brew-price-amount-d {
    display: block;
    font-size: 30px;
    font-weight: 500;
    letter-spacing: -0.02em;
    color: var(--cc-ink);
  }
  .cc-brew-price-note-d {
    display: block;
    margin-top: 4px;
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.08em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
  }
  .cc-brew-copy-d {
    font-size: 15px;
    line-height: 1.55;
    color: var(--cc-ink-dim);
    margin: 0 0 22px;
    text-wrap: pretty;
  }
  .cc-brew-bullets-d {
    list-style: none;
    padding: 0;
    margin: 0 0 28px;
    display: flex;
    flex-direction: column;
    gap: 10px;
    flex: 1;
  }
  .cc-brew-bullets-d li {
    display: flex;
    align-items: center;
    gap: 10px;
    font-size: 14px;
    color: var(--cc-ink);
  }
  .cc-brew-bullets-d li svg {
    color: var(--cc-col-shi);
    flex-shrink: 0;
  }
  .cc-brew-card-d .cc-btn {
    width: 100%;
    justify-content: center;
    padding: 14px 22px;
    font-size: 14px;
  }
  .cc-brew-common-d {
    margin-top: 56px;
    padding-top: 32px;
    border-top: 1px solid var(--cc-ink-faint);
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    justify-content: center;
    gap: 18px;
  }
  .cc-brew-common-label-d {
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.16em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
  }
  .cc-brew-common-list-d {
    list-style: none;
    padding: 0;
    margin: 0;
    display: flex;
    flex-wrap: wrap;
    gap: 8px;
  }
  .cc-brew-common-list-d li {
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.06em;
    text-transform: uppercase;
    color: var(--cc-ink);
    padding: 6px 10px;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 8px;
    background: rgba(255, 255, 255, 0.02);
  }

  /* ===== Final CTA ===== */
  .cc-act-final-cta {
    padding-top: 160px;
    padding-bottom: 140px;
  }
  .cc-final-cta-inner-d {
    max-width: 720px;
    margin: 0 auto;
    text-align: center;
    position: relative;
    z-index: 3;
  }
  .cc-final-cta-inner-d h2 {
    font-size: clamp(48px, 6.4vw, 96px);
    margin: 14px 0 20px;
  }
  .cc-final-cta-inner-d p {
    font-size: clamp(16px, 1.2vw, 19px);
    color: var(--cc-ink-dim);
    margin: 0 auto 32px;
    max-width: 56ch;
    text-wrap: pretty;
  }

  /* ===== Blog ===== */
  .cc-act-blog {
    padding-top: 120px;
    padding-bottom: 100px;
  }
  .cc-blog-inner-d {
    max-width: 1480px;
    margin: 0 auto;
  }
  .cc-blog-heading-d {
    text-align: center;
    margin: 0 auto 56px;
    max-width: 880px;
  }
  .cc-blog-heading-d h2 {
    font-size: clamp(36px, 4.4vw, 60px);
    margin: 8px auto 0;
    max-width: 18ch;
  }
  .cc-blog-grid-d {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 20px;
  }
  @media (max-width: 880px) {
    .cc-blog-grid-d {
      grid-template-columns: 1fr;
    }
  }
  .cc-blog-card-d {
    display: flex;
    flex-direction: column;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 16px;
    overflow: hidden;
    background: rgba(255, 255, 255, 0.025);
    text-decoration: none;
    color: inherit;
    transition: background 0.15s ease, border-color 0.15s ease,
      transform 0.15s ease;
  }
  .cc-blog-card-d:hover {
    background: rgba(255, 255, 255, 0.05);
    border-color: rgba(245, 241, 234, 0.3);
    transform: translateY(-2px);
  }
  .cc-blog-image-d {
    aspect-ratio: 16 / 9;
    background-color: rgba(255, 255, 255, 0.04);
    background-size: cover;
    background-position: center;
    border-bottom: 1px solid var(--cc-ink-faint);
  }
  .cc-blog-body-d {
    display: flex;
    flex-direction: column;
    flex: 1;
    padding: 22px 26px 24px;
  }
  .cc-blog-meta-d {
    display: flex;
    align-items: center;
    gap: 12px;
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.16em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
  }
  .cc-blog-tag-d {
    color: var(--cc-ink);
    padding: 4px 8px;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 6px;
  }
  .cc-blog-title-d {
    font-size: 22px;
    font-weight: 500;
    letter-spacing: -0.015em;
    line-height: 1.25;
    margin: 18px 0 12px;
    color: var(--cc-ink);
  }
  .cc-blog-excerpt-d {
    font-size: 15px;
    line-height: 1.55;
    color: var(--cc-ink-dim);
    margin: 0 0 22px;
    text-wrap: pretty;
  }
  .cc-blog-cta-d {
    margin-top: auto;
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    color: var(--cc-ink);
  }
  .cc-blog-card-d:hover .cc-blog-cta-d {
    color: var(--cc-col-shi);
  }

  .cc-canvas-wrap {
    position: relative;
  }
  .cc-canvas {
    display: block;
  }
  .cc-canvas-wrap svg {
    display: block;
  }
  .cc-act-spills .cc-canvas-wrap,
  .cc-act-spills .cc-canvas-scaler,
  .cc-act-spills .cc-canvas {
    overflow: visible;
  }
  .lazy-placeholder {
    width: 100%;
  }
`;
