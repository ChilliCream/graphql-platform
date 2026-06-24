import type { CSSProperties } from "react";

import { nitro } from "@/src/components/home/act2/variants/nitroTheme";

/**
 * "Release safety" scene, variant 2 — a compact breaking-change callout.
 *
 * A single small card cropped from the Nitro schema registry: one change in a
 * proposed schema (`Product.rating` removed) flagged with a coral BREAKING badge,
 * the change rendered as a one-line removal diff, and a short note that published
 * clients are affected. No changelog grid, no chrome beyond a thin title row, no
 * tabs. It shows exactly one thing: this change would break consumers.
 *
 * STATIC render of the settled final frame: no motion package, no clocks, no
 * in-view hooks. All markup is hand-authored; the Nitro palette is applied via
 * inline `style={{}}` with `nitro.*` values (intentional here; the rest of the
 * site uses cc-* tokens). Every SVG id is prefixed "guardrails-v2-".
 */

interface GuardrailsVariant2Props {
  readonly className?: string;
}

const MONO: CSSProperties = {
  fontFamily: nitro.mono,
  fontVariantNumeric: "tabular-nums",
};

/** GraphQL hexagon mark cloned by eye from the Nitro schema icon. */
function GraphqlMark() {
  return (
    <svg
      viewBox="0 0 16 16"
      width={13}
      height={13}
      aria-hidden="true"
      style={{ flex: "0 0 auto" }}
    >
      <path
        fill={nitro.icGraphql}
        d="M8 1.2 13.9 4.6v6.8L8 14.8 2.1 11.4V4.6L8 1.2Zm0 1.5L3.4 5.3v5.4L8 13.3l4.6-2.6V5.3L8 2.7Z"
      />
      <circle cx="8" cy="8" r="1.7" fill={nitro.icGraphql} />
    </svg>
  );
}

/** Coral circle-x glyph cloned by eye from Nitro's breaking-change icon. */
function BreakingIcon() {
  return (
    <svg
      viewBox="0 0 16 16"
      width={13}
      height={13}
      aria-hidden="true"
      style={{ flex: "0 0 auto" }}
    >
      <circle cx="8" cy="8" r="6.5" fill={nitro.errorText} />
      <path
        d="M5.4 5.4 10.6 10.6M10.6 5.4 5.4 10.6"
        stroke={nitro.bg}
        strokeWidth="1.6"
        strokeLinecap="round"
      />
    </svg>
  );
}

/** Small people glyph for the "clients affected" note. */
function ClientsIcon() {
  return (
    <svg
      viewBox="0 0 16 16"
      width={12}
      height={12}
      aria-hidden="true"
      style={{ flex: "0 0 auto" }}
    >
      <circle cx="6" cy="5" r="2.4" fill={nitro.errorText} />
      <path
        d="M1.6 13c0-2.4 2-4 4.4-4s4.4 1.6 4.4 4"
        fill="none"
        stroke={nitro.errorText}
        strokeWidth="1.4"
        strokeLinecap="round"
      />
      <circle
        cx="11.4"
        cy="5.6"
        r="2"
        fill="none"
        stroke={nitro.errorText}
        strokeWidth="1.3"
      />
      <path
        d="M10.4 13c0-1.7 1-3 2.9-3 1 0 1.8.4 2.3 1"
        fill="none"
        stroke={nitro.errorText}
        strokeWidth="1.3"
        strokeLinecap="round"
      />
    </svg>
  );
}

export function GuardrailsVariant2({ className }: GuardrailsVariant2Props) {
  return (
    <div
      className={className}
      style={{
        position: "relative",
        margin: "0 auto",
        width: "100%",
        maxWidth: "21rem",
        userSelect: "none",
        fontFamily: nitro.font,
        fontSize: 12,
        background: nitro.bg,
        border: `1px solid ${nitro.border}`,
        borderRadius: nitro.radius,
        overflow: "hidden",
        color: nitro.text,
        boxShadow: "0 1px 3px rgba(2,6,16,0.6)",
      }}
    >
      {/* Thin title row: schema coordinate + BREAKING badge. */}
      <div
        style={{
          display: "flex",
          alignItems: "center",
          gap: 8,
          height: 36,
          padding: "0 12px",
          background: nitro.surface,
          borderBottom: `1px solid ${nitro.border}`,
        }}
      >
        <GraphqlMark />
        <span
          style={{
            ...MONO,
            fontSize: 12,
            color: nitro.textStrong,
            whiteSpace: "nowrap",
          }}
        >
          Product.rating
        </span>
        <span style={{ flex: 1 }} />
        <span
          style={{
            display: "inline-flex",
            alignItems: "center",
            gap: 4,
            fontSize: 10,
            fontWeight: 600,
            letterSpacing: "0.04em",
            color: nitro.errorText,
            background: nitro.graphNodeFailure,
            border: `1px solid ${nitro.error}`,
            borderRadius: 999,
            padding: "2px 8px",
            whiteSpace: "nowrap",
          }}
        >
          <BreakingIcon />
          BREAKING
        </span>
      </div>

      {/* The single change, as a one-line removal diff. */}
      <div
        style={{
          display: "flex",
          alignItems: "baseline",
          gap: 8,
          padding: "12px 12px 6px",
          ...MONO,
          fontSize: 12,
          lineHeight: 1.5,
        }}
      >
        <span
          aria-hidden="true"
          style={{ flex: "0 0 auto", color: nitro.errorText, width: 8 }}
        >
          &minus;
        </span>
        <span style={{ minWidth: 0 }}>
          <span style={{ color: nitro.synField }}>rating</span>
          <span style={{ color: nitro.synPunct }}>: </span>
          <span style={{ color: nitro.synType }}>Float</span>
        </span>
      </div>

      {/* Plain-language summary of the change kind. */}
      <p
        style={{
          margin: 0,
          padding: "0 12px 12px 28px",
          fontSize: 11.5,
          color: nitro.textSecondary,
          lineHeight: 1.45,
        }}
      >
        Field removed from{" "}
        <span style={{ ...MONO, color: nitro.text }}>Product</span>.
      </p>

      {/* Impact note: published clients affected. */}
      <div
        style={{
          display: "flex",
          alignItems: "center",
          gap: 8,
          padding: "9px 12px",
          borderTop: `1px solid ${nitro.border}`,
          background: nitro.surface,
        }}
      >
        <ClientsIcon />
        <span style={{ fontSize: 11.5, color: nitro.text }}>
          <span style={{ ...MONO, fontWeight: 600, color: nitro.errorText }}>
            3
          </span>{" "}
          published clients affected
        </span>
      </div>
    </div>
  );
}
