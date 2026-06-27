/**
 * "Agentic coding" scene, concept 3 ("Grounding sources converge to MCP"), v3
 * "Signal & Metrics" (dark cc-* panel).
 *
 * Leads with the measurable result: four grounding artifacts the server already
 * owns (schema, published ops, the client registry, skillz) collapse into ONE
 * grounded `/graphql/mcp` endpoint a coding agent calls. The honest headline is
 * the convergence ratio, so the hero is a centered two-up cream "4 -> 1" over a
 * lowercase mono caption.
 *
 * Demoted beneath the number (topological concept) is a SMALL v2-style funnel:
 * four grey source ticks with mono labels feed grey 1px connectors down to a
 * single apex, where the one teal element (a short stem into a lone filled teal
 * node) lands on the surviving "1" - the `/graphql/mcp` endpoint, named in cream
 * directly under it. All convergence geometry stays grey structure; teal owns
 * only the apex stem and node, so the eye reads "everything grounds here". No
 * status hue: this cell reports a count, nothing is failing. A dashed-divider
 * caption closes with the agent payoff.
 *
 * Content is faithful to the v2 FeedbackVariant3 sibling: the four sources
 * schema, published ops, client registry, skillz converging into `/graphql/mcp`
 * and the "existing artifacts ground the agent" reading. Only the visual
 * language changes to the v3 dark metrics panel.
 *
 * Static settled frame: a React Server Component, no hooks, no motion, no
 * "use client". aria-hidden root; the funnel SVG carries an aria-label.
 */

interface FeedbackVariant3Props {
  readonly className?: string;
}

/* Strict cc-* dark palette mirrored locally per the v3 system. Teal is the only
 * decorative hue and is bound to the hero metric (the one converged endpoint).
 * No status hue: nothing is firing, this cell only reports a count. */
const cc = {
  surface: "#0c1322",
  heading: "#f5f0ea",
  ink: "#a1a3af",
  inkDim: "rgba(245,241,234,0.62)",
  cardBorder: "rgba(245,241,234,0.12)",
  inkFaint: "rgba(245,241,234,0.16)",
  navLabel: "#62748e",
  accent: "#5eead4",
  mono: 'ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, "Liberation Mono", monospace',
  display: '"Josefin Sans", Futura, sans-serif',
} as const;

interface Source {
  /** Existing artifact that grounds the agent, faithful to the v2 sibling. */
  readonly label: string;
  /** Tick center in viewBox units; short labels sit on the outer edges. */
  readonly x: number;
}

const SOURCES: readonly Source[] = [
  { label: "schema", x: 30 },
  { label: "published ops", x: 108 },
  { label: "client registry", x: 186 },
  { label: "skillz", x: 264 },
];

export function FeedbackVariant3({ className }: FeedbackVariant3Props) {
  // Funnel geometry (viewBox 280 x 66). The apex sits on the mean of the four
  // source ticks so the convergence reads symmetric.
  const apexX = 147;
  const tickTop = 11;
  const tickBottom = 18;
  const apexY = 38;
  const nodeY = 48;

  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      aria-hidden="true"
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        {/* eyebrow: the view this number measures */}
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          grounding sources
        </p>

        {/* hero: the convergence ratio, the honest headline metric */}
        <div className="mt-3 flex items-baseline gap-2.5">
          <span
            className="text-cc-heading leading-none font-semibold"
            style={{ fontFamily: cc.display, fontSize: "2rem" }}
          >
            4
          </span>
          <span
            aria-hidden="true"
            className="font-mono leading-none"
            style={{ fontSize: "1.05rem", color: cc.inkFaint }}
          >
            &rarr;
          </span>
          <span
            className="text-cc-heading leading-none font-semibold"
            style={{ fontFamily: cc.display, fontSize: "2rem" }}
          >
            1
          </span>
        </div>
        <p
          className="mt-1.5 lowercase"
          style={{ fontFamily: cc.mono, fontSize: "0.7rem", color: cc.inkDim }}
        >
          sources to one endpoint
        </p>

        {/* the one teal signal (demoted under the number): four grey source
            ticks feed grey connectors into a single apex; one teal stem drops to
            the lone teal node, the grounded /graphql/mcp endpoint named in cream */}
        <div className="border-cc-card-border bg-cc-surface mt-4 rounded-lg border px-3 py-2.5">
          <svg
            viewBox="0 0 280 66"
            width="100%"
            role="img"
            aria-label="Four grounding sources (schema, published ops, client registry, skillz) converge into one /graphql/mcp endpoint."
            style={{ display: "block" }}
          >
            {SOURCES.map((s) => (
              <g key={s.label}>
                {/* source label (an input, merely present) */}
                <text
                  x={s.x}
                  y={7}
                  textAnchor="middle"
                  style={{
                    fontFamily: cc.mono,
                    fontSize: 7,
                    fill: cc.navLabel,
                  }}
                >
                  {s.label}
                </text>
                {/* source tick */}
                <line
                  x1={s.x}
                  y1={tickTop}
                  x2={s.x}
                  y2={tickBottom}
                  stroke={cc.ink}
                  strokeWidth="1"
                  opacity={0.5}
                />
                {/* grey merge connector: source tick -> shared apex (structure) */}
                <line
                  x1={s.x}
                  y1={tickBottom}
                  x2={apexX}
                  y2={apexY}
                  stroke={cc.inkFaint}
                  strokeWidth="1"
                />
              </g>
            ))}

            {/* the one teal element: a single stem from the apex to the node */}
            <line
              x1={apexX}
              y1={apexY}
              x2={apexX}
              y2={nodeY}
              stroke={cc.accent}
              strokeWidth="1"
              opacity={0.85}
            />
            {/* the lone filled teal node: the surviving "1", ringed on surface */}
            <circle
              cx={apexX}
              cy={nodeY}
              r={4.5}
              fill={cc.surface}
              stroke={cc.accent}
              strokeWidth="1"
              strokeOpacity={0.5}
            />
            <circle cx={apexX} cy={nodeY} r={2.8} fill={cc.accent} />

            {/* the grounded endpoint name, cream (a fact, never teal) */}
            <text
              x={apexX}
              y={63}
              textAnchor="middle"
              style={{ fontFamily: cc.mono, fontSize: 9, fill: cc.heading }}
            >
              /graphql/mcp
            </text>
          </svg>
        </div>

        {/* dashed-divider interpretation: the agent payoff */}
        <div className="border-cc-ink-faint mt-3.5 border-t border-dashed pt-3">
          <p
            className="text-center lowercase"
            style={{
              fontFamily: cc.mono,
              fontSize: "0.62rem",
              color: cc.inkDim,
            }}
          >
            existing artifacts ground the agent
          </p>
        </div>
      </div>
    </div>
  );
}
