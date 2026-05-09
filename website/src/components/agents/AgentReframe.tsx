"use client";

import React, { FC } from "react";

import { Band } from "@/components/redesign-system/Band";

// Section 02: the reframe. Two-column compare on a tinted band (no card
// chrome). Left column reads as "what every other AI dev tool does" — opacity
// dimmed, neutral chip styling. Right column reads as "what ChilliCream does"
// — full opacity, amber chip styling, amber column heading. Punchline sits
// centered under both columns, separated by a dashed divider. Amber is used
// here as the "agent is doing something" signal, not as paint.

export const AgentReframe: FC = () => {
  return (
    <Band variant="tinted" ariaLabel="Reframe">
      <div className="cc-ag-band-inner cc-ag-tint-scope">
        <div className="cc-section-label">
          <span className="num">02</span> Reframe
        </div>
        <div className="cc-ag-feature-header">
          <div className="eyebrow">Reframe</div>
          <h2 className="display">
            Code generation is downstream of system understanding.
          </h2>
          <p>
            Most AI for developers helps an agent write the next line. Nitro
            lets an agent operate the whole platform that line runs in.
          </p>
        </div>

        <div className="cc-ag-reframe-grid">
          <div className="cc-ag-reframe-col is-muted">
            <div className="eyebrow">AI in your IDE</div>
            <h3 className="cc-ag-reframe-h">Auto-completes the next line.</h3>
            <p className="cc-ag-reframe-body">
              Knows your repo's surface. Suggests imports. Refactors the file
              you're staring at. Stops at the file boundary.
            </p>
            <ul className="cc-ag-reframe-bullets">
              <li>File-scoped</li>
              <li>Snippet-shaped</li>
              <li>Static repo view</li>
            </ul>
          </div>

          <div className="cc-ag-reframe-divider" aria-hidden />

          <div className="cc-ag-reframe-col is-bright">
            <div className="eyebrow">Agents on your platform</div>
            <h3 className="cc-ag-reframe-h">Operates the system.</h3>
            <p className="cc-ag-reframe-body">
              Reads every trace. Walks every resolver. Diffs every schema. Knows
              which subgraph owns each field, and changes it safely across the
              federation.
            </p>
            <ul className="cc-ag-reframe-bullets">
              <li>Federation-wide</li>
              <li>Schema-typed</li>
              <li>Live runtime view</li>
            </ul>
          </div>
        </div>

        <div className="cc-ag-reframe-punch">
          One agent. <span className="accent">Six surfaces.</span> Zero
          archaeology.
        </div>
      </div>
    </Band>
  );
};
