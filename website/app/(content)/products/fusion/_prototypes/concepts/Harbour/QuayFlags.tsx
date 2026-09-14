"use client";

import { useReducedMotionPreference, useSceneActive } from "../../Primitives";
import { DUSK, FONT, LABEL, LABEL_TIGHT, SOURCES, WAREHOUSES } from "./palette";

/**
 * "Both specifications, one gateway": every warehouse on the quay flies a
 * language flag and a specification pennant, and both pennants moor to the
 * same harbour. The Shipping warehouse swaps its pennant mid-loop while the
 * quay carries on, and the two dashed warehouses at the end publish an OpenAPI
 * document and a gRPC definition instead of a GraphQL schema.
 *
 * The quay is drawn in two ranks: at a phone width, one rank of seven berths
 * leaves no room for a flag anyone can read, so the back rank moors behind the
 * front one and every label stays at the site's 11px floor.
 *
 * Rest state: the full quay with each warehouse flying its declared pennant.
 */

const CSS = `
.hbr-quay [class*="hbr-q-"] { animation-play-state: running; }
.hbr-quay[data-run="false"] [class*="hbr-q-"] { animation: none; }
.hbr-quay .hbr-q-flag { animation: hbr-q-flag 5s ease-in-out infinite; transform-box: fill-box; transform-origin: left center; }
.hbr-quay .hbr-q-line { animation: hbr-q-line 3.2s linear infinite; }
.hbr-quay .hbr-q-out { animation: hbr-q-out 10s ease-in-out infinite; }
.hbr-quay .hbr-q-in { animation: hbr-q-in 10s ease-in-out infinite; }
.hbr-quay .hbr-q-pulse { animation: hbr-q-pulse 10s ease-in-out infinite; }
@keyframes hbr-q-flag {
  0%, 100% { transform: skewY(0deg) scaleX(1); }
  50% { transform: skewY(-3.5deg) scaleX(0.94); }
}
@keyframes hbr-q-line { to { stroke-dashoffset: -40; } }
@keyframes hbr-q-out {
  0%, 34% { opacity: 1; }
  44%, 76% { opacity: 0; }
  86%, 100% { opacity: 1; }
}
@keyframes hbr-q-in {
  0%, 34% { opacity: 0; }
  44%, 76% { opacity: 1; }
  86%, 100% { opacity: 0; }
}
@keyframes hbr-q-pulse {
  0%, 30%, 90%, 100% { stroke-opacity: 0.25; }
  44%, 76% { stroke-opacity: 0.8; }
}
`;

const PITCH = 160;
const COL_W = 144;
const BOX_H = 80;
/** Top of the warehouse box in each rank. */
const FRONT_TOP = 140;
const BACK_TOP = 330;
const BASIN_Y = 430;
const HARBOUR_X = 320;

interface Berth {
  readonly key: string;
  readonly name: string;
  readonly badge: string;
  readonly pennant: string;
  readonly dashed: boolean;
  readonly swaps: boolean;
}

const ALL_BERTHS: readonly Berth[] = [
  ...WAREHOUSES.map((w) => ({
    key: w.name,
    name: w.name,
    badge: w.language,
    pennant: w.pennant,
    dashed: false,
    swaps: w.name === "Shipping",
  })),
  ...SOURCES.map((s) => ({
    key: s.name,
    name: s.name,
    badge: s.kind,
    pennant: s.kind,
    dashed: true,
    swaps: false,
  })),
];

/** Front rank along the water; back rank moored behind it. */
const FRONT = ALL_BERTHS.slice(0, 4);
const BACK = ALL_BERTHS.slice(4);

interface QuayBerthProps {
  readonly berth: Berth;
  /** Left edge of the berth's slot on the quay. */
  readonly x: number;
  /** Top of the warehouse box. */
  readonly top: number;
  /** Animation offset, so the rank does not wave in lockstep. */
  readonly delay: number;
}

function QuayBerth({ berth, x, top, delay }: QuayBerthProps) {
  const mast = x + 4;
  const box = x + 8;

  return (
    <g>
      {/* Mast, language flag, specification pennant */}
      <line
        x1={mast}
        y1={top - 92}
        x2={mast}
        y2={top}
        stroke={DUSK.edgeBright}
        strokeWidth={2}
      />
      <g className="hbr-q-flag" style={{ animationDelay: `${delay}s` }}>
        <rect
          x={mast}
          y={top - 92}
          width={130}
          height={28}
          fill={DUSK.accent}
          opacity="0.75"
        />
        <text
          x={mast + 8}
          y={top - 72}
          fill={DUSK.skyTop}
          fontSize={FONT.label}
          style={LABEL}
        >
          {berth.badge}
        </text>
      </g>
      <g className="hbr-q-flag" style={{ animationDelay: `${delay + 0.4}s` }}>
        <path
          d={`M${mast} ${top - 56} H${mast + 154} L${mast + 140} ${top - 42} L${mast + 154} ${top - 28} H${mast} Z`}
          fill={berth.dashed ? DUSK.quay : DUSK.lamp}
          stroke={berth.dashed ? DUSK.edgeBright : "none"}
          strokeDasharray={berth.dashed ? "4 3" : undefined}
          opacity={berth.dashed ? 0.9 : 0.85}
        />
        {berth.swaps ? (
          <>
            <text
              className="hbr-q-out"
              x={mast + 4}
              y={top - 36}
              fill={DUSK.skyTop}
              fontSize={FONT.label}
              style={LABEL_TIGHT}
            >
              Apollo Fed
            </text>
            <text
              className="hbr-q-in"
              x={mast + 4}
              y={top - 36}
              fill={DUSK.skyTop}
              fontSize={FONT.label}
              style={LABEL_TIGHT}
              opacity="0"
            >
              GraphQL Fed
            </text>
          </>
        ) : (
          <text
            x={mast + 4}
            y={top - 36}
            fill={berth.dashed ? DUSK.ink : DUSK.skyTop}
            fontSize={FONT.label}
            style={LABEL_TIGHT}
          >
            {berth.pennant}
          </text>
        )}
      </g>

      {/* Warehouse */}
      <path
        d={`M${box} ${top} L${box + COL_W / 2} ${top - 16} L${box + COL_W} ${top} Z`}
        fill={DUSK.quayTop}
        opacity={berth.dashed ? 0.6 : 1}
      />
      <rect
        x={box}
        y={top}
        width={COL_W}
        height={BOX_H}
        fill={DUSK.quay}
        stroke={berth.swaps ? DUSK.lamp : DUSK.edge}
        strokeDasharray={berth.dashed ? "5 4" : undefined}
        className={berth.swaps ? "hbr-q-pulse" : undefined}
        strokeOpacity={berth.swaps ? 0.25 : 1}
      />
      <text
        x={box + COL_W / 2}
        y={top + 34}
        textAnchor="middle"
        fill={DUSK.heading}
        fontSize={FONT.label}
      >
        {berth.name}
      </text>
      <rect
        x={box + 12}
        y={top + 48}
        width={COL_W - 24}
        height={BOX_H - 48}
        fill={DUSK.skyTop}
        opacity="0.55"
      />
    </g>
  );
}

interface MooringProps {
  /** Centre of the berth the line leaves from. */
  readonly x: number;
  /** Bottom of that berth's warehouse. */
  readonly from: number;
  readonly delay: number;
}

/** Mooring line down to the one harbour basin. */
function Mooring({ x, from, delay }: MooringProps) {
  return (
    <path
      className="hbr-q-line"
      d={`M${x} ${from} V${BASIN_Y - 30} Q${x} ${BASIN_Y} ${HARBOUR_X} ${BASIN_Y}`}
      fill="none"
      stroke={DUSK.shimmer}
      strokeWidth={1.5}
      strokeDasharray="8 12"
      opacity="0.5"
      style={{ animationDelay: `${delay}s` }}
    />
  );
}

const frontX = (i: number) => i * PITCH;
const backX = (i: number) => PITCH / 2 + i * PITCH;

export function QuayFlags() {
  const active = useSceneActive();
  const reduced = useReducedMotionPreference();
  const run = active && !reduced;

  return (
    <div
      className="hbr-quay absolute inset-0"
      data-run={run ? "true" : "false"}
    >
      <style>{CSS}</style>
      <svg viewBox="0 0 640 480" className="h-full w-full" aria-hidden="true">
        <rect width="640" height="480" fill={DUSK.skyTop} />

        {/* Front rank, and its mooring lines running behind the back rank */}
        {FRONT.map((berth, i) => (
          <Mooring
            key={berth.key}
            x={frontX(i) + 8 + COL_W / 2}
            from={FRONT_TOP + BOX_H}
            delay={i * 0.25}
          />
        ))}
        {FRONT.map((berth, i) => (
          <QuayBerth
            key={berth.key}
            berth={berth}
            x={frontX(i)}
            top={FRONT_TOP}
            delay={(i % 4) * 0.6}
          />
        ))}

        {/* Back rank */}
        {BACK.map((berth, i) => (
          <Mooring
            key={berth.key}
            x={backX(i) + 8 + COL_W / 2}
            from={BACK_TOP + BOX_H}
            delay={(i + 4) * 0.25}
          />
        ))}
        {BACK.map((berth, i) => (
          <QuayBerth
            key={berth.key}
            berth={berth}
            x={backX(i)}
            top={BACK_TOP}
            delay={((i + 4) % 4) * 0.6}
          />
        ))}

        {/* One harbour basin: the composite schema every mooring line reaches */}
        <rect
          x="10"
          y={BASIN_Y}
          width="620"
          height="48"
          rx="10"
          fill={DUSK.quayTop}
          stroke={DUSK.edge}
        />
        <text
          x={HARBOUR_X}
          y={BASIN_Y + 30}
          textAnchor="middle"
          fill={DUSK.heading}
          fontSize={FONT.label}
          style={LABEL}
        >
          ONE COMPOSITE SCHEMA
        </text>
      </svg>
    </div>
  );
}
