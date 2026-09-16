"use client";

import { useRef } from "react";

import { CC } from "../../../tokens";
import { anim, useElementMotion } from "../../../visuals/hooks";
import { SERVICE_SPECTRUM } from "../../spectrum";
import {
  BRAID_CENTER_Y,
  ENTRY_X,
  MERGE_FADE_X,
  MERGE_WIDTH,
  MERGE_X0,
  RIBBON_WIDTH,
  VIEW_H,
  VIEW_W,
  buildBraidQuads,
  buildEntryPaths,
  buildMergeQuads,
} from "./geometry";

/**
 * Hero art: five ribbons in the service colours enter from the top right and
 * braid together in visible crossing order as they descend, becoming one
 * bright ribbon that passes behind the copy and fades out at the left. The
 * rest frame is the completed braid; running motion undulates the strands
 * and drifts the braid gently side to side.
 */

const ENTRY_PATHS = buildEntryPaths();
const BRAID_QUADS = buildBraidQuads();
const MERGE_QUADS = buildMergeQuads();

const KEYFRAMES = `
@keyframes fx-auroraribbons-drift {
  from { transform: translateX(0); }
  to { transform: translateX(-18px); }
}
@keyframes fx-auroraribbons-undulate-a {
  0%, 100% { transform: translateY(0); }
  50% { transform: translateY(-7px); }
}
@keyframes fx-auroraribbons-undulate-b {
  0%, 100% { transform: translateY(0); }
  50% { transform: translateY(7px); }
}
@keyframes fx-auroraribbons-glow {
  0%, 100% { opacity: 0.92; }
  50% { opacity: 1; }
}
`;

export default function AuroraRibbons() {
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
            id="aurora-merge"
            x1="0"
            x2={ENTRY_X}
            y1="0"
            y2="0"
            gradientUnits="userSpaceOnUse"
          >
            {SERVICE_SPECTRUM.map((stop, i) => (
              <stop
                key={stop.label}
                offset={i / (SERVICE_SPECTRUM.length - 1)}
                stopColor={stop.color}
              />
            ))}
          </linearGradient>
          <linearGradient
            id="aurora-fade"
            x1="0"
            x2={MERGE_FADE_X}
            y1="0"
            y2="0"
            gradientUnits="userSpaceOnUse"
          >
            <stop offset="0" stopColor={CC.white} stopOpacity="0" />
            <stop offset="1" stopColor={CC.white} stopOpacity="1" />
          </linearGradient>
          <mask
            id="aurora-merge-mask"
            maskUnits="userSpaceOnUse"
            x="0"
            y="0"
            width={VIEW_W}
            height={VIEW_H}
          >
            <rect
              x="0"
              y="0"
              width={MERGE_FADE_X}
              height={VIEW_H}
              fill="url(#aurora-fade)"
            />
            <rect
              x={MERGE_FADE_X}
              y="0"
              width={VIEW_W - MERGE_FADE_X}
              height={VIEW_H}
              fill={CC.white}
            />
          </mask>
        </defs>

        {/* Merged ribbon: one bright band continuing from the braid to the left edge, fading out. */}
        <rect
          x="0"
          y={BRAID_CENTER_Y - MERGE_WIDTH / 2}
          width={MERGE_X0}
          height={MERGE_WIDTH}
          rx={MERGE_WIDTH / 2}
          fill="url(#aurora-merge)"
          mask="url(#aurora-merge-mask)"
          style={{
            animation: anim(
              running,
              "fx-auroraribbons-glow 6000ms ease-in-out infinite",
            ),
          }}
        />

        {/* Entry strands, braid quads and merge-band quads share this one drift. */}
        <g
          style={{
            animation: anim(
              running,
              "fx-auroraribbons-drift 9000ms ease-in-out infinite alternate",
            ),
          }}
        >
          {BRAID_QUADS.map((quad) => (
            <polygon
              key={quad.key}
              points={quad.points}
              fill={SERVICE_SPECTRUM[quad.index].color}
              fillOpacity={quad.opacity}
            />
          ))}

          {/* Merge-band quads: each braid strand continues past the handoff, fading out. */}
          {MERGE_QUADS.map((quad) => (
            <polygon
              key={quad.key}
              points={quad.points}
              fill={SERVICE_SPECTRUM[quad.index].color}
              fillOpacity={quad.opacity}
            />
          ))}

          {/* Entry strands, undulating gently before they reach the braid. */}
          {ENTRY_PATHS.map(({ index, d }) => (
            <path
              key={SERVICE_SPECTRUM[index].label}
              d={d}
              fill="none"
              stroke={SERVICE_SPECTRUM[index].color}
              strokeWidth={RIBBON_WIDTH}
              strokeLinecap="round"
              style={{
                animation: anim(
                  running,
                  `fx-auroraribbons-undulate-${index % 2 === 0 ? "a" : "b"} ${5200 + index * 420}ms ease-in-out infinite`,
                ),
              }}
            />
          ))}
        </g>
      </svg>

      {/* Scrim behind the hero copy, over the merged ribbon. */}
      <div className="from-cc-bg/60 via-cc-bg/30 pointer-events-none absolute inset-y-0 left-0 w-full max-w-4xl bg-gradient-to-r via-60% to-transparent" />
    </div>
  );
}
