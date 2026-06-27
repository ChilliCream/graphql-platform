interface WorkflowsVariant2Props {
  readonly className?: string;
}

/**
 * Workflow scene, v2 "Flow Diagrams", variant 2: saga state machine.
 *
 * Pipeline topology in the locked cc-* flow-diagram system. The ReviewSaga drawn
 * left-to-right as three state nodes joined by 1px connectors: Draft and Checked
 * are settled (cream label, grey ink, solid grey traversed edges); Published is
 * not-yet-reached, drawn as the dashed-border / dim-ink "not reached" node. The
 * single teal path is the one in-flight transition the saga is processing now:
 * the Checked -> Published hop, traced teal and carrying its live `ChecksPassed`
 * event chip (the active teal node of the card). Long-running work advances
 * through states; only the segment currently moving is teal. A Stat duo footer
 * carries the two key numbers (states settled, the in-flight event count).
 *
 * React Server Component: no hooks, no client APIs, settled final frame only.
 * All svg ids are prefixed "v2-workflows-2-".
 */

const CC = {
  surface: "#0c1322",
  ink: "#a1a3af",
  inkDim: "rgba(245,241,234,0.62)",
  inkFaint: "rgba(245,241,234,0.16)",
  cardBorder: "rgba(245,241,234,0.12)",
  navLabel: "#62748e",
  accent: "#5eead4",
} as const;

const MONO =
  'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace';

const ID = "v2-workflows-2-";

interface SagaState {
  /** Mono identifier rendered as the node label. */
  readonly label: string;
  /** Event message emitted as this state settles. */
  readonly emitted: string;
  /** START / END marker shown above terminal states. */
  readonly badge?: "START" | "END";
  /** The not-yet-reached state the in-flight transition is entering. */
  readonly pending?: boolean;
}

// Geometry of the three-state pipeline across the 320x150 canvas. Equal node
// boxes, equal gaps, so the strip reads as one ordered flow left-to-right.
const NODE_W = 86;
const NODE_H = 42;
const NODE_Y = 58;
const NODE_X = [12, 117, 222] as const;
const CY = NODE_Y + NODE_H / 2;

// Draft and Checked are settled; Published is the dashed not-reached node the
// live ChecksPassed transition is entering. That single hop is the teal path.
const STATES: readonly SagaState[] = [
  { label: "Draft", emitted: "ReviewDrafted", badge: "START" },
  { label: "Checked", emitted: "ReviewChecked" },
  {
    label: "Published",
    emitted: "ReviewPublished",
    badge: "END",
    pending: true,
  },
];

export function WorkflowsVariant2({ className }: WorkflowsVariant2Props) {
  // Connector spans between the trailing edge of one node and the leading edge
  // of the next, leaving a small gap for the open arrowhead.
  function edge(i: number) {
    return { x0: NODE_X[i] + NODE_W, x1: NODE_X[i + 1] };
  }

  const settled = edge(0); // Draft -> Checked, traversed (grey)
  const inflight = edge(1); // Checked -> Published, in flight (teal)
  const eventCx = (inflight.x0 + inflight.x1) / 2;

  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          ReviewSaga
        </p>

        <div className="mt-3">
          <svg
            viewBox="0 0 320 150"
            width="100%"
            role="img"
            aria-label="The ReviewSaga state machine advancing left to right: Draft and Checked are settled, Published is not yet reached, and the saga is processing the in-flight ChecksPassed transition into Published."
            style={{ display: "block" }}
          >
            <defs>
              <marker
                id={`${ID}arrow`}
                markerWidth="7"
                markerHeight="7"
                refX="5"
                refY="3"
                orient="auto"
              >
                <path
                  d="M0 0 L5 3 L0 6"
                  fill="none"
                  stroke={CC.inkFaint}
                  strokeWidth={1}
                />
              </marker>
              <marker
                id={`${ID}arrowTeal`}
                markerWidth="7"
                markerHeight="7"
                refX="5"
                refY="3"
                orient="auto"
              >
                <path
                  d="M0 0 L5 3 L0 6"
                  fill="none"
                  stroke={CC.accent}
                  strokeWidth={1}
                />
              </marker>
            </defs>

            {/* Traversed edge: Draft -> Checked, settled grey. */}
            <line
              x1={settled.x0}
              y1={CY}
              x2={settled.x1 - 5}
              y2={CY}
              stroke={CC.inkFaint}
              strokeWidth={1}
              markerEnd={`url(#${ID}arrow)`}
            />

            {/* The single teal path: the in-flight Checked -> Published hop the
                saga is processing now. Dashed to read as not-yet-settled, but
                teal because it is the one segment currently moving. */}
            <line
              x1={inflight.x0}
              y1={CY}
              x2={inflight.x1 - 5}
              y2={CY}
              stroke={CC.accent}
              strokeWidth={1}
              strokeDasharray="3 3"
              markerEnd={`url(#${ID}arrowTeal)`}
            />

            {/* Live ChecksPassed event riding the in-flight edge: the active
                teal chip of the card, pulled up on a short teal leader. */}
            <line
              x1={eventCx}
              y1={CY - 4}
              x2={eventCx}
              y2={32}
              stroke={CC.accent}
              strokeWidth={1}
            />
            <rect
              x={eventCx - 46}
              y={16}
              width={92}
              height={18}
              rx="6"
              fill={CC.surface}
              stroke={CC.accent}
              strokeWidth={1}
            />
            <circle cx={eventCx - 36} cy={25} r="2.5" fill={CC.accent} />
            <text
              x={eventCx - 29}
              y={28.5}
              fill={CC.accent}
              fontFamily={MONO}
              fontSize={9}
              letterSpacing="0.02em"
            >
              ChecksPassed
            </text>

            {/* The three state nodes. */}
            {STATES.map((s, i) => {
              const x = NODE_X[i];
              const y = NODE_Y;
              const stroke = s.pending ? CC.inkFaint : CC.cardBorder;
              const labelColor = s.pending ? CC.inkDim : CC.ink;
              const emittedColor = s.pending ? CC.inkDim : CC.navLabel;
              return (
                <g key={s.label}>
                  {s.badge && (
                    <text
                      x={x + NODE_W - 2}
                      y={y - 6}
                      textAnchor="end"
                      fill={CC.navLabel}
                      fontFamily={MONO}
                      fontSize={6.5}
                      letterSpacing="0.1em"
                    >
                      {s.badge}
                    </text>
                  )}
                  <rect
                    x={x}
                    y={y}
                    width={NODE_W}
                    height={NODE_H}
                    rx="8"
                    fill={CC.surface}
                    stroke={stroke}
                    strokeWidth={1}
                    strokeDasharray={s.pending ? "3 3" : undefined}
                  />
                  <text
                    x={x + 11}
                    y={y + 18}
                    fill={labelColor}
                    fontFamily={MONO}
                    fontSize={11}
                  >
                    {s.label}
                  </text>
                  <text
                    x={x + 11}
                    y={y + 33}
                    fill={emittedColor}
                    fontFamily={MONO}
                    fontSize={7}
                    letterSpacing="0.04em"
                  >
                    {`→ ${s.emitted}`}
                  </text>
                </g>
              );
            })}

            {/* Caption under a dashed divider: the relationship in one line. */}
            <line
              x1={12}
              y1={120}
              x2={308}
              y2={120}
              stroke={CC.inkFaint}
              strokeWidth={1}
              strokeDasharray="4 3"
            />
            <text
              x={160}
              y={137}
              textAnchor="middle"
              fill={CC.inkDim}
              fontFamily={MONO}
              fontSize={9}
              letterSpacing="0.02em"
            >
              long-running work, one state at a time
            </text>
          </svg>
        </div>

        <div className="border-cc-card-border mt-4 grid grid-cols-2 gap-4 border-t pt-4">
          <div>
            <p className="font-heading text-cc-heading text-h4 leading-none font-semibold">
              2/3
            </p>
            <p className="text-cc-ink-dim mt-1.5 text-xs">states settled</p>
          </div>
          <div>
            <p className="font-heading text-cc-heading text-h4 leading-none font-semibold">
              1
            </p>
            <p className="text-cc-ink-dim mt-1.5 text-xs">
              transition in flight
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}
