"use client";

import React, { FC } from "react";

import { Band } from "@/components/redesign-system/Band";
import { ActLabel } from "@/components/redesign-system/cinematic";
import { AgentSeesKind, AGENT_SEES_TILES } from "@/data/agents/agent-sees";

import { AGENT_SEES_RENDERERS } from "./agent-sees-renderers";

// Cinematic Section 04: six surfaces. Mirrors `WhatAgentSees` 1:1 in
// content and layout; the only chrome difference is the `<ActLabel>`
// chapter marker that sits in the band gutter at top:36px instead of
// the inline `.cc-section-label` (which is hidden by AgentsCinematicRoot).
//
// The mini-illustration renderers are reused via the shared
// `AGENT_SEES_RENDERERS` map so the cinematic and default variants stay
// pixel-identical inside the tiles.

export const WhatAgentSeesCinematic: FC = () => {
  return (
    <Band variant="default" ariaLabel="Six surfaces, one MCP endpoint">
      <ActLabel n="04" name="Six surfaces" />
      <div className="cc-ag-band-inner">
        <div className="cc-ag-feature-header">
          <div className="eyebrow">One schema-typed surface</div>
          <h2 className="display">Six surfaces. One MCP endpoint.</h2>
          <p>
            Every signal a senior engineer would chase, distributed traces,
            metrics, logs, messaging topology, the API graph, and the source
            code itself, queryable from one schema-typed place.
          </p>
        </div>

        <div className="cc-ag-sees-grid">
          {AGENT_SEES_TILES.map((tile) => {
            const render = AGENT_SEES_RENDERERS[tile.key as AgentSeesKind];
            return (
              <div key={tile.key} className="cc-ag-sees-tile">
                <div className="eyebrow">{tile.eyebrow}</div>
                <h3>{tile.title}</h3>
                <p>{tile.body}</p>
                <div className="cc-ag-sees-viz">{render()}</div>
              </div>
            );
          })}
        </div>
      </div>
    </Band>
  );
};
