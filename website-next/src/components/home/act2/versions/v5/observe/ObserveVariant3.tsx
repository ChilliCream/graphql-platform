interface ObserveVariant3Props {
  readonly className?: string;
}

/**
 * Production-view scene, variant 3 (v5 "Schematic Lines"): operations ranked by
 * impact.
 *
 * A reductive monoline rank chart. Four GraphQL operations stack top-down as
 * horizontal length-bars whose length encodes impact (checkout 92, updateCart
 * 64, productPage 41, searchCatalog 27), each anchored to an open rank node on a
 * shared left spine. The single teal thread is the answer-path the headline
 * names: it leaves a hollow teal source ring below the list and runs up the rank
 * spine, weaving behind the lower rank nodes, to terminate on the rank-1 node,
 * the operation that matters most. That node happens to be checkout, the only
 * firing operation, so its bar, end tick, label, and 21% 5xx read carry the one
 * coral status hue; everything else stays cc-ink-faint grey.
 *
 * Borrowed content (exact, from the v2 sibling): checkout impact 92 firing
 * (5xx), updateCart 64, productPage 41, searchCatalog 27, ranked by impact with
 * checkout pinned at #1. React Server Component: no hooks, no client APIs,
 * settled final frame only. Every svg id is prefixed "v5-observe-3-".
 */

const C = {
  surface: "#0c1322",
  inkFaint: "rgba(245, 241, 234, 0.16)",
  ink: "#a1a3af",
  navLabel: "#62748e",
  accent: "#5eead4",
  coral: "#f0786a",
  mono: 'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace',
} as const;

const ID = "v5-observe-3-";

// Left rank spine and bar scale: impact 0..100 maps onto a bar of up to MAX_LEN.
const SPINE_X = 92;
const MAX_LEN = 168;

interface RankRow {
  readonly rank: number;
  readonly op: string;
  /** 0..100, drives bar length. */
  readonly impact: number;
  readonly y: number;
  // focal: the rank-1 operation, the teal thread terminus and the firing row.
  readonly focal: boolean;
}

const ROWS: readonly RankRow[] = [
  { rank: 1, op: "checkout", impact: 92, y: 30, focal: true },
  { rank: 2, op: "updateCart", impact: 64, y: 57, focal: false },
  { rank: 3, op: "productPage", impact: 41, y: 84, focal: false },
  { rank: 4, op: "searchCatalog", impact: 27, y: 111, focal: false },
] as const;

const SOURCE_Y = 130;
const FOCAL_Y = ROWS[0].y;

function barEnd(impact: number): number {
  return SPINE_X + (impact / 100) * MAX_LEN;
}

export function ObserveVariant3({ className }: ObserveVariant3Props) {
  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      aria-hidden="true"
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          operations by impact
        </p>

        {/* Monoline rank chart floating directly on the card. */}
        <svg
          viewBox="0 0 280 150"
          width="100%"
          role="img"
          aria-label="Four GraphQL operations ranked by impact: checkout 92 and firing at the top, then updateCart 64, productPage 41, searchCatalog 27, with the rank thread landing on the firing checkout operation."
          className="mt-4"
          style={{ display: "block", fontFamily: C.mono }}
        >
          <defs>
            <marker
              id={`${ID}head-teal`}
              markerWidth="6"
              markerHeight="6"
              refX="5"
              refY="3"
              orient="auto"
              markerUnits="userSpaceOnUse"
            >
              <path
                d="M0 0.5 L5 3 L0 5.5"
                fill="none"
                stroke={C.accent}
                strokeWidth="1"
                strokeLinecap="round"
                strokeLinejoin="round"
                vectorEffect="non-scaling-stroke"
              />
            </marker>
          </defs>

          {/* Teal thread: source ring up the rank spine to the rank-1 node. */}
          <line
            x1={SPINE_X}
            y1={SOURCE_Y - 5}
            x2={SPINE_X}
            y2={FOCAL_Y + 8}
            stroke={C.accent}
            strokeWidth="1"
            strokeLinecap="round"
            vectorEffect="non-scaling-stroke"
            markerEnd={`url(#${ID}head-teal)`}
          />
          <circle
            cx={SPINE_X}
            cy={SOURCE_Y}
            r="5"
            fill="none"
            stroke={C.accent}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
          />

          {/* Impact length-bars. The focal (rank 1) bar carries the coral status. */}
          {ROWS.map((row) => {
            const tone = row.focal ? C.coral : C.inkFaint;
            const end = barEnd(row.impact);
            return (
              <g key={row.op}>
                <line
                  x1={SPINE_X}
                  y1={row.y}
                  x2={end}
                  y2={row.y}
                  stroke={tone}
                  strokeWidth="1"
                  strokeLinecap="round"
                  vectorEffect="non-scaling-stroke"
                />
                {/* End tick: the measured value mark. */}
                <line
                  x1={end}
                  y1={row.y - 3}
                  x2={end}
                  y2={row.y + 3}
                  stroke={tone}
                  strokeWidth="1"
                  strokeLinecap="round"
                  vectorEffect="non-scaling-stroke"
                />
              </g>
            );
          })}

          {/* Lower rank nodes: open grey circles occluding the thread behind them. */}
          {ROWS.filter((row) => !row.focal).map((row) => (
            <circle
              key={row.op}
              cx={SPINE_X}
              cy={row.y}
              r="5"
              fill={C.surface}
              stroke={C.inkFaint}
              strokeWidth="1"
              vectorEffect="non-scaling-stroke"
            />
          ))}

          {/* Rank-1 focal node: teal ring + solid teal dot, the thread terminus. */}
          <circle
            cx={SPINE_X}
            cy={FOCAL_Y}
            r="6"
            fill={C.surface}
            stroke={C.accent}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
          />
          <circle cx={SPINE_X} cy={FOCAL_Y} r="2.5" fill={C.accent} />

          {/* Rank numbers + operation labels; checkout reads coral as the firing row. */}
          {ROWS.map((row) => (
            <g key={row.op}>
              <text
                x="16"
                y={row.y + 3}
                textAnchor="end"
                fontSize="7"
                letterSpacing="0.08em"
                fill={C.navLabel}
              >
                {row.rank}
              </text>
              <text
                x="24"
                y={row.y + 3}
                fontSize="8"
                fill={row.focal ? C.coral : C.ink}
              >
                {row.op}
              </text>
            </g>
          ))}

          {/* Status read for the firing rank-1 operation. */}
          <text
            x={barEnd(92)}
            y="20"
            textAnchor="end"
            fontSize="7"
            letterSpacing="0.08em"
            fill={C.coral}
          >
            21% 5XX
          </text>
        </svg>

        {/* Single footer numeral: the rank that decides where you look first. */}
        <div className="border-cc-card-border mt-4 border-t pt-4">
          <p className="font-heading text-cc-heading text-h4 leading-none font-semibold">
            #1
          </p>
          <p className="text-cc-ink-dim mt-1.5 text-xs">
            checkout, top impact and the only one firing
          </p>
        </div>
      </div>
    </div>
  );
}
