import type { CSSProperties, ReactNode } from "react";

import { nitro } from "@/src/components/home/act2/variants/nitroTheme";

/**
 * "Release safety" scene, variant 1 — a compact schema diff mini.
 *
 * One small widget: a unified diff of a few `schema.graphql` lines as the Nitro
 * registry renders a proposed change for review. Each changed line carries a
 * classifier chip — green SAFE for additive changes, coral BREAKING for the
 * removed field. A single thin title row names the file; everything else is the
 * diff body, cropped to the essential hunk. No tabs, no toolbar, no sidebar.
 *
 * STATIC render of the settled final frame: no motion package, no clocks, no
 * in-view hooks. GraphQL is hand-tokenized by eye with the Nitro editor token
 * colors. The Nitro palette is applied via inline `style={{}}` with `nitro.*`
 * values (intentional here; the rest of the site uses cc-* tokens). Every SVG id
 * is prefixed "guardrails-v1-".
 */

interface GuardrailsVariant1Props {
  readonly className?: string;
}

type Marker = "add" | "del" | "ctx";

interface DiffLine {
  readonly marker: Marker;
  /** Tokenized GraphQL runs for the line body. */
  readonly tokens: readonly Token[];
  /** Severity chip, when this line is a reviewed change. */
  readonly chip?: "SAFE" | "BREAKING";
}

interface Token {
  readonly text: string;
  readonly color: string;
}

const MONO: CSSProperties = {
  fontFamily: nitro.mono,
  fontVariantNumeric: "tabular-nums",
};

const kw = (text: string): Token => ({ text, color: nitro.synKeyword });
const ty = (text: string): Token => ({ text, color: nitro.synType });
const fl = (text: string): Token => ({ text, color: nitro.synField });
const pn = (text: string): Token => ({ text, color: nitro.synPunct });
const cm = (text: string): Token => ({ text, color: nitro.synComment });

const MARKER_GLYPH: Record<Marker, string> = { add: "+", del: "-", ctx: " " };

const MARKER_COLOR: Record<Marker, string> = {
  add: nitro.successText,
  del: nitro.errorText,
  ctx: nitro.textDim,
};

/** Tinted gutter + body background for added / removed lines, transparent for context. */
const MARKER_BG: Record<Marker, string> = {
  add: "rgba(63, 208, 127, 0.08)",
  del: "rgba(242, 118, 107, 0.08)",
  ctx: "transparent",
};

function SeverityChip({ kind }: { readonly kind: "SAFE" | "BREAKING" }) {
  const safe = kind === "SAFE";
  const color = safe ? nitro.successText : nitro.errorText;
  return (
    <span
      style={{
        flex: "0 0 auto",
        display: "inline-flex",
        alignItems: "center",
        gap: 4,
        padding: "1px 6px",
        borderRadius: 999,
        fontSize: 9.5,
        fontWeight: 600,
        letterSpacing: "0.04em",
        color,
        background: safe
          ? "rgba(63, 208, 127, 0.12)"
          : "rgba(242, 118, 107, 0.12)",
        border: `1px solid ${color}55`,
      }}
    >
      <span
        aria-hidden="true"
        style={{
          width: 5,
          height: 5,
          borderRadius: 999,
          background: color,
        }}
      />
      {kind}
    </span>
  );
}

function DiffRow({ line }: { readonly line: DiffLine }) {
  return (
    <div
      style={{
        display: "flex",
        alignItems: "center",
        gap: 8,
        padding: "0 12px 0 0",
        background: MARKER_BG[line.marker],
        lineHeight: "20px",
        minHeight: 20,
      }}
    >
      <span
        aria-hidden="true"
        style={{
          ...MONO,
          flex: "0 0 auto",
          width: 18,
          textAlign: "center",
          color: MARKER_COLOR[line.marker],
          fontSize: 11,
          fontWeight: line.marker === "ctx" ? 400 : 700,
          background:
            line.marker === "ctx"
              ? "transparent"
              : `${MARKER_COLOR[line.marker]}1f`,
          alignSelf: "stretch",
        }}
      >
        {MARKER_GLYPH[line.marker]}
      </span>
      <code
        style={{
          ...MONO,
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
  const lines: readonly DiffLine[] = [
    {
      marker: "ctx",
      tokens: [kw("type "), ty("Product"), pn(" {")],
    },
    {
      marker: "add",
      tokens: [pn("  "), fl("reviewCount"), pn(": "), ty("Int"), pn("!")],
      chip: "SAFE",
    },
    {
      marker: "del",
      tokens: [pn("  "), fl("legacySku"), pn(": "), ty("String")],
      chip: "BREAKING",
    },
    {
      marker: "add",
      tokens: [
        pn("  "),
        fl("price"),
        pn(": "),
        ty("Money"),
        pn("! "),
        cm("@deprecated"),
      ],
      chip: "SAFE",
    },
    {
      marker: "ctx",
      tokens: [pn("}")],
    },
  ];

  return (
    <div
      className={className}
      style={{
        margin: "0 auto",
        width: "100%",
        maxWidth: "21rem",
        userSelect: "none",
        fontFamily: nitro.font,
        background: nitro.bg,
        border: `1px solid ${nitro.border}`,
        borderRadius: nitro.radius,
        overflow: "hidden",
        color: nitro.text,
        boxShadow: "0 1px 3px rgba(2,6,16,0.6)",
      }}
    >
      {/* Single thin title row: the file under review. */}
      <div
        style={{
          display: "flex",
          alignItems: "center",
          gap: 8,
          height: 32,
          padding: "0 12px",
          background: nitro.surface,
          borderBottom: `1px solid ${nitro.border}`,
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
            fill={nitro.icGraphql}
            d="M8 1.2 13.9 4.6v6.8L8 14.8 2.1 11.4V4.6L8 1.2Zm0 1.5L3.4 5.3v5.4L8 13.3l4.6-2.6V5.3L8 2.7Z"
          />
          <circle cx="8" cy="8" r="1.7" fill={nitro.icGraphql} />
        </svg>
        <span
          style={{
            ...MONO,
            fontSize: 11.5,
            fontWeight: 600,
            color: nitro.textStrong,
            whiteSpace: "nowrap",
          }}
        >
          schema.graphql
        </span>
        <span style={{ flex: 1 }} />
        <span
          style={{
            ...MONO,
            fontSize: 10,
            color: nitro.successText,
            whiteSpace: "nowrap",
          }}
        >
          +2
        </span>
        <span
          style={{
            ...MONO,
            fontSize: 10,
            color: nitro.errorText,
            whiteSpace: "nowrap",
          }}
        >
          -1
        </span>
      </div>

      {/* Diff body: a single hunk, one removed field flagged breaking. */}
      <div style={{ padding: "8px 0" }}>
        {lines.map(
          (line, i): ReactNode => (
            <DiffRow key={i} line={line} />
          ),
        )}
      </div>
    </div>
  );
}
