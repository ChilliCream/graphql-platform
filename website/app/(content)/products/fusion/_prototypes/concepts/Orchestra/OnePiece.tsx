"use client";

import { useReducedMotionPreference, useSceneActive } from "../../Primitives";
import { DESKS, ENGRAVE, LISTENERS, STAGE } from "./palette";

/**
 * "What is Fusion?" visual: one query in, one piece out.
 *
 * A listener sends a single request to the rostrum, the conductor cues only
 * the desks that hold the notes, and their bars come back as one response -
 * the composite schema served at a single endpoint. At rest the cues are
 * printed faintly and the whole cue sheet is legible without motion.
 */

const W = 720;
const H = 420;
const ROSTRUM_X = 300;
const DESK_X = 452;
const DESK_TOP = 58;
const DESK_H = 66;
/** The desks the conductor cues for this query; the rest stay silent. */
const CUED = [0, 1, 3];

export function OnePiece() {
  const active = useSceneActive();
  const reduced = useReducedMotionPreference();
  const moving = active && !reduced;

  return (
    <svg
      viewBox={`0 0 ${W} ${H}`}
      className="absolute inset-0 h-full w-full"
      preserveAspectRatio="xMidYMid meet"
    >
      <style>{`
        .op-send { animation: op-send 9s ease-in-out infinite; }
        .op-back { animation: op-back 9s ease-in-out infinite; }
        .op-cue { animation: op-cue 9s ease-out infinite; }
        .op-desk { animation: op-desk 9s ease-out infinite; }
        .op-rostrum { animation: op-rostrum 9s ease-in-out infinite; }
        @keyframes op-send {
          0%, 100% { opacity: 0; transform: translateX(0); }
          4% { opacity: 1; transform: translateX(0); }
          14% { opacity: 1; transform: translateX(124px); }
          18% { opacity: 0; transform: translateX(124px); }
        }
        @keyframes op-back {
          0%, 56% { opacity: 0; transform: translateX(96px); }
          62% { opacity: 1; transform: translateX(96px); }
          74% { opacity: 1; transform: translateX(0); }
          80%, 100% { opacity: 0; transform: translateX(0); }
        }
        @keyframes op-cue {
          0%, 100% { stroke-dashoffset: 190; opacity: 0.25; }
          22% { stroke-dashoffset: 0; opacity: 1; }
          52% { stroke-dashoffset: 0; opacity: 1; }
          70% { stroke-dashoffset: 0; opacity: 0.25; }
        }
        @keyframes op-desk {
          0%, 16%, 100% { opacity: 0.3; }
          30%, 56% { opacity: 1; }
        }
        @keyframes op-rostrum {
          0%, 100% { opacity: 0.35; }
          20%, 62% { opacity: 1; }
        }
      `}</style>

      <rect width={W} height={H} fill={STAGE.hall} rx={2} />

      <text
        x={24}
        y={30}
        fill={STAGE.ink}
        fontSize={10}
        letterSpacing={1.6}
        fontFamily={ENGRAVE}
      >
        THE HALL
      </text>
      {LISTENERS.map((listener, i) => (
        <g key={listener}>
          <rect
            x={24}
            y={DESK_TOP + i * DESK_H + 8}
            width={128}
            height={34}
            rx={17}
            fill={STAGE.boards}
            stroke={STAGE.rule}
          />
          <text
            x={88}
            y={DESK_TOP + i * DESK_H + 30}
            textAnchor="middle"
            fill={STAGE.heading}
            fontSize={12}
            fontFamily={ENGRAVE}
          >
            {listener}
          </text>
        </g>
      ))}

      <g transform={`translate(${ROSTRUM_X} 0)`}>
        <circle
          cx={0}
          cy={H / 2}
          r={44}
          fill={STAGE.boards}
          stroke={STAGE.rostrum}
          strokeWidth={1.5}
        />
        <circle
          cx={0}
          cy={H / 2}
          r={62}
          fill="none"
          stroke={STAGE.rostrum}
          strokeWidth={1}
          opacity={0.35}
          className={moving ? "op-rostrum" : undefined}
        />
        <text
          x={0}
          y={H / 2 - 4}
          textAnchor="middle"
          fill={STAGE.heading}
          fontSize={12}
          fontFamily={ENGRAVE}
        >
          Fusion
        </text>
        <text
          x={0}
          y={H / 2 + 12}
          textAnchor="middle"
          fill={STAGE.ink}
          fontSize={9}
          letterSpacing={1.2}
          fontFamily={ENGRAVE}
        >
          ROSTRUM
        </text>
        <text
          x={0}
          y={H / 2 + 84}
          textAnchor="middle"
          fill={STAGE.ink}
          fontSize={9}
          letterSpacing={1.2}
          fontFamily={ENGRAVE}
        >
          ONE ENDPOINT
        </text>
      </g>

      {DESKS.map((desk, i) => {
        const y = 46 + i * DESK_H;
        const cued = CUED.includes(i);
        return (
          <g key={desk.name}>
            <path
              d={`M ${ROSTRUM_X + 46} ${H / 2} C ${ROSTRUM_X + 110} ${H / 2}, ${DESK_X - 60} ${y + 22}, ${DESK_X - 8} ${y + 22}`}
              fill="none"
              stroke={cued ? desk.hue : STAGE.ruleFaint}
              strokeWidth={cued ? 1.6 : 1}
              strokeDasharray={cued ? 190 : "4 6"}
              strokeDashoffset={cued && moving ? 190 : 0}
              opacity={cued ? 1 : 0.5}
              className={cued && moving ? "op-cue" : undefined}
              style={
                cued && moving
                  ? { animationDelay: `${0.9 + i * 0.35}s` }
                  : undefined
              }
            />
            <g
              opacity={cued ? 1 : 0.35}
              className={cued && moving ? "op-desk" : undefined}
              style={
                cued && moving
                  ? { animationDelay: `${0.9 + i * 0.35}s` }
                  : undefined
              }
            >
              <rect
                x={DESK_X}
                y={y}
                width={244}
                height={44}
                rx={6}
                fill={STAGE.boards}
                stroke={cued ? desk.hue : STAGE.rule}
              />
              <rect
                x={DESK_X}
                y={y}
                width={4}
                height={44}
                rx={2}
                fill={desk.hue}
              />
              <text
                x={DESK_X + 16}
                y={y + 20}
                fill={STAGE.heading}
                fontSize={12}
                fontFamily={ENGRAVE}
              >
                {desk.name}
              </text>
              <text
                x={DESK_X + 16}
                y={y + 34}
                fill={STAGE.ink}
                fontSize={9}
                letterSpacing={1.2}
                fontFamily={ENGRAVE}
              >
                {`${desk.section.toUpperCase()} · ${desk.language} · SUBGRAPH`}
              </text>
            </g>
          </g>
        );
      })}

      <g className={moving ? "op-send" : undefined}>
        <rect
          x={160}
          y={H / 2 - 34}
          width={116}
          height={22}
          rx={11}
          fill={STAGE.rostrum}
          opacity={moving ? 1 : 0.8}
        />
        <text
          x={218}
          y={H / 2 - 19}
          textAnchor="middle"
          fill={STAGE.hall}
          fontSize={10}
          letterSpacing={1}
          fontFamily={ENGRAVE}
        >
          ONE QUERY
        </text>
      </g>

      <g className={moving ? "op-back" : undefined}>
        <rect
          x={160}
          y={H / 2 + 14}
          width={116}
          height={22}
          rx={11}
          fill={STAGE.brass}
          opacity={moving ? 1 : 0.8}
        />
        <text
          x={218}
          y={H / 2 + 29}
          textAnchor="middle"
          fill={STAGE.hall}
          fontSize={10}
          letterSpacing={1}
          fontFamily={ENGRAVE}
        >
          ONE RESPONSE
        </text>
      </g>
    </svg>
  );
}
