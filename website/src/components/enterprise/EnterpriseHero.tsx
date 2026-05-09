"use client";

import React, { FC } from "react";

// 5-monogram "trust strip". We can't name customers, so each tile is a
// stroke-rendered single-letter monogram with a one-line caption underneath
// describing the segment. Same line-stroke vocabulary as Act5's brewers.

interface MonogramTile {
  readonly key: string;
  readonly letter: string;
  readonly caption: string;
}

const MONOGRAMS: readonly MonogramTile[] = [
  { key: "eu-bank", letter: "B", caption: "EU retail bank" },
  { key: "logistics", letter: "L", caption: "Logistics PaaS" },
  { key: "fsi", letter: "F", caption: "FSI group" },
  { key: "public", letter: "P", caption: "Public-sector cloud" },
  { key: "insurer", letter: "I", caption: "Global insurer" },
];

const Monogram: FC<{ letter: string }> = ({ letter }) => (
  <svg viewBox="0 0 56 56" width="56" height="56" aria-hidden>
    <g
      stroke="currentColor"
      strokeWidth="1.6"
      fill="none"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <circle cx="28" cy="28" r="22" opacity="0.6" />
      <text
        x="28"
        y="34"
        textAnchor="middle"
        fontFamily="var(--cc-font-sans), sans-serif"
        fontWeight={500}
        fontSize="22"
        stroke="none"
        fill="currentColor"
      >
        {letter}
      </text>
    </g>
  </svg>
);

interface EnterpriseHeroProps {
  readonly onPrimaryClick: () => void;
}

export const EnterpriseHero: FC<EnterpriseHeroProps> = ({ onPrimaryClick }) => {
  return (
    <section className="cc-ent-section cc-ent-hero">
      <div className="cc-section-label">
        <span className="num">02</span> Enterprise
      </div>
      <div className="cc-ent-hero-inner">
        <div className="eyebrow">For platform teams</div>
        <h1 className="display">
          The GraphQL platform for{" "}
          <span className="accent">enterprise platform teams.</span>
        </h1>
        <p>
          Hot Chocolate, Fusion, and Nitro give your platform team one stack to
          compose every backend you have, in any language, on infrastructure you
          control. Self-hosted, air-gapped, agent-ready, and supported by the
          engineers who built it.
        </p>
        <div className="cc-cta-row">
          <button
            type="button"
            onClick={onPrimaryClick}
            className="cc-btn cc-btn-primary"
          >
            Get a Nitro demo →
          </button>
          <a href="/" className="cc-btn cc-btn-ghost">
            Explore the platform
          </a>
        </div>

        <div className="cc-ent-trust-strip" aria-label="Customer segments">
          {MONOGRAMS.map((m) => (
            <div key={m.key} className="cc-ent-trust-tile">
              <div className="cc-ent-trust-mono">
                <Monogram letter={m.letter} />
              </div>
              <div className="cc-ent-trust-caption">{m.caption}</div>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
};
