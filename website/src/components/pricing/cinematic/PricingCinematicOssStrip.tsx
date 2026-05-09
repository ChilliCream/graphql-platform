"use client";

import React from "react";

import {
  ScatterIllustration,
  TerminalChipRow,
} from "@/components/redesign-system/cinematic";

// Cinematic OSS strip:
//   * the flat `.cc-oss-chip` row is replaced by a prism-bordered
//     `<TerminalChipRow accent="prism" />` so the OSS belt opens with a
//     piece of homepage adapter-row DNA;
//   * a single `<ScatterIllustration variant="brewer-mini" />` is anchored
//     in the band's bottom-right negative space, away from the chips and
//     the install snippet, echoing the brewing metaphor.
//
// The ActLabel ("02 OPEN SOURCE FOREVER") is mounted at the band level by
// PricingCinematic so the eyebrow lives in the band gutter, not inline.

const OSS_CHIPS = [
  "MIT LICENSE",
  "FREE FOREVER",
  "SCHEMA REGISTRY",
  "FUSION",
  "FEDERATION",
];

export const PricingCinematicOssStrip: React.FC = () => {
  return (
    <div className="cc-oss-strip">
      <div className="cc-oss-inner">
        <div className="cc-oss-copy">
          <TerminalChipRow accent="prism" chips={OSS_CHIPS} />
          <p className="cc-oss-line">
            <strong>MIT-licensed.</strong> No account needed. No upsell. Build,
            ship, and scale a production GraphQL platform on the OSS stack
            alone.
          </p>
        </div>
        <div
          className="cc-oss-terminal"
          aria-label="Install Hot Chocolate from NuGet"
        >
          <span className="prompt">$</span>
          <span>
            <span className="cmd">dotnet add package </span>
            <span className="pkg">HotChocolate</span>
          </span>
        </div>
      </div>
      <ScatterIllustration
        variant="brewer-mini"
        position={[88, 70]}
        scale={0.55}
        opacity={0.6}
      />
    </div>
  );
};
