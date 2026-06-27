interface FeedbackVariant1Props {
  readonly className?: string;
}

/**
 * Agentic-coding scene, variant 1 (v5 "Schematic Lines"): approval-gated agent
 * action.
 *
 * One monoline route, reduced to its essential structure: a hollow teal source
 * ring (the coding agent) threads through a grey createReview node, into a human
 * approval gate drawn as a knife-switch, and out to a solid teal terminal dot
 * (the one safe patch that lands). The teal thread is the single accent: it is
 * the one route the headline names through an otherwise all-grey schematic.
 *
 * The gate carries the state change with no extra colour. The prior PENDING
 * state is the dashed grey blade lifted open above the axis; the settled GRANTED
 * state is the flat teal blade on the axis that completes the thread. A faint
 * strip of registration ticks acts as the diagram baseline. Fully static, no
 * hooks, no animation, settled final frame.
 */

const C = {
  surface: "#0c1322",
  cardBorder: "rgba(245,241,234,0.12)",
  ink: "#a1a3af",
  inkFaint: "rgba(245,241,234,0.16)",
  navLabel: "#62748e",
  accent: "#5eead4",
  mono: 'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace',
} as const;

const ID = "v5-feedback-1-";

export function FeedbackVariant1({ className }: FeedbackVariant1Props) {
  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      aria-hidden="true"
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        {/* Panel eyebrow (ScrollScenes header). */}
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          approval-gated action
        </p>

        {/* Monoline schematic floating directly on the card, no inner panel. */}
        <div className="mt-4">
          <svg viewBox="0 0 280 150" width="100%" style={{ display: "block" }}>
            <defs>
              {/* Teal open chevron: the thread's single arrowhead. */}
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

            {/* ---- Registration ticks: the diagram baseline / scale rhyme ---- */}
            <g
              stroke={C.inkFaint}
              strokeWidth="1"
              strokeLinecap="round"
              vectorEffect="non-scaling-stroke"
            >
              {[40, 60, 80, 100, 120, 140, 160, 180, 200, 220, 240].map((x) => (
                <line key={x} x1={x} y1="120" x2={x} y2="125" />
              ))}
            </g>

            {/* ---- The teal thread: agent -> createReview -> gate -> patch ----
                 Drawn first so the grey nodes/posts occlude where it passes. */}
            <line
              x1="48"
              y1="66"
              x2="228"
              y2="66"
              stroke={C.accent}
              strokeWidth="1"
              strokeLinecap="round"
              vectorEffect="non-scaling-stroke"
              markerEnd={`url(#${ID}head-teal)`}
            />

            {/* ---- Gate, prior PENDING state: dashed grey blade lifted open ---- */}
            <line
              x1="158"
              y1="66"
              x2="186"
              y2="46"
              stroke={C.inkFaint}
              strokeWidth="1"
              strokeLinecap="round"
              strokeDasharray="2 3"
              vectorEffect="non-scaling-stroke"
            />

            {/* ---- createReview node (grey open circle, occludes the thread) ---- */}
            <circle
              cx="104"
              cy="66"
              r="8"
              fill={C.surface}
              stroke={C.inkFaint}
              strokeWidth="1"
              vectorEffect="non-scaling-stroke"
            />

            {/* ---- Gate terminal posts (occlude the thread at the switch) ---- */}
            <circle
              cx="158"
              cy="66"
              r="3"
              fill={C.surface}
              stroke={C.inkFaint}
              strokeWidth="1"
              vectorEffect="non-scaling-stroke"
            />
            <circle
              cx="198"
              cy="66"
              r="3"
              fill={C.surface}
              stroke={C.inkFaint}
              strokeWidth="1"
              vectorEffect="non-scaling-stroke"
            />

            {/* ---- Source ring: hollow teal, where the thread begins ---- */}
            <circle
              cx="40"
              cy="66"
              r="8"
              fill="none"
              stroke={C.accent}
              strokeWidth="1"
              vectorEffect="non-scaling-stroke"
            />

            {/* ---- Terminal focal node: teal ring + solid teal dot (landed) ---- */}
            <circle
              cx="238"
              cy="66"
              r="8"
              fill="none"
              stroke={C.accent}
              strokeWidth="1"
              vectorEffect="non-scaling-stroke"
            />
            <circle cx="238" cy="66" r="2.5" fill={C.accent} />

            {/* ---- Sparse micro-labels (key 7px / value 8px) ---- */}
            <text
              x="104"
              y="88"
              textAnchor="middle"
              fill={C.ink}
              fontFamily={C.mono}
              fontSize="8"
            >
              createReview()
            </text>
            <text
              x="178"
              y="34"
              textAnchor="middle"
              fill={C.navLabel}
              fontFamily={C.mono}
              fontSize="7"
              letterSpacing="0.08em"
            >
              HUMAN GATE
            </text>
            <text
              x="190"
              y="44"
              textAnchor="start"
              fill={C.navLabel}
              fontFamily={C.mono}
              fontSize="7"
              letterSpacing="0.08em"
            >
              PENDING
            </text>
            <text
              x="178"
              y="88"
              textAnchor="middle"
              fill={C.ink}
              fontFamily={C.mono}
              fontSize="8"
              letterSpacing="0.04em"
            >
              GRANTED
            </text>
          </svg>
        </div>

        {/* Single-element footer: the one number the gate turns on. */}
        <div className="border-cc-card-border mt-4 border-t pt-4">
          <p className="font-heading text-cc-heading text-h4 leading-none font-semibold">
            1
          </p>
          <p className="text-cc-ink-dim mt-1.5 text-xs">
            safe patch, applied after the grant
          </p>
        </div>
      </div>
    </div>
  );
}
