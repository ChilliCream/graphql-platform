"use client";

import React, { FC } from "react";

// Section 09: two CTAs side by side. Mirrors Vercel's "Request an Integration"
// + "Join the Marketplace" pattern, acknowledging that the page is read by
// two audiences: developers wishing for a missing integration, and authors
// looking to ship one. Both addresses are surfaced; we don't push the
// authoring path harder than the wishlist path.
export const IntegrationsDualCta: FC = () => {
  return (
    <section className="cc-in-section cc-in-dualcta">
      <div className="cc-in-dualcta-inner">
        <a
          href="https://github.com/ChilliCream/graphql-platform/issues/new?labels=integration-request"
          className="cc-in-dualcta-card"
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
          className="cc-in-dualcta-card"
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
    </section>
  );
};
