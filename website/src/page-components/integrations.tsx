"use client";

import React, { FC, Suspense, useEffect } from "react";

import { CommunityIntegrationsGrid } from "@/components/integrations/CommunityIntegrationsGrid";
import { FeaturedIntegrations } from "@/components/integrations/FeaturedIntegrations";
import { IntegrationsByCategory } from "@/components/integrations/IntegrationsByCategory";
import { IntegrationsDualCta } from "@/components/integrations/IntegrationsDualCta";
import { IntegrationsHero } from "@/components/integrations/IntegrationsHero";
import { IntegrationsRoot } from "@/components/integrations/IntegrationsRoot";
import { IntegrationsSpotlight } from "@/components/integrations/IntegrationsSpotlight";
import { IntegrationStarterTemplates } from "@/components/integrations/IntegrationStarterTemplates";
import { NativeIntegrationsGrid } from "@/components/integrations/NativeIntegrationsGrid";
import { LandingGlobalStyle } from "@/components/landing/LandingRoot";
import { SiteLayout } from "@/components/layout";
import { SEO } from "@/components/misc";

const IntegrationsPage: FC = () => {
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
    </SiteLayout>
  );
};

export default IntegrationsPage;
