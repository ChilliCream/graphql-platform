"use client";

import React, { FC } from "react";

import { OTEL_BACKENDS } from "@/data/observability/otel-backends";

// Six monogram tiles used inside the OTEL & integrations panel (Section 07).
// Each tile renders a stroke-rendered single-letter monogram and the backend
// name underneath in monospace, matching the same vocabulary used by Act5
// brewers and the EnterpriseHero customer-segment monograms. We deliberately
// do NOT ship real brand assets, this is a logo-strip placeholder.

const Monogram: FC<{ letter: string }> = ({ letter }) => (
  <svg viewBox="0 0 44 44" width="44" height="44" aria-hidden>
    <g
      stroke="currentColor"
      strokeWidth="1.4"
      fill="none"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <circle cx="22" cy="22" r="17" opacity="0.55" />
      <text
        x="22"
        y="27"
        textAnchor="middle"
        fontFamily="var(--cc-font-sans), sans-serif"
        fontWeight={500}
        fontSize="17"
        stroke="none"
        fill="currentColor"
      >
        {letter}
      </text>
    </g>
  </svg>
);

export const OtelLogoStrip: FC = () => {
  return (
    <div className="cc-otel-strip" aria-label="OTEL-compatible backends">
      {OTEL_BACKENDS.map((b) => (
        <div key={b.key} className="cc-otel-tile">
          <div className="cc-otel-mono">
            <Monogram letter={b.letter} />
          </div>
          <div className="cc-otel-name">{b.name}</div>
        </div>
      ))}
    </div>
  );
};

// Tiny 0–500ms timeline shown above the logo strip in Section 07. It's a
// schematic marker bar, not a real measurement.
export const OtelTimeline: FC = () => (
  <svg
    viewBox="0 0 720 80"
    className="cc-otel-timeline"
    preserveAspectRatio="xMidYMid meet"
    aria-hidden
  >
    {/* axis line */}
    <line
      x1="20"
      y1="58"
      x2="700"
      y2="58"
      stroke="var(--cc-ink-faint)"
      strokeWidth="1.2"
    />
    {/* axis ticks */}
    {[0, 100, 200, 300, 400, 500].map((ms, i) => {
      const x = 20 + (680 * i) / 5;
      return (
        <g key={ms}>
          <line
            x1={x}
            y1="54"
            x2={x}
            y2="62"
            stroke="var(--cc-ink-faint)"
            strokeWidth="1.2"
          />
          <text
            x={x}
            y="74"
            textAnchor="middle"
            fontFamily="var(--cc-font-mono), monospace"
            fontSize="10"
            letterSpacing="0.08em"
            fill="var(--cc-ink-dim)"
          >
            {ms}ms
          </text>
        </g>
      );
    })}
    {/* marker dots for spans flowing into the strip */}
    {[
      { x: 80, c: "var(--cc-col-cat)", label: "Catalog" },
      { x: 180, c: "var(--cc-col-bil)", label: "Billing" },
      { x: 290, c: "var(--cc-col-ord)", label: "Ordering" },
      { x: 410, c: "var(--cc-col-shi)", label: "Shipping" },
      { x: 540, c: "var(--cc-col-usr)", label: "Users" },
    ].map((m) => (
      <g key={m.label}>
        <circle cx={m.x} cy="58" r="4" fill={m.c} />
        <line
          x1={m.x}
          y1="54"
          x2={m.x}
          y2="30"
          stroke={m.c}
          strokeWidth="1"
          opacity="0.5"
        />
        <text
          x={m.x}
          y="22"
          textAnchor="middle"
          fontFamily="var(--cc-font-mono), monospace"
          fontSize="10"
          letterSpacing="0.08em"
          fill="var(--cc-ink-dim)"
        >
          {m.label}
        </text>
      </g>
    ))}
  </svg>
);
