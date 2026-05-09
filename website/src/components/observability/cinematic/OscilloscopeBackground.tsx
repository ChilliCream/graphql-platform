"use client";

import React from "react";

// Ambient oscilloscope/ECG-style background for the cinematic
// /products/nitro/observability page. Renders multiple horizontal hairline
// waveform traces stacked vertically, a faint vertical time grid, ms time
// labels along the bottom edge, and one trace with a single amplitude spike
// to suggest a captured event. The component is purely decorative and is
// hidden from assistive tech.
//
// The SVG fills its container with `position: absolute; inset: 0;` and is
// pointer-events: none so it never intercepts clicks. Wrap it in a
// `position: relative` ancestor (the cinematic root does this) so it spans
// the full page height.

export interface OscilloscopeBackgroundProps {
  className?: string;
}

// SVG viewBox dimensions. The viewBox is fixed so the math for the trace
// paths and tick grid is deterministic; the SVG scales via
// `preserveAspectRatio="none"` so it stretches to its container.
const VB_W = 1440;
const VB_H = 1800;

// Wave band layout. Each trace occupies a vertical band of `BAND_H`; traces
// are centered inside their band so the oscillation has equal headroom on
// either side. `TOP_PAD` leaves room above the first wave and `BOTTOM_PAD`
// reserves the strip where time labels render.
const BAND_H = 280;
const TOP_PAD = 80;
const BOTTOM_PAD = 60;

// Vertical tick grid. One tick every `TICK_X` units of viewBox width.
const TICK_X = 120;

// Sample step for path generation. Smaller = smoother curve, larger = lighter
// DOM. 6px is a good balance for hairline strokes at 1px.
const SAMPLE_STEP = 6;

interface Trace {
  /** Period in viewBox units (one full sine cycle). */
  period: number;
  /** Peak amplitude in viewBox units. */
  amplitude: number;
  /** Phase offset in viewBox units. */
  phase: number;
  /** Stroke opacity. Brighter traces simulate an "active" signal. */
  opacity: number;
  /** Optional spike: at `spikeX` add `spikeAmp` for one half-cycle. */
  spike?: { x: number; amp: number };
  /** Optional secondary harmonic added to the base sine. */
  harmonic?: { period: number; amplitude: number };
}

const TRACES: Trace[] = [
  {
    period: 320,
    amplitude: 22,
    phase: 0,
    opacity: 0.12,
  },
  {
    period: 220,
    amplitude: 14,
    phase: 60,
    opacity: 0.18,
    harmonic: { period: 90, amplitude: 4 },
  },
  {
    period: 480,
    amplitude: 30,
    phase: 120,
    opacity: 0.12,
    // Captured-event spike on the third trace. Positioned roughly two-thirds
    // across so it sits within the visible band on most viewports.
    spike: { x: 940, amp: 56 },
  },
  {
    period: 180,
    amplitude: 10,
    phase: 30,
    opacity: 0.18,
  },
  {
    period: 360,
    amplitude: 18,
    phase: 90,
    opacity: 0.12,
    harmonic: { period: 110, amplitude: 3 },
  },
  {
    period: 260,
    amplitude: 16,
    phase: 200,
    opacity: 0.12,
  },
];

const TIME_LABELS = ["0ms", "100ms", "200ms", "300ms", "400ms", "500ms"];

// Compose a horizontal sine path for one trace. The path samples y values at
// `SAMPLE_STEP` intervals across the viewBox width and adds an optional
// half-cycle spike near `spike.x` to simulate a captured event.
function buildTracePath(trace: Trace, centerY: number): string {
  const { period, amplitude, phase, harmonic, spike } = trace;
  const segments: string[] = [];

  for (let x = 0; x <= VB_W; x += SAMPLE_STEP) {
    const base = Math.sin(((x + phase) / period) * Math.PI * 2) * amplitude;
    const harm = harmonic
      ? Math.sin((x / harmonic.period) * Math.PI * 2) * harmonic.amplitude
      : 0;

    let spikeY = 0;
    if (spike) {
      const dist = Math.abs(x - spike.x);
      // Half-cycle envelope, ~40 units wide on each side. Smooth cosine
      // ramp so the spike doesn't introduce a hard discontinuity.
      const half = 40;
      if (dist < half) {
        const t = dist / half;
        spikeY = -spike.amp * Math.cos((t * Math.PI) / 2);
      }
    }

    const y = centerY + base + harm + spikeY;
    segments.push(`${x === 0 ? "M" : "L"}${x.toFixed(1)} ${y.toFixed(2)}`);
  }

  return segments.join(" ");
}

// Reference dashes drawn at the trace center line. A few short hairlines on
// each side suggest amplitude tick marks like a real scope graticule.
function buildReferenceDashes(centerY: number): React.ReactNode {
  const dashes: React.ReactNode[] = [];
  // Place 4 dashes spread across the band, avoiding the edges.
  const positions = [220, 540, 860, 1220];
  for (const x of positions) {
    dashes.push(
      <line
        key={`dash-${centerY}-${x}`}
        x1={x - 6}
        x2={x + 6}
        y1={centerY}
        y2={centerY}
        stroke="rgba(96, 200, 220, 0.18)"
        strokeWidth={0.75}
      />
    );
  }
  return dashes;
}

/**
 * Oscilloscope-style ambient background. Multiple hairline waveform traces
 * stacked vertically over a faint time grid, with ms axis labels along the
 * bottom edge. Decorative only.
 */
export const OscilloscopeBackground: React.FC<OscilloscopeBackgroundProps> = ({
  className,
}) => {
  // Vertical tick gridline x positions.
  const ticks: number[] = [];
  for (let x = TICK_X; x < VB_W; x += TICK_X) {
    ticks.push(x);
  }

  return (
    <div
      aria-hidden="true"
      className={className}
      style={{
        position: "absolute",
        inset: 0,
        zIndex: 0,
        pointerEvents: "none",
        overflow: "hidden",
      }}
    >
      <svg
        xmlns="http://www.w3.org/2000/svg"
        viewBox={`0 0 ${VB_W} ${VB_H}`}
        preserveAspectRatio="none"
        style={{
          position: "absolute",
          inset: 0,
          width: "100%",
          height: "100%",
          display: "block",
        }}
      >
        {/* Vertical tick grid. Sub-pixel hairline so it reads as a faint
            graticule on a dark canvas without ever asserting itself. */}
        <g>
          {ticks.map((x) => (
            <line
              key={`tick-${x}`}
              x1={x}
              x2={x}
              y1={0}
              y2={VB_H - BOTTOM_PAD + 8}
              stroke="rgba(245, 241, 234, 0.04)"
              strokeWidth={0.5}
            />
          ))}
        </g>

        {/* Waveform traces. Each trace sits in its own horizontal band so
            the page reads as a stack of independent live signals. */}
        <g>
          {TRACES.map((trace, i) => {
            const centerY = TOP_PAD + i * BAND_H + BAND_H / 2;
            const path = buildTracePath(trace, centerY);
            return (
              <g key={`trace-${i}`}>
                {buildReferenceDashes(centerY)}
                <path
                  d={path}
                  fill="none"
                  stroke={`rgba(96, 200, 220, ${trace.opacity})`}
                  strokeWidth={1}
                  strokeLinejoin="round"
                  strokeLinecap="round"
                />
              </g>
            );
          })}
        </g>

        {/* Time axis labels along the bottom edge. Mono, tiny, dim, evenly
            distributed across the same number of buckets as the page. */}
        <g>
          {TIME_LABELS.map((label, i) => {
            const x = (VB_W / (TIME_LABELS.length - 1)) * i;
            // Anchor first label at the left edge, last at the right edge,
            // and middle labels centered on their tick position.
            let textAnchor: "start" | "middle" | "end" = "middle";
            if (i === 0) {
              textAnchor = "start";
            } else if (i === TIME_LABELS.length - 1) {
              textAnchor = "end";
            }
            return (
              <text
                key={`label-${label}`}
                x={x}
                y={VB_H - 24}
                fill="rgba(245, 241, 234, 0.18)"
                fontFamily="var(--cc-font-mono), ui-monospace, monospace"
                fontSize={9}
                letterSpacing={1.4}
                textAnchor={textAnchor}
              >
                {label}
              </text>
            );
          })}
        </g>
      </svg>
    </div>
  );
};
