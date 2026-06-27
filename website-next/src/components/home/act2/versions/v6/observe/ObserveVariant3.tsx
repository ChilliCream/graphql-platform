"use client";

import { motion } from "motion/react";
import type { CSSProperties } from "react";

/**
 * v6 "Production view" hook, variant 3: an impact leaderboard.
 *
 * Bespoke, one-off illustration (no shared v6 theme): a numbered ranking of the
 * EShops storefront operations, ordered by production impact rather than raw call
 * count. The #1 `checkout` row floats above the field as a highlighted leader card
 * with a faint coral inset border and a coral status dot that pulses, so the eye
 * lands on the operation that hurts most first. The chasing field steps the status
 * down through one amber (updateCart) to two calm greens, and each row carries its
 * p95 latency in mono on the right so the cost of fixing checkout reads as time.
 *
 * The lone looping accent is the coral pulse ring on the #1 dot; ranks, names, and
 * figures are static, so the resting and first frame are fully legible.
 *
 * cc-* dark palette only, thin 1px strokes, generous negative space.
 */

interface ObserveVariant3Props {
  readonly className?: string;
}

const MONO =
  'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace';
const HEADING = '"Josefin Sans", Futura, sans-serif';

/** Locked v6 cc-* palette for this cell: dark surfaces, neutral ink, status hues. */
const C = {
  surface: "#0c1322",
  cardBorder: "rgba(245, 241, 234, 0.12)",
  grid: "rgba(245, 241, 234, 0.08)",
  heading: "#f5f0ea",
  ink: "#a1a3af",
  inkDim: "rgba(245, 241, 234, 0.62)",
  navLabel: "#62748e",
  coral: "#f0786a",
  amber: "#fbbf24",
  healthy: "#34d399",
} as const;

type Status = "coral" | "amber" | "healthy";

const STATUS_COLOR: Record<Status, string> = {
  coral: C.coral,
  amber: C.amber,
  healthy: C.healthy,
};

interface RankRow {
  readonly rank: number;
  readonly operation: string;
  readonly p95: string;
  readonly status: Status;
}

// Locked EShops sample: checkout ranks #1 by impact (the only failing operation),
// updateCart is degraded, the rest are healthy. Impact descends top to bottom.
const ROWS: readonly RankRow[] = [
  { rank: 1, operation: "checkout", p95: "1.84s", status: "coral" },
  { rank: 2, operation: "updateCart", p95: "612ms", status: "amber" },
  { rank: 3, operation: "productPage", p95: "142ms", status: "healthy" },
  { rank: 4, operation: "searchCatalog", p95: "90ms", status: "healthy" },
];

const ROW_GRID: CSSProperties = {
  display: "grid",
  gridTemplateColumns: "30px 16px 1fr auto",
  alignItems: "center",
  gap: 10,
  padding: "8px 11px",
};

export function ObserveVariant3({ className }: ObserveVariant3Props) {
  const lead = ROWS[0];
  const field = ROWS.slice(1);

  return (
    <div
      className={[
        "mx-auto w-full max-w-[330px] select-none",
        className ?? "",
      ].join(" ")}
    >
      <div
        role="img"
        aria-label="Impact leaderboard. Ranked by impact, not call count: #1 checkout, p95 1.84s, failing; #2 updateCart, p95 612ms, degraded; #3 productPage, p95 142ms, healthy; #4 searchCatalog, p95 90ms, healthy."
        className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-4 backdrop-blur-sm"
      >
        {/* Header: panel label + the right-column meaning and scope. */}
        <div
          style={{
            display: "flex",
            alignItems: "baseline",
            justifyContent: "space-between",
            gap: 8,
          }}
        >
          <span
            style={{
              fontFamily: MONO,
              fontSize: 10,
              letterSpacing: "0.15em",
              textTransform: "uppercase",
              color: C.navLabel,
            }}
          >
            Operations by impact
          </span>
          <span
            style={{
              fontFamily: MONO,
              fontSize: 9.5,
              letterSpacing: "0.08em",
              color: C.navLabel,
              whiteSpace: "nowrap",
            }}
          >
            p95 &middot; 24h
          </span>
        </div>

        {/* Leader card: #1 checkout, highlighted with a faint coral inset border. */}
        <div
          style={{
            ...ROW_GRID,
            marginTop: 12,
            borderRadius: 11,
            border: `1px solid rgba(240, 120, 106, 0.34)`,
            background: "rgba(240, 120, 106, 0.07)",
            boxShadow: "inset 0 0 14px rgba(240, 120, 106, 0.06)",
          }}
        >
          <RankNum rank={lead.rank} lead />
          <StatusDot status={lead.status} pulse />
          <OperationName name={lead.operation} lead />
          <P95 value={lead.p95} lead />
        </div>

        {/* The chasing field, stepping down through amber to calm green. */}
        <div
          style={{
            marginTop: 8,
            background: C.surface,
            border: `1px solid ${C.cardBorder}`,
            borderRadius: 11,
            overflow: "hidden",
          }}
        >
          {field.map((row, i) => (
            <div
              key={row.operation}
              style={{
                ...ROW_GRID,
                borderTop: i === 0 ? "none" : `1px solid ${C.grid}`,
              }}
            >
              <RankNum rank={row.rank} lead={false} />
              <StatusDot status={row.status} pulse={false} />
              <OperationName name={row.operation} lead={false} />
              <P95 value={row.p95} lead={false} />
            </div>
          ))}
        </div>

        {/* Promise: ranking is impact, not raw traffic. */}
        <p
          style={{
            marginTop: 12,
            fontFamily: MONO,
            fontSize: 9.5,
            lineHeight: 1.5,
            color: C.inkDim,
          }}
        >
          Ranked by impact, not raw call count.
        </p>
      </div>
    </div>
  );
}

/** Big rank numeral in the heading voice, with a small mono rank marker. */
function RankNum({
  rank,
  lead,
}: {
  readonly rank: number;
  readonly lead: boolean;
}) {
  return (
    <span
      style={{
        display: "inline-flex",
        alignItems: "baseline",
        gap: 1,
        lineHeight: 1,
      }}
    >
      <span
        style={{
          fontFamily: MONO,
          fontSize: 9,
          color: lead ? C.coral : C.navLabel,
        }}
      >
        #
      </span>
      <span
        style={{
          fontFamily: HEADING,
          fontSize: 19,
          fontWeight: 600,
          color: lead ? C.heading : C.inkDim,
          fontVariantNumeric: "tabular-nums",
        }}
      >
        {rank}
      </span>
    </span>
  );
}

/** Status dot reading the operation's dominant health; the leader dot pulses. */
function StatusDot({
  status,
  pulse,
}: {
  readonly status: Status;
  readonly pulse: boolean;
}) {
  const color = STATUS_COLOR[status];
  return (
    <svg
      width={16}
      height={16}
      viewBox="0 0 16 16"
      aria-hidden="true"
      style={{ display: "block", overflow: "visible" }}
    >
      {pulse ? (
        <motion.circle
          cx={8}
          cy={8}
          fill="none"
          stroke={color}
          strokeWidth={1}
          vectorEffect="non-scaling-stroke"
          initial={{ r: 5, opacity: 0.5 }}
          animate={{ r: [5, 9, 5], opacity: [0.5, 0, 0.5] }}
          transition={{ duration: 2.6, repeat: Infinity, ease: "easeInOut" }}
        />
      ) : null}
      <circle
        cx={8}
        cy={8}
        r={5.5}
        fill={`${color}22`}
        stroke={color}
        strokeWidth={1}
        vectorEffect="non-scaling-stroke"
      />
      <circle cx={8} cy={8} r={2.4} fill={color} />
    </svg>
  );
}

/** Operation name in mono; the leader reads strongest. */
function OperationName({
  name,
  lead,
}: {
  readonly name: string;
  readonly lead: boolean;
}) {
  return (
    <span
      style={{
        fontFamily: MONO,
        fontSize: 12,
        color: lead ? C.heading : C.ink,
        fontWeight: lead ? 600 : 400,
        overflow: "hidden",
        textOverflow: "ellipsis",
        whiteSpace: "nowrap",
      }}
    >
      {name}
    </span>
  );
}

/** p95 latency figure in mono, right-aligned; the leader figure is highlighted. */
function P95({
  value,
  lead,
}: {
  readonly value: string;
  readonly lead: boolean;
}) {
  return (
    <span
      style={{
        fontFamily: MONO,
        fontSize: 11.5,
        color: lead ? C.heading : C.inkDim,
        fontVariantNumeric: "tabular-nums",
        textAlign: "right",
      }}
    >
      {value}
    </span>
  );
}
