/**
 * "Production view" scene, concept 3 ("Operations ranked by impact"), v3
 * "Signal & Metrics" (dark cc-* panel).
 *
 * Re-expresses the v2 Nitro impact ranking in v3's locked metrics strategy with
 * layout archetype A (stat-left / signal-right): the measurable result leads. The
 * hero is a single large cream "#1" over a lowercase mono caption, sitting left.
 * Bound to its right, the one teal accent is a ranked impact bar-meter: four
 * EShops operations stacked top-down by real impact (the ScrollScenes
 * ServiceTopology bar idiom), the rank-1 checkout bar drawn in teal and carrying
 * exactly one filled teal node at its reading edge, the three lower operations
 * left inactive grey. The single status hue is the coral "firing" chip in the
 * eyebrow: checkout is the one operation whose calls are erroring, a genuine
 * state. A dashed-divider footer prints the one-line reading.
 *
 * Content is faithful to the v2 ObserveVariant3: operations checkout, updateCart,
 * productPage, searchCatalog ranked by descending impact (92 / 64 / 41 / 27);
 * checkout pinned #1, firing, the only 5xx operation, 21% of its calls 5xx. Only
 * the visual language changes to the v3 dark metrics panel.
 *
 * Static settled frame: server component, no "use client", no hooks, no motion.
 * Local cc-* palette mirrors the cc-* tokens; teal is the only decorative hue and
 * is bound to the #1 impact bar + its one node, coral is rationed to the single
 * firing operation. No SVG, so no ids; any would be prefixed "v3-observe-3-".
 */
import type { CSSProperties } from "react";

interface ObserveVariant3Props {
  readonly className?: string;
}

/* v3 "Signal & Metrics" strict cc-* dark palette. Teal is the ONLY decorative
 * hue and is bound to the #1 impact bar; coral encodes the one genuine status,
 * the firing checkout operation. Everything else is cream / grey. */
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

interface ImpactRow {
  readonly operation: string;
  /** 0..100, drives the bar width and the descending reading order. */
  readonly impact: number;
  /** The single firing (5xx) operation, the one genuine status element. */
  readonly firing: boolean;
}

// Locked EShops sample: checkout #1 is the only firing operation, impact
// descends down the ranking.
const ROWS: readonly ImpactRow[] = [
  { operation: "checkout", impact: 92, firing: true },
  { operation: "updateCart", impact: 64, firing: false },
  { operation: "productPage", impact: 41, firing: false },
  { operation: "searchCatalog", impact: 27, firing: false },
];

const MAX = 92;

export function ObserveVariant3({ className }: ObserveVariant3Props) {
  const rowGrid: CSSProperties = {
    display: "grid",
    gridTemplateColumns: "72px 1fr",
    alignItems: "center",
    columnGap: 8,
    padding: "4px 0",
  };

  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      aria-hidden="true"
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        {/* eyebrow row: view label + the one status chip (coral firing) */}
        <div className="flex items-center justify-between gap-2">
          <span
            className="font-mono text-[0.58rem] tracking-[0.15em] uppercase"
            style={{ color: cc.navLabel }}
          >
            operations by impact
          </span>
          <span
            className="inline-flex items-center gap-1.5 rounded-full border px-2 py-0.5 font-mono text-[0.5rem] tracking-[0.08em] uppercase"
            style={{ borderColor: `${cc.coral}59`, color: cc.coral }}
          >
            <span
              className="inline-block h-1.5 w-1.5 rounded-full"
              style={{ background: cc.coral }}
            />
            firing
          </span>
        </div>

        {/* hero band (archetype A): cream numeral left, ranked signal right */}
        <div className="mt-4 flex items-center gap-3">
          {/* the measurable result: checkout is the #1 impact operation */}
          <div className="shrink-0">
            <p
              className="leading-none font-semibold"
              style={{
                margin: 0,
                fontFamily: HEADING,
                fontSize: "2.75rem",
                color: cc.heading,
                fontVariantNumeric: "tabular-nums",
              }}
            >
              #1
            </p>
            <p
              className="mt-1.5 font-mono text-[0.68rem] lowercase"
              style={{ color: cc.inkDim }}
            >
              impact: checkout
            </p>
          </div>

          {/* the one teal signal: operations ranked top-down by impact, the
              rank-1 checkout bar teal with its single node, the rest grey */}
          <div className="min-w-0 flex-1">
            {ROWS.map((row, i) => {
              const active = i === 0;
              const width = (row.impact / MAX) * 100;
              return (
                <div key={row.operation} style={rowGrid}>
                  {/* operation label; the #1 firing row reads strongest */}
                  <span
                    className="overflow-hidden font-mono text-[0.52rem] whitespace-nowrap"
                    style={{
                      color: active ? cc.heading : cc.ink,
                      fontWeight: active ? 600 : 400,
                      textOverflow: "ellipsis",
                    }}
                  >
                    {row.operation}
                  </span>

                  {/* impact bar: cc-surface track + fill; #1 is the teal signal
                      and carries the one filled teal node at its reading edge */}
                  <span
                    className="relative block w-full rounded-full"
                    style={{
                      height: 7,
                      background: cc.surface,
                      border: `1px solid ${cc.cardBorder}`,
                    }}
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
                </div>
              );
            })}
          </div>
        </div>

        {/* interpretation caption under a dashed faint divider */}
        <div
          className="mt-4 border-t border-dashed pt-3"
          style={{ borderColor: cc.inkFaint }}
        >
          <p
            className="text-center font-mono text-[0.62rem] lowercase"
            style={{ color: cc.inkDim }}
          >
            the operation worth fixing first
          </p>
        </div>
      </div>
    </div>
  );
}
