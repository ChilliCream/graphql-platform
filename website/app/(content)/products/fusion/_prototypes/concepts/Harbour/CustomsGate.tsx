"use client";

import { useReducedMotionPreference, useSceneActive } from "../../Primitives";
import { DUSK, LABEL, WAREHOUSES } from "./palette";

/**
 * "Any GraphQL server, no plugin": five ordinary manifests queue at the quay,
 * and the one step added to the harbour is the customs check. The inspector
 * sweeps the queue, stamps every manifest against the others, and when one
 * declares a conflicting type the barrier drops and nothing sails.
 *
 * Rest state: all five manifests stamped and the composite crate assembled.
 */

const CSS = `
.hbr-gate [class*="hbr-c-"] { animation-play-state: running; }
.hbr-gate[data-run="false"] [class*="hbr-c-"] { animation: none; }
.hbr-gate .hbr-c-stamp { animation: hbr-c-stamp 12s ease-in-out infinite; transform-box: fill-box; transform-origin: center; }
.hbr-gate .hbr-c-scan { animation: hbr-c-scan 12s cubic-bezier(0.65, 0, 0.35, 1) infinite; }
.hbr-gate .hbr-c-stop { animation: hbr-c-stop 12s ease-in-out infinite; }
.hbr-gate .hbr-c-slat { animation: hbr-c-slat 12s ease-in-out infinite; }
@keyframes hbr-c-stamp {
  0%, 4% { opacity: 0.12; transform: scale(0.86); }
  14%, 56% { opacity: 1; transform: scale(1); }
  64%, 88% { opacity: 0.25; transform: scale(1); }
  96%, 100% { opacity: 1; transform: scale(1); }
}
@keyframes hbr-c-scan {
  0% { transform: translateY(0); opacity: 0; }
  6% { opacity: 0.9; }
  56% { transform: translateY(316px); opacity: 0.9; }
  64%, 100% { transform: translateY(316px); opacity: 0; }
}
@keyframes hbr-c-stop {
  0%, 58% { opacity: 0; }
  64%, 88% { opacity: 1; }
  94%, 100% { opacity: 0; }
}
@keyframes hbr-c-slat {
  0%, 6% { opacity: 0.12; }
  18%, 56% { opacity: 1; }
  64%, 88% { opacity: 0.18; }
  96%, 100% { opacity: 1; }
}
`;

const ROW_H = 56;
const ROW_GAP = 16;
const ROW_TOP = 76;
const CONFLICT = "Ordering";

const rowY = (i: number) => ROW_TOP + i * (ROW_H + ROW_GAP);

export function CustomsGate() {
  const active = useSceneActive();
  const reduced = useReducedMotionPreference();
  const run = active && !reduced;

  return (
    <div
      className="hbr-gate absolute inset-0"
      data-run={run ? "true" : "false"}
    >
      <style>{CSS}</style>
      <svg viewBox="0 0 640 480" className="h-full w-full" aria-hidden="true">
        <rect width="640" height="480" fill={DUSK.skyTop} />

        <text x="16" y="44" fill={DUSK.ink} fontSize={10} style={LABEL}>
          SOURCE SCHEMAS
        </text>
        <text x="330" y="44" fill={DUSK.ink} fontSize={10} style={LABEL}>
          COMPOSITION
        </text>
        <text x="482" y="44" fill={DUSK.ink} fontSize={10} style={LABEL}>
          COMPOSITE
        </text>

        {/* The queue of manifests, one per subgraph */}
        {WAREHOUSES.map((w, i) => (
          <g key={w.name}>
            <rect
              x={16}
              y={rowY(i)}
              width={276}
              height={ROW_H}
              rx={8}
              fill={DUSK.quay}
              stroke={DUSK.edge}
            />
            <text x={32} y={rowY(i) + 24} fill={DUSK.heading} fontSize={13}>
              {w.name}
            </text>
            <text
              x={32}
              y={rowY(i) + 42}
              fill={DUSK.ink}
              fontSize={10}
              style={LABEL}
            >
              {`${w.language} · ${w.pennant}`}
            </text>
            <g
              className="hbr-c-stamp"
              style={{ animationDelay: `${i * 0.9}s` }}
            >
              <circle
                cx={262}
                cy={rowY(i) + ROW_H / 2}
                r={13}
                fill="none"
                stroke={DUSK.ok}
                strokeWidth={2}
              />
              <path
                d={`M${255} ${rowY(i) + 28} l5 5 l10 -11`}
                fill="none"
                stroke={DUSK.ok}
                strokeWidth={2.5}
                strokeLinecap="round"
                strokeLinejoin="round"
              />
            </g>
            {w.name === CONFLICT && (
              <g className="hbr-c-stop" opacity="0">
                <rect
                  x={16}
                  y={rowY(i)}
                  width={276}
                  height={ROW_H}
                  rx={8}
                  fill="none"
                  stroke={DUSK.stop}
                  strokeWidth={2}
                />
                <rect
                  x={150}
                  y={rowY(i) + 12}
                  width={92}
                  height={20}
                  rx={10}
                  fill={DUSK.stop}
                  opacity="0.18"
                />
                <text
                  x={196}
                  y={rowY(i) + 26}
                  textAnchor="middle"
                  fill={DUSK.stop}
                  fontSize={10}
                  style={LABEL}
                >
                  type conflict
                </text>
              </g>
            )}
            <line
              x1={292}
              y1={rowY(i) + ROW_H / 2}
              x2={330}
              y2={rowY(i) + ROW_H / 2}
              stroke={DUSK.edgeBright}
              strokeWidth={1.5}
              strokeDasharray="5 6"
            />
          </g>
        ))}

        {/* The customs gate */}
        <path
          d="M334 60 h122 v380 h-22 V126 h-78 v314 h-22 Z"
          fill={DUSK.quay}
          stroke={DUSK.edge}
        />
        <rect
          className="hbr-c-scan"
          x={340}
          y={64}
          width={110}
          height={4}
          rx={2}
          fill={DUSK.accent}
          opacity="0"
        />
        <g className="hbr-c-stop" opacity="0">
          <rect
            x={334}
            y={236}
            width={122}
            height={22}
            rx={4}
            fill={DUSK.stop}
          />
          <rect
            x={334}
            y={236}
            width={122}
            height={22}
            rx={4}
            fill="none"
            stroke={DUSK.skyTop}
            strokeWidth={1.5}
            strokeDasharray="10 10"
          />
        </g>

        {/* The composite schema crate on the far side of the gate */}
        <rect
          x={482}
          y={76}
          width={142}
          height={364}
          rx={10}
          fill={DUSK.quay}
          stroke={DUSK.edge}
        />
        {WAREHOUSES.map((w, i) => (
          <rect
            key={w.name}
            className="hbr-c-slat"
            x={498}
            y={rowY(i) + 4}
            width={110}
            height={ROW_H - 8}
            rx={6}
            fill={DUSK.accent}
            fillOpacity={0.55}
            style={{ animationDelay: `${i * 0.9}s` }}
          />
        ))}
        <g className="hbr-c-stop" opacity="0">
          <rect
            x={482}
            y={222}
            width={142}
            height={44}
            rx={8}
            fill={DUSK.skyTop}
            stroke={DUSK.stop}
            strokeWidth={2}
          />
          <text
            x={553}
            y={250}
            textAnchor="middle"
            fill={DUSK.stop}
            fontSize={12}
            style={LABEL}
          >
            BUILD STOPPED
          </text>
        </g>
      </svg>
    </div>
  );
}
