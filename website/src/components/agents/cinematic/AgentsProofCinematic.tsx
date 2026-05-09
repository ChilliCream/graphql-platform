"use client";

import React, { FC } from "react";

import { AgentDemo } from "@/components/agents/AgentDemo";
import { Band } from "@/components/redesign-system/Band";
import { ActLabel } from "@/components/redesign-system/cinematic";
import { DEMOS } from "@/data/agents/demos";

// Cinematic Section 05: proof. Inverted band that hosts both demos
// (Demo A diagnose, Demo B compose). Mirrors the inline proof block
// in `nitro-agents.tsx` 1:1; the only chrome difference is the
// `<ActLabel>` chapter marker that sits in the band gutter at top:36px
// instead of the inline `.cc-section-label` (which is hidden by
// `AgentsCinematicRoot`).
//
// The demos themselves are rendered through the existing `AgentDemo`
// component so the TraceWaterfall and animated transcripts continue to
// work unchanged.

export const AgentsProofCinematic: FC = () => {
  return (
    <Band variant="inverted" id="proof" ariaLabel="Proof">
      <ActLabel n="05" name="Proof" />
      <div className="cc-ag-band-inner">
        <div className="cc-ag-feature-header">
          <div className="eyebrow">Proof</div>
          <h2 className="display">Diagnose. Compose. Two loops, one agent.</h2>
          <p>
            Two prompts, two complete loops. Demo A descends into causes
            (Observe + Reason). Demo B fans out across the four surfaces the
            agent has to register against (Act + Compose + Ship). The
            transcripts are real-shape: the tool calls match what Nitro emits
            today.
          </p>
        </div>
        <div className="cc-ag-demos">
          {DEMOS.map((demo) => (
            <AgentDemo key={demo.key} demo={demo} />
          ))}
        </div>
      </div>
    </Band>
  );
};
