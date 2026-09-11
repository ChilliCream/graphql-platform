"use client";

import { TYPE } from "../../brand";
import { anim, useSceneMotion } from "./hooks";
import { AP, GATES } from "./palette";

/**
 * Nitro band: the on-time performance strip. Three meters read the gateway -
 * latency, throughput and error rate - and a row of gate bars reads each
 * subgraph behind it, the way the Fusion dashboard does. At rest the meters
 * hold their current values instead of sweeping.
 */

const W = 640;
const H = 360;

const METERS = [
  { label: "P95 LATENCY", value: "84 ms", fill: 0.42, color: AP.approach },
  { label: "THROUGHPUT", value: "1.9k op/s", fill: 0.72, color: AP.taxi },
  { label: "ERROR RATE", value: "0.21 %", fill: 0.14, color: AP.amber },
] as const;

/** Gate load, in the roster's order. */
const LOAD = [0.78, 0.52, 0.91, 0.44, 0.63] as const;

const KEYFRAMES = `
@keyframes ap-meter-bar { 0%, 100% { transform: scaleX(0.88); } 50% { transform: scaleX(1); } }
@keyframes ap-meter-tick { 0%, 100% { opacity: 1; } 50% { opacity: 0.35; } }
`;

export function ApronMeters() {
  const running = useSceneMotion();

  return (
    <svg viewBox={`0 0 ${W} ${H}`} className="h-full w-full">
      <style>{KEYFRAMES}</style>
      <rect width={W} height={H} fill={AP.bg} />

      <text
        x="24"
        y="34"
        fill={AP.dim}
        fontFamily={AP.mono}
        fontSize={TYPE.label}
        letterSpacing="0.22em"
      >
        GATEWAY · ON-TIME PERFORMANCE
      </text>
      <circle
        cx={W - 30}
        cy="30"
        r="4"
        fill={AP.taxi}
        style={{
          animation: anim(running, "ap-meter-tick 1900ms ease-in-out infinite"),
        }}
      />

      {METERS.map((meter, i) => {
        const x = 24 + i * 200;

        return (
          <g key={meter.label}>
            <rect
              x={x}
              y="56"
              width="176"
              height="86"
              rx="8"
              fill={AP.panel}
              stroke={AP.panelEdge}
            />
            <text
              x={x + 16}
              y="80"
              fill={AP.dim}
              fontFamily={AP.mono}
              fontSize={TYPE.label}
              letterSpacing="0.14em"
            >
              {meter.label}
            </text>
            <text
              x={x + 16}
              y="108"
              fill={meter.color}
              fontFamily={AP.mono}
              fontSize={TYPE.h5}
            >
              {meter.value}
            </text>
            <rect
              x={x + 16}
              y="120"
              width="144"
              height="6"
              rx="3"
              fill={AP.deck}
            />
            <rect
              x={x + 16}
              y="120"
              width={144 * meter.fill}
              height="6"
              rx="3"
              fill={meter.color}
              style={{
                transformBox: "fill-box",
                transformOrigin: "left center",
                animation: anim(
                  running,
                  `ap-meter-bar 3200ms ease-in-out ${i * 420}ms infinite`,
                ),
              }}
            />
          </g>
        );
      })}

      <text
        x="24"
        y="182"
        fill={AP.dim}
        fontFamily={AP.mono}
        fontSize={TYPE.label}
        letterSpacing="0.18em"
      >
        PER GATE · SUBGRAPH BEHIND THE GATEWAY
      </text>

      {GATES.map((gate, i) => {
        const y = 200 + i * 30;

        return (
          <g key={gate.name}>
            <text
              x="24"
              y={y + 12}
              fill={AP.ink}
              fontFamily={AP.mono}
              fontSize={TYPE.caption}
            >
              {gate.stand} {gate.name}
            </text>
            <text
              x="132"
              y={y + 12}
              fill={AP.dim}
              fontFamily={AP.mono}
              fontSize={TYPE.label}
              letterSpacing="0.1em"
            >
              {gate.language}
            </text>
            <rect
              x="196"
              y={y + 3}
              width={W - 240}
              height="10"
              rx="5"
              fill={AP.deck}
            />
            <rect
              x="196"
              y={y + 3}
              width={(W - 240) * LOAD[i]}
              height="10"
              rx="5"
              fill={AP.taxi}
              opacity="0.75"
              style={{
                transformBox: "fill-box",
                transformOrigin: "left center",
                animation: anim(
                  running,
                  `ap-meter-bar 3600ms ease-in-out ${i * 300}ms infinite`,
                ),
              }}
            />
          </g>
        );
      })}
    </svg>
  );
}
