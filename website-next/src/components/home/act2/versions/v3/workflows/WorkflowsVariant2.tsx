/**
 * "Workflow" scene, concept #2 ("Saga state machine"), v3 "Signal & Metrics"
 * (strict cc-* dark) take.
 *
 * Leads with the saga's honest progress metric: the ReviewSaga has settled 2 of
 * its 3 states, so the hero is the cream "2 / 3" numeral over the lowercase mono
 * caption "states settled". The teal signal (layout B) is the state machine drawn
 * as a node chain, Draft -> Checked -> Published. Draft is the traversed grey
 * state, Checked is the current state and carries the single filled teal node
 * (the "this is the reading" glyph), and Published is the pending destination.
 *
 * Published is the one element with genuine status: the saga is processing the
 * long-running transition into it on the live ChecksPassed event, so amber (the
 * in-flight hue) rides only that dashed edge, the destination marker, and the
 * IN-FLIGHT tag. Teal stays bound to the current state; the hero numeral stays
 * cream. Content is faithful to the proven v2 sibling: Draft -> Checked ->
 * Published, with Published entered via the in-flight ChecksPassed event.
 *
 * Static settled frame: a React Server Component, no hooks, no motion, no
 * "use client". aria-hidden root. All svg ids prefixed "v3-workflows-2-".
 */

interface WorkflowsVariant2Props {
  readonly className?: string;
}

/* Strict cc-* dark palette, mirrored locally per the v3 system. Teal is the only
 * decorative hue and is bound to the current saga state; amber encodes the one
 * real in-flight transition the long-running saga is processing into Published. */
const cc = {
  surface: "#0c1322",
  cardBorder: "rgba(245,241,234,0.12)",
  inkFaint: "rgba(245,241,234,0.16)",
  heading: "#f5f0ea",
  ink: "#a1a3af",
  inkDim: "rgba(245,241,234,0.62)",
  navLabel: "#62748e",
  teal: "#5eead4",
  amber: "#fbbf24",
} as const;

const MONO =
  'ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, "Liberation Mono", monospace';

interface StateNode {
  readonly label: string;
  /** "done" = traversed (grey), "current" = the teal reading, "pending" = the
   * in-flight destination (amber status). */
  readonly kind: "done" | "current" | "pending";
  /** Node center x in the 264-wide viewBox. */
  readonly x: number;
}

/* Draft and Checked have settled; Published is the pending state the live
 * ChecksPassed event is advancing the saga into. */
const STATES: readonly StateNode[] = [
  { label: "Draft", kind: "done", x: 40 },
  { label: "Checked", kind: "current", x: 132 },
  { label: "Published", kind: "pending", x: 224 },
];

export function WorkflowsVariant2({ className }: WorkflowsVariant2Props) {
  const nodeY = 18;
  const labelY = 42;
  const r = 7;

  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      aria-hidden="true"
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        {/* eyebrow + the saga under view */}
        <div className="flex items-baseline justify-between gap-3">
          <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
            saga state machine
          </p>
          <span
            className="shrink-0 font-mono text-[0.55rem]"
            style={{ color: cc.navLabel }}
          >
            ReviewSaga
          </span>
        </div>

        {/* HERO: how far the long-running saga has advanced, the honest metric,
            with the one status hue tagging the in-flight transition */}
        <div className="mt-3 flex items-end justify-between gap-3">
          <div>
            <p
              className="text-cc-heading font-heading leading-none font-semibold"
              style={{
                fontSize: "2.25rem",
                fontVariantNumeric: "tabular-nums",
              }}
            >
              2
              <span
                className="font-heading"
                style={{ fontSize: "1.25rem", color: cc.navLabel }}
              >
                {" "}
                / 3
              </span>
            </p>
            <p className="text-cc-ink-dim mt-2 font-mono text-[0.7rem] lowercase">
              states settled
            </p>
          </div>

          <span
            className="rounded-md border px-2 py-1 font-mono uppercase"
            style={{
              borderColor: `${cc.amber}66`,
              fontSize: "0.5rem",
              letterSpacing: "0.08em",
              color: cc.amber,
            }}
          >
            in-flight
          </span>
        </div>

        {/* TEAL SIGNAL: the saga state machine as a node chain. Draft is the
            traversed grey state, Checked is the current state and holds the single
            teal node, Published is the pending destination the saga is advancing
            into on the live (amber) ChecksPassed transition. */}
        <svg
          viewBox="0 0 264 50"
          width="100%"
          aria-hidden="true"
          className="mt-4"
          style={{ display: "block" }}
        >
          {/* traversed edge: Draft -> Checked, solid grey with a grey arrow */}
          <line
            x1={40 + r}
            y1={nodeY}
            x2={132 - r - 4}
            y2={nodeY}
            stroke={cc.ink}
            strokeWidth="1"
            opacity="0.55"
          />
          <path
            d={`M${132 - r - 7} ${nodeY - 3} L${132 - r - 3} ${nodeY} L${132 - r - 7} ${nodeY + 3}`}
            fill="none"
            stroke={cc.ink}
            strokeWidth="1"
            strokeLinecap="round"
            strokeLinejoin="round"
            opacity="0.55"
          />

          {/* in-flight edge: Checked -> Published, dashed grey with an amber
              packet mid-flight and an amber arrow into the destination */}
          <line
            x1={132 + r}
            y1={nodeY}
            x2={224 - r - 4}
            y2={nodeY}
            stroke={cc.inkFaint}
            strokeWidth="1"
            strokeDasharray="2 3"
          />
          <path
            d={`M178 ${nodeY - 4} L182 ${nodeY} L178 ${nodeY + 4} L174 ${nodeY} Z`}
            fill={cc.amber}
          />
          <path
            d={`M${224 - r - 7} ${nodeY - 3} L${224 - r - 3} ${nodeY} L${224 - r - 7} ${nodeY + 3}`}
            fill="none"
            stroke={cc.amber}
            strokeWidth="1"
            strokeLinecap="round"
            strokeLinejoin="round"
          />

          {STATES.map((s) => (
            <g key={`v3-workflows-2-${s.label}`}>
              {s.kind === "done" && (
                <>
                  <circle
                    cx={s.x}
                    cy={nodeY}
                    r={r}
                    fill={cc.surface}
                    stroke={cc.ink}
                    strokeWidth="1"
                    opacity="0.7"
                  />
                  <path
                    d={`M${s.x - 3} ${nodeY} L${s.x - 0.6} ${nodeY + 2.4} L${s.x + 3.2} ${nodeY - 2.6}`}
                    fill="none"
                    stroke={cc.ink}
                    strokeWidth="1.2"
                    strokeLinecap="round"
                    strokeLinejoin="round"
                  />
                </>
              )}

              {s.kind === "current" && (
                <>
                  <circle
                    cx={s.x}
                    cy={nodeY}
                    r={r}
                    fill={cc.surface}
                    stroke={cc.teal}
                    strokeWidth="1"
                  />
                  {/* the single filled teal node: the saga's current reading */}
                  <circle cx={s.x} cy={nodeY} r="3" fill={cc.teal} />
                </>
              )}

              {s.kind === "pending" && (
                <>
                  <circle
                    cx={s.x}
                    cy={nodeY}
                    r={r}
                    fill={cc.surface}
                    stroke={cc.inkFaint}
                    strokeWidth="1"
                    strokeDasharray="2 2"
                  />
                  <circle cx={s.x} cy={nodeY} r="2.4" fill={cc.amber} />
                </>
              )}

              <text
                x={s.x}
                y={labelY}
                textAnchor="middle"
                fontFamily={MONO}
                fontSize="8"
                letterSpacing="0.04em"
                fill={s.kind === "current" ? cc.ink : cc.navLabel}
              >
                {s.label}
              </text>
            </g>
          ))}
        </svg>

        {/* interpretation caption under a dashed divider: the live transition */}
        <div className="border-cc-ink-faint mt-4 border-t border-dashed pt-3">
          <p
            className="flex items-center gap-1.5 font-mono text-[0.62rem]"
            style={{ color: cc.inkDim }}
          >
            <span
              aria-hidden="true"
              className="rounded-full"
              style={{
                width: 5,
                height: 5,
                flex: "0 0 auto",
                background: cc.amber,
              }}
            />
            <span style={{ color: cc.ink }}>ChecksPassed</span>
            <span>&middot; advancing into Published</span>
          </p>
        </div>
      </div>
    </div>
  );
}
