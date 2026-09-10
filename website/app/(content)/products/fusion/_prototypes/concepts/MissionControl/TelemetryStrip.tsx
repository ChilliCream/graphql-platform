"use client";

import { anim, useSceneMotion } from "./hooks";
import { MC, STATIONS } from "./palette";

/**
 * Nitro band visual: the telemetry wall. A latency trace for the gateway runs
 * across the top and the rows below hold latency, throughput and error rate for
 * the gateway and for each subgraph behind it, the way the Fusion dashboard
 * reports them.
 */

const W = 560;
const H = 320;
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
    meta: "composite schema",
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

const KEYFRAMES = `
@keyframes mc-tele-trace { from { stroke-dashoffset: 40; } to { stroke-dashoffset: 0; } }
@keyframes mc-tele-bar { 0%, 100% { transform: scaleX(0.82); } 50% { transform: scaleX(1); } }
@keyframes mc-tele-live { 0%, 100% { opacity: 1; } 50% { opacity: 0.35; } }
`;

export function TelemetryStrip() {
  const running = useSceneMotion();

  return (
    <svg viewBox={`0 0 ${W} ${H}`} className="h-full w-full">
      <style>{KEYFRAMES}</style>
      <rect width={W} height={H} fill={MC.bg} />

      <text
        x={TRACE.x}
        y={18}
        fill={MC.dim}
        fontFamily={MC.mono}
        fontSize="9"
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
        y={18}
        fill={MC.phosphor}
        fontFamily={MC.mono}
        fontSize="9"
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
              fontSize="11"
              letterSpacing="0.14em"
            >
              {row.label}
            </text>
            <text
              x={TRACE.x + 108}
              y={y}
              fill={MC.dim}
              fontFamily={MC.mono}
              fontSize="9"
              letterSpacing="0.1em"
            >
              {row.meta}
            </text>
            <text
              x={BAR.x - 12}
              y={y}
              fill={MC.ink}
              fontFamily={MC.mono}
              fontSize="10"
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
              fontSize="10"
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
        fontSize="9"
        letterSpacing="0.16em"
      >
        LATENCY · THROUGHPUT · ERROR RATE, PER SUBGRAPH
      </text>
    </svg>
  );
}
