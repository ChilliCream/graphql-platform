"use client";

import React, { FC, useEffect } from "react";

import { LandingGlobalStyle } from "@/components/landing/LandingRoot";
import { SiteLayout } from "@/components/layout";
import { SEO } from "@/components/misc";
import { RelatedTemplates } from "@/components/templates/RelatedTemplates";
import { TemplateBody } from "@/components/templates/TemplateBody";
import { TemplateDeploySidebar } from "@/components/templates/TemplateDeploySidebar";
import { TemplateDetailHeader } from "@/components/templates/TemplateDetailHeader";
import { TemplatesRoot } from "@/components/templates/TemplatesRoot";
import { findRelated, type Template } from "@/data/templates/templates";

interface TemplateDetailPageProps {
  readonly template: Template;
}

const TemplateDetailPage: FC<TemplateDetailPageProps> = ({ template }) => {
  useEffect(() => {
    document.body.classList.add("cc-landing-body");
    return () => {
      document.body.classList.remove("cc-landing-body");
    };
  }, []);

  const related = findRelated(template);

  return (
    <SiteLayout disableStars>
      <SEO
        title={`${template.title} template`}
        description={template.tagline}
      />
      <LandingGlobalStyle />
      <TemplatesRoot>
        <TemplateDetailHeader template={template} />

        <section className="cc-tpd-section cc-tpd-body-section">
          <div className="cc-tpd-body-inner">
            <TemplateBody sections={template.body} />
            <TemplateDeploySidebar template={template} />
          </div>
        </section>

        <RelatedTemplates templates={related} />
      </TemplatesRoot>
    </SiteLayout>
  );
};

export default TemplateDetailPage;
