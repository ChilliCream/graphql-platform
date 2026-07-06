/**
 * InsightsTable — operations insights grid with staggered rows.
 *
 * A semantic HTML `<table>` of the most-impactful operations. Each row carries a name +
 * span-kind badge, the usual numeric metrics (avg latency, throughput, error rate), an
 * inline `Sparkline` of its recent latency, and a mini impact bar. Rows fade + slide in
 * one after another, staggered by index across the normalized clock `t` (0→1) from
 * `useChartClock`, so the table reveals top-down standalone in its story and also slots
 * into the Monitoring Overview's shared cycle.
 *
 * The master `progress` is forwarded down to each row's `Sparkline` (offset to the row's
 * own slice of the cycle) so the spark draws in lock-step with the row's entrance — both
 * standalone and when composed under the shared clock.
 *
 * This is a DOM primitive, not an SVG chart: numeric cells use `token.mono`, the header
 * is small uppercase `token.textSecondary`, and everything reads in both themes.
 */
import type { CSSProperties } from "react";
import { motion, useTransform, type MotionValue } from "motion/react";
import { Sparkline } from "./Sparkline";
import { ease } from "../lib/motion";
import { token, IMPACT_STOPS } from "../lib/tokens";
import { colorAt, compact, ms } from "../lib/scale";
import { useChartClock } from "../lib/useInViewLoop";
import type { InsightRow, SpanKind } from "../lib/data";

export interface InsightsTableProps {
  rows: InsightRow[];
  /** error rate above this fraction (0..1) tints the cell `token.error` */
  errorThreshold?: number;
  /** shared master clock (overview); omit for a self-contained standalone loop */
  progress?: MotionValue<number>;
  playWindow?: [number, number];
  /** fraction of the window each successive row is offset by */
  rowStagger?: number;
  durationMs?: number;
  className?: string;
  style?: CSSProperties;
  ariaLabel?: string;
}

/** Short, theme-independent label glyphs for the span-kind badge pill. */
const SPAN_LABEL: Record<SpanKind, string> = {
  server: "SRV",
  client: "CLI",
  internal: "INT",
  producer: "PRD",
  consumer: "CON",
};

const HEADER: CSSProperties = {
  textAlign: "left",
  fontSize: 12,
  fontWeight: 500,
  color: token.textSecondary,
  padding: "8px 10px",
  borderBottom: `1px solid ${token.border}`,
};

const CELL: CSSProperties = {
  padding: "7px 10px",
  borderBottom: `1px solid ${token.border}`,
  verticalAlign: "middle",
};

const NUM: CSSProperties = {
  ...CELL,
  fontFamily: token.mono,
  fontVariantNumeric: "tabular-nums",
  textAlign: "right",
  color: token.textStrong,
};

export function InsightsTable({
  rows,
  errorThreshold = 0.03,
  progress,
  playWindow,
  rowStagger = 0.1,
  durationMs,
  className,
  style,
  ariaLabel,
}: InsightsTableProps) {
  const { ref, t } = useChartClock({ progress, playWindow, durationMs });

  // Each row gets a sub-window [r0, r1] of t; the row before it is fully in before the
  // next finishes, so the reveal cascades without leaving big gaps.
  const span = Math.max(0.001, 1 - Math.max(0, rows.length - 1) * rowStagger);
  const label =
    ariaLabel ??
    `Top ${rows.length} operations by impact, with latency and error rate`;

  return (
    <div
      ref={ref}
      className={className}
      // A horizontally scrollable region must be keyboard-focusable and named (WCAG 2.1.1).
      tabIndex={0}
      role="group"
      aria-label={label}
      style={{
        position: "relative",
        width: "100%",
        height: "100%",
        overflowX: "auto",
        ...style,
      }}
    >
      <table
        style={{
          width: "100%",
          minWidth: 520,
          borderCollapse: "collapse",
          fontSize: 12,
          color: token.text,
          tableLayout: "fixed",
        }}
      >
        <colgroup>
          <col style={{ width: "34%" }} />
          <col style={{ width: "13%" }} />
          <col style={{ width: "13%" }} />
          <col style={{ width: "11%" }} />
          <col style={{ width: "17%" }} />
          <col style={{ width: "12%" }} />
        </colgroup>
        <thead>
          <tr>
            <th scope="col" style={HEADER}>
              Subgraph
            </th>
            <th scope="col" style={{ ...HEADER, textAlign: "right" }}>
              Avg latency
            </th>
            <th scope="col" style={{ ...HEADER, textAlign: "right" }}>
              Throughput
            </th>
            <th scope="col" style={{ ...HEADER, textAlign: "right" }}>
              Errors
            </th>
            <th scope="col" style={HEADER}>
              Latency
            </th>
            <th scope="col" style={HEADER}>
              Impact
            </th>
          </tr>
        </thead>
        <tbody>
          {rows.map((row, i) => {
            const r0 = Math.min(i * rowStagger, 0.99);
            const r1 = Math.min(r0 + span, 1);
            return (
              <Row
                key={row.id}
                row={row}
                t={t}
                progress={progress}
                window={[r0, r1]}
                errorThreshold={errorThreshold}
              />
            );
          })}
        </tbody>
      </table>
    </div>
  );
}

function Row({
  row,
  t,
  progress,
  window: [w0, w1],
  errorThreshold,
}: {
  row: InsightRow;
  t: MotionValue<number>;
  progress?: MotionValue<number>;
  window: [number, number];
  errorThreshold: number;
}) {
  // Entrance: fade + slide up across this row's slice of the clock.
  const reveal = useTransform(t, [w0, w1], [0, 1], {
    ease: ease.out,
    clamp: true,
  });
  const opacity = useTransform(reveal, [0, 1], [0, 1]);
  const y = useTransform(reveal, [0, 1], [6, 0]);

  // Impact bar grows from the left over the same slice.
  const barW = useTransform(reveal, [0, 1], ["0%", `${row.impact}%`]);
  const impactColor = colorAt(IMPACT_STOPS, row.impact / 100);

  const isHot = row.errorRate > errorThreshold;

  return (
    <motion.tr style={{ opacity, y }}>
      {/* Operation: name + span-kind badge pill */}
      <td style={CELL}>
        <div
          style={{ display: "flex", alignItems: "center", gap: 8, minWidth: 0 }}
        >
          <span
            aria-label={SPAN_LABEL[row.spanKind]}
            style={{
              flex: "none",
              width: 17,
              height: 17,
              display: "inline-flex",
              alignItems: "center",
              justifyContent: "center",
              fontFamily: token.mono,
              fontSize: 10,
              fontWeight: 700,
              color: token.textSecondary,
              background: token.surface,
              border: `1px solid ${token.border}`,
              borderRadius: 4,
            }}
          >
            Q
          </span>
          <span
            title={row.name}
            style={{
              overflow: "hidden",
              textOverflow: "ellipsis",
              whiteSpace: "nowrap",
              color: token.textStrong,
              fontWeight: 500,
            }}
          >
            {row.name}
          </span>
        </div>
      </td>

      {/* Avg latency */}
      <td style={NUM}>{ms(row.averageLatency)}</td>

      {/* Throughput */}
      <td style={NUM}>
        {compact(row.opm)}
        <span style={{ color: token.textSecondary }}>/m</span>
      </td>

      {/* Error rate — tinted when over threshold */}
      <td style={{ ...NUM, color: isHot ? token.errorText : token.textStrong }}>
        {(row.errorRate * 100).toFixed(1)}%
      </td>

      {/* Inline latency sparkline, drawn in sync with the row reveal */}
      <td style={CELL}>
        <div style={{ height: 22, width: "100%" }}>
          <Sparkline
            values={row.latencySeries}
            stroke={token.cLatency}
            height={22}
            progress={progress}
            playWindow={[w0, w1]}
            ariaLabel={`${row.name} latency trend`}
          />
        </div>
      </td>

      {/* Impact: track + fill colored by the impact gradient */}
      <td style={CELL}>
        <div
          style={{
            position: "relative",
            height: 6,
            width: "100%",
            background: token.skeleton,
            borderRadius: 999,
            overflow: "hidden",
          }}
        >
          <motion.div
            style={{
              position: "absolute",
              inset: 0,
              width: barW,
              background: impactColor,
              borderRadius: 999,
            }}
          />
        </div>
      </td>
    </motion.tr>
  );
}
