"use client";

import type { CSSProperties } from "react";

import { useReducedMotionPreference, useSceneActive } from "../../Primitives";
import { DUSK, FONT, LABEL, WAREHOUSES } from "./palette";

/**
 * Nitro band: the harbour beacon sweeps the basin and every berth reports
 * back. The gateway readout carries latency, throughput and error rate, and
 * each warehouse berth behind it keeps its own trace.
 *
 * Rest state: the beacon at rest with every readout already drawn.
 */

const CSS = `
.hbr-beacon [class*="hbr-b-"] { animation-play-state: running; }
.hbr-beacon[data-run="false"] [class*="hbr-b-"] { animation: none; }
.hbr-beacon .hbr-b-beam { animation: hbr-b-beam 11s ease-in-out infinite; transform-box: view-box; transform-origin: 320px 344px; }
.hbr-beacon .hbr-b-bar { animation: hbr-b-bar 4.4s ease-in-out infinite; transform-box: fill-box; transform-origin: bottom; }
.hbr-beacon .hbr-b-lamp { animation: hbr-b-lamp 11s ease-in-out infinite; }
@keyframes hbr-b-beam {
  0%, 100% { transform: rotate(-42deg); opacity: 0.28; }
  50% { transform: rotate(42deg); opacity: 0.5; }
}
@keyframes hbr-b-bar {
  0%, 100% { transform: scaleY(1); }
  50% { transform: scaleY(var(--s, 0.6)); }
}
@keyframes hbr-b-lamp {
  0%, 100% { opacity: 0.5; }
  50% { opacity: 1; }
}
`;

const METRICS = [
  { label: "LATENCY", value: "38 ms" },
  { label: "THROUGHPUT", value: "1.2k rps" },
  { label: "ERROR RATE", value: "0.04%" },
];

/** Per-bar delay and the height it dips to, as CSS custom properties. */
function barStyle(delay: number, scale: number): CSSProperties {
  return {
    animationDelay: `${delay}s`,
    "--s": `${scale}`,
  } as CSSProperties;
}

/** Deterministic traces, so the server and the client draw the same bars. */
const trace = (seed: number) =>
  Array.from({ length: 9 }, (_, i) => 8 + ((seed * 7 + i * 13) % 22));

export function BeaconSweep() {
  const active = useSceneActive();
  const reduced = useReducedMotionPreference();
  const run = active && !reduced;

  return (
    <div
      className="hbr-beacon absolute inset-0"
      data-run={run ? "true" : "false"}
    >
      <style>{CSS}</style>
      <svg viewBox="0 0 640 360" className="h-full w-full" aria-hidden="true">
        <rect width="640" height="360" fill={DUSK.skyTop} />

        {/* Beacon and its sweep, behind the readouts */}
        <path
          className="hbr-b-beam"
          d="M320 344 L106 44 L534 44 Z"
          fill={DUSK.lamp}
          opacity="0.28"
        />
        <path d="M310 356 L314 330 H326 L330 356 Z" fill={DUSK.quayTop} />
        <circle
          className="hbr-b-lamp"
          cx={320}
          cy={332}
          r={6}
          fill={DUSK.lamp}
          opacity="0.5"
        />

        {/* Gateway readout */}
        <rect
          x={16}
          y={20}
          width={608}
          height={136}
          rx={10}
          fill={DUSK.quay}
          stroke={DUSK.edge}
        />
        <text
          x={36}
          y={50}
          fill={DUSK.heading}
          fontSize={FONT.label}
          style={LABEL}
        >
          FUSION GATEWAY
        </text>
        {METRICS.map((metric, i) => (
          <g key={metric.label}>
            <text
              x={36 + i * 198}
              y={84}
              fill={DUSK.ink}
              fontSize={FONT.label}
              style={LABEL}
            >
              {metric.label}
            </text>
            <text
              x={36 + i * 198}
              y={118}
              fill={DUSK.accent}
              fontSize={FONT.caption}
              style={LABEL}
            >
              {metric.value}
            </text>
            {trace(i + 2).map((h, j) => (
              <rect
                key={j}
                className="hbr-b-bar"
                x={36 + i * 198 + j * 8}
                y={148 - h}
                width={5}
                height={h}
                rx={1}
                fill={DUSK.accent}
                fillOpacity={0.45}
                style={barStyle((i * 9 + j) * 0.07, 0.45 + ((j * 5) % 9) / 18)}
              />
            ))}
          </g>
        ))}

        {/* One berth readout per subgraph behind the gateway */}
        {WAREHOUSES.slice(0, 4).map((w, i) => (
          <g key={w.name}>
            <rect
              x={16 + i * 156}
              y={176}
              width={144}
              height={112}
              rx={10}
              fill={DUSK.quay}
              stroke={DUSK.edge}
            />
            <text
              x={32 + i * 156}
              y={206}
              fill={DUSK.heading}
              fontSize={FONT.label}
            >
              {w.name}
            </text>
            <text
              x={32 + i * 156}
              y={232}
              fill={DUSK.ink}
              fontSize={FONT.label}
              style={LABEL}
            >
              {w.language}
            </text>
            {trace(i + 5).map((h, j) => (
              <rect
                key={j}
                className="hbr-b-bar"
                x={32 + i * 156 + j * 12}
                y={278 - h}
                width={7}
                height={h}
                rx={1}
                fill={DUSK.shimmer}
                style={barStyle((i * 9 + j) * 0.09, 0.4 + ((j * 7) % 11) / 20)}
              />
            ))}
          </g>
        ))}
      </svg>
    </div>
  );
}
