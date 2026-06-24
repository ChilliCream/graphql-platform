import type { CSSProperties } from "react";

import { nitro } from "@/src/components/home/act2/variants/nitroTheme";

/**
 * "Release safety" scene illustration (variant 5) — the Stage gate mini.
 *
 * A small, focused widget cropped from the Nitro / Banana Cake Pop schema
 * registry (GitHub-dark): a single promotion lane, `dev` on the left, `prod`
 * on the right, joined by a short arrow with a registry check sitting on it.
 * The check came back blocked, so a coral badge marks the gate and the prod
 * stage stays on its previously published tag. One line underneath names the
 * breaking change the registry caught.
 *
 * This is deliberately minimal: two compact stage chips, one gate, one reason
 * line. No deployment table, no tabs, no app chrome. It reads as the answer to
 * a single question — can this schema be promoted to production?
 *
 * Static render of the settled final frame: no motion package, no clocks, no
 * in-view hooks. All chrome is hand-authored markup; the Nitro palette is
 * applied via inline `style={{}}` with `nitro.*` values (intentional here; the
 * rest of the site uses cc-* tokens). Every SVG id is prefixed `guardrails-v5-`.
 */

interface GuardrailsVariant5Props {
  readonly className?: string;
}

const MONO: CSSProperties = {
  fontFamily: nitro.mono,
  fontVariantNumeric: "tabular-nums",
};

export function GuardrailsVariant5({ className }: GuardrailsVariant5Props) {
  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      style={{
        background: nitro.bg,
        border: `1px solid ${nitro.border}`,
        borderRadius: nitro.radius,
        overflow: "hidden",
        fontFamily: nitro.font,
        color: nitro.text,
        boxShadow: "0 1px 3px rgba(2, 6, 16, 0.6)",
      }}
    >
      {/* Title row: one thin label naming the gate. */}
      <div
        style={{
          display: "flex",
          alignItems: "center",
          gap: 7,
          height: 30,
          padding: "0 12px",
          borderBottom: `1px solid ${nitro.border}`,
          background: nitro.surface,
        }}
      >
        <RegistryIcon />
        <span
          style={{
            fontSize: 12,
            fontWeight: 600,
            color: nitro.textStrong,
            whiteSpace: "nowrap",
          }}
        >
          Promote to production
        </span>
      </div>

      {/* The lane: dev → registry gate → prod. */}
      <div
        style={{
          display: "flex",
          alignItems: "center",
          gap: 4,
          padding: "16px 12px 14px",
        }}
      >
        <StageChip name="dev" tag="eshops@2291" state="ok" />
        <GateArrow />
        <StageChip name="prod" tag="eshops@2274" state="blocked" />
      </div>

      {/* The reason line: the breaking change the registry caught. */}
      <div
        style={{
          display: "flex",
          alignItems: "baseline",
          gap: 7,
          padding: "9px 12px 11px",
          borderTop: `1px solid ${nitro.border}`,
          background: nitro.surface,
          fontSize: 11,
        }}
      >
        <span
          aria-hidden="true"
          style={{
            flex: "0 0 auto",
            width: 6,
            height: 6,
            borderRadius: "50%",
            background: nitro.errorText,
            transform: "translateY(1px)",
          }}
        />
        <span style={{ ...MONO, color: nitro.textSecondary, lineHeight: 1.4 }}>
          <span style={{ color: nitro.synField }}>Product.rating</span> removed
          &middot; breaking change
        </span>
      </div>
    </div>
  );
}

/* ── lane pieces ─────────────────────────────────────────────────────────────── */

/** One stage chip: stage name + its currently published schema tag. */
function StageChip({
  name,
  tag,
  state,
}: {
  readonly name: string;
  readonly tag: string;
  readonly state: "ok" | "blocked";
}) {
  const blocked = state === "blocked";
  const accent = blocked ? nitro.errorText : nitro.successText;
  return (
    <div
      style={{
        flex: "1 1 0",
        minWidth: 0,
        display: "flex",
        flexDirection: "column",
        gap: 4,
        padding: "9px 10px",
        borderRadius: 6,
        border: `1px solid ${blocked ? nitro.error : nitro.graphEdge}`,
        background: nitro.card,
      }}
    >
      <span
        style={{
          display: "flex",
          alignItems: "center",
          gap: 5,
          ...MONO,
          fontSize: 12,
          fontWeight: 600,
          color: nitro.textStrong,
        }}
      >
        <span
          aria-hidden="true"
          style={{
            width: 7,
            height: 7,
            borderRadius: "50%",
            background: accent,
          }}
        />
        {name}
      </span>
      <span
        style={{
          ...MONO,
          fontSize: 9.5,
          color: nitro.textDim,
          whiteSpace: "nowrap",
          overflow: "hidden",
          textOverflow: "ellipsis",
        }}
      >
        {tag}
      </span>
    </div>
  );
}

/** Short promotion arrow with a coral "blocked" registry badge on the edge. */
function GateArrow() {
  return (
    <div
      style={{
        flex: "0 0 auto",
        display: "flex",
        flexDirection: "column",
        alignItems: "center",
        gap: 5,
        padding: "0 2px",
      }}
    >
      <span
        style={{
          display: "inline-flex",
          alignItems: "center",
          gap: 4,
          padding: "2px 6px",
          borderRadius: 999,
          border: `1px solid ${nitro.error}`,
          background: "rgba(255, 30, 10, 0.1)",
          color: nitro.errorText,
          ...MONO,
          fontSize: 8.5,
          letterSpacing: "0.06em",
          textTransform: "uppercase",
          whiteSpace: "nowrap",
        }}
      >
        <LockIcon />
        blocked
      </span>
      <svg
        aria-hidden="true"
        width={40}
        height={10}
        viewBox="0 0 40 10"
        style={{ display: "block" }}
      >
        <defs>
          <marker
            id="guardrails-v5-arrow"
            markerWidth={6}
            markerHeight={6}
            refX={4.5}
            refY={3}
            orient="auto"
          >
            <path d="M0,0 L6,3 L0,6 Z" fill={nitro.error} />
          </marker>
        </defs>
        <line
          x1={0}
          y1={5}
          x2={36}
          y2={5}
          stroke={nitro.error}
          strokeWidth={1.6}
          strokeDasharray="4 4"
          markerEnd="url(#guardrails-v5-arrow)"
        />
      </svg>
    </div>
  );
}

/* ── icons ───────────────────────────────────────────────────────────────────── */

/** GraphQL-mark registry glyph for the title row. */
function RegistryIcon() {
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

/** Small lock glyph for the blocked badge; inherits the badge color via fill. */
function LockIcon() {
  return (
    <svg
      aria-hidden="true"
      width={9}
      height={9}
      viewBox="0 0 16 16"
      fill={nitro.errorText}
      style={{ flex: "0 0 auto" }}
    >
      <path
        id="guardrails-v5-lock"
        d="M5 6V4.5a3 3 0 1 1 6 0V6h.5A1.5 1.5 0 0 1 13 7.5v5A1.5 1.5 0 0 1 11.5 14h-7A1.5 1.5 0 0 1 3 12.5v-5A1.5 1.5 0 0 1 4.5 6H5Zm1.5 0h3V4.5a1.5 1.5 0 0 0-3 0V6Z"
      />
    </svg>
  );
}
