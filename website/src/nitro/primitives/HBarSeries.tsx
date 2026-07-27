import type { CSSProperties } from "react";
import { motion, useTransform, type MotionValue } from "motion/react";
import { clamp, compact, colorAt } from "../lib/scale";
import { ease } from "../lib/motion";
import { token, IMPACT_STOPS } from "../lib/tokens";
import { useChartClock } from "../lib/useInViewLoop";
import type { Client } from "../lib/data";
import { ChartCanvas } from "./ChartCanvas";

export interface BarItem {
  label: string;
  value: number;
  impact: number;
}

export interface HBarSeriesProps {
  clients?: Client[];
  items?: BarItem[];
  maxBars?: number;
  labelWidth?: number;
  barHeight?: number;
  progress?: MotionValue<number>;
  playWindow?: [number, number];
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

  const span = Math.max(0.001, 1 - (rows.length - 1) * rowStagger);

  const label =
    ariaLabel ??
    `Top ${rows.length} clients by request volume: ${rows
      .map(
        (r) => `${r.label} ${compact(r.value)}, impact ${Math.round(r.impact)}`,
      )
      .join(", ")}`;

  return (
    <ChartCanvas
      ref={ref}
      className={className}
      label={label}
      style={{
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
    </ChartCanvas>
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
  const grow = useTransform(t, [draw[0], draw[1]], [0, 1], {
    ease: ease.out,
    clamp: true,
  });

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
