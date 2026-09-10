"use client";

import { useReducedMotionPreference, useSceneActive } from "../../Primitives";
import { DESKS, ENGRAVE, GUESTS, STAGE, notation } from "./palette";

/**
 * Hero visual: the conductor's score, scrolling.
 *
 * Every source schema owns one staff, engraved with its section, language and
 * notation stamp; the two guest staves are the OpenAPI and gRPC sources. Bars
 * travel right to left through the baton line, each staff lights as its bar is
 * played, and the single line at the foot is what the hall hears - one
 * composite performance. At rest the score is simply printed and still.
 */

const W = 960;
const H = 380;
const GUTTER = 214;
const BATON_X = 660;
const ROW_H = 40;
const TOP = 34;
const PATTERN = 248;

const ROWS = [
  ...DESKS.map((desk) => ({
    key: desk.name,
    label: desk.name,
    section: desk.section,
    meta: `${desk.language} · ${notation(desk.spec)}`,
    hue: desk.hue,
    guest: false,
  })),
  ...GUESTS.map((guest) => ({
    key: guest.name,
    label: guest.name,
    section: "Guest",
    meta: `${guest.kind.toUpperCase()} · GUEST`,
    hue: guest.hue,
    guest: true,
  })),
];

/** Deterministic bar layout, so the server and the client engrave the same score. */
function bars(row: number): readonly { x: number; y: number; w: number }[] {
  const out: { x: number; y: number; w: number }[] = [];
  let x = 6 + ((row * 37) % 44);
  let n = row * 7 + 3;
  while (x < PATTERN - 10) {
    n = (n * 1103515245 + 12345) % 2147483648;
    const step = 26 + (n % 5) * 9;
    const w = 10 + ((n >> 5) % 3) * 7;
    const y = -6 + (((n >> 9) % 4) - 1) * 5;
    out.push({ x, y, w });
    x += step;
  }
  return out;
}

export function ScoreStrip() {
  const active = useSceneActive();
  const reduced = useReducedMotionPreference();
  const moving = active && !reduced;

  return (
    <svg
      viewBox={`0 0 ${W} ${H}`}
      className="absolute inset-0 h-full w-full"
      preserveAspectRatio="xMidYMid slice"
    >
      <style>{`
        .os-scroll { animation: os-scroll 11s linear infinite; }
        .os-row { animation: os-row 11s ease-in-out infinite; }
        .os-baton { animation: os-baton 2.75s ease-in-out infinite; transform-origin: ${BATON_X}px ${H - 40}px; }
        .os-piece { animation: os-piece 11s linear infinite; }
        @keyframes os-scroll { from { transform: translateX(0); } to { transform: translateX(-${PATTERN}px); } }
        @keyframes os-row {
          0%, 100% { opacity: 0.42; }
          8% { opacity: 1; }
          26% { opacity: 0.42; }
        }
        @keyframes os-baton {
          0%, 100% { transform: rotate(-13deg); }
          50% { transform: rotate(15deg); }
        }
        @keyframes os-piece { from { stroke-dashoffset: 0; } to { stroke-dashoffset: -${PATTERN}; } }
      `}</style>

      <rect width={W} height={H} fill={STAGE.hall} />

      {ROWS.map((row, i) => {
        const y = TOP + i * ROW_H;
        return (
          <g key={row.key}>
            {[-10, -5, 0, 5, 10].map((d) => (
              <line
                key={d}
                x1={GUTTER}
                x2={W}
                y1={y + d}
                y2={y + d}
                stroke={d === 0 ? STAGE.rule : STAGE.ruleFaint}
                strokeWidth={1}
                strokeDasharray={row.guest ? "3 5" : undefined}
              />
            ))}
            <text
              x={20}
              y={y - 2}
              fill={STAGE.heading}
              fontSize={13}
              fontFamily={ENGRAVE}
            >
              {row.label}
            </text>
            <text
              x={20}
              y={y + 12}
              fill={STAGE.ink}
              fontSize={9}
              letterSpacing={1.4}
              fontFamily={ENGRAVE}
            >
              {row.meta}
            </text>
            <rect
              x={GUTTER - 12}
              y={y - 12}
              width={4}
              height={24}
              rx={2}
              fill={row.hue}
              opacity={i === 0 && !moving ? 1 : 0.42}
              className={moving ? "os-row" : undefined}
              style={moving ? { animationDelay: `${-i * 0.55}s` } : undefined}
            />
          </g>
        );
      })}

      <defs>
        <clipPath id="os-staves">
          <rect
            x={GUTTER}
            y={TOP - 20}
            width={W - GUTTER}
            height={ROWS.length * ROW_H}
          />
        </clipPath>
      </defs>

      <g clipPath="url(#os-staves)">
        {ROWS.map((row, i) => {
          const y = TOP + i * ROW_H;
          const pattern = bars(i);
          return (
            <g
              key={row.key}
              className={moving ? "os-scroll" : undefined}
              style={moving ? { animationDelay: `${-i * 0.4}s` } : undefined}
            >
              {[0, 1, 2, 3, 4].map((copy) =>
                pattern.map((bar) => (
                  <rect
                    key={`${copy}-${bar.x}`}
                    x={GUTTER + copy * PATTERN + bar.x}
                    y={y + bar.y}
                    width={bar.w}
                    height={7}
                    rx={3.5}
                    fill={row.hue}
                    opacity={row.guest ? 0.55 : 0.85}
                  />
                )),
              )}
            </g>
          );
        })}
      </g>

      <line
        x1={BATON_X}
        x2={BATON_X}
        y1={TOP - 22}
        y2={H - 46}
        stroke={STAGE.rostrum}
        strokeWidth={1.5}
        opacity={0.7}
      />
      <g className={moving ? "os-baton" : undefined}>
        <line
          x1={BATON_X}
          x2={BATON_X + 44}
          y1={H - 40}
          y2={H - 74}
          stroke={STAGE.baton}
          strokeWidth={2.5}
          strokeLinecap="round"
        />
        <circle cx={BATON_X} cy={H - 40} r={5} fill={STAGE.rostrum} />
      </g>
      <text
        x={BATON_X - 10}
        y={H - 46}
        textAnchor="end"
        fill={STAGE.heading}
        fontSize={10}
        letterSpacing={1.6}
        fontFamily={ENGRAVE}
      >
        GATEWAY · CONDUCTOR
      </text>

      <line
        x1={GUTTER}
        x2={W}
        y1={H - 22}
        y2={H - 22}
        stroke={STAGE.rostrum}
        strokeWidth={3}
        strokeDasharray={`${PATTERN / 4} ${PATTERN / 4}`}
        opacity={0.85}
        className={moving ? "os-piece" : undefined}
      />
      <text
        x={20}
        y={H - 18}
        fill={STAGE.heading}
        fontSize={11}
        letterSpacing={1.4}
        fontFamily={ENGRAVE}
      >
        ONE PIECE
      </text>
    </svg>
  );
}
