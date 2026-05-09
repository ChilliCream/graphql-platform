"use client";

import React, { FC } from "react";

import { Band } from "@/components/redesign-system/Band";
import { ScatterIllustration } from "@/components/redesign-system/cinematic";

// Cinematic dual CTA: same content as IntegrationsDualCta with a single
// `<ScatterIllustration variant="orbit-mini" />` anchored bottom-right of the
// band as a visual rhyme with the hero's MCP orbital diagram. The CTA grid
// sits on z-index 1 (set by IntegrationsCinematicRoot) so the scatter never
// occludes the click targets; the scatter itself is `pointer-events: none`.
//
// The in-section `.cc-section-label` is hidden by IntegrationsCinematicRoot.
export const IntegrationsCinematicDualCta: FC = () => {
  return (
    <Band
      variant="tinted"
      className="cc-band cc-band-dualcta cc-in-tinted-band"
      ariaLabel="Help shape the marketplace"
    >
      <div className="cc-in-dualcta-inner is-bandlocked">
        <a
          href="https://github.com/ChilliCream/graphql-platform/issues/new?labels=integration-request"
          className="cc-in-dualcta-card is-ghost"
          rel="noopener"
        >
          <span className="eyebrow">Don't see your tool?</span>
          <h3>File an integration request.</h3>
          <p>
            Tell us which auth provider, observability backend, or messaging
            transport is missing. The popular ones become native packages.
          </p>
          <span className="cta">Open an issue →</span>
        </a>
        <a
          href="https://chillicream.com/docs/hotchocolate/v14/server"
          className="cc-in-dualcta-card is-ghost"
          rel="noopener"
        >
          <span className="eyebrow">Building on the platform?</span>
          <h3>Build an integration.</h3>
          <p>
            The integration docs walk through the package layout, the testing
            harness, and the PR shape we use to add a community listing here.
          </p>
          <span className="cta">Read the docs →</span>
        </a>
      </div>
      <ScatterIllustration
        variant="orbit-mini"
        position={[88, 30]}
        scale={0.4}
        opacity={0.5}
      />
    </Band>
  );
};
