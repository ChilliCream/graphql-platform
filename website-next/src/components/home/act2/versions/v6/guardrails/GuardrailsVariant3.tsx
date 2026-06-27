"use client";

import { motion } from "motion/react";
import type { CSSProperties } from "react";

/**
 * v6 "Release safety" hook, variant 3: the client-impact matrix.
 *
 * Bespoke, one-off illustration (no shared v6 theme): before a schema change is
 * published, the registry reconciles it against the operations each registered
 * client actually sends. This widget names every published client (`web`,
 * `mobile`, `partner-api`) as a row in an impact matrix. Each row carries a tiny
 * segmented operations bar (one segment per published operation, green when the
 * op stays clear, coral when the change would break it) and a verdict pill that
 * reads `OK`, the `3/5` fraction `at risk`, or `queued`. The one amber row stands
 * out: `mobile` is tinted, accented with a left amber rule, and its status dot
 * carries the lone looping pulse, so the eye lands on the client that breaks.
 *
 * Delivers "Know exactly which clients break.": you read which named clients are
 * clear and which would break without parsing a diff.
 *
 * cc-* dark palette only; status color is rationed (green clear, coral break,
 * amber at-risk, grey queued). The single accent gradient and the pulse are the
 * only flourishes; the resting and first frame are fully legible. Every inline
 * SVG id is prefixed "v6-guardrails-3-".
 */

interface GuardrailsVariant3Props {
  readonly className?: string;
}

const MONO =
  'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace';

/** Locked v6 cc-* palette for this cell: dark surfaces, neutral ink, status hues. */
const C = {
  surface: "#0c1322",
  cardBorder: "rgba(245, 241, 234, 0.12)",
  grid: "rgba(245, 241, 234, 0.08)",
  heading: "#f5f0ea",
  ink: "#a1a3af",
  inkDim: "rgba(245, 241, 234, 0.62)",
  inkFaint: "rgba(245, 241, 234, 0.16)",
  navLabel: "#62748e",
  accent: "#5eead4",
  coral: "#f0786a",
  amber: "#fbbf24",
  healthy: "#34d399",
} as const;

type Status = "ok" | "at-risk" | "queued";

const STATUS_COLOR: Record<Status, string> = {
  ok: C.healthy,
  "at-risk": C.amber,
  queued: C.navLabel,
};

interface ClientRow {
  readonly name: string;
  readonly status: Status;
  /** Published operations that stay clear under the proposed change. */
  readonly clear: number;
  /** Total published operations registered for this client. */
  readonly total: number;
}

// Locked verdict: web is fully clear, mobile is the at-risk standout (2 of its 5
// ops would break), partner-api has not been reconciled yet.
const CLIENTS: readonly ClientRow[] = [
  { name: "web", status: "ok", clear: 5, total: 5 },
  { name: "mobile", status: "at-risk", clear: 3, total: 5 },
  { name: "partner-api", status: "queued", clear: 0, total: 4 },
];

const ROW_GRID: CSSProperties = {
  display: "grid",
  gridTemplateColumns: "92px 1fr auto",
  alignItems: "center",
  gap: 12,
  padding: "10px 12px",
};

export function GuardrailsVariant3({ className }: GuardrailsVariant3Props) {
  return (
    <div
      className={["mx-auto w-full max-w-[330px] select-none", className ?? ""]
        .join(" ")
        .trim()}
    >
      <div
        role="img"
        aria-label="Client-impact matrix for the checkout-v3 schema change. web: 5 of 5 operations clear, OK. mobile: 3 of 5 operations clear, 2 would break, at risk. partner-api: 4 operations queued, not yet checked."
        className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-4 backdrop-blur-sm"
      >
        {/* Header: what this matrix reconciles and against which change. */}
        <div
          style={{
            display: "flex",
            alignItems: "center",
            gap: 8,
          }}
        >
          <svg
            viewBox="0 0 16 16"
            width={13}
            height={13}
            aria-hidden="true"
            style={{ flex: "0 0 auto", display: "block" }}
          >
            <defs>
              <linearGradient
                id="v6-guardrails-3-mark"
                x1="2"
                y1="2"
                x2="14"
                y2="14"
                gradientUnits="userSpaceOnUse"
              >
                <stop offset="0" stopColor={C.accent} />
                <stop offset="1" stopColor="#7c92c6" />
              </linearGradient>
            </defs>
            <path
              fill="url(#v6-guardrails-3-mark)"
              d="M8 1.2 13.9 4.6v6.8L8 14.8 2.1 11.4V4.6L8 1.2Zm0 1.5L3.4 5.3v5.4L8 13.3l4.6-2.6V5.3L8 2.7Z"
            />
            <circle cx="8" cy="8" r="1.7" fill="url(#v6-guardrails-3-mark)" />
          </svg>
          <span
            style={{
              fontFamily: MONO,
              fontSize: 10,
              letterSpacing: "0.15em",
              textTransform: "uppercase",
              color: C.navLabel,
            }}
          >
            Client impact
          </span>
          <span style={{ flex: 1 }} />
          <span
            style={{
              fontFamily: MONO,
              fontSize: 9.5,
              letterSpacing: "0.04em",
              color: C.navLabel,
              whiteSpace: "nowrap",
            }}
          >
            checkout-v3
          </span>
        </div>

        {/* Column key: reads the matrix as client / operations / verdict. */}
        <div
          style={{
            ...ROW_GRID,
            paddingTop: 12,
            paddingBottom: 4,
          }}
        >
          <ColLabel>client</ColLabel>
          <ColLabel>operations</ColLabel>
          <ColLabel align="right">status</ColLabel>
        </div>

        {/* The named published clients and how each fares against the change. */}
        <div
          style={{
            background: C.surface,
            border: `1px solid ${C.cardBorder}`,
            borderRadius: 11,
            overflow: "hidden",
          }}
        >
          {CLIENTS.map((row, i) => {
            const standout = row.status === "at-risk";
            return (
              <div
                key={row.name}
                style={{
                  ...ROW_GRID,
                  borderTop: i === 0 ? "none" : `1px solid ${C.grid}`,
                  background: standout ? "rgba(251, 191, 36, 0.06)" : "none",
                  boxShadow: standout ? `inset 2px 0 0 ${C.amber}` : "none",
                }}
              >
                <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
                  <StatusDot status={row.status} pulse={standout} />
                  <span
                    style={{
                      fontFamily: MONO,
                      fontSize: 11.5,
                      color: standout ? C.heading : C.ink,
                      fontWeight: standout ? 600 : 400,
                      whiteSpace: "nowrap",
                      overflow: "hidden",
                      textOverflow: "ellipsis",
                    }}
                  >
                    {row.name}
                  </span>
                </div>

                <OperationsBar row={row} />

                <Verdict row={row} />
              </div>
            );
          })}
        </div>

        {/* Punchline: name the client that breaks and how much. */}
        <p
          style={{
            marginTop: 12,
            fontFamily: MONO,
            fontSize: 9.5,
            lineHeight: 1.5,
            color: C.inkDim,
          }}
        >
          <span style={{ color: C.amber, fontWeight: 600 }}>mobile</span> would
          break <span style={{ color: C.coral, fontWeight: 600 }}>2/5 ops</span>{" "}
          before release.
        </p>
      </div>
    </div>
  );
}

/** Thin uppercase mono column key. */
function ColLabel({
  children,
  align = "left",
}: {
  readonly children: string;
  readonly align?: "left" | "right";
}) {
  return (
    <span
      style={{
        fontFamily: MONO,
        fontSize: 8.5,
        letterSpacing: "0.12em",
        textTransform: "uppercase",
        color: C.navLabel,
        textAlign: align,
      }}
    >
      {children}
    </span>
  );
}

/**
 * Per-operation segmented bar: one segment per published operation. Green where
 * the op stays clear, coral where the change would break it, grey outline when
 * the client has not been reconciled yet.
 */
function OperationsBar({ row }: { readonly row: ClientRow }) {
  const segments = Array.from({ length: row.total }, (_, i) => {
    if (row.status === "queued") {
      return "queued" as const;
    }
    return i < row.clear ? ("clear" as const) : ("break" as const);
  });

  return (
    <div style={{ display: "flex", alignItems: "center", gap: 3 }}>
      {segments.map((seg, i) => (
        <span
          key={`${row.name}-${i}`}
          style={{
            height: 7,
            flex: 1,
            borderRadius: 2,
            background:
              seg === "clear"
                ? C.healthy
                : seg === "break"
                  ? C.coral
                  : "transparent",
            border: seg === "queued" ? `1px solid ${C.inkFaint}` : "none",
            opacity: seg === "clear" ? 0.8 : 1,
          }}
        />
      ))}
    </div>
  );
}

/** Verdict pill: OK, the at-risk fraction, or queued, tinted by status. */
function Verdict({ row }: { readonly row: ClientRow }) {
  const color = STATUS_COLOR[row.status];
  const text =
    row.status === "ok"
      ? "OK"
      : row.status === "at-risk"
        ? `${row.clear}/${row.total}`
        : "queued";

  return (
    <span
      style={{
        justifySelf: "end",
        fontFamily: MONO,
        fontSize: 9.5,
        fontWeight: 600,
        letterSpacing: "0.02em",
        fontVariantNumeric: "tabular-nums",
        color,
        background: `${color}14`,
        border: `1px solid ${color}59`,
        borderRadius: 999,
        padding: "2px 8px",
        whiteSpace: "nowrap",
      }}
    >
      {text}
    </span>
  );
}

/** Status dot reading the client verdict; the at-risk standout dot pulses. */
function StatusDot({
  status,
  pulse,
}: {
  readonly status: Status;
  readonly pulse: boolean;
}) {
  const color = STATUS_COLOR[status];

  if (status === "queued") {
    return (
      <svg
        width={14}
        height={14}
        viewBox="0 0 16 16"
        aria-hidden="true"
        style={{ display: "block", flex: "0 0 auto" }}
      >
        <circle
          cx={8}
          cy={8}
          r={5.5}
          fill="none"
          stroke={color}
          strokeWidth={1.2}
          strokeDasharray="2.2 2.2"
        />
      </svg>
    );
  }

  return (
    <svg
      width={14}
      height={14}
      viewBox="0 0 16 16"
      aria-hidden="true"
      style={{ display: "block", flex: "0 0 auto", overflow: "visible" }}
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
