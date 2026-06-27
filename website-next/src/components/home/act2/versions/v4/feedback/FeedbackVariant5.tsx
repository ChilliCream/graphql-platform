interface FeedbackVariant5Props {
  readonly className?: string;
}

/**
 * Agentic-coding scene, v4 "Generated Artifacts", concept 5 ("SKILL.md as source
 * of truth"). Locked v4 PATTERN A: a single hero artifact tile with exactly one
 * teal callout.
 *
 * One full-width tile on cc-surface renders the checked-in `SKILL.md` the agent
 * loads before it calls the MCP tools. A header band carries the cream filename
 * and a `markdown` kind tag, closed by a 1px divider; below it six monochrome
 * mono lines borrowed verbatim from the v1 sibling: the YAML frontmatter
 * (`name: search-eshops-catalog`), a `tools` entry naming `createReview` with its
 * one coral `@destructive` hint, and the fenced GraphQL example posted to
 * `/graphql/mcp` (`{ searchCatalog(term: "shoes") { id name } }`).
 *
 * The single teal accent cluster is the signature callout on the one load-bearing
 * token, `/graphql/mcp`: a 2px teal underline tick, a 2.5px anchor dot, a 1px
 * leader into the line's negative space, and the 7px uppercase "MCP" micro-label
 * (a fixed verb-set label). Strip the teal and the artifact still reads as neutral
 * mono code; coral stays on its own `@destructive` token and never carries a
 * callout, so teal and coral never compete as focal points.
 *
 * React Server Component: no "use client", no hooks, no animation, settled final
 * frame. Every svg id is prefixed "v4-feedback-5-".
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
} as const;

const MONO =
  'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace';

const ID = "v4-feedback-5-";

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

        <div className="mt-4">
          <svg viewBox="0 0 320 148" width="100%" style={{ display: "block" }}>
            <defs>
              {/* Open teal chevron for the single callout leader. */}
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
                  stroke={C.accent}
                  strokeWidth="1"
                />
              </marker>
            </defs>

            {/* Hero tile: the checked-in SKILL.md artifact. */}
            <rect
              x={6}
              y={2}
              width={308}
              height={144}
              rx={8}
              fill={C.surface}
              stroke={C.cardBorder}
              strokeWidth={1}
            />

            {/* Header band: file glyph + cream filename + markdown kind tag. */}
            <path
              d="M13 7.5 H18.5 L22 11 V19 H13 Z"
              fill="none"
              stroke={C.inkDim}
              strokeWidth={1}
              strokeLinejoin="round"
            />
            <path
              d="M18.5 7.5 V11 H22"
              fill="none"
              stroke={C.inkDim}
              strokeWidth={1}
              strokeLinejoin="round"
            />
            <text
              x={29}
              y={16.5}
              fontFamily={MONO}
              fontSize={10}
              fontWeight={600}
              fill={C.heading}
            >
              SKILL.md
            </text>
            <text
              x={306}
              y={16.5}
              textAnchor="end"
              fontFamily={MONO}
              fontSize={8}
              letterSpacing="0.1em"
              fill={C.navLabel}
            >
              markdown
            </text>
            <line
              x1={6}
              y1={25}
              x2={314}
              y2={25}
              stroke={C.cardBorder}
              strokeWidth={1}
            />

            {/* L1: frontmatter fence. */}
            <text x={14} y={40} fontFamily={MONO} fontSize={9} fill={C.inkDim}>
              ---
            </text>

            {/* L2: name key + value. */}
            <text x={14} y={57} fontFamily={MONO} fontSize={9}>
              <tspan fill={C.navLabel}>{"name: "}</tspan>
              <tspan fill={C.ink}>search-eshops-catalog</tspan>
            </text>

            {/* L3: tools entry, carrying the one coral @destructive hint. */}
            <text x={14} y={74} fontFamily={MONO} fontSize={9}>
              <tspan fill={C.navLabel}>{"tools: "}</tspan>
              <tspan fill={C.ink}>createReview</tspan>
              <tspan fill={C.coral}>{"  @destructive"}</tspan>
            </text>

            {/* L4: frontmatter fence. */}
            <text x={14} y={91} fontFamily={MONO} fontSize={9} fill={C.inkDim}>
              ---
            </text>

            {/* L5: the fenced endpoint comment (carries the single teal token). */}
            <text x={14} y={108} fontFamily={MONO} fontSize={9}>
              <tspan fill={C.navLabel}>{"# POST "}</tspan>
              <tspan fill={C.accent}>/graphql/mcp</tspan>
            </text>

            {/* L6: the GraphQL operation, monochrome mono. */}
            <text x={14} y={125} fontFamily={MONO} fontSize={9}>
              <tspan fill={C.navLabel}>{"{ "}</tspan>
              <tspan fill={C.ink}>searchCatalog</tspan>
              <tspan fill={C.navLabel}>{"("}</tspan>
              <tspan fill={C.ink}>term</tspan>
              <tspan fill={C.navLabel}>{": "}</tspan>
              <tspan fill={C.ink}>{'"shoes"'}</tspan>
              <tspan fill={C.navLabel}>{") { "}</tspan>
              <tspan fill={C.ink}>id name</tspan>
              <tspan fill={C.navLabel}>{" } }"}</tspan>
            </text>

            {/* Signature teal callout on /graphql/mcp: tick, dot, leader, label. */}
            <line
              x1={52}
              y1={112}
              x2={116}
              y2={112}
              stroke={C.accent}
              strokeWidth={2}
              strokeLinecap="round"
            />
            <circle cx={119} cy={108} r={2.5} fill={C.accent} />
            <line
              x1={123}
              y1={108}
              x2={248}
              y2={108}
              stroke={C.accent}
              strokeWidth={1}
              markerEnd={`url(#${ID}head)`}
            />
            <text
              x={254}
              y={111}
              fontFamily={MONO}
              fontSize={7}
              letterSpacing="0.12em"
              fill={C.accent}
            >
              MCP
            </text>
          </svg>
        </div>

        {/* Dashed caption: the artifact is reviewed and checked in, not improvised. */}
        <div className="border-cc-ink-faint mt-4 border-t border-dashed pt-3">
          <p className="text-cc-ink-dim text-center text-xs">
            a reviewed file grounds the agent, not a guess
          </p>
        </div>
      </div>
    </div>
  );
}
