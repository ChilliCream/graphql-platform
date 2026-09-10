"use client";

import { useReducedMotionPreference, useSceneActive } from "../../Primitives";
import { DUSK, LABEL, SOURCES, WAREHOUSES } from "./palette";

/**
 * "Both specifications, one gateway": every warehouse on the quay flies a
 * language flag and a specification pennant, and both pennants moor to the
 * same harbour. The Shipping warehouse swaps its pennant mid-loop while the
 * quay carries on, and the two dashed warehouses at the end publish an OpenAPI
 * document and a gRPC definition instead of a GraphQL schema.
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

const COL_W = 78;
const COL_GAP = 11;
const TOP = 168;
const BOTTOM = 344;
const QUAY_Y = 396;

interface Berth {
  readonly key: string;
  readonly name: string;
  readonly badge: string;
  readonly pennant: string;
  readonly dashed: boolean;
  readonly swaps: boolean;
}

const BERTHS: readonly Berth[] = [
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

const colX = (i: number) => 10 + i * (COL_W + COL_GAP);

const HARBOUR_X = colX(BERTHS.length - 1) / 2 + COL_W / 2;

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
      <svg
        viewBox="0 0 640 480"
        className="h-full w-full"
        role="img"
        aria-label="Warehouses flying GraphQL Federation and Apollo Federation pennants, plus OpenAPI and gRPC warehouses, all moored to one harbour."
      >
        <rect width="640" height="480" fill={DUSK.skyTop} />

        {BERTHS.map((berth, i) => {
          const x = colX(i);
          const mast = x + 8;
          return (
            <g key={berth.key}>
              {/* Mast, language flag, specification pennant */}
              <line
                x1={mast}
                y1={TOP - 82}
                x2={mast}
                y2={TOP}
                stroke={DUSK.edgeBright}
                strokeWidth={2}
              />
              <g
                className="hbr-q-flag"
                style={{ animationDelay: `${(i % 4) * 0.6}s` }}
              >
                <rect
                  x={mast}
                  y={TOP - 82}
                  width={COL_W - 20}
                  height={22}
                  fill={DUSK.accent}
                  opacity="0.75"
                />
                <text
                  x={mast + 6}
                  y={TOP - 66}
                  fill={DUSK.skyTop}
                  fontSize={11}
                  style={LABEL}
                >
                  {berth.badge}
                </text>
              </g>
              <g
                className="hbr-q-flag"
                style={{ animationDelay: `${(i % 4) * 0.6 + 0.4}s` }}
              >
                <path
                  d={`M${mast} ${TOP - 52} H${mast + COL_W - 16} L${mast + COL_W - 30} ${TOP - 42} L${mast + COL_W - 16} ${TOP - 32} H${mast} Z`}
                  fill={berth.dashed ? DUSK.quay : DUSK.lamp}
                  stroke={berth.dashed ? DUSK.edgeBright : "none"}
                  strokeDasharray={berth.dashed ? "4 3" : undefined}
                  opacity={berth.dashed ? 0.9 : 0.85}
                />
                {berth.swaps ? (
                  <>
                    <text
                      className="hbr-q-out"
                      x={mast + 5}
                      y={TOP - 38}
                      fill={DUSK.skyTop}
                      fontSize={9}
                      style={LABEL}
                    >
                      Apollo Fed
                    </text>
                    <text
                      className="hbr-q-in"
                      x={mast + 5}
                      y={TOP - 38}
                      fill={DUSK.skyTop}
                      fontSize={9}
                      style={LABEL}
                      opacity="0"
                    >
                      GraphQL Fed
                    </text>
                  </>
                ) : (
                  <text
                    x={mast + 5}
                    y={TOP - 38}
                    fill={berth.dashed ? DUSK.ink : DUSK.skyTop}
                    fontSize={9}
                    style={LABEL}
                  >
                    {berth.pennant}
                  </text>
                )}
              </g>

              {/* Warehouse */}
              <path
                d={`M${x} ${TOP} L${x + COL_W / 2} ${TOP - 24} L${x + COL_W} ${TOP} Z`}
                fill={DUSK.quayTop}
                opacity={berth.dashed ? 0.6 : 1}
              />
              <rect
                x={x}
                y={TOP}
                width={COL_W}
                height={BOTTOM - TOP}
                fill={DUSK.quay}
                stroke={berth.swaps ? DUSK.lamp : DUSK.edge}
                strokeDasharray={berth.dashed ? "5 4" : undefined}
                className={berth.swaps ? "hbr-q-pulse" : undefined}
                strokeOpacity={berth.swaps ? 0.25 : 1}
              />
              <text
                x={x + COL_W / 2}
                y={TOP + 40}
                textAnchor="middle"
                fill={DUSK.heading}
                fontSize={13}
              >
                {berth.name}
              </text>
              <rect
                x={x + 12}
                y={TOP + 96}
                width={COL_W - 24}
                height={BOTTOM - TOP - 96}
                fill={DUSK.skyTop}
                opacity="0.55"
              />

              {/* Mooring line down to the one harbour basin */}
              <path
                className="hbr-q-line"
                d={`M${x + COL_W / 2} ${BOTTOM} V${QUAY_Y - 28} Q${x + COL_W / 2} ${QUAY_Y} ${HARBOUR_X} ${QUAY_Y}`}
                fill="none"
                stroke={DUSK.shimmer}
                strokeWidth={1.5}
                strokeDasharray="8 12"
                opacity="0.5"
                style={{ animationDelay: `${i * 0.25}s` }}
              />
            </g>
          );
        })}

        {/* One harbour basin: the composite schema every mooring line reaches */}
        <rect
          x="10"
          y={QUAY_Y}
          width="620"
          height="52"
          rx="10"
          fill={DUSK.quayTop}
          stroke={DUSK.edge}
        />
        <text
          x="320"
          y={QUAY_Y + 32}
          textAnchor="middle"
          fill={DUSK.heading}
          fontSize={13}
          style={LABEL}
        >
          ONE COMPOSITE SCHEMA
        </text>
      </svg>
    </div>
  );
}
