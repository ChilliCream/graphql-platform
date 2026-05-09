"use client";

import React, { FC } from "react";

import { AccentThread } from "@/components/redesign-system/AccentThread";
import { findRelatedSolutions } from "@/data/solutions/solutions";
import type { SolutionRecord } from "@/data/solutions/types";

import { SolutionGridCodeSnippet } from "./SolutionGridCodeSnippet";
import { SolutionGridCollateral } from "./SolutionGridCollateral";
import { SolutionGridDiagram } from "./SolutionGridDiagram";
import { SolutionGridFeatureRow } from "./SolutionGridFeatureRow";
import { SolutionGridFinalCta } from "./SolutionGridFinalCta";
import { SolutionGridHero } from "./SolutionGridHero";
import { SolutionGridLogoWall } from "./SolutionGridLogoWall";
import { SolutionGridPillars } from "./SolutionGridPillars";
import { SolutionGridProofStrip } from "./SolutionGridProofStrip";
import { SolutionGridRelated } from "./SolutionGridRelated";
import { SolutionGridTestimonial } from "./SolutionGridTestimonial";
import { SolutionsGridRoot } from "./SolutionsGridRoot";

interface SolutionPageRendererGridProps {
  readonly record: SolutionRecord;
  readonly slug: string;
}

// Master template for the Grid variant of /solutions/[slug]. One component
// renders all 7 pages by composing the Grid archetype components against
// the same SolutionRecord data the Default and Cinematic variants consume.
//
// Section sequence (per spec § 9 /solutions/[slug] mapping):
//   01 hero               GridSection with H1 + sub + 2-button + optional motif
//   02 proof strip        4-up <GridRow cols={4}> of <GridStat>
//   03 pillars            3-up GridRow of square pillar cards
//   04 concept diagram    full-bleed inverted band, noPadding card
//   05 code snippet       GridSection with code in noPadding card  (use-case only)
//   06 testimonial        full-width quote, no card chrome
//   07 foundations row    <GridRow cols={6}> dense feature tiles
//   08 collateral         single GridCard for the playbook offer  (when present)
//   09 logo wall          <GridRow cols={4}> typographic descriptor lockups
//   10 final CTA          1 primary button + secondary link  (no third CTA)
//   11 related solutions  <GridRow cols={3}>
//
// Industry pages (banking, regulated) skip 05 (code snippet) by virtue of
// `record.codeSnippet` being absent. Optional collateral works the same way.
export const SolutionPageRendererGrid: FC<SolutionPageRendererGridProps> = ({
  record,
  slug,
}) => {
  const related = findRelatedSolutions(record);

  return (
    <AccentThread page="solutions" override={record.accent}>
      <SolutionsGridRoot>
        <SolutionGridHero
          hero={record.hero}
          motif={record.heroMotif}
          slug={slug}
        />
        <SolutionGridProofStrip metrics={record.proofMetrics} />
        <SolutionGridPillars pillars={record.pillars} />
        <SolutionGridDiagram kind={record.diagram} />
        {record.codeSnippet && (
          <SolutionGridCodeSnippet snippet={record.codeSnippet} />
        )}
        <SolutionGridTestimonial testimonials={record.testimonials} />
        <SolutionGridFeatureRow cards={record.featureCards} />
        {record.collateral && (
          <SolutionGridCollateral collateral={record.collateral} />
        )}
        <SolutionGridLogoWall
          logos={record.logos}
          caption={record.logoCaption}
        />
        <SolutionGridFinalCta cta={record.finalCta} />
        <SolutionGridRelated solutions={related} />
      </SolutionsGridRoot>
    </AccentThread>
  );
};
