"use client";

import { TYPE } from "../../brand";
import { useReducedMotionPreference, useSceneActive } from "../../Primitives";
import { DESKS, ENGRAVE, STAGE } from "./palette";

/**
 * Nitro band visual: the house meters.
 *
 * One channel for the gateway and one for every desk behind it, each riding
 * its own level - the latency, throughput and error rate Nitro's Fusion
 * dashboard reports. At rest the meters simply hold their levels, which is
 * still a readable console.
 */

const W = 520;
const H = 230;
const CH_W = 62;
const TRACK_TOP = 52;
const TRACK_H = 118;

const CHANNELS = [
  { name: "Gateway", hue: STAGE.rostrum, level: 0.72 },
  ...DESKS.slice(0, 4).map((desk, i) => ({
    name: desk.name,
    hue: desk.hue,
    level: 0.42 + ((i * 17) % 41) / 100,
  })),
];

export function HouseMeters() {
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
        .hm-level { animation: hm-level 4.6s ease-in-out infinite; transform-origin: 50% 100%; }
        @keyframes hm-level {
          0%, 100% { transform: scaleY(1); }
          28% { transform: scaleY(0.62); }
          54% { transform: scaleY(1.18); }
          78% { transform: scaleY(0.84); }
        }
      `}</style>

      <rect width={W} height={H} fill={STAGE.hall} rx={2} />
      <text
        x={20}
        y={28}
        fill={STAGE.ink}
        fontSize={TYPE.label}
        letterSpacing={1.6}
        fontFamily={ENGRAVE}
      >
        LATENCY · THROUGHPUT · ERROR RATE
      </text>

      {CHANNELS.map((channel, i) => {
        const x = 24 + i * (CH_W + 34);
        const height = TRACK_H * channel.level;
        return (
          <g key={channel.name}>
            <rect
              x={x}
              y={TRACK_TOP}
              width={CH_W}
              height={TRACK_H}
              rx={6}
              fill={STAGE.boards}
              stroke={STAGE.ruleFaint}
            />
            {[0.25, 0.5, 0.75].map((t) => (
              <line
                key={t}
                x1={x}
                x2={x + CH_W}
                y1={TRACK_TOP + TRACK_H * t}
                y2={TRACK_TOP + TRACK_H * t}
                stroke={STAGE.ruleFaint}
              />
            ))}
            <g
              className={moving ? "hm-level" : undefined}
              style={
                moving
                  ? {
                      animationDelay: `${-i * 0.7}s`,
                      transformOrigin: `${x + CH_W / 2}px ${TRACK_TOP + TRACK_H}px`,
                    }
                  : undefined
              }
            >
              <rect
                x={x + 8}
                y={TRACK_TOP + TRACK_H - height}
                width={CH_W - 16}
                height={height}
                rx={4}
                fill={channel.hue}
                opacity={0.85}
              />
            </g>
            <text
              x={x + CH_W / 2}
              y={TRACK_TOP + TRACK_H + 22}
              textAnchor="middle"
              fill={STAGE.heading}
              fontSize={TYPE.caption}
              fontFamily={ENGRAVE}
            >
              {channel.name}
            </text>
            <text
              x={x + CH_W / 2}
              y={TRACK_TOP + TRACK_H + 38}
              textAnchor="middle"
              fill={STAGE.ink}
              fontSize={TYPE.label}
              letterSpacing={1.1}
              fontFamily={ENGRAVE}
            >
              {i === 0 ? "COMPOSITE" : "SUBGRAPH"}
            </text>
          </g>
        );
      })}
    </svg>
  );
}
