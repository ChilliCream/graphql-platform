"use client";

import React, { FC } from "react";

import { ErrorFeedMock } from "@/components/observability/ErrorFeedMock";
import { ReplayPanel } from "@/components/observability/ReplayPanel";
import { SchemaDiffMock } from "@/components/observability/SchemaDiffMock";
import {
  DENSE_TRACE,
  TraceWaterfall,
} from "@/components/observability/TraceWaterfall";
import { GridSection } from "@/components/redesign-system/grid";
import { PILLARS } from "@/data/observability/pillars";

import { ObservabilityGridFeaturePanel } from "./ObservabilityGridFeaturePanel";
import { ObservabilityGridFinalCta } from "./ObservabilityGridFinalCta";
import { ObservabilityGridHero } from "./ObservabilityGridHero";
import { ObservabilityGridMcpBand } from "./ObservabilityGridMcpBand";
import { ObservabilityGridOtelSection } from "./ObservabilityGridOtelSection";
import { ObservabilityGridRoot } from "./ObservabilityGridRoot";
import { ObservabilityGridTrustStrip } from "./ObservabilityGridTrustStrip";

// Grid variant of /products/nitro/observability. Mirrors the Vercel
// observability page archetype-for-archetype: hero (text + bleeding dashboard
// composite), per-feature asymmetric split panels with shared 1px borders,
// inverted MCP band as the value statement, OTEL split, then a centered final
// CTA. The dark navy palette, cream ink tokens, and per-page accent thread
// stay; everything else collapses to strict hairline-bordered squares.

export const ObservabilityGrid: FC = () => {
  // Pillar copy comes from the shared data module so all three variants stay
  // in lockstep with /data/observability/pillars.ts.
  const tracesPillar = PILLARS.find((p) => p.key === "traces")!;
  const errorsPillar = PILLARS.find((p) => p.key === "errors")!;
  const replayPillar = PILLARS.find((p) => p.key === "replay")!;
  const schemaPillar = PILLARS.find((p) => p.key === "schema-diffs")!;

  return (
    <ObservabilityGridRoot>
      {/* 01 Hero - text left, dashboard composite bleeds off right edge.
          Bottom hairline closes the band; the next section reuses it as
          its top border, no double line. */}
      <GridSection hairlineBottom>
        <ObservabilityGridHero />
      </GridSection>

      {/* 02 Federation traces - asymmetric split, copy left, dense trace
          waterfall right. Borders shared with the row above and below. */}
      <GridSection>
        <ObservabilityGridFeaturePanel
          eyebrow={tracesPillar.eyebrow}
          headline={tracesPillar.headline}
          sub={tracesPillar.sub}
          chips={tracesPillar.chips}
          bullets={tracesPillar.bullets}
        >
          <TraceWaterfall
            spans={DENSE_TRACE}
            monoLane
            totalLabel="0ms · 600ms"
            axisMs={[0, 150, 300, 450, 600]}
          />
        </ObservabilityGridFeaturePanel>
      </GridSection>

      {/* 03 Origin-tagged errors - reverse layout, viz-left + copy-right. */}
      <GridSection>
        <ObservabilityGridFeaturePanel
          eyebrow={errorsPillar.eyebrow}
          headline={errorsPillar.headline}
          sub={errorsPillar.sub}
          chips={errorsPillar.chips}
          reverse
        >
          <ErrorFeedMock />
        </ObservabilityGridFeaturePanel>
      </GridSection>

      {/* 04 Query replay - copy-left, prod/staging replay panes right. */}
      <GridSection>
        <ObservabilityGridFeaturePanel
          eyebrow={replayPillar.eyebrow}
          headline={replayPillar.headline}
          sub={replayPillar.sub}
          chips={replayPillar.chips}
        >
          <ReplayPanel />
        </ObservabilityGridFeaturePanel>
      </GridSection>

      {/* 05 Schema diffs - reverse layout. */}
      <GridSection>
        <ObservabilityGridFeaturePanel
          eyebrow={schemaPillar.eyebrow}
          headline={schemaPillar.headline}
          sub={schemaPillar.sub}
          chips={schemaPillar.chips}
          reverse
        >
          <SchemaDiffMock />
        </ObservabilityGridFeaturePanel>
      </GridSection>

      {/* 06 Trust strip - 3 text-only cells on the band, no card chrome. */}
      <GridSection hairlineTop hairlineBottom>
        <ObservabilityGridTrustStrip />
      </GridSection>

      {/* 07 MCP for agents - the page's elevated beat, inverted band. */}
      <GridSection variant="inverted" hairlineTop hairlineBottom>
        <ObservabilityGridMcpBand />
      </GridSection>

      {/* 08 OTEL & integrations - split, text + 6-monogram strip. */}
      <GridSection>
        <ObservabilityGridOtelSection />
      </GridSection>

      {/* 09 Final CTA - centered headline, 3-button row. */}
      <GridSection hairlineTop>
        <ObservabilityGridFinalCta />
      </GridSection>
    </ObservabilityGridRoot>
  );
};
