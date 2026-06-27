"use client";

import { motion } from "motion/react";
import type { CSSProperties } from "react";

/**
 * v6 "Release safety" hook, variant 2: a blocked pull-request merge box.
 *
 * Bespoke, one-off illustration (no shared v6 theme): a cropped GitHub-style PR
 * checks panel for "Remove Product.rating". Three passing checks (green) sit above
 * the one that holds the line: a coral `schema-registry / breaking-change` row that
 * found the removed field breaks published clients. Below the list, the merge box
 * reads "Merging is blocked" and the `Merge pull request` button is dimmed and
 * un-clickable, sitting behind a small coral cross. The merge is held, not the
 * release: production is never reached.
 *
 * The sole looping accent is a soft coral halo pulsing around the cross over the
 * Merge button, drawing the eye to the block; every row, label, and the button
 * text are fully legible at rest, with no layout shift.
 *
 * cc-* dark palette only; status colors encode real status (green = checks that
 * passed, coral = the breaking-change failure and the blocked merge). Thin 1px
 * strokes, mono labels, generous negative space. Inline SVG id prefix
 * "v6-guardrails-2-".
 */

interface GuardrailsVariant2Props {
  readonly className?: string;
}

const ID = "v6-guardrails-2-";

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
  navLabel: "#62748e",
  coral: "#f0786a",
  healthy: "#34d399",
} as const;

interface PassingCheck {
  readonly name: string;
  readonly duration: string;
}

// Passing CI checks above the one that fails: build, unit tests, and the registry's
// client-usage check all green; only the breaking-change check holds the merge.
const PASSING: readonly PassingCheck[] = [
  { name: "ci / build", duration: "22s" },
  { name: "ci / unit-tests", duration: "1m 08s" },
  { name: "client-registry / usage", duration: "14s" },
];

const ROW: CSSProperties = {
  display: "grid",
  gridTemplateColumns: "16px 1fr auto",
  alignItems: "center",
  gap: 10,
  padding: "9px 12px",
};

export function GuardrailsVariant2({ className }: GuardrailsVariant2Props) {
  return (
    <div
      className={[
        "mx-auto w-full max-w-[330px] select-none",
        className ?? "",
      ].join(" ")}
    >
      <div
        role="img"
        aria-label="Pull request 'Remove Product.rating', 1 failing check. ci / build passed, ci / unit-tests passed, client-registry / usage passed, schema-registry / breaking-change failed because it breaks 3 published clients. Merging is blocked: the breaking-change check must pass before this pull request can be merged. The Merge pull request button is dimmed and un-clickable."
        className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-4 backdrop-blur-sm"
      >
        {/* Header: PR title + number, with a coral failing-checks pill. */}
        <div
          style={{
            display: "flex",
            alignItems: "baseline",
            justifyContent: "space-between",
            gap: 10,
          }}
        >
          <div
            style={{
              display: "flex",
              alignItems: "baseline",
              gap: 7,
              minWidth: 0,
            }}
          >
            <span
              style={{
                fontSize: 13,
                fontWeight: 600,
                color: C.heading,
                whiteSpace: "nowrap",
                overflow: "hidden",
                textOverflow: "ellipsis",
              }}
            >
              Remove Product.rating
            </span>
            <span style={{ fontFamily: MONO, fontSize: 11, color: C.navLabel }}>
              #482
            </span>
          </div>
          <span
            style={{
              fontFamily: MONO,
              fontSize: 9.5,
              fontWeight: 600,
              letterSpacing: "0.06em",
              textTransform: "uppercase",
              color: C.coral,
              background: "rgba(240, 120, 106, 0.10)",
              border: "1px solid rgba(240, 120, 106, 0.34)",
              borderRadius: 999,
              padding: "2px 8px",
              whiteSpace: "nowrap",
            }}
          >
            1 failing
          </span>
        </div>

        {/* Section label for the checks list. */}
        <p
          style={{
            margin: "13px 0 0",
            fontFamily: MONO,
            fontSize: 9.5,
            letterSpacing: "0.15em",
            textTransform: "uppercase",
            color: C.navLabel,
          }}
        >
          Checks
        </p>

        {/* The checks list: passing rows above the coral breaking-change failure. */}
        <div
          style={{
            marginTop: 8,
            background: C.surface,
            border: `1px solid ${C.cardBorder}`,
            borderRadius: 11,
            overflow: "hidden",
          }}
        >
          {PASSING.map((check, i) => (
            <div
              key={check.name}
              style={{
                ...ROW,
                borderTop: i === 0 ? "none" : `1px solid ${C.grid}`,
              }}
            >
              <CheckGlyph />
              <span
                style={{
                  fontFamily: MONO,
                  fontSize: 11,
                  color: C.ink,
                  whiteSpace: "nowrap",
                  overflow: "hidden",
                  textOverflow: "ellipsis",
                }}
              >
                {check.name}
              </span>
              <span
                style={{
                  fontFamily: MONO,
                  fontSize: 10,
                  color: C.navLabel,
                  fontVariantNumeric: "tabular-nums",
                }}
              >
                {check.duration}
              </span>
            </div>
          ))}

          {/* The one check that holds the merge. */}
          <div
            style={{
              ...ROW,
              alignItems: "flex-start",
              borderTop: `1px solid ${C.grid}`,
              background: "rgba(240, 120, 106, 0.06)",
            }}
          >
            <span style={{ marginTop: 1, display: "block" }}>
              <CrossGlyph size={16} />
            </span>
            <div style={{ minWidth: 0 }}>
              <span
                style={{
                  fontFamily: MONO,
                  fontSize: 11,
                  fontWeight: 600,
                  color: C.coral,
                  whiteSpace: "nowrap",
                  overflow: "hidden",
                  textOverflow: "ellipsis",
                  display: "block",
                }}
              >
                schema-registry / breaking-change
              </span>
              <span
                style={{
                  display: "inline-flex",
                  alignItems: "center",
                  gap: 7,
                  marginTop: 4,
                }}
              >
                <span
                  style={{
                    fontFamily: MONO,
                    fontSize: 9,
                    fontWeight: 600,
                    letterSpacing: "0.08em",
                    color: C.coral,
                    border: "1px solid rgba(240, 120, 106, 0.45)",
                    borderRadius: 4,
                    padding: "1px 5px",
                  }}
                >
                  FAIL
                </span>
                <span
                  style={{
                    fontFamily: MONO,
                    fontSize: 10,
                    color: C.inkDim,
                    whiteSpace: "nowrap",
                  }}
                >
                  breaks 3 published clients
                </span>
              </span>
            </div>
            <span
              style={{
                fontFamily: MONO,
                fontSize: 10,
                color: C.navLabel,
                marginTop: 1,
              }}
            >
              Required
            </span>
          </div>
        </div>

        {/* Merge box: blocked status + the dimmed, un-clickable Merge button. */}
        <div
          style={{
            marginTop: 12,
            paddingTop: 12,
            borderTop: `1px solid ${C.cardBorder}`,
          }}
        >
          <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
            <CrossGlyph size={16} />
            <span style={{ fontSize: 12.5, fontWeight: 600, color: C.coral }}>
              Merging is blocked
            </span>
          </div>
          <p
            style={{
              margin: "5px 0 0 24px",
              fontSize: 11,
              lineHeight: 1.45,
              color: C.inkDim,
            }}
          >
            The breaking-change check must pass before this pull request can be
            merged.
          </p>

          <div style={{ position: "relative", marginTop: 11 }}>
            <div
              aria-hidden="true"
              style={{
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
                height: 34,
                borderRadius: 8,
                border: `1px solid ${C.cardBorder}`,
                background: "rgba(245, 241, 234, 0.04)",
                color: C.inkDim,
                fontSize: 12.5,
                fontWeight: 600,
                opacity: 0.5,
                cursor: "not-allowed",
              }}
            >
              Merge pull request
            </div>

            {/* The small coral cross the dimmed button sits behind. */}
            <span
              style={{
                position: "absolute",
                left: 10,
                top: "50%",
                transform: "translateY(-50%)",
                display: "block",
              }}
            >
              <MergeBlockBadge />
            </span>
          </div>
        </div>
      </div>
    </div>
  );
}

/** Green pass glyph: a filled circle with a check, cloned by eye from a CI check. */
function CheckGlyph() {
  return (
    <svg
      width={16}
      height={16}
      viewBox="0 0 16 16"
      aria-hidden="true"
      style={{ display: "block" }}
    >
      <circle cx="8" cy="8" r="7" fill={C.healthy} />
      <path
        d="M4.7 8.2 6.9 10.5 11.3 5.7"
        fill="none"
        stroke={C.surface}
        strokeWidth="1.7"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}

/** Coral failure glyph: a filled circle-x, cloned by eye from a breaking change. */
function CrossGlyph({ size }: { readonly size: number }) {
  return (
    <svg
      width={size}
      height={size}
      viewBox="0 0 16 16"
      aria-hidden="true"
      style={{ display: "block" }}
    >
      <circle cx="8" cy="8" r="7" fill={C.coral} />
      <path
        d="M5.4 5.4 10.6 10.6M10.6 5.4 5.4 10.6"
        stroke={C.surface}
        strokeWidth="1.7"
        strokeLinecap="round"
      />
    </svg>
  );
}

/** The coral cross over the Merge button, with a soft pulsing halo (sole motion). */
function MergeBlockBadge() {
  return (
    <svg
      width={30}
      height={30}
      viewBox="0 0 30 30"
      aria-hidden="true"
      style={{ display: "block", overflow: "visible" }}
    >
      <defs>
        <radialGradient id={`${ID}cross-glow`} cx="0.5" cy="0.5" r="0.5">
          <stop offset="0" stopColor={C.coral} stopOpacity="0.45" />
          <stop offset="1" stopColor={C.coral} stopOpacity="0" />
        </radialGradient>
      </defs>

      <circle cx="15" cy="15" r="14" fill={`url(#${ID}cross-glow)`} />

      <motion.circle
        cx="15"
        cy="15"
        fill="none"
        stroke={C.coral}
        strokeWidth="1"
        vectorEffect="non-scaling-stroke"
        initial={{ r: 8, opacity: 0.5 }}
        animate={{ r: [8, 13, 8], opacity: [0.5, 0, 0.5] }}
        transition={{ duration: 2.6, repeat: Infinity, ease: "easeInOut" }}
      />

      <circle cx="15" cy="15" r="8" fill={C.coral} />
      <path
        d="M11.7 11.7 18.3 18.3M18.3 11.7 11.7 18.3"
        stroke={C.surface}
        strokeWidth="1.9"
        strokeLinecap="round"
      />
    </svg>
  );
}
