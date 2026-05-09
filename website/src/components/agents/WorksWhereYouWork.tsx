"use client";

import React, { FC } from "react";

import { IDE_CLIENTS } from "@/data/agents/ide-clients";

// Section 08: four monogram tiles for the IDE / chat clients that consume
// the Nitro MCP server. We reuse the OtelLogoStrip vocabulary (single-letter
// stroke monogram + name) so the page reads as part of the same visual
// system as /products/nitro/observability. Each tile carries an `Add MCP →`
// link (currently anchored to a marketing setup page).

const Monogram: FC<{ letter: string }> = ({ letter }) => (
  <svg viewBox="0 0 48 48" width="48" height="48" aria-hidden>
    <g
      stroke="currentColor"
      strokeWidth="1.4"
      fill="none"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <circle cx="24" cy="24" r="18" opacity="0.55" />
      <text
        x="24"
        y="30"
        textAnchor="middle"
        fontFamily="var(--cc-font-sans), sans-serif"
        fontWeight={500}
        fontSize="19"
        stroke="none"
        fill="currentColor"
      >
        {letter}
      </text>
    </g>
  </svg>
);

export const WorksWhereYouWork: FC = () => {
  return (
    <section className="cc-ag-section cc-ag-feature">
      <div className="cc-section-label">
        <span className="num">08</span> Distribution
      </div>
      <div className="cc-ag-feature-inner">
        <div className="cc-ag-feature-header">
          <div className="eyebrow">Distribution</div>
          <h2 className="display">It's not another chat window.</h2>
          <p>
            It's the same chat window, suddenly aware of your platform. The
            Nitro MCP server slots into the agent clients your team already
            uses, with one config line.
          </p>
        </div>

        <div className="cc-ag-clients">
          {IDE_CLIENTS.map((c) => (
            <a key={c.key} href={c.setup} className="cc-ag-client">
              <span className="cc-ag-client-mono">
                <Monogram letter={c.letter} />
              </span>
              <span className="cc-ag-client-name">{c.name}</span>
              <span className="cc-ag-client-cta">Add MCP →</span>
            </a>
          ))}
        </div>
      </div>
    </section>
  );
};
