"use client";

import { useSearchParams } from "next/navigation";
import React, { FC, Suspense, useEffect } from "react";

import { CommunityIntegrationsGrid } from "@/components/integrations/CommunityIntegrationsGrid";
import { FeaturedIntegrations } from "@/components/integrations/FeaturedIntegrations";
import { IntegrationsByCategory } from "@/components/integrations/IntegrationsByCategory";
import { IntegrationsCinematic } from "@/components/integrations/cinematic/IntegrationsCinematic";
import { IntegrationsDualCta } from "@/components/integrations/IntegrationsDualCta";
import { IntegrationsGrid } from "@/components/integrations/grid/IntegrationsGrid";
import { IntegrationsHero } from "@/components/integrations/IntegrationsHero";
import { IntegrationsRoot } from "@/components/integrations/IntegrationsRoot";
import { IntegrationsSpotlight } from "@/components/integrations/IntegrationsSpotlight";
import { IntegrationStarterTemplates } from "@/components/integrations/IntegrationStarterTemplates";
import { NativeIntegrationsGrid } from "@/components/integrations/NativeIntegrationsGrid";
import { LandingGlobalStyle } from "@/components/landing/LandingRoot";
import { SiteLayout } from "@/components/layout";
import { SEO } from "@/components/misc";
import { AccentThread } from "@/components/redesign-system/AccentThread";
import { VariantSwitcher } from "@/components/redesign-system/cinematic";

const VARIANT_OPTIONS = [
  { id: "default", label: "Default", href: "/integrations/" },
  {
    id: "cinematic",
    label: "Cinematic",
    href: "/integrations/?v=cinematic",
  },
  { id: "grid", label: "Grid", href: "/integrations/?v=grid" },
];

// Page rhythm (uplift-plan P0-integrations-3 / 4): the page reads as a stack
// of full-bleed bands in alternating registers, with the per-page green-
// circuit accent threaded through the spotlight, marquee category, starter
// glow, and CTA cards.
//
//   Hero          default  | typography on the page surface
//   Spotlight     accent   | full-bleed accent wash, MCP orbital diagram
//   Featured      tinted   | curated cards on a cream cutout
//   By Category   default  | sticky rail + categories, first one is marquee
//   Native        default  | filled, partner-color tiles
//   Community     tinted   | dense, stroked monograms (visibly secondary)
//   Starters      glow     | accent-tinted radial cast under the row
//   Dual CTA      tinted   | content-on-band, no card chrome
const IntegrationsDefault: FC = () => {
  return (
    <SiteLayout disableStars>
      <SEO
        title="Integrations"
        description="Plug ChilliCream into the rest of your stack. The API platform for humans and agents works with the auth, observability, messaging, data, and frontend tools you already run."
      />
      <LandingGlobalStyle />
      <AccentThread page="integrations">
        <IntegrationsRoot>
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
        </IntegrationsRoot>
      </AccentThread>
      <VariantSwitcher options={VARIANT_OPTIONS} currentId="default" />
    </SiteLayout>
  );
};

// Variant dispatcher reads `?v=cinematic` or `?v=grid` and renders the
// matching tree; any other value (or none) falls through to the default
// variant. Wrapped in <Suspense> because useSearchParams suspends during
// static export.
const IntegrationsPageInner: FC = () => {
  const searchParams = useSearchParams();
  const variant = searchParams?.get("v");

  if (variant === "cinematic") {
    return <IntegrationsCinematic />;
  }
  if (variant === "grid") {
    return <IntegrationsGrid />;
  }
  return <IntegrationsDefault />;
};

const IntegrationsPage: FC = () => {
  useEffect(() => {
    document.body.classList.add("cc-landing-body");
    return () => {
      document.body.classList.remove("cc-landing-body");
    };
  }, []);

  return (
    <Suspense fallback={null}>
      <IntegrationsPageInner />
    </Suspense>
  );
};

export default IntegrationsPage;
