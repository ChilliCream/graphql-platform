interface ObserveVariant3Props {
  readonly className?: string;
}

/**
 * "Production view" scene, v4 "Generated Artifacts", variant 3: the operations
 * leaderboard Nitro emits from live traffic, ranked by impact.
 *
 * Locked v4 PATTERN A (one cc-surface artifact tile + a single callout). The
 * tile is the Nitro "operations by impact" view: a title bar (view name + 24h
 * scope) over four ranked rows, each carrying its rank, operation name, dominant
 * response class, a monochrome cc-ink impact bar, and the impact score. Rows
 * descend by impact, so the table reads as a leaderboard of what matters most.
 *
 * The highlighted token is the #1 operation, `checkout`, and it carries a real
 * firing status (5xx), so per the signature rule the callout adopts the status
 * hue and teal steps aside entirely: coral is the lone accent cluster. Coral
 * marks checkout's `5xx` class, its load-bearing `92` impact score, and the
 * signature callout (2.5px anchor dot, 2px underline tick, 1px leader, "#1
 * IMPACT" micro-label) pinned to it. The other rows' classes (`4xx`, `2xx`,
 * `2xx`) stay monochrome navLabel so only one status family is colored. The lone
 * cream strong token is the `checkout` name. Strip the coral and a neutral grey
 * leaderboard remains.
 *
 * Literal content (operations checkout / updateCart / productPage /
 * searchCatalog, impacts 92 / 64 / 41 / 27, classes 5xx / 4xx / 2xx / 2xx, scope
 * 24h) is borrowed verbatim from the v1 sibling. React Server Component, settled
 * final frame, no motion. Every svg id is prefixed "v4-observe-3-".
 */

const C = {
  surface: "#0c1322",
  cardBorder: "rgba(245,241,234,0.12)",
  ink: "#a1a3af",
  inkDim: "rgba(245,241,234,0.62)",
  navLabel: "#62748e",
  heading: "#f5f0ea",
  // Frozen palette keeps teal for set-wide coherence; this cell's #1 token is a
  // firing op, so coral owns the single accent cluster and teal is unused.
  accent: "#5eead4",
  coral: "#f0786a",
  mono: 'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace',
} as const;

const ID = "v4-observe-3-";

interface ImpactRow {
  readonly rank: number;
  readonly operation: string;
  /** 0..100, drives the impact bar width and the descending rank order. */
  readonly impact: number;
  /** Dominant response class (verbatim from the v1 sibling). */
  readonly status: string;
  /** True only for the one firing (5xx) operation, drives the coral accent. */
  readonly firing: boolean;
}

// Locked EShops sample (verbatim from the v1 sibling): checkout #1 is the only
// firing (5xx) operation; impact descends 92 -> 64 -> 41 -> 27.
const ROWS: readonly ImpactRow[] = [
  { rank: 1, operation: "checkout", impact: 92, status: "5xx", firing: true },
  {
    rank: 2,
    operation: "updateCart",
    impact: 64,
    status: "4xx",
    firing: false,
  },
  {
    rank: 3,
    operation: "productPage",
    impact: 41,
    status: "2xx",
    firing: false,
  },
  {
    rank: 4,
    operation: "searchCatalog",
    impact: 27,
    status: "2xx",
    firing: false,
  },
];

// Row geometry: first row center, then a constant vertical step.
const ROW_Y0 = 58;
const ROW_STEP = 27;
const STATUS_X = 160;
const BAR_X = 172;
const BAR_W = 42;
const VAL_X = 240;

export function ObserveVariant3({ className }: ObserveVariant3Props) {
  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      aria-hidden="true"
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        {/* Panel eyebrow (ScrollScenes header voice). */}
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          ranked by impact
        </p>

        <div className="mt-4">
          <svg
            viewBox="0 0 320 156"
            width="100%"
            style={{ display: "block", fontFamily: C.mono }}
          >
            <defs>
              {/* Coral open chevron for the single callout leader. */}
              <marker
                id={`${ID}head`}
                markerWidth="6"
                markerHeight="6"
                refX="4.6"
                refY="3"
                orient="auto"
                markerUnits="userSpaceOnUse"
              >
                <path
                  d="M0 0.5 L5 3 L0 5.5"
                  fill="none"
                  stroke={C.coral}
                  strokeWidth="1"
                />
              </marker>
            </defs>

            {/* ---- Artifact tile: the ranked operations leaderboard ---- */}
            <rect
              x="6"
              y="4"
              width="246"
              height="148"
              rx="8"
              fill={C.surface}
              stroke={C.cardBorder}
              strokeWidth="1"
            />

            {/* title bar: view name + 24h scope tag */}
            <text x="16" y="18" fontSize="9" fill={C.inkDim}>
              operations
            </text>
            <text
              x="244"
              y="18"
              textAnchor="end"
              fontSize="8"
              fill={C.navLabel}
            >
              24h
            </text>
            <line
              x1="6"
              y1="26"
              x2="252"
              y2="26"
              stroke={C.cardBorder}
              strokeWidth="1"
            />

            {/* column header labels */}
            <text
              x="30"
              y="39"
              fontSize="6.5"
              letterSpacing="0.08em"
              fill={C.navLabel}
            >
              OP
            </text>
            <text
              x={STATUS_X}
              y="39"
              textAnchor="end"
              fontSize="6.5"
              letterSpacing="0.08em"
              fill={C.navLabel}
            >
              STATUS
            </text>
            <text
              x={VAL_X}
              y="39"
              textAnchor="end"
              fontSize="6.5"
              letterSpacing="0.08em"
              fill={C.navLabel}
            >
              IMPACT
            </text>
            <line
              x1="6"
              y1="44"
              x2="252"
              y2="44"
              stroke={C.cardBorder}
              strokeWidth="1"
            />

            {/* ---- Ranked rows ---- */}
            {ROWS.map((row, i) => {
              const cy = ROW_Y0 + i * ROW_STEP;
              const fillW = (BAR_W * row.impact) / 100;
              const isHero = i === 0;
              return (
                <g key={row.operation}>
                  {/* row separator above each row past the first */}
                  {i > 0 && (
                    <line
                      x1="6"
                      y1={cy - 13.5}
                      x2="252"
                      y2={cy - 13.5}
                      stroke={C.cardBorder}
                      strokeWidth="1"
                    />
                  )}

                  {/* rank index */}
                  <text
                    x="24"
                    y={cy + 3}
                    textAnchor="end"
                    fontSize="8.5"
                    fill={C.navLabel}
                  >
                    {`#${row.rank}`}
                  </text>

                  {/* operation name: the #1 row is the lone cream strong token */}
                  <text
                    x="30"
                    y={cy + 3.5}
                    fontSize={isHero ? "10.5" : "9.5"}
                    fontWeight={isHero ? 600 : 400}
                    fill={isHero ? C.heading : C.ink}
                  >
                    {row.operation}
                  </text>

                  {/* dominant response class: coral only for the firing op */}
                  <text
                    x={STATUS_X}
                    y={cy + 3}
                    textAnchor="end"
                    fontSize="8.5"
                    fontWeight={row.firing ? 600 : 400}
                    fill={row.firing ? C.coral : C.navLabel}
                    style={{ fontVariantNumeric: "tabular-nums" }}
                  >
                    {row.status}
                  </text>

                  {/* monochrome impact bar: track + cc-ink fill */}
                  <rect
                    x={BAR_X}
                    y={cy - 2}
                    width={BAR_W}
                    height="4"
                    rx="2"
                    fill={C.cardBorder}
                  />
                  <rect
                    x={BAR_X}
                    y={cy - 2}
                    width={fillW}
                    height="4"
                    rx="2"
                    fill={C.ink}
                  />

                  {/* impact score: the hero figure is the coral token */}
                  <text
                    x={VAL_X}
                    y={cy + 3}
                    textAnchor="end"
                    fontSize="9"
                    fontWeight={isHero ? 600 : 400}
                    fill={isHero ? C.coral : C.ink}
                    style={{ fontVariantNumeric: "tabular-nums" }}
                  >
                    {row.impact}
                  </text>
                </g>
              );
            })}

            {/* ---- The single coral callout on the #1 impact score ---- */}
            <circle cx="219" cy={ROW_Y0} r="2.5" fill={C.coral} />
            <line
              x1="225"
              y1={ROW_Y0 + 5}
              x2="240"
              y2={ROW_Y0 + 5}
              stroke={C.coral}
              strokeWidth="2"
            />
            <path
              d={`M240 ${ROW_Y0 - 1} C 249 ${ROW_Y0 - 2}, 253 ${ROW_Y0 - 4}, 258 ${ROW_Y0 - 4}`}
              fill="none"
              stroke={C.coral}
              strokeWidth="1"
              markerEnd={`url(#${ID}head)`}
            />
            <text
              x="262"
              y={ROW_Y0 - 1}
              fontSize="6.5"
              letterSpacing="0.08em"
              fill={C.coral}
            >
              #1 IMPACT
            </text>
          </svg>
        </div>

        {/* Stat duo footer: the leaderboard's verdict, no hand-built dashboard. */}
        <div className="border-cc-card-border mt-4 grid grid-cols-2 gap-4 border-t pt-4">
          <div>
            <p className="font-heading text-cc-heading text-h4 leading-none font-semibold">
              #1
            </p>
            <p className="text-cc-ink-dim mt-1.5 text-xs">impact: checkout</p>
          </div>
          <div>
            <p className="font-heading text-cc-heading text-h4 leading-none font-semibold">
              5xx
            </p>
            <p className="text-cc-ink-dim mt-1.5 text-xs">the only firing op</p>
          </div>
        </div>
      </div>
    </div>
  );
}
