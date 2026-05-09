"use client";

import React, { FC, Suspense, useEffect } from "react";

import { CommunityIntegrationsGrid } from "@/components/integrations/CommunityIntegrationsGrid";
import { FeaturedIntegrations } from "@/components/integrations/FeaturedIntegrations";
import { IntegrationsByCategory } from "@/components/integrations/IntegrationsByCategory";
import { IntegrationsDualCta } from "@/components/integrations/IntegrationsDualCta";
import { IntegrationsHero } from "@/components/integrations/IntegrationsHero";
import { IntegrationsSpotlight } from "@/components/integrations/IntegrationsSpotlight";
import { IntegrationStarterTemplates } from "@/components/integrations/IntegrationStarterTemplates";
import { NativeIntegrationsGrid } from "@/components/integrations/NativeIntegrationsGrid";
import { LandingGlobalStyle } from "@/components/landing/LandingRoot";
import { SiteLayout } from "@/components/layout";
import { SEO } from "@/components/misc";
import { AccentThread } from "@/components/redesign-system/AccentThread";
import { VariantSwitcher } from "@/components/redesign-system/cinematic";

import { IntegrationsCinematicRoot } from "./IntegrationsCinematicRoot";
import { PortSilhouettes } from "./PortSilhouettes";

// Cinematic variant of /integrations. Renders the default component tree
// under an `<IntegrationsCinematicRoot>` shell that lays a single
// distinctive flourish behind the bands: a `<PortSilhouettes>` pair of
// faint corner-anchored port outlines (RJ45 top-right, USB-A
// bottom-left). The bands themselves are unchanged from the default
// variant, so the catalog, hero, spotlight, and CTA chrome stay 1:1.
const VARIANT_OPTIONS = [
  { id: "default", label: "Default", href: "/integrations/" },
  {
    id: "cinematic",
    label: "Cinematic",
    href: "/integrations/?v=cinematic",
  },
  { id: "grid", label: "Grid", href: "/integrations/?v=grid" },
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
          <PortSilhouettes />
          <Suspense fallback={null}>
            <IntegrationsHero />
          </Suspense>
          <IntegrationsSpotlight />
          <FeaturedIntegrations />
          <Suspense fallback={null}>
            <IntegrationsByCategory />
          </Suspense>
          <Suspense fallback={null}>
            <NativeIntegrationsGrid />
          </Suspense>
          <Suspense fallback={null}>
            <CommunityIntegrationsGrid />
          </Suspense>
          <IntegrationStarterTemplates />
          <IntegrationsDualCta />
        </IntegrationsCinematicRoot>
      </AccentThread>
      <VariantSwitcher options={VARIANT_OPTIONS} currentId="cinematic" />
    </SiteLayout>
  );
};
