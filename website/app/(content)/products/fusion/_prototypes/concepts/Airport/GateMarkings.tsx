"use client";

import { TYPE } from "../../brand";
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
/** Gate signs, sized so every marking reads at the `TYPE.label` floor. */
const SIGN = { y: 150, w: 138, h: 112, pitch: 148 } as const;
/** Ground vehicles, wide enough for the cargo they carry. */
const TRUCK = { y: 330, w: 150, h: 46 } as const;
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

const standX = (i: number) => 26 + i * SIGN.pitch;

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
        fontSize={TYPE.caption}
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
              d={`M${x + SIGN.w / 2} ${SIGN.y} L${x + SIGN.w / 2} ${CONCOURSE.y + CONCOURSE.h}`}
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
              y={SIGN.y}
              width={SIGN.w}
              height={SIGN.h}
              rx="9"
              fill={AP.panel}
              stroke={active ? AP.amber : AP.panelEdge}
              style={{ transition: "stroke 400ms ease" }}
            />
            <text
              x={x + 10}
              y={SIGN.y + 24}
              fill={AP.amber}
              fontFamily={AP.mono}
              fontSize={TYPE.caption}
            >
              {gate.stand} {gate.name}
            </text>
            <text
              x={x + 10}
              y={SIGN.y + 44}
              fill={AP.dim}
              fontFamily={AP.mono}
              fontSize={TYPE.label}
              letterSpacing="0.12em"
            >
              {gate.language}
            </text>
            <text
              x={x + 10}
              y={SIGN.y + 62}
              fill={AP.dim}
              fontFamily={AP.mono}
              fontSize={TYPE.label}
              letterSpacing="0.12em"
            >
              SOURCE SCHEMA
            </text>
            <rect
              x={x + 10}
              y={SIGN.y + 72}
              width={SIGN.w - 20}
              height="28"
              rx="5"
              fill={AP.wash}
              stroke={AP.panelEdge}
            />
            <text
              key={mark}
              x={x + SIGN.w / 2}
              y={SIGN.y + 91}
              fill={AP.ink}
              fontFamily={AP.mono}
              fontSize={TYPE.label}
              textAnchor="middle"
              letterSpacing="0.1em"
              style={{
                transformBox: "fill-box",
                transformOrigin: "top center",
                animation: anim(running, "ap-mark-flip 460ms ease-out"),
              }}
            >
              {mark}
            </text>
            <text
              x={x + SIGN.w / 2}
              y={SIGN.y + 130}
              fill={AP.taxi}
              fontFamily={AP.mono}
              fontSize={TYPE.label}
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
              d={`M${x + TRUCK.w / 2} ${TRUCK.y} L${x + TRUCK.w / 2} ${SIGN.y + SIGN.h + 30}`}
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
              <g transform={`translate(${x} ${TRUCK.y})`}>
                <rect
                  width={TRUCK.w}
                  height={TRUCK.h}
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
                  fontSize={TYPE.caption}
                >
                  {source.name}
                </text>
                <text
                  x="12"
                  y="38"
                  fill={AP.dim}
                  fontFamily={AP.mono}
                  fontSize={TYPE.label}
                  letterSpacing="0.12em"
                >
                  {source.cargo}
                </text>
                <circle cx="30" cy={TRUCK.h + 4} r="5" fill={AP.dim} />
                <circle
                  cx={TRUCK.w - 24}
                  cy={TRUCK.h + 4}
                  r="5"
                  fill={AP.dim}
                />
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
        fontSize={TYPE.label}
        textAnchor="end"
        letterSpacing="0.16em"
      >
        SAME COMPOSITION STEP
      </text>
    </svg>
  );
}
