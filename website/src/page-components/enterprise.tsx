"use client";

import React, { FC, useCallback, useEffect, useRef } from "react";

import { LandingGlobalStyle } from "@/components/landing/LandingRoot";
import { SiteLayout } from "@/components/layout";
import { SEO } from "@/components/misc";
import { BuiltByTeam } from "@/components/enterprise/BuiltByTeam";
import { ComplianceGrid } from "@/components/enterprise/ComplianceGrid";
import { EnterpriseHero } from "@/components/enterprise/EnterpriseHero";
import { EnterpriseRoot } from "@/components/enterprise/EnterpriseRoot";
import { EnterpriseSkuCards } from "@/components/enterprise/EnterpriseSkuCards";
import { FederationDeepDive } from "@/components/enterprise/FederationDeepDive";
import { InlineSalesForm } from "@/components/enterprise/InlineSalesForm";
import { MigrationSection } from "@/components/enterprise/MigrationSection";
import { PlatformPillars } from "@/components/enterprise/PlatformPillars";
import { PlatformTeamRoi } from "@/components/enterprise/PlatformTeamRoi";
import { SelfHostedAirGapped } from "@/components/enterprise/SelfHostedAirGapped";
import { AccentThread } from "@/components/redesign-system/AccentThread";

// Band rhythm (no two adjacent same-surface bands):
//   hero (default) → ROI (tinted, StatRow not cards) →
//   pillars (accent, no card chrome) →
//   federation deep-dive (default, full-bleed signature diagram) →
//   SKUs (inverted, three constraint cards) →
//   self-hosted (default, Hemisphere accent on the right) →
//   compliance (accent, two-tier: Attestations + Capabilities) →
//   authority "Built by the team" (tinted, TypographicMoment) →
//   migration (default, ghost cards — paths, not constraints) →
//   inline form (accent, "What happens next" 3-step strip above).
const EnterprisePage: FC = () => {
  const formRef = useRef<HTMLElement>(null);

  useEffect(() => {
    document.body.classList.add("cc-landing-body");
    return () => {
      document.body.classList.remove("cc-landing-body");
    };
  }, []);

  const handleScrollToForm = useCallback(() => {
    formRef.current?.scrollIntoView({ behavior: "smooth", block: "start" });
  }, []);

  return (
    <SiteLayout disableStars>
      <SEO
        title="Enterprise"
        description="The GraphQL platform for enterprise platform teams. Federate any backend in any language, on infrastructure you control."
      />
      <LandingGlobalStyle />
      <AccentThread page="enterprise">
        <EnterpriseRoot>
          <EnterpriseHero onPrimaryClick={handleScrollToForm} />
          <PlatformTeamRoi />
          <PlatformPillars />
          <FederationDeepDive />
          <EnterpriseSkuCards />
          <SelfHostedAirGapped />
          <ComplianceGrid />
          <BuiltByTeam />
          <MigrationSection />
          <InlineSalesForm ref={formRef} />
        </EnterpriseRoot>
      </AccentThread>
    </SiteLayout>
  );
};

export default EnterprisePage;
