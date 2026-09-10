"use client";

import { anim, useSceneMotion } from "./hooks";
import { CN, PLANETS, PROBES } from "./palette";

/**
 * Nitro band visual: the observatory's sky survey.
 *
 * One column per body in the system - the gateway star first, then every
 * planet in its own colour - each drawn as a bar trace that breathes with the
 * latency, throughput and error rate the dashboard reports, while a survey
 * beam sweeps the strip and the registered probes are counted off on the left.
 * At rest the traces hold a still profile rather than collapsing to nothing.
 */

const VIEW_W = 640;
const VIEW_H = 240;
const BASE_Y = 190;

interface Column {
  readonly label: string;
  readonly colour: string;
  /** Deterministic rest profile, in bar heights. */
  readonly profile: readonly number[];
}

const COLUMNS: readonly Column[] = [
  {
    label: "Gateway",
    colour: CN.star,
    profile: [38, 54, 46, 62, 50, 58, 44],
  },
  ...PLANETS.map((planet, i) => ({
    label: planet.name,
    colour: planet.colour,
    profile: [22, 34, 28, 44, 30, 38, 26].map(
      (value) => value + ((i * 7) % 13) - 4,
    ),
  })),
];

const COL_W = VIEW_W / COLUMNS.length;

export function SkySurvey() {
  const running = useSceneMotion();

  return (
    <div className="absolute inset-0">
      <style>{`
        @keyframes cn9-ss-bar {
          0%, 100% { transform: scaleY(0.62); }
          50% { transform: scaleY(1.24); }
        }
        @keyframes cn9-ss-sweep {
          from { transform: translateX(-60px); }
          to { transform: translateX(${VIEW_W}px); }
        }
      `}</style>

      <svg
        viewBox={`0 0 ${VIEW_W} ${VIEW_H}`}
        preserveAspectRatio="xMidYMid meet"
        className="h-full w-full"
      >
        <text
          x={20}
          y={30}
          fill={CN.dim}
          fontSize="11"
          style={{ fontFamily: CN.mono, letterSpacing: "0.16em" }}
        >
          {`SKY SURVEY · LATENCY · THROUGHPUT · ERROR RATE · ${PROBES.length} REGISTERED PROBES`}
        </text>

        <line
          x1={0}
          y1={BASE_Y + 6}
          x2={VIEW_W}
          y2={BASE_Y + 6}
          stroke={CN.orbitFaint}
          strokeWidth="1"
        />

        {COLUMNS.map((column, c) => (
          <g key={column.label}>
            {column.profile.map((height, b) => {
              const x = c * COL_W + 16 + b * 11;

              return (
                <rect
                  key={b}
                  x={x}
                  y={BASE_Y - height}
                  width="6"
                  height={height}
                  rx="2"
                  fill={column.colour}
                  opacity={0.5 + (b % 3) * 0.18}
                  style={{
                    transformBox: "fill-box",
                    transformOrigin: "bottom",
                    animation: anim(
                      running,
                      `cn9-ss-bar ${2.6 + (b % 4) * 0.35}s ease-in-out ${
                        c * 0.18 + b * 0.12
                      }s infinite`,
                    ),
                  }}
                />
              );
            })}
            <text
              x={c * COL_W + 16}
              y={BASE_Y + 26}
              fill={CN.ink}
              fontSize="11"
              style={{ fontFamily: CN.mono, letterSpacing: "0.08em" }}
            >
              {column.label}
            </text>
          </g>
        ))}

        <g
          style={{
            animation: anim(running, "cn9-ss-sweep 9s linear infinite"),
          }}
        >
          <rect
            x={0}
            y={44}
            width="46"
            height={BASE_Y - 38}
            fill={CN.beam}
            opacity={running ? 0.08 : 0}
          />
          <line
            x1={46}
            y1={44}
            x2={46}
            y2={BASE_Y}
            stroke={CN.beam}
            strokeWidth="1"
            opacity={running ? 0.5 : 0}
          />
        </g>
      </svg>
    </div>
  );
}
