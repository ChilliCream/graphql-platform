"use client";

import React, { FC } from "react";

import { SKUS, SkuKey } from "@/data/enterprise/skus";

const Check: FC = () => (
  <svg viewBox="0 0 16 16" width="14" height="14" aria-hidden>
    <path
      d="M3 8.5 L6.5 12 L13 4.5"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
    />
  </svg>
);

// Brewer-style stroke icons (sw=1.6) for the three named SKUs. Same drawing
// vocabulary as Act5 / NitroTierCards so the cards read as siblings.
const SKU_ICONS: Record<SkuKey, React.ReactNode> = {
  "fusion-mesh": (
    <svg viewBox="0 0 200 220" width="100%" height="100%" aria-hidden>
      <g
        stroke="var(--cc-ink)"
        strokeWidth={1.6}
        fill="none"
        strokeLinecap="round"
        strokeLinejoin="round"
      >
        <circle cx="100" cy="50" r="14" />
        <circle cx="40" cy="120" r="14" />
        <circle cx="160" cy="120" r="14" />
        <circle cx="70" cy="180" r="14" />
        <circle cx="130" cy="180" r="14" />
        <circle cx="100" cy="120" r="22" />
        <line x1="100" y1="64" x2="100" y2="98" />
        <line x1="54" y1="120" x2="78" y2="120" />
        <line x1="146" y1="120" x2="122" y2="120" />
        <line x1="80" y1="166" x2="92" y2="138" />
        <line x1="120" y1="166" x2="108" y2="138" />
      </g>
    </svg>
  ),
  "nitro-control-plane": (
    <svg viewBox="0 0 200 220" width="100%" height="100%" aria-hidden>
      <g
        stroke="var(--cc-ink)"
        strokeWidth={1.6}
        fill="none"
        strokeLinecap="round"
        strokeLinejoin="round"
      >
        <rect x="40" y="40" width="120" height="38" rx="6" />
        <rect x="40" y="92" width="120" height="38" rx="6" />
        <rect x="40" y="144" width="120" height="38" rx="6" />
        <circle cx="58" cy="59" r="3" />
        <circle cx="58" cy="111" r="3" />
        <circle cx="58" cy="163" r="3" />
        <line x1="78" y1="59" x2="142" y2="59" opacity="0.5" />
        <line x1="78" y1="111" x2="142" y2="111" opacity="0.5" />
        <line x1="78" y1="163" x2="142" y2="163" opacity="0.5" />
      </g>
    </svg>
  ),
  "agent-bridge": (
    <svg viewBox="0 0 200 220" width="100%" height="100%" aria-hidden>
      <g
        stroke="var(--cc-ink)"
        strokeWidth={1.6}
        fill="none"
        strokeLinecap="round"
        strokeLinejoin="round"
      >
        <rect x="50" y="60" width="100" height="80" rx="14" />
        <circle cx="80" cy="100" r="6" />
        <circle cx="120" cy="100" r="6" />
        <line x1="100" y1="30" x2="100" y2="60" />
        <circle cx="100" cy="28" r="4" />
        <line x1="78" y1="140" x2="78" y2="170" />
        <line x1="122" y1="140" x2="122" y2="170" />
        <path d="M 50 175 Q 50 195 70 195 L 130 195 Q 150 195 150 175" />
      </g>
    </svg>
  ),
};

export const EnterpriseSkuCards: FC = () => {
  return (
    <section className="cc-ent-section cc-ent-skus">
      <div className="cc-section-label">
        <span className="num">08</span> Enterprise SKUs
      </div>
      <div className="cc-ent-skus-inner">
        <div className="cc-ent-heading">
          <div className="eyebrow">Three named capabilities</div>
          <h2 className="display">
            Three SKUs your bake-off matrix will recognise.
          </h2>
          <p>
            One noun per capability. Memorable, namespace-able, and easy to put
            on a vendor selection sheet next to whatever else you're evaluating.
          </p>
        </div>
        <div className="cc-ent-skus-grid">
          {SKUS.map((sku) => (
            <article key={sku.key} className="cc-ent-sku-card">
              <div className="cc-ent-sku-icon">{SKU_ICONS[sku.key]}</div>
              <h3 className="cc-ent-sku-name">{sku.name}</h3>
              <p className="cc-ent-sku-tagline">{sku.tagline}</p>
              <ul className="cc-ent-sku-bullets">
                {sku.bullets.map((b) => (
                  <li key={b}>
                    <Check />
                    <span>{b}</span>
                  </li>
                ))}
              </ul>
              <a href={sku.docsHref} className="cc-ent-sku-link">
                {sku.docsLabel} →
              </a>
            </article>
          ))}
        </div>
      </div>
    </section>
  );
};
