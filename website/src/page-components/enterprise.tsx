"use client";

import React, { FC, useCallback, useEffect, useRef } from "react";

import { LandingGlobalStyle } from "@/components/landing/LandingRoot";
import { SiteLayout } from "@/components/layout";
import { SEO } from "@/components/misc";
import { BuiltByTeam } from "@/components/enterprise/BuiltByTeam";
import { ComplianceGrid } from "@/components/enterprise/ComplianceGrid";
import { CustomerOutcome } from "@/components/enterprise/CustomerOutcome";
import { EnterpriseHero } from "@/components/enterprise/EnterpriseHero";
import { EnterpriseRoot } from "@/components/enterprise/EnterpriseRoot";
import { EnterpriseSkuCards } from "@/components/enterprise/EnterpriseSkuCards";
import { FederationDeepDive } from "@/components/enterprise/FederationDeepDive";
import { InlineSalesForm } from "@/components/enterprise/InlineSalesForm";
import { MigrationSection } from "@/components/enterprise/MigrationSection";
import { PlatformPillars } from "@/components/enterprise/PlatformPillars";
import { PlatformTeamRoi } from "@/components/enterprise/PlatformTeamRoi";
import { SelfHostedAirGapped } from "@/components/enterprise/SelfHostedAirGapped";
import { CUSTOMER_OUTCOMES } from "@/data/enterprise/outcomes";

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
      <EnterpriseRoot>
        <EnterpriseHero onPrimaryClick={handleScrollToForm} />
        <PlatformPillars />
        <CustomerOutcome
          outcome={CUSTOMER_OUTCOMES[0]}
          sectionNumber="04"
          sectionLabel="Customer outcome"
        />
        <PlatformTeamRoi />
        <FederationDeepDive />
        <CustomerOutcome
          outcome={CUSTOMER_OUTCOMES[2]}
          sectionNumber="07"
          sectionLabel="Polyglot adopter"
        />
        <EnterpriseSkuCards />
        <SelfHostedAirGapped />
        <ComplianceGrid />
        <BuiltByTeam />
        <MigrationSection />
        <InlineSalesForm ref={formRef} />
      </EnterpriseRoot>
    </SiteLayout>
  );
};

export default EnterprisePage;
