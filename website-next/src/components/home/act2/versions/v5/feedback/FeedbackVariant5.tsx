interface FeedbackVariant5Props {
  readonly className?: string;
}

/**
 * Agentic-coding scene, variant 5 (v5 "Schematic Lines"): SKILL.md as the
 * source of truth.
 *
 * A reductive hub-and-spoke schematic. The hub is the reviewed, checked-in
 * SKILL.md, drawn as the single hollow teal source ring. Three grey spokes fan
 * left into the artifact's contents (frontmatter, the /graphql/mcp example, and
 * the createReview @destructive hint), the structure an agent loads to drive
 * the MCP tools. From the hub, the one teal thread (the answer path) runs right
 * to a focal teal node: the coding agent it grounds. Everything structural is
 * 1px cc-ink-faint grey; teal appears only on the source ring, the thread, its
 * arrowhead, and the terminal dot.
 *
 * Settled final frame: static, no animation, no hooks, no client APIs. React
 * Server Component. Every svg id is prefixed "v5-feedback-5-".
 */

const C = {
  surface: "#0c1322", // occluder under a node where a spoke passes behind
  inkFaint: "rgba(245,241,234,0.16)", // every grey structure stroke
  ink: "#a1a3af", // mono value-labels
  navLabel: "#62748e", // mono key-labels
  accent: "#5eead4", // the one accent: source ring, thread, dot, arrowhead
  mono: 'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace',
} as const;

const ID = "v5-feedback-5-";

// Hub-and-spoke: SKILL.md hub, three content spokes, one teal thread to agent.
const HUB = { x: 158, y: 74 } as const;
const AGENT = { x: 240, y: 74 } as const;

// The artifact's contents, fanned left of the hub as grey open rings.
const SPOKES = [
  { y: 38, label: "frontmatter" },
  { y: 74, label: "/graphql/mcp" },
  { y: 110, label: "createReview" },
] as const;

const SPOKE_X = 84;

export function FeedbackVariant5({ className }: FeedbackVariant5Props) {
  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      aria-hidden="true"
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          source of truth
        </p>

        {/* Monoline schematic floating directly on the card, no inner panel. */}
        <svg
          width="100%"
          viewBox="0 0 280 150"
          fill="none"
          className="mt-3 block"
          aria-hidden="true"
        >
          <defs>
            {/* Teal open chevron: the thread's single arrowhead. */}
            <marker
              id={`${ID}arrow-teal`}
              markerUnits="userSpaceOnUse"
              markerWidth="6"
              markerHeight="6"
              refX="5"
              refY="3"
              orient="auto"
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

          {/* grey spokes: hub center -> each content node (occluded under rings) */}
          <g
            stroke={C.inkFaint}
            strokeWidth="1"
            strokeLinecap="round"
            strokeLinejoin="round"
            vectorEffect="non-scaling-stroke"
          >
            {SPOKES.map((s) => (
              <line
                key={`${ID}spoke-${s.y}`}
                x1={SPOKE_X}
                y1={s.y}
                x2={HUB.x}
                y2={HUB.y}
              />
            ))}
          </g>

          {/* the single teal thread: SKILL.md hub -> the coding agent it grounds */}
          <line
            x1={HUB.x + 11}
            y1={HUB.y}
            x2={AGENT.x - 8}
            y2={AGENT.y}
            stroke={C.accent}
            strokeWidth="1"
            strokeLinecap="round"
            vectorEffect="non-scaling-stroke"
            markerEnd={`url(#${ID}arrow-teal)`}
          />

          {/* occlude spokes beneath the content nodes, then draw open grey rings */}
          {SPOKES.map((s) => (
            <circle
              key={`${ID}occ-${s.y}`}
              cx={SPOKE_X}
              cy={s.y}
              r="6"
              fill={C.surface}
            />
          ))}
          {SPOKES.map((s) => (
            <circle
              key={`${ID}node-${s.y}`}
              cx={SPOKE_X}
              cy={s.y}
              r="5"
              fill="none"
              stroke={C.inkFaint}
              strokeWidth="1"
              vectorEffect="non-scaling-stroke"
            />
          ))}

          {/* the SKILL.md hub: hollow teal source ring (occlude spokes first) */}
          <circle cx={HUB.x} cy={HUB.y} r="10" fill={C.surface} />
          <circle
            cx={HUB.x}
            cy={HUB.y}
            r="11"
            fill="none"
            stroke={C.accent}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
          />

          {/* focal agent node: teal ring + solid teal terminal dot */}
          <circle
            cx={AGENT.x}
            cy={AGENT.y}
            r="8"
            fill="none"
            stroke={C.accent}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
          />
          <circle cx={AGENT.x} cy={AGENT.y} r="2.5" fill={C.accent} />

          {/* registration baseline ticks under the grounded flow (the scale) */}
          <g
            stroke={C.inkFaint}
            strokeWidth="1"
            strokeLinecap="round"
            vectorEffect="non-scaling-stroke"
          >
            {[128, 144, 160, 176, 192, 208, 224, 240].map((x) => (
              <line key={`${ID}tick-${x}`} x1={x} y1={126} x2={x} y2={131} />
            ))}
          </g>

          {/* sparse mono micro-labels: the three contents + the hub */}
          <text
            x={72}
            y={41}
            textAnchor="end"
            fontFamily={C.mono}
            fontSize="8"
            fill={C.ink}
          >
            frontmatter
          </text>
          <text
            x={72}
            y={77}
            textAnchor="end"
            fontFamily={C.mono}
            fontSize="8"
            fill={C.ink}
          >
            /graphql/mcp
          </text>
          <text
            x={72}
            y={107}
            textAnchor="end"
            fontFamily={C.mono}
            fontSize="8"
            fill={C.ink}
          >
            createReview
          </text>
          <text
            x={72}
            y={117}
            textAnchor="end"
            fontFamily={C.mono}
            fontSize="7"
            letterSpacing="0.06em"
            fill={C.navLabel}
          >
            @destructive
          </text>
          <text
            x={HUB.x}
            y={100}
            textAnchor="middle"
            fontFamily={C.mono}
            fontSize="9"
            fill={C.ink}
          >
            SKILL.md
          </text>
        </svg>

        {/* Single-element footer: one dim caption, gated by the shared rule. */}
        <div className="border-cc-card-border mt-4 border-t pt-4">
          <p className="text-cc-ink-dim text-xs">
            a reviewed, checked-in artifact grounds the agent
          </p>
        </div>
      </div>
    </div>
  );
}
