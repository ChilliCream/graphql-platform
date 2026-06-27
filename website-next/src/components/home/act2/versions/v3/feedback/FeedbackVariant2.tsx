/**
 * "Agentic coding" scene, concept 2 ("MCP tool catalog"), v3 "Signal & Metrics"
 * (dark cc-* panel).
 *
 * Re-expresses the v2 FeedbackVariant2 MCP tool list (published /graphql/mcp
 * operations exposed as tools, each tagged query/mutation with a behavior hint)
 * as the measured result the v3 strategy leads with. The hero is a single cream
 * "18", the count of published operations the catalog exposes as callable tools.
 * Beneath it (layout B, stat-top / signal-row) a full-width ranked bar strip
 * breaks those 18 down by behavior hint (read-only, idempotent, destructive,
 * open-world), descending by count. The single teal signal is the rank-1
 * read-only bar carrying the one filled teal node: most of the surface is safe to
 * call unattended. The one genuine status element is the destructive class, the
 * calls that need an approval gate, marked by a single coral dot on its row. A
 * dashed-divider footer reads the payoff.
 *
 * Content traces to v2 FeedbackVariant2: the /graphql/mcp tool surface, query +
 * mutation kinds, readOnly / idempotent / destructive behavior hints (plus the
 * open-world hint named in the concept). Only the visual language changes to the
 * v3 dark metrics panel.
 *
 * Static settled frame: server component, no "use client", no hooks, no motion.
 * Local cc-* palette mirrored exactly; teal is the lone decorative hue (the
 * rank-1 bar and its one node), coral is rationed to the destructive class. No
 * SVG, so no ids; any would be prefixed "v3-feedback-2-".
 */
import type { CSSProperties } from "react";

interface FeedbackVariant2Props {
  readonly className?: string;
}

/* Strict cc-* dark palette, mirrored locally per the v3 system. Teal is the only
 * decorative hue and is bound to the rank-1 read-only bar; coral encodes the one
 * genuine status, the destructive class that needs an approval gate. */
const cc = {
  surface: "#0c1322",
  cardBorder: "rgba(245,241,234,0.12)",
  inkFaint: "rgba(245,241,234,0.16)",
  heading: "#f5f0ea",
  ink: "#a1a3af",
  inkDim: "rgba(245,241,234,0.62)",
  navLabel: "#62748e",
  accent: "#5eead4",
  coral: "#f0786a",
} as const;

const HEADING = '"Josefin Sans", Futura, sans-serif';

interface BehaviorRow {
  readonly hint: string;
  /** Tools carrying this behavior hint; drives bar width and the ranking. */
  readonly count: number;
  /** The destructive class: the one genuine status (needs an approval gate). */
  readonly gated: boolean;
}

// 18 published tools, split by behavior hint, descending by count.
const ROWS: readonly BehaviorRow[] = [
  { hint: "read-only", count: 8, gated: false },
  { hint: "idempotent", count: 5, gated: false },
  { hint: "destructive", count: 3, gated: true },
  { hint: "open-world", count: 2, gated: false },
];

const MAX = 8;

export function FeedbackVariant2({ className }: FeedbackVariant2Props) {
  const rowGrid: CSSProperties = {
    display: "grid",
    gridTemplateColumns: "76px 1fr 14px 12px",
    alignItems: "center",
    columnGap: 9,
    padding: "5px 0",
  };

  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      aria-hidden="true"
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        {/* eyebrow row: the view label + the real registry path */}
        <div className="flex items-center justify-between gap-2">
          <span
            className="font-mono text-[0.58rem] tracking-[0.15em] uppercase"
            style={{ color: cc.navLabel }}
          >
            mcp tool catalog
          </span>
          <span
            className="font-mono text-[0.5rem] tracking-[0.06em]"
            style={{ color: cc.navLabel }}
          >
            /graphql/mcp
          </span>
        </div>

        {/* hero band (layout B, stat-top): the honest headline count */}
        <div className="mt-3 flex items-baseline gap-2">
          <span
            className="leading-none font-semibold"
            style={{
              fontFamily: HEADING,
              fontSize: "2.75rem",
              color: cc.heading,
              fontVariantNumeric: "tabular-nums",
            }}
          >
            18
          </span>
          <span
            className="font-mono lowercase"
            style={{
              fontSize: "0.72rem",
              letterSpacing: "0.04em",
              color: cc.inkDim,
            }}
          >
            operations exposed
          </span>
        </div>

        {/* signal row: the 18 tools ranked by behavior hint. teal marks rank-1
            (read-only, safe to auto-call); coral marks the destructive class. */}
        <div className="mt-4">
          {ROWS.map((row, i) => {
            const active = i === 0;
            const width = (row.count / MAX) * 100;
            return (
              <div key={row.hint} style={rowGrid}>
                {/* behavior hint label; the rank-1 class reads strongest */}
                <span
                  className="font-mono text-[0.6rem]"
                  style={{
                    color: active ? cc.heading : cc.ink,
                    fontWeight: active ? 600 : 400,
                  }}
                >
                  {row.hint}
                </span>

                {/* bar: cc-surface track + fill; rank-1 is the teal signal and
                    carries the one filled teal node at its reading edge */}
                <span
                  className="relative block w-full rounded-full"
                  style={{ height: 7, background: cc.surface }}
                >
                  <span
                    className="absolute inset-y-0 left-0 rounded-full"
                    style={{
                      width: `${width}%`,
                      background: active ? cc.accent : cc.ink,
                      opacity: active ? 0.72 : 0.28,
                    }}
                  />
                  {active ? (
                    <span
                      aria-hidden="true"
                      className="absolute rounded-full"
                      style={{
                        left: `${width}%`,
                        top: "50%",
                        width: 6,
                        height: 6,
                        transform: "translate(-50%, -50%)",
                        background: cc.accent,
                      }}
                    />
                  ) : null}
                </span>

                {/* tool count for the class */}
                <span
                  className="text-right font-mono text-[0.6rem]"
                  style={{
                    color: active ? cc.heading : cc.ink,
                    fontVariantNumeric: "tabular-nums",
                  }}
                >
                  {row.count}
                </span>

                {/* status dot: only the destructive class carries coral */}
                {row.gated ? (
                  <span
                    className="relative inline-flex h-3 w-3 items-center justify-center"
                    style={{ justifySelf: "center" }}
                  >
                    <span
                      className="absolute inset-0 rounded-full"
                      style={{
                        background: `${cc.coral}26`,
                        border: `1px solid ${cc.coral}99`,
                      }}
                    />
                    <span
                      className="rounded-full"
                      style={{ width: 4, height: 4, background: cc.coral }}
                    />
                  </span>
                ) : (
                  <span />
                )}
              </div>
            );
          })}
        </div>

        {/* dashed-divider payoff caption */}
        <div
          className="mt-4 border-t border-dashed pt-3"
          style={{ borderColor: cc.inkFaint }}
        >
          <p
            className="text-center font-mono text-[0.62rem] lowercase"
            style={{ color: cc.inkDim }}
          >
            the exact surface an agent can call
          </p>
        </div>
      </div>
    </div>
  );
}
