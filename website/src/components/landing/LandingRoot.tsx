"use client";

import React from "react";
import styled, { createGlobalStyle } from "styled-components";

// TODO: When porting the desktop variant, branch on viewport here. For now we
// constrain the mobile experience to a phone-shaped column on large screens.

export const LandingGlobalStyle = createGlobalStyle`
  body.cc-landing-body {
    margin: 0;
    padding: 0;
    background: #0b0f1a;
    color: var(--cc-ink);
    font-family: var(--cc-font-sans), system-ui, sans-serif;
    -webkit-font-smoothing: antialiased;
    overflow-x: hidden;
  }
  body.cc-landing-body * { box-sizing: border-box; }
`;

export const LandingRoot = styled.div`
  --cc-ink: #f5f1ea;
  --cc-ink-dim: rgba(245, 241, 234, 0.62);
  --cc-ink-faint: rgba(245, 241, 234, 0.16);
  --cc-line-w: 2px;
  --cc-col-cat: oklch(0.74 0.18 30);
  --cc-col-bil: oklch(0.82 0.16 90);
  --cc-col-ord: oklch(0.76 0.16 150);
  --cc-col-shi: oklch(0.74 0.14 220);
  --cc-col-usr: oklch(0.72 0.18 310);
  --cc-pad-x: clamp(16px, 5vw, 22px);

  position: relative;
  width: 100%;
  margin: 0 auto;
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
  color: var(--cc-ink);
  font-family: var(--cc-font-sans), system-ui, sans-serif;

  @media (min-width: 700px) {
    max-width: 480px;
    box-shadow: 0 0 80px rgba(0, 0, 0, 0.6);
  }

  /* ===== Section shell ===== */
  section.act {
    position: relative;
    width: 100%;
    padding: 56px var(--cc-pad-x);
    overflow: hidden;
  }
  section.act.act-hero {
    padding: 88px 0 0;
  }
  section.act.final-cta {
    padding: 88px var(--cc-pad-x);
  }
  section.act.act-spills {
    overflow: visible;
  }

  .act-label {
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.18em;
    color: var(--cc-ink-dim);
    text-transform: uppercase;
    margin-bottom: 14px;
  }
  .act-label .num {
    display: inline-block;
    padding: 3px 7px;
    margin-right: 10px;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 4px;
    color: var(--cc-ink);
  }

  .display {
    font-family: var(--cc-font-sans), sans-serif;
    font-weight: 500;
    letter-spacing: -0.035em;
    line-height: 0.98;
  }
  .eyebrow {
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.18em;
    color: var(--cc-ink-dim);
    text-transform: uppercase;
  }

  .btn {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    gap: 12px;
    padding: 16px 26px;
    border-radius: 999px;
    font-family: var(--cc-font-sans), sans-serif;
    font-size: 16px;
    font-weight: 500;
    cursor: pointer;
    border: none;
    min-height: 56px;
    transition: transform 0.12s ease, background 0.12s ease,
      border-color 0.12s ease;
  }
  .btn-primary {
    background: var(--cc-ink);
    color: #0c1322;
  }
  .btn-primary:hover {
    transform: translateY(-1px);
  }
  .btn-ghost {
    background: transparent;
    color: var(--cc-ink);
    border: 1px solid var(--cc-ink-faint);
  }
  .btn-ghost:hover {
    border-color: var(--cc-ink);
  }

  /* ===== Hero ===== */
  .hero-wrap {
    position: relative;
    width: 100%;
    aspect-ratio: 360 / 640;
    max-width: 100vw;
    overflow: hidden;
  }
  .hero-canvas {
    position: absolute;
    inset: 0;
    width: 100%;
    height: 100%;
    pointer-events: none;
    z-index: 1;
  }
  .hero-copy {
    position: absolute;
    left: 50%;
    top: 30%;
    transform: translateX(-50%);
    width: calc(100% - 32px);
    max-width: calc(100% - 32px);
    text-align: center;
    z-index: 5;
  }
  .hero-copy h1 {
    font-size: clamp(40px, 11vw, 56px);
    margin: 14px 0 18px;
    line-height: 1.05;
  }
  .hero-copy h1 .accent {
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
  .hero-copy p {
    font-size: 16px;
    line-height: 1.5;
    color: var(--cc-ink-dim);
    margin: 0 auto 22px;
    text-wrap: pretty;
  }
  .hero-copy .cta-stack {
    display: flex;
    flex-direction: column;
    gap: 12px;
    width: 100%;
    align-items: stretch;
  }
  .hero-copy .cta-stack .btn {
    width: 100%;
  }
  .cup-pos-scatter {
    position: absolute;
    transform: translate(-50%, -50%);
    width: 44px;
    height: 44px;
    z-index: 3;
    pointer-events: none;
  }
  .cup-wrap {
    position: relative;
  }

  /* ===== Section headline fade ===== */
  .section-headline-fade {
    position: relative;
  }
  .section-headline-fade::before {
    content: "";
    position: absolute;
    left: 50%;
    top: 50%;
    transform: translate(-50%, -50%);
    width: min(96vw, 380px);
    height: 70px;
    background: radial-gradient(
      ellipse 200px 50px at 50% 50%,
      rgba(12, 19, 34, 0.96) 0%,
      rgba(12, 19, 34, 0.78) 30%,
      rgba(12, 19, 34, 0.42) 60%,
      transparent 100%
    );
    pointer-events: none;
    z-index: -1;
  }

  .act-heading {
    text-align: center;
    margin: 0 auto 24px;
  }
  .act-heading h2 {
    font-size: clamp(28px, 7.5vw, 38px);
    margin: 8px auto 0;
    max-width: 16ch;
  }

  /* ===== Tabbar (snap) ===== */
  .tabbar {
    display: flex;
    gap: 0;
    overflow-x: auto;
    -webkit-overflow-scrolling: touch;
    scroll-snap-type: x mandatory;
    border-bottom: 1px solid var(--cc-ink-faint);
    margin-bottom: 14px;
    position: relative;
    scrollbar-width: none;
  }
  .tabbar::-webkit-scrollbar {
    display: none;
  }
  .tabbar-tab {
    flex: 0 0 auto;
    scroll-snap-align: center;
    padding: 12px 14px;
    background: transparent;
    border: 0;
    border-bottom: 2px solid transparent;
    color: var(--cc-ink-dim);
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.16em;
    text-transform: uppercase;
    cursor: pointer;
    min-height: 44px;
    margin-bottom: -1px;
    white-space: nowrap;
    transition: color 0.15s ease, border-color 0.15s ease;
  }
  .tabbar-tab.is-active {
    color: var(--cc-ink);
    border-bottom-color: var(--cc-ink);
  }
  .tabbar-wrap {
    position: relative;
  }
  .tabbar-fade {
    position: absolute;
    top: 0;
    bottom: 0;
    right: 0;
    width: 32px;
    background: linear-gradient(90deg, transparent, #0c1322 80%);
    pointer-events: none;
    transition: opacity 200ms ease;
  }
  .tabbar-chevron {
    position: absolute;
    top: 50%;
    right: 6px;
    transform: translateY(-50%);
    color: var(--cc-ink-dim);
    font-size: 14px;
    pointer-events: none;
    transition: opacity 200ms ease;
  }

  /* ===== Tab panel ===== */
  .tab-panel {
    background: rgba(255, 255, 255, 0.025);
    border: 1px solid var(--cc-ink-faint);
    border-radius: 12px;
    padding: 22px 18px;
    animation: cc-fadein 0.25s ease;
  }
  @keyframes cc-fadein {
    from {
      opacity: 0;
      transform: translateY(4px);
    }
    to {
      opacity: 1;
      transform: translateY(0);
    }
  }
  @media (prefers-reduced-motion: reduce) {
    .tab-panel {
      animation: none;
    }
    .cc-drops {
      display: none;
    }
  }
  .tab-panel .tab-key {
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.16em;
    color: var(--cc-ink-dim);
    text-transform: uppercase;
    margin-bottom: 6px;
  }
  .tab-panel .tab-title {
    font-size: 20px;
    font-weight: 500;
    letter-spacing: -0.02em;
    margin: 0 0 12px;
  }
  .tab-panel .tab-body {
    font-size: 16px;
    line-height: 1.55;
    color: var(--cc-ink-dim);
    margin: 0 0 16px;
    text-wrap: pretty;
  }
  .tab-panel .tab-bullets {
    list-style: none;
    padding: 0;
    margin: 0 0 16px;
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 6px;
  }
  .tab-panel .tab-bullets li {
    font-family: var(--cc-font-mono), monospace;
    font-size: 10px;
    letter-spacing: 0.06em;
    color: var(--cc-ink);
    text-transform: uppercase;
    padding: 6px 10px;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 8px;
    background: rgba(255, 255, 255, 0.02);
    text-align: center;
  }
  .tab-panel .tab-meta {
    display: flex;
    flex-direction: column;
    gap: 4px;
    font-family: var(--cc-font-mono), monospace;
    font-size: 12px;
    letter-spacing: 0.14em;
    text-transform: uppercase;
    margin-top: 14px;
    padding-top: 14px;
    border-top: 1px solid var(--cc-ink-faint);
  }
  .tab-panel .tab-meta a {
    color: var(--cc-ink);
    text-decoration: none;
    padding: 12px 0;
    min-height: 44px;
    display: flex;
    align-items: center;
  }

  /* ===== Adapters ===== */
  .adapter-stack {
    display: flex;
    flex-direction: column;
    gap: 12px;
    margin-top: 16px;
  }
  .adapter-cell {
    display: flex;
    flex-direction: column;
    gap: 6px;
  }
  .adapter-caption {
    font-family: var(--cc-font-mono), monospace;
    font-size: 10px;
    letter-spacing: 0.14em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
    text-align: center;
  }
  .adapter-pill {
    height: 64px;
    border-radius: 10px;
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
  .adapter-fanout {
    display: block;
    margin: 8px auto 0;
  }

  /* ===== Clients (2x2 grid) ===== */
  .clients-grid {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 16px;
    margin-top: 16px;
  }
  .endpoint {
    text-align: center;
  }
  .endpoint .frame {
    aspect-ratio: 1/1;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 12px;
    display: flex;
    align-items: center;
    justify-content: center;
    background: rgba(255, 255, 255, 0.025);
  }
  .endpoint .name {
    margin-top: 12px;
    font-size: 17px;
    font-weight: 500;
    letter-spacing: -0.01em;
  }
  .endpoint .kind {
    margin-top: 4px;
    font-family: var(--cc-font-mono), monospace;
    font-size: 10px;
    letter-spacing: 0.14em;
    color: var(--cc-ink-dim);
    text-transform: uppercase;
  }
  .endpoint-protocols {
    margin-top: 8px;
    display: flex;
    gap: 6px;
    justify-content: center;
    flex-wrap: wrap;
  }
  .endpoint-protocol {
    font-family: var(--cc-font-mono), monospace;
    font-size: 9px;
    letter-spacing: 0.16em;
    text-transform: uppercase;
    padding: 3px 6px;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 999px;
    background: rgba(255, 255, 255, 0.04);
    color: var(--cc-ink-dim);
  }

  /* ===== Brew cards ===== */
  .brew-grid {
    display: flex;
    flex-direction: column;
    gap: 16px;
    margin-top: 16px;
  }
  .brew-card {
    position: relative;
    display: flex;
    flex-direction: column;
    padding: 28px 22px;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 16px;
    background: rgba(255, 255, 255, 0.025);
  }
  .brew-card.is-featured {
    border-color: rgba(245, 241, 234, 0.45);
    background: rgba(255, 255, 255, 0.06);
  }
  .brew-badge-inline {
    display: inline-flex;
    align-self: center;
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
    margin-bottom: 14px;
  }
  .brew-icon {
    width: 140px;
    height: 154px;
    margin: 0 auto 18px;
  }
  .brew-tag {
    text-align: center;
    font-family: var(--cc-font-mono), monospace;
    font-size: 10px;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
    margin-bottom: 6px;
  }
  .brew-title {
    text-align: center;
    font-size: 24px;
    font-weight: 500;
    letter-spacing: -0.02em;
    margin: 0 0 14px;
  }
  .brew-price {
    text-align: center;
    margin-bottom: 18px;
    padding-bottom: 18px;
    border-bottom: 1px solid var(--cc-ink-faint);
  }
  .brew-price-amount {
    display: block;
    font-size: 26px;
    font-weight: 500;
    letter-spacing: -0.02em;
  }
  .brew-price-note {
    display: block;
    margin-top: 4px;
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.08em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
  }
  .brew-copy {
    font-size: 15px;
    line-height: 1.55;
    color: var(--cc-ink-dim);
    margin: 0 0 18px;
    text-wrap: pretty;
  }
  .brew-bullets {
    list-style: none;
    padding: 0;
    margin: 0 0 22px;
    display: flex;
    flex-direction: column;
    gap: 10px;
  }
  .brew-bullets li {
    display: flex;
    align-items: center;
    gap: 10px;
    font-size: 14px;
  }
  .brew-bullets li svg {
    color: var(--cc-col-shi);
    flex-shrink: 0;
  }
  .brew-card .btn {
    width: 100%;
    padding: 14px 22px;
    font-size: 15px;
    min-height: 52px;
  }
  .brew-common {
    margin-top: 28px;
    padding-top: 22px;
    border-top: 1px solid var(--cc-ink-faint);
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 10px;
  }
  .brew-common-label {
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.16em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
  }
  .brew-common-list {
    list-style: none;
    padding: 0;
    margin: 0;
    display: flex;
    flex-wrap: wrap;
    gap: 6px;
    justify-content: center;
  }
  .brew-common-list li {
    font-family: var(--cc-font-mono), monospace;
    font-size: 10px;
    letter-spacing: 0.06em;
    text-transform: uppercase;
    color: var(--cc-ink);
    padding: 6px 10px;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 8px;
    background: rgba(255, 255, 255, 0.02);
  }

  /* ===== Final CTA ===== */
  .final-cta-inner {
    text-align: center;
  }
  .final-cta-inner h2 {
    font-size: clamp(38px, 10vw, 56px);
    margin: 14px 0 16px;
  }
  .final-cta-inner p {
    font-size: 16px;
    color: var(--cc-ink-dim);
    margin: 0 auto 24px;
    text-wrap: pretty;
  }
  .final-cta-inner .cta-stack {
    display: flex;
    flex-direction: column;
    gap: 12px;
  }
  .final-cta-inner .cta-stack .btn {
    width: 100%;
  }

  /* ===== Blog ===== */
  .blog-grid {
    display: flex;
    flex-direction: column;
    gap: 16px;
    margin-top: 16px;
  }
  .blog-card {
    display: flex;
    flex-direction: column;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 14px;
    overflow: hidden;
    background: rgba(255, 255, 255, 0.025);
    text-decoration: none;
    color: inherit;
  }
  .blog-image {
    aspect-ratio: 16 / 9;
    width: 100%;
    background-color: rgba(255, 255, 255, 0.04);
    object-fit: cover;
    display: block;
    border-bottom: 1px solid var(--cc-ink-faint);
  }
  .blog-body {
    display: flex;
    flex-direction: column;
    padding: 22px 20px;
  }
  .blog-meta {
    display: flex;
    align-items: center;
    gap: 10px;
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.16em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
  }
  .blog-tag {
    color: var(--cc-ink);
    padding: 4px 8px;
    border: 1px solid var(--cc-ink-faint);
    border-radius: 6px;
  }
  .blog-title {
    font-size: 18px;
    font-weight: 500;
    letter-spacing: -0.015em;
    line-height: 1.25;
    margin: 14px 0 10px;
    color: var(--cc-ink);
  }
  .blog-excerpt {
    font-size: 15px;
    line-height: 1.55;
    color: var(--cc-ink-dim);
    margin: 0 0 18px;
    text-wrap: pretty;
  }
  .blog-cta {
    font-family: var(--cc-font-mono), monospace;
    font-size: 12px;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    color: var(--cc-ink);
  }

  /* ===== Footer ===== */
  footer.foot {
    border-top: 1px solid var(--cc-ink-faint);
    padding: 40px var(--cc-pad-x) 60px;
    display: flex;
    flex-direction: column;
    gap: 28px;
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.14em;
    color: var(--cc-ink-dim);
    text-transform: uppercase;
  }
  footer.foot .col h4 {
    color: var(--cc-ink);
    margin: 0 0 12px;
    font-weight: 500;
  }
  footer.foot .col a {
    display: block;
    color: inherit;
    text-decoration: none;
    padding: 6px 0;
    min-height: 32px;
  }

  .lazy-placeholder {
    width: 100%;
  }

  .act-bundle {
    display: block;
    margin: 0 auto;
  }
`;

export const SHARED_PRODUCTS = [
  { key: "hot-chocolate", label: "Hot Chocolate" },
  { key: "nitro", label: "Nitro" },
  { key: "mocha", label: "Mocha" },
  { key: "strawberry-shake", label: "Strawberry Shake" },
] as const;

export const SHARED_SERVICES = [
  { key: "catalog", label: "Catalog", color: "var(--cc-col-cat)" },
  { key: "billing", label: "Billing", color: "var(--cc-col-bil)" },
  { key: "ordering", label: "Ordering", color: "var(--cc-col-ord)" },
  { key: "shipping", label: "Shipping", color: "var(--cc-col-shi)" },
  { key: "users", label: "Users", color: "var(--cc-col-usr)" },
] as const;
