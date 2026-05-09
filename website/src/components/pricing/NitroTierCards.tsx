"use client";

import React from "react";

import { TIERS, TierKey } from "@/data/pricing/tiers";

// Brewer icons reused from Act5.tsx — same line-stroke vocabulary so the
// pricing page reads as a sibling of the landing brew section.
const ICONS: Record<TierKey, (color: string, sw: number) => React.ReactNode> = {
  "nitro-free": (color, sw) => (
    <g
      stroke={color}
      strokeWidth={sw}
      fill="none"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <line x1="100" y1="20" x2="100" y2="34" />
      <circle cx="100" cy="20" r="4" />
      <rect x="50" y="34" width="100" height="14" rx="3" />
      <path d="M 56 48 L 56 192 L 144 192 L 144 48" />
      <path d="M 144 80 Q 168 80 168 110 Q 168 140 144 140" />
      <line x1="100" y1="48" x2="100" y2="120" />
      <line x1="68" y1="120" x2="132" y2="120" />
      <line x1="60" y1="160" x2="140" y2="160" opacity="0.4" />
    </g>
  ),
  "nitro-hosted": (color, sw) => (
    <g
      stroke={color}
      strokeWidth={sw}
      fill="none"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <path d="M 36 28 L 164 28 L 164 70 L 132 70 L 132 90 L 68 90 L 68 70 L 36 70 Z" />
      <path d="M 76 90 L 124 90 L 110 132 L 90 132 Z" />
      <path d="M 70 138 L 130 138 L 138 198 L 62 198 Z" />
      <path d="M 130 150 Q 154 150 154 174 Q 154 196 138 196" />
      <line x1="68" y1="172" x2="132" y2="172" opacity="0.4" />
      <line x1="40" y1="200" x2="160" y2="200" />
    </g>
  ),
  "nitro-self-hosted": (color, sw) => (
    <g
      stroke={color}
      strokeWidth={sw}
      fill="none"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <path d="M 56 28 L 144 28 L 116 100 L 84 100 Z" />
      <line x1="76" y1="108" x2="124" y2="108" />
      <line x1="76" y1="118" x2="124" y2="118" />
      <path d="M 84 118 L 60 196 Q 60 204 70 204 L 130 204 Q 140 204 140 196 L 116 118" />
      <line x1="66" y1="178" x2="134" y2="178" opacity="0.4" />
      <path d="M 56 28 Q 64 22 72 28" />
    </g>
  ),
};

const Check: React.FC = () => (
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

export const NitroTierCards: React.FC = () => {
  return (
    <section className="cc-pricing-section cc-tiers">
      <div className="cc-tiers-inner">
        <div className="cc-tiers-heading">
          <div className="eyebrow">Nitro plans</div>
          <h2 className="display">Brew it your way.</h2>
          <p>
            Same engine, same APIs, same DX. Pick the operational shape that
            fits your team. Move between them without re-architecting.
          </p>
        </div>

        <div className="cc-tiers-grid">
          {TIERS.map((tier) => (
            <article
              key={tier.key}
              className={"cc-tier-card" + (tier.featured ? " is-featured" : "")}
            >
              {tier.badge && <div className="cc-tier-badge">{tier.badge}</div>}

              <div className="cc-tier-icon">
                <svg
                  viewBox="0 0 200 220"
                  width="100%"
                  height="100%"
                  aria-hidden
                >
                  {ICONS[tier.key]("var(--cc-ink)", 1.6)}
                </svg>
              </div>

              <div className="cc-tier-brewer">{tier.brewer}</div>
              <h3 className="cc-tier-name">{tier.name}</h3>
              <p className="cc-tier-tagline">{tier.tagline}</p>

              <div className="cc-tier-price">
                <span className="cc-tier-price-amount">{tier.price}</span>
                <span className="cc-tier-price-note">{tier.priceNote}</span>
              </div>

              <ul className="cc-tier-bullets">
                {tier.bullets.map((b) => (
                  <li key={b}>
                    <Check />
                    <span>{b}</span>
                  </li>
                ))}
              </ul>

              <a
                href={tier.ctaHref}
                className={
                  "cc-btn " +
                  (tier.featured ? "cc-btn-primary" : "cc-btn-ghost")
                }
              >
                {tier.cta} →
              </a>
            </article>
          ))}
        </div>
      </div>
    </section>
  );
};
