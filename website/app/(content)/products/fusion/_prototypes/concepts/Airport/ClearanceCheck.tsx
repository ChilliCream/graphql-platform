"use client";

import { TYPE } from "../../brand";
import { anim, useCycle, useSceneMotion } from "./hooks";
import { AP, GATES } from "./palette";

/**
 * "Any GraphQL server, no plugin": clearance before takeoff. Composition reads
 * each source schema exactly as its server publishes it - no kit loaded on
 * board - and validates them against one another; a type conflict stamps the
 * plan GROUNDED and holds it short of the runway instead of letting it fly.
 * At rest the strip shows the conflict and the grounded stamp.
 */

const W = 640;
const H = 440;
const PHASES = 7;
const REST = 0;
const BEAT = 1400;
const ROWS = GATES.slice(0, 4);
/** The clearance stamp, wide enough for its verdict at the `TYPE.label` floor. */
const STAMP = { x: 392, y: 96, w: 240, h: 112 } as const;
/** Shipping is the plan that fails validation. */
const CONFLICT = 3;

const KEYFRAMES = `
@keyframes ap-clr-stamp {
  0% { transform: scale(1.5); opacity: 0; }
  40% { transform: scale(1); opacity: 1; }
  100% { transform: scale(1); opacity: 1; }
}
@keyframes ap-clr-hold { 0%, 100% { opacity: 1; } 50% { opacity: 0.3; } }
@keyframes ap-clr-roll {
  0% { transform: translateX(0px); }
  100% { transform: translateX(210px); }
}
`;

const rowY = (i: number) => 96 + i * 56;

export function ClearanceCheck() {
  const running = useSceneMotion();
  const phase = useCycle(running, PHASES, BEAT, REST);

  const grounded = phase === REST || phase === 4;
  const cleared = phase >= 5;
  const rolling = phase === 6;
  const checked = (i: number) => phase === REST || phase >= i + 1;
  const failing = (i: number) => i === CONFLICT && grounded;

  return (
    <svg viewBox={`0 0 ${W} ${H}`} className="h-full w-full">
      <style>{KEYFRAMES}</style>
      <rect width={W} height={H} fill={AP.bg} />

      <text
        x="24"
        y="40"
        fill={AP.dim}
        fontFamily={AP.mono}
        fontSize={TYPE.label}
        letterSpacing="0.22em"
      >
        COMPOSITION · FLIGHT PLAN VALIDATION
      </text>
      <text
        x="24"
        y="62"
        fill={AP.dim}
        fontFamily={AP.mono}
        fontSize={TYPE.label}
        letterSpacing="0.14em"
      >
        BUILD STEP · NOTHING INSTALLED ON BOARD
      </text>

      {ROWS.map((gate, i) => {
        const y = rowY(i);
        const bad = failing(i);
        const done = checked(i);
        const color = bad ? AP.stop : done ? AP.taxi : AP.dim;

        return (
          <g key={gate.name}>
            <rect
              x="24"
              y={y}
              width="352"
              height="44"
              rx="7"
              fill={AP.panel}
              stroke={bad ? AP.stop : AP.panelEdge}
              style={{ transition: "stroke 400ms ease" }}
            />
            <circle cx="44" cy={y + 22} r="5" fill={color} />
            <text
              x="60"
              y={y + 18}
              fill={AP.ink}
              fontFamily={AP.mono}
              fontSize={TYPE.caption}
            >
              {gate.name} · {gate.language}
            </text>
            <text
              x="60"
              y={y + 36}
              fill={AP.dim}
              fontFamily={AP.mono}
              fontSize={TYPE.label}
              letterSpacing="0.02em"
            >
              KEYS AND LOOKUPS DECLARED IN ITS OWN SCHEMA
            </text>
            <text
              x="364"
              y={y + 18}
              fill={color}
              fontFamily={AP.mono}
              fontSize={TYPE.label}
              textAnchor="end"
              letterSpacing="0.1em"
              style={{ transition: "fill 400ms ease" }}
            >
              {bad ? "TYPE CONFLICT" : done ? "VALIDATED" : "PENDING"}
            </text>
          </g>
        );
      })}

      <text
        x="24"
        y={rowY(ROWS.length) + 14}
        fill={grounded ? AP.stop : AP.dim}
        fontFamily={AP.mono}
        fontSize={TYPE.label}
        letterSpacing="0.02em"
        style={{ transition: "fill 400ms ease" }}
      >
        {grounded
          ? "Money: Shipping declares Decimal, Billing declares Int"
          : "Money: one type across every source schema"}
      </text>

      {/* The clearance stamp on the plan. */}
      <rect
        x={STAMP.x}
        y={STAMP.y}
        width={STAMP.w}
        height={STAMP.h}
        rx="9"
        fill={AP.panel}
        stroke={grounded ? AP.stop : cleared ? AP.taxi : AP.panelEdge}
        style={{ transition: "stroke 400ms ease" }}
      />
      <text
        key={grounded ? "grounded" : cleared ? "cleared" : "checking"}
        x={STAMP.x + STAMP.w / 2}
        y="152"
        fill={grounded ? AP.stop : cleared ? AP.taxi : AP.dim}
        fontFamily={AP.mono}
        fontSize={TYPE.h5}
        textAnchor="middle"
        letterSpacing="0.16em"
        style={{
          transformBox: "fill-box",
          transformOrigin: "center",
          animation: anim(running, "ap-clr-stamp 520ms ease-out"),
        }}
      >
        {grounded ? "GROUNDED" : cleared ? "CLEARED" : "CHECKING"}
      </text>
      <text
        x={STAMP.x + STAMP.w / 2}
        y="180"
        fill={AP.dim}
        fontFamily={AP.mono}
        fontSize={TYPE.label}
        textAnchor="middle"
        letterSpacing="0.04em"
      >
        {grounded
          ? "PIPELINE FAILS, NOT THE GATEWAY"
          : "COMPOSITE SCHEMA BUILT"}
      </text>

      {/* Hold-short bar and the plan waiting behind it. */}
      <line
        x1="392"
        y1="330"
        x2="392"
        y2="404"
        stroke={grounded ? AP.stop : AP.taxi}
        strokeWidth="4"
        strokeDasharray="10 8"
        style={{
          animation: grounded
            ? anim(running, "ap-clr-hold 1100ms ease-in-out infinite")
            : "none",
          transition: "stroke 400ms ease",
        }}
      />
      <text
        x="392"
        y="318"
        fill={grounded ? AP.stop : AP.taxi}
        fontFamily={AP.mono}
        fontSize={TYPE.label}
        textAnchor="middle"
        letterSpacing="0.14em"
        style={{ transition: "fill 400ms ease" }}
      >
        HOLD SHORT
      </text>
      <line
        x1="24"
        y1="368"
        x2={W - 24}
        y2="368"
        stroke={AP.paint}
        strokeWidth="2"
        strokeDasharray="24 20"
      />
      <g
        style={{
          animation: rolling
            ? anim(running, "ap-clr-roll 1300ms ease-in forwards")
            : "none",
        }}
      >
        <g transform="translate(320 368)">
          <ellipse
            rx="18"
            ry="3.2"
            fill={grounded ? AP.stop : AP.taxi}
            style={{ transition: "fill 400ms ease" }}
          />
          <path
            d="M2,-1 L-10,-13 L-3,-13 L10,-1 Z"
            fill={grounded ? AP.stop : AP.taxi}
            style={{ transition: "fill 400ms ease" }}
          />
          <path
            d="M2,1 L-10,13 L-3,13 L10,1 Z"
            fill={grounded ? AP.stop : AP.taxi}
            style={{ transition: "fill 400ms ease" }}
          />
        </g>
      </g>
    </svg>
  );
}
