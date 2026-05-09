"use client";

import React, { FC, Suspense, useEffect } from "react";

import { LandingGlobalStyle } from "@/components/landing/LandingRoot";
import { SiteLayout } from "@/components/layout";
import { SEO } from "@/components/misc";
import { TemplatesCtaStrip } from "@/components/templates/TemplatesCtaStrip";
import { TemplatesGrid } from "@/components/templates/TemplatesGrid";
import { TemplatesHero } from "@/components/templates/TemplatesHero";
import { TemplatesRoot } from "@/components/templates/TemplatesRoot";

const TemplatesPage: FC = () => {
  useEffect(() => {
    document.body.classList.add("cc-landing-body");
    return () => {
      document.body.classList.remove("cc-landing-body");
    };
  }, []);

  return (
    <SiteLayout disableStars>
      <SEO
        title="Templates"
        description="Production-ready GraphQL services, federations, and clients. Clone, customize, ship. Filter by topology, language, product mix, and agent-readiness."
      />
      <LandingGlobalStyle />
      <TemplatesRoot>
        <TemplatesHero />
        <Suspense fallback={null}>
          <TemplatesGrid />
        </Suspense>
        <TemplatesCtaStrip />
      </TemplatesRoot>
    </SiteLayout>
  );
};

export default TemplatesPage;
