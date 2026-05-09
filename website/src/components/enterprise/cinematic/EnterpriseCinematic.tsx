"use client";

import React, { FC, useCallback, useRef } from "react";
import styled from "styled-components";

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

import { TopographicContours } from "./TopographicContours";

// Cinematic variant of `/enterprise`. Renders the same component tree as the
// default branch (hero -> ROI -> pillars -> federation -> SKUs -> self-hosted
// -> compliance -> built-by-the-team -> migration -> sales form), but lays a
// `<TopographicContours>` SVG behind everything so the page reads as a
// surveyed federation territory. The federation deep-dive band's diagram
// surface is dialed slightly translucent so the contours show through and
// the diagram visibly sits "on the terrain".

const Root = styled(EnterpriseRoot)`
  /* All bands and content sit on z-index 1+ so the contours stay behind. */
  & > *:not([aria-hidden="true"]) {
    position: relative;
    z-index: 1;
  }

  /* Let the topo contours bleed through the federation diagram surface. */
  .cc-ent-federation-diagram {
    background: radial-gradient(
        70% 70% at 50% 50%,
        var(--cc-accent-soft, rgba(120, 140, 220, 0.08)),
        transparent 70%
      ),
      linear-gradient(180deg, rgba(14, 22, 38, 0.42), rgba(10, 17, 30, 0.42));
    backdrop-filter: blur(2px);
    -webkit-backdrop-filter: blur(2px);
  }
`;

/**
 * Cinematic variant of the enterprise page. Renders the default component
 * tree on top of a topographic contour-map background.
 */
export const EnterpriseCinematic: FC = () => {
  const formRef = useRef<HTMLElement>(null);

  const handleScrollToForm = useCallback(() => {
    formRef.current?.scrollIntoView({ behavior: "smooth", block: "start" });
  }, []);

  return (
    <Root>
      <TopographicContours />
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
    </Root>
  );
};
