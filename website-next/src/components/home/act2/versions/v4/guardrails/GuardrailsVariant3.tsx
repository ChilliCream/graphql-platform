interface GuardrailsVariant3Props {
  readonly className?: string;
}

/**
 * "Release safety" scene, v4 "Generated Artifacts", variant 3: the published-
 * client impact report Nitro emits for a breaking change, ranked by readiness.
 *
 * Locked v4 PATTERN A (one cc-surface artifact tile + a single callout). The
 * tile is the impact report for the change `checkout-v3`: a title bar (change id
 * + `impact` kind tag) over three registered clients, each carrying its name, a
 * monochrome readiness bar reading the share of its operations that still
 * validate, and the n/total tally. Bar geometry alone separates the three
 * states: a full cc-ink bar is ready, a partial bar is at risk, a dashed track
 * is queued, so the list stays neutral with no status-color rainbow.
 *
 * The load-bearing token is the iOS client the change puts at risk, and an
 * at-risk client is a real status, so per the signature rule the status hue owns
 * the single accent cluster and teal steps aside entirely: amber marks the iOS
 * row's readiness fill, its `3/5` tally, and the signature callout (2.5px anchor
 * dot, 2px underline tick, 1px leader into the right gutter, "AT-RISK" micro-
 * label) pinned to it. The lone cream strong token is the `iOS` name. Strip the
 * amber and a neutral grey readiness list remains; only one status family is
 * colored, so nothing collides with the callout.
 *
 * Literal content (registered clients Web 5/5, iOS 3/5, Partner 0/4, change
 * checkout-v3) is borrowed verbatim from the v1 sibling. React Server Component:
 * no "use client", no hooks, no animation, settled final frame. Every svg id is
 * prefixed "v4-guardrails-3-".
 */

const C = {
  surface: "#0c1322",
  cardBorder: "rgba(245,241,234,0.12)",
  trackFill: "rgba(245,241,234,0.06)",
  ink: "#a1a3af",
  inkDim: "rgba(245,241,234,0.62)",
  navLabel: "#62748e",
  heading: "#f5f0ea",
  // Frozen palette keeps teal for set-wide coherence; an at-risk client is a real
  // status, so amber owns the single accent cluster and teal is unused here.
  accent: "#5eead4",
  amber: "#fbbf24",
  mono: 'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace',
} as const;

const ID = "v4-guardrails-3-";

type ClientState = "ready" | "at-risk" | "queued";

interface ClientRow {
  readonly name: string;
  /** Operations that still validate over total registered, verbatim from v1. */
  readonly frac: string;
  /** Readiness bar width in userspace px out of the BAR_W track. */
  readonly fill: number;
  readonly state: ClientState;
}

const CLIENTS: readonly ClientRow[] = [
  { name: "Web", frac: "5/5", fill: 104, state: "ready" },
  { name: "iOS", frac: "3/5", fill: 62, state: "at-risk" },
  { name: "Partner", frac: "0/4", fill: 0, state: "queued" },
];

// Row geometry: first row center, then a constant vertical step.
const ROW_Y0 = 62;
const ROW_STEP = 33;
const BAR_X = 110;
const BAR_W = 104;
const VAL_X = 244;

export function GuardrailsVariant3({ className }: GuardrailsVariant3Props) {
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
          published-client impact
        </p>

        <div className="mt-4">
          <svg
            viewBox="0 0 320 150"
            width="100%"
            role="img"
            aria-label="Impact report for change checkout-v3: Web 5 of 5 operations still validate, iOS 3 of 5 is at risk, Partner 0 of 4 is queued."
            style={{ display: "block", fontFamily: C.mono }}
          >
            <defs>
              {/* Open amber chevron for the single status callout leader. */}
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
                  stroke={C.amber}
                  strokeWidth="1"
                />
              </marker>
            </defs>

            {/* ---- Artifact tile: the emitted impact report ---- */}
            <rect
              x="6"
              y="4"
              width="250"
              height="142"
              rx="8"
              fill={C.surface}
              stroke={C.cardBorder}
              strokeWidth="1"
            />

            {/* title bar: change id + impact kind tag */}
            <text x="16" y="18" fontSize="9" fill={C.inkDim}>
              checkout-v3
            </text>
            <text
              x="244"
              y="18"
              textAnchor="end"
              fontSize="8"
              letterSpacing="0.1em"
              fill={C.navLabel}
            >
              impact
            </text>
            <line
              x1="6"
              y1="26"
              x2="256"
              y2="26"
              stroke={C.cardBorder}
              strokeWidth="1"
            />

            {/* column header labels */}
            <text
              x="28"
              y="40"
              fontSize="6.5"
              letterSpacing="0.08em"
              fill={C.navLabel}
            >
              CLIENT
            </text>
            <text
              x={BAR_X}
              y="40"
              fontSize="6.5"
              letterSpacing="0.08em"
              fill={C.navLabel}
            >
              READINESS
            </text>
            <text
              x={VAL_X}
              y="40"
              textAnchor="end"
              fontSize="6.5"
              letterSpacing="0.08em"
              fill={C.navLabel}
            >
              OPS OK
            </text>
            <line
              x1="6"
              y1="45"
              x2="256"
              y2="45"
              stroke={C.cardBorder}
              strokeWidth="1"
            />

            {/* ---- One row per registered client ---- */}
            {CLIENTS.map((client, i) => {
              const cy = ROW_Y0 + i * ROW_STEP;
              const atRisk = client.state === "at-risk";
              const queued = client.state === "queued";
              const nameFill = atRisk ? C.heading : queued ? C.inkDim : C.ink;
              const valFill = atRisk ? C.amber : queued ? C.inkDim : C.ink;
              return (
                <g key={client.name}>
                  {/* row separator above each row past the first */}
                  {i > 0 && (
                    <line
                      x1="6"
                      y1={cy - 16.5}
                      x2="256"
                      y2={cy - 16.5}
                      stroke={C.cardBorder}
                      strokeWidth="1"
                    />
                  )}

                  {/* leading neutral registry dot */}
                  <circle cx="18" cy={cy} r="2.5" fill={C.navLabel} />

                  {/* client name: the at-risk client is the lone cream token */}
                  <text
                    x="28"
                    y={cy + 3.5}
                    fontSize={atRisk ? "10.5" : "9.5"}
                    fontWeight={atRisk ? 600 : 400}
                    fill={nameFill}
                  >
                    {client.name}
                  </text>

                  {/* readiness track */}
                  <rect
                    x={BAR_X}
                    y={cy - 2}
                    width={BAR_W}
                    height="4"
                    rx="2"
                    fill={C.trackFill}
                  />
                  {queued ? (
                    <line
                      x1={BAR_X + 2}
                      y1={cy}
                      x2={BAR_X + BAR_W - 2}
                      y2={cy}
                      stroke={C.navLabel}
                      strokeWidth="1"
                      strokeDasharray="3 3"
                    />
                  ) : (
                    <rect
                      x={BAR_X}
                      y={cy - 2}
                      width={client.fill}
                      height="4"
                      rx="2"
                      fill={atRisk ? C.amber : C.ink}
                    />
                  )}

                  {/* operation tally: the at-risk row's figure is the amber token */}
                  <text
                    x={VAL_X}
                    y={cy + 3}
                    textAnchor="end"
                    fontSize="9"
                    fontWeight={atRisk ? 600 : 400}
                    fill={valFill}
                    style={{ fontVariantNumeric: "tabular-nums" }}
                  >
                    {client.frac}
                  </text>
                </g>
              );
            })}

            {/* ---- The single amber status callout on the at-risk iOS row ---- */}
            <circle cx="220" cy={ROW_Y0 + ROW_STEP} r="2.5" fill={C.amber} />
            <line
              x1="226"
              y1={ROW_Y0 + ROW_STEP + 5}
              x2="244"
              y2={ROW_Y0 + ROW_STEP + 5}
              stroke={C.amber}
              strokeWidth="2"
            />
            <path
              d={`M244 ${ROW_Y0 + ROW_STEP - 1} C 250 ${ROW_Y0 + ROW_STEP - 2}, 252 ${ROW_Y0 + ROW_STEP - 4}, 256 ${ROW_Y0 + ROW_STEP - 4}`}
              fill="none"
              stroke={C.amber}
              strokeWidth="1"
              markerEnd={`url(#${ID}head)`}
            />
            <text
              x="260"
              y={ROW_Y0 + ROW_STEP - 1}
              fontSize="6.5"
              letterSpacing="0.08em"
              fill={C.amber}
            >
              AT-RISK
            </text>
          </svg>
        </div>

        {/* Stat duo footer: the report's verdict, in heading numerals. */}
        <div className="border-cc-card-border mt-4 grid grid-cols-2 gap-4 border-t pt-4">
          <div>
            <p className="font-heading text-cc-heading text-h4 leading-none font-semibold">
              3
            </p>
            <p className="text-cc-ink-dim mt-1.5 text-xs">clients checked</p>
          </div>
          <div>
            <p className="font-heading text-cc-heading text-h4 leading-none font-semibold">
              1
            </p>
            <p className="text-cc-ink-dim mt-1.5 text-xs">at risk</p>
          </div>
        </div>
      </div>
    </div>
  );
}
