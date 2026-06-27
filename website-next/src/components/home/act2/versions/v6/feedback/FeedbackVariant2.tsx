import type { CSSProperties } from "react";

/**
 * v6 "Agentic coding" hook, variant 2: the MCP tool registry as an inspector grid.
 *
 * Bespoke, one-off illustration (no shared v6 theme): a small data table that reads
 * like the tool registry an agent sees at `/graphql/mcp`. Each row is one published
 * operation rendered as a typed tool: a mono tool name, a query / mutation type tag,
 * and one behavior-hint badge that states exactly how the call behaves. A color rail
 * and a status dot encode that behavior, idempotentHint (healthy green) and
 * openWorldHint (amber) stay calm, while the single destructiveHint on `deleteReview`
 * lights coral so the one call an agent must gate is impossible to miss. A teal
 * "approved" tag in the header ties the grid to real, approved tools.
 *
 * Static React Server Component: no hooks, no client APIs, settled final frame.
 * cc-* dark palette only, thin 1px strokes, generous negative space. Every svg id is
 * prefixed "v6-feedback-2-".
 */

interface FeedbackVariant2Props {
  readonly className?: string;
}

const ID = "v6-feedback-2-";

const MONO =
  'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace';

/** Locked v6 cc-* palette for this cell: dark surfaces, neutral ink, status hues. */
const C = {
  surface: "#0c1322",
  cardBorder: "rgba(245, 241, 234, 0.12)",
  grid: "rgba(245, 241, 234, 0.07)",
  heading: "#f5f0ea",
  ink: "#a1a3af",
  inkDim: "rgba(245, 241, 234, 0.62)",
  navLabel: "#62748e",
  accent: "#5eead4",
  healthy: "#34d399",
  amber: "#fbbf24",
  coral: "#f0786a",
} as const;

const TYPE_W = 62;
const BEHAV_W = 104;

type OpKind = "query" | "mutation";
type Behavior = "idempotentHint" | "openWorldHint" | "destructiveHint";

interface ToolRow {
  readonly name: string;
  readonly kind: OpKind;
  readonly behavior: Behavior;
}

/** Maps a behavior hint to its rail / dot / pill treatment. */
const BEHAVIOR_STYLE: Record<
  Behavior,
  { readonly hue: string; readonly bg: string; readonly border: string }
> = {
  idempotentHint: {
    hue: C.healthy,
    bg: "rgba(52, 211, 153, 0.12)",
    border: "rgba(52, 211, 153, 0.45)",
  },
  openWorldHint: {
    hue: C.amber,
    bg: "rgba(251, 191, 36, 0.1)",
    border: "rgba(251, 191, 36, 0.42)",
  },
  destructiveHint: {
    hue: C.coral,
    bg: "rgba(240, 120, 106, 0.12)",
    border: "rgba(240, 120, 106, 0.5)",
  },
};

/** Shared geometry for the small kind / behavior pills. */
const PILL: CSSProperties = {
  display: "inline-flex",
  alignItems: "center",
  gap: 4,
  width: "100%",
  padding: "2px 7px",
  borderRadius: 999,
  border: "1px solid",
  fontFamily: MONO,
  fontSize: 9,
  lineHeight: 1,
  whiteSpace: "nowrap",
};

export function FeedbackVariant2({ className }: FeedbackVariant2Props) {
  const rows: readonly ToolRow[] = [
    { name: "getProduct", kind: "query", behavior: "idempotentHint" },
    { name: "searchOrders", kind: "query", behavior: "openWorldHint" },
    { name: "deleteReview", kind: "mutation", behavior: "destructiveHint" },
  ];

  return (
    <div
      className={[
        "mx-auto w-full max-w-[336px] select-none",
        className ?? "",
      ].join(" ")}
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-4 backdrop-blur-sm">
        {/* Header: the live MCP endpoint path + an approved-status tag. */}
        <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
          <PlugGlyph />
          <span style={{ fontFamily: MONO, fontSize: 12, color: C.heading }}>
            /graphql/mcp
          </span>
          <span
            style={{
              ...PILL,
              width: "auto",
              marginLeft: "auto",
              gap: 5,
              color: C.accent,
              borderColor: "rgba(94, 234, 212, 0.5)",
              background: "rgba(94, 234, 212, 0.1)",
            }}
          >
            <span
              style={{
                width: 5,
                height: 5,
                borderRadius: 999,
                background: C.accent,
              }}
            />
            approved
          </span>
        </div>

        {/* The inspector grid: one row per published operation, typed as a tool. */}
        <div
          style={{
            marginTop: 12,
            background: C.surface,
            border: `1px solid ${C.cardBorder}`,
            borderRadius: 8,
            padding: "0 10px",
            overflow: "hidden",
          }}
        >
          {/* Column header. */}
          <div
            style={{
              display: "flex",
              alignItems: "center",
              gap: 8,
              padding: "7px 0 6px",
              borderBottom: `1px solid ${C.grid}`,
            }}
          >
            <span style={{ width: 2, flex: "none" }} />
            <HeadLabel style={{ flex: 1, minWidth: 0 }}>tool</HeadLabel>
            <HeadLabel style={{ width: TYPE_W, flex: "none" }}>type</HeadLabel>
            <HeadLabel style={{ width: BEHAV_W, flex: "none" }}>
              behavior
            </HeadLabel>
          </div>

          {/* Data rows. */}
          {rows.map((row, i) => {
            const b = BEHAVIOR_STYLE[row.behavior];
            return (
              <div
                key={row.name}
                style={{
                  display: "flex",
                  alignItems: "center",
                  gap: 8,
                  padding: "8px 0",
                  borderTop: i === 0 ? "none" : `1px solid ${C.grid}`,
                }}
              >
                <span
                  aria-hidden="true"
                  style={{
                    width: 2,
                    alignSelf: "stretch",
                    borderRadius: 2,
                    background: b.hue,
                    opacity: 0.85,
                    flex: "none",
                  }}
                />
                <span
                  style={{
                    flex: 1,
                    minWidth: 0,
                    fontFamily: MONO,
                    fontSize: 11.5,
                    color: C.heading,
                    overflow: "hidden",
                    textOverflow: "ellipsis",
                    whiteSpace: "nowrap",
                  }}
                >
                  {row.name}
                </span>
                <span style={{ width: TYPE_W, flex: "none" }}>
                  <KindTag kind={row.kind} />
                </span>
                <span style={{ width: BEHAV_W, flex: "none" }}>
                  <BehaviorBadge behavior={row.behavior} />
                </span>
              </div>
            );
          })}
        </div>

        {/* Promise: published operations, typed as tools, labeled by behavior. */}
        <p
          style={{
            marginTop: 12,
            fontFamily: MONO,
            fontSize: 9.5,
            lineHeight: 1.5,
            color: C.inkDim,
          }}
        >
          Published operations, typed as tools and labeled by exactly how each
          behaves.
        </p>
      </div>
    </div>
  );
}

/** Column header label in the registry-grid eyebrow voice. */
function HeadLabel({
  children,
  style,
}: {
  readonly children: string;
  readonly style?: CSSProperties;
}) {
  return (
    <span
      style={{
        fontFamily: MONO,
        fontSize: 8,
        letterSpacing: "0.12em",
        textTransform: "uppercase",
        color: C.navLabel,
        ...style,
      }}
    >
      {children}
    </span>
  );
}

/** Query vs. mutation operation-kind tag, kept neutral so status hues stay distinct. */
function KindTag({ kind }: { readonly kind: OpKind }) {
  return (
    <span
      style={{
        ...PILL,
        justifyContent: "flex-start",
        borderColor: C.cardBorder,
        color: C.ink,
        background: "rgba(245, 241, 234, 0.03)",
      }}
    >
      {kind}
    </span>
  );
}

/** One behavior-hint badge per tool, with a status dot encoding how safe the call is. */
function BehaviorBadge({ behavior }: { readonly behavior: Behavior }) {
  const s = BEHAVIOR_STYLE[behavior];
  return (
    <span
      style={{
        ...PILL,
        justifyContent: "flex-start",
        borderColor: s.border,
        color: s.hue,
        background: s.bg,
      }}
    >
      <span
        style={{
          width: 5,
          height: 5,
          borderRadius: 999,
          background: s.hue,
          flex: "none",
        }}
      />
      {behavior}
    </span>
  );
}

/** Small endpoint-socket mark for the header, with a teal-lit live core. */
function PlugGlyph() {
  return (
    <svg
      aria-hidden="true"
      viewBox="0 0 16 16"
      width={14}
      height={14}
      fill="none"
      style={{ flex: "none" }}
    >
      <defs>
        <linearGradient
          id={`${ID}plug`}
          x1="4"
          y1="4"
          x2="12"
          y2="12"
          gradientUnits="userSpaceOnUse"
        >
          <stop offset="0" stopColor={C.accent} />
          <stop offset="1" stopColor={C.accent} stopOpacity="0.45" />
        </linearGradient>
      </defs>
      <rect
        x="2.5"
        y="2.5"
        width="11"
        height="11"
        rx="3"
        stroke={C.navLabel}
        strokeWidth="1.1"
      />
      <circle cx="8" cy="8" r="2.4" fill={`url(#${ID}plug)`} />
    </svg>
  );
}
