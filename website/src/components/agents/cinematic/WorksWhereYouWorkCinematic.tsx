"use client";

import React, { FC } from "react";

import { Band } from "@/components/redesign-system/Band";
import {
  ActLabel,
  FrostedExplainer,
} from "@/components/redesign-system/cinematic";
import { IDE_CLIENTS } from "@/data/agents/ide-clients";

// Cinematic Section 08: distribution. Mirrors `WorksWhereYouWork` 1:1 in
// content and layout, with two cinematic differences:
//
//   - `<ActLabel n="08" name="Distribution" />` sits in the band gutter
//     in place of the legacy inline `.cc-section-label`.
//   - The "It's not another chat window" body copy is wrapped in
//     `<FrostedExplainer tone="cream">`. The band sits on a cream ground
//     (Band variant="tinted" paints #f8f4ec), so the cream-tone plate
//     uses dark ink for legibility while keeping the family resemblance
//     with the dark-tone plate used elsewhere.

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

export const WorksWhereYouWorkCinematic: FC = () => {
  return (
    <Band variant="tinted" ariaLabel="Distribution">
      <ActLabel n="08" name="Distribution" />
      <div className="cc-ag-band-inner cc-ag-tint-scope">
        <div className="cc-ag-feature-header">
          <div className="eyebrow">Distribution</div>
          <h2 className="display">It&apos;s not another chat window.</h2>
          <FrostedExplainer tone="cream">
            <p>
              It&apos;s the same chat window, suddenly aware of your platform.
              The Nitro MCP server slots into the agent clients your team
              already uses, with one config line.
            </p>
          </FrostedExplainer>
        </div>

        <ul className="cc-ag-client-strip">
          {IDE_CLIENTS.map((c) => (
            <li key={c.key} className="cc-ag-client-chip">
              <a href={c.setup}>
                <span className="cc-ag-client-mono">
                  <Monogram letter={c.letter} />
                </span>
                <span className="cc-ag-client-name">{c.name}</span>
                <span className="cc-ag-client-cta">Add MCP &rarr;</span>
              </a>
            </li>
          ))}
        </ul>
      </div>
    </Band>
  );
};
