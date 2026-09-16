"use client";

import { useRef } from "react";

import { CC, FONTS } from "../../../tokens";
import { anim, useElementMotion } from "../../../visuals/hooks";
import { SERVICE_SPECTRUM } from "../../spectrum";
import {
  BASELINE_Y,
  JUNCTION_X,
  LANE_BOTTOM,
  LANE_CENTERS,
  LANE_START_X,
  LANE_TOP,
  PERIOD,
  VIEW_H,
  VIEW_W,
  channelPath,
  compositePath,
  leadInPath,
} from "./waveform";

/**
 * Hero art: an oscilloscope view where the five service-coloured traces on
 * the right meet a summing junction at centre and resolve into one calm
 * white composite trace under the copy, with a mixing-desk grid, channel
 * ticks and a mono readout. The rest frame is a frozen sweep of the same
 * traces.
 */

const GRID_STEP = 100;
const VERTICAL_GRID_LINES = Array.from(
  { length: VIEW_W / GRID_STEP + 1 },
  (_, i) => i * GRID_STEP,
);
const HORIZONTAL_GRID_LINES = Array.from(
  { length: VIEW_H / GRID_STEP + 1 },
  (_, i) => i * GRID_STEP,
);

const READOUT = { x: 1175, y: 784, width: 250, height: 60 };

const KEYFRAMES = `
@keyframes fx-waveformmix-scroll {
  from { transform: translateX(0); }
  to { transform: translateX(-${PERIOD}px); }
}
`;

export default function WaveformMix() {
  const ref = useRef<HTMLDivElement>(null);
  const running = useElementMotion(ref);
  const scroll = anim(running, "fx-waveformmix-scroll 5000ms linear infinite");

  return (
    <div ref={ref} className="absolute inset-0" aria-hidden="true">
      <style>{KEYFRAMES}</style>
      <svg
        className="absolute inset-0 h-full w-full"
        viewBox={`0 0 ${VIEW_W} ${VIEW_H}`}
        preserveAspectRatio="xMidYMid slice"
      >
        <defs>
          <clipPath id="waveformmix-composite-clip">
            <rect x="0" y="0" width={JUNCTION_X} height={VIEW_H} />
          </clipPath>
          <clipPath id="waveformmix-channel-clip">
            <rect
              x={LANE_START_X}
              y={LANE_TOP}
              width={VIEW_W - LANE_START_X}
              height={LANE_BOTTOM - LANE_TOP}
            />
          </clipPath>
        </defs>

        {/* Faint mixing-desk grid across the whole hero. */}
        <g stroke={CC.ink} strokeWidth="1" opacity="0.06">
          {VERTICAL_GRID_LINES.map((x) => (
            <line key={`v-${x}`} x1={x} y1="0" x2={x} y2={VIEW_H} />
          ))}
          {HORIZONTAL_GRID_LINES.map((y) => (
            <line key={`h-${y}`} x1="0" y1={y} x2={VIEW_W} y2={y} />
          ))}
        </g>

        {/* Channel strip: lane dividers and the boundary where the traces begin. */}
        <g stroke={CC.ink} opacity="0.14">
          <line
            x1={LANE_START_X}
            y1={LANE_TOP}
            x2={LANE_START_X}
            y2={LANE_BOTTOM}
            strokeWidth="1.5"
          />
          {LANE_CENTERS.map((_, i) => {
            const y = LANE_TOP + ((LANE_BOTTOM - LANE_TOP) / 5) * i;
            return (
              <line
                key={`lane-${y}`}
                x1={LANE_START_X}
                y1={y}
                x2={VIEW_W}
                y2={y}
                strokeWidth="1"
              />
            );
          })}
        </g>

        {/* Channel ticks: a small coloured chip per lane at the right edge. */}
        {SERVICE_SPECTRUM.map((stop, i) => (
          <rect
            key={stop.label}
            x={VIEW_W - 26}
            y={LANE_CENTERS[i] - 3}
            width="14"
            height="6"
            rx="3"
            fill={stop.color}
          />
        ))}

        {/* Static wiring from the summing junction to each channel's trace. */}
        <g fill="none" strokeWidth="3" strokeLinecap="round" opacity="0.55">
          {SERVICE_SPECTRUM.map((stop, i) => (
            <path key={stop.label} d={leadInPath(i)} stroke={stop.color} />
          ))}
        </g>

        {/* The five channel traces, scrolling together. */}
        <g clipPath="url(#waveformmix-channel-clip)">
          <g style={{ animation: scroll }}>
            {SERVICE_SPECTRUM.map((stop, i) => (
              <path
                key={stop.label}
                d={channelPath(i)}
                fill="none"
                stroke={stop.color}
                strokeWidth="4"
                strokeLinecap="round"
              />
            ))}
          </g>
        </g>

        {/* The summing junction. */}
        <circle
          cx={JUNCTION_X}
          cy={BASELINE_Y}
          r="26"
          fill={CC.white}
          opacity="0.12"
        />
        <circle
          cx={JUNCTION_X}
          cy={BASELINE_Y}
          r="9"
          fill={CC.white}
          stroke={CC.heading}
          strokeWidth="1.5"
        />

        {/* The composite trace, mixed from all five channels, scrolling in lockstep. */}
        <g clipPath="url(#waveformmix-composite-clip)">
          <g style={{ animation: scroll }}>
            <path
              d={compositePath()}
              fill="none"
              stroke={CC.white}
              strokeWidth="5"
              strokeLinecap="round"
            />
          </g>
        </g>

        {/* Mono readout: a small mixing-desk panel. */}
        <rect
          x={READOUT.x}
          y={READOUT.y}
          width={READOUT.width}
          height={READOUT.height}
          rx="10"
          fill={CC.cardBg}
          stroke={CC.cardBorder}
          strokeWidth="1"
        />
        <text
          x={READOUT.x + READOUT.width / 2}
          y={READOUT.y + READOUT.height / 2 + 10}
          textAnchor="middle"
          fill={CC.ink}
          fontFamily={FONTS.mono}
          fontSize="30"
          letterSpacing="1.5"
        >
          SUM 5→1
        </text>
      </svg>

      {/* Scrim so the hero copy stays readable over the composite trace. */}
      <div className="from-cc-bg via-cc-bg/85 pointer-events-none absolute inset-y-0 left-0 w-full max-w-4xl bg-gradient-to-r from-0% via-75% to-transparent" />
    </div>
  );
}
