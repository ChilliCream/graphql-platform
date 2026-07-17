/**
 * TraceWaterfall — a trace flamegraph (Operation Detail "Trace Sample" reference). Each
 * span is a row: a colored bar positioned by (start, duration) on a linear time axis, with
 * an icon + name + duration label. Bars grow left→right, staggered top→bottom, off the
 * shared clock. A time ruler + dashed gridlines sit above. Colors encode span kind.
 */
import type { CSSProperties } from "react";
import { motion, useTransform, type MotionValue } from "motion/react";
import { ease } from "../lib/motion";
import { token } from "../lib/tokens";
import { useChartClock } from "../lib/useInViewLoop";
import type { Trace, TraceSpan, SpanKindWf } from "../lib/data";

export interface TraceWaterfallProps {
  trace: Trace;
  rowHeight?: number;
  progress?: MotionValue<number>;
  playWindow?: [number, number];
  durationMs?: number;
  /** Play the standalone draw-in a single time on first view, then hold (no loop). */
  once?: boolean;
  ariaLabel?: string;
  className?: string;
  style?: CSSProperties;
}

const KIND_COLOR: Record<SpanKindWf, string> = {
  server: token.info,
  http: token.cThroughput,
  graphql: token.active,
  internal: token.accent,
};
const KIND_ICON: Record<SpanKindWf, string> = {
  server: "🌐",
  http: "↪",
  graphql: "◈",
  internal: "⚙",
};

const fmtDur = (d: number) =>
  d >= 1 ? `${d.toFixed(1)} ms` : `${Math.round(d * 1000)} µs`;

/** Smallest 1/2/5×10ⁿ value ≥ raw, so tick steps land on round numbers. */
const niceStep = (raw: number) => {
  const mag = Math.pow(10, Math.floor(Math.log10(raw)));
  for (const m of [1, 2, 5]) {
    if (m * mag >= raw) {
      return m * mag;
    }
  }
  return 10 * mag;
};

export function TraceWaterfall({
  trace,
  rowHeight = 34,
  progress,
  playWindow,
  durationMs,
  once,
  ariaLabel,
  className,
  style,
}: TraceWaterfallProps) {
  const { ref, t } = useChartClock({ progress, playWindow, durationMs, once });
  const total = trace.totalMs;
  // ms ticks within [0, total); the total is shown as a right-anchored end label.
  // Short traces keep the 1ms ruler; wider ones step by a nice 1/2/5×10ⁿ value
  // targeting ~5 ticks so a 300ms trace doesn't emit hundreds of labels.
  const step = total <= 12 ? 1 : niceStep(total / 5);
  // Stop at 90% so the last tick label cannot collide with the right-anchored
  // total label (wide traces render wider tick labels like "300ms").
  const ticks: number[] = [];
  for (let tk = 0; tk / total < 0.9; tk += step) {
    ticks.push(tk);
  }
  const n = trace.spans.length;
  const label =
    ariaLabel ?? `Trace waterfall: ${n} spans over ${fmtDur(total)}`;

  return (
    <div
      ref={ref}
      className={className}
      style={{ position: "relative", width: "100%", ...style }}
      role="img"
      aria-label={label}
    >
      {/* time ruler */}
      <div style={{ position: "relative", height: 16, marginBottom: 4 }}>
        {ticks.map((tk) => (
          <span
            key={tk}
            style={{
              position: "absolute",
              left: `${(tk / total) * 100}%`,
              top: 0,
              transform: tk === 0 ? "none" : "translateX(-50%)",
              fontSize: 9,
              color: token.textSecondary,
              whiteSpace: "nowrap",
            }}
          >
            {tk}ms
          </span>
        ))}
        <span
          style={{
            position: "absolute",
            right: 0,
            top: 0,
            fontSize: 9,
            color: token.textSecondary,
            whiteSpace: "nowrap",
          }}
        >
          {fmtDur(total)}
        </span>
      </div>

      {/* rows + gridlines */}
      <div style={{ position: "relative" }}>
        {[...ticks, total].map((tk, i) => (
          <div
            key={i}
            style={{
              position: "absolute",
              left: `${(tk / total) * 100}%`,
              top: 0,
              bottom: 0,
              width: 0,
              borderLeft: `1px dashed ${tk === total ? token.borderStrong : token.grid}`,
              pointerEvents: "none",
            }}
          />
        ))}
        {trace.spans.map((s, i) => (
          <Span
            key={s.id}
            span={s}
            total={total}
            rowHeight={rowHeight}
            frac={i / Math.max(1, n - 1)}
            t={t}
          />
        ))}
      </div>
    </div>
  );
}

function Span({
  span,
  total,
  rowHeight,
  frac,
  t,
}: {
  span: TraceSpan;
  total: number;
  rowHeight: number;
  frac: number;
  t: MotionValue<number>;
}) {
  const s0 = frac * 0.65;
  const grow = useTransform(t, [s0, s0 + 0.22], [0, 1], {
    ease: ease.out,
    clamp: true,
  });
  const labelOpacity = useTransform(t, [s0 + 0.1, s0 + 0.28], [0, 1], {
    clamp: true,
  });
  const color = KIND_COLOR[span.kind];
  const left = (span.startMs / total) * 100;
  const width = Math.max(0.4, (span.durationMs / total) * 100);

  return (
    <div style={{ position: "relative", height: rowHeight }}>
      <motion.div
        style={{
          position: "absolute",
          left: `${left}%`,
          top: 3,
          width: `${width}%`,
          height: 11,
          borderRadius: 3,
          background: color,
          transformOrigin: "left",
          scaleX: grow,
        }}
      />
      {/* Late-starting spans anchor their label to the bar's right end so the
          text extends leftward instead of clipping past the row's edge. */}
      <motion.div
        style={{
          position: "absolute",
          ...(left > 62
            ? { right: `${Math.max(0, 100 - left - width)}%` }
            : { left: `${left}%` }),
          top: 16,
          fontSize: 11,
          color: token.text,
          whiteSpace: "nowrap",
          opacity: labelOpacity,
          display: "flex",
          gap: 6,
          alignItems: "center",
        }}
      >
        <span style={{ color }}>{KIND_ICON[span.kind]}</span>
        <span style={{ color: token.textStrong }}>{span.name}</span>
        <span style={{ color: token.textSecondary, fontFamily: token.mono }}>
          {fmtDur(span.durationMs)}
        </span>
      </motion.div>
    </div>
  );
}
