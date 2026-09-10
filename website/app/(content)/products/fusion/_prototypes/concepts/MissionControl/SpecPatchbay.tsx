"use client";

import { anim, useCycle, useSceneMotion } from "./hooks";
import { MC, SOURCES, STATIONS, specTag } from "./palette";
import type { StationSpec } from "./palette";

/**
 * "Both specifications, one gateway": the patch bay. Every station is patched
 * into the same composite schema bus whichever specification its source schema
 * is written to, and mid-cycle one channel is re-patched from Apollo Federation
 * to the GraphQL Federation specification while its link light stays green.
 */

const W = 640;
const H = 460;
/** 0 is the rest frame: every channel patched, nothing being moved. */
const PHASES = 6;
const REST = 0;
const BEAT = 1600;
/** Shipping is the channel that moves across. */
const MOVED = 3;

const ROW_H = 52;
const rowY = (i: number) => 44 + i * ROW_H;
const STRIP = { x: 16, w: 286 } as const;
const BUS = { x: 452, y: 30, w: 172, h: 400 } as const;

interface Channel {
  readonly name: string;
  /** Language of a GraphQL subgraph; `null` for an OpenAPI or gRPC source. */
  readonly language: string | null;
  readonly spec: StationSpec | null;
  readonly note: string;
  readonly dashed: boolean;
}

const CHANNELS: readonly Channel[] = [
  ...STATIONS.map((station) => ({
    name: station.name,
    language: station.language,
    spec: station.spec,
    note: "",
    dashed: false,
  })),
  ...SOURCES.map((source) => ({
    name: source.name,
    language: null,
    spec: null,
    note: `${source.kind.toUpperCase()} DOCUMENT`,
    dashed: true,
  })),
];

const KEYFRAMES = `
@keyframes mc-bay-flow { from { stroke-dashoffset: 24; } to { stroke-dashoffset: 0; } }
@keyframes mc-bay-lamp { 0%, 100% { opacity: 1; } 50% { opacity: 0.45; } }
`;

export function SpecPatchbay() {
  const running = useSceneMotion();
  const phase = useCycle(running, PHASES, BEAT, REST);
  const selected = phase >= 1 && phase <= 4;
  const repatched = phase >= 3;

  return (
    <svg viewBox={`0 0 ${W} ${H}`} className="h-full w-full">
      <style>{KEYFRAMES}</style>
      <rect width={W} height={H} fill={MC.bg} />
      <text
        x={STRIP.x}
        y={24}
        fill={MC.dim}
        fontFamily={MC.mono}
        fontSize="10"
        letterSpacing="0.2em"
      >
        SOURCE SCHEMA CHANNELS
      </text>

      {CHANNELS.map((channel, i) => {
        const y = rowY(i);
        const active = selected && i === MOVED;
        const spec: StationSpec | null =
          active && repatched ? "GraphQL Federation" : channel.spec;
        const tag =
          spec === null
            ? channel.note
            : `${channel.language} · ${specTag(spec)}`;

        return (
          <g key={channel.name}>
            <rect
              x={STRIP.x}
              y={y}
              width={STRIP.w}
              height={ROW_H - 12}
              rx="7"
              fill={MC.panel}
              stroke={active ? MC.phosphor : MC.panelEdge}
              strokeOpacity={active ? 0.75 : 1}
              strokeDasharray={channel.dashed ? "5 4" : undefined}
              style={{ transition: "stroke 400ms ease" }}
            />
            <circle
              cx={STRIP.x + 16}
              cy={y + 20}
              r="4"
              fill={MC.phosphor}
              style={{
                animation: anim(
                  running,
                  `mc-bay-lamp 2200ms ease-in-out ${i * 180}ms infinite`,
                ),
              }}
            />
            <text
              x={STRIP.x + 32}
              y={y + 17}
              fill={MC.ink}
              fontFamily={MC.mono}
              fontSize="12"
              letterSpacing="0.12em"
            >
              {channel.name.toUpperCase()}
            </text>
            <text
              x={STRIP.x + 32}
              y={y + 31}
              fill={active && repatched ? MC.phosphor : MC.dim}
              fontFamily={MC.mono}
              fontSize="9"
              letterSpacing="0.14em"
              style={{ transition: "fill 400ms ease" }}
            >
              {tag}
            </text>
            <text
              x={STRIP.x + STRIP.w - 10}
              y={y + 24}
              fill={active ? MC.phosphor : MC.dim}
              fontFamily={MC.mono}
              fontSize="9"
              letterSpacing="0.14em"
              textAnchor="end"
              style={{ transition: "fill 400ms ease" }}
            >
              {active
                ? repatched
                  ? "RE-PATCHED · ONLINE"
                  : "MOVING · ONLINE"
                : "ONLINE"}
            </text>

            <path
              d={`M${STRIP.x + STRIP.w} ${y + 20}C${STRIP.x + STRIP.w + 60} ${y + 20} ${BUS.x - 60} ${BUS.y + BUS.h / 2} ${BUS.x} ${BUS.y + BUS.h / 2}`}
              fill="none"
              stroke={active ? MC.phosphor : MC.line}
              strokeOpacity={active ? 0.8 : 0.4}
              strokeDasharray={channel.dashed ? "5 4" : "4 8"}
              style={{
                animation: anim(
                  running,
                  `mc-bay-flow ${channel.dashed ? 1600 : 1200}ms linear infinite`,
                ),
                transition: "stroke 400ms ease",
              }}
            />
          </g>
        );
      })}

      <rect
        x={BUS.x}
        y={BUS.y}
        width={BUS.w}
        height={BUS.h}
        rx="10"
        fill={MC.panel}
        stroke={MC.phosphor}
        strokeOpacity="0.45"
      />
      <text
        x={BUS.x + BUS.w / 2}
        y={BUS.y + 40}
        fill={MC.ink}
        fontFamily={MC.mono}
        fontSize="13"
        letterSpacing="0.16em"
        textAnchor="middle"
      >
        COMPOSITE
      </text>
      <text
        x={BUS.x + BUS.w / 2}
        y={BUS.y + 60}
        fill={MC.ink}
        fontFamily={MC.mono}
        fontSize="13"
        letterSpacing="0.16em"
        textAnchor="middle"
      >
        SCHEMA
      </text>
      <text
        x={BUS.x + BUS.w / 2}
        y={BUS.y + 92}
        fill={MC.dim}
        fontFamily={MC.mono}
        fontSize="9"
        letterSpacing="0.16em"
        textAnchor="middle"
      >
        ONE GATEWAY
      </text>
      <path
        d={`M${BUS.x + 20} ${BUS.y + 112}H${BUS.x + BUS.w - 20}`}
        stroke={MC.panelEdge}
      />
      {["GRAPHQL FED", "APOLLO FED", "OPENAPI", "GRPC"].map((label, i) => (
        <text
          key={label}
          x={BUS.x + BUS.w / 2}
          y={BUS.y + 142 + i * 24}
          fill={MC.dim}
          fontFamily={MC.mono}
          fontSize="9"
          letterSpacing="0.16em"
          textAnchor="middle"
        >
          {`${label} ✓`}
        </text>
      ))}
      <text
        x={BUS.x + BUS.w / 2}
        y={BUS.y + BUS.h - 22}
        fill={MC.phosphor}
        fontFamily={MC.mono}
        fontSize="9"
        letterSpacing="0.14em"
        textAnchor="middle"
      >
        {repatched ? "NO CUTOVER NEEDED" : "COMPOSED IN THE BUILD"}
      </text>
    </svg>
  );
}
