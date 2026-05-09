"use client";

import React, { FC } from "react";

import {
  PRODUCT_SURFACES,
  ProductSurfaceIcon,
} from "@/data/agents/product-surfaces";

// Section 06: six product surface cards. Each card carries a hand-rolled
// stroke icon (MCP plug, Hot Chocolate cup, Mocha bean, federation mesh,
// Strawberry Shake glass, tracing waterfall). We deliberately avoid lucide /
// generic chrome — these icons live in the same brewer-icon vocabulary as
// Act5 so the page reads as part of the same system.

const STROKE = {
  fill: "none" as const,
  stroke: "currentColor",
  strokeWidth: 1.6,
  strokeLinecap: "round" as const,
  strokeLinejoin: "round" as const,
};

const ICONS: Record<ProductSurfaceIcon, () => React.ReactElement> = {
  // MCP server: plug + socket motif.
  mcp: () => (
    <svg viewBox="0 0 28 28" width="28" height="28" aria-hidden>
      <g {...STROKE}>
        <rect x="4" y="9" width="11" height="10" rx="2" />
        <line x1="15" y1="14" x2="22" y2="14" />
        <line x1="22" y1="11" x2="22" y2="17" />
        <line x1="7" y1="9" x2="7" y2="6" />
        <line x1="12" y1="9" x2="12" y2="6" />
        <circle cx="22" cy="14" r="2" fill="currentColor" stroke="none" />
      </g>
    </svg>
  ),
  // Hot Chocolate: simple cup + steam.
  hotchocolate: () => (
    <svg viewBox="0 0 28 28" width="28" height="28" aria-hidden>
      <g {...STROKE}>
        <path d="M7 11 L7 20 Q7 23 10 23 L18 23 Q21 23 21 20 L21 11 Z" />
        <path d="M21 13 Q24 13 24 16 Q24 19 21 19" />
        <path d="M11 4 Q12 6 11 8" />
        <path d="M14 3 Q15 5 14 7" />
        <path d="M17 4 Q18 6 17 8" />
      </g>
    </svg>
  ),
  // Mocha: bean motif (two halves, slit down the middle).
  mocha: () => (
    <svg viewBox="0 0 28 28" width="28" height="28" aria-hidden>
      <g {...STROKE}>
        <ellipse cx="14" cy="14" rx="8" ry="10" transform="rotate(20 14 14)" />
        <path d="M11 6 Q14 14 11 22" transform="rotate(20 14 14)" />
      </g>
    </svg>
  ),
  // Fusion: triangular mesh with a pinch in the center.
  fusion: () => (
    <svg viewBox="0 0 28 28" width="28" height="28" aria-hidden>
      <g {...STROKE}>
        <circle cx="6" cy="7" r="2" />
        <circle cx="22" cy="7" r="2" />
        <circle cx="6" cy="21" r="2" />
        <circle cx="22" cy="21" r="2" />
        <circle cx="14" cy="14" r="2" fill="currentColor" stroke="none" />
        <line x1="8" y1="8" x2="12" y2="13" />
        <line x1="20" y1="8" x2="16" y2="13" />
        <line x1="8" y1="20" x2="12" y2="15" />
        <line x1="20" y1="20" x2="16" y2="15" />
      </g>
    </svg>
  ),
  // Strawberry Shake: tall milkshake glass + straw.
  shake: () => (
    <svg viewBox="0 0 28 28" width="28" height="28" aria-hidden>
      <g {...STROKE}>
        <path d="M9 7 L19 7 L17 23 Q17 25 14 25 Q11 25 11 23 Z" />
        <line x1="9" y1="11" x2="19" y2="11" />
        <line x1="14" y1="3" x2="14" y2="9" />
        <path d="M14 3 L17 5" />
      </g>
    </svg>
  ),
  // Tracing: stacked waterfall bars.
  tracing: () => (
    <svg viewBox="0 0 28 28" width="28" height="28" aria-hidden>
      <g {...STROKE}>
        <rect x="4" y="6" width="20" height="3" rx="1" />
        <rect x="6" y="11" width="14" height="3" rx="1" />
        <rect x="9" y="16" width="14" height="3" rx="1" />
        <rect x="6" y="21" width="9" height="3" rx="1" />
      </g>
    </svg>
  ),
};

export const ProductSurfaceTiles: FC = () => {
  return (
    <section className="cc-ag-section cc-ag-feature">
      <div className="cc-section-label">
        <span className="num">06</span> Product surfaces
      </div>
      <div className="cc-ag-feature-inner">
        <div className="cc-ag-feature-header">
          <div className="eyebrow">Product surfaces</div>
          <h2 className="display">Six surfaces. One platform.</h2>
          <p>
            Each primitive earns its place in the agent loop. The MCP server is
            the endpoint; the rest are the ChilliCream products you already run,
            instrumented for an audience that isn't human.
          </p>
        </div>

        <div className="cc-ag-products">
          {PRODUCT_SURFACES.map((surface) => {
            const Icon = ICONS[surface.key];
            return (
              <article key={surface.key} className="cc-ag-product">
                <div className="cc-ag-product-icon">
                  <Icon />
                </div>
                <div className="cc-ag-product-tag">{surface.tag}</div>
                <h3>{surface.title}</h3>
                <p>{surface.body}</p>
              </article>
            );
          })}
        </div>
      </div>
    </section>
  );
};
