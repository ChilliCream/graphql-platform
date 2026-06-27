interface FeedbackVariant1Props {
  readonly className?: string;
}

/**
 * Agentic-coding scene, variant 1 (v4 "Generated Artifacts"): approval-gated
 * agent action.
 *
 * Two real artifact tiles on cc-surface joined by a grey route that is broken by
 * one human approval gate. The top tile is the agent transcript: the
 * `createReview` MCP call and the staged review id `rev_8f2c`. The route drops
 * into a gate node whose status settles from a neutral PENDING pill to a healthy
 * GRANTED pill. Only then does the route reach the bottom tile, the emitted
 * `schema.graphql` safe patch (`+ discountedPrice: Money`) on a faint healthy
 * diff-add row.
 *
 * The single teal callout is the version signature, sitting only on the gate:
 * a 3px teal anchor dot on the gate node, a 1px teal leader into the right
 * margin, a 2px teal underline tick, and a "GATED" micro-label. Teal is never
 * reused on borders, connectors, or status. Healthy green is the one orthogonal
 * status hue, used for the GRANTED verdict and the diff-add marker, and it sits
 * in a different region of the cell than the teal callout (mirroring
 * ObserveVariant1, where teal owns the callout and the status hue owns the pill).
 *
 * Literal content (createReview, schema "eshops/api", rev_8f2c, PENDING/GRANTED,
 * + discountedPrice: Money) is borrowed verbatim from the v1 sibling. React
 * Server Component, settled final frame, no hooks, no animation. Every svg id is
 * prefixed "v4-feedback-1-".
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
  healthy: "#34d399",
  mono: 'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace',
} as const;

const ID = "v4-feedback-1-";

export function FeedbackVariant1({ className }: FeedbackVariant1Props) {
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
          approval-gated action
        </p>

        <div className="mt-4">
          <svg
            viewBox="0 0 300 152"
            width="100%"
            role="img"
            aria-label="Approval-gated agent action: the agent's createReview call is held at a human gate, pending then granted, before one safe schema patch lands."
            style={{ display: "block" }}
          >
            <defs>
              {/* Grey open arrowhead for the gated route hops. */}
              <marker
                id={`${ID}headGrey`}
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
                  stroke={C.inkFaint}
                  strokeWidth="1"
                />
              </marker>
              {/* Teal open arrowhead for the single callout leader. */}
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

            {/* ---- Grey route connectors (drawn under, broken by the gate) ---- */}
            <line
              x1={111}
              y1={52}
              x2={111}
              y2={61}
              stroke={C.inkFaint}
              strokeWidth={1}
              markerEnd={`url(#${ID}headGrey)`}
            />
            <line
              x1={111}
              y1={92}
              x2={111}
              y2={99}
              stroke={C.inkFaint}
              strokeWidth={1}
              markerEnd={`url(#${ID}headGrey)`}
            />

            {/* ---- Tile 1: agent transcript (createReview MCP call) ---- */}
            <rect
              x={6}
              y={4}
              width={210}
              height={48}
              rx={8}
              fill={C.surface}
              stroke={C.cardBorder}
              strokeWidth={1}
            />
            <text
              x={14}
              y={17.5}
              fontFamily={C.mono}
              fontSize={9}
              fill={C.inkDim}
            >
              agent
            </text>
            <text
              x={208}
              y={17.5}
              textAnchor="end"
              fontFamily={C.mono}
              fontSize={8}
              fill={C.navLabel}
            >
              .mcp
            </text>
            <line
              x1={6}
              y1={24}
              x2={216}
              y2={24}
              stroke={C.cardBorder}
              strokeWidth={1}
            />
            {/* tool call line */}
            <text x={14} y={38} fontFamily={C.mono} fontSize={8.5}>
              <tspan fill={C.inkDim}>&rarr; </tspan>
              <tspan fill={C.ink}>createReview</tspan>
              <tspan fill={C.navLabel}>(</tspan>
              <tspan fill={C.navLabel}>schema: </tspan>
              <tspan fill={C.ink}>&quot;eshops/api&quot;</tspan>
              <tspan fill={C.navLabel}>)</tspan>
            </text>
            {/* staged review id line */}
            <text x={14} y={48} fontFamily={C.mono} fontSize={8.5}>
              <tspan fill={C.inkDim}>&larr; review </tspan>
              <tspan fill={C.ink}>rev_8f2c</tspan>
              <tspan fill={C.inkDim}> staged</tspan>
            </text>

            {/* ---- Gate node: human approval, PENDING -> GRANTED ---- */}
            <rect
              x={45}
              y={63}
              width={132}
              height={29}
              rx={8}
              fill={C.surface}
              stroke={C.cardBorder}
              strokeWidth={1}
            />
            <text
              x={111}
              y={73}
              textAnchor="middle"
              fontFamily={C.mono}
              fontSize={6}
              letterSpacing="0.12em"
              fill={C.navLabel}
            >
              HUMAN GATE
            </text>
            {/* neutral PENDING pill (settled past state) */}
            <rect
              x={51}
              y={79}
              width={42}
              height={11}
              rx={3}
              fill="none"
              stroke={C.inkFaint}
              strokeWidth={1}
            />
            <text
              x={72}
              y={87}
              textAnchor="middle"
              fontFamily={C.mono}
              fontSize={6.5}
              letterSpacing="0.04em"
              fill={C.navLabel}
            >
              PENDING
            </text>
            <text
              x={100}
              y={87.5}
              textAnchor="middle"
              fontFamily={C.mono}
              fontSize={8}
              fill={C.inkFaint}
            >
              &rarr;
            </text>
            {/* healthy GRANTED pill (the one status hue, owns the verdict) */}
            <rect
              x={107}
              y={79}
              width={44}
              height={11}
              rx={3}
              fill={C.healthy}
              fillOpacity={0.1}
              stroke={C.healthy}
              strokeWidth={1}
            />
            <text
              x={129}
              y={87}
              textAnchor="middle"
              fontFamily={C.mono}
              fontSize={6.5}
              letterSpacing="0.04em"
              fill={C.healthy}
            >
              GRANTED
            </text>

            {/* ---- The single teal callout: anchor dot -> leader -> GATED ---- */}
            <circle cx={177} cy={78} r={2.5} fill={C.accent} />
            <path
              d="M177 78 C 200 78, 212 70, 226 68"
              fill="none"
              stroke={C.accent}
              strokeWidth={1}
              markerEnd={`url(#${ID}headTeal)`}
            />
            <text
              x={232}
              y={71}
              fontFamily={C.mono}
              fontSize={7}
              letterSpacing="0.14em"
              fill={C.accent}
            >
              GATED
            </text>
            <line
              x1={232}
              y1={75}
              x2={264}
              y2={75}
              stroke={C.accent}
              strokeWidth={2}
            />

            {/* ---- Tile 2: emitted safe patch (schema.graphql diff) ---- */}
            <rect
              x={6}
              y={100}
              width={210}
              height={48}
              rx={8}
              fill={C.surface}
              stroke={C.cardBorder}
              strokeWidth={1}
            />
            <text
              x={14}
              y={113.5}
              fontFamily={C.mono}
              fontSize={9}
              fill={C.inkDim}
            >
              schema.graphql
            </text>
            <text
              x={208}
              y={113.5}
              textAnchor="end"
              fontFamily={C.mono}
              fontSize={8}
              fill={C.navLabel}
            >
              .graphql
            </text>
            <line
              x1={6}
              y1={120}
              x2={216}
              y2={120}
              stroke={C.cardBorder}
              strokeWidth={1}
            />
            {/* faint healthy diff-add row tint */}
            <rect
              x={8}
              y={125}
              width={206}
              height={15}
              rx={2}
              fill={C.healthy}
              fillOpacity={0.09}
            />
            <text
              x={12}
              y={135.5}
              fontFamily={C.mono}
              fontSize={8.5}
              fill={C.healthy}
            >
              +
            </text>
            <text x={24} y={135.5} fontFamily={C.mono} fontSize={8.5}>
              <tspan fill={C.ink}>discountedPrice</tspan>
              <tspan fill={C.inkDim}>: </tspan>
              <tspan fill={C.ink}>Money</tspan>
            </text>
          </svg>
        </div>

        {/* Two-stat footer: the numbers the gate turns on (keeps numerals). */}
        <div className="border-cc-card-border mt-4 grid grid-cols-2 gap-4 border-t pt-4">
          <div>
            <p className="font-heading text-cc-heading text-h4 leading-none font-semibold">
              1
            </p>
            <p className="text-cc-ink-dim mt-1.5 text-xs">safe patch applied</p>
          </div>
          <div>
            <p className="font-heading text-cc-heading text-h4 leading-none font-semibold">
              0
            </p>
            <p className="text-cc-ink-dim mt-1.5 text-xs">
              writes before grant
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}
