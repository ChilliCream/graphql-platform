/**
 * HBarSeries — horizontal bars that grow via scaleX, colored by impact.
 *
 * The "top clients" ranking: each row is a labelled track whose fill grows from the left
 * edge and is tinted along the low→high impact gradient (green → amber → red). Growth is
 * driven by a normalized clock `t` (0→1) from `useChartClock`, so it animates standalone in
 * its story and also slots into the Monitoring Overview's shared cycle. Rows are staggered
 * via per-index sub-windows of `t`.
 *
 * Built from semantic HTML (not SVG) so the labels stay crisp at any size — the only
 * animated geometry is each fill's `scaleX`, which scales cleanly from `transformOrigin:left`.
 */
import type { CSSProperties } from "react";
import { motion, useTransform, type MotionValue } from "motion/react";
import { clamp, compact, colorAt } from "../lib/scale";
import { ease } from "../lib/motion";
import { token, IMPACT_STOPS } from "../lib/tokens";
import { useChartClock } from "../lib/useInViewLoop";
import type { Client } from "../lib/data";

/** Generic bar item — pass these directly, or let `clients` map into them. */
export interface BarItem {
  label: string;
  value: number;
  /** 0..100 → position along the impact gradient */
  impact: number;
}

export interface HBarSeriesProps {
  /** Convenience: telemetry clients, mapped to `{ label: name, value: total, impact }`. */
  clients?: Client[];
  /** Generic items; takes precedence over `clients` when both are supplied. */
  items?: BarItem[];
  /** Cap the number of rows (after the implicit value-desc sort). */
  maxBars?: number;
  /** Width of the left label column, in px. */
  labelWidth?: number;
  /** Height of each track, in px. */
  barHeight?: number;
  /** shared master clock (overview); omit for a self-contained standalone loop */
  progress?: MotionValue<number>;
  playWindow?: [number, number];
  /** fraction of the local window each successive row is offset by */
  rowStagger?: number;
  durationMs?: number;
  ariaLabel?: string;
  className?: string;
  style?: CSSProperties;
}

const toItem = (c: Client): BarItem => ({
  label: c.name,
  value: c.total,
  impact: c.impact,
});

export function HBarSeries({
  clients,
  items,
  maxBars = 6,
  labelWidth = 96,
  barHeight = 14,
  progress,
  playWindow,
  rowStagger = 0.12,
  durationMs,
  ariaLabel,
  className,
  style,
}: HBarSeriesProps) {
  const { ref, t } = useChartClock({ progress, playWindow, durationMs });

  const rows = (items ?? clients?.map(toItem) ?? [])
    .slice()
    .sort((a, b) => b.value - a.value)
    .slice(0, maxBars);

  const max = Math.max(1, ...rows.map((r) => r.value));

  // Each row claims a sub-window [s0,s1] of t, shifted by index so they cascade in.
  const span = Math.max(0.001, 1 - (rows.length - 1) * rowStagger);

  // Include impact in the text — on screen it's encoded only as the bar's gradient color,
  // so it must not be color-only for assistive tech.
  const label =
    ariaLabel ??
    `Top ${rows.length} clients by request volume: ${rows
      .map(
        (r) => `${r.label} ${compact(r.value)}, impact ${Math.round(r.impact)}`,
      )
      .join(", ")}`;

  return (
    <div
      ref={ref}
      className={className}
      role="img"
      aria-label={label}
      style={{
        position: "relative",
        width: "100%",
        height: "100%",
        display: "flex",
        flexDirection: "column",
        justifyContent: "center",
        gap: 8,
        fontFamily: token.font,
        ...style,
      }}
    >
      {rows.map((row, i) => {
        const s0 = Math.min(i * rowStagger, 0.99);
        const s1 = Math.min(s0 + span, 1);
        return (
          <Bar
            key={`${row.label}-${i}`}
            row={row}
            max={max}
            labelWidth={labelWidth}
            barHeight={barHeight}
            t={t}
            draw={[s0, s1]}
          />
        );
      })}
    </div>
  );
}

function Bar({
  row,
  max,
  labelWidth,
  barHeight,
  t,
  draw,
}: {
  row: BarItem;
  max: number;
  labelWidth: number;
  barHeight: number;
  t: MotionValue<number>;
  draw: [number, number];
}) {
  // 0→1 growth across this row's window, easing in with a soft settle.
  const grow = useTransform(t, [draw[0], draw[1]], [0, 1], {
    ease: ease.out,
    clamp: true,
  });

  // Colour reads position along the SHARED low→high ramp: each bar goes from the ramp
  // start to the ramp colour at its own length (longer bars reach the warm end).
  const frac = clamp(row.value / max, 0, 1);
  const fill = `linear-gradient(90deg, ${colorAt(IMPACT_STOPS, 0)}, ${colorAt(IMPACT_STOPS, frac)})`;

  return (
    <div style={{ display: "flex", alignItems: "center", gap: 12 }}>
      <span
        title={row.label}
        style={{
          flex: `0 0 ${labelWidth}px`,
          width: labelWidth,
          color: token.text,
          fontSize: 12,
          whiteSpace: "nowrap",
          overflow: "hidden",
          textOverflow: "ellipsis",
        }}
      >
        {row.label}
      </span>
      <div style={{ flex: 1, height: barHeight, position: "relative" }}>
        <motion.div
          style={{
            width: `${frac * 100}%`,
            height: "100%",
            borderRadius: 3,
            background: fill,
            transformOrigin: "left",
            scaleX: grow,
          }}
        />
      </div>
    </div>
  );
}
