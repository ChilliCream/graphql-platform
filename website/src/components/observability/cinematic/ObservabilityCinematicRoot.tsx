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
import {
  ActLabel,
  Anchor,
  ConnectorLine,
  InsetWindow,
} from "@/components/redesign-system/cinematic";
import { PILLARS } from "@/data/observability/pillars";

// Cinematic variant of /products/nitro/observability. Mirrors the default
// page's band rhythm 1:1 so the narrative, copy, and order are identical, but
// borrows three homepage devices: the chapter-marker `<ActLabel>` per band,
// the tabbed `<InsetWindow>` chrome around the hero trace illustration, and a
// single thin `<ConnectorLine>` stitching "see the trace" to "click the row"
// between band 02 and band 03.
//
// No `<DottedGridBg>` is added: this is a dataviz-heavy page, the existing
// SVG mocks would alias against a dotted lattice and produce moire.

const tracesPillar = PILLARS.find((p) => p.key === "traces")!;
const errorsPillar = PILLARS.find((p) => p.key === "errors")!;
const replayPillar = PILLARS.find((p) => p.key === "replay")!;
const schemaPillar = PILLARS.find((p) => p.key === "schema-diffs")!;
const agentsPillar = PILLARS.find((p) => p.key === "agents")!;

const ACT_PAD_TOP = 64;

export const ObservabilityCinematicRoot: FC = () => {
  return (
    <ObservabilityRoot>
      {/* 01 Hero glow band, ActLabel above the existing hero chrome. The hero
          retains its layered DashboardComposite + Hemisphere; cinematic
          treatment sits in the gutter, not on top of the illustration. */}
      <Band variant="glow" glowFrom="bottom-right">
        <ActLabel n="01" name="Observability" />
        <div style={{ paddingTop: ACT_PAD_TOP }}>
          <ObservabilityHero />
        </div>
      </Band>

      {/* 02 + 03 connector layer. Bands 02 and 03 share a `position: relative`
          ancestor tagged `data-cc-connector-layer` so the single thin curve
          stitching "see the trace" to "click the row" can resolve a shared
          coordinate space across the two sections. */}
      <div
        data-cc-connector-layer
        style={{ position: "relative", width: "100%" }}
      >
        {/* 02 Federation traces, hero illustration wrapped in <InsetWindow>
            tabbed chrome. The dense waterfall sits in the viz slot; tabs hint
            at the surrounding feature surface (errors, replay) without the page
            having to render those panels here. The bottom-of-trace anchor is
            the start point for the connector to band 03. */}
        <Band variant="tinted" ariaLabel="Federation-aware traces">
          <div className="cc-obs-tint-scope">
            <ActLabel n="02" name="Trace the whole graph" />
            <div style={{ paddingTop: ACT_PAD_TOP }}>
              <InsetWindow
                tabs={[
                  { id: "trace", label: "Trace waterfall" },
                  { id: "errors", label: "Errors" },
                  { id: "replay", label: "Replay" },
                ]}
                title={tracesPillar.headline}
                body={tracesPillar.sub}
                bullets={tracesPillar.bullets as string[]}
                viz={
                  <div style={{ width: "100%", position: "relative" }}>
                    <TraceWaterfall
                      spans={DENSE_TRACE}
                      monoLane
                      totalLabel="0ms · 600ms"
                      axisMs={[0, 150, 300, 450, 600]}
                    />
                    <div
                      aria-hidden
                      style={{
                        position: "absolute",
                        left: "50%",
                        bottom: 0,
                        transform: "translateX(-50%)",
                      }}
                    >
                      <Anchor id="hero-trace-end" />
                    </div>
                  </div>
                }
              />
            </div>
          </div>
        </Band>

        {/* 03 Origin-tagged errors, ActLabel + breakout 3-pane mock. The first
            row anchor terminates the connector started in band 02. */}
        <Band ariaLabel="Origin-tagged errors">
          <ActLabel n="03" name="Origin-tagged errors" />
          <div style={{ paddingTop: ACT_PAD_TOP, position: "relative" }}>
            <Anchor id="errors-first-row" />
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
          </div>
        </Band>

        <div style={{ opacity: 0.18, pointerEvents: "none" }} aria-hidden>
          <ConnectorLine
            from="hero-trace-end"
            to="errors-first-row"
            curve="bezier"
            tone="accent-shi"
            weight="hairline"
          />
        </div>
      </div>

      {/* 04 Replay, accent band, ActLabel + side-by-side replay panes. */}
      <Band variant="accent" ariaLabel="Query replay">
        <ActLabel n="04" name="Query replay" />
        <div style={{ paddingTop: ACT_PAD_TOP }}>
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
        </div>
      </Band>

      {/* 05 Schema diff & PR check. */}
      <Band ariaLabel="Schema diff and audit">
        <ActLabel n="05" name="Schema diffs & audit" />
        <div style={{ paddingTop: ACT_PAD_TOP }}>
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
        </div>
      </Band>

      {/* 06 Trust strip, tinted band, 3 columns. */}
      <Band variant="tinted" ariaLabel="Trust strip">
        <div className="cc-obs-tint-scope">
          <ActLabel n="06" name="Trust" />
          <div style={{ paddingTop: ACT_PAD_TOP }}>
            <div
              className="cc-section-label"
              aria-hidden
              style={{ marginBottom: 28 }}
            >
              <span className="num">06</span> Trust
            </div>
            <TrustStrip />
          </div>
        </div>
      </Band>

      {/* 07 OTEL & integrations. */}
      <Band ariaLabel="OTEL and integrations">
        <ActLabel n="07" name="OTEL & integrations" />
        <div style={{ paddingTop: ACT_PAD_TOP }}>
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
        </div>
      </Band>

      {/* 08 MCP for agents, INVERTED band, the page's elevated beat. */}
      <Band variant="inverted" ariaLabel="MCP for agents">
        <ActLabel n="08" name="MCP for agents" />
        <div style={{ paddingTop: ACT_PAD_TOP }}>
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
        </div>
      </Band>

      {/* 09 Final CTA, glow band. */}
      <Band variant="glow" glowFrom="top-left" ariaLabel="Get started">
        <ActLabel n="09" name="Get started" align="center" />
        <div style={{ paddingTop: ACT_PAD_TOP }}>
          <ObservabilityFinalCta />
        </div>
      </Band>
    </ObservabilityRoot>
  );
};
