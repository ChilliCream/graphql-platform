"use client";

import { useRef } from "react";

import { TYPE, svgLabelGap, svgLabelSize, svgLabelWidth } from "../tokens";
import { anim, useCycle, useSceneMotion, useSvgLabelScale } from "./hooks";
import { MC, SOURCES, STATIONS, specTag } from "../palette";
import type { StationSpec } from "../palette";

/**
 * Animated console that patches every station into one graph bus
 * regardless of source specification, then re-patches one channel from Apollo
 * Federation to the GraphQL Federation specification while its link stays green.
 */

const W = 640;
const H = 460;
/** The scene box mirrors the viewBox, so the patch bay never letterboxes. */
export const SPEC_PATCHBAY_RATIO = `${W} / ${H}`;
/** 0 is the rest frame: every channel patched, nothing being moved. */
const PHASES = 6;
const REST = 0;
const BEAT = 1600;
/** Shipping is the channel that moves across. */
const MOVED = 3;

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
  const svgRef = useRef<SVGSVGElement>(null);
  const scale = useSvgLabelScale(svgRef, W);
  const label = svgLabelSize(TYPE.label, scale);
  const caption = svgLabelSize(TYPE.caption, scale);
  /** The legend's four lines stack readably instead of crowding once boosted. */
  const legendGap = svgLabelGap(24, label, TYPE.label, 1.5);
  /**
   * The channel name's size: `caption` everywhere there's room, but capped
   * at the same absolute floor as `label` inside the seven-row strip, whose
   * fixed-height column has no slack for `caption`'s full, proportionally
   * larger boost.
   */
  const rowNameSize = scale >= 1 ? caption : label;
  /** Gap between a channel row's name and its tag line, below the boost. */
  const rowGap = scale >= 1 ? 14 : svgLabelGap(14, label, TYPE.label, 1.1);
  /**
   * Row-to-row stride: the row's own (boosted) box height, plus the gap to
   * the next box, trimmed only when boosted to leave room for that growth
   * inside the fixed `H` — seven rows share this strip's column.
   */
  const rowBoxHeight = 40 + (rowGap - 14);
  const interRowGap = scale >= 1 ? 12 : 6;
  const rowStride = rowBoxHeight + interRowGap;
  const stripTop = scale >= 1 ? 44 : 40;
  const rowY = (i: number) => stripTop + i * rowStride;

  return (
    <svg ref={svgRef} viewBox={`0 0 ${W} ${H}`} className="h-full w-full">
      <style>{KEYFRAMES}</style>
      <rect width={W} height={H} fill={MC.bg} />
      <text
        x={STRIP.x}
        y={24}
        fill={MC.dim}
        fontFamily={MC.mono}
        fontSize={label}
        letterSpacing="0.2em"
        textLength={svgLabelWidth("SUBGRAPH CHANNELS", TYPE.label, scale, 0.2)}
        lengthAdjust="spacingAndGlyphs"
      >
        SUBGRAPH CHANNELS
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
        const statusText = active
          ? repatched
            ? "RE-PATCHED · ONLINE"
            : "MOVING · ONLINE"
          : "ONLINE";

        return (
          <g key={channel.name}>
            <rect
              x={STRIP.x}
              y={y}
              width={STRIP.w}
              height={rowBoxHeight}
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
              fontSize={rowNameSize}
              letterSpacing="0.12em"
              textLength={svgLabelWidth(
                channel.name.toUpperCase(),
                TYPE.caption,
                scale,
                0.12,
              )}
              lengthAdjust="spacingAndGlyphs"
            >
              {channel.name.toUpperCase()}
            </text>
            <text
              x={STRIP.x + 32}
              y={y + 17 + rowGap}
              fill={active && repatched ? MC.phosphor : MC.dim}
              fontFamily={MC.mono}
              fontSize={label}
              letterSpacing="0.06em"
              textLength={svgLabelWidth(tag, TYPE.label, scale, 0.06)}
              lengthAdjust="spacingAndGlyphs"
              style={{ transition: "fill 400ms ease" }}
            >
              {tag}
            </text>
            <text
              x={STRIP.x + STRIP.w - 10}
              y={y + 17}
              fill={active ? MC.phosphor : MC.dim}
              fontFamily={MC.mono}
              fontSize={label}
              letterSpacing="0.06em"
              textAnchor="end"
              textLength={svgLabelWidth(statusText, TYPE.label, scale, 0.06)}
              lengthAdjust="spacingAndGlyphs"
              style={{ transition: "fill 400ms ease" }}
            >
              {statusText}
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
        fontSize={caption}
        letterSpacing="0.16em"
        textAnchor="middle"
        textLength={svgLabelWidth("COHERENT", TYPE.caption, scale, 0.16)}
        lengthAdjust="spacingAndGlyphs"
      >
        COHERENT
      </text>
      <text
        x={BUS.x + BUS.w / 2}
        y={BUS.y + 40 + svgLabelGap(20, caption, TYPE.caption)}
        fill={MC.ink}
        fontFamily={MC.mono}
        fontSize={caption}
        letterSpacing="0.16em"
        textAnchor="middle"
        textLength={svgLabelWidth("GRAPH", TYPE.caption, scale, 0.16)}
        lengthAdjust="spacingAndGlyphs"
      >
        GRAPH
      </text>
      <text
        x={BUS.x + BUS.w / 2}
        y={BUS.y + 92 + (svgLabelGap(20, caption, TYPE.caption) - 20)}
        fill={MC.dim}
        fontFamily={MC.mono}
        fontSize={label}
        letterSpacing="0.16em"
        textAnchor="middle"
        textLength={svgLabelWidth("ONE GATEWAY", TYPE.label, scale, 0.16)}
        lengthAdjust="spacingAndGlyphs"
      >
        ONE GATEWAY
      </text>
      <path
        d={`M${BUS.x + 20} ${BUS.y + 112 + (svgLabelGap(20, caption, TYPE.caption) - 20)}H${BUS.x + BUS.w - 20}`}
        stroke={MC.panelEdge}
      />
      {["GRAPHQL FED", "APOLLO FED", "OPENAPI", "GRPC"].map((spec, i) => {
        const legendText = `${spec} ✓`;
        return (
          <text
            key={spec}
            x={BUS.x + BUS.w / 2}
            y={
              BUS.y +
              142 +
              (svgLabelGap(20, caption, TYPE.caption) - 20) +
              i * legendGap
            }
            fill={MC.dim}
            fontFamily={MC.mono}
            fontSize={label}
            letterSpacing="0.16em"
            textAnchor="middle"
            textLength={svgLabelWidth(legendText, TYPE.label, scale, 0.16)}
            lengthAdjust="spacingAndGlyphs"
          >
            {legendText}
          </text>
        );
      })}
      <text
        x={BUS.x + BUS.w / 2}
        y={BUS.y + BUS.h - 22}
        fill={MC.phosphor}
        fontFamily={MC.mono}
        fontSize={label}
        letterSpacing="0.06em"
        textAnchor="middle"
        textLength={svgLabelWidth(
          repatched ? "NO CUTOVER NEEDED" : "COMPOSED IN THE BUILD",
          TYPE.label,
          scale,
          0.06,
        )}
        lengthAdjust="spacingAndGlyphs"
      >
        {repatched ? "NO CUTOVER NEEDED" : "COMPOSED IN THE BUILD"}
      </text>
    </svg>
  );
}
