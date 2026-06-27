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
 * Agentic-coding scene (v4 "Generated Artifacts", concept 2): the `/graphql/mcp`
 * tool catalog rendered as one flat registry tile, the locked v4 PATTERN D
 * (a list of compact rows, one teal-flagged, one status row).
 *
 * Each published operation a coding agent can call is a thin row: a leading dot,
 * the mono operation name, a query / mutation kind tag, and one behavior-hint
 * badge. Content is borrowed verbatim from the v1 sibling: the same four tools,
 * kinds, and hints, and the "4 tools" count. The badges read monochrome so the
 * catalog stays neutral code; the single status channel is coral, flagging the
 * one destructive op (`cancelOrder`) the agent must gate.
 *
 * The lone teal callout sits on the load-bearing token, the `readOnlyHint` on
 * `products`: the hint that tells the agent the tool is safe to call unattended.
 * It carries the full four-part signature (teal token text, 3px anchor dot, 1px
 * leader with a chevron, 2px underline tick, "SAFE TO CALL" micro-label). Strip
 * the teal and the tile is a monochrome registry with one coral status flag.
 * Teal (top row) and coral (bottom row) never share an element. Settled final
 * frame, no hooks, no animation; every svg id is prefixed "v4-feedback-2-".
 */

const C = {
  surface: "#0c1322",
  cardBorder: "rgba(245,241,234,0.12)",
  inkFaint: "rgba(245,241,234,0.16)",
  ink: "#a1a3af",
  inkDim: "rgba(245,241,234,0.62)",
  navLabel: "#62748e",
  heading: "#f5f0ea",
  accent: "#5eead4",
  coral: "#f0786a",
  mono: 'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace',
} as const;

const ID = "v4-feedback-2-";

/** The four operations the v1 list exposes, borrowed verbatim. */
const ROWS: readonly ToolRow[] = [
  { name: "products", kind: "query", behavior: "readOnlyHint" },
  { name: "addToCart", kind: "mutation", behavior: "idempotentHint" },
  { name: "placeOrder", kind: "mutation", behavior: "idempotentHint" },
  { name: "cancelOrder", kind: "mutation", behavior: "destructiveHint" },
];

const ROW_TOP = 46;
const ROW_H = 30;
const RIGHT = 268;

export function FeedbackVariant2({ className }: FeedbackVariant2Props) {
  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      aria-hidden="true"
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          mcp tool catalog
        </p>

        <div className="mt-3">
          <svg
            viewBox="0 0 280 178"
            width="100%"
            style={{ display: "block", fontFamily: C.mono }}
          >
            <defs>
              {/* Teal open chevron for the single callout leader. */}
              <marker
                id={`${ID}chevron`}
                markerWidth="6"
                markerHeight="6"
                refX="3"
                refY="3"
                orient="auto"
                markerUnits="userSpaceOnUse"
              >
                <path
                  d="M0.5 0.5 L4 3 L0.5 5.5"
                  fill="none"
                  stroke={C.accent}
                  strokeWidth="1"
                />
              </marker>
            </defs>

            {/* registry tile */}
            <rect
              x={2}
              y={24}
              width={276}
              height={152}
              rx={8}
              fill={C.surface}
              stroke={C.cardBorder}
              strokeWidth={1}
            />

            {/* title bar: endpoint path + exposed-tool count, closed by a divider */}
            <text
              x={12}
              y={40}
              fontSize={10.5}
              fontWeight={600}
              fill={C.inkDim}
            >
              /graphql/mcp
            </text>
            <text
              x={266}
              y={40}
              fontSize={9}
              letterSpacing="0.05em"
              textAnchor="end"
              fill={C.navLabel}
            >
              4 tools
            </text>
            <line
              x1={8}
              y1={46}
              x2={272}
              y2={46}
              stroke={C.cardBorder}
              strokeWidth={1}
            />

            {/* catalog rows: dot, name, kind tag, behavior-hint badge */}
            {ROWS.map((row, i) => {
              const cy = ROW_TOP + ROW_H / 2 + ROW_H * i;
              const destructive = row.behavior === "destructiveHint";
              const teal = i === 0;
              const bw = row.behavior.length * 4.9 + 14;
              const bx = RIGHT - bw;
              const badgeText = destructive
                ? C.coral
                : teal
                  ? C.accent
                  : C.inkDim;
              return (
                <g key={row.name}>
                  {i > 0 && (
                    <line
                      x1={12}
                      y1={ROW_TOP + ROW_H * i}
                      x2={268}
                      y2={ROW_TOP + ROW_H * i}
                      stroke={C.cardBorder}
                      strokeWidth={1}
                    />
                  )}
                  <circle
                    cx={18}
                    cy={cy}
                    r={destructive ? 2.8 : 2}
                    fill={destructive ? C.coral : C.inkFaint}
                  />
                  <text x={30} y={cy + 3.5} fontSize={11} fill={C.ink}>
                    {row.name}
                  </text>
                  <text
                    x={150}
                    y={cy + 3}
                    fontSize={9}
                    textAnchor="end"
                    fill={C.navLabel}
                  >
                    {row.kind}
                  </text>
                  <rect
                    x={bx}
                    y={cy - 7}
                    width={bw}
                    height={14}
                    rx={7}
                    fill={destructive ? C.coral : "none"}
                    fillOpacity={destructive ? 0.08 : undefined}
                    stroke={destructive ? C.coral : C.cardBorder}
                    strokeWidth={1}
                  />
                  <text
                    x={bx + bw / 2}
                    y={cy + 2.7}
                    fontSize={8}
                    textAnchor="middle"
                    fill={badgeText}
                  >
                    {row.behavior}
                  </text>
                </g>
              );
            })}

            {/* the single teal callout on the readOnlyHint token (row 0 badge) */}
            <text
              x={170}
              y={13}
              fontSize={7}
              letterSpacing="0.12em"
              textAnchor="middle"
              fill={C.accent}
            >
              SAFE TO CALL
            </text>
            <path
              d="M206 16 C 220 28, 229 40, 231.6 49"
              fill="none"
              stroke={C.accent}
              strokeWidth={1}
              markerEnd={`url(#${ID}chevron)`}
            />
            <circle cx={231.6} cy={53} r={3} fill={C.accent} />
            <line
              x1={206}
              y1={70}
              x2={257}
              y2={70}
              stroke={C.accent}
              strokeWidth={2}
            />
          </svg>
        </div>

        {/* Stat duo footer: the exposed surface and the one op needing a gate. */}
        <div className="border-cc-card-border mt-4 grid grid-cols-2 gap-4 border-t pt-4">
          <Stat figure="4" label="exposed tools" />
          <Stat figure="1" label="needs a gate" />
        </div>
      </div>
    </div>
  );
}

interface StatProps {
  readonly figure: string;
  readonly label: string;
}

/** The ScrollScenes Stat: a display numeral over a small dim caption. */
function Stat({ figure, label }: StatProps) {
  return (
    <div>
      <p className="font-heading text-cc-heading text-h4 leading-none font-semibold">
        {figure}
      </p>
      <p className="text-cc-ink-dim mt-1.5 text-xs">{label}</p>
    </div>
  );
}
