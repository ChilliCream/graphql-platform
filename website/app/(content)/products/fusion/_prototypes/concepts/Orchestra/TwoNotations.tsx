"use client";

import { TYPE } from "../../brand";
import { useReducedMotionPreference, useSceneActive } from "../../Primitives";
import { DESKS, ENGRAVE, GUESTS, STAGE, notation } from "./palette";

/**
 * "Both specifications, one gateway" visual: two notations on one score.
 *
 * Every staff carries the notation its team writes in, the reading light
 * crosses both kinds without stopping, and the Billing staff is re-engraved
 * from one notation to the other mid-piece while its bars stay identical - a
 * subgraph moved across without a coordinated cutover. The dashed staves at
 * the foot are the OpenAPI and gRPC guests joining the same score.
 */

const W = 720;
const H = 440;
const STAFF_X = 150;
const STAMP_X = 546;
const TOP = 96;
const ROW_H = 44;
const SWAP_ROW = 1;

const BARS = [12, 74, 128, 206, 268, 322, 388];

interface StaffRow {
  readonly name: string;
  readonly meta: string;
  readonly stamp: string;
  readonly hue: string;
  readonly guest: boolean;
  readonly swap: boolean;
}

const ROWS: readonly StaffRow[] = [
  ...DESKS.map((desk, i) => ({
    name: desk.name,
    meta: `${desk.section.toUpperCase()} · ${desk.language}`,
    stamp: notation(desk.spec),
    hue: desk.hue,
    guest: false,
    swap: i === SWAP_ROW,
  })),
  ...GUESTS.map((guest) => ({
    name: guest.name,
    meta: `${guest.kind.toUpperCase()} SOURCE`,
    stamp: `${guest.kind.toUpperCase()} · COMPOSED`,
    hue: guest.hue,
    guest: true,
    swap: false,
  })),
];

export function TwoNotations() {
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
        .tn-sweep { animation: tn-sweep 9s linear infinite; }
        .tn-a { animation: tn-a 9s ease-in-out infinite; }
        .tn-b { animation: tn-b 9s ease-in-out infinite; }
        .tn-note { animation: tn-note 9s ease-in-out infinite; }
        .tn-guest { animation: tn-guest 3.6s linear infinite; }
        @keyframes tn-sweep {
          from { transform: translateY(${TOP - 34}px); }
          to { transform: translateY(${TOP + ROWS.length * ROW_H}px); }
        }
        @keyframes tn-a {
          0%, 40% { opacity: 1; }
          48%, 88% { opacity: 0; }
          96%, 100% { opacity: 1; }
        }
        @keyframes tn-b {
          0%, 40% { opacity: 0; }
          48%, 88% { opacity: 1; }
          96%, 100% { opacity: 0; }
        }
        @keyframes tn-note {
          0%, 36%, 100% { opacity: 0; }
          50%, 82% { opacity: 1; }
        }
        @keyframes tn-guest { to { stroke-dashoffset: -24; } }
      `}</style>

      <rect width={W} height={H} fill={STAGE.hall} rx={2} />

      <text
        x={24}
        y={30}
        fill={STAGE.ink}
        fontSize={TYPE.label}
        letterSpacing={1.6}
        fontFamily={ENGRAVE}
      >
        ONE SCORE · TWO NOTATIONS
      </text>
      {["GraphQL Federation", "Apollo Federation"].map((spec, i) => (
        <g key={spec} transform={`translate(${24 + i * 210} 44)`}>
          <rect
            width={196}
            height={30}
            rx={15}
            fill={STAGE.boards}
            stroke={STAGE.rule}
          />
          <rect x={14} y={12} width={26} height={6} rx={3} fill={STAGE.brass} />
          <text
            x={52}
            y={20}
            fill={STAGE.heading}
            fontSize={TYPE.label}
            fontFamily={ENGRAVE}
          >
            {spec}
          </text>
        </g>
      ))}

      <g className={moving ? "tn-sweep" : undefined}>
        <rect
          x={STAFF_X - 12}
          y={0}
          width={W - STAFF_X - 4}
          height={30}
          fill={STAGE.rostrum}
          opacity={moving ? 0.14 : 0.08}
        />
      </g>

      {ROWS.map((row, i) => {
        const y = TOP + i * ROW_H;
        const { guest, swap } = row;
        return (
          <g key={row.name}>
            {[-8, 0, 8].map((d) => (
              <line
                key={d}
                x1={STAFF_X}
                x2={STAMP_X - 16}
                y1={y + d}
                y2={y + d}
                stroke={d === 0 ? STAGE.rule : STAGE.ruleFaint}
                strokeWidth={1}
                strokeDasharray={guest ? "6 6" : undefined}
                strokeDashoffset={0}
                className={guest && moving ? "tn-guest" : undefined}
              />
            ))}
            <text
              x={24}
              y={y + 2}
              fill={STAGE.heading}
              fontSize={TYPE.caption}
              fontFamily={ENGRAVE}
            >
              {row.name}
            </text>
            <text
              x={24}
              y={y + 16}
              fill={STAGE.ink}
              fontSize={TYPE.label}
              letterSpacing={0.8}
              fontFamily={ENGRAVE}
            >
              {row.meta}
            </text>

            {BARS.map((bx, b) => (
              <rect
                key={bx}
                x={STAFF_X + bx}
                y={y - 10 + ((b % 3) - 1) * 5}
                width={guest ? 18 : 24}
                height={6}
                rx={3}
                fill={row.hue}
                opacity={guest ? 0.5 : 0.8}
              />
            ))}

            {swap ? (
              <>
                <text
                  x={STAMP_X}
                  y={y + 4}
                  fill={STAGE.heading}
                  fontSize={TYPE.label}
                  letterSpacing={1.2}
                  fontFamily={ENGRAVE}
                  className={moving ? "tn-a" : undefined}
                >
                  {row.stamp}
                </text>
                <text
                  x={STAMP_X}
                  y={y + 4}
                  fill={STAGE.heading}
                  fontSize={TYPE.label}
                  letterSpacing={1.2}
                  fontFamily={ENGRAVE}
                  opacity={0}
                  className={moving ? "tn-b" : undefined}
                >
                  GRAPHQL FED
                </text>
                <text
                  x={W - 24}
                  y={y + 18}
                  textAnchor="end"
                  fill={STAGE.safe}
                  fontSize={TYPE.label}
                  letterSpacing={1.2}
                  fontFamily={ENGRAVE}
                  opacity={0}
                  className={moving ? "tn-note" : undefined}
                >
                  RE-ENGRAVED · SAME BARS
                </text>
              </>
            ) : (
              <text
                x={STAMP_X}
                y={y + 4}
                fill={STAGE.ink}
                fontSize={TYPE.label}
                letterSpacing={1.2}
                fontFamily={ENGRAVE}
              >
                {row.stamp}
              </text>
            )}
          </g>
        );
      })}

      <path
        d={`M ${W - 40} ${TOP - 18} q 10 0 10 12 v ${(ROWS.length * ROW_H) / 2 - 28} q 0 12 10 12 q -10 0 -10 12 v ${(ROWS.length * ROW_H) / 2 - 28} q 0 12 -10 12`}
        fill="none"
        stroke={STAGE.rostrum}
        strokeWidth={1.5}
        opacity={0.8}
      />
      <text
        x={W - 24}
        y={H - 16}
        textAnchor="end"
        fill={STAGE.heading}
        fontSize={TYPE.label}
        letterSpacing={1.4}
        fontFamily={ENGRAVE}
      >
        ONE COMPOSITE SCHEMA
      </text>
    </svg>
  );
}
