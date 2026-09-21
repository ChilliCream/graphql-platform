"use client";

import { useRef } from "react";

import { TYPE, svgLabelSize } from "../tokens";
import { anim, useSceneMotion, useSvgLabelScale } from "./hooks";
import { useSceneRatio } from "./Scene";
import { MC, STATIONS } from "../palette";

/**
 * Animated telemetry strip with a gateway latency trace across the top and
 * rows of latency, throughput and error rate for the gateway and each subgraph.
 */

const W = 560;
const H = 320;
/** The scene box mirrors the viewBox, so the wall never letterboxes. */
export const TELEMETRY_STRIP_RATIO = `${W} / ${H}`;
const TRACE = { x: 16, y: 28, w: W - 32, h: 76 } as const;

/** Deterministic sawtooth trace so the server and client render the same path. */
const TRACE_POINTS = Array.from({ length: 33 }, (_, i) => {
  const t = i / 32;
  const wave =
    Math.sin(i * 0.9) * 0.28 +
    Math.sin(i * 0.31) * 0.34 +
    Math.cos(i * 1.7) * 0.14;
  return {
    x: TRACE.x + t * TRACE.w,
    y: TRACE.y + TRACE.h / 2 - wave * (TRACE.h / 2 - 8),
  };
});

const TRACE_D = TRACE_POINTS.map(
  (point, i) =>
    `${i === 0 ? "M" : "L"}${point.x.toFixed(1)} ${point.y.toFixed(1)}`,
).join("");

/**
 * Below 1024px the strip is narrower still (it sits in a sidebar slot), so
 * every row's five columns (label, meta, latency, bar, error rate) stack
 * into three lines instead of sitting side by side across the whole width.
 * `MOBILE_W` matches this panel's measured rendered width at 375px.
 */
const MOBILE_W = 267;
const MOBILE_TRACE = { x: 16, y: 58, w: MOBILE_W - 32, h: 60 } as const;
const MOBILE_ROW_Y = MOBILE_TRACE.y + MOBILE_TRACE.h + 34;
const MOBILE_ROW_STRIDE = 64;
const MOBILE_BAR = { x: 16, w: MOBILE_W - 32, h: 4 } as const;
const mobileRowY = (i: number) => MOBILE_ROW_Y + i * MOBILE_ROW_STRIDE;

interface Row {
  readonly label: string;
  readonly meta: string;
  readonly latency: string;
  readonly load: number;
  readonly errors: string;
}

const ROWS: readonly Row[] = [
  {
    label: "GATEWAY",
    meta: "coherent graph",
    latency: "42 ms",
    load: 0.86,
    errors: "0.01%",
  },
  ...STATIONS.slice(0, 4).map((station, i) => ({
    label: station.name.toUpperCase(),
    meta: station.language.toLowerCase(),
    latency: `${[11, 24, 17, 33][i]} ms`,
    load: [0.42, 0.68, 0.51, 0.74][i],
    errors: ["0.00%", "0.02%", "0.00%", "0.04%"][i],
  })),
];

const ROW_Y = 132;
const ROW_H = 36;
const BAR = { x: 250, w: 150, h: 6 } as const;

const MOBILE_FOOTER_Y = mobileRowY(ROWS.length - 1) + 46;
const MOBILE_H = MOBILE_FOOTER_Y + 3 * 16 + 20;
const MOBILE_RATIO = `${MOBILE_W} / ${MOBILE_H}`;

const KEYFRAMES = `
@keyframes mc-tele-trace { from { stroke-dashoffset: 40; } to { stroke-dashoffset: 0; } }
@keyframes mc-tele-bar { 0%, 100% { transform: scaleX(0.82); } 50% { transform: scaleX(1); } }
@keyframes mc-tele-live { 0%, 100% { opacity: 1; } 50% { opacity: 0.35; } }
`;

/** Splits `text` at every ` · ` separator, each line keeping its leading
 * separator so the lines' concatenated content is `text` again. */
function dotLines(text: string): readonly string[] {
  const parts = text.split(" · ");
  return parts.map((part, i) => (i === 0 ? part : ` · ${part}`));
}

export function TelemetryStrip() {
  const running = useSceneMotion();
  const svgRef = useRef<SVGSVGElement>(null);
  const desktopScale = useSvgLabelScale(svgRef, W);
  const mobile = desktopScale < 1;
  const mobileScale = useSvgLabelScale(svgRef, MOBILE_W);
  const scale = mobile ? mobileScale : desktopScale;
  useSceneRatio(mobile ? MOBILE_RATIO : null);
  const label = svgLabelSize(TYPE.label, scale);
  /** The header row's baseline, nudged down so its boosted ascent clears
   * the SVG's own top edge instead of clipping against it. */
  const headerY = 18 + Math.max(0, label - TYPE.label) * 0.3;

  if (mobile) {
    const trace = MOBILE_TRACE;
    return (
      <svg
        ref={svgRef}
        viewBox={`0 0 ${MOBILE_W} ${MOBILE_H}`}
        className="h-full w-full"
      >
        <style>{KEYFRAMES}</style>
        <rect width={MOBILE_W} height={MOBILE_H} fill={MC.bg} />

        <text
          x={trace.x}
          y={20}
          fill={MC.dim}
          fontFamily={MC.mono}
          fontSize={label}
          letterSpacing="0.2em"
        >
          GATEWAY LATENCY · LAST 60 s
        </text>
        <circle
          cx={trace.x + 6}
          cy={34}
          r="3"
          fill={MC.phosphor}
          style={{
            animation: anim(
              running,
              "mc-tele-live 1600ms ease-in-out infinite",
            ),
          }}
        />
        <text
          x={trace.x + 16}
          y={38}
          fill={MC.phosphor}
          fontFamily={MC.mono}
          fontSize={label}
          letterSpacing="0.2em"
        >
          LIVE
        </text>

        <rect
          x={trace.x}
          y={trace.y}
          width={trace.w}
          height={trace.h}
          rx="7"
          fill={MC.panel}
          stroke={MC.panelEdge}
        />
        <path
          d={TRACE_D}
          fill="none"
          stroke={MC.phosphor}
          strokeOpacity="0.85"
          strokeDasharray="6 4"
          style={{
            animation: anim(running, "mc-tele-trace 1800ms linear infinite"),
          }}
        />

        {ROWS.map((row, i) => {
          const y = mobileRowY(i);
          const gateway = i === 0;
          return (
            <g key={row.label}>
              <text
                x={16}
                y={y}
                fill={gateway ? MC.ink : MC.dim}
                fontFamily={MC.mono}
                fontSize={label}
                letterSpacing="0.08em"
              >
                {row.label}
              </text>
              <text
                x={MOBILE_W - 16}
                y={y}
                fill={MC.ink}
                fontFamily={MC.mono}
                fontSize={label}
                textAnchor="end"
              >
                {row.latency}
              </text>
              <text
                x={16}
                y={y + 16}
                fill={MC.dim}
                fontFamily={MC.mono}
                fontSize={label}
              >
                {row.meta}
              </text>
              <text
                x={MOBILE_W - 16}
                y={y + 16}
                fill={MC.dim}
                fontFamily={MC.mono}
                fontSize={label}
                textAnchor="end"
              >
                {row.errors}
              </text>
              <rect
                x={MOBILE_BAR.x}
                y={y + 22}
                width={MOBILE_BAR.w}
                height={MOBILE_BAR.h}
                rx="2"
                fill={MC.panelEdge}
              />
              <rect
                x={MOBILE_BAR.x}
                y={y + 22}
                width={MOBILE_BAR.w * row.load}
                height={MOBILE_BAR.h}
                rx="2"
                fill={gateway ? MC.phosphor : MC.signal}
                style={{
                  transformBox: "fill-box",
                  transformOrigin: "left center",
                  animation: anim(
                    running,
                    `mc-tele-bar ${3200 + i * 260}ms ease-in-out ${i * 180}ms infinite`,
                  ),
                }}
              />
            </g>
          );
        })}

        <text
          x={16}
          y={MOBILE_FOOTER_Y}
          fill={MC.dim}
          fontFamily={MC.mono}
          fontSize={label}
          letterSpacing="0.16em"
        >
          {dotLines("LATENCY · THROUGHPUT · ERROR RATE, PER SUBGRAPH").map(
            (line, i) => (
              <tspan key={i} x={16} dy={i === 0 ? 0 : 16}>
                {line}
              </tspan>
            ),
          )}
        </text>
      </svg>
    );
  }

  return (
    <svg ref={svgRef} viewBox={`0 0 ${W} ${H}`} className="h-full w-full">
      <style>{KEYFRAMES}</style>
      <rect width={W} height={H} fill={MC.bg} />

      <text
        x={TRACE.x}
        y={headerY}
        fill={MC.dim}
        fontFamily={MC.mono}
        fontSize={label}
        letterSpacing="0.2em"
      >
        GATEWAY LATENCY · LAST 60 s
      </text>
      <circle
        cx={W - 60}
        cy={14}
        r="3"
        fill={MC.phosphor}
        style={{
          animation: anim(running, "mc-tele-live 1600ms ease-in-out infinite"),
        }}
      />
      <text
        x={W - 50}
        y={headerY}
        fill={MC.phosphor}
        fontFamily={MC.mono}
        fontSize={label}
        letterSpacing="0.2em"
      >
        LIVE
      </text>

      <rect
        x={TRACE.x}
        y={TRACE.y}
        width={TRACE.w}
        height={TRACE.h}
        rx="7"
        fill={MC.panel}
        stroke={MC.panelEdge}
      />
      <path
        d={TRACE_D}
        fill="none"
        stroke={MC.phosphor}
        strokeOpacity="0.85"
        strokeDasharray="6 4"
        style={{
          animation: anim(running, "mc-tele-trace 1800ms linear infinite"),
        }}
      />

      {ROWS.map((row, i) => {
        const y = ROW_Y + i * ROW_H;
        const gateway = i === 0;
        return (
          <g key={row.label}>
            <text
              x={TRACE.x}
              y={y}
              fill={gateway ? MC.ink : MC.dim}
              fontFamily={MC.mono}
              fontSize={label}
              letterSpacing="0.08em"
            >
              {row.label}
            </text>
            <text
              x={TRACE.x + 68}
              y={y}
              fill={MC.dim}
              fontFamily={MC.mono}
              fontSize={label}
            >
              {row.meta}
            </text>
            <text
              x={BAR.x - 6}
              y={y}
              fill={MC.ink}
              fontFamily={MC.mono}
              fontSize={label}
              textAnchor="end"
            >
              {row.latency}
            </text>
            <rect
              x={BAR.x}
              y={y - 8}
              width={BAR.w}
              height={BAR.h}
              rx="3"
              fill={MC.panelEdge}
            />
            <rect
              x={BAR.x}
              y={y - 8}
              width={BAR.w * row.load}
              height={BAR.h}
              rx="3"
              fill={gateway ? MC.phosphor : MC.signal}
              style={{
                transformBox: "fill-box",
                transformOrigin: "left center",
                animation: anim(
                  running,
                  `mc-tele-bar ${3200 + i * 260}ms ease-in-out ${i * 180}ms infinite`,
                ),
              }}
            />
            <text
              x={W - 16}
              y={y}
              fill={MC.dim}
              fontFamily={MC.mono}
              fontSize={label}
              textAnchor="end"
            >
              {row.errors}
            </text>
          </g>
        );
      })}

      <text
        x={TRACE.x}
        y={H - 14}
        fill={MC.dim}
        fontFamily={MC.mono}
        fontSize={label}
        letterSpacing="0.16em"
      >
        LATENCY · THROUGHPUT · ERROR RATE, PER SUBGRAPH
      </text>
    </svg>
  );
}
