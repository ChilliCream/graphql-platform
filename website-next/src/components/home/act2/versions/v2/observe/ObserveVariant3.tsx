import type { CSSProperties } from "react";

/**
 * Observe scene, variant 3 (v2 flow-diagram system) - "operations ranked by impact".
 *
 * WATERFALL/RANK topology from the locked v2 system: GraphQL operations stacked
 * as thin impact bars with right-aligned mono labels (the ScrollScenes
 * ServiceTopology bar pattern). checkout is pinned at rank 1 and firing, so it
 * carries the single teal traced path (active label + teal impact bar); every
 * other operation stays cream/grey. A compact stacked 2xx/4xx/5xx status-mix bar
 * reads the dominant response class per row, and a Stat duo footer surfaces the
 * checkout impact rank and its p95.
 *
 * Settled final frame: no animation, no hooks, no "use client". Server component,
 * cc-* palette only. Every inline SVG id is prefixed "v2-observe-3-".
 */

interface ObserveVariant3Props {
  readonly className?: string;
}

/* Locked cc-* palette. Teal is the only decorative accent; status hues encode
 * real response-class status (healthy 2xx, amber 4xx, coral firing 5xx) only. */
const CC = {
  surface: "#0c1322",
  cardBg: "rgba(12,19,34,0.55)",
  cardBorder: "rgba(245,241,234,0.12)",
  inkFaint: "rgba(245,241,234,0.16)",
  ink: "#a1a3af",
  inkDim: "rgba(245,241,234,0.62)",
  navLabel: "#62748e",
  heading: "#f5f0ea",
  accent: "#5eead4",
  healthy: "#34d399",
  amber: "#fbbf24",
  coral: "#f0786a",
} as const;

const MONO =
  'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Consolas, monospace';
const HEADING = '"Josefin Sans", Futura, sans-serif';

/** Dominant response class for an operation, drives the per-row status read. */
type StatusClass = "2xx" | "4xx" | "5xx";

interface ImpactRow {
  readonly rank: number;
  readonly operation: string;
  /** 0..100, drives the impact bar width. */
  readonly impact: number;
  readonly status: StatusClass;
  /** Response-class mix percentages, summing to 100. */
  readonly mix: {
    readonly ok: number;
    readonly client: number;
    readonly server: number;
  };
}

// Locked EShops sample, ordering + impact + status identical to v1: checkout #1
// is the firing (5xx) operation and the single teal traced path, updateCart is
// degraded (4xx), the rest are healthy (2xx). Impact descends.
const ROWS: readonly ImpactRow[] = [
  {
    rank: 1,
    operation: "checkout",
    impact: 92,
    status: "5xx",
    mix: { ok: 71, client: 8, server: 21 },
  },
  {
    rank: 2,
    operation: "updateCart",
    impact: 64,
    status: "4xx",
    mix: { ok: 86, client: 12, server: 2 },
  },
  {
    rank: 3,
    operation: "productPage",
    impact: 41,
    status: "2xx",
    mix: { ok: 99, client: 1, server: 0 },
  },
  {
    rank: 4,
    operation: "searchCatalog",
    impact: 27,
    status: "2xx",
    mix: { ok: 100, client: 0, server: 0 },
  },
];

const STATUS_TONE: Record<StatusClass, string> = {
  "2xx": CC.healthy,
  "4xx": CC.amber,
  "5xx": CC.coral,
};

const eyebrowStyle: CSSProperties = {
  margin: 0,
  fontFamily: MONO,
  fontSize: "0.58rem",
  letterSpacing: "0.15em",
  textTransform: "uppercase",
  color: CC.navLabel,
};

/** Stacked response-class mix: healthy / client error / server error segments. */
function StatusMix({ row }: { readonly row: ImpactRow }) {
  const segments = [
    { key: "ok", pct: row.mix.ok, tone: CC.healthy },
    { key: "client", pct: row.mix.client, tone: CC.amber },
    { key: "server", pct: row.mix.server, tone: CC.coral },
  ];
  return (
    <span
      aria-hidden="true"
      style={{
        display: "flex",
        height: 6,
        width: 46,
        borderRadius: 999,
        overflow: "hidden",
        background: CC.surface,
        border: `1px solid ${CC.cardBorder}`,
      }}
    >
      {segments.map((s) =>
        s.pct > 0 ? (
          <span
            key={s.key}
            style={{ width: `${s.pct}%`, background: s.tone, opacity: 0.85 }}
          />
        ) : null,
      )}
    </span>
  );
}

function Stat({
  figure,
  label,
}: {
  readonly figure: string;
  readonly label: string;
}) {
  return (
    <div>
      <p
        style={{
          margin: 0,
          fontFamily: HEADING,
          fontSize: "1.75rem",
          lineHeight: 1,
          fontWeight: 600,
          color: CC.heading,
        }}
      >
        {figure}
      </p>
      <p style={{ margin: "6px 0 0", fontSize: "0.75rem", color: CC.inkDim }}>
        {label}
      </p>
    </div>
  );
}

export function ObserveVariant3({ className }: ObserveVariant3Props) {
  return (
    <div
      aria-hidden="true"
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      style={{ width: "100%", maxWidth: 330, marginInline: "auto" }}
    >
      <div
        style={{
          border: `1px solid ${CC.cardBorder}`,
          borderRadius: 16,
          background: CC.cardBg,
          backdropFilter: "blur(4px)",
          padding: 20,
        }}
      >
        <p style={eyebrowStyle}>operations by impact</p>

        {/* Ranked rows: rank index, operation label, impact bar, status mix. */}
        <div style={{ marginTop: 14, display: "grid", rowGap: 10 }}>
          {ROWS.map((row) => {
            const traced = row.rank === 1;
            return (
              <div
                key={row.operation}
                style={{
                  display: "grid",
                  gridTemplateColumns: "12px 84px 1fr 46px",
                  alignItems: "center",
                  columnGap: 9,
                }}
              >
                {/* Rank index. */}
                <span
                  style={{
                    fontFamily: MONO,
                    fontSize: "0.6rem",
                    color: CC.navLabel,
                    textAlign: "right",
                  }}
                >
                  {row.rank}
                </span>

                {/* Operation label; the firing checkout row carries the teal accent. */}
                <span
                  style={{
                    fontFamily: MONO,
                    fontSize: "0.65rem",
                    color: traced ? CC.accent : CC.ink,
                    overflow: "hidden",
                    textOverflow: "ellipsis",
                    whiteSpace: "nowrap",
                  }}
                >
                  {row.operation}
                </span>

                {/* Impact bar: teal only on the single traced path, grey otherwise. */}
                <span
                  style={{
                    position: "relative",
                    height: 8,
                    width: "100%",
                    borderRadius: 999,
                    background: CC.surface,
                    border: `1px solid ${CC.cardBorder}`,
                    overflow: "hidden",
                  }}
                >
                  <span
                    style={{
                      position: "absolute",
                      insetBlock: 0,
                      left: 0,
                      width: `${row.impact}%`,
                      borderRadius: 999,
                      background: traced ? CC.accent : CC.ink,
                      opacity: traced ? 0.85 : 0.28,
                    }}
                  />
                </span>

                {/* Stacked 2xx/4xx/5xx response-class mix. */}
                <StatusMix row={row} />
              </div>
            );
          })}
        </div>

        {/* Firing marker for the #1 traced operation. */}
        <div
          style={{
            marginTop: 14,
            paddingTop: 14,
            borderTop: `1px solid ${CC.cardBorder}`,
            display: "flex",
            alignItems: "center",
            justifyContent: "space-between",
          }}
        >
          <span
            style={{
              display: "inline-flex",
              alignItems: "center",
              gap: 6,
              fontFamily: MONO,
              fontSize: "0.6rem",
              letterSpacing: "0.08em",
              textTransform: "uppercase",
              color: STATUS_TONE["5xx"],
              border: `1px solid ${STATUS_TONE["5xx"]}59`,
              borderRadius: 999,
              padding: "2px 8px",
            }}
          >
            <svg width={8} height={8} viewBox="0 0 8 8" aria-hidden="true">
              <circle
                id="v2-observe-3-firing-dot"
                cx={4}
                cy={4}
                r={3}
                fill={STATUS_TONE["5xx"]}
              />
            </svg>
            checkout firing
          </span>
          <span
            style={{
              fontFamily: MONO,
              fontSize: "0.6rem",
              letterSpacing: "0.08em",
              textTransform: "uppercase",
              color: CC.navLabel,
            }}
          >
            21% 5xx
          </span>
        </div>

        {/* Stat duo footer: the two key numbers for the firing operation. */}
        <div
          style={{
            marginTop: 14,
            paddingTop: 14,
            borderTop: `1px solid ${CC.cardBorder}`,
            display: "grid",
            gridTemplateColumns: "1fr 1fr",
            columnGap: 16,
          }}
        >
          <Stat figure="#1" label="impact: checkout" />
          <Stat figure="812ms" label="checkout p95" />
        </div>
      </div>
    </div>
  );
}
