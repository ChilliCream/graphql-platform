"use client";

import React, { FC } from "react";

import { findRelatedSolutions } from "@/data/solutions/solutions";
import type { SolutionRecord } from "@/data/solutions/types";

import { CodeSnippet } from "./CodeSnippet";
import { ConceptDiagram } from "./ConceptDiagram";
import { PillarsSection } from "./PillarsSection";
import { ProofStrip } from "./ProofStrip";
import { RelatedSolutions } from "./RelatedSolutions";
import { SolutionCollateral } from "./SolutionCollateral";
import { SolutionFeatureCards } from "./SolutionFeatureCards";
import { SolutionFinalCta } from "./SolutionFinalCta";
import { SolutionHero } from "./SolutionHero";
import { SolutionLogoWall } from "./SolutionLogoWall";
import { SolutionTestimonial } from "./SolutionTestimonial";

interface SolutionPageRendererProps {
  readonly record: SolutionRecord;
}

// The master template. One component, every solution page. Section
// numbering is computed at render time so industry pages (no code
// snippet) renumber the trailing sections automatically. Adding a new
// section means adding one component import and one entry to the
// composition; the section labels reflow for free.
export const SolutionPageRenderer: FC<SolutionPageRendererProps> = ({
  record,
}) => {
  const related = findRelatedSolutions(record);

  // Compute step numbers in document order. Sections 01..04 are fixed; 05
  // (code snippet) and 08 (collateral) are conditional. We renumber so
  // industry pages still read 01..N without gaps.
  let step = 4; // last fixed section is 04 (Architecture)
  const nextStep = (): string => {
    step += 1;
    return step.toString().padStart(2, "0");
  };

  const codeStep = record.codeSnippet ? nextStep() : null;
  const testimonialsStep = nextStep();
  const featuresStep = nextStep();
  const collateralStep = record.collateral ? nextStep() : null;
  const logosStep = nextStep();
  const finalStep = nextStep();
  const relatedStep = nextStep();

  return (
    <>
      <SolutionHero hero={record.hero} />
      <ProofStrip metrics={record.proofMetrics} />
      <PillarsSection pillars={record.pillars} />
      <ConceptDiagram kind={record.diagram} />
      {record.codeSnippet && codeStep && (
        <CodeSnippet snippet={record.codeSnippet} stepNumber={codeStep} />
      )}
      <SolutionTestimonial
        testimonials={record.testimonials}
        stepNumber={testimonialsStep}
      />
      <SolutionFeatureCards
        cards={record.featureCards}
        stepNumber={featuresStep}
      />
      {record.collateral && collateralStep && (
        <SolutionCollateral
          collateral={record.collateral}
          stepNumber={collateralStep}
        />
      )}
      <SolutionLogoWall
        logos={record.logos}
        caption={record.logoCaption}
        stepNumber={logosStep}
      />
      <SolutionFinalCta cta={record.finalCta} stepNumber={finalStep} />
      <RelatedSolutions solutions={related} stepNumber={relatedStep} />
    </>
  );
};
