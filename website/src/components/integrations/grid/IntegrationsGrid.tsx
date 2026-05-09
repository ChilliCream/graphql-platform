"use client";

import React, { FC, Suspense, useEffect } from "react";

import { LandingGlobalStyle } from "@/components/landing/LandingRoot";
import { SiteLayout } from "@/components/layout";
import { SEO } from "@/components/misc";
import { AccentThread } from "@/components/redesign-system/AccentThread";
import { VariantSwitcher } from "@/components/redesign-system/cinematic";

import { IntegrationsGridByCategory } from "./IntegrationsGridByCategory";
import { IntegrationsGridCommunity } from "./IntegrationsGridCommunity";
import { IntegrationsGridDualCta } from "./IntegrationsGridDualCta";
import { IntegrationsGridHero } from "./IntegrationsGridHero";
import { IntegrationsGridNative } from "./IntegrationsGridNative";
import { IntegrationsGridRoot } from "./IntegrationsGridRoot";
import { IntegrationsGridSpotlight } from "./IntegrationsGridSpotlight";
import { IntegrationsGridStarters } from "./IntegrationsGridStarters";

// Grid variant of /integrations. Reads the same `integrations.ts` and
// `categories.ts` data as the Default and Cinematic variants and renders a
// strict hairline-bordered, square-cornered translation:
//
//   Hero          GridSection + Type pill row (square ghost chips)
//   Spotlight     GridSplit 60-40, copy left, inline orbital diagram right
//   By Category   GridSection (hairlineTop) per category, GridRow cols=4
//   Native        GridRow cols=4
//   Community     GridRow cols=6 (denser, secondary tier)
//   Starters      GridRow cols=3
//   Dual CTA      GridSplit 50-50 with two large content panes
//
// All URL filters (?type=, ?category=, ?q=) round-trip the same way as the
// other variants, so cross-variant deep-links keep working.
const VARIANT_OPTIONS = [
  { id: "default", label: "Default", href: "/integrations/" },
  {
    id: "cinematic",
    label: "Cinematic",
    href: "/integrations/?v=cinematic",
  },
  { id: "grid", label: "Grid", href: "/integrations/?v=grid" },
];

export const IntegrationsGrid: FC = () => {
  useEffect(() => {
    document.body.classList.add("cc-landing-body");
    return () => {
      document.body.classList.remove("cc-landing-body");
    };
  }, []);

  return (
    <SiteLayout disableStars>
      <SEO
        title="Integrations"
        description="Plug ChilliCream into the rest of your stack. The API platform for humans and agents works with the auth, observability, messaging, data, and frontend tools you already run."
      />
      <LandingGlobalStyle />
      <AccentThread page="integrations">
        <IntegrationsGridRoot>
          <Suspense fallback={null}>
            <IntegrationsGridHero />
          </Suspense>
          <IntegrationsGridSpotlight />
          <Suspense fallback={null}>
            <IntegrationsGridByCategory />
          </Suspense>
          <Suspense fallback={null}>
            <IntegrationsGridNative />
          </Suspense>
          <Suspense fallback={null}>
            <IntegrationsGridCommunity />
          </Suspense>
          <IntegrationsGridStarters />
          <IntegrationsGridDualCta />
        </IntegrationsGridRoot>
      </AccentThread>
      <VariantSwitcher options={VARIANT_OPTIONS} currentId="grid" />
    </SiteLayout>
  );
};
