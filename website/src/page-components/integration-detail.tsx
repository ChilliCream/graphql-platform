"use client";

import React, { FC, useEffect } from "react";

import { IntegrationDetailBody } from "@/components/integrations/IntegrationDetailBody";
import { IntegrationDetailHeader } from "@/components/integrations/IntegrationDetailHeader";
import { IntegrationsRoot } from "@/components/integrations/IntegrationsRoot";
import { IntegrationSidebar } from "@/components/integrations/IntegrationSidebar";
import { RelatedIntegrations } from "@/components/integrations/RelatedIntegrations";
import { LandingGlobalStyle } from "@/components/landing/LandingRoot";
import { SiteLayout } from "@/components/layout";
import { SEO } from "@/components/misc";
import {
  findRelatedIntegrations,
  type Integration,
} from "@/data/integrations/integrations";

interface IntegrationDetailPageProps {
  readonly integration: Integration;
}

const IntegrationDetailPage: FC<IntegrationDetailPageProps> = ({
  integration,
}) => {
  useEffect(() => {
    document.body.classList.add("cc-landing-body");
    return () => {
      document.body.classList.remove("cc-landing-body");
    };
  }, []);

  const related = findRelatedIntegrations(integration);

  return (
    <SiteLayout disableStars>
      <SEO
        title={`${integration.name} integration`}
        description={integration.tagline}
      />
      <LandingGlobalStyle />
      <IntegrationsRoot>
        <IntegrationDetailHeader integration={integration} />

        <section className="cc-ind-section cc-ind-body-section">
          <div className="cc-ind-body-inner">
            <IntegrationDetailBody integration={integration} />
            <IntegrationSidebar integration={integration} />
          </div>
        </section>

        <RelatedIntegrations integrations={related} />
      </IntegrationsRoot>
    </SiteLayout>
  );
};

export default IntegrationDetailPage;
