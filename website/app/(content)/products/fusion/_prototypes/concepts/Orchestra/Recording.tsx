"use client";

import { useReducedMotionPreference, useSceneActive } from "../../Primitives";
import { ENGRAVE, LISTENERS, STAGE } from "./palette";

/**
 * "Composition protects the graph, Nitro protects your clients" visual.
 *
 * A desk re-engraves its part and one bar disappears; the rehearsal still
 * passes, because nothing in composition knows what the hall listens to.
 * Nitro plays the recording of what real clients actually heard against the
 * rewritten part, finds the take that needed the missing bar and calls it
 * breaking before the change is merged. At rest the gap, the green
 * composition stamp and the breaking verdict are all already visible.
 */

const W = 720;
const H = 440;
const PART_X = 24;
const PART_W = 372;
const REC_Y = 208;
const GAP_INDEX = 4;
const BARS = [0, 1, 2, 3, 4, 5, 6];
/** How far the playback head travels across the takes. */
const HEAD_TRAVEL = 268;
/** The take that needed the removed bar. */
const BREAKING_TAKE = 1;

export function Recording() {
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
        .rc-erase { animation: rc-erase 10s ease-in-out infinite; }
        .rc-gap { animation: rc-gap 10s ease-in-out infinite; }
        .rc-green { animation: rc-green 10s ease-in-out infinite; }
        .rc-head { animation: rc-head 10s linear infinite; }
        .rc-hit { animation: rc-hit 10s ease-in-out infinite; }
        .rc-verdict { animation: rc-verdict 10s ease-in-out infinite; }
        @keyframes rc-erase {
          0%, 8% { opacity: 1; }
          18%, 100% { opacity: 0; }
        }
        @keyframes rc-gap {
          0%, 10% { opacity: 0; }
          20%, 100% { opacity: 1; }
        }
        @keyframes rc-green {
          0%, 22% { opacity: 0; }
          30%, 100% { opacity: 1; }
        }
        @keyframes rc-head {
          0%, 34% { transform: translateX(0); opacity: 0; }
          38% { opacity: 1; }
          72%, 100% { transform: translateX(${HEAD_TRAVEL}px); opacity: 1; }
        }
        @keyframes rc-hit {
          0%, 58% { opacity: 0; }
          64%, 100% { opacity: 1; }
        }
        @keyframes rc-verdict {
          0%, 66% { opacity: 0.2; }
          74%, 100% { opacity: 1; }
        }
      `}</style>

      <rect width={W} height={H} fill={STAGE.hall} rx={2} />

      <text
        x={PART_X}
        y={28}
        fill={STAGE.ink}
        fontSize={10}
        letterSpacing={1.6}
        fontFamily={ENGRAVE}
      >
        THE REWRITTEN PART · CATALOG
      </text>

      {[-9, 0, 9].map((d) => (
        <line
          key={d}
          x1={PART_X}
          x2={PART_X + PART_W}
          y1={70 + d}
          y2={70 + d}
          stroke={d === 0 ? STAGE.rule : STAGE.ruleFaint}
        />
      ))}
      {BARS.map((b) => {
        const x = PART_X + 10 + b * 52;
        if (b === GAP_INDEX) {
          return (
            <g key={b}>
              <rect
                x={x}
                y={64}
                width={38}
                height={9}
                rx={4.5}
                fill={STAGE.strings}
                className={moving ? "rc-erase" : undefined}
                opacity={moving ? 1 : 0}
              />
              <rect
                x={x - 3}
                y={56}
                width={44}
                height={26}
                rx={5}
                fill="none"
                stroke={STAGE.clash}
                strokeDasharray="4 4"
                className={moving ? "rc-gap" : undefined}
                opacity={moving ? 0 : 1}
              />
              <text
                x={x + 19}
                y={100}
                textAnchor="middle"
                fill={STAGE.clash}
                fontSize={9}
                letterSpacing={1}
                fontFamily={ENGRAVE}
                className={moving ? "rc-gap" : undefined}
                opacity={moving ? 0 : 1}
              >
                FIELD REMOVED
              </text>
            </g>
          );
        }
        return (
          <rect
            key={b}
            x={x}
            y={64}
            width={38}
            height={9}
            rx={4.5}
            fill={STAGE.strings}
            opacity={0.85}
          />
        );
      })}

      <g className={moving ? "rc-green" : undefined} opacity={moving ? 0 : 1}>
        <rect
          x={PART_X}
          y={124}
          width={PART_W}
          height={38}
          rx={8}
          fill={STAGE.boards}
          stroke={STAGE.safe}
        />
        <circle cx={PART_X + 22} cy={143} r={5} fill={STAGE.safe} />
        <text
          x={PART_X + 38}
          y={147}
          fill={STAGE.heading}
          fontSize={10}
          letterSpacing={1.2}
          fontFamily={ENGRAVE}
        >
          SOURCE SCHEMAS STILL COMPOSE · BUILD GREEN
        </text>
      </g>

      <text
        x={PART_X}
        y={REC_Y - 18}
        fill={STAGE.ink}
        fontSize={10}
        letterSpacing={1.6}
        fontFamily={ENGRAVE}
      >
        THE RECORDING · WHAT REGISTERED CLIENTS ACTUALLY PLAY
      </text>

      {LISTENERS.map((listener, i) => {
        const y = REC_Y + 16 + i * 40;
        const breaking = i === BREAKING_TAKE;
        return (
          <g key={listener}>
            <text
              x={PART_X}
              y={y + 4}
              fill={STAGE.heading}
              fontSize={11}
              fontFamily={ENGRAVE}
            >
              {listener}
            </text>
            <g transform={`translate(${PART_X + 92} ${y})`}>
              {BARS.map((b) => {
                const needsGap = breaking && b === GAP_INDEX;
                return (
                  <rect
                    key={b}
                    x={b * 40}
                    y={-7 + ((b % 3) - 1) * 2}
                    width={30}
                    height={14}
                    rx={4}
                    fill={needsGap ? STAGE.clash : STAGE.rostrum}
                    opacity={needsGap ? 0.9 : 0.45}
                  />
                );
              })}
            </g>
          </g>
        );
      })}

      <g
        className={moving ? "rc-head" : undefined}
        style={
          moving ? undefined : { transform: `translateX(${HEAD_TRAVEL}px)` }
        }
      >
        <line
          x1={PART_X + 96}
          x2={PART_X + 96}
          y1={REC_Y}
          y2={REC_Y + 158}
          stroke={STAGE.heading}
          strokeWidth={1.5}
          opacity={0.7}
        />
      </g>

      <g
        className={moving ? "rc-hit" : undefined}
        opacity={moving ? 0 : 1}
        transform={`translate(0 ${REC_Y + 16 + BREAKING_TAKE * 40})`}
      >
        <rect
          x={PART_X + 88 + GAP_INDEX * 40}
          y={-14}
          width={38}
          height={28}
          rx={6}
          fill="none"
          stroke={STAGE.clash}
          strokeWidth={1.5}
        />
      </g>

      <rect
        x={W - 264}
        y={124}
        width={240}
        height={124}
        rx={10}
        fill={STAGE.boards}
        stroke={STAGE.rule}
      />
      <text
        x={W - 244}
        y={150}
        fill={STAGE.heading}
        fontSize={11}
        letterSpacing={1.4}
        fontFamily={ENGRAVE}
      >
        NITRO · SCHEMA GOVERNANCE
      </text>
      {[
        { label: "SAFE", hue: STAGE.safe },
        { label: "RISKY", hue: STAGE.brass },
        { label: "BREAKING", hue: STAGE.clash },
      ].map((verdict, i) => {
        const chosen = verdict.label === "BREAKING";
        return (
          <g
            key={verdict.label}
            className={chosen && moving ? "rc-verdict" : undefined}
            opacity={chosen ? (moving ? 0.2 : 1) : 0.35}
          >
            <rect
              x={W - 244}
              y={164 + i * 26}
              width={10}
              height={10}
              rx={2}
              fill={verdict.hue}
            />
            <text
              x={W - 226}
              y={173 + i * 26}
              fill={chosen ? verdict.hue : STAGE.ink}
              fontSize={10}
              letterSpacing={1.2}
              fontFamily={ENGRAVE}
            >
              {verdict.label}
            </text>
          </g>
        );
      })}
      <text
        x={W - 244}
        y={236}
        fill={STAGE.ink}
        fontSize={9}
        letterSpacing={1.1}
        fontFamily={ENGRAVE}
      >
        BEFORE THE CHANGE IS MERGED
      </text>
    </svg>
  );
}
