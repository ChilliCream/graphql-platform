"use client";

import React, { FC } from "react";

import { Band } from "@/components/redesign-system/Band";

// Section 09: two CTAs side by side. Mirrors Vercel's "Request an Integration"
// + "Join the Marketplace" pattern, acknowledging that the page is read by
// two audiences: developers wishing for a missing integration, and authors
// looking to ship one. Both addresses are surfaced; we don't push the
// authoring path harder than the wishlist path.
//
// Wrapped in a tinted `<Band>` and rendered without card chrome (uplift-plan
// CC2): the band IS the surface, the CTAs sit on it as content panels with
// hairline divider, not boxes inside boxes.
export const IntegrationsDualCta: FC = () => {
  return (
    <Band
      variant="tinted"
      className="cc-in-tinted-band"
      ariaLabel="Help shape the marketplace"
    >
      <div className="cc-section-label">
        <span className="num">09</span> Get involved
      </div>
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
    </Band>
  );
};
