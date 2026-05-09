"use client";

import { useSearchParams } from "next/navigation";
import React, { FC, Suspense, useCallback, useEffect, useRef } from "react";

import { BuiltByTeam } from "@/components/enterprise/BuiltByTeam";
import { EnterpriseCinematic } from "@/components/enterprise/cinematic/EnterpriseCinematic";
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
import { LandingGlobalStyle } from "@/components/landing/LandingRoot";
import { SiteLayout } from "@/components/layout";
import { SEO } from "@/components/misc";
import { AccentThread } from "@/components/redesign-system/AccentThread";
import { VariantSwitcher } from "@/components/redesign-system/cinematic";

const VARIANT_OPTIONS = [
  { id: "default", label: "Default", href: "/enterprise/" },
  { id: "cinematic", label: "Cinematic", href: "/enterprise/?v=cinematic" },
];

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
const EnterpriseDefault: FC = () => {
  const formRef = useRef<HTMLElement>(null);

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
      <VariantSwitcher options={VARIANT_OPTIONS} currentId="default" />
    </SiteLayout>
  );
};

// Cinematic branch wraps the cinematic tree in the same SiteLayout/SEO chrome
// as the default branch and mounts the variant switcher so cinematic readers
// can hop back to the default variant.
const EnterpriseCinematicPage: FC = () => {
  return (
    <SiteLayout disableStars>
      <SEO
        title="Enterprise"
        description="The GraphQL platform for enterprise platform teams. Federate any backend in any language, on infrastructure you control."
      />
      <LandingGlobalStyle />
      <AccentThread page="enterprise">
        <EnterpriseCinematic />
      </AccentThread>
      <VariantSwitcher options={VARIANT_OPTIONS} currentId="cinematic" />
    </SiteLayout>
  );
};

// Variant dispatcher reads `?v=cinematic` and renders the cinematic tree;
// any other value (or none) falls through to the default variant. Wrapped
// in <Suspense> because useSearchParams suspends during static export.
const EnterprisePageInner: FC = () => {
  const searchParams = useSearchParams();
  const variant = searchParams?.get("v");

  if (variant === "cinematic") {
    return <EnterpriseCinematicPage />;
  }
  return <EnterpriseDefault />;
};

const EnterprisePage: FC = () => {
  useEffect(() => {
    document.body.classList.add("cc-landing-body");
    return () => {
      document.body.classList.remove("cc-landing-body");
    };
  }, []);

  return (
    <Suspense fallback={null}>
      <EnterprisePageInner />
    </Suspense>
  );
};

export default EnterprisePage;
