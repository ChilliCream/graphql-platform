"use client";

import React, { FC } from "react";

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
  DENSE_TRACE,
  TraceWaterfall,
} from "@/components/observability/TraceWaterfall";
import { TrustStrip } from "@/components/observability/TrustStrip";
import { Band } from "@/components/redesign-system/Band";
import { PILLARS } from "@/data/observability/pillars";

import { TraceTopology } from "./TraceTopology";

// Cinematic variant of /products/nitro/observability. Renders the same
// section tree as the default page (hero + 8 feature panels + final CTA);
// the cinematic-only treatment is a single distinctive idea, an ambient
// distributed-trace topology painted behind the entire page. The bands
// themselves and the dataviz mocks are unchanged so the narrative, copy,
// and rhythm stay 1:1 with the default variant.

export const ObservabilityCinematicRoot: FC = () => {
  // Pull pillar copy from the shared data module so the section text and the
  // chip variants stay in lockstep with `/data/observability/pillars.ts`.
  const tracesPillar = PILLARS.find((p) => p.key === "traces")!;
  const errorsPillar = PILLARS.find((p) => p.key === "errors")!;
  const replayPillar = PILLARS.find((p) => p.key === "replay")!;
  const schemaPillar = PILLARS.find((p) => p.key === "schema-diffs")!;
  const agentsPillar = PILLARS.find((p) => p.key === "agents")!;

  return (
    <ObservabilityRoot>
      {/* Ambient distributed-trace topology behind the entire page. Sits
          at z-index 0 with pointer-events: none; bands paint on top. */}
      <TraceTopology />

      {/* 01 Hero — glow band casting cyan ambient light from the right
          corner. The Hemisphere lives inside the hero shell, the layered
          DashboardComposite bleeds off the band's right edge. */}
      <Band variant="glow" glowFrom="bottom-right">
        <ObservabilityHero />
      </Band>

      {/* 02 Federation traces — tinted band, sidebar copy + full-width
          dense waterfall bleeding off the right edge. */}
      <Band variant="tinted" ariaLabel="Federation-aware traces">
        <div className="cc-obs-tint-scope">
          <FeaturePanel
            sectionNumber="02"
            sectionLabel={tracesPillar.eyebrow}
            eyebrow={tracesPillar.eyebrow}
            headline={tracesPillar.headline}
            sub={tracesPillar.sub}
            chips={tracesPillar.chips}
            layout="sidebar"
            bleedRight
            sidebarBullets={tracesPillar.bullets}
          >
            <TraceWaterfall
              spans={DENSE_TRACE}
              monoLane
              totalLabel="0ms · 600ms"
              axisMs={[0, 150, 300, 450, 600]}
            />
          </FeaturePanel>
        </div>
      </Band>

      {/* 03 Origin-tagged errors — default band, centered headline +
          breakout 3-pane mock with no card frame. */}
      <Band ariaLabel="Origin-tagged errors">
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
      </Band>

      {/* 04 Replay — accent band, the side-by-side replay panels are
          explicitly before/after so they keep their card framing inside
          the band, with a 16px overshoot on the right edge. */}
      <Band variant="accent" ariaLabel="Query replay">
        <FeaturePanel
          sectionNumber="04"
          sectionLabel={replayPillar.eyebrow}
          eyebrow={replayPillar.eyebrow}
          headline={replayPillar.headline}
          sub={replayPillar.sub}
          chips={replayPillar.chips}
          bleedRight
        >
          <ReplayPanel />
        </FeaturePanel>
      </Band>

      {/* 05 Schema diff & PR check — default band. */}
      <Band ariaLabel="Schema diff and audit">
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
      </Band>

      {/* 06 Trust strip — tinted band, 3 columns, no individual cards. */}
      <Band variant="tinted" ariaLabel="Trust strip">
        <div className="cc-obs-tint-scope">
          <div
            className="cc-section-label"
            aria-hidden
            style={{ marginBottom: 28 }}
          >
            <span className="num">06</span> Trust
          </div>
          <TrustStrip />
        </div>
      </Band>

      {/* 07 OTEL & integrations — default band, the OTEL timeline +
          monogram strip remain card-like since they're a specific exhibit. */}
      <Band ariaLabel="OTEL and integrations">
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
      </Band>

      {/* 08 MCP for agents — INVERTED band, the page's elevated beat.
          The transcript + trace are full-bleed inside the band with an
          accent vertical rule between them. */}
      <Band variant="inverted" ariaLabel="MCP for agents">
        <FeaturePanel
          sectionNumber="08"
          sectionLabel="MCP for agents"
          eyebrow={agentsPillar.eyebrow}
          headline={
            <>
              Your agents can{" "}
              <span
                style={{
                  background: "var(--cc-accent-gradient)",
                  WebkitBackgroundClip: "text",
                  backgroundClip: "text",
                  WebkitTextFillColor: "transparent",
                }}
              >
                read it too.
              </span>
            </>
          }
          sub={agentsPillar.sub}
          chips={agentsPillar.chips}
          bleedRight
        >
          <AgentTranscriptMock />
        </FeaturePanel>
      </Band>

      {/* 09 Final CTA — glow band, accent gradient on the headline. */}
      <Band variant="glow" glowFrom="top-left" ariaLabel="Get started">
        <ObservabilityFinalCta />
      </Band>
    </ObservabilityRoot>
  );
};
