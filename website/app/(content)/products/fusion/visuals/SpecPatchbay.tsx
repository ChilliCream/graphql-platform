"use client";

import { useRef } from "react";

import { TYPE, svgLabelGap, svgLabelSize } from "../tokens";
import {
  anim,
  useCycle,
  useNarrowViewport,
  useSceneMotion,
  useSvgLabelScale,
} from "./hooks";
import { wrapWords } from "./lines";
import { useSceneRatio } from "./Scene";
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

/**
 * Below 1024px the channel strip and the bus stack in a single column
 * (the bus below the strip instead of beside it) so every label keeps its
 * natural glyph width; the per-row and per-legend-line arithmetic below is
 * unchanged, since it was already sized for an unpinned, natural-width
 * label. `MOBILE_W` matches this panel's measured rendered width at 375px.
 */
const MOBILE_W = 333;
const STRIP_M = { x: 16, w: MOBILE_W - 32 } as const;
const STRIP_M_BOTTOM = 44 + 7 * 52;
const BUS_M = {
  x: 16,
  y: STRIP_M_BOTTOM + 40,
  w: MOBILE_W - 32,
  /**
   * Tight to its own content (title, "ONE GATEWAY", the four-line spec
   * legend, the footer caption): the desktop `BUS.h` (400) is sized for a
   * bus that sits beside a taller channel strip, which this stacked mobile
   * column doesn't have, so reusing it left an empty band above the footer.
   */
  h: 300,
} as const;
const MOBILE_H = BUS_M.y + BUS_M.h + 20;
const MOBILE_RATIO = `${MOBILE_W} / ${MOBILE_H}`;

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
  const desktopScale = useSvgLabelScale(svgRef, W);
  const mobile = useNarrowViewport();
  const mobileScale = useSvgLabelScale(svgRef, MOBILE_W);
  const scale = mobile ? mobileScale : desktopScale;
  useSceneRatio(mobile ? MOBILE_RATIO : null);
  const label = svgLabelSize(TYPE.label, scale);
  const caption = svgLabelSize(TYPE.caption, scale);
  /**
   * A sidebar-narrow desktop slot boosts `label` here the same way a narrow
   * viewport does. The footer caption fits the bus's own width at `label`'s
   * un-boosted size, but not once boosted; past that it wraps onto two
   * lines instead of running past the bus rect's edges.
   */
  const crowded = !mobile && scale < 1;
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
  const strip = mobile ? STRIP_M : STRIP;
  const bus = mobile ? BUS_M : BUS;
  const footerText = repatched ? "NO CUTOVER NEEDED" : "COMPOSED IN THE BUILD";
  const footerLines = crowded ? wrapWords(footerText, 12) : [footerText];
  const footerLineGap = svgLabelGap(16, label, TYPE.label);
  /**
   * When crowded, the wrap grows upward from the box's fixed footer slot
   * (`bus.y + bus.h - 22`) so the last line keeps that same baseline
   * instead of pushing past the bus rect.
   */
  const footerFirstY =
    bus.y + bus.h - 22 - (footerLines.length - 1) * footerLineGap;

  return (
    <svg
      ref={svgRef}
      viewBox={`0 0 ${mobile ? MOBILE_W : W} ${mobile ? MOBILE_H : H}`}
      className="h-full w-full"
    >
      <style>{KEYFRAMES}</style>
      <rect
        width={mobile ? MOBILE_W : W}
        height={mobile ? MOBILE_H : H}
        fill={MC.bg}
      />
      <text
        x={strip.x}
        y={24}
        fill={MC.dim}
        fontFamily={MC.mono}
        fontSize={label}
        letterSpacing="0.2em"
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
              x={strip.x}
              y={y}
              width={strip.w}
              height={rowBoxHeight}
              rx="7"
              fill={MC.panel}
              stroke={active ? MC.phosphor : MC.panelEdge}
              strokeOpacity={active ? 0.75 : 1}
              strokeDasharray={channel.dashed ? "5 4" : undefined}
              style={{ transition: "stroke 400ms ease" }}
            />
            <circle
              cx={strip.x + 16}
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
              x={strip.x + 32}
              y={y + 17}
              fill={MC.ink}
              fontFamily={MC.mono}
              fontSize={rowNameSize}
              letterSpacing="0.12em"
            >
              {channel.name.toUpperCase()}
            </text>
            <text
              x={strip.x + 32}
              y={y + 17 + rowGap}
              fill={active && repatched ? MC.phosphor : MC.dim}
              fontFamily={MC.mono}
              fontSize={label}
              letterSpacing="0.06em"
              style={{ transition: "fill 400ms ease" }}
            >
              {tag}
            </text>
            <text
              x={strip.x + strip.w - 10}
              y={y + 17}
              fill={active ? MC.phosphor : MC.dim}
              fontFamily={MC.mono}
              fontSize={label}
              letterSpacing="0.06em"
              textAnchor="end"
              style={{ transition: "fill 400ms ease" }}
            >
              {statusText}
            </text>

            <path
              d={
                mobile
                  ? `M${strip.x + strip.w / 2} ${y + rowBoxHeight}V${bus.y}`
                  : `M${strip.x + strip.w} ${y + 20}C${strip.x + strip.w + 60} ${y + 20} ${bus.x - 60} ${bus.y + bus.h / 2} ${bus.x} ${bus.y + bus.h / 2}`
              }
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
        x={bus.x}
        y={bus.y}
        width={bus.w}
        height={bus.h}
        rx="10"
        fill={MC.panel}
        stroke={MC.phosphor}
        strokeOpacity="0.45"
      />
      <text
        x={bus.x + bus.w / 2}
        y={bus.y + 40}
        fill={MC.ink}
        fontFamily={MC.mono}
        fontSize={caption}
        letterSpacing="0.16em"
        textAnchor="middle"
      >
        COHERENT
      </text>
      <text
        x={bus.x + bus.w / 2}
        y={bus.y + 40 + svgLabelGap(20, caption, TYPE.caption)}
        fill={MC.ink}
        fontFamily={MC.mono}
        fontSize={caption}
        letterSpacing="0.16em"
        textAnchor="middle"
      >
        GRAPH
      </text>
      <text
        x={bus.x + bus.w / 2}
        y={bus.y + 92 + (svgLabelGap(20, caption, TYPE.caption) - 20)}
        fill={MC.dim}
        fontFamily={MC.mono}
        fontSize={label}
        letterSpacing="0.16em"
        textAnchor="middle"
      >
        ONE GATEWAY
      </text>
      <path
        d={`M${bus.x + 20} ${bus.y + 112 + (svgLabelGap(20, caption, TYPE.caption) - 20)}H${bus.x + bus.w - 20}`}
        stroke={MC.panelEdge}
      />
      {["GRAPHQL FED", "APOLLO FED", "OPENAPI", "GRPC"].map((spec, i) => {
        const legendText = `${spec} ✓`;
        return (
          <text
            key={spec}
            x={bus.x + bus.w / 2}
            y={
              bus.y +
              142 +
              (svgLabelGap(20, caption, TYPE.caption) - 20) +
              i * legendGap
            }
            fill={MC.dim}
            fontFamily={MC.mono}
            fontSize={label}
            letterSpacing="0.16em"
            textAnchor="middle"
          >
            {legendText}
          </text>
        );
      })}
      <text
        x={bus.x + bus.w / 2}
        y={footerFirstY}
        fill={MC.phosphor}
        fontFamily={MC.mono}
        fontSize={label}
        letterSpacing="0.06em"
        textAnchor="middle"
      >
        {crowded
          ? footerLines.map((line, li) => (
              <tspan
                key={li}
                x={bus.x + bus.w / 2}
                dy={li === 0 ? 0 : footerLineGap}
              >
                {line}
              </tspan>
            ))
          : footerText}
      </text>
    </svg>
  );
}
