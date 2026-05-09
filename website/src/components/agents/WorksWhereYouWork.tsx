"use client";

import React, { FC } from "react";

import { Band } from "@/components/redesign-system/Band";
import { IDE_CLIENTS } from "@/data/agents/ide-clients";

// Section 08: distribution. Reduced from a 4-up card grid to a single
// horizontal chip strip (logo + name + Add MCP link). No cards. The amber
// "Add MCP" link is a system signal — it appears wherever the user can put
// the agent into action.

const Monogram: FC<{ letter: string }> = ({ letter }) => (
  <svg viewBox="0 0 36 36" width="36" height="36" aria-hidden>
    <g
      stroke="currentColor"
      strokeWidth="1.4"
      fill="none"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <circle cx="18" cy="18" r="14" opacity="0.55" />
      <text
        x="18"
        y="23"
        textAnchor="middle"
        fontFamily="var(--cc-font-sans), sans-serif"
        fontWeight={500}
        fontSize="15"
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
    <Band variant="tinted" ariaLabel="Distribution">
      <div className="cc-ag-band-inner cc-ag-tint-scope">
        <div className="cc-section-label">
          <span className="num">08</span> Distribution
        </div>
        <div className="cc-ag-feature-header">
          <div className="eyebrow">Distribution</div>
          <h2 className="display">It's not another chat window.</h2>
          <p>
            It's the same chat window, suddenly aware of your platform. The
            Nitro MCP server slots into the agent clients your team already
            uses, with one config line.
          </p>
        </div>

        <ul className="cc-ag-client-strip">
          {IDE_CLIENTS.map((c) => (
            <li key={c.key} className="cc-ag-client-chip">
              <a href={c.setup}>
                <span className="cc-ag-client-mono">
                  <Monogram letter={c.letter} />
                </span>
                <span className="cc-ag-client-name">{c.name}</span>
                <span className="cc-ag-client-cta">Add MCP →</span>
              </a>
            </li>
          ))}
        </ul>
      </div>
    </Band>
  );
};
