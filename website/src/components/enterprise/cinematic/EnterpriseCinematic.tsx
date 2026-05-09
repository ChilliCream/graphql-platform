"use client";

import React, { FC, useCallback, useRef } from "react";
import styled from "styled-components";

import { BuiltByTeam } from "@/components/enterprise/BuiltByTeam";
import { ComplianceGrid } from "@/components/enterprise/ComplianceGrid";
import { EnterpriseHero } from "@/components/enterprise/EnterpriseHero";
import { EnterpriseRoot } from "@/components/enterprise/EnterpriseRoot";
import { EnterpriseSkuCards } from "@/components/enterprise/EnterpriseSkuCards";
import { InlineSalesForm } from "@/components/enterprise/InlineSalesForm";
import { PlatformPillars } from "@/components/enterprise/PlatformPillars";
import { SelfHostedAirGapped } from "@/components/enterprise/SelfHostedAirGapped";
import { ConnectorLine } from "@/components/redesign-system/cinematic";

import { CinematicSection } from "./CinematicSection";
import { FederationDeepDiveCinematic } from "./FederationDeepDiveCinematic";
import { MigrationSectionCinematic } from "./MigrationSectionCinematic";
import { PlatformTeamRoiCinematic } from "./PlatformTeamRoiCinematic";

// Cinematic variant of `/enterprise`. Matches the default page's band
// rhythm 1:1 (hero -> ROI -> pillars -> federation -> SKUs -> self-hosted
// -> compliance -> built-by-the-team -> migration -> sales form), but
// chapters every band with an `<ActLabel>`, grounds the federation deep-dive
// in a `<DottedGridBg>`, threads a `<ConnectorLine>` from the hero stat to
// the federation gateway, and wraps the migration lede in a
// `<FrostedExplainer>` plate.
//
// The root carries `data-cc-connector-layer` so the connector primitive
// uses the page-component as its coordinate space (the connector spans two
// non-adjacent bands, so a band-scoped container would be too small).

const Layer = styled.div`
  position: relative;
`;

export interface EnterpriseCinematicProps {
  /** Imperative scroll target for the hero's primary CTA (the sales form). */
  formRef: React.RefObject<HTMLElement>;
  /** Click handler wired to the hero's primary CTA. */
  onPrimaryClick: () => void;
}

const EnterpriseCinematicInner: FC<EnterpriseCinematicProps> = ({
  formRef,
  onPrimaryClick,
}) => {
  return (
    <Layer data-cc-connector-layer>
      <CinematicSection n="01" name="ENTERPRISE">
        <EnterpriseHero onPrimaryClick={onPrimaryClick} />
      </CinematicSection>

      <CinematicSection n="02" name="OUTCOMES">
        <PlatformTeamRoiCinematic />
      </CinematicSection>

      <CinematicSection n="03" name="PILLARS">
        <PlatformPillars />
      </CinematicSection>

      <CinematicSection n="04" name="FEDERATE">
        <FederationDeepDiveCinematic />
      </CinematicSection>

      <CinematicSection n="05" name="SKUs">
        <EnterpriseSkuCards />
      </CinematicSection>

      <CinematicSection n="06" name="SELF-HOSTED">
        <SelfHostedAirGapped />
      </CinematicSection>

      <CinematicSection n="07" name="COMPLIANCE">
        <ComplianceGrid />
      </CinematicSection>

      <CinematicSection n="08" name="BUILT BY THE TEAM">
        <BuiltByTeam />
      </CinematicSection>

      <CinematicSection n="09" name="MIGRATION">
        <MigrationSectionCinematic />
      </CinematicSection>

      <CinematicSection n="10" name="LET'S TALK">
        <InlineSalesForm ref={formRef} />
      </CinematicSection>

      {/* Single hairline connector threading the "47 → 1" stat through to
          the Fusion gateway in the federation diagram. The path is faint
          (opacity 0.18) on purpose: it is narrative chrome, not a literal
          dataviz arrow. */}
      <ConnectorLine
        from="hero-stat-collapse"
        to="federation-center"
        curve="bezier"
        weight="hairline"
        tone="ink-faint"
        containerSelector="[data-cc-connector-layer]"
      />
    </Layer>
  );
};

/**
 * Cinematic variant of the enterprise page. Wraps the inner layout in
 * `<EnterpriseRoot>` so all CSS tokens and class hooks (`.cc-ent-*`,
 * `.cc-section-label`, etc.) resolve correctly.
 */
export const EnterpriseCinematic: FC = () => {
  const formRef = useRef<HTMLElement>(null);

  const handleScrollToForm = useCallback(() => {
    formRef.current?.scrollIntoView({ behavior: "smooth", block: "start" });
  }, []);

  return (
    <EnterpriseRoot>
      <EnterpriseCinematicInner
        formRef={formRef}
        onPrimaryClick={handleScrollToForm}
      />
    </EnterpriseRoot>
  );
};
