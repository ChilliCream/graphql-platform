interface FeedbackVariant2Props {
  readonly className?: string;
}

/**
 * Agentic-coding scene, variant 2 (v5 "Schematic Lines"): the MCP tool catalog.
 *
 * Skeleton: a sparse grid of tool rings. The published operations exposed at
 * /graphql/mcp are drawn as a quiet 2x2 grid of grey open circles, the exact
 * tool surface an agent can list. A hollow teal source ring on the left is the
 * coding agent; the single teal thread reaches into the catalog and terminates
 * on exactly one tool, the focal ring stroked teal with a solid teal terminal
 * dot. That one selected tool is the call the agent makes, labelled with its
 * kind tag and behavior hint (MUTATION, IDEMPOTENT) so the safety of the call
 * is legible. Every other tool stays grey and unselected.
 *
 * The teal thread is the single accent: the one route the headline names through
 * an otherwise all-grey schematic. A faint strip of registration ticks acts as
 * the catalog baseline. Static settled frame, no hooks, no animation. Every svg
 * id is prefixed `v5-feedback-2-`.
 */

const C = {
  surface: "#0c1322",
  ink: "#a1a3af",
  inkFaint: "rgba(245,241,234,0.16)",
  navLabel: "#62748e",
  accent: "#5eead4",
  mono: 'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace',
} as const;

const ID = "v5-feedback-2-";

export function FeedbackVariant2({ className }: FeedbackVariant2Props) {
  // The catalog: four published operations exposed as MCP tools. The selected
  // one is the tool the agent's teal thread terminates on.
  const tools: readonly { readonly cx: number; readonly cy: number }[] = [
    { cx: 176, cy: 54 },
    { cx: 240, cy: 54 },
    { cx: 176, cy: 104 },
    { cx: 240, cy: 104 },
  ];

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
          mcp tool catalog
        </p>

        {/* Monoline schematic floating directly on the card, no inner panel. */}
        <svg
          width="100%"
          viewBox="0 0 280 150"
          fill="none"
          className="mt-4 block"
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

          {/* ---- Registration ticks: the catalog baseline / scale rhyme ---- */}
          <g
            stroke={C.inkFaint}
            strokeWidth="1"
            strokeLinecap="round"
            vectorEffect="non-scaling-stroke"
          >
            {[
              44, 60, 76, 92, 108, 124, 140, 156, 172, 188, 204, 220, 236, 252,
            ].map((x) => (
              <line key={x} x1={x} y1="128" x2={x} y2="133" />
            ))}
          </g>

          {/* ---- Catalog: grid of grey open tool rings ---- */}
          <g
            stroke={C.inkFaint}
            strokeWidth="1"
            strokeLinecap="round"
            strokeLinejoin="round"
            vectorEffect="non-scaling-stroke"
          >
            {tools.slice(1).map((t) => (
              <circle
                key={`${t.cx}-${t.cy}`}
                cx={t.cx}
                cy={t.cy}
                r="8"
                fill="none"
              />
            ))}
          </g>

          {/* ---- The teal thread: agent reaches into the catalog and calls one
               tool. One continuous polyline, single arrowhead into the focal
               ring. ---- */}
          <polyline
            points="48,76 176,76 176,62"
            fill="none"
            stroke={C.accent}
            strokeWidth="1"
            strokeLinecap="round"
            strokeLinejoin="round"
            vectorEffect="non-scaling-stroke"
            markerEnd={`url(#${ID}arrow-teal)`}
          />

          {/* ---- Source ring: hollow teal, the coding agent ---- */}
          <circle
            cx="40"
            cy="76"
            r="8"
            fill="none"
            stroke={C.accent}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
          />

          {/* ---- Focal tool: teal ring + solid teal terminal dot (the call) ---- */}
          <circle
            cx="176"
            cy="54"
            r="8"
            fill={C.surface}
            stroke={C.accent}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
          />
          <circle cx="176" cy="54" r="2.5" fill={C.accent} />

          {/* ---- Sparse micro-labels ---- */}
          <text
            x="40"
            y="99"
            textAnchor="middle"
            fill={C.navLabel}
            fontFamily={C.mono}
            fontSize="7"
            letterSpacing="0.08em"
          >
            AGENT
          </text>
          <text
            x="176"
            y="25"
            textAnchor="middle"
            fill={C.navLabel}
            fontFamily={C.mono}
            fontSize="7"
            letterSpacing="0.08em"
          >
            MUTATION · IDEMPOTENT
          </text>
          <text
            x="176"
            y="37"
            textAnchor="middle"
            fill={C.ink}
            fontFamily={C.mono}
            fontSize="8"
          >
            addToCart()
          </text>
        </svg>

        {/* Single-element footer: the size of the callable surface. */}
        <div className="border-cc-card-border mt-4 border-t pt-4">
          <p className="font-heading text-cc-heading text-h4 leading-none font-semibold">
            4
          </p>
          <p className="text-cc-ink-dim mt-1.5 text-xs">
            callable operations at /graphql/mcp
          </p>
        </div>
      </div>
    </div>
  );
}
