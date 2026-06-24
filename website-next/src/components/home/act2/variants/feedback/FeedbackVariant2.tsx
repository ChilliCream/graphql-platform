import type { CSSProperties } from "react";

import { nitro } from "@/src/components/home/act2/variants/nitroTheme";

interface FeedbackVariant2Props {
  readonly className?: string;
}

type OpKind = "query" | "mutation";
type Behavior = "readOnlyHint" | "idempotentHint" | "destructiveHint";

interface ToolRow {
  readonly name: string;
  readonly kind: OpKind;
  readonly behavior: Behavior;
}

/**
 * Agentic-coding scene (variant 2): a compact "MCP tools" mini-list, rendered as
 * a cropped slice of the Nitro / Banana Cake Pop GitHub-dark UI.
 *
 * Four rows are what a coding agent sees when it lists the `/graphql/mcp` tools:
 * each is an operation name, a query/mutation kind tag, and one behavior-hint
 * badge (readOnlyHint / idempotentHint / destructiveHint) so the agent knows
 * which calls are safe to run unattended and which need an approval gate. Static
 * settled frame, no animation, no hooks. Mono for the operation names and tags,
 * the Nitro card chrome (bg/surface, 1px border hairline, 6px corners) so it
 * reads as a real product panel on the dark marketing page.
 */
export function FeedbackVariant2({ className }: FeedbackVariant2Props) {
  const rows: readonly ToolRow[] = [
    { name: "products", kind: "query", behavior: "readOnlyHint" },
    { name: "addToCart", kind: "mutation", behavior: "idempotentHint" },
    { name: "placeOrder", kind: "mutation", behavior: "idempotentHint" },
    { name: "cancelOrder", kind: "mutation", behavior: "destructiveHint" },
  ];

  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      style={{ fontFamily: nitro.font }}
    >
      <div
        style={{
          background: nitro.bg,
          border: `1px solid ${nitro.border}`,
          borderRadius: nitro.radius,
          overflow: "hidden",
          boxShadow: "0 1px 3px rgba(2, 6, 16, 0.6)",
        }}
      >
        {/* A single thin title row: the registry path + the exposed-tool count. */}
        <div
          style={{
            display: "flex",
            alignItems: "center",
            gap: 8,
            padding: "8px 12px",
            background: nitro.surface,
            borderBottom: `1px solid ${nitro.border}`,
          }}
        >
          <WrenchGlyph />
          <span
            style={{
              fontFamily: nitro.mono,
              fontSize: 11.5,
              color: nitro.textSecondary,
            }}
          >
            mcp / tools
          </span>
          <span
            style={{
              marginLeft: "auto",
              fontFamily: nitro.mono,
              fontSize: 10.5,
              letterSpacing: "0.06em",
              color: nitro.textDim,
            }}
          >
            {rows.length} exposed
          </span>
        </div>

        {/* The mini-list: one row per exposed tool. */}
        <div style={{ padding: "4px 0" }}>
          {rows.map((row, i) => (
            <div
              key={row.name}
              style={{
                display: "flex",
                alignItems: "center",
                gap: 8,
                padding: "8px 12px",
                borderTop: i === 0 ? "none" : `1px solid ${nitro.grid}`,
              }}
            >
              <span
                style={{
                  minWidth: 0,
                  fontFamily: nitro.mono,
                  fontSize: 12,
                  color: nitro.textStrong,
                  overflow: "hidden",
                  textOverflow: "ellipsis",
                  whiteSpace: "nowrap",
                }}
              >
                {row.name}
              </span>
              <KindTag kind={row.kind} />
              <span style={{ marginLeft: "auto", flex: "none" }}>
                <BehaviorBadge behavior={row.behavior} />
              </span>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}

/** Shared geometry for the small kind / behavior pills. */
const PILL: CSSProperties = {
  display: "inline-flex",
  alignItems: "center",
  padding: "1px 7px",
  borderRadius: 999,
  border: "1px solid",
  fontFamily: nitro.mono,
  fontSize: 9.5,
  letterSpacing: "0.06em",
  textTransform: "uppercase",
  whiteSpace: "nowrap",
};

/** Query vs. mutation operation-kind tag, colored like the schema kind icons. */
function KindTag({ kind }: { readonly kind: OpKind }) {
  const isMutation = kind === "mutation";
  return (
    <span
      style={{
        ...PILL,
        flex: "none",
        borderColor: isMutation ? nitro.icMutation : nitro.border,
        color: isMutation ? nitro.errorText : nitro.textSecondary,
        background: isMutation ? "rgba(207, 34, 46, 0.12)" : nitro.surface,
      }}
    >
      {kind}
    </span>
  );
}

/** Maps a behavior hint to its pill color treatment. */
const BEHAVIOR_STYLE: Record<
  Behavior,
  { readonly border: string; readonly color: string; readonly bg: string }
> = {
  readOnlyHint: {
    border: nitro.border,
    color: nitro.textSecondary,
    bg: nitro.surface,
  },
  idempotentHint: {
    border: nitro.success,
    color: nitro.successText,
    bg: "rgba(52, 157, 135, 0.14)",
  },
  destructiveHint: {
    border: nitro.error,
    color: nitro.errorText,
    bg: "rgba(255, 30, 10, 0.1)",
  },
};

/** One behavior-hint badge per tool, signalling how safe the call is. */
function BehaviorBadge({ behavior }: { readonly behavior: Behavior }) {
  const s = BEHAVIOR_STYLE[behavior];
  return (
    <span
      style={{
        ...PILL,
        borderColor: s.border,
        color: s.color,
        background: s.bg,
      }}
    >
      {behavior}
    </span>
  );
}

/** Small wrench mark for the title row, inheriting the secondary text color. */
function WrenchGlyph() {
  return (
    <svg
      aria-hidden="true"
      viewBox="0 0 14 14"
      width={13}
      height={13}
      fill="none"
      style={{ flex: "none" }}
    >
      <path
        d="M9.4 1.6a3.2 3.2 0 0 0-3.6 4.2L1.7 9.9a1.3 1.3 0 0 0 1.8 1.8l4.1-4.1a3.2 3.2 0 0 0 4.2-3.6L9.9 6 8 4.1Z"
        stroke={nitro.textSecondary}
        strokeWidth="1.1"
        strokeLinejoin="round"
      />
    </svg>
  );
}
