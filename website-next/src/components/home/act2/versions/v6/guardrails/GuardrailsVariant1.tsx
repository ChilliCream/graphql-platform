"use client";

import { motion } from "motion/react";

interface GuardrailsVariant1Props {
  readonly className?: string;
}

/**
 * v6 "Release safety" hook, variant 1: see the breaking line before merge.
 *
 * Bespoke, one-off illustration (no shared v6 theme): a cropped `schema.graphql`
 * diff window as the registry renders a proposed change for review. The removed
 * `- total: Float!` row glows coral and carries a loud `BREAKING` stamp; the
 * added `+ totalAmount: Money!` row carries a quiet green `SAFE` stamp; every
 * other line stays neutral grey. The colored stamps tell the whole story at a
 * glance, so a risky removal is obvious in review instead of in production.
 *
 * Sole looping accent: a soft coral wash breathes across the breaking row to
 * pull the eye to it. The diff, both stamps, and the +/- counts are fully
 * legible at rest, with no layout shift.
 *
 * Dark cc-* palette only; status colors encode real status (coral = a breaking
 * removal, green = a safe additive change). Inline SVG id prefix
 * "v6-guardrails-1-".
 */

const MONO =
  'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace';

/** Locked v6 cc-* palette for this cell: dark surfaces, neutral ink, status. */
const C = {
  page: "#0b0f1a",
  surface: "#0c1322",
  cardBorder: "rgba(245, 241, 234, 0.12)",
  heading: "#f5f0ea",
  ink: "#a1a3af",
  inkDim: "rgba(245, 241, 234, 0.62)",
  navLabel: "#62748e",
  green: "#34d399",
  coral: "#f0786a",
} as const;

type Marker = "add" | "del" | "ctx";

interface Token {
  readonly text: string;
  readonly color: string;
}

interface DiffLine {
  readonly marker: Marker;
  /** Tokenized GraphQL runs for the line body. */
  readonly tokens: readonly Token[];
  /** Severity stamp, when this line is a reviewed change. */
  readonly chip?: "SAFE" | "BREAKING";
}

const LINES: readonly DiffLine[] = [
  {
    marker: "ctx",
    tokens: [
      { text: "type ", color: C.ink },
      { text: "Invoice", color: C.heading },
      { text: " {", color: C.ink },
    ],
  },
  {
    marker: "ctx",
    tokens: [{ text: "  id: ID!", color: C.ink }],
  },
  {
    marker: "del",
    tokens: [{ text: "  total: Float!", color: C.coral }],
    chip: "BREAKING",
  },
  {
    marker: "add",
    tokens: [{ text: "  totalAmount: Money!", color: C.green }],
    chip: "SAFE",
  },
  {
    marker: "ctx",
    tokens: [{ text: "}", color: C.ink }],
  },
];

const MARKER_GLYPH: Record<Marker, string> = { add: "+", del: "-", ctx: " " };

const MARKER_COLOR: Record<Marker, string> = {
  add: C.green,
  del: C.coral,
  ctx: C.navLabel,
};

/** Tinted row background for added / removed lines, transparent for context. */
const ROW_BG: Record<Marker, string> = {
  add: "rgba(52, 211, 153, 0.08)",
  del: "rgba(240, 120, 106, 0.10)",
  ctx: "transparent",
};

function SeverityChip({ kind }: { readonly kind: "SAFE" | "BREAKING" }) {
  const safe = kind === "SAFE";
  const color = safe ? C.green : C.coral;
  return (
    <span
      style={{
        flex: "0 0 auto",
        display: "inline-flex",
        alignItems: "center",
        gap: 4,
        padding: "1px 7px",
        borderRadius: 999,
        fontFamily: MONO,
        fontSize: 9,
        fontWeight: 600,
        letterSpacing: "0.08em",
        color,
        background: safe
          ? "rgba(52, 211, 153, 0.10)"
          : "rgba(240, 120, 106, 0.15)",
        border: `1px solid ${color}${safe ? "45" : "66"}`,
      }}
    >
      <span
        aria-hidden="true"
        style={{ width: 5, height: 5, borderRadius: 999, background: color }}
      />
      {kind}
    </span>
  );
}

function DiffRow({ line }: { readonly line: DiffLine }) {
  const changed = line.marker !== "ctx";
  return (
    <div
      style={{
        position: "relative",
        display: "flex",
        alignItems: "center",
        gap: 6,
        paddingRight: 10,
        minHeight: 22,
        lineHeight: "22px",
        background: ROW_BG[line.marker],
      }}
    >
      {/* Sole looping accent: a soft coral wash breathing across the breaking row. */}
      {line.marker === "del" ? (
        <motion.span
          aria-hidden="true"
          style={{
            position: "absolute",
            inset: 0,
            zIndex: 0,
            pointerEvents: "none",
            background: `linear-gradient(90deg, ${C.coral}24 0%, ${C.coral}00 62%)`,
          }}
          initial={{ opacity: 0.45 }}
          animate={{ opacity: [0.45, 0.95, 0.45] }}
          transition={{ duration: 2.4, repeat: Infinity, ease: "easeInOut" }}
        />
      ) : null}

      {/* Left gutter accent bar, colored only on changed rows. */}
      <span
        aria-hidden="true"
        style={{
          flex: "0 0 auto",
          alignSelf: "stretch",
          width: 3,
          background: changed ? MARKER_COLOR[line.marker] : "transparent",
        }}
      />

      {/* +/- diff marker. */}
      <span
        aria-hidden="true"
        style={{
          position: "relative",
          zIndex: 1,
          fontFamily: MONO,
          flex: "0 0 auto",
          width: 10,
          textAlign: "center",
          color: MARKER_COLOR[line.marker],
          fontSize: 11,
          fontWeight: changed ? 700 : 400,
        }}
      >
        {MARKER_GLYPH[line.marker]}
      </span>

      <code
        style={{
          position: "relative",
          zIndex: 1,
          fontFamily: MONO,
          flex: 1,
          minWidth: 0,
          fontSize: 11.5,
          whiteSpace: "pre",
          overflow: "hidden",
          textOverflow: "ellipsis",
        }}
      >
        {line.tokens.map((t, i) => (
          <span key={i} style={{ color: t.color }}>
            {t.text}
          </span>
        ))}
      </code>

      {line.chip ? <SeverityChip kind={line.chip} /> : null}
    </div>
  );
}

export function GuardrailsVariant1({ className }: GuardrailsVariant1Props) {
  const added = LINES.filter((l) => l.marker === "add").length;
  const removed = LINES.filter((l) => l.marker === "del").length;

  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
    >
      <div
        style={{
          overflow: "hidden",
          borderRadius: 14,
          border: `1px solid ${C.cardBorder}`,
          background: C.surface,
          boxShadow: "0 1px 3px rgba(2, 6, 16, 0.6)",
          fontFamily: MONO,
        }}
      >
        {/* Title bar: the file under review, with +/- change counts. */}
        <div
          style={{
            display: "flex",
            alignItems: "center",
            gap: 8,
            height: 34,
            padding: "0 12px",
            background: C.page,
            borderBottom: `1px solid ${C.cardBorder}`,
          }}
        >
          <svg
            viewBox="0 0 16 16"
            width={13}
            height={13}
            aria-hidden="true"
            style={{ flex: "0 0 auto" }}
          >
            <path
              fill={C.navLabel}
              d="M8 1.2 13.9 4.6v6.8L8 14.8 2.1 11.4V4.6L8 1.2Zm0 1.5L3.4 5.3v5.4L8 13.3l4.6-2.6V5.3L8 2.7Z"
            />
            <circle cx="8" cy="8" r="1.7" fill={C.navLabel} />
          </svg>
          <span
            style={{
              fontFamily: MONO,
              fontSize: 11.5,
              fontWeight: 600,
              color: C.heading,
              whiteSpace: "nowrap",
            }}
          >
            schema.graphql
          </span>
          <span style={{ flex: 1 }} />
          <span style={{ fontFamily: MONO, fontSize: 10, color: C.green }}>
            +{added}
          </span>
          <span style={{ fontFamily: MONO, fontSize: 10, color: C.coral }}>
            -{removed}
          </span>
        </div>

        {/* Diff body: one removed field flagged breaking, its replacement safe. */}
        <div style={{ padding: "8px 0" }}>
          {LINES.map((line, i) => (
            <DiffRow key={i} line={line} />
          ))}
        </div>

        {/* Footer takeaway: the risky removal surfaced in review, not later. */}
        <div
          style={{
            display: "flex",
            alignItems: "center",
            gap: 8,
            padding: "9px 12px",
            borderTop: `1px solid ${C.cardBorder}`,
            background: C.page,
          }}
        >
          <span
            aria-hidden="true"
            style={{
              flex: "0 0 auto",
              width: 6,
              height: 6,
              borderRadius: 999,
              background: C.coral,
            }}
          />
          <span
            style={{
              fontFamily: MONO,
              fontSize: 10,
              letterSpacing: "0.02em",
              color: C.inkDim,
            }}
          >
            Breaking removal caught in review, before merge.
          </span>
        </div>
      </div>
    </div>
  );
}
