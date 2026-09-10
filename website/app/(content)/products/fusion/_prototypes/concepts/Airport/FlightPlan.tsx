"use client";

import { anim, useCycle, useSceneMotion } from "./hooks";
import { AP, GATES } from "./palette";

/**
 * "What is Fusion?": one flight plan. A single client files one plan with the
 * tower, the tower works out which gates hold the legs and calls them in turn,
 * and the whole trip leaves again as one departure on the one runway. At rest
 * the finished plan is drawn end to end with every leg lit.
 */

const W = 640;
const H = 440;
const TOWER = { x: 258, y: 150, w: 124, h: 92 } as const;
const LEGS = GATES.slice(0, 3);
const PHASES = 6;
const REST = 0;
const BEAT = 1500;

const KEYFRAMES = `
@keyframes ap-plan-flow { from { stroke-dashoffset: 26; } to { stroke-dashoffset: 0; } }
@keyframes ap-plan-depart {
  0% { transform: translateX(0px); opacity: 0; }
  12% { opacity: 1; }
  85% { opacity: 1; }
  100% { transform: translateX(440px); opacity: 0; }
}
@keyframes ap-plan-beam { 0%, 100% { opacity: 0.35; } 50% { opacity: 0.9; } }
`;

const gateY = (i: number) => 76 + i * 106;

export function FlightPlan() {
  const running = useSceneMotion();
  const phase = useCycle(running, PHASES, BEAT, REST);

  const filed = phase === REST || phase >= 1;
  const legLit = (i: number) => phase === REST || phase >= i + 2;
  const departed = phase === REST || phase === 5;

  return (
    <svg viewBox={`0 0 ${W} ${H}`} className="h-full w-full">
      <style>{KEYFRAMES}</style>
      <rect width={W} height={H} fill={AP.bg} />

      {/* The client that files the plan. */}
      <g>
        <rect
          x="16"
          y="152"
          width="120"
          height="88"
          rx="10"
          fill={AP.panel}
          stroke={filed ? AP.approach : AP.panelEdge}
          style={{ transition: "stroke 400ms ease" }}
        />
        <text
          x="76"
          y="180"
          fill={AP.ink}
          fontFamily={AP.mono}
          fontSize="13"
          textAnchor="middle"
        >
          Web client
        </text>
        <text
          x="76"
          y="202"
          fill={AP.dim}
          fontFamily={AP.mono}
          fontSize="10"
          textAnchor="middle"
          letterSpacing="0.14em"
        >
          ONE QUERY
        </text>
        <text
          x="76"
          y="224"
          fill={AP.approach}
          fontFamily={AP.mono}
          fontSize="10"
          textAnchor="middle"
          letterSpacing="0.14em"
        >
          FLIGHT PLAN
        </text>
      </g>

      {/* Plan filed with the tower. */}
      <path
        d={`M136 196 L${TOWER.x} 196`}
        stroke={filed ? AP.approach : AP.paint}
        strokeWidth="2"
        strokeDasharray="13 13"
        fill="none"
        style={{
          animation: anim(running, "ap-plan-flow 900ms linear infinite"),
          transition: "stroke 400ms ease",
        }}
      />

      {/* The tower: one gateway. */}
      <rect
        x={TOWER.x}
        y={TOWER.y}
        width={TOWER.w}
        height={TOWER.h}
        rx="10"
        fill={AP.panel}
        stroke={AP.approach}
        strokeOpacity="0.7"
      />
      <text
        x={TOWER.x + TOWER.w / 2}
        y={TOWER.y + 34}
        fill={AP.amber}
        fontFamily={AP.mono}
        fontSize="14"
        textAnchor="middle"
      >
        TOWER
      </text>
      <text
        x={TOWER.x + TOWER.w / 2}
        y={TOWER.y + 56}
        fill={AP.dim}
        fontFamily={AP.mono}
        fontSize="10"
        textAnchor="middle"
        letterSpacing="0.12em"
      >
        GATEWAY
      </text>
      <text
        x={TOWER.x + TOWER.w / 2}
        y={TOWER.y + 76}
        fill={AP.dim}
        fontFamily={AP.mono}
        fontSize="9"
        textAnchor="middle"
        letterSpacing="0.12em"
      >
        DISTRIBUTED EXECUTOR
      </text>

      {/* One leg per gate the plan touches. */}
      {LEGS.map((gate, i) => {
        const y = gateY(i);
        const lit = legLit(i);

        return (
          <g key={gate.name}>
            <path
              d={`M${TOWER.x + TOWER.w} 196 C 470 196, 452 ${y + 26}, 500 ${y + 26}`}
              stroke={lit ? AP.taxi : AP.paint}
              strokeWidth="2"
              strokeDasharray="12 12"
              fill="none"
              style={{
                animation: anim(
                  running,
                  `ap-plan-flow 1000ms linear ${i * 180}ms infinite`,
                ),
                transition: "stroke 400ms ease",
              }}
            />
            <rect
              x="500"
              y={y}
              width="124"
              height="52"
              rx="8"
              fill={AP.panel}
              stroke={lit ? AP.taxi : AP.panelEdge}
              style={{ transition: "stroke 400ms ease" }}
            />
            <text
              x="512"
              y={y + 22}
              fill={AP.ink}
              fontFamily={AP.mono}
              fontSize="12"
            >
              {gate.stand} {gate.name}
            </text>
            <text
              x="512"
              y={y + 40}
              fill={AP.dim}
              fontFamily={AP.mono}
              fontSize="9"
              letterSpacing="0.12em"
            >
              SUBGRAPH · {gate.language}
            </text>
          </g>
        );
      })}

      {/* The single departure that carries the answer back. */}
      <line
        x1="24"
        y1="392"
        x2={W - 24}
        y2="392"
        stroke={AP.paint}
        strokeWidth="2"
        strokeDasharray="30 22"
      />
      <text
        x="24"
        y="374"
        fill={departed ? AP.taxi : AP.dim}
        fontFamily={AP.mono}
        fontSize="10"
        letterSpacing="0.2em"
        style={{ transition: "fill 400ms ease" }}
      >
        ONE RESPONSE · ONE DEPARTURE
      </text>
      <g
        style={{
          animation: anim(running, "ap-plan-depart 4000ms ease-in infinite"),
        }}
      >
        <g transform={`translate(${running ? 120 : 300} 392)`}>
          <ellipse rx="18" ry="3.2" fill={AP.taxi} />
          <path d="M2,-1 L-10,-13 L-3,-13 L10,-1 Z" fill={AP.taxi} />
          <path d="M2,1 L-10,13 L-3,13 L10,1 Z" fill={AP.taxi} />
        </g>
      </g>
      <circle
        cx={TOWER.x + TOWER.w / 2}
        cy={TOWER.y - 14}
        r="5"
        fill={AP.approach}
        style={{
          animation: anim(running, "ap-plan-beam 2200ms ease-in-out infinite"),
        }}
      />
    </svg>
  );
}
