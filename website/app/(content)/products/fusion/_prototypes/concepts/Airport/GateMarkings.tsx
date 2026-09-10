"use client";

import { anim, useCycle, useSceneMotion } from "./hooks";
import { AP, GATES, GROUND_SOURCES, specMark } from "./palette";

/**
 * "Both specifications, one gateway": the gate signs. Every stand boards into
 * the same concourse whichever specification is painted on its sign, ground
 * vehicles roll OpenAPI and gRPC sources up to their own stands, and mid-cycle
 * gate B2 is re-marked from Apollo Federation to the GraphQL Federation
 * specification without ever closing. At rest the signs read as they stand:
 * two of each, one concourse.
 */

const W = 640;
const H = 440;
const CONCOURSE = { x: 28, y: 34, w: W - 56, h: 46 } as const;
const STANDS = GATES.slice(0, 4);
/** Shipping is the gate whose marking is repainted. */
const REMARKED = 3;
const PHASES = 6;
const REST = 0;
const BEAT = 1700;

const KEYFRAMES = `
@keyframes ap-mark-flip {
  0% { transform: rotateX(-84deg); opacity: 0.2; }
  100% { transform: rotateX(0deg); opacity: 1; }
}
@keyframes ap-mark-bridge { from { stroke-dashoffset: 22; } to { stroke-dashoffset: 0; } }
@keyframes ap-mark-truck {
  0% { transform: translateX(-160px); }
  45% { transform: translateX(0px); }
  100% { transform: translateX(0px); }
}
`;

const standX = (i: number) => 28 + i * 150;

export function GateMarkings() {
  const running = useSceneMotion();
  const phase = useCycle(running, PHASES, BEAT, REST);
  const repainting = phase >= 2 && phase <= 4;
  const repainted = phase >= 3 && phase <= 4;

  return (
    <svg viewBox={`0 0 ${W} ${H}`} className="h-full w-full">
      <style>{KEYFRAMES}</style>
      <rect width={W} height={H} fill={AP.bg} />

      {/* The concourse: one composite schema behind every gate. */}
      <rect
        x={CONCOURSE.x}
        y={CONCOURSE.y}
        width={CONCOURSE.w}
        height={CONCOURSE.h}
        rx="10"
        fill={AP.panel}
        stroke={AP.approach}
        strokeOpacity="0.55"
      />
      <text
        x={W / 2}
        y={CONCOURSE.y + 28}
        fill={AP.amber}
        fontFamily={AP.mono}
        fontSize="12"
        textAnchor="middle"
        letterSpacing="0.2em"
      >
        ONE CONCOURSE · COMPOSITE SCHEMA
      </text>

      {STANDS.map((gate, i) => {
        const x = standX(i);
        const active = i === REMARKED && repainting;
        const mark =
          i === REMARKED && repainted
            ? specMark("GraphQL Federation")
            : specMark(gate.spec);

        return (
          <g key={gate.name}>
            <path
              d={`M${x + 66} 152 L${x + 66} ${CONCOURSE.y + CONCOURSE.h}`}
              stroke={AP.taxi}
              strokeWidth="2"
              strokeDasharray="11 11"
              opacity="0.7"
              style={{
                animation: anim(
                  running,
                  `ap-mark-bridge 900ms linear ${i * 150}ms infinite`,
                ),
              }}
            />
            <rect
              x={x}
              y="152"
              width="132"
              height="96"
              rx="9"
              fill={AP.panel}
              stroke={active ? AP.amber : AP.panelEdge}
              style={{ transition: "stroke 400ms ease" }}
            />
            <text
              x={x + 12}
              y="176"
              fill={AP.amber}
              fontFamily={AP.mono}
              fontSize="12"
            >
              {gate.stand} {gate.name}
            </text>
            <text
              x={x + 12}
              y="196"
              fill={AP.dim}
              fontFamily={AP.mono}
              fontSize="9"
              letterSpacing="0.12em"
            >
              {gate.language} · SOURCE SCHEMA
            </text>
            <rect
              x={x + 12}
              y="208"
              width="108"
              height="26"
              rx="5"
              fill="rgba(255,255,255,0.05)"
              stroke={AP.panelEdge}
            />
            <text
              key={mark}
              x={x + 66}
              y="225"
              fill={AP.ink}
              fontFamily={AP.mono}
              fontSize="9"
              textAnchor="middle"
              letterSpacing="0.14em"
              style={{
                transformBox: "fill-box",
                transformOrigin: "top center",
                animation: anim(running, "ap-mark-flip 460ms ease-out"),
              }}
            >
              {mark}
            </text>
            <text
              x={x + 66}
              y="266"
              fill={AP.taxi}
              fontFamily={AP.mono}
              fontSize="9"
              textAnchor="middle"
              letterSpacing="0.14em"
            >
              GATE OPEN
            </text>
          </g>
        );
      })}

      {/* Ground vehicles: sources that are not GraphQL servers. */}
      <line
        x1="28"
        y1="392"
        x2={W - 28}
        y2="392"
        stroke={AP.paint}
        strokeWidth="2"
        strokeDasharray="16 14"
      />
      {GROUND_SOURCES.map((source, i) => {
        const x = 60 + i * 300;

        return (
          <g key={source.name}>
            <path
              d={`M${x + 60} 330 L${x + 60} 258`}
              stroke={AP.taxi}
              strokeWidth="2"
              strokeDasharray="9 9"
              opacity="0.6"
              style={{
                animation: anim(
                  running,
                  `ap-mark-bridge 1000ms linear ${i * 240}ms infinite`,
                ),
              }}
            />
            <g
              style={{
                animation: anim(
                  running,
                  `ap-mark-truck 5200ms ease-out ${i * 900}ms infinite`,
                ),
              }}
            >
              <g transform={`translate(${x} 330)`}>
                <rect
                  width="120"
                  height="46"
                  rx="7"
                  fill={AP.panel}
                  stroke={AP.panelEdge}
                  strokeDasharray="5 4"
                />
                <text
                  x="12"
                  y="20"
                  fill={AP.ink}
                  fontFamily={AP.mono}
                  fontSize="11"
                >
                  {source.name}
                </text>
                <text
                  x="12"
                  y="36"
                  fill={AP.dim}
                  fontFamily={AP.mono}
                  fontSize="8"
                  letterSpacing="0.12em"
                >
                  {source.cargo}
                </text>
                <circle cx="26" cy="50" r="5" fill={AP.dim} />
                <circle cx="96" cy="50" r="5" fill={AP.dim} />
              </g>
            </g>
          </g>
        );
      })}
      <text
        x={W - 28}
        y="418"
        fill={AP.dim}
        fontFamily={AP.mono}
        fontSize="9"
        textAnchor="end"
        letterSpacing="0.16em"
      >
        SAME COMPOSITION STEP
      </text>
    </svg>
  );
}
