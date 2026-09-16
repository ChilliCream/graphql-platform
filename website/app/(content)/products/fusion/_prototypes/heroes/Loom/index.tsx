"use client";

import { useRef } from "react";

import { CC } from "../../../tokens";
import { anim, useElementMotion } from "../../../visuals/hooks";
import { SERVICE_SPECTRUM } from "../../spectrum";

/**
 * Hero art: the five service colours run as warp threads into a woven
 * fabric band, with the unwoven strands trailing off to the right. A
 * shuttle sweeps the weave edge while the crossing threads shimmer along
 * the band; the rest frame is the wide finished band on its own.
 */

const VIEW_W = 1600;
const VIEW_H = 900;

/** The fabric band's vertical span and the five warp lanes inside it. */
const BAND_TOP = 292;
const BAND_HEIGHT = 336;
const LANE_GAP = BAND_HEIGHT / SERVICE_SPECTRUM.length;
const LANE_THICKNESS = LANE_GAP * 0.62;
const LANE_Y = SERVICE_SPECTRUM.map((_, i) => LANE_GAP * (i + 0.5));

/** Where the fabric ends and the loose warp threads begin. */
const WEAVE_X = 1000;
const HANDOFF = 90;
const EMERGE = HANDOFF * 0.6;
const FRAY_START = 1430;

/** One weft crossing pair: a strong "over" pick, then a soft "under" pick. */
const ROW_PITCH = 46;
const TILE_PITCH = ROW_PITCH * 2;
const TILE_COUNT = Math.ceil((WEAVE_X + HANDOFF) / TILE_PITCH) + 2;
const TICK_TILES = Array.from(
  { length: TILE_COUNT },
  (_, i) => -TILE_PITCH + i * TILE_PITCH,
);

/** Rest position and travel for the shuttle sweeping the weave edge. */
const SHUTTLE_Y = LANE_Y[2];
const SHUTTLE_X = WEAVE_X - 360;
const SHUTTLE_TRAVEL = 300;

const KEYFRAMES = `
@keyframes fx-loom-weave {
  from { transform: translateX(0); }
  to { transform: translateX(-${TILE_PITCH}px); }
}
@keyframes fx-loom-shuttle {
  0%, 100% { transform: translateX(0); }
  50% { transform: translateX(${SHUTTLE_TRAVEL}px); }
}
`;

/** A loose warp thread drifting apart from its lane as it trails off. */
function looseThreadPath(laneY: number, index: number): string {
  const y = BAND_TOP + laneY;
  const drift = (index - (SERVICE_SPECTRUM.length - 1) / 2) * 30;
  const wobble = index % 2 === 0 ? 16 : -16;

  return `M ${WEAVE_X} ${y} C ${WEAVE_X + 220} ${y + wobble}, ${WEAVE_X + 420} ${y - wobble}, ${WEAVE_X + 640} ${y + drift * 0.6} S ${VIEW_W - 60} ${y + drift}, ${VIEW_W} ${y + drift}`;
}

export default function Loom() {
  const ref = useRef<HTMLDivElement>(null);
  const running = useElementMotion(ref);

  return (
    <div ref={ref} className="absolute inset-0" aria-hidden="true">
      <style>{KEYFRAMES}</style>
      <svg
        className="absolute inset-0 h-full w-full"
        viewBox={`0 0 ${VIEW_W} ${VIEW_H}`}
        preserveAspectRatio="xMidYMid slice"
      >
        <defs>
          <linearGradient
            id="loom-emerge"
            x1={WEAVE_X}
            x2={WEAVE_X + EMERGE}
            y1="0"
            y2="0"
            gradientUnits="userSpaceOnUse"
          >
            <stop offset="0" stopColor="white" stopOpacity="0" />
            <stop offset="1" stopColor="white" stopOpacity="1" />
          </linearGradient>
          <linearGradient
            id="loom-handoff"
            x1={WEAVE_X}
            x2={WEAVE_X + HANDOFF}
            y1="0"
            y2="0"
            gradientUnits="userSpaceOnUse"
          >
            <stop offset="0" stopColor="white" stopOpacity="1" />
            <stop offset="1" stopColor="white" stopOpacity="0" />
          </linearGradient>
          <linearGradient
            id="loom-fray"
            x1={FRAY_START}
            x2={VIEW_W}
            y1="0"
            y2="0"
            gradientUnits="userSpaceOnUse"
          >
            <stop offset="0" stopColor="white" stopOpacity="1" />
            <stop offset="1" stopColor="white" stopOpacity="0.1" />
          </linearGradient>
          <mask
            id="loom-fabric-edge"
            maskUnits="userSpaceOnUse"
            x="0"
            y={BAND_TOP}
            width={VIEW_W}
            height={BAND_HEIGHT}
          >
            <rect
              x="0"
              y={BAND_TOP}
              width={WEAVE_X}
              height={BAND_HEIGHT}
              fill="white"
            />
            <rect
              x={WEAVE_X}
              y={BAND_TOP}
              width={HANDOFF}
              height={BAND_HEIGHT}
              fill="url(#loom-handoff)"
            />
          </mask>
          <mask
            id="loom-thread-fade"
            maskUnits="userSpaceOnUse"
            x="0"
            y="0"
            width={VIEW_W}
            height={VIEW_H}
          >
            <rect
              x={WEAVE_X}
              y="0"
              width={EMERGE}
              height={VIEW_H}
              fill="url(#loom-emerge)"
            />
            <rect
              x={WEAVE_X + EMERGE}
              y="0"
              width={FRAY_START - (WEAVE_X + EMERGE)}
              height={VIEW_H}
              fill="white"
            />
            <rect
              x={FRAY_START}
              y="0"
              width={VIEW_W - FRAY_START}
              height={VIEW_H}
              fill="url(#loom-fray)"
            />
          </mask>
          <clipPath id="loom-band-clip">
            <rect
              x="0"
              y={BAND_TOP}
              width={WEAVE_X + HANDOFF}
              height={BAND_HEIGHT}
            />
          </clipPath>
        </defs>

        {/* Woven fabric: the five warp colours, crossed by the shimmering weft. */}
        <g mask="url(#loom-fabric-edge)">
          {SERVICE_SPECTRUM.map((stop, i) => (
            <rect
              key={stop.label}
              x="0"
              y={BAND_TOP + i * LANE_GAP + (LANE_GAP - LANE_THICKNESS) / 2}
              width={WEAVE_X + HANDOFF}
              height={LANE_THICKNESS}
              rx={LANE_THICKNESS / 2}
              fill={stop.color}
            />
          ))}

          <g clipPath="url(#loom-band-clip)">
            <g
              style={{
                animation: anim(
                  running,
                  "fx-loom-weave 3400ms linear infinite",
                ),
              }}
            >
              {TICK_TILES.map((tileX) => (
                <g key={tileX} transform={`translate(${tileX}, 0)`}>
                  <rect
                    x={ROW_PITCH * 0.32}
                    y={BAND_TOP}
                    width={ROW_PITCH * 0.34}
                    height={BAND_HEIGHT}
                    fill={CC.ink}
                    opacity="0.5"
                  />
                  <rect
                    x={ROW_PITCH * 1.34}
                    y={BAND_TOP}
                    width={ROW_PITCH * 0.34}
                    height={BAND_HEIGHT}
                    fill={CC.ink}
                    opacity="0.2"
                  />
                </g>
              ))}
            </g>
          </g>
        </g>

        {/* Loose warp threads: still on the beam, not yet woven in. */}
        <g mask="url(#loom-thread-fade)" fill="none" strokeLinecap="round">
          {SERVICE_SPECTRUM.map((stop, i) => (
            <path
              key={stop.label}
              d={looseThreadPath(LANE_Y[i], i)}
              stroke={stop.color}
              strokeWidth="7"
            />
          ))}
        </g>

        {/* Shuttle sweeping the weave edge. */}
        <g
          transform={`translate(${SHUTTLE_X}, ${BAND_TOP + SHUTTLE_Y})`}
          style={{
            animation: anim(
              running,
              "fx-loom-shuttle 4200ms ease-in-out infinite",
            ),
          }}
        >
          <ellipse cx="0" cy="18" rx="46" ry="7" fill={CC.ink} opacity="0.12" />
          <rect
            x="-58"
            y="-9"
            width="116"
            height="18"
            rx="9"
            fill={CC.surface}
            stroke={CC.heading}
            strokeWidth="1.5"
          />
          <rect
            x="-38"
            y="-3"
            width="76"
            height="6"
            rx="3"
            fill={CC.heading}
            opacity="0.5"
          />
        </g>
      </svg>

      {/* Scrim so the hero copy stays readable over the fabric. */}
      <div className="from-cc-bg via-cc-bg/85 pointer-events-none absolute inset-y-0 left-0 w-full max-w-4xl bg-gradient-to-r from-0% via-75% to-transparent" />
    </div>
  );
}
