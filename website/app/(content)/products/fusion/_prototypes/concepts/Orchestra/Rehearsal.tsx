"use client";

import { useReducedMotionPreference, useSceneActive } from "../../Primitives";
import { DESKS, ENGRAVE, STAGE } from "./palette";

/**
 * "Any GraphQL server, no plugin" visual: the rehearsal.
 *
 * Each player brings an ordinary instrument - a GraphQL server in its own
 * language, with no runtime package bolted on - and the rehearsal is
 * composition: the conductor reads the parts against one another bar by bar
 * until two of them clash, and the run stops there, before the concert. At
 * rest the clashing bar is already marked and the plaque reads stopped.
 */

const W = 720;
const H = 420;
const CARD_W = 162;
const CARD_H = 92;
const STRIP_Y = 250;
const STRIP_X = 40;
const STRIP_W = 640;
const CLASH_X = 424;

const PLAYERS = DESKS.slice(0, 4);

export function Rehearsal() {
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
        .rh-head { animation: rh-head 8s ease-in-out infinite; }
        .rh-read { animation: rh-read 8s ease-in-out infinite; }
        .rh-clash { animation: rh-clash 8s ease-in-out infinite; }
        .rh-run { animation: rh-run 8s ease-in-out infinite; }
        .rh-stop { animation: rh-stop 8s ease-in-out infinite; }
        @keyframes rh-head {
          0% { transform: translateX(0); opacity: 0; }
          6% { opacity: 1; }
          52%, 88% { transform: translateX(${CLASH_X - STRIP_X}px); opacity: 1; }
          96%, 100% { transform: translateX(${CLASH_X - STRIP_X}px); opacity: 0; }
        }
        @keyframes rh-read {
          0% { clip-path: inset(0 100% 0 0); }
          52%, 100% { clip-path: inset(0 ${((STRIP_W - (CLASH_X - STRIP_X)) / STRIP_W) * 100}% 0 0); }
        }
        @keyframes rh-clash {
          0%, 50% { opacity: 0; transform: scale(0.7); }
          58%, 92% { opacity: 1; transform: scale(1); }
          100% { opacity: 0; transform: scale(0.7); }
        }
        @keyframes rh-run {
          0%, 50% { opacity: 1; }
          56%, 100% { opacity: 0; }
        }
        @keyframes rh-stop {
          0%, 50% { opacity: 0; }
          58%, 92% { opacity: 1; }
          100% { opacity: 0; }
        }
      `}</style>

      <rect width={W} height={H} fill={STAGE.hall} rx={2} />

      <text
        x={24}
        y={28}
        fill={STAGE.ink}
        fontSize={10}
        letterSpacing={1.6}
        fontFamily={ENGRAVE}
      >
        REHEARSAL · THE PARTS AS THEY ARRIVE
      </text>

      {PLAYERS.map((desk, i) => {
        const x = 24 + i * (CARD_W + 8);
        return (
          <g key={desk.name}>
            <rect
              x={x}
              y={48}
              width={CARD_W}
              height={CARD_H}
              rx={8}
              fill={STAGE.boards}
              stroke={STAGE.rule}
            />
            <rect
              x={x}
              y={48}
              width={CARD_W}
              height={3}
              rx={1.5}
              fill={desk.hue}
            />
            <text
              x={x + 14}
              y={74}
              fill={STAGE.heading}
              fontSize={12}
              fontFamily={ENGRAVE}
            >
              {desk.name}
            </text>
            <text
              x={x + 14}
              y={90}
              fill={STAGE.ink}
              fontSize={9}
              letterSpacing={1.1}
              fontFamily={ENGRAVE}
            >
              {`${desk.language} · ${desk.section.toUpperCase()}`}
            </text>
            <text
              x={x + 14}
              y={110}
              fill={STAGE.ink}
              fontSize={9}
              letterSpacing={1.1}
              fontFamily={ENGRAVE}
            >
              PLAIN SERVER
            </text>
            <text
              x={x + 14}
              y={126}
              fill={STAGE.safe}
              fontSize={9}
              letterSpacing={1.1}
              fontFamily={ENGRAVE}
            >
              NO PLUGIN
            </text>
          </g>
        );
      })}

      <text
        x={24}
        y={188}
        fill={STAGE.heading}
        fontSize={12}
        letterSpacing={1.4}
        fontFamily={ENGRAVE}
      >
        COMPOSITION · THE ONE BUILD STEP YOU ADD
      </text>
      <text
        x={24}
        y={206}
        fill={STAGE.ink}
        fontSize={9}
        letterSpacing={1.2}
        fontFamily={ENGRAVE}
      >
        READS THE PARTS AGAINST ONE ANOTHER, BAR BY BAR
      </text>

      {PLAYERS.map((desk, i) => {
        const y = STRIP_Y + i * 22;
        return (
          <g key={desk.name}>
            <line
              x1={STRIP_X}
              x2={STRIP_X + STRIP_W}
              y1={y}
              y2={y}
              stroke={STAGE.ruleFaint}
              strokeWidth={1}
            />
            <g
              className={moving ? "rh-read" : undefined}
              style={
                moving
                  ? undefined
                  : {
                      clipPath: `inset(0 ${((STRIP_W - (CLASH_X - STRIP_X)) / STRIP_W) * 100}% 0 0)`,
                    }
              }
            >
              {[0, 1, 2, 3, 4, 5, 6, 7].map((b) => (
                <rect
                  key={b}
                  x={STRIP_X + 10 + b * 78}
                  y={y - 5}
                  width={54}
                  height={9}
                  rx={4.5}
                  fill={desk.hue}
                  opacity={0.85}
                />
              ))}
            </g>
          </g>
        );
      })}

      <g
        className={moving ? "rh-head" : undefined}
        style={
          moving
            ? undefined
            : { transform: `translateX(${CLASH_X - STRIP_X}px)` }
        }
      >
        <line
          x1={STRIP_X}
          x2={STRIP_X}
          y1={STRIP_Y - 20}
          y2={STRIP_Y + 3 * 22 + 20}
          stroke={STAGE.rostrum}
          strokeWidth={2}
        />
      </g>

      <g
        className={moving ? "rh-clash" : undefined}
        style={{ transformOrigin: `${CLASH_X}px ${STRIP_Y + 33}px` }}
      >
        <rect
          x={CLASH_X - 14}
          y={STRIP_Y - 18}
          width={28}
          height={3 * 22 + 26}
          rx={6}
          fill={STAGE.clash}
          opacity={0.16}
        />
        <line
          x1={CLASH_X - 9}
          x2={CLASH_X + 9}
          y1={STRIP_Y + 24}
          y2={STRIP_Y + 42}
          stroke={STAGE.clash}
          strokeWidth={2.5}
          strokeLinecap="round"
        />
        <line
          x1={CLASH_X + 9}
          x2={CLASH_X - 9}
          y1={STRIP_Y + 24}
          y2={STRIP_Y + 42}
          stroke={STAGE.clash}
          strokeWidth={2.5}
          strokeLinecap="round"
        />
      </g>

      <rect
        x={24}
        y={H - 62}
        width={W - 48}
        height={42}
        rx={8}
        fill={STAGE.boards}
        stroke={STAGE.rule}
      />
      <g className={moving ? "rh-run" : undefined} opacity={moving ? 1 : 0}>
        <circle cx={48} cy={H - 41} r={5} fill={STAGE.rostrum} />
        <text
          x={64}
          y={H - 37}
          fill={STAGE.heading}
          fontSize={11}
          letterSpacing={1.2}
          fontFamily={ENGRAVE}
        >
          COMPOSING · BUILD RUNNING
        </text>
      </g>
      <g className={moving ? "rh-stop" : undefined} opacity={moving ? 0 : 1}>
        <circle cx={48} cy={H - 41} r={5} fill={STAGE.clash} />
        <text
          x={64}
          y={H - 37}
          fill={STAGE.clash}
          fontSize={11}
          letterSpacing={1.2}
          fontFamily={ENGRAVE}
        >
          BUILD STOPPED · TYPE CONFLICT · NOTHING REACHES THE CONCERT
        </text>
      </g>
    </svg>
  );
}
