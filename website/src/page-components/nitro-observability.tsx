"use client";

import React, { FC, useEffect } from "react";

import { LandingGlobalStyle } from "@/components/landing/LandingRoot";
import { SiteLayout } from "@/components/layout";
import { SEO } from "@/components/misc";
import { AgentTranscriptMock } from "@/components/observability/AgentTranscriptMock";
import { ErrorFeedMock } from "@/components/observability/ErrorFeedMock";
import { FeaturePanel } from "@/components/observability/FeaturePanel";
import { ObservabilityFinalCta } from "@/components/observability/ObservabilityFinalCta";
import { ObservabilityHero } from "@/components/observability/ObservabilityHero";
import { ObservabilityRoot } from "@/components/observability/ObservabilityRoot";
import {
  OtelLogoStrip,
  OtelTimeline,
} from "@/components/observability/OtelLogoStrip";
import { ReplayPanel } from "@/components/observability/ReplayPanel";
import { SchemaDiffMock } from "@/components/observability/SchemaDiffMock";
import {
  DEFAULT_TRACE,
  TraceWaterfall,
} from "@/components/observability/TraceWaterfall";
import { TrustStrip } from "@/components/observability/TrustStrip";
import { PILLARS } from "@/data/observability/pillars";

const NitroObservabilityPage: FC = () => {
  useEffect(() => {
    document.body.classList.add("cc-landing-body");
    return () => {
      document.body.classList.remove("cc-landing-body");
    };
  }, []);

  // Pull pillar copy from the shared data module so the section text and the
  // chip variants stay in lockstep with `/data/observability/pillars.ts`.
  const tracesPillar = PILLARS.find((p) => p.key === "traces")!;
  const errorsPillar = PILLARS.find((p) => p.key === "errors")!;
  const replayPillar = PILLARS.find((p) => p.key === "replay")!;
  const schemaPillar = PILLARS.find((p) => p.key === "schema-diffs")!;
  const agentsPillar = PILLARS.find((p) => p.key === "agents")!;

  return (
    <SiteLayout disableStars>
      <SEO
        title="Observability"
        description="One trace spans the gateway and every owning service. Federation-aware tracing, query replay, schema diffs and an MCP surface for agents — built into Nitro."
      />
      <LandingGlobalStyle />
      <ObservabilityRoot>
        <ObservabilityHero />

        <FeaturePanel
          sectionNumber="02"
          sectionLabel={tracesPillar.eyebrow}
          eyebrow={tracesPillar.eyebrow}
          headline={tracesPillar.headline}
          sub={tracesPillar.sub}
          chips={tracesPillar.chips}
        >
          <TraceWaterfall
            spans={DEFAULT_TRACE}
            totalLabel="0ms · 600ms"
            axisMs={[0, 150, 300, 450, 600]}
          />
        </FeaturePanel>

        <FeaturePanel
          sectionNumber="03"
          sectionLabel={errorsPillar.eyebrow}
          eyebrow={errorsPillar.eyebrow}
          headline={errorsPillar.headline}
          sub={errorsPillar.sub}
          chips={errorsPillar.chips}
        >
          <ErrorFeedMock />
        </FeaturePanel>

        <FeaturePanel
          sectionNumber="04"
          sectionLabel={replayPillar.eyebrow}
          eyebrow={replayPillar.eyebrow}
          headline={replayPillar.headline}
          sub={replayPillar.sub}
          chips={replayPillar.chips}
        >
          <ReplayPanel />
        </FeaturePanel>

        <FeaturePanel
          sectionNumber="05"
          sectionLabel={schemaPillar.eyebrow}
          eyebrow={schemaPillar.eyebrow}
          headline={schemaPillar.headline}
          sub={schemaPillar.sub}
          chips={schemaPillar.chips}
        >
          <SchemaDiffMock />
        </FeaturePanel>

        <TrustStrip />

        <FeaturePanel
          sectionNumber="07"
          sectionLabel="OTEL & integrations"
          eyebrow="OTEL & integrations"
          headline="Bring your backend."
          sub="Built on OpenTelemetry. Federation traces appear in the tools you already use, no glue."
          chips={["all"]}
        >
          <OtelTimeline />
          <OtelLogoStrip />
        </FeaturePanel>

        <FeaturePanel
          sectionNumber="08"
          sectionLabel="MCP for agents"
          eyebrow={agentsPillar.eyebrow}
          headline={agentsPillar.headline}
          sub={agentsPillar.sub}
          chips={agentsPillar.chips}
          elevated
        >
          <AgentTranscriptMock />
        </FeaturePanel>

        <ObservabilityFinalCta />
      </ObservabilityRoot>
    </SiteLayout>
  );
};

export default NitroObservabilityPage;
