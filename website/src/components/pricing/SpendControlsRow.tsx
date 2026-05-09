"use client";

import React from "react";

interface Tile {
  readonly key: string;
  readonly title: string;
  readonly body: string;
  readonly icon: React.ReactNode;
}

const stroke = {
  fill: "none" as const,
  stroke: "currentColor",
  strokeWidth: 1.6,
  strokeLinecap: "round" as const,
  strokeLinejoin: "round" as const,
};

const TILES: readonly Tile[] = [
  {
    key: "hard-limits",
    title: "Hard limits",
    body: "Every Nitro plan caps usage by default. Traffic is throttled at the cap, never silently overcharged.",
    icon: (
      <svg viewBox="0 0 24 24" width="20" height="20" aria-hidden {...stroke}>
        <rect x="4" y="9" width="16" height="11" rx="2" />
        <path d="M8 9 V7 a4 4 0 0 1 8 0 V9" />
        <line x1="12" y1="13" x2="12" y2="16" />
      </svg>
    ),
  },
  {
    key: "budget-alerts",
    title: "Budget alerts",
    body: "Set a monthly budget. We notify you at 50/80/100% of spend, before the invoice lands.",
    icon: (
      <svg viewBox="0 0 24 24" width="20" height="20" aria-hidden {...stroke}>
        <path d="M6 19 a6 6 0 1 1 12 0 z" />
        <path d="M12 7 V4" />
        <path d="M10 4 h4" />
        <path d="M3 19 h18" />
      </svg>
    ),
  },
  {
    key: "no-surprises",
    title: "No surprise invoices",
    body: "Per-unit prices live in the comparison table, not in a footnote. Pay-as-you-go is opt-in.",
    icon: (
      <svg viewBox="0 0 24 24" width="20" height="20" aria-hidden {...stroke}>
        <path d="M5 5 h10 l4 4 v10 a1 1 0 0 1 -1 1 H5 a1 1 0 0 1 -1 -1 V6 a1 1 0 0 1 1 -1 z" />
        <path d="M14 5 v5 h5" />
        <path d="M8 14 h8" />
        <path d="M8 17 h6" />
      </svg>
    ),
  },
];

export const SpendControlsRow: React.FC = () => {
  return (
    <section className="cc-pricing-section cc-spend-controls">
      <div className="cc-spend-inner">
        <div className="cc-spend-heading">
          <div className="eyebrow">Spend controls</div>
          <p>Hard limits. Budget alerts. No surprise invoices.</p>
        </div>
        <div className="cc-spend-grid">
          {TILES.map((tile) => (
            <div key={tile.key} className="cc-spend-tile">
              <div className="cc-spend-tile-icon">{tile.icon}</div>
              <h3 className="cc-spend-tile-title">{tile.title}</h3>
              <p className="cc-spend-tile-body">{tile.body}</p>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
};
