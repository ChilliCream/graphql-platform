interface WorkflowsVariant2Props {
  readonly className?: string;
}

/**
 * Workflow scene, v4 "Generated Artifacts", concept #2: the ReviewSaga state
 * machine (locked v4 PATTERN A, one cc-surface artifact tile + a single teal
 * callout).
 *
 * One full-width tile is the source-generated `ReviewSaga` (the cream title is the
 * tile's one strong token). A vertical rail threads three state rows: `Draft`
 * (Start) and `Checked` are settled (grey check nodes, their emitted
 * `ReviewDrafted` / `ReviewChecked` events dimmed on the right), and `Published`
 * (End) is the not-yet-reached node, drawn as the dashed / dim "not reached" disc
 * the saga is currently advancing into.
 *
 * The signature teal callout is the only accent in the cell: the in-flight
 * `ChecksPassed` event riding the rail between Checked and Published is the single
 * load-bearing token (cc-accent), with a 3px teal anchor dot on the rail, a 2px
 * teal underline tick, a 1px teal leader out to the margin, and an "IN FLIGHT"
 * micro-label. Strip the teal and the saga reads as a neutral grey state list;
 * there is no second status hue competing with it.
 *
 * Literal content (ReviewSaga, Draft/Checked/Published, Start/End, ReviewDrafted,
 * ReviewChecked, ReviewPublished, the in-flight ChecksPassed event) is borrowed
 * verbatim from the v1 / v2 siblings. React Server Component, settled final frame,
 * no motion. Every svg id is prefixed "v4-workflows-2-".
 */

const C = {
  surface: "#0c1322",
  cardBorder: "rgba(245, 241, 234, 0.12)",
  inkFaint: "rgba(245, 241, 234, 0.16)",
  ink: "#a1a3af",
  inkDim: "rgba(245, 241, 234, 0.62)",
  navLabel: "#62748e",
  heading: "#f5f0ea",
  accent: "#5eead4",
  mono: 'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace',
} as const;

const ID = "v4-workflows-2-";

// Rail and row geometry (svg userspace).
const RAIL_X = 24;
const Y1 = 54; // Draft     (settled)
const Y2 = 91; // Checked   (settled)
const Y3 = 128; // Published (not reached)
const Y_EVENT = 110; // the in-flight transition, mid-rail between Checked and Published

/** Settled "done" node: a cc-surface disc on the rail with a dim grey check. */
function DoneNode({ cy }: { readonly cy: number }) {
  return (
    <g>
      <circle
        cx={RAIL_X}
        cy={cy}
        r={5.5}
        fill={C.surface}
        stroke={C.cardBorder}
        strokeWidth={1}
      />
      <path
        d={`M${RAIL_X - 3.2} ${cy} L${RAIL_X - 0.8} ${cy + 2.2} L${RAIL_X + 3} ${cy - 2.6}`}
        fill="none"
        stroke={C.inkDim}
        strokeWidth={1.3}
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </g>
  );
}

/** Right-aligned emitted-event label for a settled state row. */
function Emitted({ y, name }: { readonly y: number; readonly name: string }) {
  return (
    <text
      x={300}
      y={y + 3.5}
      textAnchor="end"
      fontFamily={C.mono}
      fontSize={8.5}
    >
      <tspan fill={C.navLabel}>&#8594;</tspan>
      <tspan dx="4" fill={C.inkDim}>
        {name}
      </tspan>
    </text>
  );
}

export function WorkflowsVariant2({ className }: WorkflowsVariant2Props) {
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
          saga state machine
        </p>

        <div className="mt-4">
          <svg
            viewBox="0 0 320 148"
            width="100%"
            role="img"
            aria-label="ReviewSaga state machine: Draft and Checked settled, Published not yet reached, with the saga processing the in-flight ChecksPassed transition into Published."
            style={{ display: "block" }}
          >
            <defs>
              {/* Teal open chevron for the single callout leader. */}
              <marker
                id={`${ID}headTeal`}
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
                  stroke={C.accent}
                  strokeWidth="1"
                />
              </marker>
            </defs>

            {/* ---- Hero tile: the generated ReviewSaga state machine ---- */}
            <rect
              x={8}
              y={2}
              width={304}
              height={144}
              rx={8}
              fill={C.surface}
              stroke={C.cardBorder}
              strokeWidth={1}
            />

            {/* Title bar: saga name (the one cream token) + kind tag, then divider. */}
            <text
              x={20}
              y={19}
              fontFamily={C.mono}
              fontSize={11}
              fontWeight={600}
              fill={C.heading}
            >
              ReviewSaga
            </text>
            <text
              x={300}
              y={19}
              textAnchor="end"
              fontFamily={C.mono}
              fontSize={8}
              fill={C.navLabel}
            >
              saga
            </text>
            <line
              x1={8}
              y1={30}
              x2={312}
              y2={30}
              stroke={C.cardBorder}
              strokeWidth={1}
            />

            {/* Vertical rail threading the three states. */}
            <line
              x1={RAIL_X}
              y1={Y1}
              x2={RAIL_X}
              y2={Y3}
              stroke={C.cardBorder}
              strokeWidth={1}
            />

            {/* Row 1: Draft (Start, settled) -> emitted ReviewDrafted. */}
            <DoneNode cy={Y1} />
            <text
              x={38}
              y={Y1 + 3.5}
              fontFamily={C.mono}
              fontSize={10.5}
              fontWeight={600}
              fill={C.ink}
            >
              Draft
            </text>
            <rect
              x={78}
              y={Y1 - 5}
              width={30}
              height={12}
              rx={3}
              fill="none"
              stroke={C.cardBorder}
              strokeWidth={1}
            />
            <text
              x={93}
              y={Y1 + 3}
              textAnchor="middle"
              fontFamily={C.mono}
              fontSize={7}
              letterSpacing="0.08em"
              fill={C.navLabel}
            >
              START
            </text>
            <Emitted y={Y1} name="ReviewDrafted" />

            {/* Row 2: Checked (settled) -> emitted ReviewChecked. */}
            <DoneNode cy={Y2} />
            <text
              x={38}
              y={Y2 + 3.5}
              fontFamily={C.mono}
              fontSize={10.5}
              fontWeight={600}
              fill={C.ink}
            >
              Checked
            </text>
            <Emitted y={Y2} name="ReviewChecked" />

            {/* ---- Signature teal callout: the one in-flight transition event ---- */}
            <circle cx={RAIL_X} cy={Y_EVENT} r={3} fill={C.accent} />
            <text
              x={38}
              y={Y_EVENT + 3}
              fontFamily={C.mono}
              fontSize={9.5}
              fontWeight={600}
              fill={C.accent}
            >
              ChecksPassed
            </text>
            <line
              x1={38}
              y1={Y_EVENT + 6.5}
              x2={104}
              y2={Y_EVENT + 6.5}
              stroke={C.accent}
              strokeWidth={2}
            />
            <path
              d={`M108 ${Y_EVENT + 1} C 150 ${Y_EVENT - 1}, 198 ${Y_EVENT - 2}, 226 ${Y_EVENT - 2}`}
              fill="none"
              stroke={C.accent}
              strokeWidth={1}
              markerEnd={`url(#${ID}headTeal)`}
            />
            <text
              x={234}
              y={Y_EVENT + 1}
              fontFamily={C.mono}
              fontSize={7}
              letterSpacing="0.12em"
              fill={C.accent}
            >
              IN FLIGHT
            </text>

            {/* Row 3: Published (End) -> the not-yet-reached node the saga is
                advancing into; its ReviewPublished event is still pending. */}
            <circle
              cx={RAIL_X}
              cy={Y3}
              r={5.5}
              fill={C.surface}
              stroke={C.inkFaint}
              strokeWidth={1}
              strokeDasharray="2 2"
            />
            <text
              x={38}
              y={Y3 + 3.5}
              fontFamily={C.mono}
              fontSize={10.5}
              fontWeight={600}
              fill={C.inkDim}
            >
              Published
            </text>
            <rect
              x={113}
              y={Y3 - 5}
              width={26}
              height={12}
              rx={3}
              fill="none"
              stroke={C.cardBorder}
              strokeWidth={1}
            />
            <text
              x={126}
              y={Y3 + 3}
              textAnchor="middle"
              fontFamily={C.mono}
              fontSize={7}
              letterSpacing="0.08em"
              fill={C.navLabel}
            >
              END
            </text>
            <Emitted y={Y3} name="ReviewPublished" />
          </svg>
        </div>

        {/* Stat duo footer: states settled, transitions in flight. */}
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
