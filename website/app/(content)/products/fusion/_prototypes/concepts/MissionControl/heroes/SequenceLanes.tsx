"use client";

import { useRef } from "react";

import { BRAND } from "../../../brand";
import { useElementMotion } from "../hooks";
import { MC, SOURCES, STATIONS, specTag } from "../palette";
import type { StationSpec } from "../palette";

/**
 * Hero visual: the request as a sequence diagram.
 *
 * One lifeline per participant, stacked top to bottom - the four clients (web
 * app, mobile app, partner API, AI agent), the Fusion gateway on the
 * emphasised lane, the five subgraphs (Catalog, Billing, Ordering, Shipping,
 * Accounts) with their language and federation spec, and the two non-GraphQL
 * sources (OpenAPI, gRPC). Time runs left to right: a request drops from a
 * client lane to the gateway, the gateway fans arrows out to the source
 * schemas it needs - all starting at the same x, so the fan-out reads as
 * parallel - the returns come back to the gateway lane, and one merged
 * response arrow goes back up to the client.
 *
 * Sizing: the diagram is DOM text on a fixed lane pitch, not a scaled SVG, so
 * every label renders at its own px size (11px floor) at any viewport width;
 * the lanes column is pushed right of the hero copy from `lg` up.
 */

type LaneKind = "client" | "gateway" | "subgraph" | "source";

interface Lane {
  readonly name: string;
  /** Language, role or protocol line under the name. */
  readonly meta: string;
  readonly spec?: StationSpec;
  readonly kind: LaneKind;
  /** Lane colour: arrows, activation bar and plate rule. */
  readonly accent: string;
}

/** Federation specs, mixed across the subgraphs, each with its own swatch. */
const SPEC_ACCENT: Record<StationSpec, string> = {
  "GraphQL Federation": MC.phosphor,
  "Apollo Federation": BRAND.violet,
};

/**
 * The four clients. The shared `CLIENTS` roster carries only three, under the
 * short console names, so the hero keeps its own list (see heroes/README.md:
 * content the palette lacks stays local to the approach).
 */
const CLIENT_LANES: readonly Lane[] = [
  { name: "Web app", meta: "browser", kind: "client", accent: MC.signal },
  {
    name: "Mobile app",
    meta: "iOS / Android",
    kind: "client",
    accent: MC.signal,
  },
  {
    name: "Partner API",
    meta: "server to server",
    kind: "client",
    accent: MC.signal,
  },
  { name: "AI agent", meta: "tool calls", kind: "client", accent: MC.signal },
];

const GATEWAY_LANE: Lane = {
  name: "Fusion",
  meta: "gateway · composite schema",
  kind: "gateway",
  accent: MC.phosphor,
};

/** Full language names; `STATIONS` carries the short console tags. */
const SUBGRAPH_LANGUAGE: Record<string, string> = {
  Catalog: "TypeScript",
  Billing: "Java",
  Ordering: "Go",
  Shipping: "Ruby",
  Accounts: "C#",
};

const SUBGRAPH_LANES: readonly Lane[] = STATIONS.map((station) => ({
  name: station.name,
  meta: SUBGRAPH_LANGUAGE[station.name] ?? station.language,
  spec: station.spec,
  kind: "subgraph" as const,
  accent: SPEC_ACCENT[station.spec],
}));

/** The languages behind the two non-GraphQL sources complete the mix. */
const SOURCE_LANGUAGE: Record<string, string> = {
  OpenAPI: "Python",
  gRPC: "Go",
};

const SOURCE_LANES: readonly Lane[] = SOURCES.map((source) => ({
  name: source.name,
  meta: `${SOURCE_LANGUAGE[source.kind]} · ${source.kind}`,
  kind: "source" as const,
  accent: MC.amber,
}));

const LANES: readonly Lane[] = [
  ...CLIENT_LANES,
  GATEWAY_LANE,
  ...SUBGRAPH_LANES,
  ...SOURCE_LANES,
];

/** Lane index of the gateway; clients sit above it, source schemas below. */
const GATEWAY = CLIENT_LANES.length;

const laneTop = (lane: number) => `calc(var(--mc-seq-lane) * ${lane + 0.5})`;

const wash = (color: string, percent: number) =>
  `color-mix(in srgb, ${color} ${percent}%, transparent)`;

interface LanePlateProps {
  readonly lane: Lane;
}

/** Lane head: the participant name, its language and its spec badge. */
function LanePlate({ lane }: LanePlateProps) {
  const gateway = lane.kind === "gateway";

  return (
    <div
      className="flex flex-col justify-center gap-px overflow-hidden rounded-md border px-2 py-1 leading-tight"
      style={{
        height: "calc(var(--mc-seq-lane) - 4px)",
        margin: "2px 0",
        background: gateway ? wash(MC.phosphor, 10) : MC.panel,
        borderColor: gateway ? wash(MC.phosphor, 55) : MC.panelEdge,
      }}
    >
      <span
        className="truncate text-[11px] tracking-wide sm:text-[12px] lg:text-[13px]"
        style={{ color: gateway ? MC.phosphor : MC.ink }}
      >
        {lane.name}
      </span>
      <span className="truncate text-[11px]" style={{ color: MC.dim }}>
        {lane.meta}
      </span>
      {lane.spec && (
        <span className="flex items-center gap-1" style={{ color: MC.dim }}>
          <span
            className="size-[5px] shrink-0 rounded-full"
            style={{ background: SPEC_ACCENT[lane.spec] }}
          />
          <span className="hidden truncate text-[11px] lg:inline">
            {lane.spec}
          </span>
          <span className="truncate text-[11px] lg:hidden">
            {specTag(lane.spec)}
          </span>
        </span>
      )}
    </div>
  );
}

export default function SequenceLanes() {
  const ref = useRef<HTMLDivElement>(null);
  useElementMotion(ref);

  return (
    <div
      ref={ref}
      aria-hidden="true"
      className="absolute inset-0 overflow-hidden font-mono [--mc-seq-lane:48px] sm:[--mc-seq-lane:54px] lg:[--mc-seq-lane:56px]"
    >
      <div
        className="absolute inset-0"
        style={{
          backgroundImage: `repeating-linear-gradient(to right, ${MC.grid} 0 1px, transparent 1px 72px)`,
        }}
      />

      <div className="absolute inset-0 flex items-center">
        <div className="flex w-full items-stretch gap-2 px-3 sm:px-5 lg:pr-10 lg:pl-[44%]">
          <div className="w-[136px] shrink-0 sm:w-[168px] lg:w-[200px]">
            {LANES.map((lane) => (
              <LanePlate key={lane.name} lane={lane} />
            ))}
          </div>

          <div className="relative min-w-0 flex-1 overflow-hidden">
            {LANES.map((lane, i) => (
              <div
                key={lane.name}
                className="absolute right-0 left-0 h-px"
                style={{
                  top: laneTop(i),
                  background:
                    i === GATEWAY
                      ? wash(MC.phosphor, 45)
                      : `repeating-linear-gradient(to right, ${MC.line} 0 4px, transparent 4px 10px)`,
                }}
              />
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}
