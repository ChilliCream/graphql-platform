"use client";

import React, { FC } from "react";

import { AccentThread } from "@/components/redesign-system/AccentThread";
import { ActLabel } from "@/components/redesign-system/cinematic";
import { findRelatedSolutions } from "@/data/solutions/solutions";
import type { SolutionRecord } from "@/data/solutions/types";

import { CodeSnippet } from "../CodeSnippet";
import { PillarsSection } from "../PillarsSection";
import { ProofStrip } from "../ProofStrip";
import { RelatedSolutions } from "../RelatedSolutions";
import { SolutionCollateral } from "../SolutionCollateral";
import { SolutionFeatureCards } from "../SolutionFeatureCards";
import { SolutionFinalCta } from "../SolutionFinalCta";
import { SolutionLogoWall } from "../SolutionLogoWall";
import { ConceptDiagramCinematic } from "./ConceptDiagramCinematic";
import { SolutionHeroCinematic } from "./SolutionHeroCinematic";
import { SolutionTestimonialCinematic } from "./SolutionTestimonialCinematic";

interface SolutionPageRendererCinematicProps {
  readonly record: SolutionRecord;
}

// Cinematic master template. Mirrors `SolutionPageRenderer.tsx` 1:1 so the
// section composition is identical; the cinematic variant only adds page
// chrome (gutter padding, ActLabel chapter markers, connector-line overlay
// on the diagram, frosted plates on the quotes) by:
//
//   * wrapping each default-variant section in a `cc-band-cinematic-wrap`
//     element so its <Band> sits inside a positioned ancestor, then
//     mounting an `<ActLabel>` as a sibling at the band's top gutter;
//   * replacing the architecture and voices sections with cinematic
//     variants that embed the connector overlay and frosted plates.
//
// The default section components are not modified; the wrapper-and-sibling
// pattern keeps the cinematic chrome additive.
//
// Band rhythm (parallel to the default renderer; the names follow the
// homepage's act-marker conventions):
//   01 <SLUG-NAME>     glow band, motif right, accent threads in
//   02 OUTCOMES        proof strip with StatRow
//   03 PILLARS         content-on-band pillars
//   04 ARCHITECTURE    inverted band, cinematic diagram
//   05 SETUP           tinted code snippet (use-case pages only)
//   06 VOICES          frosted-plate testimonials
//   07 FOUNDATIONS     dense icon strip
//   08 PLAYBOOK        accent-band collateral (when present)
//   09 ADOPTERS        typographic logo wall
//   10 GET STARTED     glow band, final CTA
//   11 RELATED         tinted cross-link grid

const slugChapterName = (slug: string): string =>
  slug.replace(/-/g, " ").toUpperCase();

interface ChapterBandProps {
  readonly n: string;
  readonly name: string;
  readonly children: React.ReactNode;
}

// Wraps a default-variant section in a positioned container so its
// chapter marker can be absolutely positioned at the band gutter. The
// SolutionsCinematicRoot stylesheet handles the gutter padding on the
// wrapped Band's section element so the marker has clearance.
const ChapterBand: FC<ChapterBandProps> = ({ n, name, children }) => (
  <div className="cc-band-cinematic-wrap">
    <ActLabel n={n} name={name} />
    {children}
  </div>
);

export const SolutionPageRendererCinematic: FC<
  SolutionPageRendererCinematicProps
> = ({ record }) => {
  const related = findRelatedSolutions(record);

  // Step numbers are computed in document order; conditional sections (code
  // snippet, collateral) shift the trailing labels so every page reads
  // 01..N without gaps.
  let step = 4; // last fixed section is 04 ARCHITECTURE
  const nextStep = (): string => {
    step += 1;
    return step.toString().padStart(2, "0");
  };

  const codeStep = record.codeSnippet ? nextStep() : null;
  const voicesStep = nextStep();
  const foundationsStep = nextStep();
  const playbookStep = record.collateral ? nextStep() : null;
  const adoptersStep = nextStep();
  const finalStep = nextStep();
  const relatedStep = nextStep();

  return (
    <AccentThread page="solutions" override={record.accent}>
      <SolutionHeroCinematic
        hero={record.hero}
        motif={record.heroMotif}
        slug={record.slug}
        stepNumber="01"
        chapterName={slugChapterName(record.slug)}
      />
      <ChapterBand n="02" name="Outcomes">
        <ProofStrip metrics={record.proofMetrics} />
      </ChapterBand>
      <ChapterBand n="03" name="Pillars">
        <PillarsSection pillars={record.pillars} />
      </ChapterBand>
      <ConceptDiagramCinematic kind={record.diagram} stepNumber="04" />
      {record.codeSnippet && codeStep && (
        <ChapterBand n={codeStep} name="Setup">
          <CodeSnippet snippet={record.codeSnippet} stepNumber={codeStep} />
        </ChapterBand>
      )}
      <SolutionTestimonialCinematic
        testimonials={record.testimonials}
        stepNumber={voicesStep}
      />
      <ChapterBand n={foundationsStep} name="Foundations">
        <SolutionFeatureCards
          cards={record.featureCards}
          stepNumber={foundationsStep}
        />
      </ChapterBand>
      {record.collateral && playbookStep && (
        <ChapterBand n={playbookStep} name="Playbook">
          <SolutionCollateral
            collateral={record.collateral}
            stepNumber={playbookStep}
          />
        </ChapterBand>
      )}
      <ChapterBand n={adoptersStep} name="Adopters">
        <SolutionLogoWall
          logos={record.logos}
          caption={record.logoCaption}
          stepNumber={adoptersStep}
        />
      </ChapterBand>
      <ChapterBand n={finalStep} name="Get started">
        <SolutionFinalCta cta={record.finalCta} stepNumber={finalStep} />
      </ChapterBand>
      <ChapterBand n={relatedStep} name="Related">
        <RelatedSolutions solutions={related} stepNumber={relatedStep} />
      </ChapterBand>
    </AccentThread>
  );
};
