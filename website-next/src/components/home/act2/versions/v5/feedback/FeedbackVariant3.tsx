interface FeedbackVariant3Props {
  readonly className?: string;
}

/**
 * Agentic-coding scene, variant 3 (v5 "Schematic Lines"): grounding sources
 * converge to MCP.
 *
 * Reduced to its essential structure: four grey grounding-source rings (schema,
 * published ops, client registry, skillz) fan inward along grey 1px relations
 * into one hub, the hollow teal `/graphql/mcp` source ring. From that core the
 * single teal thread leaves as one tool-call and terminates on the grounded
 * coding agent (teal ring + solid teal dot). The four sources stay grey because
 * they are merely present; the teal thread is the one route the headline names,
 * the single thing the eye lands on.
 *
 * A faint strip of registration ticks acts as the diagram baseline. Fully
 * static, no hooks, no animation, settled final frame. Every svg id is prefixed
 * `v5-feedback-3-`.
 */

const C = {
  surface: "#0c1322",
  ink: "#a1a3af",
  inkFaint: "rgba(245,241,234,0.16)",
  accent: "#5eead4",
  mono: 'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace',
} as const;

const ID = "v5-feedback-3-";

/** The four existing artifacts that ground the agent, converging into the core. */
const SOURCES: readonly { readonly label: string; readonly y: number }[] = [
  { label: "schema", y: 20 },
  { label: "published ops", y: 48 },
  { label: "client registry", y: 76 },
  { label: "skillz", y: 104 },
];

const SRC_X = 100;
const SRC_R = 8;
const HUB = { x: 200, y: 62, r: 11 } as const;
const AGENT = { x: 246, y: 62, r: 8 } as const;

const TICKS: readonly number[] = [
  100, 116, 132, 148, 164, 180, 196, 212, 228, 244,
];

export function FeedbackVariant3({ className }: FeedbackVariant3Props) {
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
          grounding sources converge to mcp
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
              {TICKS.map((x) => (
                <line key={x} x1={x} y1="130" x2={x} y2="135" />
              ))}
            </g>

            {/* ---- Grey grounding relations: each source fans into the hub ---- */}
            <g
              stroke={C.inkFaint}
              strokeWidth="1"
              strokeLinecap="round"
              strokeLinejoin="round"
              vectorEffect="non-scaling-stroke"
            >
              {SOURCES.map((s) => {
                const dx = HUB.x - SRC_X;
                const dy = HUB.y - s.y;
                const len = Math.hypot(dx, dy);
                const ux = dx / len;
                const uy = dy / len;
                return (
                  <line
                    key={s.label}
                    x1={SRC_X + ux * SRC_R}
                    y1={s.y + uy * SRC_R}
                    x2={HUB.x - ux * (HUB.r + 1)}
                    y2={HUB.y - uy * (HUB.r + 1)}
                  />
                );
              })}
            </g>

            {/* ---- Source rings: grey open circles (merely present) ---- */}
            <g
              fill="none"
              stroke={C.inkFaint}
              strokeWidth="1"
              vectorEffect="non-scaling-stroke"
            >
              {SOURCES.map((s) => (
                <circle key={s.label} cx={SRC_X} cy={s.y} r={SRC_R} />
              ))}
            </g>

            {/* ---- The teal thread: /graphql/mcp -> one tool-call -> agent ---- */}
            <line
              x1={HUB.x + HUB.r}
              y1={HUB.y}
              x2={AGENT.x - AGENT.r - 2}
              y2={HUB.y}
              stroke={C.accent}
              strokeWidth="1"
              strokeLinecap="round"
              vectorEffect="non-scaling-stroke"
              markerEnd={`url(#${ID}head-teal)`}
            />

            {/* ---- Hub: the hollow teal source ring (the active core) ---- */}
            <circle
              cx={HUB.x}
              cy={HUB.y}
              r={HUB.r}
              fill={C.surface}
              stroke={C.accent}
              strokeWidth="1"
              vectorEffect="non-scaling-stroke"
            />

            {/* ---- Terminal focal node: teal ring + solid teal dot (grounded) ---- */}
            <circle
              cx={AGENT.x}
              cy={AGENT.y}
              r={AGENT.r}
              fill="none"
              stroke={C.accent}
              strokeWidth="1"
              vectorEffect="non-scaling-stroke"
            />
            <circle cx={AGENT.x} cy={AGENT.y} r="2.5" fill={C.accent} />

            {/* ---- Sparse micro-labels: four source values + the core ---- */}
            {SOURCES.map((s) => (
              <text
                key={s.label}
                x={SRC_X - SRC_R - 4}
                y={s.y + 2.8}
                textAnchor="end"
                fill={C.ink}
                fontFamily={C.mono}
                fontSize="8"
              >
                {s.label}
              </text>
            ))}
            <text
              x={HUB.x}
              y={HUB.y + HUB.r + 12}
              textAnchor="middle"
              fill={C.ink}
              fontFamily={C.mono}
              fontSize="8.5"
            >
              /graphql/mcp
            </text>
          </svg>
        </div>

        {/* Single-element footer: four sources in, one tool-call out. */}
        <div className="border-cc-card-border mt-4 border-t pt-4">
          <p className="font-heading text-cc-heading text-h4 leading-none font-semibold">
            4
          </p>
          <p className="text-cc-ink-dim mt-1.5 text-xs">
            grounding sources, one tool-call
          </p>
        </div>
      </div>
    </div>
  );
}
