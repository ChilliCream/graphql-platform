"use client";

import React, { FC, Suspense, useEffect } from "react";

import { LandingGlobalStyle } from "@/components/landing/LandingRoot";
import { SiteLayout } from "@/components/layout";
import { SEO } from "@/components/misc";
import { AccentThread } from "@/components/redesign-system/AccentThread";
import { Band } from "@/components/redesign-system/Band";
import {
  ActLabel,
  VariantSwitcher,
} from "@/components/redesign-system/cinematic";

import { IntegrationsCinematicByCategory } from "./IntegrationsCinematicByCategory";
import { IntegrationsCinematicCommunityGrid } from "./IntegrationsCinematicCommunityGrid";
import { IntegrationsCinematicDualCta } from "./IntegrationsCinematicDualCta";
import { IntegrationsCinematicFeatured } from "./IntegrationsCinematicFeatured";
import { IntegrationsCinematicHero } from "./IntegrationsCinematicHero";
import { IntegrationsCinematicNativeGrid } from "./IntegrationsCinematicNativeGrid";
import { IntegrationsCinematicRoot } from "./IntegrationsCinematicRoot";
import { IntegrationsCinematicSpotlight } from "./IntegrationsCinematicSpotlight";
import { IntegrationsCinematicStarters } from "./IntegrationsCinematicStarters";

// Cinematic variant of /integrations. Same band rhythm as the default variant,
// same data files, same accent thread. The chrome lifted from the homepage:
//   * <ActLabel n="0X" name="..." /> in the gutter of the hero, spotlight and
//     featured bands, and one per visible category block (04, 05, 06, ...);
//   * <DottedGridBg density="sm" fade="both" /> under the Native tile wall so
//     the directory reads as a topology surface;
//   * <ScatterIllustration variant="orbit-mini" /> anchored in the bottom-
//     right of the dual-CTA band, echoing the hero's MCP orbital diagram.
//
// Anti-patterns this variant deliberately AVOIDS (per uplift-plan section 4):
//   * No connector lines (the grid is a directory, not a diagram).
//   * No vibrant blog tiles (the integration tiles already carry partner
//     color; vibrant tiles on top would compound noise).
//   * No FusionLensEffect.
//
// VariantSwitcher is mounted once at the bottom of the tree so cinematic
// readers can hop back to the default variant. URL filters (?type, ?q,
// ?category) keep working: each cinematic component reads useSearchParams
// the same way the default variant does, and the hero's filter pills pass
// through current params (including ?v=cinematic) when writing.
const VARIANT_OPTIONS = [
  { id: "default", label: "Default", href: "/integrations/" },
  {
    id: "cinematic",
    label: "Cinematic",
    href: "/integrations/?v=cinematic",
  },
];

export const IntegrationsCinematic: FC = () => {
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
        <IntegrationsCinematicRoot>
          <Band variant="default" className="cc-band cc-band-hero">
            <ActLabel n="01" name="Integrations" />
            <Suspense fallback={null}>
              <IntegrationsCinematicHero />
            </Suspense>
          </Band>
          <IntegrationsCinematicSpotlight />
          <IntegrationsCinematicFeatured />
          <Suspense fallback={null}>
            <IntegrationsCinematicByCategory />
          </Suspense>
          <Suspense fallback={null}>
            <IntegrationsCinematicNativeGrid />
          </Suspense>
          <Suspense fallback={null}>
            <IntegrationsCinematicCommunityGrid />
          </Suspense>
          <IntegrationsCinematicStarters />
          <IntegrationsCinematicDualCta />
        </IntegrationsCinematicRoot>
      </AccentThread>
      <VariantSwitcher options={VARIANT_OPTIONS} currentId="cinematic" />
    </SiteLayout>
  );
};
